using System;
using System.Threading.Tasks;

namespace Amazon.StepFunction.Hosting.Example
{
  public static class StepHandlers
  {
    public static StepHandlerFactory Factory { get; } = resource =>
    {
      static StepHandler CreateHandler(Action<ExampleContext> body) => (data, cancellationToken) =>
      {
        cancellationToken.ThrowIfCancellationRequested();

        var input = data.Cast<ExampleContext>();

        body(input!);

        return Task.FromResult(new StepFunctionData(input));
      };

      return resource.ToLower() switch
      {
        "pre-flight-checks"         => CreateHandler(PreFlightChecks),
        "wait-for-dependencies"     => CreateHandler(WaitForDependencies),
        "collect-reporting-details" => CreateHandler(CollectReportingDetails),
        "collect-payee-details"     => CreateHandler(CollectPayeeDetails),
        "validate-payload"          => CreateHandler(ValidatePayload),
        "dispatch-payload"          => CreateHandler(DispatchPayload),

        _ => throw new Exception($"An unrecognized resource was requested: {resource}")
      };
    };

    public static void PreFlightChecks(ExampleContext input)
    {
    }

    public static void WaitForDependencies(ExampleContext input)
    {
      input.IsWaiting = ++input.IterationCount < 3;
    }

    public static void CollectReportingDetails(ExampleContext input)
    {
    }

    public static void CollectPayeeDetails(ExampleContext input)
    {
    }

    public static void ValidatePayload(ExampleContext input)
    {
    }

    public static void DispatchPayload(ExampleContext input)
    {
    }
  }
}