using ClipboardWizard.Service.Firestore;

namespace ClipboardWizard.Tests.Fakes
{
    /// <summary>In-memory stand-in for IFirestoreStatusProvider - lets tests toggle connection state without a real FirestoreSyncService.</summary>
    internal class FakeFirestoreStatusProvider : IFirestoreStatusProvider
    {
        private FirestoreConnectionState _state = FirestoreConnectionState.NotConfigured;

        public FirestoreConnectionState State
        {
            get => _state;
            set
            {
                _state = value;
                StateChanged?.Invoke(this, System.EventArgs.Empty);
            }
        }

        public event System.EventHandler? StateChanged;
    }
}
