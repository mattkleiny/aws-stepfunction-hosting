using System;

namespace Amazon.StepFunction.Hosting.Visualizer.ViewModels
{
  /// <summary>A single entry in the history list</summary>
  internal sealed class HistoryEntryViewModel : ViewModel
  {
    private string   status    = string.Empty;
    private DateTime startedAt = default;

    public HistoryEntryViewModel(IStepFunctionExecution execution, IStepFunctionDebugger debugger)
    {
      Execution = execution;
      StartedAt = execution.StartedAt;

      debugger.HistoryChanged += OnHistoryChanged;
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

    private void OnHistoryChanged(IStepFunctionExecution execution, ExecutionHistory history)
    {
      // if this history change is related to our top-level execution somehow, then update the status
      if (Execution == execution.ResolveRootExecution())
      {
        Status = Execution.Status.ToString();
      }
    }
  }
}