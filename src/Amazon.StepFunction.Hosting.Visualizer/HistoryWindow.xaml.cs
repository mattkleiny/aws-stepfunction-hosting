using System.Windows.Input;
using Amazon.StepFunction.Hosting.Visualizer.ViewModels;

namespace Amazon.StepFunction.Hosting.Visualizer
{
  internal partial class HistoryWindow
  {
    private readonly VisualizerApplication application;

    public HistoryWindow(VisualizerApplication application)
    {
      this.application = application;

      InitializeComponent();

      DataContext = ViewModel;
    }

    public HistoryViewModel ViewModel { get; } = new();

    private void OnDoubleClickListView(object sender, MouseButtonEventArgs e)
    {
      if (ViewModel.SelectedEntry is { Execution: var execution })
      {
        application.OpenVisualizerWindow(execution);
      }
    }
  }
}