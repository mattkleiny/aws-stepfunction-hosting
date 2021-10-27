using System;

namespace Amazon.StepFunction.Hosting.Evaluation
{
  /// <summary>Possible transitions in the state machine.</summary>
  internal abstract record Transition
  {
    public sealed record Next(string NextState, StepFunctionData Data, string? TaskToken = default) : Transition;
    public sealed record Catch(string NextState, StepFunctionData Data, Exception? InnerException = default) : Transition;
    public sealed record Fail(string? Cause, Exception? Exception) : Transition;
    public sealed record Succeed(StepFunctionData Data) : Transition;

    private Transition()
    {
    }
  }
}