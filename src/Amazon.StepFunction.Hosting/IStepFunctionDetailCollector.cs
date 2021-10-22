using System.Threading.Tasks;

namespace Amazon.StepFunction.Hosting
{
  /// <summary>Allows collecting extra details for use in Step Function debugging</summary>
  public interface IStepFunctionDetailCollector
  {
    Task<object> OnBeforeExecuteStep(string stepName, StepFunctionData input);
    Task<object> OnAfterExecuteStep(string stepName, StepFunctionData output);

    void AugmentHistory(object beforeDetails, object afterDetails, ExecutionHistory history);
  }
}