using ClipboardWizard.Service;

namespace ClipboardWizard.Tests.Fakes
{
    internal class FakeClipboardMonitor : IClipboardMonitor
    {
        public event EventHandler<ClipboardContent>? ContentCopied;

        public ClipboardContent? CurrentContent { get; set; }

        public void RaiseTextCopied(string text)
        {
            ClipboardContent content = new() { Type = ClipboardContentType.Text, Text = text };
            CurrentContent = content;
            ContentCopied?.Invoke(this, content);
        }

        public void RaiseImageCopied(byte[] imageData)
        {
            ClipboardContent content = new() { Type = ClipboardContentType.Image, ImageData = imageData };
            CurrentContent = content;
            ContentCopied?.Invoke(this, content);
        }
    }
}
