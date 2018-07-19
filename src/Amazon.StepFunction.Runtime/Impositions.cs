using System;

namespace Amazon.StepFunction
{
  public sealed class Impositions
  {
    // TODO: add max wait time here, as well as other mechanisms

    /// <summary>The maximum time to wait in <see cref="Step.Wait"/> operations.</summary>
    public TimeSpan? MaxWaitTime { get; set; }
  }
}