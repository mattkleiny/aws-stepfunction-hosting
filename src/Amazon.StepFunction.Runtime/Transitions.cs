using System;

namespace Amazon.StepFunction
{
  /// <summary>Static factory for the <see cref="Transition"/>s.</summary>
  internal static class Transitions
  {
    public static Transition Next(string    target, object input) => new Transition.Next(target, input);
    public static Transition Wait(TimeSpan  duration)         => new Transition.Wait(duration);
    public static Transition Succeed(object output)           => new Transition.Succeed(output);
    public static Transition Fail(Exception exception = null) => new Transition.Fail(exception);
  }

  /// <summary>An ADT of possible transitions in the state machine.</summary>
  internal abstract class Transition
  {
    public sealed class Next : Transition
    {
      public Next(string name, object input)
      {
        Name = name;
        Input  = input;
      }

      public string Name { get; }
      public object Input  { get; }
    }

    public sealed class Wait : Transition
    {
      public Wait(TimeSpan duration)
      {
        Duration = duration;
      }

      public TimeSpan Duration { get; }
    }

    public sealed class Succeed : Transition
    {
      public Succeed(object output)
      {
        Output = output;
      }

      public object Output { get; }
    }

    public sealed class Fail : Transition
    {
      public Fail(Exception exception = null)
      {
        Exception = exception;
      }

      public Exception Exception { get; }
    }

    /// <summary>This is a sealed ADT.</summary>
    private Transition()
    {
    }
  }
}