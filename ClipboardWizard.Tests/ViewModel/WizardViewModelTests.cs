using ClipboardWizard.Model;
using ClipboardWizard.Service;
using ClipboardWizard.Tests.Fakes;
using ClipboardWizard.ViewModel;

namespace ClipboardWizard.Tests.ViewModel
{
    public class WizardViewModelTests
    {
        private static (WizardViewModel ViewModel, FakeSnippetRepository Repository, FakeClipboardMonitor Clipboard) CreateSut()
        {
            FakeSnippetRepository repository = new();
            FakeClipboardMonitor clipboard = new();
            WizardViewModel viewModel = new(repository, clipboard);
            return (viewModel, repository, clipboard);
        }

        [Fact]
        public async Task LoadAsync_PopulatesSnippetsInOrder_AndFlagsFirstAndLast()
        {
            var (viewModel, repository, _) = CreateSut();
            repository.Snippets.Add(new Snippet { Id = 1, Content = "b", Order = 1 });
            repository.Snippets.Add(new Snippet { Id = 2, Content = "a", Order = 0 });
            repository.Snippets.Add(new Snippet { Id = 3, Content = "c", Order = 2 });

            await viewModel.LoadAsync();

            Assert.Equal(new[] { "a", "b", "c" }, viewModel.SnippetViewModels.Select(s => s.Snippet.Content));
            Assert.True(viewModel.SnippetViewModels[0].First);
            Assert.False(viewModel.SnippetViewModels[1].First);
            Assert.True(viewModel.SnippetViewModels[2].Last);
            Assert.False(viewModel.SnippetViewModels[1].Last);
        }

        [Fact]
        public async Task LoadAsync_MarksSnippetMatchingCurrentClipboardAsActive()
        {
            var (viewModel, repository, clipboard) = CreateSut();
            repository.Snippets.Add(new Snippet { Id = 1, Content = "match", Order = 0 });
            clipboard.CurrentContent = new ClipboardContent { Type = ClipboardContentType.Text, Text = "match" };

            await viewModel.LoadAsync();

            Assert.Equal(State.Active, viewModel.SnippetViewModels[0].State);
        }

        [Fact]
        public async Task ClipboardChanged_NoMatchAndNotRecording_DoesNotCreateSnippet()
        {
            var (viewModel, repository, clipboard) = CreateSut();
            await viewModel.LoadAsync();
            viewModel.Recording = false;

            clipboard.RaiseTextCopied("new content");

            Assert.Empty(viewModel.SnippetViewModels);
            Assert.Equal(0, repository.SaveCount);
        }

        [Fact]
        public async Task ClipboardChanged_NoMatchAndRecording_CreatesAndPersistsActiveSnippet()
        {
            var (viewModel, repository, clipboard) = CreateSut();
            await viewModel.LoadAsync();
            viewModel.Recording = true;

            clipboard.RaiseTextCopied("new content");
            await Task.Delay(50); // the auto-save runs fire-and-forget off the event handler

            Snippet saved = Assert.Single(viewModel.SnippetViewModels).Snippet;
            Assert.Equal("new content", saved.Content);
            Assert.Equal(State.Active, viewModel.SnippetViewModels[0].State);
            Assert.Equal(1, repository.SaveCount);
        }

        [Fact]
        public async Task ClipboardChanged_MatchesExistingSnippet_MarksItActiveWithoutDuplicating()
        {
            var (viewModel, repository, clipboard) = CreateSut();
            repository.Snippets.Add(new Snippet { Id = 1, Content = "existing", Order = 0 });
            await viewModel.LoadAsync();
            viewModel.Recording = true;

            clipboard.RaiseTextCopied("existing");
            await Task.Delay(50);

            Assert.Single(viewModel.SnippetViewModels);
            Assert.Equal(State.Active, viewModel.SnippetViewModels[0].State);
            Assert.Equal(0, repository.SaveCount);
        }

        [Fact]
        public async Task ClipboardChanged_UpdatesStateOfAllSnippetsEachTime()
        {
            var (viewModel, repository, clipboard) = CreateSut();
            repository.Snippets.Add(new Snippet { Id = 1, Content = "one", Order = 0 });
            repository.Snippets.Add(new Snippet { Id = 2, Content = "two", Order = 1 });
            await viewModel.LoadAsync();

            clipboard.RaiseTextCopied("two");

            Assert.Equal(State.Inactive, viewModel.SnippetViewModels[0].State);
            Assert.Equal(State.Active, viewModel.SnippetViewModels[1].State);

            clipboard.RaiseTextCopied("something else entirely");

            Assert.Equal(State.Inactive, viewModel.SnippetViewModels[0].State);
            Assert.Equal(State.Inactive, viewModel.SnippetViewModels[1].State);
        }

        [Fact]
        public async Task ClipboardChanged_ImageNoMatchAndRecording_CreatesAndPersistsActiveImageSnippet()
        {
            var (viewModel, repository, clipboard) = CreateSut();
            await viewModel.LoadAsync();
            viewModel.Recording = true;
            byte[] imageData = [1, 2, 3];

            clipboard.RaiseImageCopied(imageData);
            await Task.Delay(50);

            Snippet saved = Assert.Single(viewModel.SnippetViewModels).Snippet;
            Assert.Equal(SnippetType.Image, saved.Type);
            Assert.Equal(imageData, saved.ImageData);
            Assert.Equal(State.Active, viewModel.SnippetViewModels[0].State);
            Assert.Equal(1, repository.SaveCount);
        }

        [Fact]
        public async Task ClipboardChanged_ImageMatchesExistingImageSnippet_MarksItActiveWithoutDuplicating()
        {
            byte[] imageData = [4, 5, 6];
            var (viewModel, repository, clipboard) = CreateSut();
            repository.Snippets.Add(new Snippet { Id = 1, Type = SnippetType.Image, ImageData = imageData, Order = 0 });
            await viewModel.LoadAsync();
            viewModel.Recording = true;

            clipboard.RaiseImageCopied(imageData);
            await Task.Delay(50);

            Assert.Single(viewModel.SnippetViewModels);
            Assert.Equal(State.Active, viewModel.SnippetViewModels[0].State);
            Assert.Equal(0, repository.SaveCount);
        }

        [Fact]
        public async Task ClipboardChanged_ImageDoesNotMatchTextSnippet_EvenWithOverlappingBytes()
        {
            var (viewModel, repository, clipboard) = CreateSut();
            repository.Snippets.Add(new Snippet { Id = 1, Type = SnippetType.Text, Content = "abc", Order = 0 });
            await viewModel.LoadAsync();

            clipboard.RaiseImageCopied([1, 2, 3]);

            Assert.Equal(State.Inactive, viewModel.SnippetViewModels[0].State);
        }

        [Fact]
        public async Task SaveClipboardSnippetAsync_WithImageOnClipboard_CreatesImageSnippet()
        {
            var (viewModel, repository, clipboard) = CreateSut();
            await viewModel.LoadAsync();
            clipboard.CurrentContent = new ClipboardContent { Type = ClipboardContentType.Image, ImageData = [7, 8] };

            await viewModel.SaveClipboardSnippetAsync();

            Snippet saved = Assert.Single(viewModel.SnippetViewModels).Snippet;
            Assert.Equal(SnippetType.Image, saved.Type);
        }

        [Theory]
        [InlineData(null, null, false)]
        [InlineData("", null, false)]
        [InlineData("text", null, true)]
        public async Task HasSaveableClipboardContent_ReflectsTextContent(string? text, byte[]? imageData, bool expected)
        {
            var (viewModel, _, clipboard) = CreateSut();
            await viewModel.LoadAsync();
            clipboard.CurrentContent = text != null || imageData != null
                ? new ClipboardContent { Type = ClipboardContentType.Text, Text = text, ImageData = imageData }
                : null;

            Assert.Equal(expected, viewModel.HasSaveableClipboardContent);
        }

        [Fact]
        public async Task HasSaveableClipboardContent_TrueForNonEmptyImage()
        {
            var (viewModel, _, clipboard) = CreateSut();
            await viewModel.LoadAsync();
            clipboard.CurrentContent = new ClipboardContent { Type = ClipboardContentType.Image, ImageData = [1] };

            Assert.True(viewModel.HasSaveableClipboardContent);
        }

        [Fact]
        public async Task RemoveSnippetAsync_DeletesPersistsAndRecomputesEdgeFlags()
        {
            var (viewModel, repository, _) = CreateSut();
            repository.Snippets.Add(new Snippet { Id = 1, Content = "a", Order = 0 });
            repository.Snippets.Add(new Snippet { Id = 2, Content = "b", Order = 1 });
            await viewModel.LoadAsync();
            SnippetViewModel first = viewModel.SnippetViewModels[0];

            await viewModel.RemoveSnippetAsync(first);

            SnippetViewModel remaining = Assert.Single(viewModel.SnippetViewModels);
            Assert.Equal("b", remaining.Snippet.Content);
            Assert.True(remaining.First);
            Assert.True(remaining.Last);
            Assert.Equal(1, repository.DeleteCount);
        }

        [Fact]
        public async Task MoveSnippetUpAsync_SwapsOrderInCollectionAndPersistsBothSides()
        {
            var (viewModel, repository, _) = CreateSut();
            repository.Snippets.Add(new Snippet { Id = 1, Content = "a", Order = 0 });
            repository.Snippets.Add(new Snippet { Id = 2, Content = "b", Order = 1 });
            await viewModel.LoadAsync();
            SnippetViewModel second = viewModel.SnippetViewModels[1];

            await viewModel.MoveSnippetUpAsync(second);

            Assert.Equal(new[] { "b", "a" }, viewModel.SnippetViewModels.Select(s => s.Snippet.Content));
            Assert.True(viewModel.SnippetViewModels[0].First);
            Assert.True(viewModel.SnippetViewModels[1].Last);
            Assert.Equal(2, repository.UpdateCount);
        }

        [Fact]
        public async Task MoveSnippetUpAsync_AtTop_IsNoOp()
        {
            var (viewModel, repository, _) = CreateSut();
            repository.Snippets.Add(new Snippet { Id = 1, Content = "a", Order = 0 });
            repository.Snippets.Add(new Snippet { Id = 2, Content = "b", Order = 1 });
            await viewModel.LoadAsync();
            SnippetViewModel first = viewModel.SnippetViewModels[0];

            await viewModel.MoveSnippetUpAsync(first);

            Assert.Equal(new[] { "a", "b" }, viewModel.SnippetViewModels.Select(s => s.Snippet.Content));
            Assert.Equal(0, repository.UpdateCount);
        }

        [Fact]
        public async Task MoveSnippetDownAsync_AtBottom_IsNoOp()
        {
            var (viewModel, repository, _) = CreateSut();
            repository.Snippets.Add(new Snippet { Id = 1, Content = "a", Order = 0 });
            repository.Snippets.Add(new Snippet { Id = 2, Content = "b", Order = 1 });
            await viewModel.LoadAsync();
            SnippetViewModel last = viewModel.SnippetViewModels[1];

            await viewModel.MoveSnippetDownAsync(last);

            Assert.Equal(new[] { "a", "b" }, viewModel.SnippetViewModels.Select(s => s.Snippet.Content));
            Assert.Equal(0, repository.UpdateCount);
        }

        [Fact]
        public async Task MoveSnippetToAsync_InsertBefore_AndRenumbersOrderToMatchNewPositions()
        {
            var (viewModel, repository, _) = CreateSut();
            repository.Snippets.Add(new Snippet { Id = 1, Content = "a", Order = 0 });
            repository.Snippets.Add(new Snippet { Id = 2, Content = "b", Order = 1 });
            repository.Snippets.Add(new Snippet { Id = 3, Content = "c", Order = 2 });
            await viewModel.LoadAsync();
            SnippetViewModel dragged = viewModel.SnippetViewModels[2]; // "c"
            SnippetViewModel target = viewModel.SnippetViewModels[0]; // "a"

            await viewModel.MoveSnippetToAsync(dragged, target, insertBefore: true);

            Assert.Equal(new[] { "c", "a", "b" }, viewModel.SnippetViewModels.Select(s => s.Snippet.Content));
            Assert.Equal(new[] { 0, 1, 2 }, viewModel.SnippetViewModels.Select(s => s.Snippet.Order));
            Assert.True(viewModel.SnippetViewModels[0].First);
            Assert.True(viewModel.SnippetViewModels[2].Last);
        }

        [Fact]
        public async Task MoveSnippetToAsync_InsertAfter()
        {
            var (viewModel, repository, _) = CreateSut();
            repository.Snippets.Add(new Snippet { Id = 1, Content = "a", Order = 0 });
            repository.Snippets.Add(new Snippet { Id = 2, Content = "b", Order = 1 });
            repository.Snippets.Add(new Snippet { Id = 3, Content = "c", Order = 2 });
            await viewModel.LoadAsync();
            SnippetViewModel dragged = viewModel.SnippetViewModels[0]; // "a"
            SnippetViewModel target = viewModel.SnippetViewModels[2]; // "c"

            await viewModel.MoveSnippetToAsync(dragged, target, insertBefore: false);

            Assert.Equal(new[] { "b", "c", "a" }, viewModel.SnippetViewModels.Select(s => s.Snippet.Content));
        }

        [Fact]
        public async Task MoveSnippetToAsync_InsertBefore_IsBefore_RegardlessOfDragDirection()
        {
            // Dragging FORWARD (an earlier item onto a later one) but asking to insert
            // before it must still land before - this is the bug being guarded against:
            // naively calling ObservableCollection.Move(current, targetIndex) lands *after*
            // the target when dragging forward, regardless of where the user actually
            // dropped within the tile.
            var (viewModel, repository, _) = CreateSut();
            repository.Snippets.Add(new Snippet { Id = 1, Content = "a", Order = 0 });
            repository.Snippets.Add(new Snippet { Id = 2, Content = "b", Order = 1 });
            repository.Snippets.Add(new Snippet { Id = 3, Content = "c", Order = 2 });
            repository.Snippets.Add(new Snippet { Id = 4, Content = "d", Order = 3 });
            await viewModel.LoadAsync();
            SnippetViewModel dragged = viewModel.SnippetViewModels[0]; // "a"
            SnippetViewModel target = viewModel.SnippetViewModels[2]; // "c"

            await viewModel.MoveSnippetToAsync(dragged, target, insertBefore: true);

            Assert.Equal(new[] { "b", "a", "c", "d" }, viewModel.SnippetViewModels.Select(s => s.Snippet.Content));
        }

        [Fact]
        public async Task MoveSnippetToAsync_InsertAfter_IsAfter_RegardlessOfDragDirection()
        {
            // The mirror image: dragging BACKWARD but asking to insert after must still
            // land after, not before.
            var (viewModel, repository, _) = CreateSut();
            repository.Snippets.Add(new Snippet { Id = 1, Content = "a", Order = 0 });
            repository.Snippets.Add(new Snippet { Id = 2, Content = "b", Order = 1 });
            repository.Snippets.Add(new Snippet { Id = 3, Content = "c", Order = 2 });
            repository.Snippets.Add(new Snippet { Id = 4, Content = "d", Order = 3 });
            await viewModel.LoadAsync();
            SnippetViewModel dragged = viewModel.SnippetViewModels[3]; // "d"
            SnippetViewModel target = viewModel.SnippetViewModels[1]; // "b"

            await viewModel.MoveSnippetToAsync(dragged, target, insertBefore: false);

            Assert.Equal(new[] { "a", "b", "d", "c" }, viewModel.SnippetViewModels.Select(s => s.Snippet.Content));
        }

        [Fact]
        public async Task MoveSnippetToAsync_OnlyPersistsSnippetsWhoseOrderActuallyChanged()
        {
            var (viewModel, repository, _) = CreateSut();
            repository.Snippets.Add(new Snippet { Id = 1, Content = "a", Order = 0 });
            repository.Snippets.Add(new Snippet { Id = 2, Content = "b", Order = 1 });
            repository.Snippets.Add(new Snippet { Id = 3, Content = "c", Order = 2 });
            repository.Snippets.Add(new Snippet { Id = 4, Content = "d", Order = 3 });
            await viewModel.LoadAsync();
            // Swapping the last two leaves the first two untouched.
            SnippetViewModel dragged = viewModel.SnippetViewModels[3]; // "d"
            SnippetViewModel target = viewModel.SnippetViewModels[2]; // "c"

            await viewModel.MoveSnippetToAsync(dragged, target, insertBefore: true);

            Assert.Equal(new[] { "a", "b", "d", "c" }, viewModel.SnippetViewModels.Select(s => s.Snippet.Content));
            Assert.Equal(2, repository.UpdateCount);
        }

        [Fact]
        public async Task MoveSnippetToAsync_ToSamePosition_IsNoOp()
        {
            var (viewModel, repository, _) = CreateSut();
            repository.Snippets.Add(new Snippet { Id = 1, Content = "a", Order = 0 });
            repository.Snippets.Add(new Snippet { Id = 2, Content = "b", Order = 1 });
            await viewModel.LoadAsync();
            SnippetViewModel first = viewModel.SnippetViewModels[0];

            await viewModel.MoveSnippetToAsync(first, first, insertBefore: true);

            Assert.Equal(new[] { "a", "b" }, viewModel.SnippetViewModels.Select(s => s.Snippet.Content));
            Assert.Equal(0, repository.UpdateCount);
        }

        [Theory]
        [InlineData(true)] // dropping on the left half of your own immediate successor...
        [InlineData(false)] // ...or the right half of your own immediate predecessor
        public async Task MoveSnippetToAsync_ToItsOwnCurrentPosition_IsNoOp(bool insertBefore)
        {
            var (viewModel, repository, _) = CreateSut();
            repository.Snippets.Add(new Snippet { Id = 1, Content = "a", Order = 0 });
            repository.Snippets.Add(new Snippet { Id = 2, Content = "b", Order = 1 });
            repository.Snippets.Add(new Snippet { Id = 3, Content = "c", Order = 2 });
            await viewModel.LoadAsync();
            SnippetViewModel middle = viewModel.SnippetViewModels[1]; // "b"
            SnippetViewModel target = insertBefore ? viewModel.SnippetViewModels[2] : viewModel.SnippetViewModels[0];

            await viewModel.MoveSnippetToAsync(middle, target, insertBefore);

            Assert.Equal(new[] { "a", "b", "c" }, viewModel.SnippetViewModels.Select(s => s.Snippet.Content));
            Assert.Equal(0, repository.UpdateCount);
        }
    }
}
