using Newtonsoft.Json;
using NUnit.Framework;

namespace Amazon.StepFunction.Hosting.Definition
{
  public class ChoiceDefinitionTests
  {
    [Test]
    public void it_should_parse_a_not_expression()
    {
      var definition = JsonConvert.DeserializeObject<StepDefinition.ChoiceDefinition>(@"{
        ""Choices"": [
          {
            ""Not"": {
              ""Variable"": ""Test"",
              ""StringEquals"": ""Hello, World!""
            },
            ""Next"": ""Evaluated True"",
          }
        ],
        ""Default"": ""Evaluated False""
      }");

      Assert.IsNotNull(definition);
    }
  }
}