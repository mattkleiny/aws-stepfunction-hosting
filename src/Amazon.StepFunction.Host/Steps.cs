using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Amazon.StepFunction.Parsing;

namespace Amazon.StepFunction
{
  /// <summary>Defines a possible step in a <see cref="StepFunction"/>.</summary>
  internal abstract class Step
  {
    /// <summary>The name of this step.</summary>
    public string Name { get; set; }

    /// <summary>Executes the step asynchronously, observing any required step transition behaviour.</summary>
    public Task<IEnumerable<Transition>> ExecuteAsync(Impositions impositions, object input = null, CancellationToken cancellationToken = default)
    {
      // TODO: refactor this once async iterators become a thing in the next release of C#
      IEnumerable<Transition> Thunk()
      {
        try
        {
          return Execute(impositions, input, cancellationToken);
        }
        catch (Exception exception)
        {
          return new[]
          {
            Transitions.Fail(exception)
          };
        }
      }

      return Task.FromResult(Thunk());
    }

    /// <summary>Implements the actual execution operation for this step type.</summary>
    protected abstract IEnumerable<Transition> Execute(Impositions impositions, object input, CancellationToken cancellationToken);

    /// <summary>A <see cref="Step"/> that passes it's input to output.</summary>
    public sealed class Pass : Step
    {
      public bool   IsEnd { get; set; }
      public string Next  { get; set; }

      protected override IEnumerable<Transition> Execute(Impositions impositions, object input, CancellationToken cancellationToken)
      {
        if (!IsEnd)
        {
          yield return Transitions.Next(Next, input);
        }
        else
        {
          yield return Transitions.Succeed(input);
        }
      }
    }

    /// <summary>A <see cref="Step"/> that invoke a handler for some given resource.</summary>
    public sealed class Invoke : Step
    {
      private readonly Func<StepHandler> factory;

      public Invoke(Func<StepHandler> factory)
      {
        this.factory = factory;
      }

      public bool        IsEnd       { get; set; }
      public TimeSpan    Timeout     { get; set; }
      public string      Next        { get; set; }
      public RetryPolicy RetryPolicy { get; set; } = RetryPolicies.Null;

      protected override IEnumerable<Transition> Execute(Impositions impositions, object input, CancellationToken cancellationToken)
      {
        var task = RetryPolicy(async () =>
        {
          using (var timeoutToken = new CancellationTokenSource(Timeout))
          using (var linkedTokens = CancellationTokenSource.CreateLinkedTokenSource(timeoutToken.Token, cancellationToken))
          {
            var handler = factory();

            return await handler(input, linkedTokens.Token);
          }
        });

        var exception = ObserveAndCaptureException(task, cancellationToken);

        if (exception != null)
        {
          yield return Transitions.Fail(task.Exception);
        }
        else
        {
          var output = task.Result;

          if (!IsEnd)
          {
            yield return Transitions.Next(Next, output);
          }
          else
          {
            yield return Transitions.Succeed(output);
          }
        }
      }

      /// <summary>Observes the given <see cref="Task"/>, and returns any exception that it propagates.</summary>
      private static Exception ObserveAndCaptureException(Task task, CancellationToken cancellationToken)
      {
        try
        {
          task.Wait(cancellationToken);

          return null;
        }
        catch (Exception exception)
        {
          return exception;
        }
      }
    }

    /// <summary>A <see cref="Step"/> that merely waits some given interval of time.</summary>
    public sealed class Wait : Step
    {
      public TimeSpan Duration { get; set; }
      public string   Next     { get; set; }
      public bool     IsEnd    { get; set; }

      protected override IEnumerable<Transition> Execute(Impositions impositions, object input, CancellationToken cancellationToken)
      {
        yield return Transitions.Wait(Duration);

        if (!IsEnd)
        {
          yield return Transitions.Next(Next, input);
        }
        else
        {
          yield return Transitions.Succeed(input);
        }
      }
    }

    /// <summary>A <see cref="Step"/> that makes a decision based on it's input.</summary>
    public sealed class Choice : Step
    {
      public string Default { get; set; }

      public StepDefinition.Choice.Evaluator Evaluator { get; set; }

      protected override IEnumerable<Transition> Execute(Impositions impositions, object input, CancellationToken cancellationToken)
      {
        yield return Transitions.Next(Evaluator(input) ?? Default, input);
      }
    }

    /// <summary>A <see cref="Step"/> that completes the execution with a success.</summary>
    public sealed class Succeed : Step
    {
      protected override IEnumerable<Transition> Execute(Impositions impositions, object input, CancellationToken cancellationToken)
      {
        yield return Transitions.Succeed(input);
      }
    }

    /// <summary>A <see cref="Step"/> that completes the execution with a failure.</summary>
    public sealed class Fail : Step
    {
      public string Error { get; set; }
      public string Cause { get; set; }

      protected override IEnumerable<Transition> Execute(Impositions impositions, object input, CancellationToken cancellationToken)
      {
        yield return Transitions.Fail();
      }
    }

    /// <summary>A <see cref="Step"/> that executes multiple other <see cref="Step"/>s in parallel.</summary>
    public sealed class Parallel : Step
    {
      public StepFunctionDefinition[] Branches { get; set; }
      public StepHandlerFactory       Factory  { get; set; }
      public string                   Next     { get; set; }
      public bool                     IsEnd    { get; set; }

      protected override IEnumerable<Transition> Execute(Impositions impositions, object input, CancellationToken cancellationToken)
      {
        // TODO: history isn't recorded properly when there are multiple parallel steps
        
        var hosts   = Branches.Select(branch => new StepFunctionHost(branch, Factory)).ToArray();
        var results = Task.WhenAll(hosts.Select(result => result.ExecuteAsync(impositions, input, cancellationToken))).Result;

        if (results.Any(result => result.IsFailure))
        {
          var exception = new AggregateException(
            from result in results
            where result.IsFailure
            select result.Exception
          );

          yield return Transitions.Fail(exception.Flatten());
        }
        else
        {
          if (!IsEnd)
          {
            yield return Transitions.Next(Next, input);
          }
          else
          {
            yield return Transitions.Succeed(input);
          }
        }
      }
    }

    /// <summary>This is a sealed ADT.</summary>
    private Step()
    {
    }
  }
}