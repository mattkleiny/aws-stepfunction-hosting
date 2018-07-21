using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Amazon.StepFunction
{
  /// <summary>A mechanism for imposing rules or restrictions upon the <see cref="StepFunctionHost"/> to aid in development or testing.</summary>
  public sealed class Impositions
  {
    private static readonly Impositions                     Default          = new Impositions();
    private static readonly ThreadLocal<Stack<Impositions>> StackThreadLocal = new ThreadLocal<Stack<Impositions>>(() => new Stack<Impositions>());

    /// <summary>The stack of <see cref="Impositions"/> being managed.</summary>
    private static Stack<Impositions> Stack => StackThreadLocal.Value;

    /// <summary>The currently lactive <see cref="Impositions"/>.</summary>
    public static Impositions Current
    {
      get
      {
        if (Stack.Count > 0)
        {
          return Stack.Peek();
        }

        return Default;
      }
    }

    /// <summary>The maximum time to wait in <see cref="Step.Wait"/> operations.</summary>
    public TimeSpan? WaitTimeOverride { get; set; } = null;

    /// <summary>A <see cref="StepSelector"/> delegate for overriding which step to use next.</summary>
    public StepSelector StepSelector { get; set; } = desiredNext => desiredNext;

    /// <summary>Imposes these impositions upon the given method.</summary>
    public void Impose(Action during)
    {
      Check.NotNull(during, nameof(during));

      Stack.Push(this);
      try
      {
        during();
      }
      finally
      {
        Stack.Pop();
      }
    }

    /// <summary>Imposes these impositions upon the given asynchronous method.</summary>
    public async Task ImposeAsync(Func<Task> during)
    {
      Check.NotNull(during, nameof(during));

      Stack.Push(this);
      try
      {
        await during();
      }
      finally
      {
        Stack.Pop();
      }
    }
  }

  /// <summary>Selects which step to use next, and provides the default which would have been used next.</summary>
  public delegate string StepSelector(string desiredNext);
}