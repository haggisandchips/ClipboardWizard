using ClipboardWizard.Model;
using ClipboardWizard.Service;
using ClipboardWizard.View;
using ClipboardWizard.ViewModel.Command;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace ClipboardWizard.ViewModel
{
    public class WizardViewModel : INotifyPropertyChanged, ISnippetHost
    {
        private readonly ISnippetRepository _repository;
        private readonly IClipboardMonitor _clipboardMonitor;

        public ObservableCollection<SnippetViewModel> SnippetViewModels { get; } = new();

        private bool _recording;
        public bool Recording
        {
            get => _recording;
            set
            {
                _recording = value;
                OnPropertyChanged(nameof(Recording));
            }
        }

        public SaveClipboardContentsCommand SaveClipboardContents { get; }

        public AddSnippetCommand AddSnippet { get; }

        /// <summary>Text content of the clipboard, for the (text-only) edit dialog's Active check.</summary>
        public string ClipboardText => _clipboardMonitor.CurrentContent is { Type: ClipboardContentType.Text } content ? content.Text ?? string.Empty : string.Empty;

        public bool HasSaveableClipboardContent => IsSaveable(_clipboardMonitor.CurrentContent);

        public event PropertyChangedEventHandler PropertyChanged;

        public WizardViewModel(ISnippetRepository repository, IClipboardMonitor clipboardMonitor)
        {
            _repository = repository;
            _clipboardMonitor = clipboardMonitor;

            _clipboardMonitor.ContentCopied += ClipboardMonitor_ContentCopied;

            SaveClipboardContents = new(this);
            AddSnippet = new(this);
        }

        public async Task LoadAsync()
        {
            List<Snippet> snippets = await _repository.LoadSnippetsAsync();
            ClipboardContent current = _clipboardMonitor.CurrentContent;

            foreach (Snippet snippet in snippets)
            {
                State state = Matches(snippet, current) ? State.Active : State.Inactive;
                SnippetViewModels.Add(new SnippetViewModel(snippet, state, this));
            }

            RefreshEdgeFlags();
        }

        internal async Task AddNewSnippetAsync()
        {
            // The owner must be set before ShowDialog so the dialog centers over it and
            // stays modal to the correct window.
            Window owner = Application.Current.MainWindow;

            EditSnippetViewModel editSnippetViewModel = new();
            EditSnippetView editSnippetView = new()
            {
                DataContext = editSnippetViewModel,
                Owner = owner,
                Width = Math.Min(owner.ActualWidth * 0.6, 1600),
                Height = Math.Min(owner.ActualHeight * 0.8, 800)
            };

            bool? result = editSnippetView.ShowDialog();

            if (result != true)
            {
                return;
            }

            ClipboardContent content = new() { Type = ClipboardContentType.Text, Text = editSnippetViewModel.Content };
            await CreateSnippetAsync(content, editSnippetViewModel.Description);
        }

        internal Task SaveClipboardSnippetAsync()
        {
            ClipboardContent content = _clipboardMonitor.CurrentContent;
            return IsSaveable(content) ? CreateSnippetAsync(content) : Task.CompletedTask;
        }

        private void ClipboardMonitor_ContentCopied(object sender, ClipboardContent content)
        {
            bool matched = false;

            foreach (SnippetViewModel snippetViewModel in SnippetViewModels)
            {
                bool equal = Matches(snippetViewModel.Snippet, content);
                snippetViewModel.State = equal ? State.Active : State.Inactive;

                matched |= equal;
            }

            if (!matched && Recording && IsSaveable(content))
            {
                // Fire-and-forget: this runs off the back of an automatic clipboard event with
                // no user-facing command to report failure through, so it logs instead of
                // throwing back into the clipboard monitor's event.
                _ = TryAutoSaveAsync(content);
            }
        }

        private async Task TryAutoSaveAsync(ClipboardContent content)
        {
            try
            {
                await CreateSnippetAsync(content);
            }
            catch (Exception ex)
            {
                Logger.LogError(nameof(TryAutoSaveAsync), ex);
            }
        }

        private async Task CreateSnippetAsync(ClipboardContent content, string description = null)
        {
            Snippet snippet = new()
            {
                Type = content.Type == ClipboardContentType.Image ? SnippetType.Image : SnippetType.Text,
                Content = content.Text,
                ImageData = content.ImageData,
                Description = description,
                Order = GetNextOrder()
            };

            await _repository.SaveSnippetAsync(snippet);

            State state = Matches(snippet, _clipboardMonitor.CurrentContent) ? State.Active : State.Inactive;
            SnippetViewModels.Add(new SnippetViewModel(snippet, state, this));
            RefreshEdgeFlags();
        }

        private static bool IsSaveable(ClipboardContent content)
        {
            return content switch
            {
                { Type: ClipboardContentType.Text, Text: var text } => !string.IsNullOrWhiteSpace(text),
                { Type: ClipboardContentType.Image, ImageData: var data } => data is { Length: > 0 },
                _ => false
            };
        }

        private static bool Matches(Snippet snippet, ClipboardContent content)
        {
            if (!IsSaveable(content))
            {
                return false;
            }

            return snippet.Type switch
            {
                SnippetType.Text => content.Type == ClipboardContentType.Text
                    && string.Equals(snippet.Content, content.Text, StringComparison.Ordinal),
                SnippetType.Image => content.Type == ClipboardContentType.Image
                    && snippet.ImageData != null
                    && content.ImageData.AsSpan().SequenceEqual(snippet.ImageData),
                _ => false
            };
        }

        public Task UpdateSnippetAsync(Snippet snippet)
        {
            return _repository.UpdateSnippetAsync(snippet);
        }

        public async Task RemoveSnippetAsync(SnippetViewModel snippetViewModel)
        {
            await _repository.DeleteSnippetAsync(snippetViewModel.Snippet);
            SnippetViewModels.Remove(snippetViewModel);
            RefreshEdgeFlags();
        }

        public Task MoveSnippetUpAsync(SnippetViewModel snippetViewModel) => MoveAsync(snippetViewModel, -1);

        public Task MoveSnippetDownAsync(SnippetViewModel snippetViewModel) => MoveAsync(snippetViewModel, +1);

        private async Task MoveAsync(SnippetViewModel snippetViewModel, int offset)
        {
            int currentIndex = SnippetViewModels.IndexOf(snippetViewModel);
            int otherIndex = currentIndex + offset;

            if (currentIndex < 0 || otherIndex < 0 || otherIndex >= SnippetViewModels.Count)
            {
                return;
            }

            SnippetViewModel other = SnippetViewModels[otherIndex];

            (snippetViewModel.Snippet.Order, other.Snippet.Order) = (other.Snippet.Order, snippetViewModel.Snippet.Order);

            await _repository.UpdateSnippetAsync(snippetViewModel.Snippet);
            await _repository.UpdateSnippetAsync(other.Snippet);

            SnippetViewModels[currentIndex] = other;
            SnippetViewModels[otherIndex] = snippetViewModel;

            RefreshEdgeFlags();
        }

        public async Task MoveSnippetToAsync(SnippetViewModel snippetViewModel, SnippetViewModel targetSnippetViewModel, bool insertBefore)
        {
            int currentIndex = SnippetViewModels.IndexOf(snippetViewModel);
            int targetIndex = SnippetViewModels.IndexOf(targetSnippetViewModel);

            if (currentIndex < 0 || targetIndex < 0 || currentIndex == targetIndex)
            {
                return;
            }

            // ObservableCollection.Move(old, new) removes at `old` first, which shifts
            // everything after it left by one - so the same `new` index lands *before* the
            // target when moving backward but *after* it when moving forward, unless
            // corrected for here. insertBefore must mean the same thing regardless of which
            // direction the item is dragged from.
            int desiredIndex = insertBefore ? targetIndex : targetIndex + 1;
            if (currentIndex < desiredIndex)
            {
                desiredIndex--;
            }

            if (desiredIndex == currentIndex)
            {
                return;
            }

            SnippetViewModels.Move(currentIndex, desiredIndex);

            // Renumbering the whole list (rather than only shifting the items between the old
            // and new position) is simpler to get right for an arbitrary-distance move, and
            // this list is small enough that the extra writes don't matter. Only snippets whose
            // Order actually changed get persisted.
            List<Task> updates = new();
            for (int i = 0; i < SnippetViewModels.Count; i++)
            {
                Snippet snippet = SnippetViewModels[i].Snippet;
                if (snippet.Order != i)
                {
                    snippet.Order = i;
                    updates.Add(_repository.UpdateSnippetAsync(snippet));
                }
            }
            await Task.WhenAll(updates);

            RefreshEdgeFlags();
        }

        private void RefreshEdgeFlags()
        {
            for (int i = 0; i < SnippetViewModels.Count; i++)
            {
                SnippetViewModels[i].First = i == 0;
                SnippetViewModels[i].Last = i == SnippetViewModels.Count - 1;
            }
        }

        private int GetNextOrder()
        {
            return SnippetViewModels.Count == 0 ? 0 : SnippetViewModels.Max(svm => svm.Snippet.Order) + 1;
        }

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
