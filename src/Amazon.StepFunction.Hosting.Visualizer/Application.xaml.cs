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
    private readonly HashSet<string> detectedExecutions = new();
    private          NotifyIcon?     notifyIcon;
    private          HistoryWindow?  historyWindow;

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

      menuStrip.Items.Add(new ToolStripMenuItem("Open history list", null, OnTrayOpenHistoryList));

      var dropDownItems = new ToolStripItem[]
      {
        new ToolStripMenuItem("New executions", null, OnTrayToggleNewExecutions)
        {
          Checked      = AutomaticallyOpenExecutions,
          CheckOnClick = true
        },
        new ToolStripMenuItem("Failed executions", null, OnTrayToggleFailedExecutions)
        {
          Checked      = AutomaticallyOpenFailures,
          CheckOnClick = true
        },
        new ToolStripMenuItem("Successful executions", null, OnTrayToggleSuccessfulExecutions)
        {
          Checked      = AutomaticallyOpenSuccesses,
          CheckOnClick = true
        }
      };

      menuStrip.Items.Add(new ToolStripDropDownButton("Open visualizer automatically", null, dropDownItems));

      menuStrip.Items.Add(new ToolStripSeparator());
      menuStrip.Items.Add(new ToolStripMenuItem("Exit", null, OnTrayExit));

      notifyIcon.DoubleClick += OnTrayIconDoubleClick;
    }

    private void OnTrayOpenHistoryList(object? sender, EventArgs e)
    {
      ToggleHistoryWindow();
    }

    private void OnTrayIconDoubleClick(object? sender, EventArgs e)
    {
      ToggleHistoryWindow();
    }

    private void OnTrayExit(object? sender, EventArgs e)
    {
      Current.Shutdown();
    }

    private void OnTrayToggleNewExecutions(object? sender, EventArgs e)
    {
      AutomaticallyOpenExecutions = !AutomaticallyOpenExecutions;

      if (sender is ToolStripMenuItem menuItem)
      {
        menuItem.Checked = AutomaticallyOpenExecutions;
      }
    }

    private void OnTrayToggleFailedExecutions(object? sender, EventArgs e)
    {
      AutomaticallyOpenFailures = !AutomaticallyOpenFailures;

      if (sender is ToolStripMenuItem menuItem)
      {
        menuItem.Checked = AutomaticallyOpenFailures;
      }
    }

    private void OnTrayToggleSuccessfulExecutions(object? sender, EventArgs e)
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

    private void ToggleHistoryWindow()
    {
      historyWindow ??= new HistoryWindow
      {
        Title = $"{HostName} History"
      };

      if (historyWindow.IsVisible)
      {
        historyWindow.Hide();
      }
      else
      {
        historyWindow.Show();
        
      }
    }
  }
}