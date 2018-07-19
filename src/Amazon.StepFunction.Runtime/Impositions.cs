using System;
using System.Threading;
using System.Threading.Tasks;

namespace Amazon.StepFunction
{
  // TODO: add max wait time here, as well as other mechanisms
  // TODO: add a thread-local container, or some other mechanism

  public sealed class Impositions
  {
    private static readonly ThreadLocal<Impositions> ThreadLocal = new ThreadLocal<Impositions>();

    public static Impositions Of(Action<Impositions> configuration)
    {
      throw new NotImplementedException();
    }

    /// <summary>The currently active <see cref="Impositions"/>.</summary>
    public static Impositions Current => ThreadLocal.Value;

    /// <summary>The maximum time to wait in <see cref="Step.Wait"/> operations.</summary>
    public TimeSpan? WaitTimeOverride { get; set; }

    public void Impose(Action          during) => throw new NotImplementedException();
    public Task ImposeAsync(Func<Task> during) => throw new NotImplementedException();
  }
}