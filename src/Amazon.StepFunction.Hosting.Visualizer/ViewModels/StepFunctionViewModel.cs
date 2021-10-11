using System.Collections.ObjectModel;

namespace Amazon.StepFunction.Hosting.Visualizer.ViewModels
{
  public sealed class StepFunctionViewModel : ViewModel
  {
    private ObservableCollection<ExecutionViewModel> executions         = new();
    private ExecutionViewModel?                      selectedExecution  = default;
    private ObservableCollection<ExecutionViewModel> selectedExecutions = new();

    public ObservableCollection<ExecutionViewModel> Executions
    {
      get => executions;
      set => SetProperty(ref executions, value);
    }

    public ExecutionViewModel? SelectedExecution
    {
      get => selectedExecution;
      set => SetProperty(ref selectedExecution, value);
    }

    public ObservableCollection<ExecutionViewModel> SelectedExecutions
    {
      get => selectedExecutions;
      set => SetProperty(ref selectedExecutions, value);
    }
  }
}