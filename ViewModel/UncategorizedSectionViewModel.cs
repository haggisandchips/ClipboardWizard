using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;

namespace ClipboardWizard.ViewModel
{
    /// <summary>
    /// The pinned bucket for snippets with no category. Not backed by a Category row - always
    /// rendered last, never deletable or reorderable. Its expanded state isn't domain data, so
    /// it's persisted alongside window placement (see WindowSettings) rather than in SQLite.
    /// </summary>
    public class UncategorizedSectionViewModel : ICategorySection
    {
        public string Name => "Uncategorized";

        private bool _isExpanded = true;
        public bool IsExpanded
        {
            get => _isExpanded;
            set
            {
                if (_isExpanded == value)
                {
                    return;
                }

                _isExpanded = value;
                OnPropertyChanged(nameof(IsExpanded));
            }
        }

        public bool IsPinned => true;

        public ObservableCollection<SnippetViewModel> Snippets { get; } = new();

        public ICommand Delete => null;

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
