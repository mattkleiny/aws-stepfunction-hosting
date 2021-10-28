using System;
using System.Threading;
using System.Threading.Tasks;

namespace Amazon.StepFunction.Hosting
{
  /// <summary>A handler for a single step in a <see cref="StepFunctionHost"/>; this can be for any resource that a Step Function may define.</summary>
  public delegate Task<StepFunctionData> StepHandler(StepFunctionData input, CancellationToken cancellationToken = default);

  public delegate StepHandler StepHandlerFactory(string stepName, string resource);

  public static class StepHandlers
  {
    public static StepHandlerFactory Always(object result) => Adapt(() => result);

    public static StepHandlerFactory Adapt(Func<StepFunctionData, StepFunctionData> body)
      => (_, _) => (input, _) => Task.FromResult(body(input));

    public static StepHandlerFactory Adapt<T>(Func<StepFunctionData, T> body)
      => (_, _) => (input, _) => Task.FromResult(new StepFunctionData(body(input)));

    public static StepHandlerFactory Adapt<T>(Func<T> body)
      => (_, _) => (_, _) => Task.FromResult(new StepFunctionData(body()));

    public static StepHandlerFactory Adapt<T>(Func<Task<T>> body)
      => (_, _) => async (_, _) => new StepFunctionData(await body());
  }
}