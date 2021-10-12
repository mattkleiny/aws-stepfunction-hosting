using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Amazon.StepFunction.Hosting.Definition;
using Amazon.StepFunction.Hosting.Evaluation;

namespace Amazon.StepFunction.Hosting
{
  /// <summary>Defines a host capable of executing AWS StepFunction state machines locally.</summary>
  public sealed class StepFunctionHost
  {
    public static StepFunctionHost FromJson(string specification, StepHandlerFactory factory)
    {
      return FromJson(specification, factory, Impositions.Default);
    }

    public static StepFunctionHost FromJson(string specification, StepHandlerFactory factory, Impositions impositions)
    {
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

    internal StepFunctionDefinition            Definition  { get; }
    internal Impositions                       Impositions { get; }
    internal ImmutableList<Step>               Steps       { get; }
    internal ImmutableDictionary<string, Step> StepsByName { get; }
    internal Step                              InitialStep => StepsByName[Definition.StartAt];

    public Task<ExecutionResult> ExecuteAsync(object? input = null, CancellationToken cancellationToken = default)
    {
      return ExecuteAsync(Impositions, input, cancellationToken);
    }

    public async Task<ExecutionResult> ExecuteAsync(Impositions impositions, object? input = null, CancellationToken cancellationToken = default)
    {
      return await ExecuteAsync(impositions, InitialStep, input, cancellationToken);
    }

    private async Task<ExecutionResult> ExecuteAsync(Impositions impositions, Step initialStep, object? input, CancellationToken cancellationToken = default)
    {
      var execution = new Execution(this, impositions)
      {
        NextStep = initialStep,
        Data     = new StepFunctionData(input),
        Status   = ExecutionStatus.Executing
      };

      await execution.ExecuteAsync(cancellationToken);

      return new ExecutionResult
      {
        Output    = execution.Data,
        IsSuccess = execution.Status == ExecutionStatus.Success,
        Exception = execution.Exception,
        History   = execution.History.ToImmutableList()
      };
    }

    /// <summary>Contains the status of a particular execution.</summary>
    private enum ExecutionStatus
    {
      Executing,
      Success,
      Failure
    }

    /// <summary>Encapsulates the result of a particular step in the step function.</summary>
    public sealed record HistoryEntry
    {
      public string   StepName   { get; init; } = string.Empty;
      public DateTime OccurredAt { get; }       = DateTime.Now;

      public bool Succeeded { get; init; } = false;
      public bool Failed    => !Succeeded;
    }

    /// <summary>Encapsulates the result of a step function execution.</summary>
    public sealed record ExecutionResult
    {
      public bool IsSuccess { get; init; } = false;
      public bool IsFailure => !IsSuccess;

      public StepFunctionData Output    { get; init; } = StepFunctionData.Empty;
      public Exception?       Exception { get; init; } = null;

      public IImmutableList<HistoryEntry> History { get; init; } = ImmutableList<HistoryEntry>.Empty;
    }

    /// <summary>Context for a single execution of a step function.</summary>
    private sealed record Execution
    {
      private static readonly TimeSpan TokenPollTime = TimeSpan.FromSeconds(1);

      private readonly StepFunctionHost host;
      private readonly Impositions      impositions;

      public Execution(StepFunctionHost host, Impositions impositions)
      {
        this.host        = host;
        this.impositions = impositions;
      }

      public Step?              NextStep  { get; set; } = null;
      public StepFunctionData   Data      { get; set; } = StepFunctionData.Empty;
      public ExecutionStatus    Status    { get; set; } = ExecutionStatus.Executing;
      public Exception?         Exception { get; set; } = null;
      public List<HistoryEntry> History   { get; }      = new();

      public async Task ExecuteAsync(CancellationToken cancellationToken)
      {
        // trampoline over transitions provided by the step executions
        while (NextStep != null)
        {
          var currentStep = NextStep;
          var transition  = await currentStep.ExecuteAsync(impositions, Data, cancellationToken);

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
              Status   = ExecutionStatus.Success;
              NextStep = null;

              break;
            }
            case Transition.Fail(_, var exception):
            {
              // TODO: log the cause for non-exceptions?

              Exception = exception;
              Status    = ExecutionStatus.Failure;
              NextStep  = null;

              break;
            }
            case Transition.WaitForToken(var token):
            {
              impositions.Tokens.NotifyTaskWaiting(token);

              while (!impositions.Tokens.IsTaskCompleted(token))
              {
                await Task.Delay(TokenPollTime, cancellationToken);
              }

              break;
            }
            default:
              throw new InvalidOperationException("An unrecognized transition was provided: " + transition);
          }

          History.Add(new HistoryEntry
          {
            StepName  = currentStep.Name,
            Succeeded = Status != ExecutionStatus.Failure
          });
        }
      }
    }
  }
}