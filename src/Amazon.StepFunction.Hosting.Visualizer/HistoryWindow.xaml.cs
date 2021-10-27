using System.ComponentModel;
using System.Windows.Input;
using Amazon.StepFunction.Hosting.Visualizer.ViewModels;

namespace Amazon.StepFunction.Hosting.Visualizer
{
  /// <summary>Displays a list of recent step function executions and some details about them.</summary>
  internal partial class HistoryWindow
  {
    private readonly VisualizerApplication application;

    private HistoryViewModel ViewModel { get; } = new();

    public HistoryWindow(VisualizerApplication application)
    {
      this.application = application;

      InitializeComponent();

      DataContext = ViewModel;
    }

    public void TrackExecution(IStepFunctionExecution execution, IStepFunctionDebugger debugger)
    {
      ViewModel.Entries.Add(new HistoryEntryViewModel(execution, debugger));
    }

    private void OnWindowClosing(object? sender, CancelEventArgs e)
    {
      // just hide instead of closing
      e.Cancel = true;

      Hide();
    }

    private void OnDoubleClickListView(object sender, MouseButtonEventArgs e)
    {
      if (ViewModel.SelectedEntry is { Execution: var execution })
      {
        application.OpenVisualizer(execution);
      }
    }
  }
}