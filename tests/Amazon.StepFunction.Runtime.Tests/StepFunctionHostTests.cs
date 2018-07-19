using System;
using System.Threading.Tasks;
using Amazon.StepFunction.Parsing;
using Xunit;

namespace Amazon.StepFunction.Runtime.Tests
{
  public class  StepFunctionHostTests
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
      
      Impositions.Current.WaitTimeOverride = TimeSpan.FromMilliseconds(10);

      var result = await host.ExecuteAsync();

      Assert.NotNull(result);
      Assert.True(result.IsSuccess);
      Assert.Equal("Hello, World!", result.Output);
    }

    [Fact]
    public async Task it_should_support_direct_machine_execution()
    {
      var definition = BuildParallelMachine();

      var host   = new StepFunctionHost(definition, StepHandlerFactories.Always("OK"));
      var result = await host.ExecuteAsync();

      Assert.True(result.IsSuccess);
    }

    private static StepFunctionHost BuildHost(string specification)
      => StepFunctionHost.FromJson(specification, StepHandlerFactories.Always("Hello, World!"));

    /// <summary>Builds a simple parallel <see cref="StepFunctionDefinition"/> for testing.</summary>
    private static StepFunctionDefinition BuildParallelMachine() => new StepFunctionDefinition
    {
      StartAt = "Branch",
      Steps = new StepDefinition[]
      {
        new StepDefinition.Parallel
        {
          Name = "Branch",
          End  = true,
          Branches = new[]
          {
            new StepFunctionDefinition
            {
              StartAt = "Wait",
              Steps = new StepDefinition[]
              {
                new StepDefinition.Wait
                {
                  Name    = "Wait",
                  Seconds = 1,
                  End     = true
                }
              }
            },
            new StepFunctionDefinition
            {
              StartAt = "SayHello",
              Steps = new StepDefinition[]
              {
                new StepDefinition.Invoke
                {
                  Name     = "SayHello",
                  Resource = "SayHello",
                  End      = true
                }
              }
            }
          }
        }
      }
    };
  }
}