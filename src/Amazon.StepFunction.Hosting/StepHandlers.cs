using System;
using System.Threading;
using System.Threading.Tasks;

namespace Amazon.StepFunction.Hosting
{
  /// <summary>A handler for a single step in a <see cref="StepFunctionHost"/>.</summary>
  public delegate Task<StepFunctionData> StepHandler(StepFunctionData input, CancellationToken cancellationToken = default);

  /// <summary>Constructs <see cref="StepHandler"/>s for later evaluation.</summary>
  public delegate StepHandler StepHandlerFactory(string resource);

  /// <summary>Commonly used <see cref="StepHandler"/>s and <see cref="StepHandlerFactory"/>s.</summary>
  public static class StepHandlers
  {
    public static StepHandlerFactory Always(object result) => Adapt(() => result);

    public static StepHandlerFactory Adapt<T>(Func<T> body)       => _ => (_, _) => Task.FromResult(new StepFunctionData(body()));
    public static StepHandlerFactory Adapt<T>(Func<Task<T>> body) => _ => async (_, _) => new StepFunctionData(await body());
  }
}