using Google.Cloud.Firestore;

namespace ClipboardWizard.Service.Firestore
{
    /// <summary>Wire shape of a categories/{categorySyncId} document. Internal - never leaves FirestoreSyncService; callers get/give RemoteCategorySnapshot instead.</summary>
    [FirestoreData]
    internal class CategoryDocument
    {
        [FirestoreProperty]
        public string Name { get; set; }

        [FirestoreProperty]
        public int Order { get; set; }

        [FirestoreProperty]
        public Timestamp ModifiedAtUtc { get; set; }

        [FirestoreProperty]
        public string LastWriterId { get; set; }
    }
}
