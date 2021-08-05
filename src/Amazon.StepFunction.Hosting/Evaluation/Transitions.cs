using System;

namespace Amazon.StepFunction.Hosting.Evaluation
{
  /// <summary>Static factory for the <see cref="Transition"/>s.</summary>
  internal static class Transitions
  {
    public static Transition Next(string target, object? input) => new Transition.Next(target, StepFunctionData.Wrap(input));
    public static Transition Succeed(object? output)            => new Transition.Succeed(StepFunctionData.Wrap(output));
    public static Transition Fail(Exception? exception = null)  => new Transition.Fail(exception);
  }

  /// <summary>Possible transitions in the state machine.</summary>
  internal abstract record Transition
  {
    public sealed record Next(string Name, StepFunctionData Data) : Transition;
    public sealed record Succeed(StepFunctionData Data) : Transition;
    public sealed record Fail(Exception? Exception) : Transition;

    /// <summary>This is a sealed hierarchy.</summary>
    private Transition()
    {
    }
  }
}