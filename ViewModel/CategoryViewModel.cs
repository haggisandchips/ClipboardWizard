using ClipboardWizard.Model;
using ClipboardWizard.Service.Firestore;
using ClipboardWizard.View;
using ClipboardWizard.ViewModel.Command;
using System;
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
        private readonly IFirestoreStatusProvider _firestoreStatus;

        public Category Category { get; }

        public string Name => Category.Name;

        public bool IsExpanded
        {
            get => Category.IsExpanded;
            set => Category.IsExpanded = value;
        }

        public bool IsPinned => false;

        /// <summary>Drives the category header's "Firebase setup required" warning icon - true only when this category opted into sharing but the app isn't actually connected.</summary>
        public bool NeedsFirebaseSetup => Category.Shared && _firestoreStatus.State != FirestoreConnectionState.Connected;

        /// <summary>Drives the header's flame icon.</summary>
        public bool IsShared => Category.Shared;

        public ObservableCollection<SnippetViewModel> Snippets { get; } = new();

        public DeleteCategoryCommand Delete { get; }

        public EditCategoryCommand Edit { get; }

        ICommand ICategorySection.Delete => Delete;

        public AddCategorySnippetCommand AddSnippet { get; }

        ICommand ICategorySection.AddSnippet => AddSnippet;

        public SaveCategoryClipboardContentsCommand SaveClipboardContents { get; }

        ICommand ICategorySection.SaveClipboardContents => SaveClipboardContents;

        ICommand ICategorySection.Edit => Edit;

        public event PropertyChangedEventHandler PropertyChanged;

        public CategoryViewModel(Category category, ICategoryHost host, IFirestoreStatusProvider firestoreStatus)
        {
            Category = category;
            _host = host;
            _firestoreStatus = firestoreStatus;

            Delete = new(this);
            AddSnippet = new(this);
            SaveClipboardContents = new(this);
            Edit = new(this);

            // Category.Name/IsExpanded happen to share names with this wrapper's own
            // passthrough properties, so re-raising verbatim keeps bindings live - but with
            // `this` as the sender. WPF's binding/weak-event machinery keys its listener
            // registry by the object bindings were registered against (this CategoryViewModel),
            // so simply forwarding Category's own event (sender = Category) would fire
            // notifications the binding system can't match back to any listener.
            Category.PropertyChanged += (_, e) =>
            {
                PropertyChanged?.Invoke(this, e);
                if (e.PropertyName == nameof(Category.Shared))
                {
                    OnPropertyChanged(nameof(NeedsFirebaseSetup));
                    OnPropertyChanged(nameof(IsShared));
                }
            };

            _firestoreStatus.StateChanged += (_, _) => OnPropertyChanged(nameof(NeedsFirebaseSetup));
        }

        /// <summary>
        /// Deleting a category isn't a single click: unlike a snippet (which is protected by an
        /// explicit lock step, see SnippetViewModel), a category has no such per-item opt-in, so
        /// every delete confirms here instead. A Shared category gets a second prompt asking
        /// whether to also remove it from Firebase - distinct from un-sharing (the Shared
        /// checkbox), which never touches already-pushed Firebase data. That second prompt is
        /// YesNoCancel rather than YesNo: Cancel aborts the whole deletion (category included),
        /// since by this point the user may have realised they don't want to delete the category
        /// at all, not just be choosing whether Firebase is included.
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

            bool alsoDeleteFromFirebase = false;
            if (Category.Shared)
            {
                MessageBoxResult firebaseResult = MessageBox.Show(
                    "This category is Shared. Also delete it from Firebase, for every device? Choosing No leaves the cloud copy in place. Choosing Cancel leaves the category itself in place too.",
                    "Clipboard Wizard",
                    MessageBoxButton.YesNoCancel,
                    MessageBoxImage.Warning);

                if (firebaseResult == MessageBoxResult.Cancel)
                {
                    return;
                }

                alsoDeleteFromFirebase = firebaseResult == MessageBoxResult.Yes;
            }

            await _host.DeleteCategoryAsync(this, alsoDeleteFromFirebase);
        }

        internal async Task EditCategoryAsync()
        {
            Window owner = Application.Current.MainWindow;

            AddCategoryViewModel editViewModel = new(Category, _firestoreStatus);
            AddCategoryView editView = new()
            {
                DataContext = editViewModel,
                Owner = owner
            };

            bool? result = editView.ShowDialog();
            if (result != true)
            {
                return;
            }

            await _host.ApplyCategoryEditAsync(this, editViewModel.Name, editViewModel.Shared);
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

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
