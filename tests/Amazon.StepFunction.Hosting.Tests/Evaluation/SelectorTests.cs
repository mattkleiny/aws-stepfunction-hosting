using Newtonsoft.Json;
using NUnit.Framework;

namespace Amazon.StepFunction.Hosting.Evaluation
{
  public class SelectorTests
  {
    [Test]
    public void it_should_deserialize_from_json()
    {
      var parameterSet = JsonConvert.DeserializeObject<Selector>(@"{
        'Comment': 'An example parameter substitution',
        'Details': {
          'FirstName.$': '$.FirstName',
          'LastName.$': '$.LastName'
        }
      }");

      var result = parameterSet.Expand(new StepFunctionData(new
      {
        FirstName = "Matt",
        LastName  = "Kleinschafer"
      }));

      Assert.IsNotNull(result.Query("$.Details.FirstName").Cast<string>());
      Assert.IsNotNull(result.Query("$.Details.LastName").Cast<string>());
    }
  }
}