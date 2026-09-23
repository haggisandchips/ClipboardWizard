using Google.Cloud.Firestore;

namespace ClipboardWizard.Service.Firestore
{
    /// <summary>Wire shape of one categories/{c}/snippets/{s}/imageChunks/{index} document.</summary>
    [FirestoreData]
    internal class ImageChunkDocument
    {
        [FirestoreProperty]
        public int Index { get; set; }

        [FirestoreProperty]
        public string Data { get; set; }
    }
}
