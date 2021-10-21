using System;
using System.Collections.Generic;
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
  public sealed record ExecutionHistory
  {
    public string           StepName       { get; init; } = string.Empty;
    public DateTime         ExecutedAt     { get; init; } = default;
    public int              ExecutionCount { get; init; } = 0;
    public bool             IsSuccessful   { get; init; } = false;
    public bool             IsFailed       => !IsSuccessful;
    public StepFunctionData InputData      { get; init; } = StepFunctionData.Empty;
    public StepFunctionData OutputData     { get; init; } = StepFunctionData.Empty;
  }

  /// <summary>Represents a single Step Function execution and allows observing it's changes and state.</summary>
  public interface IStepFunctionExecution
  {
    event Action<string>           StepChanged;
    event Action<ExecutionHistory> HistoryAdded;

    string   ExecutionId { get; }
    string?  CurrentStep { get; }
    DateTime StartedAt   { get; }

    ExecutionStatus                 Status     { get; }
    StepFunctionDefinition          Definition { get; }
    IReadOnlyList<ExecutionHistory> History    { get; }
  }

  internal sealed class StepFunctionExecution : IStepFunctionExecution
  {
    private static readonly TimeSpan TokenPollTime = TimeSpan.FromSeconds(1);

    private readonly StepFunctionHost host;

    public StepFunctionExecution(StepFunctionHost host, string executionId)
    {
      this.host = host;

      ExecutionId = executionId;
    }

    public event Action<string>?           StepChanged;
    public event Action<ExecutionHistory>? HistoryAdded;

    public string                 ExecutionId { get; }
    public DateTime               StartedAt   { get; }      = DateTime.Now;
    public ExecutionStatus        Status      { get; set; } = ExecutionStatus.Executing;
    public StepFunctionData       Data        { get; set; } = StepFunctionData.Empty;
    public Exception?             Exception   { get; set; } = null;
    public List<ExecutionHistory> History     { get; }      = new();
    public Step?                  NextStep    { get; set; } = null;

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
      // trampoline over transitions provided by the step executions
      while (NextStep != null)
      {
        var currentStep = NextStep;
        var currentData = Data;

        StepChanged?.Invoke(currentStep.Name);

        var transition = await currentStep.ExecuteAsync(host.Impositions, Data, cancellationToken);

        switch (transition)
        {
          case Transition.Next(var name, var output, var token):
          {
            // wait for task token completion, if enabled
            if (token != null && host.Impositions.EnableTaskTokens)
            {
              while (host.TaskTokens.GetTokenStatus(token) == TaskTokenStatus.Waiting)
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

        var history = new ExecutionHistory
        {
          StepName       = currentStep.Name,
          IsSuccessful   = Status != ExecutionStatus.Failure,
          ExecutedAt     = DateTime.Now,
          ExecutionCount = History.Count(_ => _.StepName == currentStep.Name) + 1,
          InputData      = currentData,
          OutputData     = Data,
        };

        History.Add(history);

        HistoryAdded?.Invoke(history);
      }
    }

    StepFunctionDefinition IStepFunctionExecution.         Definition  => host.Definition;
    string? IStepFunctionExecution.                        CurrentStep => NextStep?.Name;
    IReadOnlyList<ExecutionHistory> IStepFunctionExecution.History     => History;
  }
}