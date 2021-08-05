using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Amazon.StepFunction.Hosting.Definition;
using Amazon.StepFunction.Hosting.Evaluation;

namespace Amazon.StepFunction.Hosting
{
  // TODO: extract a 'StepFunctionEvaluator' class to allow parallel states to share context

  /// <summary>Defines a host capable of executing AWS StepFunction state machines locally.</summary>
  public sealed class StepFunctionHost
  {
    public static StepFunctionHost FromJson(string specification, StepHandlerFactory factory)
    {
      return FromJson(specification, factory, Impositions.Default);
    }

    public static StepFunctionHost FromJson(string specification, StepHandlerFactory factory, Impositions impositions)
    {
      Debug.Assert(!string.IsNullOrEmpty(specification), "!string.IsNullOrEmpty(specification)");

      var definition = StepFunctionDefinition.Parse(specification);

      return new StepFunctionHost(definition, factory, impositions);
    }

    public StepFunctionHost(StepFunctionDefinition definition, StepHandlerFactory factory)
      : this(definition, factory, Impositions.Default)
    {
    }

    public StepFunctionHost(StepFunctionDefinition definition, StepHandlerFactory factory, Impositions impositions)
    {
      Definition  = definition;
      Impositions = impositions;

      Steps       = definition.Steps.Select(step => step.Create(factory)).ToImmutableList();
      StepsByName = Steps.ToImmutableDictionary(step => step.Name, StringComparer.OrdinalIgnoreCase);
    }

    internal StepFunctionDefinition Definition  { get; }
    internal Impositions            Impositions { get; }

    internal IImmutableList<Step>               Steps       { get; }
    internal IImmutableDictionary<string, Step> StepsByName { get; }

    internal Step InitialStep => StepsByName[Definition.StartAt];

    public Task<Result> ExecuteAsync(object? input = null, CancellationToken cancellationToken = default)
    {
      return ExecuteAsync(Impositions, input, cancellationToken);
    }

    public async Task<Result> ExecuteAsync(Impositions impositions, object? input = null, CancellationToken cancellationToken = default)
    {
      return await ExecuteAsync(impositions, InitialStep, input, cancellationToken);
    }

    private async Task<Result> ExecuteAsync(Impositions impositions, Step initialStep, object? input, CancellationToken cancellationToken = default)
    {
      var execution = new Execution(this, impositions)
      {
        NextStep = initialStep,
        Data     = StepFunctionData.Wrap(input),
        Status   = Status.Executing
      };

      await execution.ExecuteAsync(cancellationToken);

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

    /// <summary>Context for a single execution of a step function.</summary>
    private sealed record Execution
    {
      private readonly StepFunctionHost host;
      private readonly Impositions      impositions;

      public Execution(StepFunctionHost host, Impositions impositions)
      {
        this.host        = host;
        this.impositions = impositions;
      }

      public Step?            NextStep  { get; set; } = null;
      public StepFunctionData Data      { get; set; } = StepFunctionData.None;
      public Status           Status    { get; set; } = Status.Executing;
      public Exception?       Exception { get; set; } = null;
      public List<History>    History   { get; }      = new();

      /// <summary>Evaluates this execution.</summary>
      /// <param name="cancellationToken"></param>
      public async Task ExecuteAsync(CancellationToken cancellationToken)
      {
        // trampoline over transitions provided by the step executions
        while (NextStep != null)
        {
          var currentStep = NextStep;
          var transition  = await currentStep.ExecuteAsync(Data, impositions, cancellationToken);

          switch (transition)
          {
            case Transition.Next(var name, var output):
            {
              var nextStep = impositions.StepSelector(name);

              NextStep = host.StepsByName[nextStep];
              Data     = output;

              break;
            }
            case Transition.Succeed(var output):
            {
              Data     = output;
              Status   = Status.Success;
              NextStep = null;

              break;
            }
            case Transition.Fail(var exception):
            {
              Exception = exception;
              Status    = Status.Failure;
              NextStep  = null;

              break;
            }
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
    public sealed record Result
    {
      public bool IsSuccess { get; set; } = false;
      public bool IsFailure => !IsSuccess;

      public object?    Output    { get; set; }
      public Exception? Exception { get; set; } = null;

      public IImmutableList<History> History { get; set; } = ImmutableList<History>.Empty;
    }

    /// <summary>Encapsulates the result of a particular step in the step function.</summary>
    public sealed record History
    {
      public string   StepName   { get; set; } = string.Empty;
      public DateTime OccurredAt { get; }      = DateTime.Now;

      public bool Succeeded { get; set; } = false;
      public bool Failed    => !Succeeded;
    }
  }
}