using ClipboardWizard.Service;
using ClipboardWizard.ViewModel.Command;
using System;
using System.ComponentModel;

namespace ClipboardWizard.Model
{
    public class SnippetViewModel : INotifyPropertyChanged
    {

        private Snippet snippet;
        public Snippet Snippet
        {
            get { return snippet; }
            set {
                snippet = value;
                OnPropertyChanged(nameof(Snippet));
            }
        }

        private State state;
        public State State
        {
            get { return state; }
            set
            {
                state = value;
                OnPropertyChanged(nameof(State));
            }
        }

        public CopyCommand Copy { get; } = new();

        public DeleteCommand Delete { get; private set; }

        public event PropertyChangedEventHandler PropertyChanged;

        public SnippetViewModel(Snippet snippet, State state)
        {
            Snippet = snippet;
            State = state;

            Delete = new(this);
        }

        internal void DeleteSnippet(SnippetViewModel snippetViewModel)
        {
            SnippetManager.DeleteSnippet(snippetViewModel.Snippet);
            App.DeleteSnippet(snippetViewModel);
        }

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
