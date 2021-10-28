using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Amazon.StepFunction.Hosting.Example.Tests
{
  public class StepFunctionTests : StepFunctionTestCase
  {
    [Test]
    public async Task it_should_execute_successfully()
    {
      var result = await Host.ExecuteAsync(new ExampleContext
      {
        PayeeIds = new List<Guid>
        {
          Guid.NewGuid(),
          Guid.NewGuid(),
          Guid.NewGuid(),
          Guid.NewGuid(),
        }
      });

      Assert.True(result.IsSuccess);
    }
  }
}