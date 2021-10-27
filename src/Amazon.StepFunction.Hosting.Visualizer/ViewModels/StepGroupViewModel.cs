using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using Amazon.StepFunction.Hosting.Definition;
using Amazon.StepFunction.Hosting.Visualizer.Internal;
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Core.Layout;

namespace Amazon.StepFunction.Hosting.Visualizer.ViewModels
{
  // TODO: combine this and ExecutionViewModel?

  /// <summary>Describes a group of steps in a Step Function</summary>
  internal sealed class StepGroupViewModel : StepViewModel, IGraphLayoutTarget
  {
    private readonly Dictionary<string, StepViewModel> stepsByName = new(StringComparer.OrdinalIgnoreCase);
    private readonly StepFunctionDefinition            definition;

    private ObservableCollection<StepViewModel>       steps       = new();
    private ObservableCollection<ConnectionViewModel> connections = new();

    public StepGroupViewModel(StepFunctionDefinition definition, IStepFunctionExecution execution, IStepFunctionDebugger debugger, List<IStepDetailProvider> detailProviders)
    {
      this.definition = definition; // remember which branch of the step function we're representing

      var historiesByName = debugger
        .GetExecutionHistory(execution)
        .Where(_ => _.Definition == definition && _.Execution.Parent == execution)
        .ToDictionary(_ => _.StepName, StringComparer.OrdinalIgnoreCase);

      void InitializeStep(StepViewModel viewModel)
      {
        if (historiesByName.TryGetValue(viewModel.Name, out var history))
        {
          viewModel.CopyFromHistory(history);
        }

        stepsByName[viewModel.Name] = viewModel;
        Steps.Add(viewModel);
      }

      // wire steps
      foreach (var step in definition.Steps)
      {
        if (step.NestedBranches.Any())
        {
          throw new NotSupportedException("The visualizer currently does not support more than one nesting level of Step Functions");
        }

        InitializeStep(new StepViewModel
        {
          Type       = step.Type,
          Name       = step.Name,
          Comment    = step.Comment,
          IsStart    = step.Name == definition.StartAt,
          IsTerminal = step.Name == definition.StartAt || step.IsTerminal,
          Details    = new ObservableCollection<StepDetailViewModel>(detailProviders.Select(provider => new StepDetailViewModel(provider)))
        });
      }

      // wire connections
      foreach (var step in definition.Steps)
      {
        if (stepsByName.TryGetValue(step.Name, out var source))
        {
          foreach (var connection in step.PossibleConnections)
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

      GraphLayouts.Standard.Invoke(this);
    }

    public ObservableCollection<StepViewModel> Steps
    {
      get => steps;
      set => SetProperty(ref steps, value);
    }

    public ObservableCollection<ConnectionViewModel> Connections
    {
      get => connections;
      set => SetProperty(ref connections, value);
    }

    public bool IsForBranch(StepFunctionDefinition definition)
    {
      return this.definition == definition;
    }

    public void OnStepChanged(IStepFunctionExecution execution, string nextStep)
    {
      foreach (var step in Steps)
      {
        step.IsActive = string.Equals(step.Name, nextStep, StringComparison.OrdinalIgnoreCase);
      }
    }

    public void OnHistoryAdded(IStepFunctionExecution execution, ExecutionHistory history)
    {
      if (stepsByName.TryGetValue(history.StepName, out var step))
      {
        step.IsActive = false;
        step.CopyFromHistory(history);
      }
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