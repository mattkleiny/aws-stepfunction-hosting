namespace Amazon.StepFunction.Hosting.ViewModels
{
  /// <summary>Describes a connection between two steps in a step function</summary>
  internal sealed class ConnectionViewModel : ViewModel
  {
    private StepViewModel? source;
    private StepViewModel? target;

    public StepViewModel? Source
    {
      get => source;
      set => SetProperty(ref source, value);
    }

    public StepViewModel? Target
    {
      get => target;
      set => SetProperty(ref target, value);
    }
  }
}