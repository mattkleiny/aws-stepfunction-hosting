using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Amazon.StepFunction.Hosting.Definition;

namespace Amazon.StepFunction.Hosting.Evaluation
{
  /// <summary>Defines a possible step in a <see cref="StepFunction"/>.</summary>
  internal abstract record Step
  {
    public string Name { get; init; } = string.Empty;

    public Task<Transition> ExecuteAsync(StepFunctionData input = default, CancellationToken cancellationToken = default)
    {
      return ExecuteAsync(Impositions.Default, input, cancellationToken);
    }

    public async Task<Transition> ExecuteAsync(Impositions impositions, StepFunctionData input = default, CancellationToken cancellationToken = default)
    {
      try
      {
        var context = new ExecutionContext(input)
        {
          Impositions       = impositions,
          CancellationToken = cancellationToken
        };

        return await ExecuteInnerAsync(context);
      }
      catch (Exception exception)
      {
        return Transitions.Fail(exception);
      }
    }

    protected abstract Task<Transition> ExecuteInnerAsync(ExecutionContext context);

    protected record ExecutionContext(StepFunctionData Input)
    {
      public Guid              ExecutionId       { get; init; } = Guid.NewGuid();
      public Impositions       Impositions       { get; init; } = Impositions.Default;
      public CancellationToken CancellationToken { get; init; } = CancellationToken.None;
    }

    public sealed record PassStep : Step
    {
      public string Next  { get; init; } = string.Empty;
      public bool   IsEnd { get; init; } = false;

      protected override Task<Transition> ExecuteInnerAsync(ExecutionContext context)
      {
        var transition = IsEnd
          ? Transitions.Succeed(context.Input)
          : Transitions.Next(Next, context.Input);

        return Task.FromResult(transition);
      }
    }

    public sealed record TaskStep : Step
    {
      private readonly string             resource;
      private readonly StepHandlerFactory factory;

      public TaskStep(string resource, StepHandlerFactory factory)
      {
        this.resource = resource;
        this.factory  = factory;
      }

      public TimeSpan    Timeout     { get; init; } = default;
      public string      Next        { get; init; } = string.Empty;
      public bool        IsEnd       { get; init; } = false;
      public string      InputPath   { get; init; } = string.Empty;
      public string      ResultPath  { get; init; } = string.Empty;
      public RetryPolicy RetryPolicy { get; init; } = RetryPolicy.Null;
      public CatchPolicy CatchPolicy { get; init; } = CatchPolicy.Null;

      protected override async Task<Transition> ExecuteInnerAsync(ExecutionContext context)
      {
        var impositions       = context.Impositions;
        var cancellationToken = context.CancellationToken;

        var (result, nextState) = await CatchPolicy.EvaluateAsync(impositions.EnableCatchPolicies, async () =>
        {
          return await RetryPolicy.EvaluateAsync(impositions.EnableRetryPolicies, async () =>
          {
            using var timeoutToken = new CancellationTokenSource(impositions.TimeoutOverride.GetValueOrDefault(Timeout));
            using var linkedTokens = CancellationTokenSource.CreateLinkedTokenSource(timeoutToken.Token, cancellationToken);

            var handler = factory(resource);

            var input  = context.Input.Query(InputPath);
            var output = await handler(input, linkedTokens.Token);

            return output.Query(ResultPath);
          });
        });

        if (nextState != null)
        {
          return Transitions.Next(nextState, result);
        }

        return IsEnd
          ? Transitions.Succeed(result)
          : Transitions.Next(Next, result);
      }
    }

    public sealed record WaitStep : Step
    {
      public TimeSpan Duration { get; init; } = TimeSpan.FromSeconds(300);
      public string   Next     { get; init; } = string.Empty;
      public bool     IsEnd    { get; init; } = false;

      protected override async Task<Transition> ExecuteInnerAsync(ExecutionContext context)
      {
        var impositions       = context.Impositions;
        var cancellationToken = context.CancellationToken;

        await Task.Delay(impositions.WaitTimeOverride.GetValueOrDefault(Duration), cancellationToken);

        return IsEnd
          ? Transitions.Succeed(context.Input)
          : Transitions.Next(Next, context.Input);
      }
    }

    public sealed record ChoiceStep : Step
    {
      public string      Default    { get; init; } = string.Empty;
      public Condition[] Conditions { get; init; } = Array.Empty<Condition>();

      protected override Task<Transition> ExecuteInnerAsync(ExecutionContext context)
      {
        foreach (var condition in Conditions)
        {
          if (condition.Evaluate(context.Input))
          {
            return Task.FromResult(Transitions.Next(condition.Next, context.Input));
          }
        }

        return Task.FromResult(Transitions.Next(Default, context.Input));
      }
    }

    public sealed record SucceedStep : Step
    {
      protected override Task<Transition> ExecuteInnerAsync(ExecutionContext context)
      {
        return Task.FromResult(Transitions.Succeed(context.Input));
      }
    }

    public sealed record FailStep : Step
    {
      public string Cause { get; init; } = string.Empty;

      protected override Task<Transition> ExecuteInnerAsync(ExecutionContext context)
      {
        return Task.FromResult(Transitions.Fail(Cause));
      }
    }

    public sealed record ParallelStep : Step
    {
      private readonly StepHandlerFactory factory;

      public ParallelStep(StepHandlerFactory factory)
      {
        this.factory = factory;
      }

      public string Next  { get; init; } = string.Empty;
      public bool   IsEnd { get; init; } = false;

      public ImmutableList<StepFunctionDefinition> Branches { get; init; } = ImmutableList<StepFunctionDefinition>.Empty;

      protected override async Task<Transition> ExecuteInnerAsync(ExecutionContext context)
      {
        // TODO: history isn't recorded properly when there are multiple parallel steps

        var hosts   = Branches.Select(branch => new StepFunctionHost(branch, factory)).ToArray();
        var results = await Task.WhenAll(hosts.Select(result => result.ExecuteAsync(context.Impositions, context.Input, context.CancellationToken)));

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
          ? Transitions.Succeed(context.Input)
          : Transitions.Next(Next, context.Input);
      }
    }

    public sealed record MapStep : Step
    {
      private readonly StepHandlerFactory factory;

      public MapStep(StepHandlerFactory factory)
      {
        this.factory = factory;
      }

      protected override Task<Transition> ExecuteInnerAsync(ExecutionContext context)
      {
        throw new NotImplementedException();
      }
    }

    /// <summary>This is a sealed hierarchy.</summary>
    private Step()
    {
    }
  }
}