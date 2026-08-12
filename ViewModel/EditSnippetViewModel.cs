using ClipboardWizard.Model;
using System.ComponentModel;

namespace ClipboardWizard.ViewModel
{
    public class EditSnippetViewModel : INotifyPropertyChanged
    {
        public bool IsNew { get; }

        public string Title => IsNew ? "New Snippet" : "Edit Snippet";

        public string ActionButtonText => IsNew ? "Save" : "Update";

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
                OnPropertyChanged(nameof(IsValid));
            }
        }

        public bool IsValid => !string.IsNullOrWhiteSpace(Content);

        public event PropertyChangedEventHandler PropertyChanged;

        public EditSnippetViewModel()
        {
            IsNew = true;
        }

        public EditSnippetViewModel(Snippet snippet)
        {
            IsNew = false;
            Description = snippet.Description;
            Content = snippet.Content;
        }

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
