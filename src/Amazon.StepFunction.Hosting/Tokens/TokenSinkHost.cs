using System;
using ServiceWire;
using ServiceWire.NamedPipes;

namespace Amazon.StepFunction.Hosting.Tokens
{
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