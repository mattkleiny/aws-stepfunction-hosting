namespace Amazon.StepFunction.Hosting.Visualizer.ViewModels
{
  /// <summary>A single entry in the history list</summary>
  internal sealed class HistoryEntryViewModel : ViewModel
  {
    private string status = string.Empty;

    public HistoryEntryViewModel(IStepFunctionExecution execution)
    {
      Execution = execution;

      execution.HistoryAdded += OnHistoryAdded;
    }

    public IStepFunctionExecution Execution { get; }

    public string Status
    {
      get => status;
      set => SetProperty(ref status, value);
    }

    private void OnHistoryAdded(ExecutionHistory history)
    {
      Status = Execution.Status.ToString();
    }
  }
}