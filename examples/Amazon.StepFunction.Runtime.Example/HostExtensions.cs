using Amazon.Lambda.Hosting;
using Microsoft.Extensions.Hosting;

namespace Amazon.StepFunction.Runtime.Example
{
  public static class HostExtensions
  {
    public static StepHandlerFactory ToStepHandlerFactory(this IHost host) => definition =>
    {
      var context = new LocalLambdaContext(definition.Resource);

      return (input, cancellationToken) =>
      {
        var handler = host.Services.ResolveLambdaHandler(input, context);

        return handler.ExecuteAsync(input, context, cancellationToken);
      };
    };
  }
}