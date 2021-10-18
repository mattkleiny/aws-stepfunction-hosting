using System;
using System.Collections.Concurrent;

namespace Amazon.StepFunction.Hosting
{
  /// <summary>Possible states for a task token.</summary>
  public enum TokenStatus
  {
    Waiting,
    Success,
    Failure
  }

  /// <summary>Allows waiting and signalling the completion of task tokens.</summary>
  public interface ITokenSink
  {
    TokenStatus GetTokenStatus(string token, TokenStatus defaultStatus = TokenStatus.Waiting);
    void        SetTokenStatus(string token, TokenStatus status);
  }

  /// <summary>A simple thread-safe <see cref="ITokenSink"/>.</summary>
  internal sealed class ConcurrentTokenSink : ITokenSink
  {
    private readonly ConcurrentDictionary<string, TokenStatus> statusByToken = new(StringComparer.OrdinalIgnoreCase);

    public TokenStatus GetTokenStatus(string token, TokenStatus defaultStatus = TokenStatus.Waiting)
    {
      if (statusByToken.TryGetValue(token, out var status))
      {
        return status;
      }

      return defaultStatus;
    }

    public void SetTokenStatus(string token, TokenStatus status)
    {
      statusByToken[token] = status;
    }
  }
}