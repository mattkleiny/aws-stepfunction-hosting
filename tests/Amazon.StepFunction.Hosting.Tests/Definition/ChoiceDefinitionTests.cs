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
        'Choices': [
          {
            'Not': {
              'Variable': 'Test',
              'StringEquals': 'Hello, World!'
            },
            'Next': 'Evaluated True',
          }
        ],
        'Default': 'Evaluated False'
      }");

      Assert.IsNotNull(definition);
      Assert.IsNotNull(definition.Choices.Length > 0);
    }

    [Test]
    public void it_should_parse_an_and_expression()
    {
      var definition = JsonConvert.DeserializeObject<StepDefinition.ChoiceDefinition>(@"{
        'Choices': [
          {
            'And': [
              {
                'Variable': 'Test',
                'StringEquals': 'Hello, World!'
              },
              {
                'Variable': 'Test',
                'StringEquals': 'Goodbye, World!'
              },
            ],
            'Next': 'Evaluated True',
          }
        ],
        'Default': 'Evaluated False'
      }");

      Assert.IsNotNull(definition);
      Assert.IsNotNull(definition.Choices.Length > 0);
    }

    [Test]
    public void it_should_parse_an_or_expression()
    {
      var definition = JsonConvert.DeserializeObject<StepDefinition.ChoiceDefinition>(@"{
        'Choices': [
          {
            'Or': [
              {
                'Variable': 'Test',
                'StringEquals': 'Hello, World!'
              },
              {
                'Variable': 'Test',
                'StringEquals': 'Goodbye, World!'
              },
            ],
            'Next': 'Evaluated True',
          }
        ],
        'Default': 'Evaluated False'
      }");

      Assert.IsNotNull(definition);
      Assert.IsNotNull(definition.Choices.Length > 0);
    }

    [TestCase("BooleanEquals", "true")]
    [TestCase("StringEquals", "Hello, World!")]
    public void it_should_parse_basic_boolean_and_string_expressions(string expression, object comparand)
    {
      const string template = @"{
        'Choices': [
          {
            'Variable': 'Test',
            '$Expression': '$Value',
            'Next': 'Evaluated True',
          }
        ],
        'Default': 'Evaluated False'
      }";

      var raw = template
        .Replace("$Expression", expression)
        .Replace("$Value", comparand.ToString());

      var definition = JsonConvert.DeserializeObject<StepDefinition.ChoiceDefinition>(raw);

      Assert.IsNotNull(definition);
      Assert.IsNotNull(definition.Choices.Length > 0);
    }

    [TestCase("NumericEquals", 42)]
    [TestCase("NumericLessThan", 42)]
    [TestCase("NumericLessThanEquals", 42)]
    [TestCase("NumericGreaterThan", 42)]
    [TestCase("NumericGreaterThanEquals", 42)]
    public void it_should_parse_basic_numeric_expressions(string expression, object comparand)
    {
      const string template = @"{
        'Choices': [
          {
            'Variable': 'Test',
            '$Expression': $Value,
            'Next': 'Evaluated True',
          }
        ],
        'Default': 'Evaluated False'
      }";

      var raw = template
        .Replace("$Expression", expression)
        .Replace("$Value", comparand.ToString());

      var definition = JsonConvert.DeserializeObject<StepDefinition.ChoiceDefinition>(raw);

      Assert.IsNotNull(definition);
      Assert.IsNotNull(definition.Choices.Length > 0);
    }
  }
}