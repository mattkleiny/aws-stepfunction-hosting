using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Amazon.StepFunction.Hosting.Definition;
using Amazon.StepFunction.Hosting.Evaluation;
using Amazon.StepFunction.Hosting.Tokens;

namespace Amazon.StepFunction.Hosting
{
  /// <summary>Defines a host capable of executing AWS StepFunction state machines locally.</summary>
  public sealed class StepFunctionHost : IDisposable
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

      // instantiate steps and remember them by name
      StepsByName = definition.Steps
        .Select(step => step.Create(factory))
        .ToImmutableDictionary(step => step.Name, StringComparer.OrdinalIgnoreCase);

      // create host for task token notification
      TokenSinkHost = new(new ConcurrentTokenSink());
    }

    public event Action<IStepFunctionExecution>? ExecutionStarted;

    public StepFunctionDefinition Definition { get; }
    public ITokenSink             TokenSink  => TokenSinkHost.Sink;

    internal Impositions                       Impositions   { get; }
    internal ImmutableDictionary<string, Step> StepsByName   { get; }
    internal Step                              InitialStep   => StepsByName[Definition.StartAt];
    internal TokenSinkHost                     TokenSinkHost { get; }

    public Task<ExecutionResult> ExecuteAsync(object? input = default, CancellationToken cancellationToken = default)
    {
      return ExecuteAsync(Impositions, input, cancellationToken);
    }

    public async Task<ExecutionResult> ExecuteAsync(Impositions impositions, object? input = default, CancellationToken cancellationToken = default)
    {
      return await ExecuteAsync(impositions, InitialStep, input, cancellationToken);
    }

    private async Task<ExecutionResult> ExecuteAsync(Impositions impositions, Step initialStep, object? input, CancellationToken cancellationToken = default)
    {
      var execution = new StepFunctionExecution(this)
      {
        NextStep = initialStep,
        Data     = new StepFunctionData(input),
        Status   = ExecutionStatus.Executing
      };

      ExecutionStarted?.Invoke(execution);

      await execution.ExecuteAsync(cancellationToken);

      return new ExecutionResult
      {
        Output    = execution.Data,
        IsSuccess = execution.Status == ExecutionStatus.Success,
        Exception = execution.Exception,
        History   = execution.History.ToImmutableList()
      };
    }

    public void Dispose()
    {
      TokenSinkHost.Dispose();
    }

    /// <summary>Encapsulates the result of a step function execution.</summary>
    public sealed record ExecutionResult
    {
      public bool             IsSuccess { get; init; } = false;
      public bool             IsFailure => !IsSuccess;
      public StepFunctionData Output    { get; init; } = StepFunctionData.Empty;
      public Exception?       Exception { get; init; } = null;

      public IImmutableList<ExecutionHistory> History { get; init; } = ImmutableList<ExecutionHistory>.Empty;
    }
  }
}