using System.Threading.Tasks;
using Amazon.StepFunction.Hosting.Example.Services;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using NUnit.Framework;

namespace Amazon.StepFunction.Hosting.Example.Tests
{
  public class ExampleMachineTests : StepFunctionTestCase
  {
    [Test]
    public async Task it_should_execute_successfully()
    {
      var result = await Host.ExecuteAsync(input: new { Message = "world" });

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