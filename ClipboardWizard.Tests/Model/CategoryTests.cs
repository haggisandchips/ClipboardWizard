using ClipboardWizard.Model;

namespace ClipboardWizard.Tests.Model
{
    public class CategoryTests
    {
        [Fact]
        public void Shared_StartsFalse()
        {
            Category category = new();

            Assert.False(category.Shared);
        }

        [Fact]
        public void Shared_CanBeSetTrueOnce()
        {
            Category category = new();

            category.Shared = true;

            Assert.True(category.Shared);
        }

        [Fact]
        public void Shared_IgnoresFurtherAssignmentsOnceShared()
        {
            Category category = new()
            {
                Shared = true
            };

            // Once Shared, it must not be revertible, in code or by a stray re-save - the
            // setter is expected to silently no-op rather than throw.
            category.Shared = false;

            Assert.True(category.Shared);
        }

        [Fact]
        public void Shared_RaisesPropertyChangedOnlyOnTheTransitionToShared()
        {
            Category category = new();
            List<string?> raised = new();
            category.PropertyChanged += (_, e) => raised.Add(e.PropertyName);

            category.Shared = true;
            category.Shared = true;

            Assert.Equal(new[] { nameof(Category.Shared) }, raised);
        }
    }
}
