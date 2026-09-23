using ClipboardWizard.Model;
using ClipboardWizard.Service.Firestore;
using System.ComponentModel;

namespace ClipboardWizard.ViewModel
{
    public class AddCategoryViewModel : INotifyPropertyChanged
    {
        private readonly IFirestoreStatusProvider _firestoreStatus;

        public bool IsNew { get; }

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
        }

        public AddCategoryViewModel(Category category, IFirestoreStatusProvider firestoreStatus)
        {
            _firestoreStatus = firestoreStatus;
            IsNew = false;
            Name = category.Name;
            Shared = category.Shared;
        }

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
