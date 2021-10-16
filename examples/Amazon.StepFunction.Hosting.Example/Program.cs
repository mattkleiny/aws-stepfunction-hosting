using System;
using Amazon.StepFunction.Hosting.Visualizer;

namespace Amazon.StepFunction.Hosting.Example
{
  public static class Program
  {
    [STAThread]
    public static void Main(string[] args)
    {
      var application = new VisualizerApplication
      {
        Host = StepFunctionHost.FromJson(
          specification: EmbeddedResources.ExampleMachine,
          factory: Startup.HostBuilder.ToStepHandlerFactory(),
          impositions: new Impositions
          {
            WaitTimeOverride = TimeSpan.FromMilliseconds(10)
          }
        )
      };

      application.Run();
    }
  }
}