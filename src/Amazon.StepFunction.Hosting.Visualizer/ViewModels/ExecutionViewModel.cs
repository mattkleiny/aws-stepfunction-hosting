using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Threading;
using Amazon.StepFunction.Hosting.Visualizer.Layouts;
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Core.Layout;

namespace Amazon.StepFunction.Hosting.Visualizer.ViewModels
{
  /// <summary>Describes a single execution of a step function</summary>
  internal sealed class ExecutionViewModel : ViewModel, IGraphLayoutTarget
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

      ApplyGraphLayout(GraphLayouts.Standard);
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

    public void ApplyGraphLayout(GraphLayout layout)
    {
      layout(this);
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

    GeometryGraph IGraphLayoutTarget.ToGeometryGraph()
    {
      var result = new GeometryGraph();

      foreach (var step in steps)
      {
        var curve = CurveFactory.CreateRectangle(
          // size doesn't matter, the layout will imply a minimum width/height
          width: 0,
          height: 0,
          center: new(0, 0)
        );

        result.Nodes.Add(new Node(curve, step));
      }

      foreach (var connection in connections)
      {
        var source = result.FindNodeByUserData(connection.Source);
        var target = result.FindNodeByUserData(connection.Target);

        var edge = new Edge(source, target)
        {
          Weight   = 1,
          UserData = connection
        };

        result.Edges.Add(edge);
      }

      return result;
    }

    void IGraphLayoutTarget.FromGeometryGraph(GeometryGraph graph)
    {
      foreach (var node in graph.Nodes)
      {
        var step = (StepViewModel) node.UserData;

        step.Location = new Point(
          node.BoundingBox.Center.X,
          -node.BoundingBox.Center.Y // flip the graph vertically
        );
      }
    }
  }
}