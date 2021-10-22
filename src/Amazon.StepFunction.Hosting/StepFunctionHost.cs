using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Amazon.StepFunction.Hosting.Definition;
using Amazon.StepFunction.Hosting.Evaluation;
using Amazon.StepFunction.Hosting.IPC;

namespace Amazon.StepFunction.Hosting
{
  /// <summary>Defines a host capable of executing AWS StepFunction state machines locally.</summary>
  public sealed class StepFunctionHost : StepFunctionHost.IInterProcessChannel
  {
    private const string DefaultPipeName = "ipc://stepfunctionhost";

    public static StepFunctionHost CreateFromJson(string specification, StepHandlerFactory factory)
    {
      return CreateFromJson(specification, factory, Impositions.Default);
    }

    public static StepFunctionHost CreateFromJson(string specification, StepHandlerFactory factory, Impositions impositions)
    {
      var definition = StepFunctionDefinition.Parse(specification);

      return new StepFunctionHost(definition, factory, impositions);
    }

    public static InterProcessHost<IInterProcessChannel> CreateHost(StepFunctionHost host, string pipeName = DefaultPipeName)
    {
      return new InterProcessHost<IInterProcessChannel>(host, pipeName);
    }

    public static InterProcessClient<IInterProcessChannel> CreateClient(string pipeName = DefaultPipeName)
    {
      return new InterProcessClient<IInterProcessChannel>(pipeName);
    }

    public StepFunctionHost(StepFunctionDefinition definition, StepHandlerFactory factory)
      : this(definition, factory, Impositions.Default)
    {
    }

    public StepFunctionHost(StepFunctionDefinition definition, StepHandlerFactory factory, Impositions impositions)
    {
      Definition  = definition;
      Impositions = impositions;

      // instantiate steps and remember them by name
      StepsByName = definition.Steps
        .Select(step => step.Create(factory))
        .ToImmutableDictionary(step => step.Name, StringComparer.OrdinalIgnoreCase);
    }

    public event Action<IStepFunctionExecution>? ExecutionStarted;
    public event Action<IStepFunctionExecution>? ExecutionStopped;

    public StepFunctionDefinition             Definition { get; }
    public ITaskTokenSink                     TaskTokens { get; } = new ConcurrentTaskTokenSink();
    public List<IStepFunctionDetailCollector> Collectors { get; } = new();

    internal Impositions                       Impositions { get; }
    internal ImmutableDictionary<string, Step> StepsByName { get; }

    public Task<ExecutionResult> ExecuteAsync(object? input = default, CancellationToken cancellationToken = default)
    {
      var executionId = Guid.NewGuid().ToString();

      return ExecuteAsync(executionId, input, cancellationToken);
    }

    public Task<ExecutionResult> ExecuteAsync(string executionId, object? input = default, CancellationToken cancellationToken = default)
    {
      return ExecuteAtStepAsync(Definition.StartAt, executionId, input, cancellationToken);
    }

    public Task<ExecutionResult> ExecuteAtStepAsync(string initialStepName, string executionId, object? input = default, CancellationToken cancellationToken = default)
    {
      if (!StepsByName.TryGetValue(initialStepName, out var step))
      {
        throw new Exception($"Unable to locate initial step {step}");
      }

      return ExecuteAsync(step, executionId, input, cancellationToken);
    }

    private async Task<ExecutionResult> ExecuteAsync(Step initialStep, string executionId, object? input, CancellationToken cancellationToken)
    {
      var data = new StepFunctionData(input);

      var execution = new StepFunctionExecution(this, executionId)
      {
        NextStep = initialStep,
        Input    = data,
        Output   = data,
        Status   = ExecutionStatus.Executing
      };

      ExecutionStarted?.Invoke(execution);

      await execution.ExecuteAsync(cancellationToken);

      ExecutionStopped?.Invoke(execution);

      return new ExecutionResult(execution)
      {
        Output    = execution.Output,
        Exception = execution.Exception,
      };
    }

    void IInterProcessChannel.ExecuteAsync(string executionId, object? input)
    {
      ExecuteAsync(executionId, input);
    }

    void IInterProcessChannel.ExecuteAtAsync(string initialStepName, string executionId, object? input)
    {
      ExecuteAtStepAsync(initialStepName, executionId, input);
    }

    void IInterProcessChannel.SetTaskStatus(string taskToken, TaskTokenStatus status)
    {
      TaskTokens.SetTokenStatus(taskToken, status);
    }

    /// <summary>Defines the restricted set of methods available via IPC.</summary>
    public interface IInterProcessChannel
    {
      void ExecuteAsync(string executionId, object? input);
      void ExecuteAtAsync(string stepName, string executionId, object? input);
      void SetTaskStatus(string taskToken, TaskTokenStatus status);
    }

    /// <summary>Encapsulates the result of a step function execution.</summary>
    public sealed record ExecutionResult(IStepFunctionExecution Execution)
    {
      public bool             IsSuccess => Execution.Status == ExecutionStatus.Success;
      public bool             IsFailure => !IsSuccess;
      public StepFunctionData Output    { get; init; } = StepFunctionData.Empty;
      public Exception?       Exception { get; init; } = null;
    }
  }
}