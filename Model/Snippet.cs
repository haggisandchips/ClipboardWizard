using SQLite;
using System.ComponentModel;

namespace ClipboardWizard.Model
{
    public class Snippet : INotifyPropertyChanged
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        private int _order;
        public int Order
        {
            get { return _order; }
            set {
                _order = value;
                OnPropertyChanged(nameof(Order));
            }
        }

        private string _description;
        public string Description
        {
            get { return _description; }
            set
            {
                _description = value;
                OnPropertyChanged(nameof(Description));
            }
        }

        private SnippetType _type;

        /// <summary>
        /// Which of Content/ImageData holds this snippet's payload. Existing rows created
        /// before this column existed default to 0 (Text), which is correct for them since
        /// only text snippets existed at the time.
        /// </summary>
        public SnippetType Type
        {
            get => _type;
            set
            {
                _type = value;
                OnPropertyChanged(nameof(Type));
            }
        }

        private string _content;

        /// <summary>Text payload. Only meaningful when Type is Text.</summary>
        public string Content
        {
            get => _content;
            set
            {
                _content = value;
                OnPropertyChanged(nameof(Content));
            }
        }

        private byte[] _imageData;

        /// <summary>PNG-encoded image payload. Only meaningful when Type is Image.</summary>
        public byte[] ImageData
        {
            get => _imageData;
            set
            {
                _imageData = value;
                OnPropertyChanged(nameof(ImageData));
            }
        }

        private int? _categoryId;

        /// <summary>The Category this snippet belongs to, or null if uncategorized.</summary>
        public int? CategoryId
        {
            get => _categoryId;
            set
            {
                _categoryId = value;
                OnPropertyChanged(nameof(CategoryId));
            }
        }

        private bool _locked;

        /// <summary>
        /// Permanent, persisted protection flag. Once set to true it can never be reverted -
        /// further assignments are silently ignored rather than throwing, since this is the
        /// normal, expected way callers re-save an already-locked snippet.
        /// </summary>
        public bool Locked
        {
            get { return _locked; }
            set
            {
                if (_locked)
                {
                    return;
                }

                _locked = value;
                OnPropertyChanged(nameof(Locked));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
