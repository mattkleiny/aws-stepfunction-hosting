namespace Amazon.StepFunction.Hosting
{
  /// <summary>Provides extra details to the Step Function visualizer to aid in debugging.</summary>
  public interface IStepDetailProvider
  {
    string TabName { get; }

    string GetInputData(ExecutionHistory  history);
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
  public sealed class DiffDetailProvider : IStepDetailProvider
  {
    public DiffDetailProvider(string tabName, StepDiffCollector collector)
    {
      TabName   = tabName;
      Collector = collector;
    }

    public string            TabName   { get; }
    public StepDiffCollector Collector { get; }

    string IStepDetailProvider.GetInputData(ExecutionHistory history)
    {
      return Collector.GetBeforeSnippet(history);
    }

    string IStepDetailProvider.GetOutputData(ExecutionHistory history)
    {
      return Collector.GetAfterSnippet(history);
    }
  }
}