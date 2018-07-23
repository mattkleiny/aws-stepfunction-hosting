using System;
using System.Threading.Tasks;
using Xunit;

namespace Amazon.StepFunction.Host.Example.Tests
{
  public class ExampleMachineTests
  {
    [Fact]
    public async Task it_should_execute_successfully()
    {
      var host = StepFunctionHost.FromJson(
        specification: EmbeddedResources.ExampleMachine,
        factory: Startup.HostBuilder.ToStepHandlerFactory()
      );

      var result = await host.ExecuteAsync(new Impositions
      {
        WaitTimeOverride = TimeSpan.FromMilliseconds(10)
      });
  
      Assert.True(result.IsSuccess);
    }
  }
}