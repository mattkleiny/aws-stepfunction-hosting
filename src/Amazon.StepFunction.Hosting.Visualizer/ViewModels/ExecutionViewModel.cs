using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Threading;
using Amazon.StepFunction.Hosting.Visualizer.Layouts;

namespace Amazon.StepFunction.Hosting.Visualizer.ViewModels
{
  /// <summary>Describes a single execution of a step function</summary>
  internal sealed class ExecutionViewModel : ViewModel
  {
    private readonly Dictionary<string, StepViewModel> stepsByName = new(StringComparer.OrdinalIgnoreCase);

    private string                                    title         = string.Empty;
    private ObservableCollection<StepViewModel>       steps         = new();
    private ObservableCollection<StepViewModel>       selectedSteps = new();
    private ObservableCollection<ConnectionViewModel> connections   = new();
    private StepViewModel?                            selectedStep  = default;

    public ExecutionViewModel()
    {
    }

    public ExecutionViewModel(IStepFunctionExecution execution)
    {
      var historiesByName = execution.History.ToDictionary(_ => _.StepName, StringComparer.OrdinalIgnoreCase);

      execution.StepChanged  += OnStepChanged;
      execution.HistoryAdded += OnHistoryAdded;

      // wire steps
      foreach (var step in execution.Definition.Steps)
      {
        var stepViewModel = new StepViewModel
        {
          Type        = step.Type,
          Name        = step.Name,
          Description = step.Comment,
          IsActive    = step.Name == execution.CurrentStep,
          IsStart     = step.Name == execution.Definition.StartAt,
          IsTerminal  = step.Name == execution.Definition.StartAt || step.End || step.Type is "Fail" or "Success"
        };

        if (historiesByName.TryGetValue(step.Name, out var history))
        {
          stepViewModel.CopyFromHistory(history);
        }

        stepsByName[step.Name] = stepViewModel;
        Steps.Add(stepViewModel);
      }

      // wire connections
      foreach (var step in execution.Definition.Steps)
      {
        if (stepsByName.TryGetValue(step.Name, out var source))
        {
          foreach (var connection in step.PotentialConnections)
          {
            if (stepsByName.TryGetValue(connection, out var target))
            {
              Connections.Add(new ConnectionViewModel
              {
                Source = source,
                Target = target
              });
            }
          }
        }
      }

      Title = execution.ExecutionId;

      ApplyNodeLayout();
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

    public Rect ComputeBoundingRect()
    {
      var rect = new Rect();

      foreach (var step in steps)
      {
        rect = Rect.Union(rect, new Rect(step.Location, step.Size));
      }

      return rect;
    }

    private void OnStepChanged(string nextStep)
    {
      Dispatcher.CurrentDispatcher.Invoke(() =>
      {
        foreach (var step in Steps)
        {
          step.IsActive = string.Equals(step.Name, nextStep, StringComparison.OrdinalIgnoreCase);
        }
      });
    }

    private void OnHistoryAdded(ExecutionHistory history)
    {
      Dispatcher.CurrentDispatcher.Invoke(() =>
      {
        if (stepsByName.TryGetValue(history.StepName, out var step))
        {
          step.IsActive = false;
          step.CopyFromHistory(history);
        }
      });
    }

    /// <summary>Automatically formats the graph with a simple top-down node layout</summary>
    private void ApplyNodeLayout()
    {
      // build up a layout node graph equivalent from our step view models
      var nodesByStep       = steps.ToDictionary(_ => _, step => new LayoutNode<StepViewModel>(step));
      var connectionsByStep = connections.ToLookup(_ => _.Source);

      foreach (var (step, node) in nodesByStep)
      foreach (var connection in connectionsByStep[step])
      {
        // assign parent/child relationships
        if (connection.Target != null && nodesByStep.TryGetValue(connection.Target, out var target))
        {
          target.Parent = node; // TODO: multiple parents?

          node.Children.Add(target);
        }
      }

      // recursively compute node positions from the root node
      var rootStep = steps.FirstOrDefault(_ => _.IsStart);
      if (rootStep != null && nodesByStep.TryGetValue(rootStep, out var rootNode))
      {
        ReingoldTilfordLayout.CalculateNodePositions(rootNode);

        // convert nodes back into on-screen locations
        foreach (var node in nodesByStep.Values)
        {
          const int nodeSize = ReingoldTilfordLayout.NodeSize;

          node.Item.Location = new Point(node.X, node.Y * nodeSize);
        }
      }
    }
  }
}