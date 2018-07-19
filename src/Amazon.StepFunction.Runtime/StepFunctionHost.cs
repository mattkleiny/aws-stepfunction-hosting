using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Amazon.StepFunction
{
  // TODO: support an attributed object model 
  // TODO: don't forget integration testing scenarios (IStepUnderTest<T> and the like)
  // TODO: create a definition per step type and generify over a type bound?
  // TODO: ferry json input around the execution, as that is the native process in AWS.

  /// <summary>Defines a host capable of executing AWS StepFunction state machines locally.</summary>
  public sealed class StepFunctionHost
  {
    /// <summary>Creates a <see cref="StepFunctionHost"/> from the given state machine specification and <see cref="StepHandlerFactory"/>.</summary>
    public static StepFunctionHost FromJson(string specification, StepHandlerFactory handlerFactory)
    {
      Check.NotNullOrEmpty(specification, nameof(specification));
      Check.NotNull(handlerFactory, nameof(handlerFactory));

      var definition = StepFunctionDefinition.Parse(specification);

      return new StepFunctionHost(definition, handlerFactory);
    }

    public StepFunctionHost(StepFunctionDefinition definition, StepHandlerFactory factory)
    {
      Check.NotNull(definition, nameof(definition));
      Check.NotNull(factory, nameof(factory));

      Definition = definition;

      Steps       = definition.Steps.Select(step => step.Create(factory)).ToImmutableList();
      StepsByName = Steps.ToImmutableDictionary(step => step.Name, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>The underlying <see cref="StepFunctionDefinition"/> used to derive this host.</summary>
    public StepFunctionDefinition Definition { get; }

    /// <summary>A maximum period for 'Wait' steps in the step function.</summary>
    public TimeSpan? MaxWaitDuration { get; set; }

    /// <summary>A list of <see cref="Step"/>s that back this step function.</summary>
    internal IImmutableList<Step> Steps { get; }

    /// <summary>Permits looking up <see cref="Step"/>s by name.</summary>
    internal IImmutableDictionary<string, Step> StepsByName { get; }

    /// <summary>The initial step to use when executing the step function.</summary>
    internal Step InitialStep => StepsByName[Definition.StartAt];

    /// <summary>Executes the step function from it's <see cref="InitialStep"/>.</summary>
    public async Task<Result> ExecuteAsync(object input = null, CancellationToken cancellationToken = default)
    {
      var context = new Context
      {
        CurrentStep       = InitialStep,
        State             = input,
        Status            = Status.Executing,
        CancellationToken = cancellationToken
      };

      await ExecuteAsync(context);

      return new Result
      {
        Output    = context.State,
        IsSuccess = context.Status == Status.Success,
        Exception = context.Exception,
        History   = context.History.ToImmutableList()
      };
    }

    /// <summary>Executes the step function on the given <see cref="Context"/>.</summary>
    private async Task ExecuteAsync(Context context)
    {
      TimeSpan Min(TimeSpan a, TimeSpan b) => a < b ? a : b;

      // trampoline... weeee
      // TODO: who cares about the stack? remove transitions and add logic directly to step implementations
      while (context.CurrentStep != null && context.Status == Status.Executing)
      {
        foreach (var transition in await context.CurrentStep.ExecuteAsync(context.State, context.CancellationToken))
        {
          switch (transition)
          {
            case Transition.Next next:
              context.CurrentStep = StepsByName[next.Name];
              context.State       = next.Input;
              break;

            case Transition.Wait wait:
              var delay = Min(wait.Duration, MaxWaitDuration.GetValueOrDefault(wait.Duration));
              await Task.Delay(delay, context.CancellationToken);
              break;

            case Transition.Succeed succeed:
              context.State  = succeed.Output;
              context.Status = Status.Success;
              break;

            case Transition.Fail fail:
              context.Exception = fail.Exception;
              context.Status    = Status.Failure;
              break;

            default:
              throw new InvalidOperationException("An unrecognized transition was provided: " + transition);
          }
        }
      }
    }

    /// <summary>Contains the status of a particular execution.</summary>
    private enum Status
    {
      Executing,
      Success,
      Failure
    }

    /// <summary>Context for the execution of a step function.</summary>
    private sealed class Context
    {
      public Step      CurrentStep { get; set; }
      public object    State       { get; set; }
      public Status    Status      { get; set; }
      public Exception Exception   { get; set; }

      public CancellationToken CancellationToken { get; set; }

      public List<History> History { get; } = new List<History>();
    }

    /// <summary>Encapsulates the result of a step function execution.</summary>
    public sealed class Result
    {
      /// <summary>The final output from the step function.</summary>
      public object Output { get; set; }

      public bool IsSuccess { get; set; }
      public bool IsFailure => !IsSuccess;

      /// <summary>The <see cref="Exception"/> that broke the step function.</summary>
      public Exception Exception { get; set; }

      /// <summary><see cref="StepFunctionHost.History"/> for this execution.</summary>
      public IImmutableList<History> History { get; set; }
    }

    /// <summary>Encapsulates the result of a particular step in the step function.</summary>
    public sealed class History
    {
      public History(string stepName, object input, object output, bool succeeded = true)
      {
        Check.NotNullOrEmpty(stepName, nameof(stepName));

        StepName = stepName;
        Input    = input;
        Output   = output;

        Succeeded = succeeded;
      }

      public string StepName { get; }

      public object Input  { get; }
      public object Output { get; }

      public bool Succeeded { get; }
      public bool Failed    => !Succeeded;
    }
  }
}