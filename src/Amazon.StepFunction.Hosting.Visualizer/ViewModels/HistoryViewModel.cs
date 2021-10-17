using System.Collections.ObjectModel;

namespace Amazon.StepFunction.Hosting.Visualizer.ViewModels
{
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