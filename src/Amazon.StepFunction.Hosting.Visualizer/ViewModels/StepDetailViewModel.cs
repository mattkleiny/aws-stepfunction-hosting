using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace Amazon.StepFunction.Hosting.Visualizer.ViewModels
{
  /// <summary>Provides extra details to the Step Function visualizer to aid in debugging.</summary>
  public interface IStepDetailProvider
  {
    string TabName { get; }

    Task<string> GetInputDataAsync(ExecutionHistory history);
    Task<string> GetOutputDataAsync(ExecutionHistory history);
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

    public async void CopyFromHistory(ExecutionHistory history)
    {
      try
      {
        var inputData  = await provider.GetInputDataAsync(history);
        var outputData = await provider.GetOutputDataAsync(history);

        Dispatcher.CurrentDispatcher.Invoke(() =>
        {
          InputData  = inputData;
          OutputData = outputData;
        });
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

    public Task<string> GetInputDataAsync(ExecutionHistory history)
    {
      return Task.FromResult(history.InputData.ToIndentedString());
    }

    public Task<string> GetOutputDataAsync(ExecutionHistory history)
    {
      return Task.FromResult(history.OutputData.ToIndentedString());
    }
  }
}