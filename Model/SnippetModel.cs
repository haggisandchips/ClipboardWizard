using ClipboardWizard.ViewModel.Command;
using System.ComponentModel;

namespace ClipboardWizard.Model
{
    public class SnippetModel : INotifyPropertyChanged
    {

        private string content;
        public string Content
        {
            get { return content; }
            set {
                content = value;
                OnPropertyChanged(nameof(Content));
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

        public CopyCommand CopyCommand { get; } = new();

        public event PropertyChangedEventHandler PropertyChanged;

        public SnippetModel(string content, State state)
        {
            Content = content;
            State = state;
        }

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
