using System;
using System.IO;
using System.Threading.Tasks;
using Amazon.Lambda.Hosting;
using Amazon.StepFunction.Definition;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Amazon.StepFunction.Runtime.Example
{
  public sealed class Startup
  {
    public static async Task Main(string[] args)
    {
      using (var host = new HostBuilder().UseStartup<Startup>().Build())
      {
        StepHandler HandlerFactory(StepDefinition definition)
        {
          var context = new LocalLambdaContext(definition.Resource);
          var handler = host.Services.ResolveLambdaHandler(null, context);

          return (input, cancellationToken) => handler.ExecuteAsync(input, context, cancellationToken);
        }

        var specification = await File.ReadAllTextAsync("example-machine.json");
        var stepFunction  = StepFunctionHost.FromJson(specification, HandlerFactory);

        var result = await stepFunction.ExecuteAsync(input: "Matt");

        Console.WriteLine(result.Output);
      }
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