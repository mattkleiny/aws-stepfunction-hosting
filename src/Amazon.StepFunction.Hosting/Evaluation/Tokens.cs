using System.Collections.Concurrent;

namespace Amazon.StepFunction.Hosting.Evaluation
{
  /// <summary>Allows waiting and signalling the completion of a task token.</summary>
  public interface ITokenSink
  {
    bool IsTaskCompleted(string token);
    void NotifyTaskWaiting(string token);
    void NotifyTaskCompleted(string token);
  }

  /// <summary>A simple thread-safe <see cref="ITokenSink"/>.</summary>
  public sealed class ConcurrentTokenSink : ITokenSink
  {
    private readonly ConcurrentDictionary<string, TokenStatus> statusByToken = new();

    public bool IsTaskCompleted(string token)
    {
      if (statusByToken.TryGetValue(token, out var status))
      {
        return status == TokenStatus.Completed;
      }

      return false;
    }

    public void NotifyTaskWaiting(string token)
    {
      statusByToken.TryAdd(token, TokenStatus.Waiting);
    }

    public void NotifyTaskCompleted(string token)
    {
      statusByToken[token] = TokenStatus.Completed;
    }

    private enum TokenStatus
    {
      Waiting,
      Completed
    }
  }

  /// <summary>No-op/disabled task token support.</summary>
  public sealed class NullTokenSink : ITokenSink
  {
    public static NullTokenSink Instance { get; } = new();

    public bool IsTaskCompleted(string token)
    {
      return true; // no waiting
    }

    public void NotifyTaskWaiting(string token)
    {
      // no-op
    }

    public void NotifyTaskCompleted(string token)
    {
      // no-op
    }
  }
}