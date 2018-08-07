using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Amazon.StepFunction.Hosting.Example.Tests
{
  /// <summary>Base class for any test that exercises the step function.</summary>
  public abstract class StepFunctionTestCase
  {
    protected StepFunctionTestCase()
    {
      Host = StepFunctionHost.FromJson(
        specification: EmbeddedResources.ExampleMachine,
        factory: Startup.HostBuilder.ConfigureServices(ConfigureServices).ToStepHandlerFactory(),
        impositions: new Impositions { WaitTimeOverride = TimeSpan.FromMilliseconds(0) }
      );
    }

    /// <summary>The configured <see cref="StepFunctionHost"/> for testing.</summary>
    protected StepFunctionHost Host { get; }

    /// <summary>Configures the services available ot the <see cref="StepFunctionHost"/> execution.</summary>
    protected virtual void ConfigureServices(IServiceCollection services)
    {
    }
  }
}