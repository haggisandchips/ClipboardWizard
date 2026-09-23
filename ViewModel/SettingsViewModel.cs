using ClipboardWizard.Model;
using ClipboardWizard.Service.Firestore;
using System.ComponentModel;
using System.Threading.Tasks;

namespace ClipboardWizard.ViewModel
{
    public class SettingsViewModel : INotifyPropertyChanged
    {
        private readonly IFirestoreSyncService _firestoreSyncService;

        private string _serviceAccountJson;
        public string ServiceAccountJson
        {
            get => _serviceAccountJson;
            set
            {
                _serviceAccountJson = value;
                OnPropertyChanged(nameof(ServiceAccountJson));
                OnPropertyChanged(nameof(ProjectId));
                OnPropertyChanged(nameof(IsValid));
                TestResultMessage = null;
            }
        }

        /// <summary>Parsed live from ServiceAccountJson for display - immediate feedback that a pasted/browsed key looks right, before Save or Test Connection.</summary>
        public string ProjectId => new FirestoreCredentials { ServiceAccountJson = ServiceAccountJson }.ProjectId;

        public bool IsValid => !string.IsNullOrEmpty(ProjectId);

        private string _testResultMessage;
        public string TestResultMessage
        {
            get => _testResultMessage;
            private set
            {
                _testResultMessage = value;
                OnPropertyChanged(nameof(TestResultMessage));
            }
        }

        private bool _lastTestSucceeded;
        public bool LastTestSucceeded
        {
            get => _lastTestSucceeded;
            private set
            {
                _lastTestSucceeded = value;
                OnPropertyChanged(nameof(LastTestSucceeded));
            }
        }

        private bool _isTesting;
        public bool IsTesting
        {
            get => _isTesting;
            private set
            {
                _isTesting = value;
                OnPropertyChanged(nameof(IsTesting));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public SettingsViewModel(FirestoreCredentials existing, IFirestoreSyncService firestoreSyncService)
        {
            _firestoreSyncService = firestoreSyncService;
            _serviceAccountJson = existing?.ServiceAccountJson;
        }

        internal async Task TestConnectionAsync()
        {
            IsTesting = true;
            TestResultMessage = null;

            try
            {
                FirestoreTestResult result = await _firestoreSyncService.TestConnectionAsync(
                    new FirestoreCredentials { ServiceAccountJson = ServiceAccountJson });

                LastTestSucceeded = result.Success;
                TestResultMessage = result.Message;
            }
            finally
            {
                IsTesting = false;
            }
        }

        internal FirestoreCredentials ToCredentials() => new() { ServiceAccountJson = ServiceAccountJson };

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
