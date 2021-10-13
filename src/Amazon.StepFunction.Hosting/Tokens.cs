using System;
using System.Collections.Concurrent;

namespace Amazon.StepFunction.Hosting
{
  /// <summary>Allows waiting and signalling the completion of task tokens.</summary>
  public interface ITokenSink
  {
    bool IsTaskCompleted(string token);
    void NotifyTaskWaiting(string token);
    void NotifyTaskCompleted(string token);
  }

  /// <summary>A simple thread-safe <see cref="ITokenSink"/>.</summary>
  internal sealed class ConcurrentTokenSink : ITokenSink
  {
    private readonly ConcurrentDictionary<string, TokenStatus> statusByToken = new(StringComparer.OrdinalIgnoreCase);

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
}