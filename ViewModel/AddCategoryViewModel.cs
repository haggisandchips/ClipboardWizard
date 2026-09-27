using ClipboardWizard.Model;
using ClipboardWizard.Service.Firestore;
using System.ComponentModel;

namespace ClipboardWizard.ViewModel
{
    public class AddCategoryViewModel : INotifyPropertyChanged
    {
        private readonly IFirestoreStatusProvider _firestoreStatus;

        public bool IsNew { get; }

        /// <summary>Whether the Shared checkbox is shown at all - true for a new category, or an existing one that isn't Shared yet. Once Shared, it's a one-way latch (see Category.Shared) so there's nothing left to toggle.</summary>
        public bool CanToggleShared { get; }

        public string Title => IsNew ? "New Category" : "Edit Category";

        public string ActionButtonText => IsNew ? "Save" : "Update";

        private string _name;
        public string Name
        {
            get => _name;
            set
            {
                _name = value;
                OnPropertyChanged(nameof(Name));
                OnPropertyChanged(nameof(IsValid));
            }
        }

        private bool _shared;
        public bool Shared
        {
            get => _shared;
            set
            {
                _shared = value;
                OnPropertyChanged(nameof(Shared));
                OnPropertyChanged(nameof(NeedsFirebaseSetup));
            }
        }

        /// <summary>Drives the dialog's own inline "Firebase setup required" hint - shown the moment Shared is checked, not only later on the category header.</summary>
        public bool NeedsFirebaseSetup => Shared && _firestoreStatus.State != FirestoreConnectionState.Connected;

        public bool IsValid => !string.IsNullOrWhiteSpace(Name);

        public event PropertyChangedEventHandler PropertyChanged;

        public AddCategoryViewModel(IFirestoreStatusProvider firestoreStatus)
        {
            _firestoreStatus = firestoreStatus;
            IsNew = true;
            CanToggleShared = true;
        }

        public AddCategoryViewModel(Category category, IFirestoreStatusProvider firestoreStatus)
        {
            _firestoreStatus = firestoreStatus;
            IsNew = false;
            Name = category.Name;
            Shared = category.Shared;
            CanToggleShared = !category.Shared;
        }

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
