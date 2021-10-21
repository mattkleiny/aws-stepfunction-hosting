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

    public async Task<CatchResult> EvaluateAsync(bool isEnabled, Func<Task<StepFunctionData>> body)
    {
      try
      {
        return new CatchResult(await body());
      }
      catch (Exception exception) when (isEnabled && CanHandle(exception))
      {
        return ToResult(exception);
      }
    }

    protected abstract bool        CanHandle(Exception exception);
    protected abstract CatchResult ToResult(Exception exception);

    /// <summary>A no-op <see cref="CatchPolicy"/>.</summary>
    private sealed record NullCatchPolicy : CatchPolicy
    {
      protected override bool CanHandle(Exception exception)
      {
        return false;
      }

      protected override CatchResult ToResult(Exception exception)
      {
        return default;
      }
    }

    /// <summary>A <see cref="CatchPolicy"/> that evaluates an <see cref="ErrorSet"/>.</summary>
    private sealed record StandardPolicy(ErrorSet ErrorSet, string ResultPath, string? NextState) : CatchPolicy
    {
      protected override bool CanHandle(Exception exception)
      {
        return ErrorSet.Contains(exception);
      }

      protected override CatchResult ToResult(Exception exception)
      {
        // TODO: transform exception into the resultant output

        return new CatchResult(null, NextState);
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

      protected override CatchResult ToResult(Exception exception)
      {
        foreach (var policy in Policies)
        {
          if (policy.CanHandle(exception))
          {
            return policy.ToResult(exception);
          }
        }

        throw new Exception("This should never be reached");
      }
    }
  }

  /// <summary>The result from evaluating a <see cref="CatchPolicy"/>.</summary>
  internal readonly record struct CatchResult(StepFunctionData? Output, string? CatchState = default)
  {
    public Transition ToTransition(StepFunctionData input, bool isEnd, string nextState, string? taskToken = default)
    {
      // N.B: catch operations can mutate the resultant 'output' that is passed to the next state in the step function.
      //      the 'input' here is the input to the step, the 'output' here is perhaps-mutated output if the catch
      //      clause had decided to do so
      
      if (CatchState != null)
      {
        return Transitions.Next(CatchState, Output ?? input);
      }

      return isEnd
        ? Transitions.Succeed(Output ?? input)
        : Transitions.Next(nextState, Output ?? input, taskToken);
    }
  }
}