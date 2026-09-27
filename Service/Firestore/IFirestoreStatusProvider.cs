using System;

namespace ClipboardWizard.Service.Firestore
{
    /// <summary>Read-only view of FirestoreSyncService's connection state, for CategoryViewModel's warning-icon binding without pulling in the rest of IFirestoreSyncService.</summary>
    public interface IFirestoreStatusProvider
    {
        FirestoreConnectionState State { get; }

        /// <summary>The raw detail of the most recent connection failure (e.g. a Firestore missing-index message, which embeds a console link to create it), or null if the last attempt succeeded/none has happened yet. Only meaningful while State is Error.</summary>
        string LastErrorDetail { get; }

        event EventHandler StateChanged;
    }
}
