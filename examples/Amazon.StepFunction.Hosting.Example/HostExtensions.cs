using Amazon.Lambda.Hosting;
using Microsoft.Extensions.Hosting;

namespace Amazon.StepFunction.Hosting.Example
{
  internal static class HostExtensions
  {
    /// <summary>Builds a <see cref="StepHandlerFactory"/> from the given <see cref="IHostBuilder"/>.</summary>
    public static StepHandlerFactory ToStepHandlerFactory(this IHostBuilder builder) => ToStepHandlerFactory(builder.Build());

    /// <summary>Builds a <see cref="StepHandlerFactory"/> from the given <see cref="IHost"/>.</summary>
    public static StepHandlerFactory ToStepHandlerFactory(this IHost host) => definition =>
    {
      var context = LambdaContext.ForFunction(definition.Resource);

      return async (data, cancellationToken) =>
      {
        // resolve the handler via our lambda runtime and use that for execution
        var (handler, metadata) = host.Services.ResolveLambdaHandlerWithMetadata(context);
        var input = data.Reinterpret(metadata.InputType);

        return await handler.ExecuteAsync(input, context, cancellationToken);
      };
    };
  }
}