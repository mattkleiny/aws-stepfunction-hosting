using System.Windows;

namespace Amazon.StepFunction.Hosting.Visualizer.ViewModels
{
  public sealed class StepViewModel : ViewModel
  {
    private string name        = string.Empty;
    private string description = string.Empty;
    private Point  position    = default;

    public string Name
    {
      get => name;
      set => SetProperty(ref name, value);
    }

    public string Description
    {
      get => name;
      set => SetProperty(ref description, value);
    }

    public Point Position
    {
      get => position;
      set => SetProperty(ref position, value);
    }
  }
}