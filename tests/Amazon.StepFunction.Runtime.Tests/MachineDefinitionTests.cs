using Xunit;

namespace Amazon.StepFunction.Runtime.Tests
{
  public class MachineDefinitionTests
  {
    [Fact]
    public void it_should_parse_a_simple_machine()
    {
      Assert.NotNull(StepFunctionDefinition.Parse(EmbeddedResources.SimpleSpecification));
    }

    [Fact]
    public void it_should_parse_a_complex_machine()
    {
      Assert.NotNull(StepFunctionDefinition.Parse(EmbeddedResources.ComplexSpecification));
    }
  }
}