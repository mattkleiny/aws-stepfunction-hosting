using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Amazon.StepFunction.Hosting
{
  /// <summary>A utility that permits notification of executions and transitions across a Step Function and all of it's sub-steps.</summary>
  public interface IStepFunctionDebugger
  {
    event Action<IStepFunctionExecution, string>           StepChanged;
    event Action<IStepFunctionExecution, ExecutionHistory> HistoryChanged;

    IEnumerable<ExecutionHistory> GetExecutionHistory(IStepFunctionExecution execution);

    void NotifyStepChanged(IStepFunctionExecution execution, string currentStepName);
    void NotifyHistoryAdded(IStepFunctionExecution execution, ExecutionHistory history);
  }

  internal sealed class StepFunctionDebugger : IStepFunctionDebugger
  {
    private readonly ConcurrentDictionary<IStepFunctionExecution, ConcurrentBag<ExecutionHistory>> historiesByRootExecution = new();

    public event Action<IStepFunctionExecution, string>?           StepChanged;
    public event Action<IStepFunctionExecution, ExecutionHistory>? HistoryChanged;

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

      // serialize all execution histories into a single enumerable
      return execution.History.Concat(execution.History.SelectMany(_ => _.ChildHistory).SelectMany(_ => _));
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
  }
}