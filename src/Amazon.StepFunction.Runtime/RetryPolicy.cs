using System;
using System.Threading.Tasks;

namespace Amazon.StepFunction
{
  /// <summary>A retry policy over some asynchronous task body.</summary>
  public delegate Task<object> RetryPolicy(Func<Task<object>> body);

  /// <summary>Static factory for <see cref="RetryPolicy"/>s.</summary>
  public static class RetryPolicies
  {
    /// <summary>A <see cref="RetryPolicy"/> that doesn't retry.</summary>
    public static readonly RetryPolicy NoOp = async body => await body();
  }
}