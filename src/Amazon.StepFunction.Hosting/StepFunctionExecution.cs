﻿using System;
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
  /// <summary>Contains the status of a particular <see cref="IStepFunctionExecution"/>.</summary>
  public enum ExecutionStatus
  {
    Executing,
    Success,
    Failure
  }

  /// <summary>A single history entry in a particular <see cref="IStepFunctionExecution"/>.</summary>
  [DebuggerDisplay("{StepName} at {ExecutedAt}")]
  public sealed record ExecutionHistory(IStepFunctionExecution Execution)
  {
    public StepFunctionDefinition Definition     => Execution.Definition;
    public string                 StepName       { get; init; } = string.Empty;
    public DateTime               ExecutedAt     { get; init; } = default;
    public int                    ExecutionCount { get; init; } = 0;
    public bool                   IsSuccessful   { get; init; } = false;
    public bool                   IsFailed       => !IsSuccessful;
    public StepFunctionData       InputData      { get; init; } = StepFunctionData.Empty;
    public StepFunctionData       OutputData     { get; init; } = StepFunctionData.Empty;
    public List<object>           UserData       { get; init; } = new();

    /// <summary>History from child steps of this particular step</summary>
    public ImmutableList<IEnumerable<ExecutionHistory>> ChildHistory { get; init; } = ImmutableList<IEnumerable<ExecutionHistory>>.Empty;
  }

  /// <summary>Represents a single Step Function execution and allows observing it's state and changes.</summary>
  public interface IStepFunctionExecution
  {
    event Action<string>?          StepChanged;
    event Action<ExecutionHistory> HistoryAdded;

    string           ExecutionId   { get; }
    string?          CurrentStep   { get; }
    DateTime         StartedAt     { get; }
    StepFunctionData Input         { get; }
    StepFunctionData Output        { get; }
    Stack<Exception> Exceptions    { get; }
    string?          FailureCause  { get; }

    public Exception? LastException
    {
      get
      {
        if (Exceptions.TryPeek(out var exception))
        {
          return exception;
        }

        return default;
      }
    }

    IStepFunctionExecution?         Parent     { get; }
    ExecutionStatus                 Status     { get; }
    StepFunctionDefinition          Definition { get; }
    IReadOnlyList<ExecutionHistory> History    { get; }

    /// <summary>Determines the root <see cref="IStepFunctionExecution"/> from this potentially nested execution.</summary>
    IStepFunctionExecution ResolveRootExecution()
    {
      var current = this;

      while (true)
      {
        if (current.Parent != null)
        {
          current = current.Parent;
        }
        else
        {
          return current;
        }
      }
    }

    /// <summary>Collects all history recursively down the execution hierarchy</summary>
    IEnumerable<ExecutionHistory> CollectAllHistory()
    {
      IEnumerable<ExecutionHistory> GetHistoryRecursively(IEnumerable<ExecutionHistory> histories)
      {
        foreach (var history in histories)
        {
          yield return history;

          foreach (var childHistory in history.ChildHistory.SelectMany(GetHistoryRecursively))
          {
            yield return childHistory;
          }
        }
      }

      return GetHistoryRecursively(History);
    }
  }

  [DebuggerDisplay("{ExecutionId} started at {StartedAt}")]
  internal sealed class StepFunctionExecution : IStepFunctionExecution
  {
    private static readonly TimeSpan TokenPollTime = TimeSpan.FromSeconds(1);

    private readonly StepFunctionHost host;

    public StepFunctionExecution(StepFunctionHost host, string executionId, IStepFunctionExecution? parent)
    {
      this.host = host;

      ExecutionId = executionId;
      Parent      = parent;
    }

    public event Action<string>?           StepChanged;
    public event Action<ExecutionHistory>? HistoryAdded;

    public string           ExecutionId  { get; }
    public DateTime         StartedAt    { get; }       = DateTime.Now;
    public StepFunctionData Input        { get; init; } = StepFunctionData.Empty;
    public StepFunctionData Output       { get; set; }  = StepFunctionData.Empty;
    public Stack<Exception> Exceptions   { get; }       = new();
    public string?          FailureCause { get; set; }  = null;

    public IStepFunctionExecution? Parent   { get; set; } = null;
    public ExecutionStatus         Status   { get; set; } = ExecutionStatus.Executing;
    public List<ExecutionHistory>  History  { get; }      = new();
    public Step?                   NextStep { get; set; } = null;

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
      var impositions          = host.Impositions;
      var beforeDetailsForStep = new Dictionary<string, object>();
      var afterDetailsForStep  = new Dictionary<string, object>();

      // trampoline over transitions provided by the executions
      while (NextStep != null)
      {
        var currentStep  = NextStep;
        var initialInput = Output;

        beforeDetailsForStep.Clear();
        afterDetailsForStep.Clear();

        StepChanged?.Invoke(currentStep.Name);
        impositions.Debugger.NotifyStepChanged(this, currentStep.Name);

        // allow processing before entering a particular step
        Output = await impositions.Debugger.NotifyStepEntered(this, currentStep.Name, Output, cancellationToken);

        // collect before details for this step
        foreach (var collector in impositions.Collectors)
        {
          beforeDetailsForStep[collector.Key] = await collector.OnBeforeExecuteStep(currentStep.Name, Output);
        }

        // execute the current step, collecting transition and sub-histories, if available
        var result = await currentStep.ExecuteAsync(impositions, Output, this, cancellationToken);

        switch (result.Transition)
        {
          case Transition.Next(var name, var output, var token):
          {
            // wait for task token completion, if enabled
            if (token != null && impositions.EnableTaskTokens)
            {
              while (host.TaskTokens.GetTokenStatus(token) == TaskTokenStatus.Waiting)
              {
                await Task.Delay(TokenPollTime, cancellationToken);
              }
            }

            var nextStep = impositions.StepSelector(name);

            if (!host.StepsByName.TryGetValue(nextStep, out var step))
            {
              throw new Exception($"Unable to resolve the next step '{step}' in the execution");
            }

            NextStep = host.StepsByName[nextStep];
            Output   = output;
            Status   = ExecutionStatus.Executing;

            break;
          }
          case Transition.Catch(var name, var output, var exception):
          {
            var nextStep = impositions.StepSelector(name);

            if (!host.StepsByName.TryGetValue(nextStep, out var step))
            {
              throw new Exception($"Unable to resolve the next step '{step}' in the execution");
            }

            NextStep = host.StepsByName[nextStep];
            Output   = output;
            Status   = ExecutionStatus.Failure;

            if (exception != null)
            {
              Exceptions.Push(exception);
            }

            break;
          }
          case Transition.Fail(var failureCause, var exception):
          {
            FailureCause = failureCause;
            Status       = ExecutionStatus.Failure;
            NextStep     = null;

            if (exception != null)
            {
              Exceptions.Push(exception);
            }

            break;
          }
          case Transition.Succeed(var output):
          {
            Output   = output;
            Status   = ExecutionStatus.Success;
            NextStep = null;

            break;
          }
          default:
            throw new InvalidOperationException($"An unrecognized transition was provided: {result.Transition}");
        }

        // allow processing after exiting a particular step
        Output = await impositions.Debugger.NotifyStepExited(this, currentStep.Name, Output, cancellationToken);

        // collect after details for this step
        foreach (var collector in impositions.Collectors)
        {
          afterDetailsForStep[collector.Key] = await collector.OnAfterExecuteStep(currentStep.Name, Output);
        }

        // build and augment history from attached collectors
        var history = new ExecutionHistory(this)
        {
          StepName       = currentStep.Name,
          IsSuccessful   = Status != ExecutionStatus.Failure,
          ExecutedAt     = DateTime.Now,
          ExecutionCount = History.Count(_ => _.StepName == currentStep.Name) + 1,
          InputData      = initialInput,
          OutputData     = Output,
          ChildHistory   = result.ChildHistory.ToImmutableList()
        };

        foreach (var collector in impositions.Collectors)
        {
          if (beforeDetailsForStep.TryGetValue(collector.Key, out var beforeDetails) &&
              afterDetailsForStep.TryGetValue(collector.Key, out var afterDetails))
          {
            collector.AugmentHistory(beforeDetails, afterDetails, history);
          }
        }

        // wait for a little while if we've introduced artificial delay into the step function
        if (impositions.StepTransitionDelay.HasValue)
        {
          await Task.Delay(impositions.StepTransitionDelay.Value, cancellationToken);
        }

        History.Add(history);
        HistoryAdded?.Invoke(history);

        impositions.Debugger.NotifyHistoryAdded(this, history);
      }
    }

    StepFunctionDefinition IStepFunctionExecution.         Definition  => host.Definition;
    string? IStepFunctionExecution.                        CurrentStep => NextStep?.Name;
    IReadOnlyList<ExecutionHistory> IStepFunctionExecution.History     => History;
  }
}
