﻿using Amazon.StepFunction.Definition;
using Xunit;

namespace Amazon.StepFunction.Runtime.Tests.Definition
{
  public class MachineDefinitionTests
  {
    [Fact]
    public void it_should_parse_a_simple_machine()
    {
      Assert.NotNull(MachineDefinition.Parse(json: EmbeddedResources.SimpleSpecification));
    }
  }
}