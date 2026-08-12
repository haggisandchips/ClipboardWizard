using ClipboardWizard.Model;
using System.ComponentModel;

namespace ClipboardWizard.ViewModel
{
    public class EditSnippetViewModel : INotifyPropertyChanged
    {
        public bool IsNew { get; }

        /// <summary>
        /// Content is only ever text-editable for Text snippets - an Image snippet's picture
        /// can't be retyped in a text box, so editing one only offers its Description.
        /// </summary>
        public SnippetType Type { get; }

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

        /// <summary>Read-only preview source for Image snippets; never edited here.</summary>
        public byte[] ImageData { get; }

        public bool IsValid => Type != SnippetType.Text || !string.IsNullOrWhiteSpace(Content);

        public event PropertyChangedEventHandler PropertyChanged;

        public EditSnippetViewModel()
        {
            IsNew = true;
            Type = SnippetType.Text;
        }

        public EditSnippetViewModel(Snippet snippet)
        {
            IsNew = false;
            Type = snippet.Type;
            Description = snippet.Description;
            Content = snippet.Content;
            ImageData = snippet.ImageData;
        }

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
