using System;
using System.Threading;
using System.Threading.Tasks;
using Amazon.StepFunction.Hosting.Definition;

namespace Amazon.StepFunction.Hosting
{
  public delegate StepHandler StepHandlerFactory(StepDefinition.TaskDefinition definition);

  /// <summary>A handler for a single step in a <see cref="StepFunctionHost"/>.</summary>
  public delegate Task<StepFunctionData> StepHandler(StepFunctionData data, CancellationToken cancellationToken = default);

  public static class StepHandlers
  {
    /// <summary>A <see cref="StepHandler"/> that passes it's input to output.</summary>
    public static StepHandler Identity { get; } = (data, _) => Task.FromResult(data);

    /// <summary>A <see cref="StepHandler"/> that always returns the given value.</summary>
    public static StepHandlerFactory Always(object result) => Adapt(() => result);

    /// <summary>A <see cref="StepHandler"/> that executes the given function and yield it's result.</summary>
    public static StepHandlerFactory Adapt<T>(Func<T> body) => _ => (_, _) => Task.FromResult(StepFunctionData.Wrap(body()));

    /// <summary>A <see cref="StepHandler"/> that executes the given function and yield it's result.</summary>
    public static StepHandlerFactory Adapt<T>(Func<Task<T>> body) => _ => async (_, _) => StepFunctionData.Wrap(await body());
  }
}