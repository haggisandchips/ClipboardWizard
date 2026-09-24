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
        /// every delete confirms here instead. All-or-nothing: deleting a Shared category also
        /// deletes it from Firestore - Shared can only be set when a category is created (see
        /// AddCategoryViewModel) and never toggled off, so there's no separate "un-share" action
        /// this could be confused with. A Shared category's snippets are deleted outright too,
        /// not uncategorized (see WizardViewModel.DeleteCategoryAsync) - that's what every other
        /// device sharing it ends up doing on the same delete, so a second, stronger prompt
        /// spells that out before it happens.
        /// </summary>
        internal async Task DeleteCategoryAsync()
        {
            string message = Category.Shared
                ? $"Delete category \"{Category.Name}\"? It's Shared, so this also deletes it remotely for every device sharing it."
                : $"Delete category \"{Category.Name}\"? This permanently deletes it from the database. Its snippets will become uncategorized, not deleted.";

            MessageBoxResult result = MessageBox.Show(
                message,
                "Clipboard Wizard",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes)
            {
                return;
            }

            if (Category.Shared && Snippets.Count > 0)
            {
                MessageBoxResult snippetsResult = MessageBox.Show(
                    $"This also permanently deletes all {Snippets.Count} snippet(s) in \"{Category.Name}\" - on every device sharing it, not just here. This cannot be undone. Are you REALLY sure?",
                    "Clipboard Wizard",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (snippetsResult != MessageBoxResult.Yes)
                {
                    return;
                }
            }

            await _host.DeleteCategoryAsync(this);
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

            await _host.ApplyCategoryEditAsync(this, editViewModel.Name);
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
