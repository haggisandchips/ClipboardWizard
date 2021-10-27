using ClipboardWizard.ViewModel.Command;
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

        public CopyCommand Copy {
            get; 
        } = new();

        public event PropertyChangedEventHandler PropertyChanged;

        public SnippetViewModel(Snippet snippet, State state)
        {
            Snippet = snippet;
            State = state;
        }

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
