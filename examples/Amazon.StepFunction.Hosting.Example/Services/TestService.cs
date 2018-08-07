using System.Threading.Tasks;

namespace Amazon.StepFunction.Hosting.Example.Services
{
  public sealed class TestService : ITestService
  {
    public Task<string> FormatMessageAsync(string input)
    {
      return Task.FromResult($"Hello, {input}!");
    }
  }
}