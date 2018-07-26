using System;
using System.Threading.Tasks;
using Amazon.StepFunction.Host.Example.Services;
using Microsoft.Extensions.Hosting;
using NSubstitute;
using Xunit;

namespace Amazon.StepFunction.Host.Example.Tests
{
  public class ExampleMachineTests
  {
    private static readonly IHostBuilder HostBuilderFixture = Startup.HostBuilder
      .ConfigureServices(services =>
      {
        services.ReplaceWithSubstitute<ITestService>(service =>
        {
          service.FormatMessage(Arg.Any<string>()).Returns(_ => $"Goodbye, {_.Arg<string>()}");
        });
      });

    [Fact]
    public async Task it_should_execute_successfully()
    {
      var host = StepFunctionHost.FromJson(
        specification: EmbeddedResources.ExampleMachine,
        factory: HostBuilderFixture.ToStepHandlerFactory()
      );

      var impositions = new Impositions
      {
        WaitTimeOverride = TimeSpan.FromMilliseconds(10)
      };

      var result = await host.ExecuteAsync(impositions, input: "world");

      Assert.True(result.IsSuccess);
    }
  }
}