using System.Threading.Tasks;
using Amazon.StepFunction.Host.Example.Services;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Xunit;

namespace Amazon.StepFunction.Host.Example.Tests
{
  public class ExampleMachineTests : StepFunctionTestCase
  {
    [Fact]
    public async Task it_should_execute_successfully()
    {
      var result = await Host.ExecuteAsync(input: "world");

      Assert.True(result.IsSuccess);
    }

    protected override void ConfigureServices(IServiceCollection services)
    {
      base.ConfigureServices(services);

      services.ReplaceWithSubstitute<ITestService>(service =>
      {
        service.FormatMessageAsync(Arg.Any<string>()).Returns(_ => $"Goodbye, {_.Arg<string>()}");
      });
    }
  }
}