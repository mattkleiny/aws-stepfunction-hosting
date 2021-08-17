using NUnit.Framework;

namespace Amazon.StepFunction.Hosting.Definition
{
  public class StepFunctionDefinitionTests
  {
    [Test]
    public void it_should_parse_a_simple_machine()
    {
      var definition = StepFunctionDefinition.Parse(EmbeddedResources.SimpleSpecification);

      Assert.NotNull(definition);
      Assert.AreEqual(2, definition.Steps.Length);
    }

    [Test]
    public void it_should_parse_a_complex_machine()
    {
      var definition = StepFunctionDefinition.Parse(EmbeddedResources.ComplexSpecification);

      Assert.NotNull(definition);
      Assert.AreEqual(4, definition.Steps.Length);
    }
  }
}