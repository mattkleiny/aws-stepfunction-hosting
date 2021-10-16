using System.Windows;

namespace Amazon.StepFunction.Hosting.Visualizer.ViewModels
{
  internal sealed class StepViewModel : ViewModel
  {
    private string name         = string.Empty;
    private string description  = string.Empty;
    private Point  location     = default;
    private Size   size         = default;
    private Point  anchor       = default;
    private bool   isActive     = false;
    private bool   isSuccessful = false;
    private bool   isFailed     = false;
    private string data         = string.Empty;

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

    public bool IsSuccessful
    {
      get => isSuccessful;
      set => SetProperty(ref isSuccessful, value);
    }

    public bool IsFailed
    {
      get => isFailed;
      set => SetProperty(ref isFailed, value);
    }

    public string Data
    {
      get => data;
      set => SetProperty(ref data, value);
    }
  }
}