namespace Amazon.StepFunction.Hosting.Visualizer.ViewModels
{
  internal sealed class HistoryEntryViewModel : ViewModel
  {
    private readonly IStepFunctionExecution execution;

    private string status = string.Empty;

    public HistoryEntryViewModel(IStepFunctionExecution execution)
    {
      this.execution = execution;

      execution.HistoryAdded += OnHistoryAdded;
    }

    public IStepFunctionExecution Execution   => execution;
    public string                 ExecutionId => execution.ExecutionId;

    public string Status
    {
      get => status;
      set => SetProperty(ref status, value);
    }

    private void OnHistoryAdded(ExecutionHistory history)
    {
      Status = execution.Status.ToString();
    }
  }
}