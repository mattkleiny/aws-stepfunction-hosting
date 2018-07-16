using System;

namespace Amazon.StepFunction
{
  /// <summary>Static factory for the <see cref="StepTransition"/>s.</summary>
  internal static class StepTransitions
  {
    public static StepTransition Next(string    target, object input) => new StepTransition.Next(target, input);
    public static StepTransition Wait(TimeSpan  duration)         => new StepTransition.Wait(duration);
    public static StepTransition Succeed(object output)           => new StepTransition.Succeed(output);
    public static StepTransition Fail(Exception exception = null) => new StepTransition.Fail(exception);
  }

  /// <summary>An ADT of possible transitions in the state machine.</summary>
  internal abstract class StepTransition
  {
    public sealed class Next : StepTransition
    {
      public Next(string name, object input)
      {
        Name = name;
        Input  = input;
      }

      public string Name { get; }
      public object Input  { get; }
    }

    public sealed class Wait : StepTransition
    {
      public Wait(TimeSpan duration)
      {
        Duration = duration;
      }

      public TimeSpan Duration { get; }
    }

    public sealed class Succeed : StepTransition
    {
      public Succeed(object output)
      {
        Output = output;
      }

      public object Output { get; }
    }

    public sealed class Fail : StepTransition
    {
      public Fail(Exception exception = null)
      {
        Exception = exception;
      }

      public Exception Exception { get; }
    }

    /// <summary>This is a sealed ADT.</summary>
    private StepTransition()
    {
    }
  }
}