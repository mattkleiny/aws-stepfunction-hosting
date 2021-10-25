using System;
using System.Collections.Generic;
using Amazon.StepFunction.Hosting.Evaluation;

namespace Amazon.StepFunction.Hosting
{
  /// <summary>Selects which step to use next, and provides the default which would have been used.</summary>
  public delegate string StepSelector(string next);

  /// <summary>A mechanism for imposing rules or restrictions upon the <see cref="StepFunctionHost"/> to aid in development or testing.</summary>
  public sealed record Impositions
  {
    public static Impositions Default { get; } = new();

    /// <summary>A <see cref="StepSelector"/> delegate for overriding which step to use next.</summary>
    public StepSelector StepSelector { get; set; } = next => next;

    /// <summary>A delay period to introduce between each step transition, for debugging.</summary>
    public TimeSpan? StepTransitionDelay { get; set; } = null;

    /// <summary>The timeout period to use for task invocations.</summary>
    public TimeSpan? TaskTimeoutOverride { get; set; } = null;

    /// <summary>The wait time to use in <see cref="Step.WaitStep"/> operations.</summary>
    public TimeSpan? WaitTimeOverride { get; set; } = null;

    /// <summary>Should retry policies be honored?</summary>
    public bool EnableRetryPolicies { get; set; } = true;

    /// <summary>Should catch policies be honored?</summary>
    public bool EnableCatchPolicies { get; set; } = true;

    /// <summary>Should task tokens be honored?</summary>
    public bool EnableTaskTokens { get; set; } = true;
    
    /// <summary>A list of <see cref="IStepFunctionDetailCollector"/>s that should be run on every step execution.</summary>
    public List<IStepFunctionDetailCollector> Collectors { get; } = new();
  }
}