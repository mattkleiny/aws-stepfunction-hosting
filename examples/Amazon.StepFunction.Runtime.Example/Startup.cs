using System;
using System.IO;
using System.Threading.Tasks;
using Amazon.Lambda.Hosting;
using Amazon.StepFunction.Parsing;
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
      var impositions = new Impositions
      {
        WaitTimeOverride = TimeSpan.FromMilliseconds(10),
        StepSelector     = next => next == "Print" ? "Delay" : next
      };

      await impositions.ImposeAsync(async () =>
      {
        var machine = StepFunctionHost.FromJson(
          specification: File.ReadAllText("example-machine.json"),
          handlerFactory: BuildHandler
        );

        var result = await machine.ExecuteAsync(input: "Hello, World!");

        Console.WriteLine(result.Output);
      });
    }

    [LambdaFunction("format-message")]
    public string Format(string input) => $"Hello, {input}!";

    [LambdaFunction("capitalize-message")]
    public string Capitalize(string input) => input.ToUpper();

    [LambdaFunction("print-message")]
    public void Print(string input) => Console.WriteLine(input);

    [UsedImplicitly]
    public void ConfigureServices(IServiceCollection services)
    {
      services.AddFunctionalHandlers<Startup>();
    }

    /// <summary>Builds a <see cref="StepHandler"/> for the given <see cref="definition"/>.</summary>
    private static StepHandler BuildHandler(StepDefinition.Invoke definition)
    {
      var context = new LocalLambdaContext(definition.Resource);

      return (input, cancellationToken) =>
      {
        // resolve the handler via our lambda runtime and use that for execution
        var handler = Host.Services.ResolveLambdaHandler(input, context);

        return handler.ExecuteAsync(input, context, cancellationToken);
      };
    }
  }
}