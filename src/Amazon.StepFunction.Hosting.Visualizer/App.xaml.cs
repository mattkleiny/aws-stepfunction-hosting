using System;
using System.Collections.Generic;
using System.Drawing;
using System.Reflection;
using System.Windows;
using System.Windows.Forms;

namespace Amazon.StepFunction.Hosting.Visualizer
{
  public partial class VisualizerApplication
  {
    private NotifyIcon?     notifyIcon;
    private HashSet<string> detectedExecutions = new();

    public VisualizerApplication()
    {
      InitializeComponent();
    }

    public StepFunctionHost? Host     { get; init; }
    public string            HostName { get; init; } = "Step Function";

    public bool AutomaticallyOpenExecutions { get; set; } = true;
    public bool AutomaticallyOpenFailures   { get; set; } = true;
    public bool AutomaticallyOpenSuccesses  { get; set; } = true;

    protected override void OnStartup(StartupEventArgs e)
    {
      base.OnStartup(e);

      CreateTrayIcon();

      if (Host != null)
      {
        Host.ExecutionStarted += OnExecutionStarted;
        Host.ExecutionStopped += OnExecutionStopped;
      }
    }

    protected override void OnExit(ExitEventArgs e)
    {
      notifyIcon?.Dispose();

      base.OnExit(e);
    }

    private void OnExecutionStarted(IStepFunctionExecution execution)
    {
      if (AutomaticallyOpenExecutions &&
          detectedExecutions.Add(execution.ExecutionId))
      {
        OpenVisualizerWindow(execution);
      }
    }

    private void OnExecutionStopped(IStepFunctionExecution execution)
    {
      // TODO: visualize the completed execution (pre-fill with history)

      if (AutomaticallyOpenFailures &&
          execution.Status == ExecutionStatus.Failure &&
          detectedExecutions.Add(execution.ExecutionId))
      {
        OpenVisualizerWindow(execution);
      }

      if (AutomaticallyOpenSuccesses &&
          execution.Status == ExecutionStatus.Success &&
          detectedExecutions.Add(execution.ExecutionId))
      {
        OpenVisualizerWindow(execution);
      }
    }

    private void CreateTrayIcon()
    {
      ContextMenuStrip menuStrip = new();

      notifyIcon = new NotifyIcon
      {
        Icon             = Icon.ExtractAssociatedIcon(Assembly.GetExecutingAssembly().Location),
        Text             = HostName,
        Visible          = true,
        ContextMenuStrip = menuStrip
      };

      menuStrip.Items.Add(new ToolStripMenuItem("Open history list", null, (_, _) => OpenHistoryWindow()));

      menuStrip.Items.Add(new ToolStripDropDownButton("Open visualizer automatically", null, new ToolStripItem[]
      {
        new ToolStripMenuItem("New executions", null, OnToggleNewExecutions)
        {
          Checked      = AutomaticallyOpenExecutions,
          CheckOnClick = true
        },
        new ToolStripMenuItem("Failed executions", null, OnToggleFailedExecutions)
        {
          Checked      = AutomaticallyOpenFailures,
          CheckOnClick = true
        },
        new ToolStripMenuItem("Successful executions", null, OnToggleSuccessfulExecutions)
        {
          Checked      = AutomaticallyOpenSuccesses,
          CheckOnClick = true
        }
      }));

      menuStrip.Items.Add(new ToolStripSeparator());
      menuStrip.Items.Add(new ToolStripMenuItem("Exit", null, (_, _) => Current.Shutdown()));

      notifyIcon.DoubleClick += OnTrayIconDoubleClick;
    }

    private void OnTrayIconDoubleClick(object? sender, EventArgs e)
    {
      OpenHistoryWindow();
    }

    private void OnToggleNewExecutions(object? sender, EventArgs e)
    {
      AutomaticallyOpenExecutions = !AutomaticallyOpenExecutions;

      if (sender is ToolStripMenuItem menuItem)
      {
        menuItem.Checked = AutomaticallyOpenExecutions;
      }
    }

    private void OnToggleFailedExecutions(object? sender, EventArgs e)
    {
      AutomaticallyOpenFailures = !AutomaticallyOpenFailures;

      if (sender is ToolStripMenuItem menuItem)
      {
        menuItem.Checked = AutomaticallyOpenFailures;
      }
    }

    private void OnToggleSuccessfulExecutions(object? sender, EventArgs e)
    {
      AutomaticallyOpenSuccesses = !AutomaticallyOpenSuccesses;

      if (sender is ToolStripMenuItem menuItem)
      {
        menuItem.Checked = AutomaticallyOpenSuccesses;
      }
    }

    private void OpenVisualizerWindow(IStepFunctionExecution execution)
    {
      var window = new VisualizerWindow(execution)
      {
        Title = $"{HostName} Visualizer"
      };

      window.Show();
    }

    private void OpenHistoryWindow()
    {
      var window = new HistoryWindow
      {
        Title = $"{HostName} History"
      };

      window.Show();
    }
  }
}