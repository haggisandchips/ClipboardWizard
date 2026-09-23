using ClipboardWizard.Model;
using System;
using System.Threading.Tasks;

namespace ClipboardWizard.Service.Firestore
{
    /// <summary>
    /// A snippet document as received from Firestore, decoupled from the SDK's own document
    /// types. For an inline (non-chunked) image, ImageData already holds the decoded bytes. For
    /// a chunked image, ImageData is left null and FetchChunkedImageDataAsync is set instead -
    /// the chunk fetch is a real Firestore read, so IFirestoreSyncEventSink only pays for it when
    /// the snippet is genuinely new to it; an existing image snippet's pixels never change (SPEC:
    /// replacing one means delete+recreate with a new SyncId), so a sink that already knows this
    /// SyncId should never need to call it.
    /// </summary>
    public class RemoteSnippetSnapshot
    {
        public string SyncId { get; set; }

        public SnippetType Type { get; set; }

        public string Description { get; set; }

        public string Content { get; set; }

        public byte[] ImageData { get; set; }

        /// <summary>Non-null only for a chunked image - fetches and reassembles its imageChunks subcollection on demand.</summary>
        public Func<Task<byte[]>> FetchChunkedImageDataAsync { get; set; }

        public int Order { get; set; }

        public bool Locked { get; set; }

        public DateTime ModifiedAtUtc { get; set; }
    }
}
