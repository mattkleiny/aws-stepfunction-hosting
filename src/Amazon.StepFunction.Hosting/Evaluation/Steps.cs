using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Amazon.StepFunction.Hosting.Definition;

namespace Amazon.StepFunction.Hosting.Evaluation
{
  /// <summary>Defines a possible step in a <see cref="StepFunction"/>.</summary>
  internal abstract record Step
  {
    public string Name { get; set; }

    public Task<Transition> ExecuteAsync(Impositions impositions, CancellationToken cancellationToken = default)
    {
      return ExecuteAsync(StepFunctionData.None, impositions, cancellationToken);
    }

    public async Task<Transition> ExecuteAsync(StepFunctionData data, Impositions impositions, CancellationToken cancellationToken = default)
    {
      try
      {
        return await ExecuteInnerAsync(data, impositions, cancellationToken);
      }
      catch (Exception exception)
      {
        return Transitions.Fail(exception);
      }
    }

    protected abstract Task<Transition> ExecuteInnerAsync(StepFunctionData data, Impositions impositions, CancellationToken cancellationToken);

    /// <summary>A <see cref="Step"/> that passes it's input to output.</summary>
    public sealed record PassStep : Step
    {
      public string Next  { get; set; } = string.Empty;
      public bool   IsEnd { get; set; } = false;

      protected override Task<Transition> ExecuteInnerAsync(StepFunctionData data, Impositions impositions, CancellationToken cancellationToken)
      {
        var transition = IsEnd
          ? Transitions.Succeed(data)
          : Transitions.Next(Next, data);

        return Task.FromResult(transition);
      }
    }

    /// <summary>A <see cref="Step"/> that invokes a handler for some given resource.</summary>
    public sealed record TaskStep : Step
    {
      private readonly Func<StepHandler> factory;

      public TaskStep(Func<StepHandler> factory)
      {
        this.factory = factory;
      }

      public TimeSpan Timeout    { get; set; } = TimeSpan.MaxValue;
      public string   Next       { get; set; } = string.Empty;
      public string   InputPath  { get; set; } = string.Empty;
      public string   OutputPath { get; set; } = string.Empty;
      public bool     IsEnd      { get; set; } = false;

      public RetryPolicy RetryPolicy { get; set; } = RetryPolicies.None;
      public CatchPolicy CatchPolicy { get; set; } = CatchPolicies.None;

      protected override async Task<Transition> ExecuteInnerAsync(StepFunctionData data, Impositions impositions, CancellationToken cancellationToken)
      {
        var output = await CatchPolicy(async () =>
        {
          var input = StepFunctionData.Wrap(data.Query<object>(InputPath));

          return await RetryPolicy(async () =>
          {
            using var timeoutToken = new CancellationTokenSource(impositions.TimeoutOverride.GetValueOrDefault(Timeout));
            using var linkedTokens = CancellationTokenSource.CreateLinkedTokenSource(timeoutToken.Token, cancellationToken);

            var handler = factory();

            return await handler(input, linkedTokens.Token);
          });
        });

        // TODO: support transform on the output path
        // TODO: support result paths
        // TODO: parse the actual retry policy
        // TODO: parse the actual catch policy

        return IsEnd
          ? Transitions.Succeed(output)
          : Transitions.Next(Next, output);
      }
    }

    /// <summary>A <see cref="Step"/> that merely waits some given interval of time.</summary>
    public sealed record WaitStep : Step
    {
      public TimeSpan Duration { get; set; } = TimeSpan.FromSeconds(300);
      public string   Next     { get; set; } = string.Empty;
      public bool     IsEnd    { get; set; } = false;

      protected override async Task<Transition> ExecuteInnerAsync(StepFunctionData data, Impositions impositions, CancellationToken cancellationToken)
      {
        await Task.Delay(impositions.WaitTimeOverride.GetValueOrDefault(Duration), cancellationToken);

        return IsEnd
          ? Transitions.Succeed(data)
          : Transitions.Next(Next, data);
      }
    }

    /// <summary>A <see cref="Step"/> that makes a decision based on it's input.</summary>
    public sealed record ChoiceStep : Step
    {
      public string    Default   { get; set; } = string.Empty;
      public Condition Condition { get; set; } = Conditions.False;

      protected override Task<Transition> ExecuteInnerAsync(StepFunctionData data, Impositions impositions, CancellationToken cancellationToken)
      {
        throw new NotImplementedException();
      }
    }

    /// <summary>A <see cref="Step"/> that completes the execution with a success.</summary>
    public sealed record SucceedStep : Step
    {
      protected override Task<Transition> ExecuteInnerAsync(StepFunctionData data, Impositions impositions, CancellationToken cancellationToken)
      {
        return Task.FromResult(Transitions.Succeed(data));
      }
    }

    /// <summary>A <see cref="Step"/> that completes the execution with a failure.</summary>
    public sealed record FailStep : Step
    {
      public string Error { get; set; } = string.Empty;
      public string Cause { get; set; } = string.Empty;

      protected override Task<Transition> ExecuteInnerAsync(StepFunctionData data, Impositions impositions, CancellationToken cancellationToken)
      {
        return Task.FromResult(Transitions.Fail());
      }
    }

    /// <summary>A <see cref="Step"/> that executes multiple other <see cref="Step"/>s in parallel.</summary>
    public sealed record ParallelStep : Step
    {
      private readonly StepHandlerFactory factory;

      public ParallelStep(StepHandlerFactory factory)
      {
        this.factory = factory;
      }

      public StepFunctionDefinition[] Branches { get; set; } = Array.Empty<StepFunctionDefinition>();

      public string Next  { get; set; } = string.Empty;
      public bool   IsEnd { get; set; } = false;

      protected override async Task<Transition> ExecuteInnerAsync(StepFunctionData data, Impositions impositions, CancellationToken cancellationToken)
      {
        // TODO: history isn't recorded properly when there are multiple parallel steps

        var hosts   = Branches.Select(branch => new StepFunctionHost(branch, factory)).ToArray();
        var results = await Task.WhenAll(hosts.Select(result => result.ExecuteAsync(impositions, data, cancellationToken)));

        if (results.Any(result => result.IsFailure))
        {
          var exception = new AggregateException(
            from result in results
            where result.IsFailure
            select result.Exception
          );

          return Transitions.Fail(exception.Flatten());
        }

        return IsEnd
          ? Transitions.Succeed(data)
          : Transitions.Next(Next, data);
      }
    }

    /// <summary>This is a sealed hierarchy.</summary>
    private Step()
    {
    }
  }
}