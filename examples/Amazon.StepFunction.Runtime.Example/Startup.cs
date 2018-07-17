using System;
using System.IO;
using System.Threading.Tasks;
using Amazon.Lambda.Hosting;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Amazon.StepFunction.Runtime.Example
{
  public sealed class Startup
  {
    private static IHost Host { get; } = new HostBuilder()
      .UseStartup<Startup>()
      .Build();

    public static async Task Main(string[] args)
    {
      var stepFunction = StepFunctionHost.FromJson(
        specification: File.ReadAllText("example-machine.json"),
        factory: Host.ToStepHandlerFactory()
      );

      var result = await stepFunction.ExecuteAsync(input: "Matt");

      Console.WriteLine(result.Output);
    }

    [LambdaFunction("format-message")]
    public string Format(string input) => $"Hello, {input}!";

    [LambdaFunction("capitalize-message")]
    public string Capitalize(string input) => input.ToUpper();

    [UsedImplicitly]
    public void ConfigureServices(IServiceCollection services)
    {
      services.AddFunctionalHandlers<Startup>();
    }
  }
}