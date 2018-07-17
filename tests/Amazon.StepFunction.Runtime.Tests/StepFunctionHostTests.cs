using System;
using System.Threading.Tasks;
using Xunit;

namespace Amazon.StepFunction.Runtime.Tests
{
  public class StepFunctionHostTests
  {
    [Fact]
    public void it_should_parse_state_machine_from_simple_machine_template()
    {
      var host = BuildHost(EmbeddedResources.SimpleSpecification);

      Assert.NotNull(host);
      Assert.Equal(2, host.Steps.Count);
    }

    [Fact]
    public void it_should_parse_state_machine_from_complex_machine_template()
    {
      var host = BuildHost(EmbeddedResources.ComplexSpecification);

      Assert.NotNull(host);
      Assert.Equal(10, host.Steps.Count);
    }

    [Fact]
    public async Task it_should_support_basic_machine_execution()
    {
      var host = BuildHost(EmbeddedResources.SimpleSpecification);
      host.MaxWaitDuration = TimeSpan.FromMilliseconds(10);

      var result = await host.ExecuteAsync();

      Assert.NotNull(result);
      Assert.True(result.IsSuccess);
      Assert.Equal("Hello, World!", result.Output);
    }

    private static StepFunctionHost BuildHost(string specification) => StepFunctionHost.FromJson(
      specification: specification,
      handlerFactory: StepHandlerFactories.Always("Hello, World!")
    );
  }
}