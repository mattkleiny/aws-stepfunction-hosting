using System.Threading.Tasks;
using NUnit.Framework;

namespace Amazon.StepFunction.Hosting.Example.Tests
{
  public class StepFunctionTests : StepFunctionTestCase
  {
    [Test]
    public async Task it_should_execute_successfully()
    {
      var result = await Host.ExecuteAsync(input: new { Message = "world" });

      Assert.True(result.IsSuccess);
      Assert.AreEqual("[\"HELLO, WORLD!\"]", result.Output.ToString());
    }
  }
}