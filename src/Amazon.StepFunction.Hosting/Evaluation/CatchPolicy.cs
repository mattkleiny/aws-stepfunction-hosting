using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Amazon.StepFunction.Hosting.Evaluation
{
  /// <summary>
  /// Permits constructing Catch Policies from pieces. A Catch Policy permits error handling scenarios across a
  /// Step Function execution, and is specific to the AWS state machines language.
  /// <para/>
  /// See here for more details https://docs.aws.amazon.com/step-functions/latest/dg/concepts-error-handling.html
  /// </summary>
  internal abstract record CatchPolicy
  {
    public static CatchPolicy Null { get; } = new NullCatchPolicy();

    public static CatchPolicy Standard(ErrorSet errorSet, string resultPath, string nextState)
    {
      return new StandardPolicy(errorSet, resultPath, nextState);
    }

    public static CatchPolicy Composite(IEnumerable<CatchPolicy> policies)
    {
      return new CompositePolicy(policies.ToArray());
    }

    public async Task<CatchResult<T>> EvaluateAsync<T>(bool isEnabled, Func<Task<T>> body)
    {
      try
      {
        return new CatchResult<T>.Success(await body());
      }
      catch (Exception exception) when (isEnabled && CanHandle(exception))
      {
        return ToResult<T>(exception);
      }
    }

    protected abstract bool           CanHandle(Exception exception);
    protected abstract CatchResult<T> ToResult<T>(Exception exception);

    /// <summary>A no-op <see cref="CatchPolicy"/>.</summary>
    private sealed record NullCatchPolicy : CatchPolicy
    {
      protected override bool CanHandle(Exception exception)
      {
        return false;
      }

      protected override CatchResult<T> ToResult<T>(Exception exception)
      {
        throw new InvalidOperationException("This should never be reached");
      }
    }

    /// <summary>A <see cref="CatchPolicy"/> that evaluates an <see cref="ErrorSet"/>.</summary>
    private sealed record StandardPolicy(ErrorSet ErrorSet, string ResultPath, string? NextState) : CatchPolicy
    {
      protected override bool CanHandle(Exception exception)
      {
        return ErrorSet.Contains(exception);
      }

      protected override CatchResult<T> ToResult<T>(Exception exception)
      {
        // TODO: place exception details into the resultant output

        if (!string.IsNullOrEmpty(ResultPath))
        {
          var output = new StepFunctionData(exception).Query(ResultPath);

          return new CatchResult<T>.Failure(output, NextState);
        }

        return new CatchResult<T>.Failure(null, NextState);
      }
    }

    /// <summary>A <see cref="CatchPolicy"/> that composes multiple other <see cref="CatchPolicy"/>s.</summary>
    private sealed record CompositePolicy(params CatchPolicy[] Policies) : CatchPolicy
    {
      protected override bool CanHandle(Exception exception)
      {
        foreach (var policy in Policies)
        {
          if (policy.CanHandle(exception))
          {
            return true;
          }
        }

        return false;
      }

      protected override CatchResult<T> ToResult<T>(Exception exception)
      {
        foreach (var policy in Policies)
        {
          if (policy.CanHandle(exception))
          {
            return policy.ToResult<T>(exception);
          }
        }

        throw new InvalidOperationException("This should never be reached");
      }
    }
  }

  /// <summary>The result from evaluating a <see cref="CatchPolicy"/>.</summary>
  internal abstract record CatchResult<T>
  {
    public abstract Transition ToTransition(StepFunctionData input, bool isEnd, string next, string? taskToken = default);

    /// <summary>A <see cref="CatchResult{T}"/> that succeeded and is proceeding as normal</summary>
    public sealed record Success(T Result) : CatchResult<T>
    {
      public override Transition ToTransition(StepFunctionData input, bool isEnd, string next, string? taskToken = default)
      {
        var output = new StepFunctionData(Result);

        return isEnd
          ? new Transition.Succeed(output)
          : new Transition.Next(next, output, taskToken);
      }
    }

    /// <summary>A <see cref="CatchResult{T}"/> that caught an exception and is transitioning to error handling</summary>
    public sealed record Failure(StepFunctionData? Output, string? NextState) : CatchResult<T>
    {
      public override Transition ToTransition(StepFunctionData input, bool isEnd, string next, string? taskToken = default)
      {
        // N.B: catch operations can mutate the resultant 'output' that is passed to the next state in the step function.
        //      the 'input' here is the input to the step, the 'output' here is perhaps-mutated output if the catch
        //      clause had decided to do so

        return new Transition.Next(NextState ?? next, Output ?? input, taskToken);
      }
    }
  }
}