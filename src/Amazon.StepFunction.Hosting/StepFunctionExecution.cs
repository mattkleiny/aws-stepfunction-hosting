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
    public List<object>     UserData       { get; init; } = new();

    /// <summary>History from potential sub-steps in this execution</summary>
    public List<ImmutableArray<ExecutionHistory>> SubHistories { get; init; } = new();
  }

  /// <summary>Represents a single Step Function execution and allows observing it's changes and state.</summary>
  public interface IStepFunctionExecution
  {
    event Action<string>           StepChanged;
    event Action<ExecutionHistory> HistoryAdded;

    string           ExecutionId { get; }
    string?          CurrentStep { get; }
    DateTime         StartedAt   { get; }
    StepFunctionData Input       { get; }
    StepFunctionData Output      { get; }

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
    public DateTime               StartedAt   { get; }       = DateTime.Now;
    public StepFunctionData       Input       { get; init; } = StepFunctionData.Empty;
    public StepFunctionData       Output      { get; set; }  = StepFunctionData.Empty;
    public ExecutionStatus        Status      { get; set; }  = ExecutionStatus.Executing;
    public Exception?             Exception   { get; set; }  = null;
    public List<ExecutionHistory> History     { get; }       = new();
    public Step?                  NextStep    { get; set; }  = null;

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
      var beforeDetailsForStep = new Dictionary<Type, object>();
      var afterDetailsForStep  = new Dictionary<Type, object>();

      // trampoline over transitions provided by the step executions
      while (NextStep != null)
      {
        beforeDetailsForStep.Clear();
        afterDetailsForStep.Clear();

        var currentStep = NextStep;
        var currentData = Output;

        StepChanged?.Invoke(currentStep.Name);

        // collect before details for this step
        foreach (var collector in host.Impositions.Collectors)
        {
          beforeDetailsForStep[collector.GetType()] = await collector.OnBeforeExecuteStep(currentStep.Name, currentData);
        }

        // execute the current step, collecting transition and sub-histories, if available
        var result = await currentStep.ExecuteAsync(host.Impositions, Output, cancellationToken);

        switch (result.Transition)
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
            Output   = output;

            break;
          }
          case Transition.Succeed(var output):
          {
            Output   = output;
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
            throw new InvalidOperationException($"An unrecognized transition was provided: {result.Transition}");
        }

        var history = new ExecutionHistory
        {
          StepName       = currentStep.Name,
          IsSuccessful   = Status != ExecutionStatus.Failure,
          ExecutedAt     = DateTime.Now,
          ExecutionCount = History.Count(_ => _.StepName == currentStep.Name) + 1,
          InputData      = currentData,
          OutputData     = Output,
          SubHistories   = result.SubHistories
        };

        // collect after details for this step
        foreach (var collector in host.Impositions.Collectors)
        {
          afterDetailsForStep[collector.GetType()] = await collector.OnAfterExecuteStep(currentStep.Name, currentData);
        }

        // augment execution history from collectors
        foreach (var collector in host.Impositions.Collectors)
        {
          var collectorType = collector.GetType();

          if (beforeDetailsForStep.TryGetValue(collectorType, out var beforeDetails) &&
              afterDetailsForStep.TryGetValue(collectorType, out var afterDetails))
          {
            collector.AugmentHistory(beforeDetails, afterDetails, history);
          }
        }

        History.Add(history);
        HistoryAdded?.Invoke(history);

        // wait for a little while if we've introduced artificial delay into the step function
        if (host.Impositions.StepTransitionDelay.HasValue)
        {
          await Task.Delay(host.Impositions.StepTransitionDelay.Value, cancellationToken);
        }
      }
    }

    StepFunctionDefinition IStepFunctionExecution.         Definition  => host.Definition;
    string? IStepFunctionExecution.                        CurrentStep => NextStep?.Name;
    IReadOnlyList<ExecutionHistory> IStepFunctionExecution.History     => History;
  }
}