using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Threading;

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
          factory: StepHandlers.Factory,
          impositions: new()
          {
            StepTransitionDelay = TimeSpan.FromSeconds(0.5f)
          }
        )
      };

      // HACK: simulate an execution after opening the window
      Dispatcher.CurrentDispatcher.Invoke<Task>(async () =>
      {
        await Task.Yield();

        await application.Host.ExecuteAsync(new ExampleContext
        {
          PayeeIds = new List<Guid>
          {
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
          }
        });
      });

      return application.Run();
    }
  }
}