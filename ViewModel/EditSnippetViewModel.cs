using ClipboardWizard.Model;
using System.ComponentModel;

namespace ClipboardWizard.ViewModel
{
    public class EditSnippetViewModel : INotifyPropertyChanged
    {
        private string _description;
        public string Description
        {
            get => _description;
            set
            {
                _description = value;
                OnPropertyChanged(nameof(Description));
            }
        }

        private string _content;
        public string Content
        {
            get => _content;
            set
            {
                _content = value;
                OnPropertyChanged(nameof(Content));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public EditSnippetViewModel(Snippet snippet)
        {
            Description = snippet.Description;
            Content = snippet.Content;
        }

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
