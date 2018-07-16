﻿using System;
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
    public static async Task Main(string[] args)
    {
      using (var host = new HostBuilder().UseStartup<Startup>().Build())
      {
        var specification = await File.ReadAllTextAsync("example-machine.json");
        var stepFunction = StepFunctionHost.FromJson(specification, definition =>
        {
          var context = new LocalLambdaContext(definition.Resource);
          var handler = host.Services.ResolveLambdaHandler(null, context);

          return (input, cancellationToken) => handler.ExecuteAsync(input, context, cancellationToken);
        });

        var result = await stepFunction.ExecuteAsync();
        
        Console.WriteLine(result.Output);
      }
    }

    [LambdaFunction("handler-1")]
    public string Handler1()
    {
      return "Hello, World!";
    }

    [UsedImplicitly]
    public void ConfigureServices(IServiceCollection services)
    {
      services.AddFunctionalHandlers<Startup>();
    }
  }
}