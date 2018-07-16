using System;
using System.Threading;
using System.Threading.Tasks;
using Amazon.StepFunction.Definition;

namespace Amazon.StepFunction
{
  /// <summary>Defines a handler for some step function <see cref="Step.Invoke"/> execution.</summary>
  public delegate Task<object> StepHandler(object input, CancellationToken cancellationToken);

  /// <summary>Defines a factory for <see cref="StepHandler"/>s.</summary>
  public delegate StepHandler StepHandlerFactory(StepDefinition definition);

  /// <summary>Composable <see cref="StepHandlerFactory"/>s to aid in common scenarios.</summary>
  public static class StepHandlerFactories
  {
    public static readonly StepHandlerFactory NoOp = Always(null);

    public static StepHandlerFactory Always(object    result) => Adapt(() => result);
    public static StepHandlerFactory Adapt<T>(Func<T> body)   => definition => (_, cancellationToken) => Task.FromResult<object>(body());
  }
}