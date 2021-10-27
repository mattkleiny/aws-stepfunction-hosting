using System;
using System.Threading;

namespace Amazon.StepFunction.Hosting
{
  // TODO: collect history here, instead?
  
  /// <summary>A utility that permits notification and scoping of executions and transitions across a Step Function and it's sub-steps.</summary>
  public interface IStepFunctionDebugger
  {
    event Action<IStepFunctionScope, string>           StepChanged;
    event Action<IStepFunctionScope, ExecutionHistory> HistoryChanged;

    IStepFunctionScope OnEnterExecutionScope(IStepFunctionExecution execution);
  }

  /// <summary>Scoping details for a particular <see cref="IStepFunctionExecution"/>.</summary>
  public interface IStepFunctionScope : IDisposable
  {
    IStepFunctionExecution  Execution       { get; }
    IStepFunctionExecution? ParentExecution { get; }

    void OnStepChanged(string currentStepName);
    void OnHistoryAdded(ExecutionHistory history);
  }

  internal sealed class StepFunctionDebugger : IStepFunctionDebugger
  {
    public event Action<IStepFunctionScope, string>?           StepChanged;
    public event Action<IStepFunctionScope, ExecutionHistory>? HistoryChanged;

    public IStepFunctionScope OnEnterExecutionScope(IStepFunctionExecution execution)
    {
      return StepFunctionScope.Enter(this, execution);
    }

    private void NotifyStepChanged(IStepFunctionScope scope, string currentStepName)
    {
      StepChanged?.Invoke(scope, currentStepName);
    }

    private void NotifyHistoryAdded(IStepFunctionScope scope, ExecutionHistory history)
    {
      HistoryChanged?.Invoke(scope, history);
    }

    private sealed class StepFunctionScope : IStepFunctionScope
    {
      private static AsyncLocal<StepFunctionScope?> Current { get; } = new();

      public static StepFunctionScope Enter(StepFunctionDebugger debugger, IStepFunctionExecution execution)
      {
        Current.Value = new StepFunctionScope(debugger, execution, Current.Value);

        return Current.Value;
      }

      private readonly StepFunctionDebugger debugger;
      private readonly StepFunctionScope?   parent;

      private StepFunctionScope(StepFunctionDebugger debugger, IStepFunctionExecution execution, StepFunctionScope? parent)
      {
        this.debugger = debugger;
        this.parent   = parent;

        Execution = execution;
      }

      public IStepFunctionExecution  Execution       { get; }
      public IStepFunctionExecution? ParentExecution => parent?.Execution;

      public void OnStepChanged(string currentStepName)
      {
        debugger.NotifyStepChanged(this, currentStepName);
      }

      public void OnHistoryAdded(ExecutionHistory history)
      {
        debugger.NotifyHistoryAdded(this, history);
      }

      public void Dispose()
      {
        Current.Value = parent;
      }
    }
  }
}