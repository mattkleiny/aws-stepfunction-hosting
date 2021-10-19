using System;
using ServiceWire.NamedPipes;

namespace Amazon.StepFunction.Hosting.IPC
{
  /// <summary>Connects to a <see cref="TService"/> instance for communication via an IPC channel.</summary>
  public sealed class InterProcessClient<TService> : IDisposable
    where TService : class
  {
    private readonly NpClient<TService> client;

    public TService Service => client.Proxy;

    public InterProcessClient(string pipeName)
    {
      client = new NpClient<TService>(new NpEndPoint(pipeName));
    }

    public void Dispose()
    {
      client.Dispose();
    }
  }
}