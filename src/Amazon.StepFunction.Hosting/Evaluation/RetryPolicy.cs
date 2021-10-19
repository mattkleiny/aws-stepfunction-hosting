using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Amazon.StepFunction.Hosting.Evaluation
{
  /// <summary>Permits constructing retry policies from pieces.</summary>
  internal abstract record RetryPolicy
  {
    public static RetryPolicy Null { get; } = new NullRetryPolicy();

    public static RetryPolicy Linear(ErrorSet errorSet, int maxRetries, TimeSpan delay)
      => new DelegatePolicy(errorSet, maxRetries, retryCount => retryCount * delay);

    public static RetryPolicy Exponential(ErrorSet errorSet, int maxRetries, TimeSpan delay, float backoffRate)
      => new DelegatePolicy(errorSet, maxRetries, retryCount => delay * backoffRate * Math.Pow(2, retryCount));

    public static RetryPolicy Composite(IEnumerable<RetryPolicy> policies)
      => new CompositePolicy(policies.ToArray());

    public async Task<StepFunctionData> EvaluateAsync(bool isEnabled, Func<Task<StepFunctionData>> body)
    {
      var retryCount = 0;

      while (true)
      {
        try
        {
          return await body();
        }
        catch (Exception exception) when (isEnabled && CanRetry(retryCount, exception))
        {
          await Task.Delay(GetWaitDelay(retryCount, exception));
        }

        retryCount++;
      }
    }

    protected abstract bool     CanRetry(int retryCount, Exception exception);
    protected abstract TimeSpan GetWaitDelay(int retryCount, Exception exception);

    /// <summary>A no-op <see cref="RetryPolicy"/>.</summary>
    private sealed record NullRetryPolicy : RetryPolicy
    {
      protected override bool CanRetry(int retryCount, Exception exception)
      {
        return false;
      }

      protected override TimeSpan GetWaitDelay(int retryCount, Exception exception)
      {
        return TimeSpan.Zero;
      }
    }

    /// <summary>A <see cref="RetryPolicy"/> that computes a delay based on a delegate.</summary>
    private sealed record DelegatePolicy(ErrorSet ErrorSet, int MaxRetries, Func<int, TimeSpan> Delay) : RetryPolicy
    {
      protected override bool CanRetry(int retryCount, Exception exception)
      {
        return retryCount < MaxRetries && ErrorSet.Contains(exception);
      }

      protected override TimeSpan GetWaitDelay(int retryCount, Exception exceptions)
      {
        return Delay(retryCount);
      }
    }

    /// <summary>A <see cref="RetryPolicy"/> that composes multiple other <see cref="RetryPolicy"/>s.</summary>
    private sealed record CompositePolicy(params RetryPolicy[] Policies) : RetryPolicy
    {
      protected override bool CanRetry(int retryCount, Exception exception)
      {
        foreach (var policy in Policies)
        {
          if (policy.CanRetry(retryCount, exception))
          {
            return true;
          }
        }

        return false;
      }

      protected override TimeSpan GetWaitDelay(int retryCount, Exception exception)
      {
        foreach (var policy in Policies)
        {
          if (policy.CanRetry(retryCount, exception))
          {
            return policy.GetWaitDelay(retryCount, exception);
          }
        }

        return TimeSpan.Zero;
      }
    }
  }
}