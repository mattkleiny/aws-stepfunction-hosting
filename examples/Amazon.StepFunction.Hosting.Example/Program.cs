using System;
using System.Threading.Tasks;
using System.Windows.Threading;
using Amazon.StepFunction.Hosting.Visualizer;

namespace Amazon.StepFunction.Hosting.Example
{
  public static class Program
  {
    [STAThread]
    public static int Main(string[] args)
    {
      var application = new VisualizerApplication
      {
        Host = StepFunctionHost.FromJson(
          specification: EmbeddedResources.ExampleMachine,
          factory: Startup.HostBuilder.ToStepHandlerFactory()
        )
      };

      // HACK: simulate an execution after opening the window
      Dispatcher.CurrentDispatcher.Invoke<Task>(async () =>
      {
        await Task.Delay(TimeSpan.FromSeconds(2));

        await application.Host.ExecuteAsync(input: new { Message = "matt" });
      });

      return application.Run();
    }
  }
}