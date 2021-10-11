namespace Amazon.StepFunction.Hosting.Visualizer
{
  public partial class VisualizerApplication
  {
    public VisualizerApplication()
    {
      InitializeComponent();
    }

    public static void Start(StepFunctionHost host)
    {
      // TODO: implement me

      var application = new VisualizerApplication();

      application.Run();
    }
  }
}