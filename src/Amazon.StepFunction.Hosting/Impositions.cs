using System;

namespace Amazon.StepFunction.Hosting
{
  /// <summary>Selects which step to use next, and provides the default which would have been used.</summary>
  public delegate string StepSelector(string next);

  /// <summary>A mechanism for imposing rules or restrictions upon the <see cref="StepFunctionHost"/> to aid in development or testing.</summary>
  public sealed class Impositions
  {
    /// <summary>The default <see cref="Impositions"/>s.</summary>
    public static readonly Impositions Default = new Impositions();

    /// <summary>The timeout period to use for task invocations.</summary>
    public TimeSpan? TimeoutOverride { get; set; } = null;

    /// <summary>The wait time to use in <see cref="Step.Wait"/> operations.</summary>
    public TimeSpan? WaitTimeOverride { get; set; } = null;

    /// <summary>A <see cref="StepSelector"/> delegate for overriding which step to use next.</summary>
    public StepSelector StepSelector { get; set; } = next => next;
  }
}