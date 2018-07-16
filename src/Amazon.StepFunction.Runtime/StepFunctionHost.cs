using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Amazon.StepFunction.Runtime
{
  public static class Program
  {
    public static async Task Main(string[] args)
    {
      var host = StepFunctionHost.FromTemplates(
        serverlessTemplate: "serverless.template",
        cloudFormationTemplate: "cloudformation.template"
      );

      await host.ExecuteAsync();
    }
  }

  public sealed class StepFunctionHost
  {
    public static StepFunctionHost FromTemplates(string serverlessTemplate, string cloudFormationTemplate)
    {
      Check.NotNullOrEmpty(serverlessTemplate,     nameof(serverlessTemplate));
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

  public class StateDescriptor
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

    protected abstract Task ExecuteAsync(Context context, Action<Task> next);

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
      protected override Task ExecuteAsync(Context context, Action<Task> next)
      {
        throw new NotImplementedException();
      }
    }

    public sealed class Invoke : State
    {
      protected override Task ExecuteAsync(Context context, Action<Task> next)
      {
        throw new NotImplementedException();
      }
    }

    public sealed class ParallelInvoke : State
    {
      protected override Task ExecuteAsync(Context context, Action<Task> next)
      {
        throw new NotImplementedException();
      }
    }

    public sealed class Success : State
    {
      protected override Task ExecuteAsync(Context context, Action<Task> next)
      {
        context.CompleteWithStatus(Status.Success);

        return Task.CompletedTask;
      }
    }

    public sealed class Fail : State
    {
      protected override Task ExecuteAsync(Context context, Action<Task> next)
      {
        context.CompleteWithStatus(Status.Failure);

        return Task.CompletedTask;
      }
    }
  }

  public interface IEffect
  {
  }

  public static class Effects
  {
    public static IEffect Anonymous(Action body) => throw new NotImplementedException();
    public static IEffect Skip()                 => throw new NotImplementedException();
  }
}