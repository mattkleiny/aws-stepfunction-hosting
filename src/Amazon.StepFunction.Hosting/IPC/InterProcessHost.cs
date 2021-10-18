using System;
using ServiceWire;
using ServiceWire.NamedPipes;

namespace Amazon.StepFunction.Hosting.IPC
{
  /// <summary>Hosts a <see cref="TService"/> for communication via an IPC channel.</summary>
  public sealed class InterProcessHost<TService> : IDisposable
    where TService : class
  {
    private readonly NpHost host;

    public TService Service { get; }

    public InterProcessHost(TService service, string pipeName)
    {
      var logger = new Logger(logLevel: LogLevel.Debug);

      Service = service;

      host = new NpHost(pipeName, logger);

      host.AddService(service);
      host.Open();
    }

    public void Dispose()
    {
      host.Dispose();
    }
  }
}