using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Amazon.StepFunction.Hosting
{
  /// <summary>A callback that is received when a step is entered/exited.</summary>
  public delegate Task<StepFunctionData> StepCallback(string stepName, StepFunctionData input, CancellationToken cancellationToken = default);

  /// <summary>A utility that permits notification of executions and transitions across a Step Function and all of it's sub-steps.</summary>
  public interface IStepFunctionDebugger
  {
    event Action<IStepFunctionExecution, string>           StepChanged;
    event Action<IStepFunctionExecution, ExecutionHistory> HistoryChanged;

    event StepCallback? StepEntered;
    event StepCallback? StepExited;

    IEnumerable<ExecutionHistory> GetExecutionHistory(IStepFunctionExecution execution);

    void NotifyStepChanged(IStepFunctionExecution execution, string currentStepName);
    void NotifyHistoryAdded(IStepFunctionExecution execution, ExecutionHistory history);

    Task<StepFunctionData> NotifyStepEntered(IStepFunctionExecution execution, string stepName, StepFunctionData input, CancellationToken cancellationToken = default);
    Task<StepFunctionData> NotifyStepExited(IStepFunctionExecution execution, string stepName, StepFunctionData input, CancellationToken cancellationToken = default);
  }

  internal sealed class StepFunctionDebugger : IStepFunctionDebugger
  {
    private readonly ConcurrentDictionary<IStepFunctionExecution, ConcurrentBag<ExecutionHistory>> historiesByRootExecution = new();

    public event Action<IStepFunctionExecution, string>?           StepChanged;
    public event Action<IStepFunctionExecution, ExecutionHistory>? HistoryChanged;

    public event StepCallback? StepEntered;
    public event StepCallback? StepExited;

    public IEnumerable<ExecutionHistory> GetExecutionHistory(IStepFunctionExecution execution)
    {
      // NOTE: until a step has completed, it's sub-step history isn't recorded, but we want to be able to unit test
      //       and visualize the state of those steps in progress. to do this, we record steps that are currently 'in-progress'
      //       using a special hot list of 'real time' execution history. once those executions have completed however,
      //       we'll fallback to using the history we retain on the execution itself so as to not balloon the ongoing memory
      //       costs of holding on to every possible execution

      var root = execution.ResolveRootExecution();

      if (historiesByRootExecution.TryGetValue(root, out var history))
      {
        return history;
      }

      return execution.CollectAllHistory();
    }

    public void NotifyStepChanged(IStepFunctionExecution execution, string currentStepName)
    {
      StepChanged?.Invoke(execution, currentStepName);
    }

    public void NotifyHistoryAdded(IStepFunctionExecution execution, ExecutionHistory history)
    {
      // retain history in the hot list for later retrieval, clear the history if we're done with the execution
      var root = execution.ResolveRootExecution();

      if (execution.Status == ExecutionStatus.Executing)
      {
        if (!historiesByRootExecution.TryGetValue(root, out var historyList))
        {
          historiesByRootExecution[root] = historyList = new ConcurrentBag<ExecutionHistory>();
        }

        historyList.Add(history);
      }
      else
      {
        historiesByRootExecution.TryRemove(root, out _);
      }

      HistoryChanged?.Invoke(execution, history);
    }

    public Task<StepFunctionData> NotifyStepEntered(IStepFunctionExecution execution, string stepName, StepFunctionData input, CancellationToken cancellationToken = default)
    {
      if (StepEntered != null)
      {
        return StepEntered(stepName, input, cancellationToken);
      }

      return Task.FromResult(input);
    }

    public Task<StepFunctionData> NotifyStepExited(IStepFunctionExecution execution, string stepName, StepFunctionData input, CancellationToken cancellationToken = default)
    {
      if (StepExited != null)
      {
        return StepExited(stepName, input, cancellationToken);
      }

      return Task.FromResult(input);
    }
  }
}