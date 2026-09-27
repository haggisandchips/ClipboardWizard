using ClipboardWizard.Model;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ClipboardWizard.Service.Firestore
{
    public interface IFirestoreSyncService : IFirestoreStatusProvider
    {
        /// <summary>
        /// Set once by the composition root before Start(). A settable property rather than a
        /// constructor parameter because the sink (WizardViewModel) itself needs a reference to
        /// this service at its own construction time - a straight constructor dependency would
        /// be circular.
        /// </summary>
        IFirestoreSyncEventSink Sink { set; }

        /// <summary>Applies (new or changed) credentials and the set of category SyncIds to exclude from sync. Reconnects immediately if already Start()-ed.</summary>
        void Configure(FirestoreCredentials credentials, IReadOnlySet<string> hiddenCategorySyncIds);

        /// <summary>Signs in with the given (possibly unsaved/draft) credentials and does one lightweight read, without affecting this instance's own Configure()d state/connection.</summary>
        Task<FirestoreTestResult> TestConnectionAsync(FirestoreCredentials credentials, CancellationToken cancellationToken = default);

        /// <summary>One-shot, unfiltered fetch of every Shared category in the remote database (SyncId/Name only) - for the Settings "Configure" category picker. Uses its own temporary connection; never touches this instance's live state.</summary>
        Task<IReadOnlyList<RemoteCategorySnapshot>> FetchCategoryListAsync(FirestoreCredentials credentials, CancellationToken cancellationToken = default);

        /// <summary>One-shot check that the not-in filtered queries hiddenCategorySyncIds would require actually work (i.e. the needed Firestore index exists), without pulling real data or touching this instance's live state.</summary>
        Task<FirestoreTestResult> VerifyHiddenSetAsync(FirestoreCredentials credentials, IReadOnlySet<string> hiddenCategorySyncIds, CancellationToken cancellationToken = default);

        /// <summary>Starts the two realtime listeners (categories, and a snippets collection-group query) if configured. Safe to call when not yet configured - becomes a no-op until Configure() is called.</summary>
        void Start();

        void Stop();

        Task PushCategoryAsync(Category category, CancellationToken cancellationToken = default);

        Task PushSnippetAsync(Category category, Snippet snippet, CancellationToken cancellationToken = default);

        /// <summary>Initial push when a category's Shared flag flips on: the category document, then every snippet currently in it.</summary>
        Task PushCategoryBulkAsync(Category category, IReadOnlyList<Snippet> snippets, CancellationToken cancellationToken = default);

        Task DeleteRemoteCategoryAsync(string categorySyncId, CancellationToken cancellationToken = default);

        Task DeleteRemoteSnippetAsync(string categorySyncId, string snippetSyncId, CancellationToken cancellationToken = default);
    }
}
