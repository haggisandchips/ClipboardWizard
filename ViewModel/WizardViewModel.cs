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

        public string ClipboardText => _clipboardMonitor.CurrentText;

        public event PropertyChangedEventHandler PropertyChanged;

        public WizardViewModel(ISnippetRepository repository, IClipboardMonitor clipboardMonitor)
        {
            _repository = repository;
            _clipboardMonitor = clipboardMonitor;

            _clipboardMonitor.TextCopied += ClipboardMonitor_TextCopied;

            SaveClipboardContents = new(this);
            AddSnippet = new(this);
        }

        public async Task LoadAsync()
        {
            List<Snippet> snippets = await _repository.LoadSnippetsAsync();

            foreach (Snippet snippet in snippets)
            {
                State state = IsCurrentClipboardContent(snippet) ? State.Active : State.Inactive;
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

            await CreateSnippetAsync(editSnippetViewModel.Content, editSnippetViewModel.Description);
        }

        internal async Task SaveClipboardSnippetAsync()
        {
            string content = _clipboardMonitor.CurrentText;
            if (string.IsNullOrWhiteSpace(content))
            {
                return;
            }

            await CreateSnippetAsync(content);
        }

        private void ClipboardMonitor_TextCopied(object sender, string content)
        {
            bool nullOrWhitespace = string.IsNullOrWhiteSpace(content);
            bool matched = false;

            foreach (SnippetViewModel snippetViewModel in SnippetViewModels)
            {
                bool equal = !nullOrWhitespace && snippetViewModel.Snippet.Content.Equals(content, StringComparison.Ordinal);
                snippetViewModel.State = equal ? State.Active : State.Inactive;

                matched |= equal;
            }

            if (!nullOrWhitespace && !matched && Recording)
            {
                // Fire-and-forget: this runs off the back of an automatic clipboard event with
                // no user-facing command to report failure through, so it logs instead of
                // throwing back into the clipboard monitor's event.
                _ = TryAutoSaveAsync(content);
            }
        }

        private async Task TryAutoSaveAsync(string content)
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

        private async Task CreateSnippetAsync(string content, string description = null)
        {
            Snippet snippet = new()
            {
                Content = content,
                Description = description,
                Order = GetNextOrder()
            };

            await _repository.SaveSnippetAsync(snippet);

            State state = IsCurrentClipboardContent(snippet) ? State.Active : State.Inactive;
            SnippetViewModels.Add(new SnippetViewModel(snippet, state, this));
            RefreshEdgeFlags();
        }

        private bool IsCurrentClipboardContent(Snippet snippet)
        {
            string content = _clipboardMonitor.CurrentText;
            return !string.IsNullOrWhiteSpace(content) && snippet.Content.Equals(content, StringComparison.Ordinal);
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
