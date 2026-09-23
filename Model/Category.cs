using SQLite;
using System;
using System.ComponentModel;

namespace ClipboardWizard.Model
{
    public class Category : INotifyPropertyChanged
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        private int _order;
        public int Order
        {
            get => _order;
            set
            {
                _order = value;
                OnPropertyChanged(nameof(Order));
            }
        }

        private string _name;
        public string Name
        {
            get => _name;
            set
            {
                _name = value;
                OnPropertyChanged(nameof(Name));
            }
        }

        private bool _isExpanded = true;
        public bool IsExpanded
        {
            get => _isExpanded;
            set
            {
                _isExpanded = value;
                OnPropertyChanged(nameof(IsExpanded));
            }
        }

        private bool _shared;

        /// <summary>
        /// Whether this category's snippets sync to Firestore. Existing rows created before
        /// this column existed default to false, which is correct for them since sharing is
        /// opt-in.
        /// </summary>
        public bool Shared
        {
            get => _shared;
            set
            {
                _shared = value;
                OnPropertyChanged(nameof(Shared));
            }
        }

        private string _syncId;

        /// <summary>
        /// Globally-unique id used as this category's Firestore document id. Null until the
        /// category is shared for the first time - assigned lazily at that point rather than
        /// up front, since most categories are never shared.
        /// </summary>
        public string SyncId
        {
            get => _syncId;
            set
            {
                _syncId = value;
                OnPropertyChanged(nameof(SyncId));
            }
        }

        private DateTime _modifiedAtUtc;

        /// <summary>Last-writer-wins timestamp for reconciling this category against remote edits. Only meaningful once Shared.</summary>
        public DateTime ModifiedAtUtc
        {
            get => _modifiedAtUtc;
            set
            {
                _modifiedAtUtc = value;
                OnPropertyChanged(nameof(ModifiedAtUtc));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
