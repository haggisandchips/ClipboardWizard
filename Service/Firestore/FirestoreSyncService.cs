using ClipboardWizard.Model;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Firestore;
using Grpc.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ClipboardWizard.Service.Firestore
{
    /// <summary>
    /// Thin orchestration over the official Google.Cloud.Firestore SDK: credential setup (the
    /// SDK refreshes the service-account's OAuth token internally - no custom code needed here),
    /// exactly two realtime listeners regardless of how many categories are shared (one on the
    /// categories collection, one collection-group query across every category's snippets
    /// subcollection), push/delete, and echo suppression via a per-instance LastWriterId stamped
    /// on every write.
    /// </summary>
    public class FirestoreSyncService : IFirestoreSyncService
    {
        private const string CategoriesCollectionId = "categories";
        private const string SnippetsCollectionId = "snippets";
        private const string ImageChunksCollectionId = "imageChunks";
        private const string SnippetCategorySyncIdField = "CategorySyncId";

        private IFirestoreSyncEventSink _sink;

        public IFirestoreSyncEventSink Sink { set => _sink = value; }

        /// <summary>Generated once per instance (i.e. once per app launch) and stamped on every push - lets the listener callbacks recognize and skip echoes of this app's own writes.</summary>
        private readonly string _instanceWriterId = Guid.NewGuid().ToString("N");

        private readonly object _stateLock = new();
        private FirestoreConnectionState _state = FirestoreConnectionState.NotConfigured;
        private string _lastErrorDetail;

        private FirestoreCredentials _credentials;
        private IReadOnlySet<string> _hiddenCategorySyncIds = new HashSet<string>();
        private FirestoreDb _db;
        private FirestoreChangeListener _categoryListener;
        private FirestoreChangeListener _snippetListener;
        private bool _running;

        public FirestoreConnectionState State
        {
            get { lock (_stateLock) { return _state; } }
        }

        public string LastErrorDetail
        {
            get { lock (_stateLock) { return _lastErrorDetail; } }
        }

        public event EventHandler StateChanged;

        public void Configure(FirestoreCredentials credentials, IReadOnlySet<string> hiddenCategorySyncIds)
        {
            _credentials = credentials;
            _hiddenCategorySyncIds = hiddenCategorySyncIds ?? new HashSet<string>();
            if (_running)
            {
                _ = ReconnectAsync();
            }
        }

        public void Start()
        {
            _running = true;
            _ = ReconnectAsync();
        }

        public void Stop()
        {
            _running = false;
            _ = TeardownAsync();
            SetState(FirestoreConnectionState.NotConfigured);
        }

        private async Task ReconnectAsync()
        {
            await TeardownAsync();

            if (string.IsNullOrWhiteSpace(_credentials?.ServiceAccountJson))
            {
                SetState(FirestoreConnectionState.NotConfigured);
                return;
            }

            SetState(FirestoreConnectionState.Connecting);

            try
            {
                FirestoreDb db = await BuildDbAsync(_credentials);

                Query categoriesQuery = _hiddenCategorySyncIds.Count > 0
                    ? db.Collection(CategoriesCollectionId).WhereNotIn(FieldPath.DocumentId, _hiddenCategorySyncIds.ToList())
                    : db.Collection(CategoriesCollectionId);
                Query snippetsQuery = _hiddenCategorySyncIds.Count > 0
                    ? db.CollectionGroup(SnippetsCollectionId).WhereNotIn(SnippetCategorySyncIdField, _hiddenCategorySyncIds.ToList())
                    : db.CollectionGroup(SnippetsCollectionId);

                FirestoreChangeListener categoryListener = categoriesQuery.Listen(OnCategoriesSnapshotAsync);
                FirestoreChangeListener snippetListener = snippetsQuery.Listen(OnSnippetsSnapshotAsync);

                _db = db;
                _categoryListener = categoryListener;
                _snippetListener = snippetListener;

                lock (_stateLock) { _lastErrorDetail = null; }
                SetState(FirestoreConnectionState.Connected);
            }
            catch (Exception ex)
            {
                Logger.LogError(nameof(ReconnectAsync), ex);
                lock (_stateLock) { _lastErrorDetail = (ex as RpcException)?.Status.Detail; }
                SetState(FirestoreConnectionState.Error);
            }
        }

        private async Task TeardownAsync()
        {
            FirestoreChangeListener categoryListener = _categoryListener;
            FirestoreChangeListener snippetListener = _snippetListener;
            _categoryListener = null;
            _snippetListener = null;
            _db = null;

            if (categoryListener != null)
            {
                try { await categoryListener.StopAsync(); }
                catch (Exception ex) { Logger.LogError(nameof(TeardownAsync), ex); }
            }

            if (snippetListener != null)
            {
                try { await snippetListener.StopAsync(); }
                catch (Exception ex) { Logger.LogError(nameof(TeardownAsync), ex); }
            }
        }

        private static async Task<FirestoreDb> BuildDbAsync(FirestoreCredentials credentials)
        {
            ServiceAccountCredential credential = CredentialFactory.FromJson<ServiceAccountCredential>(credentials.ServiceAccountJson);
            FirestoreDbBuilder builder = new()
            {
                ProjectId = credentials.ProjectId,
                GoogleCredential = credential.ToGoogleCredential()
            };
            return await builder.BuildAsync();
        }

        public async Task<FirestoreTestResult> TestConnectionAsync(FirestoreCredentials credentials, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(credentials?.ServiceAccountJson))
            {
                return FirestoreTestResult.Failed("Paste or browse to a service-account key first.");
            }

            if (string.IsNullOrEmpty(credentials.ProjectId))
            {
                return FirestoreTestResult.Failed("That doesn't look like a service-account key - no \"project_id\" found in the JSON.");
            }

            try
            {
                FirestoreDb db = await BuildDbAsync(credentials);
                await db.Collection(CategoriesCollectionId).Limit(1).GetSnapshotAsync(cancellationToken);
                return FirestoreTestResult.Ok($"Connected to Firestore project \"{credentials.ProjectId}\".");
            }
            catch (RpcException ex)
            {
                return FirestoreTestResult.Failed(DescribeRpcFailure(ex));
            }
            catch (Exception ex)
            {
                // Redact: this may include the credential JSON in its message for some
                // deserialization failures, and must never reach the log verbatim.
                Logger.LogError(nameof(TestConnectionAsync), new Exception(ex.GetType().Name));
                return FirestoreTestResult.Failed("Couldn't connect - check the service-account key is valid JSON and the project id is correct.");
            }
        }

        public async Task<IReadOnlyList<RemoteCategorySnapshot>> FetchCategoryListAsync(FirestoreCredentials credentials, CancellationToken cancellationToken = default)
        {
            FirestoreDb db = await BuildDbAsync(credentials);
            QuerySnapshot snapshot = await db.Collection(CategoriesCollectionId).GetSnapshotAsync(cancellationToken);

            List<RemoteCategorySnapshot> categories = new();
            foreach (DocumentSnapshot doc in snapshot.Documents)
            {
                CategoryDocument document = doc.ConvertTo<CategoryDocument>();
                categories.Add(new RemoteCategorySnapshot
                {
                    SyncId = doc.Reference.Id,
                    Name = document.Name,
                    ModifiedAtUtc = document.ModifiedAtUtc.ToDateTime()
                });
            }
            return categories;
        }

        public async Task<FirestoreTestResult> VerifyHiddenSetAsync(FirestoreCredentials credentials, IReadOnlySet<string> hiddenCategorySyncIds, CancellationToken cancellationToken = default)
        {
            if (hiddenCategorySyncIds == null || hiddenCategorySyncIds.Count == 0)
            {
                return FirestoreTestResult.Ok("Nothing hidden - no index needed.");
            }

            try
            {
                FirestoreDb db = await BuildDbAsync(credentials);
                List<string> ids = hiddenCategorySyncIds.ToList();

                await db.Collection(CategoriesCollectionId).WhereNotIn(FieldPath.DocumentId, ids).Limit(1).GetSnapshotAsync(cancellationToken);
                await db.CollectionGroup(SnippetsCollectionId).WhereNotIn(SnippetCategorySyncIdField, ids).Limit(1).GetSnapshotAsync(cancellationToken);

                return FirestoreTestResult.Ok("Filter verified.");
            }
            catch (RpcException ex)
            {
                return FirestoreTestResult.Failed(DescribeRpcFailure(ex));
            }
            catch (Exception ex)
            {
                Logger.LogError(nameof(VerifyHiddenSetAsync), new Exception(ex.GetType().Name));
                return FirestoreTestResult.Failed("Couldn't verify the hidden-category filter - check the service-account key is valid JSON and the project id is correct.");
            }
        }

        /// <summary>
        /// For FailedPrecondition (a missing composite/single-field index), Firestore's own detail
        /// message already embeds a console link to create the exact index needed - passed through
        /// verbatim rather than replaced with a generic sentence, since that link is the actionable
        /// part.
        /// </summary>
        private static string DescribeRpcFailure(RpcException ex) => ex.StatusCode switch
        {
            StatusCode.PermissionDenied => "Permission denied - check the service account's IAM role includes Firestore access.",
            StatusCode.Unauthenticated => "Authentication failed - the service-account key may be invalid or revoked.",
            StatusCode.NotFound => "Project or database not found - check the project id and that Firestore is enabled for it.",
            StatusCode.Unavailable => "Couldn't reach Firestore - check your network connection.",
            StatusCode.FailedPrecondition => ex.Status.Detail ?? "Firestore needs an index for this query.",
            _ => $"Firestore error ({ex.StatusCode})."
        };

        private FirestoreDb RequireDb()
        {
            FirestoreDb db = _db;
            if (db == null)
            {
                throw new InvalidOperationException("Firestore isn't connected.");
            }
            return db;
        }

        public Task PushCategoryAsync(Category category, CancellationToken cancellationToken = default)
        {
            DocumentReference categoryRef = RequireDb().Collection(CategoriesCollectionId).Document(category.SyncId);
            return categoryRef.SetAsync(ToCategoryDocument(category), cancellationToken: cancellationToken);
        }

        public async Task PushSnippetAsync(Category category, Snippet snippet, CancellationToken cancellationToken = default)
        {
            FirestoreDb db = RequireDb();
            DocumentReference snippetRef = db.Collection(CategoriesCollectionId).Document(category.SyncId)
                .Collection(SnippetsCollectionId).Document(snippet.SyncId);

            await WriteSnippetAsync(db, snippetRef, category.SyncId, snippet, cancellationToken);
        }

        public async Task PushCategoryBulkAsync(Category category, IReadOnlyList<Snippet> snippets, CancellationToken cancellationToken = default)
        {
            await PushCategoryAsync(category, cancellationToken);
            foreach (Snippet snippet in snippets)
            {
                await PushSnippetAsync(category, snippet, cancellationToken);
            }
        }

        private async Task WriteSnippetAsync(FirestoreDb db, DocumentReference snippetRef, string categorySyncId, Snippet snippet, CancellationToken cancellationToken)
        {
            SnippetDocument document = new()
            {
                Type = snippet.Type.ToString(),
                Description = snippet.Description,
                Content = snippet.Type == SnippetType.Text ? snippet.Content : null,
                Order = snippet.Order,
                Locked = snippet.Locked,
                ModifiedAtUtc = ToTimestamp(snippet.ModifiedAtUtc),
                LastWriterId = _instanceWriterId,
                CategorySyncId = categorySyncId
            };

            bool isChunkedImage = snippet.Type == SnippetType.Image
                && snippet.ImageData is { Length: > 0 }
                && snippet.ImageData.Length > ImageChunker.ChunkThresholdBytes;

            if (snippet.Type == SnippetType.Image && snippet.ImageData is { Length: > 0 })
            {
                document.ImageTotalBytes = snippet.ImageData.Length;

                if (!isChunkedImage)
                {
                    document.ImageDataBase64 = Convert.ToBase64String(snippet.ImageData);
                    document.ImageChunkCount = 1;
                }
                else
                {
                    document.ImageChunkCount = ImageChunker.Split(snippet.ImageData, ImageChunker.ChunkThresholdBytes).Count;
                }
            }

            if (!isChunkedImage)
            {
                await snippetRef.SetAsync(document, cancellationToken: cancellationToken);
                return;
            }

            // Chunked image: parent doc + every chunk doc are written together in one batch, so
            // a listener can never observe a snippet claiming N chunks before they all exist.
            IReadOnlyList<byte[]> chunks = ImageChunker.Split(snippet.ImageData, ImageChunker.ChunkThresholdBytes);
            WriteBatch batch = db.StartBatch();
            batch.Set(snippetRef, document);

            CollectionReference chunksRef = snippetRef.Collection(ImageChunksCollectionId);
            for (int i = 0; i < chunks.Count; i++)
            {
                ImageChunkDocument chunkDocument = new() { Index = i, Data = Convert.ToBase64String(chunks[i]) };
                batch.Set(chunksRef.Document(i.ToString()), chunkDocument);
            }

            await batch.CommitAsync(cancellationToken);
        }

        public async Task DeleteRemoteCategoryAsync(string categorySyncId, CancellationToken cancellationToken = default)
        {
            FirestoreDb db = RequireDb();
            DocumentReference categoryRef = db.Collection(CategoriesCollectionId).Document(categorySyncId);

            QuerySnapshot snippetsSnapshot = await categoryRef.Collection(SnippetsCollectionId).GetSnapshotAsync(cancellationToken);
            foreach (DocumentSnapshot snippetDoc in snippetsSnapshot.Documents)
            {
                await DeleteSnippetDocumentAsync(db, snippetDoc.Reference, cancellationToken);
            }

            await categoryRef.DeleteAsync(cancellationToken: cancellationToken);
        }

        public async Task DeleteRemoteSnippetAsync(string categorySyncId, string snippetSyncId, CancellationToken cancellationToken = default)
        {
            FirestoreDb db = RequireDb();
            DocumentReference snippetRef = db.Collection(CategoriesCollectionId).Document(categorySyncId)
                .Collection(SnippetsCollectionId).Document(snippetSyncId);

            await DeleteSnippetDocumentAsync(db, snippetRef, cancellationToken);
        }

        /// <summary>Deletes a snippet doc together with its imageChunks subcollection, if any - Firestore doesn't cascade-delete subcollections on its own.</summary>
        private static async Task DeleteSnippetDocumentAsync(FirestoreDb db, DocumentReference snippetRef, CancellationToken cancellationToken)
        {
            QuerySnapshot chunksSnapshot = await snippetRef.Collection(ImageChunksCollectionId).GetSnapshotAsync(cancellationToken);
            if (chunksSnapshot.Count > 0)
            {
                WriteBatch batch = db.StartBatch();
                foreach (DocumentSnapshot chunkDoc in chunksSnapshot.Documents)
                {
                    batch.Delete(chunkDoc.Reference);
                }
                batch.Delete(snippetRef);
                await batch.CommitAsync(cancellationToken);
                return;
            }

            await snippetRef.DeleteAsync(cancellationToken: cancellationToken);
        }

        private async Task OnCategoriesSnapshotAsync(QuerySnapshot snapshot, CancellationToken cancellationToken)
        {
            foreach (DocumentChange change in snapshot.Changes)
            {
                try
                {
                    string categorySyncId = change.Document.Reference.Id;

                    if (change.ChangeType == DocumentChange.Type.Removed)
                    {
                        await _sink.OnRemoteCategoryDeletedAsync(categorySyncId);
                        continue;
                    }

                    CategoryDocument document = change.Document.ConvertTo<CategoryDocument>();
                    if (document.LastWriterId == _instanceWriterId)
                    {
                        continue;
                    }

                    await _sink.OnRemoteCategoryPutAsync(new RemoteCategorySnapshot
                    {
                        SyncId = categorySyncId,
                        Name = document.Name,
                        ModifiedAtUtc = document.ModifiedAtUtc.ToDateTime()
                    });
                }
                catch (Exception ex)
                {
                    Logger.LogError(nameof(OnCategoriesSnapshotAsync), ex);
                }
            }
        }

        private async Task OnSnippetsSnapshotAsync(QuerySnapshot snapshot, CancellationToken cancellationToken)
        {
            foreach (DocumentChange change in snapshot.Changes)
            {
                try
                {
                    DocumentReference snippetRef = change.Document.Reference;
                    string snippetSyncId = snippetRef.Id;
                    // snippetRef.Parent is the "snippets" subcollection; .Parent.Parent is the owning category document.
                    string categorySyncId = snippetRef.Parent?.Parent?.Id;
                    if (categorySyncId == null)
                    {
                        continue;
                    }

                    if (change.ChangeType == DocumentChange.Type.Removed)
                    {
                        await _sink.OnRemoteSnippetDeletedAsync(categorySyncId, snippetSyncId);
                        continue;
                    }

                    SnippetDocument document = change.Document.ConvertTo<SnippetDocument>();
                    if (document.LastWriterId == _instanceWriterId)
                    {
                        continue;
                    }

                    if (!Enum.TryParse(document.Type, out SnippetType type))
                    {
                        continue;
                    }

                    byte[] imageData = null;
                    Func<Task<byte[]>> fetchChunkedImageData = null;
                    if (type == SnippetType.Image)
                    {
                        if (document.ImageChunkCount > 1)
                        {
                            // Deferred: this snippet's pixels can't have changed since we last saw
                            // this SyncId (see RemoteSnippetSnapshot), so the sink only invokes this -
                            // and only then pays for the chunk read - when the snippet is new to it.
                            fetchChunkedImageData = () => FetchAndReassembleChunksAsync(snippetRef, cancellationToken);
                        }
                        else
                        {
                            imageData = document.ImageDataBase64 != null ? Convert.FromBase64String(document.ImageDataBase64) : null;
                        }
                    }

                    await _sink.OnRemoteSnippetPutAsync(categorySyncId, new RemoteSnippetSnapshot
                    {
                        SyncId = snippetSyncId,
                        Type = type,
                        Description = document.Description,
                        Content = document.Content,
                        ImageData = imageData,
                        FetchChunkedImageDataAsync = fetchChunkedImageData,
                        Order = document.Order,
                        Locked = document.Locked,
                        ModifiedAtUtc = document.ModifiedAtUtc.ToDateTime()
                    });
                }
                catch (Exception ex)
                {
                    Logger.LogError(nameof(OnSnippetsSnapshotAsync), ex);
                }
            }
        }

        /// <summary>
        /// One-shot fetch, not a realtime listen - chunk docs are write-once in practice (SPEC.md
        /// forbids editing an image snippet's pixels; replacing one means delete+recreate, which
        /// produces a new SyncId and a new chunk set entirely).
        /// </summary>
        private static async Task<byte[]> FetchAndReassembleChunksAsync(DocumentReference snippetRef, CancellationToken cancellationToken)
        {
            QuerySnapshot chunksSnapshot = await snippetRef.Collection(ImageChunksCollectionId).GetSnapshotAsync(cancellationToken);
            List<byte[]> orderedChunks = chunksSnapshot.Documents
                .Select(doc => doc.ConvertTo<ImageChunkDocument>())
                .OrderBy(chunk => chunk.Index)
                .Select(chunk => Convert.FromBase64String(chunk.Data))
                .ToList();

            return ImageChunker.Reassemble(orderedChunks);
        }

        private CategoryDocument ToCategoryDocument(Category category) => new()
        {
            Name = category.Name,
            ModifiedAtUtc = ToTimestamp(category.ModifiedAtUtc),
            LastWriterId = _instanceWriterId
        };

        /// <summary>Timestamp.FromDateTime requires an explicit Utc Kind - sqlite-net-pcl round-trips DateTime as Unspecified, so this is never skippable.</summary>
        private static Timestamp ToTimestamp(DateTime dateTime) => Timestamp.FromDateTime(DateTime.SpecifyKind(dateTime, DateTimeKind.Utc));

        private void SetState(FirestoreConnectionState state)
        {
            bool changed;
            lock (_stateLock)
            {
                changed = _state != state;
                _state = state;
            }

            if (!changed)
            {
                return;
            }

            StateChanged?.Invoke(this, EventArgs.Empty);
            _sink?.OnConnectionStateChanged(state);
        }
    }
}
