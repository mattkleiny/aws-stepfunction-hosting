using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NSubstitute;

namespace Amazon.StepFunction.Host.Example.Tests
{
  internal static class ServiceCollectionExtensions
  {
    /// <summary>Replaces the given <see cref="TService"/> with a NSubstitute.</summary>
    public static IServiceCollection ReplaceWithSubstitute<TService>(this IServiceCollection services)
      where TService : class
    {
      return services.ReplaceWithSubstitute<TService>(_ => { });
    }

    /// <summary>Replaces the given <see cref="TService"/> with a NSubstitute and configures it with the given delegate.</summary>
    public static IServiceCollection ReplaceWithSubstitute<TService>(this IServiceCollection services, Action<TService> configurer)
      where TService : class
    {
      services.RemoveAll(typeof(TService));
      services.AddScoped(_ =>
      {
        var substitute = Substitute.For<TService>();
        configurer(substitute);
        return substitute;
      });

      return services;
    }
  }
}