using SQLite;
using System;
using System.ComponentModel;

namespace ClipboardWizard.Model
{
    public class Snippet : INotifyPropertyChanged
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        private string content;
        public string Content
        {
            get => content;
            set
            {
                if (content != null)
                {
                    throw new InvalidOperationException("Content cannot be changed once set");
                }

                content = value;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        // TODO This will be used for tags - not required for content which is effectively immutable
        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
