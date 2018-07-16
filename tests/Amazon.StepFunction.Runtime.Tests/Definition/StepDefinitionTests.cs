using Amazon.StepFunction.Definition;
using Xunit;

namespace Amazon.StepFunction.Runtime.Tests.Definition
{
  public class StepDefinitionTests
  {
    [Fact]
    public void it_should_create_pass_steps() => AssertCreatesType<Step.Pass>("Pass");

    [Fact]
    public void it_should_create_invoke_steps() => AssertCreatesType<Step.Invoke>("Task");

    [Fact]
    public void it_should_create_wait_steps() => AssertCreatesType<Step.Wait>("Wait");

    [Fact]
    public void it_should_create_choice_steps() => AssertCreatesType<Step.Choice>("Choice");

    [Fact]
    public void it_should_create_succeed_steps() => AssertCreatesType<Step.Succeed>("Succeed");

    [Fact]
    public void it_should_create_fail_steps() => AssertCreatesType<Step.Fail>("Fail");

    [Fact]
    public void it_should_create_parallel_invoke_steps() => AssertCreatesType<Step.Parallel>("Parallel");

    private static void AssertCreatesType<TStep>(string type)
      where TStep : Step
    {
      Assert.IsType<TStep>(new StepDefinition {Type = type}.Create(factory: StepHandlerFactories.NoOp()));
    }
  }
}