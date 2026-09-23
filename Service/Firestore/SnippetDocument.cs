using Google.Cloud.Firestore;

namespace ClipboardWizard.Service.Firestore
{
    /// <summary>
    /// Wire shape of a categories/{categorySyncId}/snippets/{snippetSyncId} document. Internal -
    /// never leaves FirestoreSyncService; callers get/give RemoteSnippetSnapshot instead.
    /// ImageDataBase64 holds the whole image inline when it fits under ImageChunker.ChunkThresholdBytes;
    /// otherwise it's null and ImageChunkCount/ImageTotalBytes describe the imageChunks
    /// subcollection holding the (chunked) image data instead.
    /// </summary>
    [FirestoreData]
    internal class SnippetDocument
    {
        [FirestoreProperty]
        public string Type { get; set; }

        [FirestoreProperty]
        public string Description { get; set; }

        [FirestoreProperty]
        public string Content { get; set; }

        [FirestoreProperty]
        public string ImageDataBase64 { get; set; }

        [FirestoreProperty]
        public int ImageChunkCount { get; set; }

        [FirestoreProperty]
        public long ImageTotalBytes { get; set; }

        [FirestoreProperty]
        public int Order { get; set; }

        [FirestoreProperty]
        public bool Locked { get; set; }

        [FirestoreProperty]
        public Timestamp ModifiedAtUtc { get; set; }

        [FirestoreProperty]
        public string LastWriterId { get; set; }
    }
}
