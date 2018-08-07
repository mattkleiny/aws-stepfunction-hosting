using System;
using System.Threading.Tasks;
using Amazon.StepFunction.Hosting.Definition;
using Xunit;

namespace Amazon.StepFunction.Hosting
{
  public class StepFunctionHostTests
  {
    [Fact]
    public void it_should_parse_state_machine_from_simple_machine_template()
    {
      var host = StepFunctionHost.FromJson(
        EmbeddedResources.SimpleSpecification,
        StepHandlerFactories.Always("Hello, World!")
      );

      Assert.NotNull(host);
      Assert.Equal(2, host.Steps.Count);
    }

    [Fact]
    public void it_should_parse_state_machine_from_complex_machine_template()
    {
      var host = StepFunctionHost.FromJson(
        EmbeddedResources.ComplexSpecification,
        StepHandlerFactories.Always("Hello, World!")
      );

      Assert.NotNull(host);
      Assert.Equal(4, host.Steps.Count);
    }

    [Fact]
    public async Task it_should_support_basic_machine_execution()
    {
      var host = StepFunctionHost.FromJson(
        EmbeddedResources.SimpleSpecification,
        StepHandlerFactories.Always("Hello, World!"),
        new Impositions
        {
          WaitTimeOverride = TimeSpan.FromMilliseconds(10)
        }
      );

      var result = await host.ExecuteAsync();

      Assert.NotNull(result);
      Assert.True(result.IsSuccess);
      Assert.Equal("Hello, World!", result.Output);
    }


    [Fact]
    public async Task it_should_support_complex_machine_execution()
    {
      var host = StepFunctionHost.FromJson(
        EmbeddedResources.ComplexSpecification,
        definition =>
        {
          switch (definition.Resource)
          {
            case "format-message":     return (input, token) => Task.FromResult<object>($"Hello, {input}!");
            case "capitalize-message": return (input, token) => Task.FromResult<object>(input.ToString().ToUpper());
            case "print-message":
              return (input, token) =>
              {
                Console.WriteLine(input);
                return Task.FromResult(input);
              };

            default:
              throw new InvalidOperationException();
          }
        },
        new Impositions
        {
          WaitTimeOverride = TimeSpan.FromMilliseconds(10)
        }
      );

      var result = await host.ExecuteAsync(input: "world");

      Assert.NotNull(result);
      Assert.True(result.IsSuccess);
      Assert.Equal("HELLO, WORLD!", result.Output);
    }

    [Fact]
    public async Task it_should_support_direct_machine_execution()
    {
      var definition = BuildParallelMachine();

      var host   = new StepFunctionHost(definition, StepHandlerFactories.Always("OK"));
      var result = await host.ExecuteAsync();

      Assert.True(result.IsSuccess);
    }

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