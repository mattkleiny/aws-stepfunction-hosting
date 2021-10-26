using System;
using System.Diagnostics;
using System.Linq;

namespace Amazon.StepFunction.Hosting.Visualizer.ViewModels
{
  /// <summary>Provides extra details to the Step Function visualizer to aid in debugging.</summary>
  public interface IStepDetailProvider
  {
    string TabName { get; }

    string GetInputData(ExecutionHistory history);
    string GetOutputData(ExecutionHistory history);
  }

  internal sealed class StepDetailViewModel : ViewModel
  {
    private readonly IStepDetailProvider provider;

    private string tabName    = string.Empty;
    private string inputData  = string.Empty;
    private string outputData = string.Empty;

    public StepDetailViewModel(IStepDetailProvider provider)
    {
      this.provider = provider;

      TabName = provider.TabName;
    }

    public string TabName
    {
      get => tabName;
      set => SetProperty(ref tabName, value);
    }

    public string InputData
    {
      get => inputData;
      set => SetProperty(ref inputData, value);
    }

    public string OutputData
    {
      get => outputData;
      set => SetProperty(ref outputData, value);
    }

    public void CopyFromHistory(ExecutionHistory history)
    {
      try
      {
        InputData  = provider.GetInputData(history);
        OutputData = provider.GetOutputData(history);
      }
      catch (Exception exception)
      {
        Debug.WriteLine(exception);
      }
    }
  }

  /// <summary>Provides Step Function input/output diff details to the visualizer.</summary>
  internal sealed class InputOutputDetailProvider : IStepDetailProvider
  {
    public string TabName => "Input/Output";

    public string GetInputData(ExecutionHistory history)
    {
      return history.InputData.ToIndentedString();
    }

    public string GetOutputData(ExecutionHistory history)
    {
      return history.OutputData.ToIndentedString();
    }
  }

  /// <summary>A <see cref="StepDiffCollector"/> that also provides details on detail tabs.</summary>
  public abstract class StepDiffDetailProvider : StepDiffCollector, IStepDetailProvider
  {
    public abstract string TabName { get; }

    string IStepDetailProvider.GetInputData(ExecutionHistory history)
    {
      var diff = history.UserData.OfType<Details>().FirstOrDefault(_ => _.Type == GetType());

      if (diff != null)
      {
        return diff.Before;
      }

      return string.Empty;
    }

    string IStepDetailProvider.GetOutputData(ExecutionHistory history)
    {
      var diff = history.UserData.OfType<Details>().FirstOrDefault(_ => _.Type == GetType());

      if (diff != null)
      {
        return diff.After;
      }

      return string.Empty;
    }
  }
}