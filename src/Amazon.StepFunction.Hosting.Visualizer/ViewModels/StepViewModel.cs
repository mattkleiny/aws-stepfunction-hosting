using System.Windows;

namespace Amazon.StepFunction.Hosting.Visualizer.ViewModels
{
  internal sealed class StepViewModel : ViewModel
  {
    private string type         = string.Empty;
    private string name         = string.Empty;
    private string description  = string.Empty;
    private string status       = "Inactive";
    private string executedAt   = string.Empty;
    private Point  location     = default;
    private Size   size         = default;
    private Point  anchor       = default;
    private bool   isStart      = false;
    private bool   isActive     = false;
    private bool   isSuccessful = false;
    private bool   isFailed     = false;
    private string data         = string.Empty;

    public string Type
    {
      get => type;
      set => SetProperty(ref type, value);
    }

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

    public string Status
    {
      get => status;
      set => SetProperty(ref status, value);
    }

    public string ExecutedAt
    {
      get => executedAt;
      set => SetProperty(ref executedAt, value);
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

    public bool IsStart
    {
      get => isStart;
      set => SetProperty(ref isStart, value);
    }

    public bool IsActive
    {
      get => isActive;
      set
      {
        SetProperty(ref isActive, value);

        if (value)
        {
          Status = "Active";
        }
      }
    }

    public bool IsSuccessful
    {
      get => isSuccessful;
      set
      {
        SetProperty(ref isSuccessful, value);

        if (value)
        {
          Status = "Success";
        }
      }
    }

    public bool IsFailed
    {
      get => isFailed;
      set
      {
        SetProperty(ref isFailed, value);

        if (value)
        {
          Status = "Failed";
        }
      }
    }

    public string Data
    {
      get => data;
      set => SetProperty(ref data, value);
    }

    public void CopyFromHistory(ExecutionHistory history)
    {
      Data         = history.Data.Cast<string>() ?? string.Empty;
      IsSuccessful = history.IsSuccessful;
      IsFailed     = history.IsFailed;
      ExecutedAt   = history.OccurredAt.ToString("h:mm:ss tt");
    }
  }
}