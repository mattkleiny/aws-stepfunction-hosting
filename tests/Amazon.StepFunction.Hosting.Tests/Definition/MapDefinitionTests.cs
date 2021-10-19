using Newtonsoft.Json;
using NUnit.Framework;

namespace Amazon.StepFunction.Hosting.Definition
{
  public class MapDefinitionTests
  {
    [Test]
    public void it_should_parse_a_simple_map_expression()
    {
      var definition = JsonConvert.DeserializeObject<StepDefinition.MapDefinition>(@"{
        'InputPath': '$.detail',
        'ItemsPath': '$.shipped',
        'MaxConcurrency': 0,
        'Iterator': {
          'StartAt': 'Validate',
          'States': {
            'Validate': {
              'Type': 'Task',
              'Resource': 'arn:aws:lambda:us-east-1:123456789012:function:ship-val',
              'End': true
            }
          }
        },
        'ResultPath': '$.detail.shipped',
        'End': true
      }");

      Assert.IsNotNull(definition);
      Assert.IsNotNull(definition.Iterator);
    }
  }
}