using System;

namespace Amazon.StepFunction.Hosting.Visualizer.ViewModels
{
  /// <summary>A single entry in the history list</summary>
  internal sealed class HistoryEntryViewModel : ViewModel
  {
    private string   status    = string.Empty;
    private DateTime startedAt = default;

    public HistoryEntryViewModel(IStepFunctionExecution execution)
    {
      Execution = execution;
      StartedAt = execution.StartedAt;

      execution.HistoryAdded += OnHistoryAdded;
    }

    public IStepFunctionExecution Execution { get; }

    public string Status
    {
      get => status;
      set => SetProperty(ref status, value);
    }

    public DateTime StartedAt
    {
      get => startedAt;
      set => SetProperty(ref startedAt, value);
    }

    private void OnHistoryAdded(ExecutionHistory history)
    {
      Status = Execution.Status.ToString();
    }
  }
}