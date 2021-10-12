using System;

namespace Amazon.StepFunction.Hosting.Evaluation
{
  /// <summary>Provides timeout values given a step function input.</summary>
  internal delegate TimeSpan TimeSpanProvider(StepFunctionData input);

  /// <summary>Static factory for <see cref="TimeSpanProvider"/>s.</summary>
  internal static class TimeSpanProviders
  {
    public static TimeSpanProvider FromDurationParts(string? secondsPath, int seconds)
    {
      return secondsPath != null
        ? FromSecondsPath(secondsPath)
        : FromSeconds(seconds);
    }

    public static TimeSpanProvider FromTimestampParts(string? timestampPath, DateTime timestamp)
    {
      return timestampPath != null
        ? FromTimestampPath(timestampPath)
        : FromTimestamp(timestamp);
    }

    public static  TimeSpanProvider FromSeconds(int seconds)          => _ => TimeSpan.FromSeconds(seconds);
    public static  TimeSpanProvider FromSecondsPath(string path)      => input => TimeSpan.FromSeconds(input.Query(path).Cast<int>());
    private static TimeSpanProvider FromTimestamp(DateTime timestamp) => _ => DateTime.Now - timestamp;
    public static  TimeSpanProvider FromTimestampPath(string path)    => input => DateTime.Now - input.Query(path).Cast<DateTime>();
  }
}