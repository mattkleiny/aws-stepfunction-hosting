using System.Threading.Tasks;
using NUnit.Framework;

namespace Amazon.StepFunction.Hosting.Example.Tests
{
  public class ExampleMachineTests : StepFunctionTestCase
  {
    [Test]
    public async Task it_should_execute_successfully()
    {
      var result = await Host.ExecuteAsync(input: new { Message = "matt" });

      Assert.True(result.IsSuccess);
    }
  }
}