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

    /// <summary>The context for a particular step execution.</summary>
    protected record ExecutionContext(StepFunctionData Input)
    {
      public Impositions       Impositions       { get; init; } = Impositions.Default;
      public CancellationToken CancellationToken { get; init; } = CancellationToken.None;

      public string GenerateTaskToken()
      {
        return Guid.NewGuid().ToString();
      }
    }

    /// <summary>Pass to the next step in the chain, potentially transforming input to output</summary>
    public sealed record PassStep : Step
    {
      public string Next       { get; init; } = string.Empty;
      public bool   IsEnd      { get; init; } = false;
      public string InputPath  { get; set; }  = string.Empty;
      public string ResultPath { get; set; }  = string.Empty;

      protected override Task<Transition> ExecuteInnerAsync(ExecutionContext context)
      {
        // TODO: input/output transformerss

        var transition = IsEnd
          ? Transitions.Succeed(context.Input.Query(InputPath))
          : Transitions.Next(Next, context.Input.Query(InputPath));

        return Task.FromResult(transition);
      }
    }

    /// <summary>Executes a single logical task, with retry and error handling</summary>
    public sealed record TaskStep : Step
    {
      private readonly string             resource;
      private readonly StepHandlerFactory factory;

      public TaskStep(string resource, StepHandlerFactory factory)
      {
        this.resource = resource;
        this.factory  = factory;
      }

      public TimeSpanProvider TimeoutProvider { get; init; } = TimeSpanProviders.FromSeconds(300);
      public string           Next            { get; init; } = string.Empty;
      public bool             IsEnd           { get; init; } = false;
      public string           InputPath       { get; init; } = string.Empty;
      public string           ResultPath      { get; init; } = string.Empty;
      public RetryPolicy      RetryPolicy     { get; init; } = RetryPolicy.Null;
      public CatchPolicy      CatchPolicy     { get; init; } = CatchPolicy.Null;

      protected override async Task<Transition> ExecuteInnerAsync(ExecutionContext context)
      {
        // TODO: step function context
        // TODO: input/output transformers

        var impositions       = context.Impositions;
        var cancellationToken = context.CancellationToken;

        // check for a task token request, and generate one if necessary
        var hasTaskToken = resource.EndsWith(".waitForTaskToken", StringComparison.OrdinalIgnoreCase);
        var taskToken    = hasTaskToken ? context.GenerateTaskToken() : null; // TODO: allow this to be passed to step input

        var (result, nextState) = await CatchPolicy.EvaluateAsync(impositions.EnableCatchPolicies, async () =>
        {
          return await RetryPolicy.EvaluateAsync(impositions.EnableRetryPolicies, async () =>
          {
            var timeout = impositions.TimeoutOverride.GetValueOrDefault(TimeoutProvider(context.Input));

            using var timeoutToken = new CancellationTokenSource(timeout);
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
          : Transitions.Next(Next, result, taskToken);
      }
    }

    /// <summary>Wait an amount of time, either a constant amount or from a path on the input</summary>
    public sealed record WaitStep : Step
    {
      public TimeSpanProvider WaitTimeProvider { get; init; } = TimeSpanProviders.FromSeconds(300);
      public string           Next             { get; init; } = string.Empty;
      public bool             IsEnd            { get; init; } = false;

      protected override async Task<Transition> ExecuteInnerAsync(ExecutionContext context)
      {
        var impositions       = context.Impositions;
        var cancellationToken = context.CancellationToken;

        var timeout = impositions.WaitTimeOverride.GetValueOrDefault(WaitTimeProvider(context.Input));

        await Task.Delay(timeout, cancellationToken);

        return IsEnd
          ? Transitions.Succeed(context.Input)
          : Transitions.Next(Next, context.Input);
      }
    }

    /// <summary>Decide between different logical branches based on input data</summary>
    public sealed record ChoiceStep : Step
    {
      public string                   Default    { get; init; } = string.Empty;
      public ImmutableList<Condition> Conditions { get; init; } = ImmutableList<Condition>.Empty;

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

    /// <summary>Complete successfully</summary>
    public sealed record SucceedStep : Step
    {
      protected override Task<Transition> ExecuteInnerAsync(ExecutionContext context)
      {
        return Task.FromResult(Transitions.Succeed(context.Input));
      }
    }

    /// <summary>Fail with an optional error message</summary>
    public sealed record FailStep : Step
    {
      public string Cause { get; init; } = string.Empty;

      protected override Task<Transition> ExecuteInnerAsync(ExecutionContext context)
      {
        return Task.FromResult(Transitions.Fail(Cause));
      }
    }

    /// <summary>Execute different branches in parallel, with top-level error and retry handling</summary>
    public sealed record ParallelStep : Step
    {
      private readonly StepHandlerFactory factory;

      public ParallelStep(StepHandlerFactory factory)
      {
        this.factory = factory;
      }

      public string Next  { get; init; } = string.Empty;
      public bool   IsEnd { get; init; } = false;

      public RetryPolicy RetryPolicy { get; init; } = RetryPolicy.Null;
      public CatchPolicy CatchPolicy { get; init; } = CatchPolicy.Null;

      public ImmutableList<StepFunctionDefinition> Branches { get; init; } = ImmutableList<StepFunctionDefinition>.Empty;

      protected override async Task<Transition> ExecuteInnerAsync(ExecutionContext context)
      {
        // TODO: input/output transformers
        // TODO: history isn't recorded properly when there are multiple parallel steps

        var hosts   = Branches.Select(branch => new StepFunctionHost(branch, factory)).ToArray();
        var results = await Task.WhenAll(hosts.Select(host => host.ExecuteAsync(context.Impositions, context.Input, context.CancellationToken)));

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

    /// <summary>Map over some input source and execute sub-branches against it</summary>
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