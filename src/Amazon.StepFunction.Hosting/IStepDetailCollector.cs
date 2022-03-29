using System.Linq;
using System.Threading.Tasks;

namespace Amazon.StepFunction.Hosting
{
  /// <summary>Allows collecting extra details for use in Step Function debugging</summary>
  public interface IStepDetailCollector
  {
    string Key { get; }

    Task<object> OnBeforeExecuteStep(string stepName, StepFunctionData input);
    Task<object> OnAfterExecuteStep(string stepName, StepFunctionData output);

    void AugmentHistory(object beforeDetails, object afterDetails, ExecutionHistory history);
  }

  /// <summary>A <see cref="IStepDetailCollector"/> that collects before/after diffs of some context surrounding step function execution.</summary>
  public abstract class StepDiffCollector : IStepDetailCollector
  {
    public abstract string Key { get; }

    protected abstract Task<string> GetDetailsForStep(string stepName, StepFunctionData data);

    public string GetBeforeSnippet(ExecutionHistory history)
    {
      var diff = history.UserData.OfType<Diff>().FirstOrDefault(_ => _.Key == Key);

      if (diff != null)
      {
        return diff.Before;
      }

      return string.Empty;
    }

    public string GetAfterSnippet(ExecutionHistory history)
    {
      var diff = history.UserData.OfType<Diff>().FirstOrDefault(_ => _.Key == Key);

      if (diff != null)
      {
        return diff.After;
      }

      return string.Empty;
    }

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
      history.UserData.Add(new Diff(
        Key: Key,
        Before: (string)beforeDetails,
        After: (string)afterDetails
      ));
    }

    protected sealed record Diff(string Key, string Before, string After);
  }
}
