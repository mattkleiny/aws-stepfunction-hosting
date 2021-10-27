namespace Amazon.StepFunction.Hosting.Visualizer.ViewModels
{
  internal sealed class StepDetailViewModel : ViewModel
  {
    private readonly IStepDetailProvider provider;

    private string tabName    = string.Empty;
    private string inputData  = string.Empty;
    private string outputData = string.Empty;

    public StepDetailViewModel(IStepDetailProvider provider)
    {
      this.provider = provider;

      TabName = provider.TabName;
    }

    public string TabName
    {
      get => tabName;
      set => SetProperty(ref tabName, value);
    }

    public string InputData
    {
      get => inputData;
      set => SetProperty(ref inputData, value);
    }

    public string OutputData
    {
      get => outputData;
      set => SetProperty(ref outputData, value);
    }

    public void CopyFromHistory(ExecutionHistory history)
    {
      InputData  = provider.GetInputData(history);
      OutputData = provider.GetOutputData(history);
    }
  }
}