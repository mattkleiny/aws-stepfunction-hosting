using System;
using System.Collections.Concurrent;

namespace Amazon.StepFunction.Hosting
{
  /// <summary>Possible states for a task token.</summary>
  public enum TaskTokenStatus
  {
    Waiting,
    Success,
    Failure
  }

  /// <summary>Allows waiting and signalling the completion of task tokens.</summary>
  public interface ITaskTokenSink
  {
    TaskTokenStatus GetTokenStatus(string token, TaskTokenStatus defaultStatus = TaskTokenStatus.Waiting);
    void            SetTokenStatus(string token, TaskTokenStatus status);
  }

  internal sealed class ConcurrentTaskTokenSink : ITaskTokenSink
  {
    private readonly ConcurrentDictionary<string, TaskTokenStatus> statusByToken = new(StringComparer.OrdinalIgnoreCase);

    public TaskTokenStatus GetTokenStatus(string token, TaskTokenStatus defaultStatus = TaskTokenStatus.Waiting)
    {
      if (statusByToken.TryGetValue(token, out var status))
      {
        return status;
      }

      return defaultStatus;
    }

    public void SetTokenStatus(string token, TaskTokenStatus status)
    {
      statusByToken[token] = status;
    }
  }
}