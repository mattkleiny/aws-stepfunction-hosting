using Xunit;

namespace Amazon.StepFunction.Hosting.Definition
{
  public class StepFunctionDefinitionTests
  {
    [Fact]
    public void it_should_parse_a_simple_machine()
    {
      var definition = StepFunctionDefinition.Parse(EmbeddedResources.SimpleSpecification);
      
      Assert.NotNull(definition);
      Assert.Equal(2, definition.Steps.Length);
    }

    [Fact]
    public void it_should_parse_a_complex_machine()
    {
      var definition = StepFunctionDefinition.Parse(EmbeddedResources.ComplexSpecification);
      
      Assert.NotNull(definition);
      Assert.Equal(4, definition.Steps.Length);
    }
  }
}