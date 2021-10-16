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
      base.OnExit(e);

      notifyIcon?.Dispose();
    }

    private static void OnExecutionStarted(IStepFunctionExecution execution)
    {
      var window = new VisualizerWindow(execution);

      window.Show();
    }

    private void CreateTrayIcon()
    {
      notifyIcon = new NotifyIcon
      {
        Icon             = Icon.ExtractAssociatedIcon(Assembly.GetExecutingAssembly().Location),
        Text             = "Step Function Visualizer",
        Visible          = true,
        ContextMenuStrip = new()
      };

      notifyIcon.ContextMenuStrip.Items.Add(new ToolStripMenuItem("Exit", null, (_, _) => Current.Shutdown()));
    }
  }
}