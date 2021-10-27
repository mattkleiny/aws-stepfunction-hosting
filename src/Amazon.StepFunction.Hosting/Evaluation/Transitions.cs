using System;

namespace Amazon.StepFunction.Hosting.Evaluation
{
  /// <summary>Possible transitions in the state machine.</summary>
  internal abstract record Transition
  {
    public sealed record Next(string Name, StepFunctionData Data, string? TaskToken) : Transition;
    public sealed record Succeed(StepFunctionData Data) : Transition;
    public sealed record Fail(string? Cause, Exception? Exception) : Transition;

    private Transition()
    {
    }
  }

  internal static class Transitions
  {
    public static Transition Next(string target, StepFunctionData input, string? taskToken = default)
      => new Transition.Next(target, input, taskToken);

    public static Transition Succeed(StepFunctionData output)
      => new Transition.Succeed(output);

    public static Transition Fail(string? cause)
      => new Transition.Fail(cause, null);

    public static Transition Fail(Exception? exception = null)
      => new Transition.Fail(null, exception);
  }
}