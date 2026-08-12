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

        private bool _first;
        public bool First
        {
            get => _first;
            set
            {
                _first = value;
                OnPropertyChanged(nameof(First));
            }
        }

        private bool _last;
        public bool Last
        {
            get => _last;
            set
            {
                _last = value;
                OnPropertyChanged(nameof(Last));
            }
        }

        public CopyCommand Copy { get; } = new();

        public DeleteCommand Delete { get; }

        public LockCommand Lock { get; }

        public EditCommand Edit { get; }

        public OrderHigherCommand OrderHigher { get; }

        public OrderLowerCommand OrderLower { get; }

        public event PropertyChangedEventHandler PropertyChanged;

        public SnippetViewModel(Snippet snippet, State state, ISnippetHost host)
        {
            Snippet = snippet;
            State = state;
            Protected = snippet.Locked;
            _host = host;

            Delete = new(this);
            Edit = new(this);
            OrderHigher = new(this);
            OrderLower = new(this);
            Lock = new(this);
        }

        internal Task DeleteSnippetAsync()
        {
            return _host.RemoveSnippetAsync(this);
        }

        internal async Task EditSnippetAsync()
        {
            // Defense in depth: EditCommand.CanExecute already blocks this from the UI, but
            // there's no sensible way to edit an image's pixels in a text box, so refuse it
            // here too rather than trusting the command binding alone.
            if (Snippet.Type != SnippetType.Text)
            {
                return;
            }

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
            Snippet.Content = editSnippetViewModel.Content;

            await _host.UpdateSnippetAsync(Snippet);

            State = Snippet.Content.Equals(_host.ClipboardText, StringComparison.Ordinal) ? State.Active : State.Inactive;

            OnPropertyChanged(nameof(Snippet));
        }

        internal Task OrderSnippetHigherAsync()
        {
            return _host.MoveSnippetUpAsync(this);
        }

        internal Task OrderSnippetLowerAsync()
        {
            return _host.MoveSnippetDownAsync(this);
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
