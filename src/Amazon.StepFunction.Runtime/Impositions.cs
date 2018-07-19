using System;

namespace Amazon.StepFunction
{
  // TODO: add max wait time here, as well as other mechanisms
  // TODO: add a thread-local container, or some other mechanism

  public sealed class Impositions
  {
    public static Impositions Current { get; } = new Impositions();

    /// <summary>The maximum time to wait in <see cref="Step.Wait"/> operations.</summary>
    public TimeSpan? WaitTimeOverride { get; set; }
  }
}