using System.Linq;

namespace Amazon.StepFunction.Hosting.Visualizer
{
  /// <summary>Provides extra details to the Step Function visualizer to aid in debugging.</summary>
  public interface IStepDetailProvider
  {
    string TabName { get; }

    string GetInputData(ExecutionHistory history);
    string GetOutputData(ExecutionHistory history);
  }

  /// <summary>Provides Step Function step input/output details to the visualizer.</summary>
  internal sealed class InputOutputDetailProvider : IStepDetailProvider
  {
    public string TabName => "Input/Output";

    public string GetInputData(ExecutionHistory history)
    {
      return history.InputData.ToIndentedString();
    }

    public string GetOutputData(ExecutionHistory history)
    {
      return history.OutputData.ToIndentedString();
    }
  }

  /// <summary>A <see cref="StepDiffCollector"/> that providers before/after details to the visualizer.</summary>
  public abstract class StepDiffDetailProvider : StepDiffCollector, IStepDetailProvider
  {
    public abstract string TabName { get; }

    string IStepDetailProvider.GetInputData(ExecutionHistory history)
    {
      var diff = history.UserData.OfType<Diff>().FirstOrDefault(_ => _.Type == GetType());

      if (diff != null)
      {
        return diff.Before;
      }

      return string.Empty;
    }

    string IStepDetailProvider.GetOutputData(ExecutionHistory history)
    {
      var diff = history.UserData.OfType<Diff>().FirstOrDefault(_ => _.Type == GetType());

      if (diff != null)
      {
        return diff.After;
      }

      return string.Empty;
    }
  }
}