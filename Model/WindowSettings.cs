namespace ClipboardWizard.Model
{
    /// <summary>Persisted window placement, restored on the next launch.</summary>
    public class WindowSettings
    {
        public double Left { get; set; }

        public double Top { get; set; }

        public double Width { get; set; }

        public double Height { get; set; }

        public bool IsMaximized { get; set; }
    }
}
