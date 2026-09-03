using ClipboardWizard.Model;
using ClipboardWizard.ViewModel.Command;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace ClipboardWizard.ViewModel
{
    public class CategoryViewModel : ICategorySection
    {
        private readonly ICategoryHost _host;

        public Category Category { get; }

        public string Name => Category.Name;

        public bool IsExpanded
        {
            get => Category.IsExpanded;
            set => Category.IsExpanded = value;
        }

        public bool IsPinned => false;

        public ObservableCollection<SnippetViewModel> Snippets { get; } = new();

        public DeleteCategoryCommand Delete { get; }

        ICommand ICategorySection.Delete => Delete;

        public AddCategorySnippetCommand AddSnippet { get; }

        ICommand ICategorySection.AddSnippet => AddSnippet;

        public SaveCategoryClipboardContentsCommand SaveClipboardContents { get; }

        ICommand ICategorySection.SaveClipboardContents => SaveClipboardContents;

        public event PropertyChangedEventHandler PropertyChanged;

        public CategoryViewModel(Category category, ICategoryHost host)
        {
            Category = category;
            _host = host;

            Delete = new(this);
            AddSnippet = new(this);
            SaveClipboardContents = new(this);

            // Category.Name/IsExpanded happen to share names with this wrapper's own
            // passthrough properties, so re-raising verbatim keeps bindings live - but with
            // `this` as the sender. WPF's binding/weak-event machinery keys its listener
            // registry by the object bindings were registered against (this CategoryViewModel),
            // so simply forwarding Category's own event (sender = Category) would fire
            // notifications the binding system can't match back to any listener.
            Category.PropertyChanged += (_, e) => PropertyChanged?.Invoke(this, e);
        }

        /// <summary>
        /// Deleting a category isn't a single click: unlike a snippet (which is protected by an
        /// explicit lock step, see SnippetViewModel), a category has no such per-item opt-in, so
        /// every delete confirms here instead.
        /// </summary>
        internal async Task DeleteCategoryAsync()
        {
            MessageBoxResult result = MessageBox.Show(
                $"Delete category \"{Category.Name}\"? Its snippets will become uncategorized, not deleted.",
                "Clipboard Wizard",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes)
            {
                return;
            }

            await _host.DeleteCategoryAsync(this);
        }

        /// <summary>Drag-and-drop reordering: moves this category immediately before/after <paramref name="target"/>.</summary>
        internal Task MoveToAsync(CategoryViewModel target, bool insertBefore)
        {
            return _host.MoveCategoryToAsync(this, target, insertBefore);
        }

        internal Task ToggleExpandedAsync()
        {
            IsExpanded = !IsExpanded;
            return _host.UpdateCategoryAsync(Category);
        }

        /// <summary>Whether the current clipboard contents could be saved as a snippet - backs SaveClipboardContents' enabled state.</summary>
        internal bool HasSaveableClipboardContent => _host.HasSaveableClipboardContent;

        internal Task AddNewSnippetAsync()
        {
            return _host.AddSnippetAsync(this);
        }

        internal Task SaveClipboardSnippetAsync()
        {
            return _host.SaveClipboardSnippetAsync(this);
        }
    }
}
