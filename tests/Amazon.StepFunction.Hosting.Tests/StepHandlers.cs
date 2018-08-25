using System;
using System.Threading.Tasks;

namespace Amazon.StepFunction.Hosting
{
  /// <summary>Composable <see cref="StepHandlerFactory"/>s to aid in testing.</summary>
  public static class StepHandlers
  {
    public static StepHandlerFactory Always(object result)  => Adapt(() => result);
    public static StepHandlerFactory Adapt<T>(Func<T> body) => definition => (_, cancellationToken) => Task.FromResult<object>(body());
  }
}