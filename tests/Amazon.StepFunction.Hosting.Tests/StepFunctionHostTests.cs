using System;
using System.Threading.Tasks;
using Amazon.StepFunction.Hosting.Definition;
using Amazon.StepFunction.Hosting.Utilities;
using NUnit.Framework;

namespace Amazon.StepFunction.Hosting
{
  public class StepFunctionHostTests
  {
    [Test]
    public void it_should_parse_state_machine_from_simple_machine_template()
    {
      var host = StepFunctionHost.CreateFromJson(
        specification: Resources.SimpleSpecification,
        factory: StepHandlers.Always("Hello, World!")
      );

      Assert.NotNull(host);
      Assert.AreEqual(2, host.StepsByName.Count);
    }

    [Test]
    public void it_should_parse_state_machine_from_complex_machine_template()
    {
      var host = StepFunctionHost.CreateFromJson(
        specification: Resources.ComplexSpecification,
        factory: StepHandlers.Always("Hello, World!")
      );

      Assert.NotNull(host);
      Assert.AreEqual(4, host.StepsByName.Count);
    }

    [Test]
    public async Task it_should_support_basic_machine_execution()
    {
      var host = StepFunctionHost.CreateFromJson(
        specification: Resources.SimpleSpecification,
        factory: StepHandlers.Always("Hello, World!"),
        impositions: new Impositions
        {
          WaitTimeOverride = TimeSpan.FromMilliseconds(10)
        }
      );

      var result = await host.ExecuteAsync();

      Assert.NotNull(result);
      Assert.True(result.IsSuccess);
      Assert.AreEqual("Hello, World!", result.Output.Cast<string>());
    }

    [Test]
    public async Task it_should_support_direct_machine_execution()
    {
      var definition = BuildTestMachine();

      var host   = new StepFunctionHost(definition, StepHandlers.Always("OK"));
      var result = await host.ExecuteAsync();

      Assert.True(result.IsSuccess);
    }

    [Test]
    public void it_should_communicate_via_ipc()
    {
      var wasStarted  = false;
      var executionId = Guid.NewGuid().ToString();

      var factory = StepHandlers.Always("Hello, World!");
      var inner   = StepFunctionHost.CreateFromJson(Resources.ComplexSpecification, factory);

      inner.ExecutionStarted += execution =>
      {
        wasStarted = true;

        Assert.AreEqual(executionId, execution.ExecutionId);
      };

      using var host   = StepFunctionHost.CreateInterProcessHost(inner);
      using var client = StepFunctionHost.CreateInterProcessClient();

      client.Service.ExecuteAsync(executionId, "Hello, World!");
      client.Service.SetTaskStatus("test", TaskTokenStatus.Success);

      Assert.IsTrue(wasStarted);
    }

    [Test]
    public async Task it_should_build_a_simple_dot_graph_visualization_from_execution()
    {
      var host = StepFunctionHost.CreateFromJson(
        specification: Resources.ComplexSpecification,
        factory: StepHandlers.Always("Hello, World!")
      );

      var result = await host.ExecuteAsync(new { Message = "Hello, World!" });

      var dotGraph     = result.Execution.ToDotGraph();
      var dotGraphLink = result.Execution.ToDotGraphLink();

      Assert.IsNotEmpty(dotGraph);
      Assert.IsNotEmpty(dotGraphLink);
    }

    private static StepFunctionDefinition BuildTestMachine() => new()
    {
      StartAt = "Branch",
      Steps = new StepDefinition[]
      {
        new StepDefinition.ParallelDefinition
        {
          Name = "Branch",
          End  = true,
          Branches =
          {
            new StepFunctionDefinition
            {
              StartAt = "Wait",
              Steps = new StepDefinition[]
              {
                new StepDefinition.WaitDefinition
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
                new StepDefinition.TaskDefinition
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