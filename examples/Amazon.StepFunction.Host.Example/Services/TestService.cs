using System.Threading.Tasks;

namespace Amazon.StepFunction.Host.Example.Services
{
  public sealed class TestService : ITestService
  {
    public Task<string> FormatMessageAsync(string input)
    {
      return Task.FromResult($"Hello, {input}!");
    }
  }
}