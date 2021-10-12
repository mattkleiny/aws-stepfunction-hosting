using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Amazon.StepFunction.Hosting.Evaluation
{
  /// <summary>The result from evaluating a <see cref="CatchPolicy"/>.</summary>
  public readonly record struct CatchResult(StepFunctionData Output, string? NextState = default);

  /// <summary>Permits constructing catch policies from pieces.</summary>
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
      if (!isEnabled)
      {
        return new CatchResult(await body());
      }

      try
      {
        return new CatchResult(await body());
      }
      catch (Exception exception) when (CanHandle(exception))
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

    /// <summary>A <see cref="CatchPolicy"/> that evaluates an error list and behaves accordingly</summary>
    private sealed record StandardPolicy(ErrorSet ErrorSet, string ResultPath, string? NextState) : CatchPolicy
    {
      protected override bool CanHandle(Exception exception)
      {
        return ErrorSet.CanHandle(exception);
      }

      protected override CatchResult ToResult(Exception exception)
      {
        // TODO: transformers on this?
        var output = new StepFunctionData(exception);

        return new CatchResult(output.GetPath(ResultPath), NextState);
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