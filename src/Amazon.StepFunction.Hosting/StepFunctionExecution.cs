﻿using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Amazon.StepFunction.Hosting.Definition;
using Amazon.StepFunction.Hosting.Evaluation;

namespace Amazon.StepFunction.Hosting
{
  /// <summary>Contains the status of a particular execution.</summary>
  public enum ExecutionStatus
  {
    Executing,
    Success,
    Failure
  }

  /// <summary>Encapsulates the history of a particular execution in the step function.</summary>
  public sealed record ExecutionHistory
  {
    public string           StepName     { get; init; } = string.Empty;
    public StepFunctionData Data         { get; init; } = StepFunctionData.Empty;
    public DateTime         OccurredAt   { get; }       = DateTime.Now;
    public bool             IsSuccessful { get; init; } = false;
    public bool             IsFailure    => !IsSuccessful;
  }

  /// <summary>Provides information about a single step function execution.</summary>
  public interface IStepFunctionExecution
  {
    event Action<string> StepChanged;
    event Action         Completed;

    string                          ExecutionId { get; }
    ExecutionStatus                 Status      { get; }
    StepFunctionData                Data        { get; }
    StepFunctionDefinition          Definition  { get; }
    IReadOnlyList<ExecutionHistory> History     { get; }
  }

  /// <summary>Context for a single execution of a step function.</summary>
  internal sealed class StepFunctionExecution : IStepFunctionExecution
  {
    private static readonly TimeSpan TokenPollTime = TimeSpan.FromSeconds(1);

    private readonly StepFunctionHost host;

    public StepFunctionExecution(StepFunctionHost host)
    {
      this.host = host;
    }

    public event Action<string>? StepChanged;
    public event Action?         Completed;

    public string                 ExecutionId { get; }      = Guid.NewGuid().ToString();
    public ExecutionStatus        Status      { get; set; } = ExecutionStatus.Executing;
    public StepFunctionData       Data        { get; set; } = StepFunctionData.Empty;
    public StepFunctionDefinition Definition  => host.Definition;
    public Exception?             Exception   { get; set; } = null;
    public List<ExecutionHistory> History     { get; }      = new();
    public Step?                  NextStep    { get; set; } = null;

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
      // trampoline over transitions provided by the step executions
      while (NextStep != null)
      {
        var currentStep = NextStep;

        StepChanged?.Invoke(currentStep.Name);

        var transition = await currentStep.ExecuteAsync(host.Impositions, Data, cancellationToken);

        switch (transition)
        {
          case Transition.Next(var name, var output, var token):
          {
            // wait for task token completion, if enabled
            if (token != null && host.Impositions.EnableTaskTokens)
            {
              host.Tokens.NotifyTaskWaiting(token);

              while (!host.Tokens.IsTaskCompleted(token))
              {
                await Task.Delay(TokenPollTime, cancellationToken);
              }
            }

            var nextStep = host.Impositions.StepSelector(name);

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
          default:
            throw new InvalidOperationException("An unrecognized transition was provided: " + transition);
        }

        History.Add(new ExecutionHistory
        {
          StepName     = currentStep.Name,
          Data         = Data,
          IsSuccessful = Status != ExecutionStatus.Failure
        });
      }

      Completed?.Invoke();
    }

    IReadOnlyList<ExecutionHistory> IStepFunctionExecution.History => History;
  }
}