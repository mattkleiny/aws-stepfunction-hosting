using System;
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
    private readonly Metadata metadata;

    public static StepFunctionHost FromTemplates(string serverlessTemplate, string cloudFormationTemplate)
    {
      Check.NotNullOrEmpty(serverlessTemplate,     nameof(serverlessTemplate));
      Check.NotNullOrEmpty(cloudFormationTemplate, nameof(cloudFormationTemplate));

      throw new NotImplementedException();
    }

    private StepFunctionHost(Metadata metadata)
    {
      Check.NotNull(metadata, nameof(metadata));

      this.metadata = metadata;
    }

    public Task ExecuteAsync()
    {
      throw new NotImplementedException();
    }

    private sealed class Metadata
    {
    }
  }

  internal enum StateStatus
  {
    Success,
    Failure
  }

  internal class StateMetadata
  {
    public string Type     { get; set; }
    public string Resource { get; set; }
    public string Next     { get; set; }
    public string Default  { get; set; }
  }

  internal abstract class State
  {
    private State()
    {
    }

    public Task ExecuteAsync()
    {
      throw new NotImplementedException();
    }

    protected abstract Task ExecuteAsync(Context context, Action<Task> next);

    protected sealed class Context
    {
      public void CompleteWithStatus(StateStatus failed) => throw new NotImplementedException();
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
        context.CompleteWithStatus(StateStatus.Success);

        return Task.CompletedTask;
      }
    }

    public sealed class Fail : State
    {
      protected override Task ExecuteAsync(Context context, Action<Task> next)
      {
        context.CompleteWithStatus(StateStatus.Failure);

        return Task.CompletedTask;
      }
    }
  }
}