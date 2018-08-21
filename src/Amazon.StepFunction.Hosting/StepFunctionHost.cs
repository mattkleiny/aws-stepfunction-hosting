using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Amazon.StepFunction.Hosting.Definition;

namespace Amazon.StepFunction.Hosting
{
  /// <summary>Defines a host capable of executing AWS StepFunction state machines locally.</summary>
  public sealed class StepFunctionHost
  {
    /// <summary>Creates a <see cref="StepFunctionHost"/> from the given state machine specification and <see cref="StepHandlerFactory"/>.</summary>
    public static StepFunctionHost FromJson(string specification, StepHandlerFactory factory) => FromJson(specification, factory, Impositions.Default);

    /// <summary>Creates a <see cref="StepFunctionHost"/> from the given state machine specification and <see cref="StepHandlerFactory"/>.</summary>
    public static StepFunctionHost FromJson(string specification, StepHandlerFactory factory, Impositions impositions)
    {
      Check.NotNullOrEmpty(specification, nameof(specification));
      Check.NotNull(factory, nameof(factory));
      Check.NotNull(impositions, nameof(impositions));

      var definition = StepFunctionDefinition.Parse(specification);

      return new StepFunctionHost(definition, factory, impositions);
    }

    public StepFunctionHost(StepFunctionDefinition definition, StepHandlerFactory factory)
      : this(definition, factory, Impositions.Default)
    {
    }

    public StepFunctionHost(StepFunctionDefinition definition, StepHandlerFactory factory, Impositions impositions)
    {
      Check.NotNull(definition, nameof(definition));
      Check.NotNull(factory, nameof(factory));
      Check.NotNull(impositions, nameof(impositions));

      Definition  = definition;
      Impositions = impositions;

      Steps       = definition.Steps.Select(step => step.Create(factory)).ToImmutableList();
      StepsByName = Steps.ToImmutableDictionary(step => step.Name, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>The <see cref="Hosting.Impositions"/> on the step function</summary>
    internal Impositions Impositions { get; }

    /// <summary>A list of <see cref="Step"/>s that back this step function.</summary>
    internal IImmutableList<Step> Steps { get; }

    /// <summary>Permits looking up <see cref="Step"/>s by name.</summary>
    internal IImmutableDictionary<string, Step> StepsByName { get; }

    /// <summary>The underlying <see cref="StepFunctionDefinition"/> used to derive this host.</summary>
    internal StepFunctionDefinition Definition { get; }

    /// <summary>The initial step to use when executing the step function.</summary>
    internal Step InitialStep => StepsByName[Definition.StartAt];

    /// <summary>Executes the step function from it's <see cref="InitialStep"/>.</summary>
    public Task<Result> ExecuteAsync(object input = null, CancellationToken cancellationToken = default)
    {
      return ExecuteAsync(Impositions, input, cancellationToken);
    }

    /// <summary>Executes the step function from it's <see cref="InitialStep"/>.</summary>
    public async Task<Result> ExecuteAsync(Impositions impositions, object input = null, CancellationToken cancellationToken = default)
    {
      Check.NotNull(impositions, nameof(impositions));

      var execution = new Execution(this, impositions)
      {
        NextStep          = InitialStep,
        Data              = StepFunctionData.Wrap(input),
        Status            = Status.Executing,
        CancellationToken = cancellationToken
      };

      await execution.ExecuteAsync();

      return new Result
      {
        Output    = execution.Data.Value,
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
      private readonly Impositions      impositions;

      public Execution(StepFunctionHost host, Impositions impositions)
      {
        this.host        = host;
        this.impositions = impositions;
      }

      public Step             NextStep  { get; set; }
      public StepFunctionData Data      { get; set; }
      public Status           Status    { get; set; }
      public Exception        Exception { get; set; }

      public CancellationToken CancellationToken { get; set; }

      public List<History> History { get; } = new List<History>();

      /// <summary>Evaluates this execution.</summary>
      /// <remarks>This is a trampoline off <see cref="Transition"/>s provided by the step executions.</remarks>
      public async Task ExecuteAsync()
      {
        while (NextStep != null)
        {
          var currentStep = NextStep;

          var transition = await currentStep.ExecuteAsync(impositions, Data, CancellationToken);

          switch (transition)
          {
            case Transition.Next next:
              var nextStep = impositions.StepSelector(next.Name);

              NextStep = host.StepsByName[nextStep];
              Data     = next.Output;
              break;

            case Transition.Succeed succeed:
              Data     = succeed.Output;
              Status   = Status.Success;
              NextStep = null;
              break;

            case Transition.Fail fail:
              Exception = fail.Exception;
              Status    = Status.Failure;
              NextStep  = null;
              break;

            default:
              throw new InvalidOperationException("An unrecognized transition was provided: " + transition);
          }

          History.Add(new History
          {
            StepName  = currentStep.Name,
            Succeeded = Status != Status.Failure
          });
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
      public DateTime OccurredAt { get; } = DateTime.Now;

      public string StepName { get; set; }

      public bool Succeeded { get; set; }
      public bool Failed    => !Succeeded;
    }
  }
}