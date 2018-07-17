using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using Amazon.StepFunction.Definition;

namespace Amazon.StepFunction
{
  // TODO: support an attributed object model 
  // TODO: add a tool to build state machine language from IEnumerable saga trace
  // TODO: don't forget about getting flexible information out of the execution results
  // TODO: don't forget integration testing scenarios (IStepUnderTest<T> and the like)
  // TODO: create a definition per step type and generify over a type bound?
  // TODO: ferry json input around the execution, as that is the native process in AWS.

  /// <summary>Defines a host capable of executing AWS StepFunction state machines locally.</summary>
  public sealed class StepFunctionHost
  {
    /// <summary>Creates a <see cref="StepFunctionHost"/> from the given state machine specification and <see cref="StepHandlerFactory"/>.</summary>
    public static StepFunctionHost FromJson(string specification, StepHandlerFactory factory)
    {
      Check.NotNullOrEmpty(specification, nameof(specification));
      Check.NotNull(factory, nameof(factory));

      var definition = MachineDefinition.Parse(specification);

      return new StepFunctionHost(definition, factory);
    }

    private StepFunctionHost(MachineDefinition definition, StepHandlerFactory factory)
    {
      Check.NotNull(definition, nameof(definition));
      Check.NotNull(factory,    nameof(factory));

      Definition = definition;

      Steps       = definition.Steps.Select(step => Step.Create(step, factory)).ToArray();
      StepsByName = Steps.ToDictionary(step => step.Name, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>The underlying <see cref="MachineDefinition"/> used to derive this host.</summary>
    public MachineDefinition Definition { get; }

    /// <summary>A maximum period for wait tasks</summary>
    public TimeSpan? MaxWaitDuration { get; set; }

    /// <summary>A list of <see cref="Step"/>s that back this step function.</summary>
    internal IReadOnlyList<Step> Steps { get; }

    /// <summary>Permits looking up <see cref="Step"/>s by name.</summary>
    internal IReadOnlyDictionary<string, Step> StepsByName { get; }

    /// <summary>The initial step to use when executing the step function.</summary>
    internal Step InitialStep => Steps[0];

    /// <summary>Executes the step function from it's <see cref="InitialStep"/>.</summary>
    public async Task<ExecutionResult> ExecuteAsync(object input = null, CancellationToken cancellationToken = default)
    {
      var output = await ExecuteAsync(InitialStep, input, cancellationToken);

      return new ExecutionResult(output);
    }

    // TODO: convert this into a trampoline
    /// <summary>Executes the given <see cref="Step"/> recursively.</summary>
    private async Task<object> ExecuteAsync(Step step, object input, CancellationToken cancellationToken = default)
    {
      TimeSpan Min(TimeSpan a, TimeSpan? b) => b.HasValue ? (a < b ? a : b.Value) : a;

      object output = null;

      foreach (var transition in await step.ExecuteAsync(input, cancellationToken))
      {
        switch (transition)
        {
          case Transition.Next next:
            output = await ExecuteAsync(StepsByName[next.Name], next.Input, cancellationToken);
            break;

          case Transition.Wait wait:
            await Task.Delay(Min(wait.Duration, MaxWaitDuration), cancellationToken);
            break;

          case Transition.Succeed succeed:
            return succeed.Output;

          case Transition.Fail fail:
            // TODO: do something better here
            ExceptionDispatchInfo.Capture(fail.Exception).Throw();
            break;

          default:
            throw new InvalidOperationException("An unrecognized transition was provided: " + transition);
        }
      }

      return output;
    }

    /// <summary>Encapsulates the result of a step function execution.</summary>
    public sealed class ExecutionResult
    {
      public ExecutionResult(object output)
      {
        Output = output;
      }

      public object Output { get; }

      public bool IsSuccess { get; } = true;
      public bool IsFailure => !IsSuccess;

      public Exception Exception { get; }
    }
  }
}