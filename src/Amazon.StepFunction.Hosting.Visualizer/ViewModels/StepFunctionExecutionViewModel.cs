using System.Collections.ObjectModel;
using System.Windows;
using Amazon.StepFunction.Hosting.Definition;

namespace Amazon.StepFunction.Hosting.Visualizer.ViewModels
{
  public sealed class StepFunctionExecutionViewModel : ViewModel
  {
    private ObservableCollection<StepViewModel> steps         = new();
    private ObservableCollection<StepViewModel> selectedSteps = new();
    private StepViewModel?                      selectedStep  = default;

    public static StepFunctionExecutionViewModel Create(
      StepFunctionDefinition definition,
      IStepFunctionExecution execution
    )
    {
      var viewModel = new StepFunctionExecutionViewModel();
      var position  = new Point(50, 50);

      foreach (var step in definition.Steps)
      {
        viewModel.Steps.Add(new StepViewModel
        {
          Name        = step.Name,
          Description = step.Comment,
          Location    = position,
          IsTerminal  = step.End
        });

        position += new Vector(50, 50);
      }

      return viewModel;
    }

    public ObservableCollection<StepViewModel> Steps
    {
      get => steps;
      set => SetProperty(ref steps, value);
    }

    public ObservableCollection<StepViewModel> SelectedSteps
    {
      get => selectedSteps;
      set => SetProperty(ref selectedSteps, value);
    }

    public StepViewModel? SelectedStep
    {
      get => selectedStep;
      set => SetProperty(ref selectedStep, value);
    }
  }
}