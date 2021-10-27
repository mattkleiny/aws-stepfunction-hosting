using System;

namespace Amazon.StepFunction.Hosting.Evaluation
{
  /// <summary>Possible transitions in the state machine.</summary>
  internal abstract record Transition
  {
    public sealed record Next(string Name, StepFunctionData Data, string? TaskToken = default, Exception? InnerException = default) : Transition;
    public sealed record Succeed(StepFunctionData Data) : Transition;
    public sealed record Fail(string? Cause, Exception? Exception) : Transition;

    private Transition()
    {
    }
  }
}