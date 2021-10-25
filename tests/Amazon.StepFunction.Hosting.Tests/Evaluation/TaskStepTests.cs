using System;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Amazon.StepFunction.Hosting.Evaluation
{
  public class TaskStepTests
  {
    [Test]
    public async Task it_should_execute_a_step_and_honor_input_and_output_paths()
    {
      // arbitrarily nested messages to test the input/output paths
      static StepHandler StepHandler(string resource) => (input, _) =>
      {
        var output = new StepFunctionData(new
        {
          Result = input.Cast<string>()!.ToUpper()
        });

        return Task.FromResult(output);
      };

      var step = new Step.TaskStep("Test", StepHandler)
      {
        InputPath  = "$.Message",
        ResultPath = "$.Result",
        IsEnd      = true
      };

      var input = new StepFunctionData(new
      {
        Message = "Hello, World!"
      });

      var result = await step.ExecuteAsync(input);

      Assert.IsTrue(result is { Transition: Transition.Succeed { Data: var output } });
      Assert.AreEqual("HELLO, WORLD!", output.Cast<string>()!);
    }

    [Test]
    public async Task it_should_execute_a_step_and_honor_retry_policies()
    {
      var executionCount = 0;

      StepHandler StepHandler(string resource) => (input, _) =>
      {
        if (executionCount++ < 2)
        {
          throw new Exception("Just pretending!");
        }

        return Task.FromResult(input);
      };

      var step = new Step.TaskStep("Test", StepHandler)
      {
        IsEnd       = true,
        RetryPolicy = RetryPolicy.Linear(ErrorSet.FromTypes(typeof(Exception)), 3, TimeSpan.Zero)
      };

      var result = await step.ExecuteAsync();

      Assert.IsTrue(result is { Transition: Transition.Succeed });
    }

    [Test]
    public async Task it_should_execute_a_step_and_honor_catch_policies()
    {
      StepHandler StepHandler(string resource) => (_, _) => throw new Exception("Just pretending!");

      var step = new Step.TaskStep("Test", StepHandler)
      {
        IsEnd       = true,
        CatchPolicy = CatchPolicy.Standard(ErrorSet.FromTypes(typeof(Exception)), "$.Message", "Error Handler")
      };

      var result = await step.ExecuteAsync();

      Assert.IsTrue(result is { Transition: Transition.Next { Name: "Error Handler" } });
    }
  }
}