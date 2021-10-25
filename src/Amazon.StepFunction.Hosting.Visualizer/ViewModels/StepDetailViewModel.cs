using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

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

  /// <summary>Collects diff details for use as a <see cref="IStepFunctionDetailCollector"/> and <see cref="IStepDetailProvider"/>.</summary>
  public abstract class StepFunctionDiffCollector : IStepFunctionDetailCollector, IStepDetailProvider
  {
    public abstract string TabName { get; }

    protected abstract Task<string> GetDetailsForStep(string stepName, StepFunctionData data);

    async Task<object> IStepFunctionDetailCollector.OnBeforeExecuteStep(string stepName, StepFunctionData input)
    {
      return await GetDetailsForStep(stepName, input);
    }

    async Task<object> IStepFunctionDetailCollector.OnAfterExecuteStep(string stepName, StepFunctionData output)
    {
      return await GetDetailsForStep(stepName, output);
    }

    void IStepFunctionDetailCollector.AugmentHistory(object beforeDetails, object afterDetails, ExecutionHistory history)
    {
      history.UserData.Add(new Details(
        Type: GetType(),
        Before: (string) beforeDetails,
        After: (string) afterDetails
      ));
    }

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

    private sealed record Details(Type Type, string Before, string After);
  }
}