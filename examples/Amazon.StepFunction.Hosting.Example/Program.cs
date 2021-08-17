using System;
using Amazon.StepFunction.Hosting;
using Amazon.StepFunction.Hosting.Example;

var host = StepFunctionHost.FromJson(
  specification: EmbeddedResources.ExampleMachine,
  factory: Startup.HostBuilder.ToStepHandlerFactory(),
  impositions: new Impositions
  {
    WaitTimeOverride = TimeSpan.FromMilliseconds(10)
  }
);

await host.ExecuteAsync(input: new { Message = "matt" });