using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Amazon.StepFunction.Hosting.Visualizer.ViewModels;

namespace Amazon.StepFunction.Hosting.Visualizer
{
  /// <summary>Presents a visual graph of step function executions for exploration.</summary>
  internal partial class VisualizerWindow
  {
    private readonly VisualizerApplication application;

    public VisualizerWindow(VisualizerApplication application, IStepFunctionExecution execution)
    {
      this.application = application;

      InitializeComponent();

      ViewModel   = new ExecutionViewModel(execution, application.DetailProviders);
      DataContext = ViewModel;

      Width  = application.Settings.LastWindowSize.Width;
      Height = application.Settings.LastWindowSize.Height;

      // HACK: wait for one-way propagation back to sizing properties
      Dispatcher.Invoke(async () =>
      {
        await Task.Yield();

        Title = $"{Title} - {execution.ExecutionId}";

        CenterOnEverything(isAnimated: false);
      });
    }

    public ExecutionViewModel ViewModel { get; }

    private void OnKeyDown(object sender, KeyEventArgs e)
    {
      switch (e.Key)
      {
        case Key.A:
        {
          CenterOnEverything(isAnimated: true);
          break;
        }
        case Key.F:
        {
          CenterOnSelected();
          break;
        }
      }
    }

    private void CenterOnEverything(bool isAnimated)
    {
      var boundingRect = ViewModel.ComputeBoundingRect();

      var middlePoint = new Point(
        boundingRect.X + boundingRect.Width / 2,
        boundingRect.Y + boundingRect.Height / 2
      );

      NodeEditor.BringIntoView(middlePoint, isAnimated);
    }

    private void CenterOnSelected()
    {
      if (ViewModel.SelectedStep != null)
      {
        NodeEditor.BringIntoView(ViewModel.SelectedStep.Location);
      }
    }

    private async void OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
      application.Settings.LastWindowSize = e.NewSize;

      await application.Settings.SaveAsync();
    }

    private void OnSubEditorSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
      NodeEditor.SelectedItem = null;
    }
  }
}