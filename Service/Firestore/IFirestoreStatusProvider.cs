using System;

namespace ClipboardWizard.Service.Firestore
{
    /// <summary>Read-only view of FirestoreSyncService's connection state, for CategoryViewModel's warning-icon binding without pulling in the rest of IFirestoreSyncService.</summary>
    public interface IFirestoreStatusProvider
    {
        FirestoreConnectionState State { get; }

        event EventHandler StateChanged;
    }
}
