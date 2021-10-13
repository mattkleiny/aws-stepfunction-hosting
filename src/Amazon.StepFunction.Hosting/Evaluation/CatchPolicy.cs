using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Amazon.StepFunction.Hosting.Evaluation
{
  /// <summary>The result from evaluating a <see cref="CatchPolicy"/>.</summary>
  internal readonly record struct CatchResult(StepFunctionData Output, string? NextState = default);

  /// <summary>Permits constructing catch policies from pieces.</summary>
  internal abstract record CatchPolicy
  {
    public static CatchPolicy Null { get; } = new NullCatchPolicy();

    public static CatchPolicy Standard(ErrorSet errorSet, string resultPath, string nextState)
      => new ErrorSetPolicy(errorSet, resultPath, nextState);

    public static CatchPolicy Composite(IEnumerable<CatchPolicy> policies)
      => new CompositePolicy(policies.ToArray());

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
    private sealed record ErrorSetPolicy(ErrorSet ErrorSet, string ResultPath, string? NextState) : CatchPolicy
    {
      protected override bool CanHandle(Exception exception)
      {
        return ErrorSet.CanHandle(exception);
      }

      protected override CatchResult ToResult(Exception exception)
      {
        var output = new StepFunctionData(exception).Query(ResultPath);

        return new CatchResult(output, NextState);
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
}