using SQLite;
using System;
using System.ComponentModel;

namespace ClipboardWizard.Model
{
    public class Snippet : INotifyPropertyChanged
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        private string _content;
        public string Content
        {
            get => _content;
            set
            {
                if (_content != null)
                {
                    throw new InvalidOperationException("Content cannot be changed once set");
                }

                _content = value;
            }
        }

        private bool _locked;
        public bool Locked
        {
            get { return _locked; }
            set {
                if(_locked)
                {
                    throw new InvalidOperationException("Once locked, always locked");
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
