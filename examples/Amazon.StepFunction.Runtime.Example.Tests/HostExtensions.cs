using Amazon.Lambda.Hosting;
using Microsoft.Extensions.Hosting;

namespace Amazon.StepFunction.Runtime.Example.Tests
{
  internal static class HostExtensions
  {
    /// <summary>Builds a <see cref="StepHandlerFactory"/> from the given <see cref="IHostBuilder"/>.</summary>
    public static StepHandlerFactory ToStepHandlerFactory(this IHostBuilder builder) => ToStepHandlerFactory(builder.Build());

    /// <summary>Builds a <see cref="StepHandlerFactory"/> from the given <see cref="IHost"/>.</summary>
    public static StepHandlerFactory ToStepHandlerFactory(this IHost host) => definition =>
    {
      var context = new LocalLambdaContext(definition.Resource);

      return (input, cancellationToken) =>
      {
        // resolve the handler via our lambda runtime and use that for execution
        var handler = host.Services.ResolveLambdaHandler(input, context);

        return handler.ExecuteAsync(input, context, cancellationToken);
      };
    };
  }
}