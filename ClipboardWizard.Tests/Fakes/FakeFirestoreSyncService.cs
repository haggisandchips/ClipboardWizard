using ClipboardWizard.Model;
using ClipboardWizard.Service.Firestore;

namespace ClipboardWizard.Tests.Fakes
{
    /// <summary>In-memory stand-in for IFirestoreSyncService - records every push/delete call so tests can assert what did (or, importantly, didn't) get pushed.</summary>
    internal class FakeFirestoreSyncService : IFirestoreSyncService
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

        public string? LastErrorDetail { get; set; }

        public IFirestoreSyncEventSink? Sink { get; set; }

        public List<(FirestoreCredentials Credentials, IReadOnlySet<string> HiddenCategorySyncIds)> Configured { get; } = new();

        public IReadOnlyList<RemoteCategorySnapshot> NextCategoryList { get; set; } = new List<RemoteCategorySnapshot>();

        public FirestoreTestResult NextVerifyResult { get; set; } = FirestoreTestResult.Ok("fake");

        public List<Category> PushedCategories { get; } = new();

        public List<(Category Category, Snippet Snippet)> PushedSnippets { get; } = new();

        public List<(Category Category, IReadOnlyList<Snippet> Snippets)> BulkPushes { get; } = new();

        public List<string> DeletedRemoteCategories { get; } = new();

        public List<(string CategorySyncId, string SnippetSyncId)> DeletedRemoteSnippets { get; } = new();

        public bool StartCalled { get; private set; }

        public bool StopCalled { get; private set; }

        public FirestoreTestResult NextTestResult { get; set; } = FirestoreTestResult.Ok("fake");

        /// <summary>When set, PushSnippetAsync throws this instead of succeeding - simulates a push that fails (e.g. offline).</summary>
        public Exception? NextPushSnippetException { get; set; }

        public void Configure(FirestoreCredentials credentials, IReadOnlySet<string> hiddenCategorySyncIds)
        {
            Configured.Add((credentials, hiddenCategorySyncIds));
        }

        public Task<FirestoreTestResult> TestConnectionAsync(FirestoreCredentials credentials, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(NextTestResult);
        }

        public Task<IReadOnlyList<RemoteCategorySnapshot>> FetchCategoryListAsync(FirestoreCredentials credentials, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(NextCategoryList);
        }

        public Task<FirestoreTestResult> VerifyHiddenSetAsync(FirestoreCredentials credentials, IReadOnlySet<string> hiddenCategorySyncIds, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(NextVerifyResult);
        }

        public void Start()
        {
            StartCalled = true;
        }

        public void Stop()
        {
            StopCalled = true;
        }

        public Task PushCategoryAsync(Category category, CancellationToken cancellationToken = default)
        {
            PushedCategories.Add(category);
            return Task.CompletedTask;
        }

        public Task PushSnippetAsync(Category category, Snippet snippet, CancellationToken cancellationToken = default)
        {
            if (NextPushSnippetException != null)
            {
                throw NextPushSnippetException;
            }

            PushedSnippets.Add((category, snippet));
            return Task.CompletedTask;
        }

        public Task PushCategoryBulkAsync(Category category, IReadOnlyList<Snippet> snippets, CancellationToken cancellationToken = default)
        {
            BulkPushes.Add((category, snippets));
            return Task.CompletedTask;
        }

        public Task DeleteRemoteCategoryAsync(string categorySyncId, CancellationToken cancellationToken = default)
        {
            DeletedRemoteCategories.Add(categorySyncId);
            return Task.CompletedTask;
        }

        public Task DeleteRemoteSnippetAsync(string categorySyncId, string snippetSyncId, CancellationToken cancellationToken = default)
        {
            DeletedRemoteSnippets.Add((categorySyncId, snippetSyncId));
            return Task.CompletedTask;
        }
    }
}
