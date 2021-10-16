using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;

namespace Amazon.StepFunction.Hosting.Visualizer.ViewModels
{
  internal sealed class ExecutionViewModel : ViewModel
  {
    private string                                    title         = string.Empty;
    private ObservableCollection<StepViewModel>       steps         = new();
    private ObservableCollection<StepViewModel>       selectedSteps = new();
    private ObservableCollection<ConnectionViewModel> connections   = new();
    private StepViewModel?                            selectedStep  = default;

    public static ExecutionViewModel Create(IStepFunctionExecution execution)
    {
      var viewModel   = new ExecutionViewModel { Title = execution.ExecutionId };
      var stepsByName = new Dictionary<string, StepViewModel>(StringComparer.OrdinalIgnoreCase);

      var position = new Point(150, 50);

      // TODO: graph layout algorithm? reingold tilford?

      // wire steps
      foreach (var step in execution.Definition.Steps)
      {
        var stepViewModel = stepsByName[step.Name] = new StepViewModel
        {
          Name        = step.Name,
          Description = step.Comment,
          Location    = position
        };

        viewModel.Steps.Add(stepViewModel);

        position += new Vector(0, 150);
      }

      // wire connections
      foreach (var step in execution.Definition.Steps)
      {
        if (stepsByName.TryGetValue(step.Name, out var source))
        {
          foreach (var connection in step.Connections)
          {
            if (stepsByName.TryGetValue(connection, out var target))
            {
              viewModel.Connections.Add(new ConnectionViewModel
              {
                Source = source,
                Target = target
              });
            }
          }
        }
      }

      execution.StepChanged  += viewModel.OnStepChanged;
      execution.HistoryAdded += viewModel.OnHistoryAdded;

      return viewModel;
    }

    public string Title
    {
      get => title;
      set => SetProperty(ref title, value);
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

    public ObservableCollection<ConnectionViewModel> Connections
    {
      get => connections;
      set => SetProperty(ref connections, value);
    }

    public StepViewModel? SelectedStep
    {
      get => selectedStep;
      set => SetProperty(ref selectedStep, value);
    }

    public Rect BoundingRect
    {
      get
      {
        // TODO: fix this up; not encapsulating properly
        var rect = new Rect();

        foreach (var step in steps)
        {
          rect.Union(new Rect(step.Location, step.Size));
        }

        return rect;
      }
    }

    private void OnStepChanged(string nextStep)
    {
      // TODO: optimize these lookups?

      foreach (var step in Steps)
      {
        step.IsActive = string.Equals(step.Name, nextStep, StringComparison.OrdinalIgnoreCase);
      }
    }

    private void OnHistoryAdded(ExecutionHistory history)
    {
      foreach (var step in Steps)
      {
        if (string.Equals(step.Name, history.StepName, StringComparison.OrdinalIgnoreCase))
        {
          step.Data         = history.Data.Cast<string>() ?? string.Empty;
          step.IsSuccessful = history.IsSuccessful;
          step.IsFailed     = history.IsFailed;

          break;
        }
      }
    }
  }
}