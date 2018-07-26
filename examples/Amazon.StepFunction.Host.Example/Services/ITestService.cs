using System.Threading.Tasks;

namespace Amazon.StepFunction.Host.Example.Services
{
  public interface ITestService
  {
    Task<string> FormatMessageAsync(string input);
  }
}