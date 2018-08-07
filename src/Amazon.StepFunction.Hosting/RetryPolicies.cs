using System;
using System.Threading.Tasks;

namespace Amazon.StepFunction.Hosting
{
  /// <summary>A retry policy over some asynchronous task body.</summary>
  public delegate Task<object> RetryPolicy(Func<Task<object>> body);

  /// <summary>Static factory for <see cref="RetryPolicy"/>s.</summary>
  public static class RetryPolicies
  {
    /// <summary>A <see cref="RetryPolicy"/> that doesn't retry.</summary>
    public static readonly RetryPolicy Null = async body => await body();

    /// <summary>Builds a <see cref="RetryPolicy"/> that waits a linear amount of time between each retry.</summary>
    public static RetryPolicy Linear(int maxRetries, int delayMs = 100) => async body =>
    {
      var retryCount = 0;

      while (true)
      {
        try
        {
          return await body();
        }
        catch
        {
          if (retryCount++ >= maxRetries) throw;

          await Task.Delay(delayMs);
        }
      }
    };

    /// <summary>Builds a <see cref="RetryPolicy"/> that executes with exponential back-off.</summary>
    public static RetryPolicy Exponential(int maxRetries = 10, int delayMs = 100) => async body =>
    {
      var retryCount = 0;

      while (true)
      {
        try
        {
          return await body();
        }
        catch
        {
          if (retryCount++ >= maxRetries) throw;

          await Task.Delay((int) (delayMs * Math.Pow(2, retryCount)));
        }
      }
    };
  }
}