using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Amazon.StepFunction.Hosting.Visualizer.ViewModels
{
  public abstract class ViewModel : INotifyPropertyChanged
  {
    public event PropertyChangedEventHandler? PropertyChanged;

    private bool isDirty;

    public bool IsDirty
    {
      get => isDirty;
      protected set
      {
        if (isDirty != value)
        {
          isDirty = value;
          OnPropertyChanged();
        }
      }
    }

    public bool SetProperty<T>(ref T reference, T value, [CallerMemberName] in string propertyName = default!)
    {
      if (!Equals(reference, value))
      {
        reference = value;
        IsDirty   = true;

        OnPropertyChanged(propertyName);
        return true;
      }

      return false;
    }

    protected void OnPropertyChanged([CallerMemberName] in string? propertyName = default)
    {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
  }
}