using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using Amazon.StepFunction.Hosting.Utilities;
using Newtonsoft.Json.Linq;

namespace Amazon.StepFunction.Hosting.Evaluation
{
  /// <summary>Defines a possible step in a <see cref="StepFunction"/>.</summary>
  internal abstract record Step
  {
    public string Name { get; init; } = string.Empty;

    public Task<StepResult> ExecuteAsync(StepFunctionData input = default, IStepFunctionExecution? execution = default, CancellationToken cancellationToken = default)
    {
      return ExecuteAsync(Impositions.CreateDefault(), input, execution, cancellationToken);
    }

    public async Task<StepResult> ExecuteAsync(Impositions impositions, StepFunctionData input = default, IStepFunctionExecution? execution = default, CancellationToken cancellationToken = default)
    {
      var context = new StepContext(input, execution)
      {
        Impositions       = impositions,
        CancellationToken = cancellationToken
      };

      try
      {
        var result = await ExecuteStepAsync(context);

        return new StepResult(result)
        {
          ChildHistory = context.ChildHistories
        };
      }
      catch (Exception exception)
      {
        return new StepResult(new Transition.Fail(null, exception))
        {
          ChildHistory = context.ChildHistories
        };
      }
    }

    protected abstract Task<Transition> ExecuteStepAsync(StepContext context);

    /// <summary>The result for a particular <see cref="Step"/> execution.</summary>
    public record StepResult(Transition Transition)
    {
      public IEnumerable<IEnumerable<ExecutionHistory>> ChildHistory { get; init; } = Enumerable.Empty<IEnumerable<ExecutionHistory>>();
    }

    /// <summary>The context for a particular step execution.</summary>
    protected record StepContext(StepFunctionData Input, IStepFunctionExecution? Execution)
    {
      private int tokenSequence = 0;

      public Guid                                         ExecutionId       { get; init; } = Guid.NewGuid();
      public Impositions                                  Impositions       { get; init; } = Impositions.CreateDefault();
      public CancellationToken                            CancellationToken { get; init; } = CancellationToken.None;
      public ConcurrentBag<IEnumerable<ExecutionHistory>> ChildHistories    { get; }       = new();

      public string CreateTaskToken()
      {
        return $"{ExecutionId}_{Interlocked.Increment(ref tokenSequence)}";
      }

      public StepFunctionData CreateParameterData(object? parameters = default)
      {
        var result = new JObject
        {
          ["ExecutionId"] = ExecutionId
        };

        if (parameters != null)
        {
          foreach (var (key, value) in JObject.FromObject(parameters))
          {
            if (value != null)
            {
              result.Add(key, value);
            }
          }
        }

        return new StepFunctionData(result);
      }

      public void AddChildHistory(IEnumerable<ExecutionHistory> history)
      {
        ChildHistories.Add(history);
      }

      public void AddChildHistories(IEnumerable<IEnumerable<ExecutionHistory>> histories)
      {
        foreach (var history in histories)
        {
          ChildHistories.Add(history);
        }
      }

      public void ClearChildHistory()
      {
        ChildHistories.Clear();
      }
    }

    /// <summary>Pass to the next step in the chain, potentially transforming input to output</summary>
    public sealed record PassStep : Step
    {
      public string   Next       { get; init; } = string.Empty;
      public bool     IsEnd      { get; init; } = false;
      public string   InputPath  { get; set; }  = string.Empty;
      public string   ResultPath { get; set; }  = string.Empty;
      public string   Result     { get; set; }  = string.Empty;
      public Selector Parameters { get; set; }  = default;

      protected override Task<Transition> ExecuteStepAsync(StepContext context)
      {
        var input = Parameters.Expand(context.Input.Query(InputPath), context.CreateParameterData());

        Transition transition = IsEnd
          ? new Transition.Succeed(context.Input.Query(InputPath))
          : new Transition.Next(Next, input);

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

      public string   Next           { get; init; } = string.Empty;
      public bool     IsEnd          { get; init; } = false;
      public string   InputPath      { get; init; } = string.Empty;
      public string   OutputPath     { get; init; } = string.Empty;
      public string   ResultPath     { get; init; } = string.Empty;
      public Selector Parameters     { get; set; }  = default;
      public Selector ResultSelector { get; set; }  = default;

      public RetryPolicy RetryPolicy { get; init; } = RetryPolicy.Null;
      public CatchPolicy CatchPolicy { get; init; } = CatchPolicy.Null;

      public TimeSpanProvider TimeoutProvider { get; init; } = TimeSpanProviders.FromSeconds(300);

      protected override async Task<Transition> ExecuteStepAsync(StepContext context)
      {
        var impositions       = context.Impositions;
        var cancellationToken = context.CancellationToken;

        var taskToken = resource.EndsWith(".waitForTaskToken", StringComparison.OrdinalIgnoreCase) ? context.CreateTaskToken() : null;
        var parameters = context.CreateParameterData(new
        {
          Task = new { Token = taskToken }
        });

        var input = Parameters.Expand(context.Input.Query(InputPath), parameters);

        var result = await CatchPolicy.EvaluateAsync(impositions.EnableCatchPolicies, async () =>
        {
          return await RetryPolicy.EvaluateAsync(impositions.EnableRetryPolicies, async () =>
          {
            var timeout = impositions.TaskTimeoutOverride.GetValueOrDefault(TimeoutProvider(context.Input));

            using var timeoutToken = new CancellationTokenSource(timeout);
            using var linkedTokens = CancellationTokenSource.CreateLinkedTokenSource(timeoutToken.Token, cancellationToken);

            var handler = factory(Name, resource);
            var output  = await handler(input, linkedTokens.Token);

            return ResultSelector.Expand(output.Query(OutputPath), parameters, ResultPath);
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

      protected override async Task<Transition> ExecuteStepAsync(StepContext context)
      {
        var impositions       = context.Impositions;
        var cancellationToken = context.CancellationToken;

        var timeout = impositions.WaitTimeOverride.GetValueOrDefault(WaitTimeProvider(context.Input));
        if (timeout >= TimeSpan.Zero)
        {
          // the wake up time might have already passed by the time we arrived here
          await Task.Delay(timeout, cancellationToken);
        }

        return IsEnd
          ? new Transition.Succeed(context.Input)
          : new Transition.Next(Next, context.Input);
      }
    }

    /// <summary>Decide between different logical branches based on input data</summary>
    public sealed record ChoiceStep : Step
    {
      public string                Default { get; init; } = string.Empty;
      public ImmutableList<Choice> Choices { get; init; } = ImmutableList<Choice>.Empty;

      protected override Task<Transition> ExecuteStepAsync(StepContext context)
      {
        foreach (var choice in Choices)
        {
          if (choice.Evaluate(context.Input))
          {
            return Task.FromResult<Transition>(new Transition.Next(choice.Next, context.Input));
          }
        }

        return Task.FromResult<Transition>(new Transition.Next(Default, context.Input));
      }
    }

    /// <summary>Complete the Step Function successfully</summary>
    public sealed record SucceedStep : Step
    {
      protected override Task<Transition> ExecuteStepAsync(StepContext context)
      {
        return Task.FromResult<Transition>(new Transition.Succeed(context.Input));
      }
    }

    /// <summary>Fail the Step Function with an optional error message</summary>
    public sealed record FailStep : Step
    {
      public string Cause { get; init; } = string.Empty;

      protected override Task<Transition> ExecuteStepAsync(StepContext context)
      {
        return Task.FromResult<Transition>(new Transition.Fail(Cause, null));
      }
    }

    /// <summary>Execute different branches in parallel, with top-level error and retry handling</summary>
    public sealed record ParallelStep : Step
    {
      public string      Next           { get; init; } = string.Empty;
      public string      InputPath      { get; set; }  = string.Empty;
      public string      OutputPath     { get; set; }  = string.Empty;
      public string      ResultPath     { get; set; }  = string.Empty;
      public Selector    ResultSelector { get; set; }  = default;
      public bool        IsEnd          { get; init; } = false;
      public RetryPolicy RetryPolicy    { get; init; } = RetryPolicy.Null;
      public CatchPolicy CatchPolicy    { get; init; } = CatchPolicy.Null;

      public ImmutableList<StepFunctionHost> Branches { get; init; } = ImmutableList<StepFunctionHost>.Empty;

      protected override async Task<Transition> ExecuteStepAsync(StepContext context)
      {
        var impositions       = context.Impositions;
        var cancellationToken = context.CancellationToken;

        var input = context.Input.Query(InputPath);

        // execute the inner steps, collecting results
        var result = await CatchPolicy.EvaluateAsync(impositions.EnableCatchPolicies, async () =>
        {
          return await RetryPolicy.EvaluateAsync(impositions.EnableRetryPolicies, async () =>
          {
            context.ClearChildHistory();

            var results = await Task.WhenAll(Branches.Select(host => host.ExecuteAsync(input, context.Execution, cancellationToken)));

            // capture sub-histories for use in debugging
            context.AddChildHistories(results.Select(_ => _.Execution.History));

            var failures = results.Where(result => result.IsFailure).ToArray();
            if (failures.Length == 1)
            {
              // propagate the first inner exception
              if (failures[0].Exception != null)
              {
                ExceptionDispatchInfo.Capture(failures[0].Exception!).Throw();
              }
            }
            else if (failures.Length > 1)
            {
              // wrap all inner exceptions
              var exception = new AggregateException(
                from result in results
                where result.Execution != null
                where result.IsFailure
                select result.Exception
              );

              throw exception.Flatten();
            }

            var output = new StepFunctionData(results.Select(_ => _.Output).ToArray()).Query(OutputPath);

            return ResultSelector.Expand(output, context.CreateParameterData(), ResultPath);
          });
        });

        return result.ToTransition(input, IsEnd, Next);
      }
    }

    /// <summary>Map over some input source and execute sub-branches against it</summary>
    public sealed record MapStep(StepFunctionHost Iterator) : Step
    {
      public string           Next     { get; init; } = string.Empty;
      public bool             IsEnd    { get; init; } = false;

      public  int MaxConcurrency         { get; init; } = 0;
      private int MaxDegreeOfParallelism => MaxConcurrency == 0 ? Environment.ProcessorCount : MaxConcurrency;

      public string   InputPath      { get; set; }  = string.Empty;
      public string   OutputPath     { get; set; }  = string.Empty;
      public string   ItemsPath      { get; init; } = string.Empty;
      public Selector Parameters     { get; init; } = default;
      public string   ResultPath     { get; init; } = string.Empty;
      public Selector ResultSelector { get; init; } = default;

      public RetryPolicy RetryPolicy { get; init; } = RetryPolicy.Null;
      public CatchPolicy CatchPolicy { get; init; } = CatchPolicy.Null;

      protected override async Task<Transition> ExecuteStepAsync(StepContext context)
      {
        var impositions       = context.Impositions;
        var cancellationToken = context.CancellationToken;

        // fetch the items array from the input
        var items = context.Input.Query(InputPath).Query(ItemsPath).Cast<JArray>();
        if (items == null)
        {
          throw new Exception("Unable to locate a valid array to map over!");
        }

        // cast into a more usable form
        var entries = items.Children().Select(item => new StepFunctionData(item));

        // execute the inner steps, collecting results
        var result = await CatchPolicy.EvaluateAsync(impositions.EnableCatchPolicies, async () =>
        {
          return await RetryPolicy.EvaluateAsync(impositions.EnableRetryPolicies, async () =>
          {
            context.ClearChildHistory();

            // N.B: preferred instead of .AsParallel(), which would unfortunately block the calling thread
            //      until all items have completed, even though most of the work inside completely asynchronous
            var results = new ConcurrentBag<StepFunctionHost.ExecutionResult>();

            // TODO: replace this with Parallel.ForEachAsync in .NET 6+
            await entries.ForEachAsync(
              partitionCount: MaxDegreeOfParallelism,
              cancellationToken: cancellationToken,
              body: async entry =>
              {
                var parameters = context.CreateParameterData(new
                {
                  Map = new { Item = new { Value = entry.Cast<JToken>() } }
                });

                var input  = Parameters.Expand(entry, parameters);
                var result = await Iterator.ExecuteAsync(input, context.Execution, cancellationToken);

                results.Add(result);
                context.AddChildHistory(result.Execution.History);
              }
            );

            var failures = results.Where(result => result.IsFailure).ToArray();
            if (failures.Length == 1)
            {
              // propagate the first inner exception
              if (failures[0].Exception != null)
              {
                ExceptionDispatchInfo.Capture(failures[0].Exception!).Throw();
              }
            }
            else if (failures.Length > 1)
            {
              // wrap all inner exceptions
              var exception = new AggregateException(
                from result in results
                where result.Execution != null
                where result.IsFailure
                select result.Exception
              );

              throw exception.Flatten();
            }

            var output = new StepFunctionData(results.Select(_ => _.Output)).Query(OutputPath);

            return ResultSelector.Expand(output, context.CreateParameterData(), ResultPath);
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