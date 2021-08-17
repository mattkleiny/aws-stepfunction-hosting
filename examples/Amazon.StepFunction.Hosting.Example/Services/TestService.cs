using System.Threading.Tasks;

namespace Amazon.StepFunction.Hosting.Example.Services
{
  public interface ITestService
  {
    Task<string> FormatMessageAsync(string input);
  }

  public sealed class TestService : ITestService
  {
    public Task<string> FormatMessageAsync(string input)
    {
      return Task.FromResult($"Hello, {input}!");
    }
  }
}