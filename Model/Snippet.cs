using SQLite;
using System;
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

        private bool _locked;
        public bool Locked
        {
            get { return _locked; }
            set
            {
                if (_locked)
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
