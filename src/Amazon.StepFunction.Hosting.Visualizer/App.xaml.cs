using System.Drawing;
using System.Windows;
using System.Windows.Forms;
using Application = System.Windows.Application;

namespace Amazon.StepFunction.Hosting.Visualizer
{
  public partial class VisualizerApplication
  {
    public new static VisualizerApplication Current => (VisualizerApplication) Application.Current;

    public VisualizerApplication()
    {
      InitializeComponent();
    }

    public StepFunctionHost? Host       { get; init; }
    public NotifyIcon?       NotifyIcon { get; private set; }

    protected override void OnStartup(StartupEventArgs e)
    {
      base.OnStartup(e);

      CreateNotifyIcon();

      if (Host != null)
      {
        Host.ExecutionStarted += OnExecutionStarted;
      }
    }

    protected override void OnExit(ExitEventArgs e)
    {
      base.OnExit(e);

      NotifyIcon?.Dispose();
    }

    private void OnExecutionStarted(IStepFunctionExecution execution)
    {
      var window = new VisualizerWindow(execution);

      window.Show();
    }

    private void CreateNotifyIcon()
    {
      NotifyIcon = new NotifyIcon
      {
        Icon             = SystemIcons.Application,
        Text             = "Step Function Visualizer",
        Visible          = true,
        ContextMenuStrip = new()
      };

      NotifyIcon.ContextMenuStrip.Items.Add(new ToolStripMenuItem("Exit", null, (_, _) => Current.Shutdown()));
    }
  }
}