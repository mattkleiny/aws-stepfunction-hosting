﻿using System;
using Amazon.Lambda.Hosting;
using Amazon.StepFunction.Hosting.Example.Services;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Amazon.StepFunction.Hosting.Example
{
  public sealed class Startup
  {
    public static IHostBuilder HostBuilder { get; } = new HostBuilder().UseStartup<Startup>();

    [LambdaFunction("format-message")]
    public object Format(string input, ITestService service)
    {
      return service.FormatMessageAsync(input);
    }

    [LambdaFunction("capitalize-message")]
    public string Capitalize(string input)
    {
      return input.ToUpper();
    }

    [LambdaFunction("print-message")]
    public string Print(string input)
    {
      Console.WriteLine(input);

      return input;
    }

    [UsedImplicitly]
    public void ConfigureServices(IServiceCollection services)
    {
      services.AddSingleton<ITestService, TestService>();
      services.AddFunctionalHandlers<Startup>();
    }
  }
}