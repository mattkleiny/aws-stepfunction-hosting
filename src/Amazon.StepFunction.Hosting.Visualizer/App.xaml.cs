using System;
using System.Drawing;
using System.Reflection;
using System.Windows;
using System.Windows.Forms;

namespace Amazon.StepFunction.Hosting.Visualizer
{
  public partial class VisualizerApplication
  {
    private NotifyIcon? notifyIcon;

    public VisualizerApplication()
    {
      InitializeComponent();
    }

    public StepFunctionHost? Host { get; init; }

    public string TrayIconLabel          { get; init; } = "Step Function Visualizer";
    public bool   VisualizeAutomatically { get; set; }  = true;

    protected override void OnStartup(StartupEventArgs e)
    {
      base.OnStartup(e);

      CreateTrayIcon();

      if (Host != null)
      {
        Host.ExecutionStarted += OnExecutionStarted;
      }
    }

    protected override void OnExit(ExitEventArgs e)
    {
      notifyIcon?.Dispose();

      base.OnExit(e);
    }

    private void OnExecutionStarted(IStepFunctionExecution execution)
    {
      if (VisualizeAutomatically)
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
        Text             = TrayIconLabel,
        Visible          = true,
        ContextMenuStrip = menuStrip
      };

      menuStrip.Items.Add(new ToolStripMenuItem("Open history list", null, (_, _) => OpenHistoryWindow()));

      menuStrip.Items.Add(new ToolStripMenuItem("Visualize new executions", null, (_, _) => ToggleVisualizeAutomatically())
      {
        CheckState   = VisualizeAutomatically ? CheckState.Checked : CheckState.Unchecked,
        CheckOnClick = true
      });


      menuStrip.Items.Add(new ToolStripSeparator());
      menuStrip.Items.Add(new ToolStripMenuItem("Exit", null, (_, _) => Current.Shutdown()));

      notifyIcon.DoubleClick += OnTrayIconDoubleClick;
    }

    private void OnTrayIconDoubleClick(object? sender, EventArgs e)
    {
      OpenHistoryWindow();
    }

    private void ToggleVisualizeAutomatically()
    {
      VisualizeAutomatically = !VisualizeAutomatically;
    }

    private void OpenHistoryWindow()
    {
      var window = new HistoryWindow();

      window.Show();
    }

    private static void OpenVisualizerWindow(IStepFunctionExecution execution)
    {
      var window = new VisualizerWindow(execution);

      window.Show();
    }
  }
}