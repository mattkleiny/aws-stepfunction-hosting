using System;
using System.Collections.Immutable;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Amazon.StepFunction.Hosting.Evaluation
{
  public class ParallelStepTests
  {
    private static readonly Impositions Impositions = new()
    {
      WaitTimeOverride = TimeSpan.Zero
    };

    [Test]
    public async Task it_should_execute_and_combine_results()
    {
      var step = new Step.ParallelStep
      {
        IsEnd = true,
        Branches = new[]
        {
          StepFunctionHost.CreateFromJson(Resources.SimpleSpecification, StepHandlers.Always("LEFT"), Impositions),
          StepFunctionHost.CreateFromJson(Resources.ComplexSpecification, StepHandlers.Always("RIGHT"), Impositions),
        }.ToImmutableList()
      };

      var result = await step.ExecuteAsync(Impositions, new StepFunctionData("Hello, World!"));

      // N.B: the order is only consistent because we do no asynchronous work in our StepHandlers, but this is good enough for testing
      Assert.IsTrue(result.Transition is Transition.Succeed { Data: var data });
      Assert.IsNotEmpty(result.ChildHistory);
      Assert.AreEqual("[\"LEFT\",\"RIGHT\"]", data.ToString()); 
    }
  }
}