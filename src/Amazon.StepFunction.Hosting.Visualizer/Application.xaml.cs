using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Linq.Expressions;
using System.Reflection;
using System.Windows;
using System.Windows.Forms;
using Amazon.StepFunction.Hosting.Visualizer.ViewModels;

namespace Amazon.StepFunction.Hosting.Visualizer
{
  /// <summary>A native Windows visualizer application for <see cref="StepFunctionHost"/>s.</summary>
  public partial class VisualizerApplication
  {
    private readonly HashSet<string>               seenExecutions   = new();
    private readonly Stack<IStepFunctionExecution> recentExecutions = new();
    private          NotifyIcon?                   notifyIcon;
    private          HistoryWindow?                historyWindow;

    public VisualizerApplication()
    {
      InitializeComponent();
    }

    public   StepFunctionHost?   Host     { get; init; }
    public   string              HostName { get; init; } = "Step Function";
    internal ApplicationSettings Settings { get; init; } = ApplicationSettings.LoadAsync().Result;

    /// <summary>Providers for extra details on the visualizer overview</summary>
    internal List<IStepDetailProvider> DetailProviders { get; } = new()
    {
      new InputOutputDetailProvider()
    };

    /// <summary>Adds a collector to the visualizer and <see cref="StepFunctionHost"/>.</summary>
    public void AddCollector<T>(T collector)
      where T : IStepFunctionDetailCollector, IStepDetailProvider
    {
      Host!.Collectors.Add(collector);
      DetailProviders.Add(collector);
    }

    public void OpenVisualizer(IStepFunctionExecution execution)
    {
      seenExecutions.Add(execution.ExecutionId);

      var window = new VisualizerWindow(this, execution)
      {
        Title = $"{HostName} Visualizer"
      };

      window.Show();
      window.Activate();
    }

    protected override void OnStartup(StartupEventArgs e)
    {
      base.OnStartup(e);

      historyWindow = new HistoryWindow(this)
      {
        Title = $"{HostName} History"
      };

      if (Host != null)
      {
        Host.ExecutionStarted += OnExecutionStarted;
        Host.ExecutionStopped += OnExecutionStopped;
      }

      CreateTrayIcon();
    }

    private void OnExecutionStarted(IStepFunctionExecution execution)
    {
      Dispatcher.Invoke(() =>
      {
        historyWindow?.TrackExecution(execution);

        if (Settings.AutomaticallyOpenExecutions && !HasSeenBefore(execution))
        {
          OpenVisualizer(execution);
        }

        if (Settings.NotifyOnExecutions)
        {
          recentExecutions.Push(execution);

          notifyIcon?.ShowBalloonTip(
            timeout: 3000,
            tipTitle: execution.ExecutionId,
            tipText: "The execution has started",
            tipIcon: ToolTipIcon.Info
          );
        }
      });
    }

    private void OnExecutionStopped(IStepFunctionExecution execution)
    {
      Dispatcher.Invoke(() =>
      {
        if (execution.Status == ExecutionStatus.Success)
        {
          if (Settings.AutomaticallyOpenSuccesses && !HasSeenBefore(execution))
          {
            OpenVisualizer(execution);
          }

          if (Settings.NotifyOnSuccesses)
          {
            recentExecutions.Push(execution);

            notifyIcon?.ShowBalloonTip(
              timeout: 3000,
              tipTitle: execution.ExecutionId,
              tipText: "The execution has succeeded",
              tipIcon: ToolTipIcon.Info
            );
          }
        }
        else if (execution.Status == ExecutionStatus.Failure)
        {
          if (Settings.AutomaticallyOpenFailures && !HasSeenBefore(execution))
          {
            OpenVisualizer(execution);
          }

          if (Settings.NotifyOnFailures)
          {
            recentExecutions.Push(execution);

            notifyIcon?.ShowBalloonTip(
              timeout: 3000,
              tipTitle: execution.ExecutionId,
              tipText: "The execution has failed",
              tipIcon: ToolTipIcon.Warning
            );
          }
        }
      });
    }

    protected override void OnExit(ExitEventArgs e)
    {
      notifyIcon?.Dispose();

      base.OnExit(e);
    }

    [SuppressMessage("ReSharper", "RedundantExplicitParamsArrayCreation")]
    private void CreateTrayIcon()
    {
      ContextMenuStrip menuStrip = new();

      notifyIcon = new NotifyIcon
      {
        Icon             = Icon.ExtractAssociatedIcon(Assembly.GetExecutingAssembly().Location),
        Text             = HostName,
        BalloonTipTitle  = HostName,
        Visible          = true,
        ContextMenuStrip = menuStrip
      };

      menuStrip.Items.Add(new ToolStripMenuItem("Open history list", null, OnTrayOpenHistoryList));

      menuStrip.Items.Add(new ToolStripSeparator());

      menuStrip.Items.Add(new ToolStripDropDownButton("Show visualizer for", null, new ToolStripItem[]
      {
        new ToolStripMenuItem("New executions", null, OnTrayToggleOpenNewExecutions)
        {
          Checked      = Settings.AutomaticallyOpenExecutions,
          CheckOnClick = true
        },
        new ToolStripMenuItem("Failed executions", null, OnTrayToggleOpenFailedExecutions)
        {
          Checked      = Settings.AutomaticallyOpenFailures,
          CheckOnClick = true
        },
        new ToolStripMenuItem("Successful executions", null, OnTrayToggleOpenSuccessfulExecutions)
        {
          Checked      = Settings.AutomaticallyOpenSuccesses,
          CheckOnClick = true
        }
      }));

      menuStrip.Items.Add(new ToolStripDropDownButton("Show notifications for", null, new ToolStripItem[]
      {
        new ToolStripMenuItem("New executions", null, OnTrayToggleNotifyNewExecutions)
        {
          Checked      = Settings.NotifyOnExecutions,
          CheckOnClick = true
        },
        new ToolStripMenuItem("Failed executions", null, OnTrayToggleNotifyFailedExecutions)
        {
          Checked      = Settings.NotifyOnFailures,
          CheckOnClick = true
        },
        new ToolStripMenuItem("Successful executions", null, OnTrayToggleNotifySuccessfulExecutions)
        {
          Checked      = Settings.NotifyOnSuccesses,
          CheckOnClick = true
        }
      }));

      menuStrip.Items.Add(new ToolStripSeparator());
      menuStrip.Items.Add(new ToolStripMenuItem("Exit", null, OnTrayExit));

      notifyIcon.DoubleClick       += OnTrayIconDoubleClicked;
      notifyIcon.BalloonTipClicked += OnBalloonTipClicked;
    }

    private bool HasSeenBefore(IStepFunctionExecution execution)
    {
      return seenExecutions.Contains(execution.ExecutionId);
    }

    private void ToggleHistoryWindow()
    {
      if (historyWindow != null)
      {
        if (historyWindow.IsVisible)
        {
          historyWindow.Hide();
        }
        else
        {
          historyWindow.Show();
          historyWindow.Activate();
        }
      }
    }

    private void OnBalloonTipClicked(object? sender, EventArgs e)
    {
      if (recentExecutions.TryPop(out var execution))
      {
        OpenVisualizer(execution);
      }
    }

    private void OnTrayExit(object? sender, EventArgs e) => Current.Shutdown();

    private void OnTrayOpenHistoryList(object? sender, EventArgs e)   => ToggleHistoryWindow();
    private void OnTrayIconDoubleClicked(object? sender, EventArgs e) => ToggleHistoryWindow();

    private void OnTrayToggleOpenNewExecutions(object? sender, EventArgs e)          => ToggleMenuItem(sender, _ => _.AutomaticallyOpenExecutions);
    private void OnTrayToggleOpenFailedExecutions(object? sender, EventArgs e)       => ToggleMenuItem(sender, _ => _.AutomaticallyOpenFailures);
    private void OnTrayToggleOpenSuccessfulExecutions(object? sender, EventArgs e)   => ToggleMenuItem(sender, _ => _.AutomaticallyOpenSuccesses);
    private void OnTrayToggleNotifyNewExecutions(object? sender, EventArgs e)        => ToggleMenuItem(sender, _ => _.NotifyOnExecutions);
    private void OnTrayToggleNotifyFailedExecutions(object? sender, EventArgs e)     => ToggleMenuItem(sender, _ => _.NotifyOnFailures);
    private void OnTrayToggleNotifySuccessfulExecutions(object? sender, EventArgs e) => ToggleMenuItem(sender, _ => _.NotifyOnSuccesses);

    // HACK: bit of an ugly hack to allow lots of read/writes against application settings
    //       unfortunately ref parameters don't work with properties
    private async void ToggleMenuItem(object? sender, Expression<Func<ApplicationSettings, bool>> setting)
    {
      if (setting.Body is not MemberExpression { Member: PropertyInfo property })
      {
        throw new Exception("An unexpected expression was encountered");
      }

      var value = (bool) property.GetValue(Settings)!;

      property.SetValue(Settings, !value);

      if (sender is ToolStripMenuItem menuItem)
      {
        menuItem.Checked = !value;
      }

      await Settings.SaveAsync();
    }
  }
}