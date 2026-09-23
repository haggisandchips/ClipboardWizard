namespace ClipboardWizard.Service.Firestore
{
    /// <summary>Drives the "Firebase setup required" warning shown on Shared categories.</summary>
    public enum FirestoreConnectionState
    {
        /// <summary>No settings saved yet.</summary>
        NotConfigured,

        /// <summary>Settings saved; connecting/reconnecting.</summary>
        Connecting,

        /// <summary>Both realtime listeners are live.</summary>
        Connected,

        /// <summary>Settings saved, but the last connection attempt failed (bad credentials, unreachable, etc).</summary>
        Error
    }
}
