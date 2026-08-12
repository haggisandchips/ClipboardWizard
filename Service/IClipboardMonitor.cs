using System;

namespace ClipboardWizard.Service
{
    public interface IClipboardMonitor
    {
        event EventHandler<ClipboardContent> ContentCopied;

        /// <summary>Null if the clipboard currently holds nothing in a supported format.</summary>
        ClipboardContent CurrentContent { get; }
    }
}
