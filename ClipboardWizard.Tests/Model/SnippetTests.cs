using ClipboardWizard.Model;

namespace ClipboardWizard.Tests.Model
{
    public class SnippetTests
    {
        [Fact]
        public void Locked_StartsFalse()
        {
            Snippet snippet = new();

            Assert.False(snippet.Locked);
        }

        [Fact]
        public void Locked_CanBeSetTrueOnce()
        {
            Snippet snippet = new();

            snippet.Locked = true;

            Assert.True(snippet.Locked);
        }

        [Fact]
        public void Locked_IgnoresFurtherAssignmentsOnceLocked()
        {
            Snippet snippet = new()
            {
                Locked = true
            };

            // A permanently-locked snippet must not be revertible, in code or by a stray
            // re-save - the setter is expected to silently no-op rather than throw.
            snippet.Locked = false;

            Assert.True(snippet.Locked);
        }

        [Fact]
        public void Locked_RaisesPropertyChangedOnlyOnTheTransitionToLocked()
        {
            Snippet snippet = new();
            List<string?> raised = new();
            snippet.PropertyChanged += (_, e) => raised.Add(e.PropertyName);

            snippet.Locked = true;
            snippet.Locked = true;

            Assert.Equal(new[] { nameof(Snippet.Locked) }, raised);
        }
    }
}
