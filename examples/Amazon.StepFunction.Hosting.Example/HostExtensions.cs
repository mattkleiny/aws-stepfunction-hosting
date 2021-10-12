using Amazon.Lambda.Hosting;
using Microsoft.Extensions.Hosting;

namespace Amazon.StepFunction.Hosting.Example
{
  internal static class HostExtensions
  {
    public static StepHandlerFactory ToStepHandlerFactory(this IHostBuilder builder)
    {
      return ToStepHandlerFactory(builder.Build());
    }

    public static StepHandlerFactory ToStepHandlerFactory(this IHost host)
    {
      return resource =>
      {
        var context = LambdaContext.ForARN(resource);

        return async (data, cancellationToken) =>
        {
          var (handler, metadata) = host.Services.ResolveLambdaHandlerWithMetadata(context);
          var input = data.Cast(metadata.InputType);

          var result = await handler.ExecuteAsync(input, context, cancellationToken);

          return new StepFunctionData(result);
        };
      };
    }
  }
}