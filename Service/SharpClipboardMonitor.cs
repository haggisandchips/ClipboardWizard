using System;
using System.Drawing;
using WK.Libraries.SharpClipboardNS;

namespace ClipboardWizard.Service
{
    /// <summary>
    /// Adapts the third-party SharpClipboard monitor to IClipboardMonitor so the rest of the
    /// app only depends on our own, easily-fakeable abstraction, and only knows about the
    /// content types we actually support (text, image) rather than SharpClipboard's full set.
    /// </summary>
    public class SharpClipboardMonitor : IClipboardMonitor, IDisposable
    {
        private readonly SharpClipboard _clipboard;

        public event EventHandler<ClipboardContent> ContentCopied;

        public SharpClipboardMonitor(SharpClipboard clipboard)
        {
            _clipboard = clipboard;
            _clipboard.ClipboardChanged += OnClipboardChanged;
        }

        public ClipboardContent CurrentContent
        {
            get
            {
                string text = _clipboard.ClipboardText;
                if (!string.IsNullOrWhiteSpace(text))
                {
                    return new ClipboardContent { Type = ClipboardContentType.Text, Text = text };
                }

                Image image = _clipboard.ClipboardImage;
                if (image != null)
                {
                    return new ClipboardContent { Type = ClipboardContentType.Image, ImageData = ImageCodec.EncodePng(image) };
                }

                return null;
            }
        }

        private void OnClipboardChanged(object sender, SharpClipboard.ClipboardChangedEventArgs e)
        {
            ClipboardContent content = e.ContentType switch
            {
                SharpClipboard.ContentTypes.Text => new ClipboardContent
                {
                    Type = ClipboardContentType.Text,
                    Text = e.Content?.ToString() ?? string.Empty
                },
                SharpClipboard.ContentTypes.Image when e.Content is Image image => new ClipboardContent
                {
                    Type = ClipboardContentType.Image,
                    ImageData = ImageCodec.EncodePng(image)
                },
                _ => null
            };

            if (content != null)
            {
                ContentCopied?.Invoke(this, content);
            }
        }

        public void Dispose()
        {
            _clipboard.ClipboardChanged -= OnClipboardChanged;
        }
    }
}
