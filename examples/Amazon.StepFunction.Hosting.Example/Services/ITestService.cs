using System.Threading.Tasks;

namespace Amazon.StepFunction.Hosting.Example.Services
{
  public interface ITestService
  {
    Task<string> FormatMessageAsync(string input);
  }
}