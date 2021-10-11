using System.Collections.ObjectModel;
using System.Windows;

namespace Amazon.StepFunction.Hosting.Visualizer.ViewModels
{
  public sealed class ExecutionViewModel : ViewModel
  {
    private ObservableCollection<StepViewModel> steps         = new();
    private StepViewModel?                      selectedStep  = default;
    private ObservableCollection<StepViewModel> selectedSteps = new();
    private string                              executionId   = string.Empty;
    private Point                               position      = default;

    public ObservableCollection<StepViewModel> Steps
    {
      get => steps;
      set => SetProperty(ref steps, value);
    }

    public StepViewModel? SelectedStep
    {
      get => selectedStep;
      set => SetProperty(ref selectedStep, value);
    }

    public ObservableCollection<StepViewModel> SelectedSteps
    {
      get => selectedSteps;
      set => SetProperty(ref selectedSteps, value);
    }

    public string ExecutionId
    {
      get => executionId;
      set => SetProperty(ref executionId, value);
    }

    public Point Position
    {
      get => position;
      set => SetProperty(ref position, value);
    }

    public ExecutionViewModel()
    {
      Steps.Add(new StepViewModel { Position = new Point(100, 200) });
      Steps.Add(new StepViewModel { Position = new Point(200, 300) });
      Steps.Add(new StepViewModel { Position = new Point(300, 400) });
      Steps.Add(new StepViewModel { Position = new Point(400, 500) });
    }
  }
}