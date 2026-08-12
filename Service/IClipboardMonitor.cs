using System;

namespace ClipboardWizard.Service
{
    public interface IClipboardMonitor
    {
        event EventHandler<string> TextCopied;

        string CurrentText { get; }
    }
}
