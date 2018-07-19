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
        handlerFactory: definition =>
        {
          var context = new LocalLambdaContext(definition.Resource);

          return (input, cancellationToken) =>
          {
            var handler = Host.Services.ResolveLambdaHandler(input, context);

            return handler.ExecuteAsync(input, context, cancellationToken);
          };
        }
      );

      await stepFunction.ExecuteAsync();
    }

    [LambdaFunction("format-message")]
    public string Format(string input) => $"Hello, {input}!";

    [LambdaFunction("capitalize-message")]
    public string Capitalize(string input) => input.ToUpper();

    [LambdaFunction("print-message")]
    public void Print() => Console.WriteLine("Hello, World!");

    [UsedImplicitly]
    public void ConfigureServices(IServiceCollection services)
    {
      services.AddFunctionalHandlers<Startup>();
    }
  }
}