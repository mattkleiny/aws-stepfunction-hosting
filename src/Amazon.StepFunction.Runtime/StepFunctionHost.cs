using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Amazon.StepFunction.Parsing;

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

    /// <summary>A list of <see cref="Step"/>s that back this step function.</summary>
    internal IImmutableList<Step> Steps { get; }

    /// <summary>Permits looking up <see cref="Step"/>s by name.</summary>
    internal IImmutableDictionary<string, Step> StepsByName { get; }

    /// <summary>The initial step to use when executing the step function.</summary>
    internal Step InitialStep => StepsByName[Definition.StartAt];

    /// <summary>Executes the step function from it's <see cref="InitialStep"/>.</summary>
    public async Task<Result> ExecuteAsync(object input = null, CancellationToken cancellationToken = default)
    {
      var execution = new Execution(this)
      {
        CurrentStep       = InitialStep,
        State             = input,
        Status            = Status.Executing,
        CancellationToken = cancellationToken
      };

      await execution.ExecuteAsync();

      return new Result
      {
        Output    = execution.State,
        IsSuccess = execution.Status == Status.Success,
        Exception = execution.Exception,
        History   = execution.History.ToImmutableList()
      };
    }

    /// <summary>Contains the status of a particular execution.</summary>
    private enum Status
    {
      Executing,
      Success,
      Failure
    }

    /// <summary>Context for the execution of a step function.</summary>
    private sealed class Execution
    {
      private readonly StepFunctionHost host;

      public Execution(StepFunctionHost host)
      {
        this.host = host;
      }

      public Step      CurrentStep { get; set; }
      public object    State       { get; set; }
      public Status    Status      { get; set; }
      public Exception Exception   { get; set; }

      public CancellationToken CancellationToken { get; set; }

      public List<History> History { get; } = new List<History>();

      /// <summary>Evaluates this execution.</summary>
      /// <remarks>This is a trampoline of a <see cref="Transition"/>-ADT provided by the step executions.</remarks>
      public async Task ExecuteAsync()
      {
        while (CurrentStep != null && Status == Status.Executing)
        {
          foreach (var transition in await CurrentStep.ExecuteAsync(State, CancellationToken))
          {
            switch (transition)
            {
              case Transition.Next next:
                CurrentStep = host.StepsByName[next.Name];
                State       = next.Input;
                break;

              case Transition.Wait wait:
                await Task.Delay(wait.Duration, CancellationToken);
                break;

              case Transition.Succeed succeed:
                State  = succeed.Output;
                Status = Status.Success;
                break;

              case Transition.Fail fail:
                Exception = fail.Exception;
                Status    = Status.Failure;
                break;

              default:
                throw new InvalidOperationException("An unrecognized transition was provided: " + transition);
            }
          }
        }
      }
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