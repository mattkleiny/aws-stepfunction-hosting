using System.Collections.Concurrent;

namespace Amazon.StepFunction.Hosting.Evaluation
{
  /// <summary>A token that can be used for concurrent scheduling.</summary>
  public readonly record struct Token(string Id);

  /// <summary>Allows waiting and signalling the completion of a task token.</summary>
  public interface ITokenSink
  {
    bool IsTaskCompleted(Token token);
    void NotifyTaskWaiting(Token token);
    void NotifyTaskCompleted(Token token);
  }

  /// <summary>A simple thread-safe <see cref="ITokenSink"/>.</summary>
  public sealed class ConcurrentTokenSink : ITokenSink
  {
    private readonly ConcurrentDictionary<Token, TokenStatus> statusByToken = new();

    public bool IsTaskCompleted(Token token)
    {
      if (statusByToken.TryGetValue(token, out var status))
      {
        return status == TokenStatus.Completed;
      }

      return false;
    }

    public void NotifyTaskWaiting(Token token)
    {
      statusByToken.TryAdd(token, TokenStatus.Waiting);
    }

    public void NotifyTaskCompleted(Token token)
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

    public bool IsTaskCompleted(Token token)
    {
      return true; // no waiting
    }

    public void NotifyTaskWaiting(Token token)
    {
      // no-op
    }

    public void NotifyTaskCompleted(Token token)
    {
      // no-op
    }
  }
}