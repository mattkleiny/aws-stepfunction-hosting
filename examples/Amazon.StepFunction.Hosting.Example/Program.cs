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
        Host = StepFunctionHost.FromJson(
          specification: Resources.ExampleMachine,
          factory: StepHandlers.Factory
        )
      };

      // HACK: simulate an execution after opening the window
      Dispatcher.CurrentDispatcher.Invoke<Task>(async () =>
      {
        await Task.Delay(TimeSpan.FromSeconds(1));

        for (var i = 0; i < 10; i++)
        {
          if (Random.Shared.Next(100) <= 50)
          {
            await application.Host.ExecuteAsync(input: new { Message = "world" });
          }
          else
          {
            await application.Host.ExecuteAsync(input: new { Message = "bad" });
          }
        }
      });

      return application.Run();
    }
  }
}