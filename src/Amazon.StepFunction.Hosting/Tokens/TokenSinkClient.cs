using System;
using ServiceWire.NamedPipes;

namespace Amazon.StepFunction.Hosting.Tokens
{
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
}