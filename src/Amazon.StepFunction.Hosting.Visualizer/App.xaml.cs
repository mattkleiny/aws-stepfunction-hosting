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

      NotifyIcon.ContextMenuStrip.Items.Add(new ToolStripMenuItem("Open visualizer", null, (_, _) => ToggleWindowVisibility()));
      NotifyIcon.ContextMenuStrip.Items.Add(new ToolStripSeparator());
      NotifyIcon.ContextMenuStrip.Items.Add(new ToolStripMenuItem("Exit", null, (_, _) => Current.Shutdown()));

      NotifyIcon.DoubleClick += (_, _) => ToggleWindowVisibility();
    }

    private void ToggleWindowVisibility()
    {
      if (MainWindow != null)
      {
        if (MainWindow.IsVisible)
        {
          MainWindow.Hide();
        }
        else
        {
          MainWindow.Show();
        }
      }
    }

    protected override void OnExit(ExitEventArgs e)
    {
      base.OnExit(e);

      NotifyIcon?.Dispose();
    }
  }
}