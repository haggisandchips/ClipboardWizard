using ClipboardWizard.Service;
using ClipboardWizard.View;
using ClipboardWizard.ViewModel;
using ClipboardWizard.ViewModel.Command;
using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace ClipboardWizard.Model
{
    public class SnippetViewModel : INotifyPropertyChanged
    {

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

        private bool _locked;
        public bool Locked
        {
            get => _locked;
            set
            {
                _locked = value;
                OnPropertyChanged(nameof(Locked));
            }
        }

        public CopyCommand Copy { get; } = new();

        public DeleteCommand Delete { get; private set; }

        public LockCommand Lock { get; private set; }

        public EditCommand Edit { get; private set; }

        public event PropertyChangedEventHandler PropertyChanged;

        public SnippetViewModel(Snippet snippet, State state)
        {
            Snippet = snippet;
            State = state;
            Locked = snippet.Locked;

            Delete = new(this);
            Lock = new(this);
            Edit = new(this);
        }

        internal void EditSnippet()
        {
            // TODO https://stackoverflow.com/a/40792516/2194201
            Window owner = Application.Current.MainWindow;

            EditSnippetViewModel editSnippetViewModel = new EditSnippetViewModel(Snippet);
            EditSnippetView editSnippetView = new()
            {
                DataContext = editSnippetViewModel,
                Owner = owner,
                Width = Math.Min(owner.ActualWidth * 0.6, 1600),
                Height = Math.Min(owner.ActualHeight * 0.8, 800)
            };

            bool? result = editSnippetView.ShowDialog();

            if ((bool)result)
            {
                Snippet.Description = editSnippetViewModel.Description;
                Snippet.Content = editSnippetViewModel.Content;
                SnippetManager.UpdateSnippet(Snippet);

                // TODO What if we have modified this snippet to contain the same content as another snippet?
                // Could prevent that happening when editing by asking the WizardViewModel?

                if (Snippet.Content.Equals(App.ClipboardMonitor.ClipboardText, StringComparison.Ordinal))
                {
                    State = State.Active;
                }
                else
                {
                    State = State.Inactive;
                }

                OnPropertyChanged(nameof(Snippet));
            }
        }

        internal void DeleteSnippet()
        {
            SnippetManager.DeleteSnippet(Snippet);
            App.DeleteSnippet(this);
        }

        internal void HandleLock()
        {
            if (Snippet.Locked)
            {
                Locked = false;
                _ = ScheduleReLockAsync();
            }
            else
            {
                Snippet.Locked = true;
                Locked = true;
                SnippetManager.UpdateSnippet(Snippet);
            }
        }

        private async Task ScheduleReLockAsync()
        {
            await Task.Delay(TimeSpan.FromSeconds(3.0));
            Locked = true;
            CommandManager.InvalidateRequerySuggested();
        }

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
