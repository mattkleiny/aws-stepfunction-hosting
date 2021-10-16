using System;
using System.Collections.ObjectModel;
using System.Windows;

namespace Amazon.StepFunction.Hosting.Visualizer.ViewModels
{
  internal sealed class ExecutionViewModel : ViewModel
  {
    private ObservableCollection<StepViewModel> steps         = new();
    private ObservableCollection<StepViewModel> selectedSteps = new();
    private StepViewModel?                      selectedStep  = default;

    public static ExecutionViewModel Create(IStepFunctionExecution execution)
    {
      var viewModel = new ExecutionViewModel();
      var position  = new Point(50, 50);

      execution.StepChanged += viewModel.OnStepChanged;

      foreach (var step in execution.Definition.Steps)
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

    private void OnStepChanged(string nextStep)
    {
      foreach (var step in Steps)
      {
        step.IsActive = string.Equals(step.Name, nextStep, StringComparison.OrdinalIgnoreCase);
      }
    }
  }
}