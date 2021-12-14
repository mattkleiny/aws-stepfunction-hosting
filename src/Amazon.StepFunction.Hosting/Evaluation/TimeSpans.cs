using System;

namespace Amazon.StepFunction.Hosting.Evaluation
{
  /// <summary>Provides timeout values given <see cref="StepFunctionData"/> as input; used for retrieving timeout values from Step Function payloads.</summary>
  internal delegate TimeSpan TimeSpanProvider(StepFunctionData input);

  internal static class TimeSpanProviders
  {
    public static TimeSpanProvider FromSeconds(int seconds)     => _ => TimeSpan.FromSeconds(seconds);
    public static TimeSpanProvider FromSecondsPath(string path) => input => TimeSpan.FromSeconds(input.Query(path).Cast<int>());

    public static TimeSpanProvider FromSecondsParts(string? secondsPath, int seconds)
    {
      return secondsPath != null
        ? FromSecondsPath(secondsPath)
        : FromSeconds(seconds);
    }

    public static TimeSpanProvider FromTimestamp(DateTime timestamp) => _ => timestamp - DateTime.UtcNow;
    public static TimeSpanProvider FromTimestampPath(string path)    => input => input.Query(path).Cast<DateTime>() - DateTime.UtcNow;

    public static TimeSpanProvider FromTimestampParts(string? timestampPath, DateTime timestamp)
    {
      return timestampPath != null
        ? FromTimestampPath(timestampPath)
        : FromTimestamp(timestamp);
    }
  }
}