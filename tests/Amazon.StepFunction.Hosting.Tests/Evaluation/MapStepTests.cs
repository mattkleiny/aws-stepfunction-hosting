using System;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace Amazon.StepFunction.Hosting.Evaluation
{
  public class MapStepTests
  {
    private static readonly Impositions Impositions = new()
    {
      WaitTimeOverride = TimeSpan.Zero
    };

    [Test]
    public async Task it_should_map_and_combine_results()
    {
      var iterator = StepFunctionHost.CreateFromJson(
        specification: Resources.SimpleSpecification,
        factory: StepHandlers.Adapt(input => input.Cast<int>() * 2),
        impositions: Impositions
      );

      var step = new Step.MapStep(iterator)
      {
        IsEnd = true
      };

      var result = await step.ExecuteAsync(Impositions, new StepFunctionData(new[] { 1, 2, 3, 4, 5 }));

      Assert.IsTrue(result.Transition is Transition.Succeed { Data: var data });
      Assert.IsNotEmpty(result.ChildHistory);

      var array = data.Cast<JArray>()!.Select(_ => _.Value<int>()).ToList();

      Assert.Contains(2, array);
      Assert.Contains(4, array);
      Assert.Contains(6, array);
      Assert.Contains(8, array);
      Assert.Contains(10, array);
    }
  }
}