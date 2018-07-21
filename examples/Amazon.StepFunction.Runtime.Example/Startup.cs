using System;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Amazon.Lambda.Hosting;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Amazon.StepFunction.Runtime.Example
{
  public sealed class Startup
  {
    public static IHostBuilder HostBuilder => new HostBuilder()
      .UseStartup<Startup>();

    public static async Task<int> Main(string[] args)
      => await HostBuilder.RunLambdaConsoleAsync(args);

    [UsedImplicitly]
    public static async Task<object> ExecuteAsync(object input, ILambdaContext context)
      => await HostBuilder.RunLambdaAsync(input, context);

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
  }
}