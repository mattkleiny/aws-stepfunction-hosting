using Amazon.StepFunction.Hosting.Visualizer.ViewModels;

namespace Amazon.StepFunction.Hosting.Visualizer
{
  public partial class VisualizerWindow
  {
    public VisualizerWindow()
    {
      InitializeComponent();

      var application = VisualizerApplication.Current;
      if (application.Host != null)
      {
        DataContext = StepFunctionExecutionViewModel.Create(application.Host.Definition);
      }
    }
  }
}