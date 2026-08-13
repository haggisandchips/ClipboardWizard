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

        /// <summary>
        /// Expanded state of the pinned Uncategorized accordion section - not domain data, so it
        /// lives here (persisted on close, like the rest of this leftover UI arrangement) rather
        /// than in SQLite alongside real categories' IsExpanded.
        /// </summary>
        public bool UncategorizedExpanded { get; set; } = true;
    }
}
