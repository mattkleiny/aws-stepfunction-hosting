﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Amazon.StepFunction.Hosting.Internal;
using Amazon.StepFunction.Hosting.Utilities;
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Core.Layout;

namespace Amazon.StepFunction.Hosting.ViewModels
{
  /// <summary>Describes a single execution of a step function</summary>
  internal sealed class ExecutionViewModel : ViewModel, IGraphLayoutTarget
  {
    private readonly Dictionary<string, StepViewModel> stepsByName = new(StringComparer.OrdinalIgnoreCase);
    private readonly IStepFunctionExecution            execution   = null!;

    private string                                    title            = string.Empty;
    private ObservableCollection<StepViewModel>       steps            = new();
    private ObservableCollection<StepViewModel>       selectedSteps    = new();
    private ObservableCollection<ConnectionViewModel> connections      = new();
    private StepViewModel?                            selectedStep     = default;
    private int                                       selectedTabIndex = 0;

    public ExecutionViewModel()
    {
    }

    public ExecutionViewModel(IStepFunctionExecution execution, IStepFunctionDebugger debugger, List<IStepDetailProvider> detailProviders)
    {
      this.execution = execution;

      var historiesByName = execution.History.ToDictionaryByLatest(_ => _.StepName, _ => _.ExecutedAt, StringComparer.OrdinalIgnoreCase);

      debugger.StepChanged    += OnStepChanged;
      debugger.HistoryChanged += OnHistoryChanged;

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
      foreach (var step in execution.Definition.Steps)
      {
        if (step.NestedBranches.Any())
        {
          foreach (var branch in step.NestedBranches)
          {
            InitializeStep(new StepGroupViewModel(branch, execution, debugger, detailProviders)
            {
              Type       = step.Type,
              Name       = step.Name,
              Comment    = step.Comment,
              IsActive   = step.Name == execution.CurrentStep,
              IsTerminal = step.Name == execution.Definition.StartAt || step.IsTerminal,
              Details    = new ObservableCollection<StepDetailViewModel>(detailProviders.Select(provider => new StepDetailViewModel(provider)))
            });
          }
        }
        else
        {
          InitializeStep(new StepViewModel
          {
            Type       = step.Type,
            Name       = step.Name,
            Comment    = step.Comment,
            IsActive   = step.Name == execution.CurrentStep,
            IsTerminal = step.Name == execution.Definition.StartAt || step.IsTerminal,
            Details    = new ObservableCollection<StepDetailViewModel>(detailProviders.Select(provider => new StepDetailViewModel(provider)))
          });
        }
      }

      // wire connections
      foreach (var step in execution.Definition.Steps)
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

      Title = execution.ExecutionId;
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

    public int SelectedTabIndex
    {
      get => selectedTabIndex;
      set
      {
        // HACK: re-select old tab; tab controls are fiddly in WPF
        if (value == -1)
        {
          var oldTabIndex = selectedTabIndex;

          Dispatcher.CurrentDispatcher.InvokeAsync<Task>(async () =>
          {
            await Task.Yield();

            SelectedTabIndex = oldTabIndex;
          });
        }

        SetProperty(ref selectedTabIndex, value);
      }
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

    private void OnStepChanged(IStepFunctionExecution scope, string nextStep)
    {
      Dispatcher.CurrentDispatcher.Invoke(() =>
      {
        if (scope == execution)
        {
          foreach (var step in Steps)
          {
            step.IsActive = string.Equals(step.Name, nextStep, StringComparison.OrdinalIgnoreCase);
          }
        }
        else if (scope.Parent == execution)
        {
          foreach (var step in Steps.OfType<StepGroupViewModel>())
          {
            if (step.IsForBranch(scope.Definition))
            {
              step.OnStepChanged(scope, nextStep);
              break;
            }
          }
        }
      });
    }

    private void OnHistoryChanged(IStepFunctionExecution scope, ExecutionHistory history)
    {
      Dispatcher.CurrentDispatcher.Invoke((Action) (() =>
      {
        if (scope == execution)
        {
          if (stepsByName.TryGetValue(history.StepName, out var step))
          {
            step.IsActive = false;
            step.CopyFromHistory(history);
          }
        }
        else if (scope.Parent == execution)
        {
          foreach (var step in Steps.OfType<StepGroupViewModel>())
          {
            if (step.IsForBranch(scope.Definition))
            {
              step.OnHistoryAdded(scope, history);
            }
          }
        }
      }));
    }

    public void ApplyGraphLayout(GraphLayoutAlgorithm algorithm)
    {
      // layout sub-graphs, first
      foreach (var group in steps.OfType<StepGroupViewModel>())
      {
        group.ApplyGraphLayout(algorithm);
      }

      algorithm(this);
    }

    GeometryGraph IGraphLayoutTarget.ToGeometryGraph()
    {
      var result = new GeometryGraph();

      foreach (var step in steps)
      {
        var curve = CurveFactory.CreateRectangle(
          width: step.Size.Width,
          height: step.Size.Height,
          center: new(
            step.Location.X + step.Size.Width / 2f,
            step.Location.Y + step.Size.Height / 2f
          )
        );

        if (step is StepGroupViewModel group)
        {
          var boundingRect = group.ComputeBoundingRect();

          curve = CurveFactory.CreateRectangle(
            width: boundingRect.Width,
            height: boundingRect.Height,
            center: new(
              boundingRect.Left + boundingRect.Width / 2f,
              boundingRect.Top + boundingRect.Height / 2f
            )
          );
        }

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
          node.BoundingBox.Left,
          -node.BoundingBox.Top // flip the graph vertically
        );

        if (step is StepGroupViewModel group)
        {
          group.Size = new Size(node.BoundingBox.Width, node.BoundingBox.Height);
        }
      }
    }
  }
}