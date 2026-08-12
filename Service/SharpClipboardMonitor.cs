using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media.Imaging;
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

        /// <summary>
        /// Queries the OS clipboard directly rather than SharpClipboard's ClipboardText/
        /// ClipboardImage properties - those turned out to be last-seen-per-format caches,
        /// not live state, so ClipboardText kept reporting stale text after copying an image
        /// over it. Image is checked first: a plain "copy image" action is what this exists
        /// to fix, and apps that copy both rarely mean for the text to win.
        /// </summary>
        public ClipboardContent CurrentContent
        {
            get
            {
                try
                {
                    if (Clipboard.ContainsImage())
                    {
                        BitmapSource image = Clipboard.GetImage();
                        if (image != null)
                        {
                            return new ClipboardContent { Type = ClipboardContentType.Image, ImageData = ImageCodec.EncodePng(image) };
                        }
                    }

                    if (Clipboard.ContainsText())
                    {
                        string text = Clipboard.GetText();
                        if (!string.IsNullOrWhiteSpace(text))
                        {
                            return new ClipboardContent { Type = ClipboardContentType.Text, Text = text };
                        }
                    }
                }
                catch (COMException)
                {
                    // The clipboard is a shared OS resource that can be transiently locked by
                    // another process. This getter backs CanExecute checks, which must never
                    // throw, so treat that the same as "nothing available right now".
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
