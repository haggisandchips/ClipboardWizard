using System;
using WK.Libraries.SharpClipboardNS;

namespace ClipboardWizard.Service
{
    /// <summary>
    /// Adapts the third-party SharpClipboard monitor to IClipboardMonitor so the rest of the
    /// app only depends on our own, easily-fakeable abstraction.
    /// </summary>
    public class SharpClipboardMonitor : IClipboardMonitor, IDisposable
    {
        private readonly SharpClipboard _clipboard;

        public event EventHandler<string> TextCopied;

        public SharpClipboardMonitor(SharpClipboard clipboard)
        {
            _clipboard = clipboard;
            _clipboard.ClipboardChanged += OnClipboardChanged;
        }

        public string CurrentText => _clipboard.ClipboardText;

        private void OnClipboardChanged(object sender, SharpClipboard.ClipboardChangedEventArgs e)
        {
            if (e.ContentType != SharpClipboard.ContentTypes.Text)
            {
                return;
            }

            TextCopied?.Invoke(this, e.Content?.ToString() ?? string.Empty);
        }

        public void Dispose()
        {
            _clipboard.ClipboardChanged -= OnClipboardChanged;
        }
    }
}
