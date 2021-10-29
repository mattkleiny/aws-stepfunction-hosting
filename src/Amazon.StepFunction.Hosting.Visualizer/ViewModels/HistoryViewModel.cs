using System.Collections.ObjectModel;

namespace Amazon.StepFunction.Hosting.ViewModels
{
  /// <summary>A list of history entries for the history window</summary>
  internal sealed class HistoryViewModel : ViewModel
  {
    private ObservableCollection<HistoryEntryViewModel> entries       = new();
    private HistoryEntryViewModel?                      selectedEntry = default;

    public ObservableCollection<HistoryEntryViewModel> Entries
    {
      get => entries;
      set => SetProperty(ref entries, value);
    }

    public HistoryEntryViewModel? SelectedEntry
    {
      get => selectedEntry;
      set => SetProperty(ref selectedEntry, value);
    }
  }
}