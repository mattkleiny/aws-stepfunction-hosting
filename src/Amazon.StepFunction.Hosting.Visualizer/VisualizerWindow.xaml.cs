using System.Windows.Input;
using Amazon.StepFunction.Hosting.Visualizer.ViewModels;

namespace Amazon.StepFunction.Hosting.Visualizer
{
  internal partial class VisualizerWindow
  {
    public VisualizerWindow(IStepFunctionExecution execution)
    {
      InitializeComponent();

      ViewModel   = ExecutionViewModel.Create(execution);
      DataContext = ViewModel;
    }

    public ExecutionViewModel ViewModel { get; }

    private void OnKeyDown(object sender, KeyEventArgs e)
    {
      switch (e.Key)
      {
        case Key.A:
        {
          NodeEditor.BringIntoView(ViewModel.BoundingRect);

          break;
        }
        case Key.F:
        {
          if (ViewModel.SelectedStep != null)
          {
            NodeEditor.BringIntoView(ViewModel.SelectedStep.Location);
          }

          break;
        }
      }
    }
  }
}