using Amazon.StepFunction.Hosting.Visualizer.ViewModels;

namespace Amazon.StepFunction.Hosting.Visualizer
{
  internal partial class VisualizerWindow
  {
    public VisualizerWindow(IStepFunctionExecution execution)
    {
      InitializeComponent();

      Title       = $"Execution {execution.ExecutionId}";
      DataContext = ExecutionViewModel.Create(execution);
    }
  }
}