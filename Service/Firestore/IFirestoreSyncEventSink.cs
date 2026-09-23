using System.Threading.Tasks;

namespace ClipboardWizard.Service.Firestore
{
    /// <summary>
    /// The callbacks FirestoreSyncService raises for remote-originated events. Implemented by
    /// WizardViewModel, mirroring ICategoryHost/ISnippetHost's explicit-interface pattern rather
    /// than a static event bus (see CLAUDE.md). Every event has already had echoes of this app's
    /// own writes filtered out (see FirestoreSyncService's lastWriterId handling) - everything
    /// that reaches this interface is a genuine remote change to apply.
    /// </summary>
    public interface IFirestoreSyncEventSink
    {
        void OnConnectionStateChanged(FirestoreConnectionState state);

        Task OnRemoteCategoryPutAsync(RemoteCategorySnapshot snapshot);

        Task OnRemoteCategoryDeletedAsync(string categorySyncId);

        Task OnRemoteSnippetPutAsync(string categorySyncId, RemoteSnippetSnapshot snapshot);

        Task OnRemoteSnippetDeletedAsync(string categorySyncId, string snippetSyncId);
    }
}
