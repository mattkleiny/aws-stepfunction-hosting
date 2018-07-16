using System;
using System.Reflection;
using System.Threading.Tasks;

namespace Amazon.StepFunction
{
  public sealed class StepFunctionHost
  {
    public static readonly StepFunctionHost Empty = new StepFunctionHost();

    public static StepFunctionHost FromAttributedModel(Assembly assembly)
    {
      Check.NotNull(assembly, nameof(assembly));

      throw new NotImplementedException();
    }

    public static StepFunctionHost FromAttributedModel(params Type[] types)
    {
      Check.NotNull(types, nameof(types));
      Check.That(types.Length > 0, "types.Length > 0");

      throw new NotImplementedException();
    }

    public static StepFunctionHost FromTemplates(string stateMachineTemplate)
    {
      Check.NotNullOrEmpty(stateMachineTemplate, nameof(stateMachineTemplate));

      throw new NotImplementedException();
    }

    public static StepFunctionHost FromTemplates(string stateMachineTemplate, string cloudFormationTemplate)
    {
      Check.NotNullOrEmpty(stateMachineTemplate,   nameof(stateMachineTemplate));
      Check.NotNullOrEmpty(cloudFormationTemplate, nameof(cloudFormationTemplate));

      throw new NotImplementedException();
    }

    private StepFunctionHost()
    {
    }

    public Task ExecuteAsync()
    {
      throw new NotImplementedException();
    }
  }

  public class StateDefinition
  {
    public string Type     { get; set; }
    public string Resource { get; set; }
    public string Next     { get; set; }
    public string Default  { get; set; }
  }

  public abstract class State
  {
    private State()
    {
    }

    public Task ExecuteAsync()
    {
      throw new NotImplementedException();
    }

    protected abstract Task ExecuteAsync(Context context);

    protected enum Status
    {
      Success,
      Failure
    }

    protected sealed class Context
    {
      public void CompleteWithStatus(Status failed) => throw new NotImplementedException();
    }

    public sealed class Choice : State
    {
      protected override Task ExecuteAsync(Context context)
      {
        throw new NotImplementedException();
      }
    }

    public sealed class Invoke : State
    {
      protected override Task ExecuteAsync(Context context)
      {
        throw new NotImplementedException();
      }
    }

    public sealed class ParallelInvoke : State
    {
      protected override Task ExecuteAsync(Context context)
      {
        throw new NotImplementedException();
      }
    }

    public sealed class Success : State
    {
      protected override Task ExecuteAsync(Context context)
      {
        context.CompleteWithStatus(Status.Success);

        return Task.CompletedTask;
      }
    }

    public sealed class Fail : State
    {
      protected override Task ExecuteAsync(Context context)
      {
        context.CompleteWithStatus(Status.Failure);

        return Task.CompletedTask;
      }
    }
  }
}