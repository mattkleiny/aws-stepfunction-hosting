using System.Windows;

namespace Amazon.StepFunction.Hosting.Visualizer.ViewModels
{
  public sealed class StepViewModel : ViewModel
  {
    private string name        = string.Empty;
    private string description = string.Empty;
    private Point  location    = default;
    private Size   size        = default;
    private Point  anchor      = default;
    private bool   isActive    = false;
    private bool   isTerminal  = false;

    public string Name
    {
      get => name;
      set => SetProperty(ref name, value);
    }

    public string Description
    {
      get => description;
      set => SetProperty(ref description, value);
    }

    public Point Location
    {
      get => location;
      set => SetProperty(ref location, value);
    }

    public Size Size
    {
      get => size;
      set => SetProperty(ref size, value);
    }

    public Point Anchor
    {
      get => anchor;
      set => SetProperty(ref anchor, value);
    }

    public bool IsActive
    {
      get => isActive;
      set => SetProperty(ref isActive, value);
    }

    public bool IsTerminal
    {
      get => isTerminal;
      set => SetProperty(ref isTerminal, value);
    }
  }
}