using ClipboardWizard.Service;
using ClipboardWizard.ViewModel.Command;
using FontAwesome5;
using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Input;

namespace ClipboardWizard.Model
{
    public class SnippetViewModel : INotifyPropertyChanged
    {

        private Snippet _snippet;
        public Snippet Snippet
        {
            get { return _snippet; }
            set
            {
                _snippet = value;
                OnPropertyChanged(nameof(Snippet));
            }
        }

        private State _state;
        public State State
        {
            get { return _state; }
            set
            {
                _state = value;
                OnPropertyChanged(nameof(State));
            }
        }

        private bool _locked;

        public bool Locked
        {
            get { return _locked; }
            set
            {
                _locked = value;
                OnPropertyChanged(nameof(Locked));
            }
        }

        public CopyCommand Copy { get; } = new();

        public DeleteCommand Delete { get; private set; }

        public LockCommand Lock { get; private set; }

        public event PropertyChangedEventHandler PropertyChanged;

        public SnippetViewModel(Snippet snippet, State state)
        {
            Snippet = snippet;
            State = state;
            Locked = snippet.Locked;

            Delete = new(this);
            Lock = new(this);
        }

        internal void DeleteSnippet()
        {
            SnippetManager.DeleteSnippet(Snippet);
            App.DeleteSnippet(this);
        }

        internal async Task HandleLockAsync()
        {
            // If unlocked then lock and save snippet and lock view model
            // If locked then unlock view model and schedule relock after n seconds
            if (Snippet.Locked)
            {
                Locked = false;
                // TODO Schedule relock
                await Task.Delay(TimeSpan.FromSeconds(3.0));
                Locked = true;
                CommandManager.InvalidateRequerySuggested();
            }
            else
            {
                Snippet.Locked = true;
                Locked = true;
                SnippetManager.UpdateSnippet(Snippet);
            }
        }

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
