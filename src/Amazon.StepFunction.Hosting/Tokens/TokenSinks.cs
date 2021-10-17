using System;
using System.Collections.Concurrent;
using ServiceWire;
using ServiceWire.NamedPipes;

namespace Amazon.StepFunction.Hosting.Tokens
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

  /// <summary>An IPC client for the <see cref="ITokenSink"/>, allowing cross-process task notification</summary>
  public sealed class TokenSinkClient : IDisposable
  {
    private readonly NpClient<ITokenSink> client;

    public TokenSinkClient(string pipeName = "stp-task-notifier")
    {
      client = new NpClient<ITokenSink>(new NpEndPoint(pipeName));
    }

    public ITokenSink Sink => client.Proxy;

    public void Dispose()
    {
      client.Dispose();
    }
  }

  /// <summary>An IPC host for the <see cref="ITokenSink"/>, allowing cross-process task notification</summary>
  internal sealed class TokenSinkHost : IDisposable
  {
    private readonly NpHost host;

    public TokenSinkHost(ITokenSink sink, string pipeName = "stp-task-notifier")
    {
      var logger = new Logger(logLevel: LogLevel.Debug);

      Sink = sink;
      host = new NpHost(pipeName, logger);

      host.AddService(sink);
      host.Open();
    }

    public ITokenSink Sink { get; }

    public void Dispose()
    {
      host.Dispose();
    }
  }
}