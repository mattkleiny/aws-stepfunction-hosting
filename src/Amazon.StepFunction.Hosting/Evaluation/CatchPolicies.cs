using System;
using System.Threading.Tasks;

namespace Amazon.StepFunction.Hosting.Evaluation
{
  /// <summary>A catch policy over some asynchronous task body.</summary>
  internal delegate Task<object?> CatchPolicy(Func<Task<object?>> body);

  internal static class CatchPolicies
  {
    /// <summary>A <see cref="RetryPolicy"/> that doesn't catch anything.</summary>
    public static readonly CatchPolicy None = async body => await body();
  }
}