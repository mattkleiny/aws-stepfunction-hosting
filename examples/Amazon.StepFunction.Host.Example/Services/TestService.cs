namespace Amazon.StepFunction.Host.Example.Services
{
  public sealed class TestService : ITestService
  {
    public string FormatMessage(string input)
    {
      return $"Hello, {input}!";
    }
  }
}