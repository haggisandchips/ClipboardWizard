namespace ClipboardWizard.Service
{
    public enum ClipboardContentType
    {
        Text,
        Image
    }

    /// <summary>
    /// A snapshot of clipboard content in whichever of the supported formats it's in.
    /// Only one of Text/ImageData is meaningful, matching Type.
    /// </summary>
    public sealed record ClipboardContent
    {
        public required ClipboardContentType Type { get; init; }

        public string Text { get; init; }

        public byte[] ImageData { get; init; }
    }
}
