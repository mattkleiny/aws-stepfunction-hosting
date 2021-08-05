﻿using System;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Amazon.Lambda.Hosting;
using Amazon.StepFunction.Hosting.Example.Services;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Amazon.StepFunction.Hosting.Example
{
  public sealed class Startup
  {
    public static IHostBuilder HostBuilder => new HostBuilder()
      .UseStartup<Startup>();

    public static async Task Main(string[] args)
    {
      var host = StepFunctionHost.FromJson(
        specification: EmbeddedResources.ExampleMachine,
        factory: HostBuilder.ToStepHandlerFactory(),
        impositions: new Impositions
        {
          WaitTimeOverride = TimeSpan.FromMilliseconds(10)
        }
      );

      await host.ExecuteAsync(input: new { Message = "matt" });
    }

    [UsedImplicitly]
    public static async Task<object> ExecuteAsync(object input, ILambdaContext context)
    {
      return await HostBuilder.RunLambdaAsync(input, context);
    }

    [LambdaFunction("format-message")]
    public Task<string> Format(string input, ITestService service)
    {
      return service.FormatMessageAsync(input);
    }

    [LambdaFunction("capitalize-message")]
    public string Capitalize(string input)
    {
      return input.ToUpper();
    }

    [LambdaFunction("print-message")]
    public void Print(string input)
    {
      Console.WriteLine(input);
    }

    [UsedImplicitly]
    public void ConfigureServices(IServiceCollection services)
    {
      services.AddSingleton<ITestService, TestService>();
      services.AddFunctionalHandlers<Startup>();
    }
  }
}