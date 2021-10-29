using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Amazon.StepFunction.Hosting.ViewModels
{
  /// <summary>Base class for any bindable view model for use in WPF</summary>
  internal abstract class ViewModel : INotifyPropertyChanged
  {
    public event PropertyChangedEventHandler? PropertyChanged;

    public bool SetProperty<T>(ref T reference, T value, [CallerMemberName] in string propertyName = default!)
    {
      if (!Equals(reference, value))
      {
        reference = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        return true;
      }

      return false;
    }
  }
}