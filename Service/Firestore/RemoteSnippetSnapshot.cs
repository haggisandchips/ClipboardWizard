using ClipboardWizard.Model;
using System;

namespace ClipboardWizard.Service.Firestore
{
    /// <summary>
    /// A snippet document as received from Firestore, decoupled from the SDK's own document
    /// types. ImageData is already fully reassembled (chunk fetch/concatenation, if the source
    /// document was chunked, has already happened) by the time this reaches IFirestoreSyncEventSink.
    /// </summary>
    public class RemoteSnippetSnapshot
    {
        public string SyncId { get; set; }

        public SnippetType Type { get; set; }

        public string Description { get; set; }

        public string Content { get; set; }

        public byte[] ImageData { get; set; }

        public int Order { get; set; }

        public bool Locked { get; set; }

        public DateTime ModifiedAtUtc { get; set; }
    }
}
