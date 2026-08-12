using ClipboardWizard.Service;

namespace ClipboardWizard.Tests.Fakes
{
    internal class FakeClipboardMonitor : IClipboardMonitor
    {
        public event EventHandler<string>? TextCopied;

        public string CurrentText { get; set; } = string.Empty;

        public void RaiseTextCopied(string content)
        {
            CurrentText = content;
            TextCopied?.Invoke(this, content);
        }
    }
}
