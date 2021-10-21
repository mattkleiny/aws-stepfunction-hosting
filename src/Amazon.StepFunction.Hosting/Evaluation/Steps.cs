using System;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

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
      private int tokenSequence = 0;

      public Guid              ExecutionId       { get; init; } = Guid.NewGuid();
      public Impositions       Impositions       { get; init; } = Impositions.Default;
      public CancellationToken CancellationToken { get; init; } = CancellationToken.None;

      public string GenerateTaskToken()
      {
        return $"{ExecutionId}_{Interlocked.Increment(ref tokenSequence)}";
      }
    }

    /// <summary>Pass to the next step in the chain, potentially transforming input to output</summary>
    public sealed record PassStep : Step
    {
      public string Next       { get; init; } = string.Empty;
      public bool   IsEnd      { get; init; } = false;
      public string InputPath  { get; set; }  = string.Empty;
      public string ResultPath { get; set; }  = string.Empty;
      public string Result     { get; set; }  = string.Empty;
      public string Parameters { get; set; }  = string.Empty;

      protected override Task<Transition> ExecuteInnerAsync(ExecutionContext context)
      {
        // TODO: input/output transformers

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

      public string Next       { get; init; } = string.Empty;
      public bool   IsEnd      { get; init; } = false;
      public string InputPath  { get; init; } = string.Empty;
      public string ResultPath { get; init; } = string.Empty;

      public RetryPolicy RetryPolicy { get; init; } = RetryPolicy.Null;
      public CatchPolicy CatchPolicy { get; init; } = CatchPolicy.Null;

      public TimeSpanProvider TimeoutProvider { get; init; } = TimeSpanProviders.FromSeconds(300);

      protected override async Task<Transition> ExecuteInnerAsync(ExecutionContext context)
      {
        // TODO: input/output transformers

        var impositions       = context.Impositions;
        var cancellationToken = context.CancellationToken;

        var taskToken = resource.EndsWith(".waitForTaskToken", StringComparison.OrdinalIgnoreCase) ? context.GenerateTaskToken() : null;

        var result = await CatchPolicy.EvaluateAsync(impositions.EnableCatchPolicies, async () =>
        {
          return await RetryPolicy.EvaluateAsync(impositions.EnableRetryPolicies, async () =>
          {
            var timeout = impositions.TaskTimeoutOverride.GetValueOrDefault(TimeoutProvider(context.Input));

            using var timeoutToken = new CancellationTokenSource(timeout);
            using var linkedTokens = CancellationTokenSource.CreateLinkedTokenSource(timeoutToken.Token, cancellationToken);

            var handler = factory(resource);

            // TODO: allow input transformer to receive task token

            var input  = context.Input.Query(InputPath);
            var output = await handler(input, linkedTokens.Token);

            return output.Query(ResultPath);
          });
        });

        return result.ToTransition(context.Input, IsEnd, Next, taskToken);
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
      public string                Default { get; init; } = string.Empty;
      public ImmutableList<Choice> Choices { get; init; } = ImmutableList<Choice>.Empty;

      protected override Task<Transition> ExecuteInnerAsync(ExecutionContext context)
      {
        foreach (var choice in Choices)
        {
          if (choice.Evaluate(context.Input))
          {
            return Task.FromResult(Transitions.Next(choice.Next, context.Input));
          }
        }

        return Task.FromResult(Transitions.Next(Default, context.Input));
      }
    }

    /// <summary>Complete the Step Function successfully</summary>
    public sealed record SucceedStep : Step
    {
      protected override Task<Transition> ExecuteInnerAsync(ExecutionContext context)
      {
        return Task.FromResult(Transitions.Succeed(context.Input));
      }
    }

    /// <summary>Fail the Step Function with an optional error message</summary>
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
      public string                          Next        { get; init; } = string.Empty;
      public bool                            IsEnd       { get; init; } = false;
      public RetryPolicy                     RetryPolicy { get; init; } = RetryPolicy.Null;
      public CatchPolicy                     CatchPolicy { get; init; } = CatchPolicy.Null;
      public ImmutableList<StepFunctionHost> Branches    { get; init; } = ImmutableList<StepFunctionHost>.Empty;

      protected override async Task<Transition> ExecuteInnerAsync(ExecutionContext context)
      {
        // TODO: input/output transformers
        // TODO: history isn't serialized properly when there are multiple parallel steps

        var results = await Task.WhenAll(Branches.Select(host => host.ExecuteAsync(context.Input, context.CancellationToken)));

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
      public MapStep(StepFunctionHost iterator)
      {
        Iterator = iterator;
      }

      public StepFunctionHost Iterator { get; }
      public string           Next     { get; init; } = string.Empty;
      public bool             IsEnd    { get; init; } = false;

      public  int MaxConcurrency         { get; init; } = 0;
      private int MaxDegreeOfParallelism => MaxConcurrency == 0 ? Environment.ProcessorCount : MaxConcurrency;

      public string InputPath      { get; set; }  = string.Empty;
      public string OutputPath     { get; set; }  = string.Empty;
      public string ItemsPath      { get; init; } = string.Empty;
      public string ResultPath     { get; init; } = string.Empty;
      public string ResultSelector { get; init; } = string.Empty;

      public RetryPolicy RetryPolicy { get; init; } = RetryPolicy.Null;
      public CatchPolicy CatchPolicy { get; init; } = CatchPolicy.Null;

      protected override async Task<Transition> ExecuteInnerAsync(ExecutionContext context)
      {
        // TODO: input/output transformers
        // TODO: history isn't serialized properly when there are multiple parallel steps

        var impositions       = context.Impositions;
        var cancellationToken = context.CancellationToken;

        var input = context.Input.Query(InputPath);
        var items = input.Query(ItemsPath).Cast<JArray>();

        if (items == null)
        {
          throw new Exception("Unable to locate a valid array to map over");
        }

        var options = new ParallelOptions
        {
          CancellationToken      = cancellationToken,
          MaxDegreeOfParallelism = MaxDegreeOfParallelism
        };

        var result = await CatchPolicy.EvaluateAsync(impositions.EnableCatchPolicies, async () =>
        {
          return await RetryPolicy.EvaluateAsync(impositions.EnableRetryPolicies, async () =>
          {
            // N.B: preferred instead of .AsParallel(), which would unfortunately block the calling thread
            //      until all items have completed, even though most of the work is completely asynchronous
            var results = new ConcurrentBag<StepFunctionHost.ExecutionResult>();

            await Parallel.ForEachAsync(items.Children(), options, async (item, innerToken) =>
            {
              results.Add(await Iterator.ExecuteAsync(item, innerToken));
            });

            if (results.Any(_ => _.IsFailure))
            {
              var exception = new AggregateException(
                from result in results
                where result.IsFailure
                select result.Exception
              );

              throw exception.Flatten();
            }

            // TODO: process the results and put in the right place on the result path

            throw new NotImplementedException();
          });
        });

        return result.ToTransition(context.Input, IsEnd, Next);
      }
    }

    /// <summary>This is a sealed hierarchy.</summary>
    private Step()
    {
    }
  }
}