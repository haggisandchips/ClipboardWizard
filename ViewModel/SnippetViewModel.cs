using ClipboardWizard.Model;
using ClipboardWizard.View;
using ClipboardWizard.ViewModel.Command;
using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace ClipboardWizard.ViewModel
{
    public class SnippetViewModel : INotifyPropertyChanged
    {
        private readonly ISnippetHost _host;

        private Snippet _snippet;
        public Snippet Snippet
        {
            get => _snippet;
            set
            {
                _snippet = value;
                OnPropertyChanged(nameof(Snippet));
            }
        }

        private State _state;
        public State State
        {
            get => _state;
            set
            {
                _state = value;
                OnPropertyChanged(nameof(State));
            }
        }

        private bool _protected;

        /// <summary>
        /// Transient, display-only: true while this snippet is protected from deletion.
        /// Distinct from the permanent, persisted Snippet.Locked - this flips to false for a
        /// few seconds after the lock icon is clicked, to give the user a short window to
        /// click delete, then re-locks itself automatically.
        /// </summary>
        public bool Protected
        {
            get => _protected;
            private set
            {
                _protected = value;
                OnPropertyChanged(nameof(Protected));
            }
        }

        public CopyCommand Copy { get; } = new();

        public DeleteCommand Delete { get; }

        public LockCommand Lock { get; }

        public EditCommand Edit { get; }

        public event PropertyChangedEventHandler PropertyChanged;

        public SnippetViewModel(Snippet snippet, State state, ISnippetHost host)
        {
            Snippet = snippet;
            State = state;
            Protected = snippet.Locked;
            _host = host;

            Delete = new(this);
            Edit = new(this);
            Lock = new(this);
        }

        internal Task DeleteSnippetAsync()
        {
            return _host.RemoveSnippetAsync(this);
        }

        internal async Task EditSnippetAsync()
        {
            // The owner must be set before ShowDialog so the dialog centers over it and
            // stays modal to the correct window.
            Window owner = Application.Current.MainWindow;

            EditSnippetViewModel editSnippetViewModel = new(Snippet);
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

            Snippet.Description = editSnippetViewModel.Description;

            // Only Text snippets have editable content; an Image snippet's picture never
            // changes here, so its match-against-clipboard State can't have changed either.
            if (Snippet.Type == SnippetType.Text)
            {
                Snippet.Content = editSnippetViewModel.Content;
                State = string.Equals(Snippet.Content, _host.ClipboardText, StringComparison.Ordinal) ? State.Active : State.Inactive;
            }

            await _host.UpdateSnippetAsync(Snippet);

            OnPropertyChanged(nameof(Snippet));
        }

        /// <summary>Drag-and-drop reordering: moves this snippet immediately before/after <paramref name="target"/>.</summary>
        internal Task MoveToAsync(SnippetViewModel target, bool insertBefore)
        {
            return _host.MoveSnippetToAsync(this, target, insertBefore);
        }

        internal async Task HandleLockAsync()
        {
            if (Snippet.Locked)
            {
                Protected = false;
                _ = ScheduleReProtectAsync();
            }
            else
            {
                Snippet.Locked = true;
                Protected = true;
                await _host.UpdateSnippetAsync(Snippet);
            }
        }

        private async Task ScheduleReProtectAsync()
        {
            await Task.Delay(TimeSpan.FromSeconds(3.0));
            Protected = true;
            CommandManager.InvalidateRequerySuggested();
        }

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
