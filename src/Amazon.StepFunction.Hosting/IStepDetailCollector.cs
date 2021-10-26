using System;
using System.Threading.Tasks;

namespace Amazon.StepFunction.Hosting
{
  /// <summary>Allows collecting extra details for use in Step Function debugging</summary>
  public interface IStepDetailCollector
  {
    Task<object> OnBeforeExecuteStep(string stepName, StepFunctionData input);
    Task<object> OnAfterExecuteStep(string stepName, StepFunctionData output);

    void AugmentHistory(object beforeDetails, object afterDetails, ExecutionHistory history);
  }

  /// <summary>A <see cref="IStepDetailCollector"/> that collects before/after diffs of some context surrounding step function execution.</summary>
  public abstract class StepDiffCollector : IStepDetailCollector
  {
    protected abstract Task<string> GetDetailsForStep(string stepName, StepFunctionData data);

    async Task<object> IStepDetailCollector.OnBeforeExecuteStep(string stepName, StepFunctionData input)
    {
      return await GetDetailsForStep(stepName, input);
    }

    async Task<object> IStepDetailCollector.OnAfterExecuteStep(string stepName, StepFunctionData output)
    {
      return await GetDetailsForStep(stepName, output);
    }

    void IStepDetailCollector.AugmentHistory(object beforeDetails, object afterDetails, ExecutionHistory history)
    {
      history.UserData.Add(new Details(
        Type: GetType(),
        Before: (string) beforeDetails,
        After: (string) afterDetails
      ));
    }

    protected sealed record Details(Type Type, string Before, string After);
  }
}