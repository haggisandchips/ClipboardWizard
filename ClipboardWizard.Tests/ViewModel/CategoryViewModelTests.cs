using ClipboardWizard.Model;
using ClipboardWizard.Tests.Fakes;
using ClipboardWizard.ViewModel;

namespace ClipboardWizard.Tests.ViewModel
{
    public class CategoryViewModelTests
    {
        [Fact]
        public async Task MoveToAsync_DelegatesToHostWithSelfTargetAndSide()
        {
            FakeCategoryHost host = new();
            CategoryViewModel dragged = new(new Category(), host);
            CategoryViewModel target = new(new Category(), host);

            await dragged.MoveToAsync(target, insertBefore: true);

            var move = Assert.Single(host.MovedTo);
            Assert.Same(dragged, move.Category);
            Assert.Same(target, move.Target);
            Assert.True(move.InsertBefore);
        }

        // DeleteCategoryAsync itself isn't unit tested: it shows a real confirmation
        // MessageBox, which needs a running WPF Application - see the equivalent note on
        // SnippetViewModel.EditSnippetAsync in SnippetViewModelTests.

        [Fact]
        public async Task ToggleExpandedAsync_FlipsIsExpandedAndPersists()
        {
            FakeCategoryHost host = new();
            Category category = new() { IsExpanded = true };
            CategoryViewModel viewModel = new(category, host);

            await viewModel.ToggleExpandedAsync();

            Assert.False(viewModel.IsExpanded);
            Assert.False(category.IsExpanded);
            Assert.Single(host.UpdatedCategories, category);
        }

        [Fact]
        public async Task ToggleExpandedAsync_TogglesBothWays()
        {
            FakeCategoryHost host = new();
            CategoryViewModel viewModel = new(new Category { IsExpanded = false }, host);

            await viewModel.ToggleExpandedAsync();
            Assert.True(viewModel.IsExpanded);

            await viewModel.ToggleExpandedAsync();
            Assert.False(viewModel.IsExpanded);
        }

        [Fact]
        public async Task AddNewSnippetAsync_DelegatesToHostWithSelf()
        {
            FakeCategoryHost host = new();
            CategoryViewModel viewModel = new(new Category(), host);

            await viewModel.AddNewSnippetAsync();

            Assert.Same(viewModel, Assert.Single(host.AddSnippetCalls));
        }

        [Fact]
        public async Task SaveClipboardSnippetAsync_DelegatesToHostWithSelf()
        {
            FakeCategoryHost host = new();
            CategoryViewModel viewModel = new(new Category(), host);

            await viewModel.SaveClipboardSnippetAsync();

            Assert.Same(viewModel, Assert.Single(host.SaveClipboardSnippetCalls));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void HasSaveableClipboardContent_ReflectsHost(bool hasContent)
        {
            FakeCategoryHost host = new() { HasSaveableClipboardContent = hasContent };
            CategoryViewModel viewModel = new(new Category(), host);

            Assert.Equal(hasContent, viewModel.HasSaveableClipboardContent);
        }

        [Fact]
        public void IsPinned_IsAlwaysFalse()
        {
            CategoryViewModel viewModel = new(new Category(), new FakeCategoryHost());

            Assert.False(viewModel.IsPinned);
        }

        [Fact]
        public void PropertyChanged_WhenCategoryChanges_FiresWithTheViewModelAsSender()
        {
            // WPF's binding/weak-event machinery keys its listener registry by the object
            // bindings were registered against (this view model, since that's what's set as a
            // control's DataContext) - a notification arriving with a different sender (e.g.
            // the wrapped Category) is silently unmatched and dropped, so bound UI never
            // updates even though the underlying value did change.
            Category category = new();
            CategoryViewModel viewModel = new(category, new FakeCategoryHost());
            object? raisedBy = null;
            viewModel.PropertyChanged += (sender, _) => raisedBy = sender;

            category.IsExpanded = !category.IsExpanded;

            Assert.Same(viewModel, raisedBy);
        }
    }
}
