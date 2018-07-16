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
    public static StepHandlerFactory NoOp()                   => Always(null);
    public static StepHandlerFactory Always(object    result) => Adapt(() => result);
    public static StepHandlerFactory Adapt<T>(Func<T> body)   => definition => (_,     cancellationToken) => Task.FromResult<object>(body());
    public static StepHandlerFactory PassThrough()            => definition => (input, _) => Task.FromResult(input);

    /// <summary>Adapts a <see cref="IServiceProvider"/> that provisions <see cref="IStepHandler"/> instances for use in the runtime.</summary>
    public static StepHandlerFactory FromServiceProvider<T>(IServiceProvider provider)
      where T : class, IStepHandler => definition =>
    {
      var handler = (T) provider.GetService(typeof(T));

      return (input, cancellationToken) => handler.ExecuteAsync(input, cancellationToken);
    };
  }

  /// <summary>A handler for some step function step.</summary>
  public interface IStepHandler
  {
    /// <summary>Executes the step function step with the given input.</summary>
    Task<object> ExecuteAsync(object input, CancellationToken cancellationToken);
  }
}