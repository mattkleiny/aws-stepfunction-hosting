using System;

namespace Amazon.StepFunction.Hosting.Evaluation
{
  /// <summary>Static factory for the <see cref="Transition"/>s.</summary>
  internal static class Transitions
  {
    public static Transition Next(string target, object? input) => new Transition.Next(target, new StepFunctionData(input));
    public static Transition Succeed(object? output)            => new Transition.Succeed(new StepFunctionData(output));
    public static Transition Fail(string? cause)                => new Transition.Fail(cause, null);
    public static Transition Fail(Exception? exception = null)  => new Transition.Fail(null, exception);
    public static Transition WaitForToken(Token token)          => new Transition.WaitForToken(token);
  }

  /// <summary>Possible transitions in the state machine.</summary>
  internal abstract record Transition
  {
    public sealed record Next(string Name, StepFunctionData Data) : Transition;
    public sealed record Succeed(StepFunctionData Data) : Transition;
    public sealed record Fail(string? Cause, Exception? Exception) : Transition;
    public sealed record WaitForToken(Token Token) : Transition;

    /// <summary>This is a sealed hierarchy.</summary>
    private Transition()
    {
    }
  }
}