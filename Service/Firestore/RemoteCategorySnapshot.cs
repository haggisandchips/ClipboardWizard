using System;

namespace ClipboardWizard.Service.Firestore
{
    /// <summary>A category document as received from Firestore, decoupled from the SDK's own document types.</summary>
    public class RemoteCategorySnapshot
    {
        public string SyncId { get; set; }

        public string Name { get; set; }

        public int Order { get; set; }

        public DateTime ModifiedAtUtc { get; set; }
    }
}
