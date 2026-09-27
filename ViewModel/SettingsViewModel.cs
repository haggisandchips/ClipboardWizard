using ClipboardWizard.Model;
using ClipboardWizard.Service.Firestore;
using System.Collections.Generic;
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

                // The key changed - Save is disabled again until Configure succeeds against
                // whatever the key is now (even if it's edited back to its original value).
                IsConfigured = false;
            }
        }

        /// <summary>Parsed live from ServiceAccountJson for display - immediate feedback that a pasted/browsed key looks right, before Save or Configure.</summary>
        public string ProjectId => new FirestoreCredentials { ServiceAccountJson = ServiceAccountJson }.ProjectId;

        public bool IsValid => !string.IsNullOrEmpty(ProjectId);

        private bool _isConfigured = true;

        /// <summary>True when Save may be clicked: initially true (an unchanged, already-saved key), flips false on any edit to the key, and back to true only once Configure (connection test + category picker + index check) has fully succeeded for the current key.</summary>
        public bool IsConfigured
        {
            get => _isConfigured;
            private set
            {
                _isConfigured = value;
                OnPropertyChanged(nameof(IsConfigured));
                OnPropertyChanged(nameof(CanSave));
            }
        }

        public bool CanSave => IsValid && IsConfigured;

        /// <summary>The staged "unchecked" set from the last successful Configure - unchanged (still whatever was loaded) until Configure succeeds again.</summary>
        public IReadOnlySet<string> HiddenCategorySyncIds { get; private set; }

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
            HiddenCategorySyncIds = new HashSet<string>(existing?.HiddenCategorySyncIds ?? new List<string>());
        }

        /// <summary>
        /// Tests the connection and, on success, fetches the remote category list - the caller
        /// (SettingsView) then opens the category picker with it. Returns null (with the failure
        /// already recorded in TestResultMessage) if the connection test itself failed - the
        /// picker never opens and Save stays disabled.
        /// </summary>
        internal async Task<IReadOnlyList<RemoteCategorySnapshot>> ConfigureAsync()
        {
            IsTesting = true;
            TestResultMessage = null;

            try
            {
                FirestoreCredentials credentials = new() { ServiceAccountJson = ServiceAccountJson };
                FirestoreTestResult result = await _firestoreSyncService.TestConnectionAsync(credentials);

                LastTestSucceeded = result.Success;
                TestResultMessage = result.Message;

                return result.Success ? await _firestoreSyncService.FetchCategoryListAsync(credentials) : null;
            }
            finally
            {
                IsTesting = false;
            }
        }

        /// <summary>
        /// Called once the category picker returns OK with hiddenCategorySyncIds. If anything's
        /// unchecked, verifies the filtered queries that set would require actually work (i.e.
        /// the needed Firestore index exists) before unlocking Save - a failure here (most likely
        /// a missing index that's still building) surfaces the same way a failed connection test
        /// does, including the index-creation link Firestore's own error embeds, and leaves Save
        /// disabled. There's no way to detect from here when the index finishes building - it
        /// still stages hiddenCategorySyncIds either way, though, so the picker reopens with the
        /// same categories already unchecked next time: retrying is just Configure, then OK.
        /// </summary>
        internal async Task<bool> ConfirmCategorySelectionAsync(IReadOnlySet<string> hiddenCategorySyncIds)
        {
            HiddenCategorySyncIds = hiddenCategorySyncIds;

            if (hiddenCategorySyncIds.Count > 0)
            {
                FirestoreCredentials credentials = new() { ServiceAccountJson = ServiceAccountJson };
                FirestoreTestResult result = await _firestoreSyncService.VerifyHiddenSetAsync(credentials, hiddenCategorySyncIds);
                if (!result.Success)
                {
                    LastTestSucceeded = false;
                    TestResultMessage = result.Message + " Once that's fixed, click Configure again to retry.";
                    return false;
                }
            }

            IsConfigured = true;
            return true;
        }

        /// <summary>
        /// Seeds the status message from a runtime listener failure that happened before this
        /// dialog was even opened (e.g. an index was deleted after the fact) - the same warning
        /// icon that already opens Settings for a NeedsFirebaseSetup category now also surfaces
        /// *why*, without the user having to click Configure again first.
        /// </summary>
        internal void SeedConnectionError(string detail)
        {
            if (string.IsNullOrEmpty(detail))
            {
                return;
            }

            LastTestSucceeded = false;
            TestResultMessage = detail;
        }

        internal FirestoreCredentials ToCredentials() => new() { ServiceAccountJson = ServiceAccountJson };

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
