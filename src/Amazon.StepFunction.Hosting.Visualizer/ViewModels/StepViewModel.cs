using System;
using System.Collections.ObjectModel;
using System.Windows;

namespace Amazon.StepFunction.Hosting.Visualizer.ViewModels
{
  /// <summary>Describes a single step in a Step Function</summary>
  internal class StepViewModel : ViewModel
  {
    private string   type           = string.Empty;
    private string   name           = string.Empty;
    private string   comment        = string.Empty;
    private string   status         = "Inactive";
    private DateTime executedAt     = default;
    private int      executionCount = 0;
    private Point    location       = default;
    private Size     size           = default;
    private Point    anchor         = default;
    private bool     isStart        = false;
    private bool     isActive       = false;
    private bool     isSuccessful   = false;
    private bool     isFailed       = false;
    private bool     isTerminal     = false;

    private ObservableCollection<StepDetailViewModel> details = new();

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

    public string Comment
    {
      get => comment;
      set => SetProperty(ref comment, value);
    }

    public string Status
    {
      get => status;
      set => SetProperty(ref status, value);
    }

    public DateTime ExecutedAt
    {
      get => executedAt;
      set => SetProperty(ref executedAt, value);
    }

    public int ExecutionCount
    {
      get => executionCount;
      set => SetProperty(ref executionCount, value);
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

    public bool IsTerminal
    {
      get => isTerminal;
      set => SetProperty(ref isTerminal, value);
    }

    public ObservableCollection<StepDetailViewModel> Details
    {
      get => details;
      set => SetProperty(ref details, value);
    }

    public void CopyFromHistory(ExecutionHistory history)
    {
      IsSuccessful   = history.IsSuccessful;
      IsFailed       = history.IsFailed;
      ExecutedAt     = history.ExecutedAt;
      ExecutionCount = history.ExecutionCount;

      foreach (var detail in Details)
      {
        detail.CopyFromHistory(history);
      }
    }
  }
}