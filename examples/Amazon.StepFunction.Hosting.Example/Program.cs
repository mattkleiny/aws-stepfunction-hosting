using System;
using System.Threading.Tasks;
using System.Windows.Threading;
using Amazon.StepFunction.Hosting.Visualizer;

namespace Amazon.StepFunction.Hosting.Example
{
  public static class Program
  {
    [STAThread]
    public static int Main()
    {
      var application = new VisualizerApplication
      {
        HostName = "Example",
        Host = StepFunctionHost.CreateFromJson(
          specification: Resources.ExampleMachine,
          factory: StepHandlers.Factory
        )
      };

      // HACK: simulate an execution after opening the window
      Dispatcher.CurrentDispatcher.Invoke<Task>(async () =>
      {
        await Task.Yield();

        await application.Host.ExecuteAsync(new ExampleContext());
      });

      return application.Run();
    }
  }
}