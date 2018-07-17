using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Amazon.StepFunction.Definition;

namespace Amazon.StepFunction
{
  /// <summary>Defines a possible step in a <see cref="StepFunction"/>.</summary>
  internal abstract class Step
  {
    /// <summary>Creates the <see cref="Step"/> for the given <see cref="StepDefinition"/>.</summary>
    public static Step Create(StepDefinition definition, StepHandlerFactory factory)
    {
      Check.NotNull(definition, nameof(definition));
      Check.NotNull(factory,    nameof(factory));

      switch (definition.Type.ToLower())
      {
        case "pass":
          return new Pass
          {
            Name  = definition.Name,
            IsEnd = definition.End.GetValueOrDefault(false),
            Next  = definition.Next ?? definition.Default
          };

        case "task":
          return new Invoke(() => factory(definition))
          {
            Name    = definition.Name,
            IsEnd   = definition.End.GetValueOrDefault(false),
            Timeout = TimeSpan.FromSeconds(definition.TimeoutSeconds.GetValueOrDefault(300)),
            Next    = definition.Next ?? definition.Default
          };

        case "choice":
          return new Choice
          {
            Name    = definition.Name,
            Default = definition.Default
          };

        case "wait":
          return new Wait
          {
            Name     = definition.Name,
            IsEnd    = definition.End.GetValueOrDefault(false),
            Duration = TimeSpan.FromSeconds(definition.Seconds.GetValueOrDefault(5)),
            Next     = definition.Next ?? definition.Default
          };

        case "succeed":
          return new Succeed
          {
            Name = definition.Name
          };

        case "fail":
          return new Fail
          {
            Name = definition.Name
          };

        case "parallel":
          return new Parallel
          {
            Name  = definition.Name,
            IsEnd = definition.End.GetValueOrDefault(false)
          };

        default:
          throw new ArgumentException($"An unrecognized step type was requested: {definition.Type}");
      }
    }

    /// <summary>The name of this step.</summary>
    public string Name { get; set; }

    /// <summary>Executes the step asynchronously, observing any required step transition behaviour.</summary>
    public Task<IEnumerable<Transition>> ExecuteAsync(object input = null, CancellationToken cancellationToken = default)
    {
      return Task.FromResult(ExecuteInner(input, cancellationToken));
    }

    /// <summary>Executes task synchronously as a bridge until C# gets async iterators.</summary>
    private IEnumerable<Transition> ExecuteInner(object input, CancellationToken cancellationToken)
    {
      var context = new Context
      {
        Input             = input,
        CancellationToken = cancellationToken
      };

      try
      {
        return Execute(context);
      }
      catch (Exception exception)
      {
        return new[]
        {
          Transitions.Fail(exception)
        };
      }
    }

    /// <summary>Implements the actual execution operation for this step type.</summary>
    /// TODO: refactor this once async iterators become a thing in the next release of C#
    protected abstract IEnumerable<Transition> Execute(Context context);

    /// <summary>Encapsulates the working state for a <see cref="Step"/> execution.</summary>
    protected sealed class Context
    {
      public object            Input             { get; set; }
      public CancellationToken CancellationToken { get; set; }
    }

    /// <summary>A <see cref="Step"/> that passes it's input to output.</summary>
    public sealed class Pass : Step
    {
      public bool   IsEnd { get; set; }
      public string Next  { get; set; }

      protected override IEnumerable<Transition> Execute(Context context)
      {
        if (!IsEnd)
        {
          yield return Transitions.Next(Next, context.Input);
        }
        else
        {
          yield return Transitions.Succeed(context.Input);
        }
      }
    }

    /// <summary>A <see cref="Step"/> that invoke a handler for some given resource.</summary>
    public sealed class Invoke : Step
    {
      private readonly Func<StepHandler> factory;

      public Invoke(Func<StepHandler> factory)
      {
        this.factory = factory;
      }

      public bool        IsEnd       { get; set; }
      public TimeSpan    Timeout     { get; set; }
      public string      Next        { get; set; }
      public RetryPolicy RetryPolicy { get; set; } = RetryPolicies.NoOp;

      protected override IEnumerable<Transition> Execute(Context context)
      {
        var task = RetryPolicy(async () =>
        {
          using (var timeoutToken = new CancellationTokenSource(Timeout))
          using (var linkedTokens = CancellationTokenSource.CreateLinkedTokenSource(timeoutToken.Token, context.CancellationToken))
          {
            var handler = factory();

            return await handler(context.Input, linkedTokens.Token);
          }
        });

        var output = task.Result;

        if (!IsEnd)
        {
          yield return Transitions.Next(Next, output);
        }
        else
        {
          yield return Transitions.Succeed(output);
        }
      }
    }

    /// <summary>A <see cref="Step"/> that merely waits some given interval of time.</summary>
    public sealed class Wait : Step
    {
      public bool     IsEnd    { get; set; }
      public TimeSpan Duration { get; set; }
      public string   Next     { get; set; }

      protected override IEnumerable<Transition> Execute(Context context)
      {
        yield return Transitions.Wait(Duration);

        if (!IsEnd)
        {
          yield return Transitions.Next(Next, context.Input);
        }
        else
        {
          yield return Transitions.Succeed(context.Input);
        }
      }
    }

    /// <summary>A <see cref="Step"/> that makes a decision based on it's input.</summary>
    public sealed class Choice : Step
    {
      public string Default { get; set; }

      protected override IEnumerable<Transition> Execute(Context context)
      {
        throw new NotImplementedException();
      }
    }

    /// <summary>A <see cref="Step"/> that completes the execution with a success.</summary>
    public sealed class Succeed : Step
    {
      protected override IEnumerable<Transition> Execute(Context context)
      {
        yield return Transitions.Succeed(context.Input);
      }
    }

    /// <summary>A <see cref="Step"/> that completes the execution with a failure.</summary>
    public sealed class Fail : Step
    {
      protected override IEnumerable<Transition> Execute(Context context)
      {
        yield return Transitions.Fail();
      }
    }

    /// <summary>A <see cref="Step"/> that executes multiple other <see cref="Step"/>s in parallel.</summary>
    public sealed class Parallel : Step
    {
      public bool IsEnd { get; set; }

      protected override IEnumerable<Transition> Execute(Context context)
      {
        throw new NotImplementedException();
      }
    }

    /// <summary>This is a sealed ADT.</summary>
    private Step()
    {
    }
  }
}