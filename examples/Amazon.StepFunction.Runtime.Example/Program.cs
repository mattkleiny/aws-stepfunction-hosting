using System.Threading.Tasks;

namespace Amazon.StepFunction.Runtime.Example
{
  public static class Program
  {
    public static async Task Main(string[] args)
    {
      var host = StepFunctionHost.FromTemplates(
        stateMachineTemplate: "serverless.template",
        cloudFormationTemplate: "cloudformation.template"
      );

      await host.ExecuteAsync();
    }
  }
}