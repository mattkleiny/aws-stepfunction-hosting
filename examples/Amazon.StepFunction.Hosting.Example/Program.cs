﻿using System;
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
        HostName = "Example",
        Host = StepFunctionHost.FromJson(
          specification: Resources.ExampleMachine,
          factory: StepHandlers.Factory
        )
      };

      // HACK: simulate an execution after opening the window
      Dispatcher.CurrentDispatcher.Invoke<Task>(async () =>
      {
        for (int i = 0; i < 10; i++)
        {
          await Task.Delay(TimeSpan.FromSeconds(2));

          await application.Host.ExecuteAsync(input: new { Message = "world" });
        }
      });

      return application.Run();
    }
  }
}