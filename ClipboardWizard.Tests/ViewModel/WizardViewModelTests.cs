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
            var (viewModel, repository, _, clipboard) = CreateSutWithCategories();
            return (viewModel, repository, clipboard);
        }

        private static (WizardViewModel ViewModel, FakeSnippetRepository Repository, FakeCategoryRepository CategoryRepository, FakeClipboardMonitor Clipboard) CreateSutWithCategories()
        {
            FakeSnippetRepository repository = new();
            FakeCategoryRepository categoryRepository = new();
            FakeClipboardMonitor clipboard = new();
            WizardViewModel viewModel = new(repository, categoryRepository, clipboard);
            return (viewModel, repository, categoryRepository, clipboard);
        }

        [Fact]
        public async Task LoadAsync_PopulatesSnippetsInOrder()
        {
            var (viewModel, repository, _) = CreateSut();
            repository.Snippets.Add(new Snippet { Id = 1, Content = "b", Order = 1 });
            repository.Snippets.Add(new Snippet { Id = 2, Content = "a", Order = 0 });
            repository.Snippets.Add(new Snippet { Id = 3, Content = "c", Order = 2 });

            await viewModel.LoadAsync();

            Assert.Equal(new[] { "a", "b", "c" }, viewModel.SnippetViewModels.Select(s => s.Snippet.Content));
        }

        [Fact]
        public async Task LoadAsync_PutsSnippetsIntoTheirCategoryBuckets()
        {
            var (viewModel, repository, categoryRepository, _) = CreateSutWithCategories();
            categoryRepository.Categories.Add(new Category { Id = 1, Name = "Work", Order = 0 });
            repository.Snippets.Add(new Snippet { Id = 1, Content = "categorized", Order = 0, CategoryId = 1 });
            repository.Snippets.Add(new Snippet { Id = 2, Content = "uncategorized", Order = 0, CategoryId = null });

            await viewModel.LoadAsync();

            CategoryViewModel category = Assert.Single(viewModel.Categories);
            Assert.Equal("categorized", Assert.Single(category.Snippets).Snippet.Content);
            Assert.Equal("uncategorized", Assert.Single(viewModel.UncategorizedSection.Snippets).Snippet.Content);
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
        public async Task RemoveSnippetAsync_DeletesAndPersists()
        {
            var (viewModel, repository, _) = CreateSut();
            repository.Snippets.Add(new Snippet { Id = 1, Content = "a", Order = 0 });
            repository.Snippets.Add(new Snippet { Id = 2, Content = "b", Order = 1 });
            await viewModel.LoadAsync();
            SnippetViewModel first = viewModel.SnippetViewModels[0];

            await viewModel.RemoveSnippetAsync(first);

            SnippetViewModel remaining = Assert.Single(viewModel.SnippetViewModels);
            Assert.Equal("b", remaining.Snippet.Content);
            Assert.Equal(1, repository.DeleteCount);
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

            Assert.Equal(new[] { "c", "a", "b" }, viewModel.UncategorizedSection.Snippets.Select(s => s.Snippet.Content));
            Assert.Equal(new[] { 0, 1, 2 }, viewModel.UncategorizedSection.Snippets.Select(s => s.Snippet.Order));
        }

        [Fact]
        public async Task WouldMoveSnippet_OnItself_IsFalse()
        {
            var (viewModel, repository, _) = CreateSut();
            repository.Snippets.Add(new Snippet { Id = 1, Content = "a", Order = 0 });
            await viewModel.LoadAsync();
            SnippetViewModel snippet = viewModel.SnippetViewModels[0];

            Assert.False(viewModel.WouldMoveSnippet(snippet, snippet, insertBefore: true));
        }

        [Theory]
        [InlineData(true)] // dropping on the left half of your own immediate successor...
        [InlineData(false)] // ...or the right half of your own immediate predecessor
        public async Task WouldMoveSnippet_ForItsOwnCurrentPosition_IsFalse(bool insertBefore)
        {
            var (viewModel, repository, _) = CreateSut();
            repository.Snippets.Add(new Snippet { Id = 1, Content = "a", Order = 0 });
            repository.Snippets.Add(new Snippet { Id = 2, Content = "b", Order = 1 });
            repository.Snippets.Add(new Snippet { Id = 3, Content = "c", Order = 2 });
            await viewModel.LoadAsync();
            SnippetViewModel middle = viewModel.SnippetViewModels[1]; // "b"
            SnippetViewModel target = insertBefore ? viewModel.SnippetViewModels[2] : viewModel.SnippetViewModels[0];

            Assert.False(viewModel.WouldMoveSnippet(middle, target, insertBefore));
        }

        [Fact]
        public async Task WouldMoveSnippet_ForADifferentPositionInTheSameCategory_IsTrue()
        {
            var (viewModel, repository, _) = CreateSut();
            repository.Snippets.Add(new Snippet { Id = 1, Content = "a", Order = 0 });
            repository.Snippets.Add(new Snippet { Id = 2, Content = "b", Order = 1 });
            repository.Snippets.Add(new Snippet { Id = 3, Content = "c", Order = 2 });
            await viewModel.LoadAsync();
            SnippetViewModel dragged = viewModel.SnippetViewModels[2]; // "c"
            SnippetViewModel target = viewModel.SnippetViewModels[0]; // "a"

            Assert.True(viewModel.WouldMoveSnippet(dragged, target, insertBefore: true));
        }

        [Fact]
        public async Task WouldMoveSnippet_ForADifferentCategory_IsAlwaysTrue()
        {
            var (viewModel, repository, categoryRepository, _) = CreateSutWithCategories();
            categoryRepository.Categories.Add(new Category { Id = 1, Name = "Work", Order = 0 });
            repository.Snippets.Add(new Snippet { Id = 1, Content = "a", Order = 0, CategoryId = null });
            repository.Snippets.Add(new Snippet { Id = 2, Content = "b", Order = 0, CategoryId = 1 });
            await viewModel.LoadAsync();
            SnippetViewModel uncategorized = viewModel.SnippetViewModels.Single(s => s.Snippet.Content == "a");
            SnippetViewModel categorized = viewModel.SnippetViewModels.Single(s => s.Snippet.Content == "b");

            // Even dropping on the near side of the only item already in that category -
            // which would be a no-op for a same-category drop - is still a real change here,
            // since it also recategorizes.
            Assert.True(viewModel.WouldMoveSnippet(uncategorized, categorized, insertBefore: true));
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

            Assert.Equal(new[] { "b", "c", "a" }, viewModel.UncategorizedSection.Snippets.Select(s => s.Snippet.Content));
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

            Assert.Equal(new[] { "b", "a", "c", "d" }, viewModel.UncategorizedSection.Snippets.Select(s => s.Snippet.Content));
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

            Assert.Equal(new[] { "a", "b", "d", "c" }, viewModel.UncategorizedSection.Snippets.Select(s => s.Snippet.Content));
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

            Assert.Equal(new[] { "a", "b", "d", "c" }, viewModel.UncategorizedSection.Snippets.Select(s => s.Snippet.Content));
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

            Assert.Equal(new[] { "a", "b" }, viewModel.UncategorizedSection.Snippets.Select(s => s.Snippet.Content));
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

            Assert.Equal(new[] { "a", "b", "c" }, viewModel.UncategorizedSection.Snippets.Select(s => s.Snippet.Content));
            Assert.Equal(0, repository.UpdateCount);
        }

        [Fact]
        public async Task MoveSnippetToAsync_AcrossCategories_RecategorizesAndPositionsNearTarget()
        {
            var (viewModel, repository, categoryRepository, _) = CreateSutWithCategories();
            categoryRepository.Categories.Add(new Category { Id = 1, Name = "Work", Order = 0 });
            repository.Snippets.Add(new Snippet { Id = 1, Content = "a", Order = 0, CategoryId = null });
            repository.Snippets.Add(new Snippet { Id = 2, Content = "b", Order = 0, CategoryId = 1 });
            repository.Snippets.Add(new Snippet { Id = 3, Content = "c", Order = 1, CategoryId = 1 });
            await viewModel.LoadAsync();
            SnippetViewModel dragged = viewModel.UncategorizedSection.Snippets.Single(s => s.Snippet.Content == "a");
            SnippetViewModel target = viewModel.Categories[0].Snippets.Single(s => s.Snippet.Content == "c");

            await viewModel.MoveSnippetToAsync(dragged, target, insertBefore: true);

            Assert.Empty(viewModel.UncategorizedSection.Snippets);
            Assert.Equal(new[] { "b", "a", "c" }, viewModel.Categories[0].Snippets.Select(s => s.Snippet.Content));
            Assert.Equal(1, dragged.Snippet.CategoryId);
        }

        [Fact]
        public async Task AddCategoryAsync_PersistsAndAddsToCategories()
        {
            var (viewModel, _, categoryRepository, _) = CreateSutWithCategories();

            await viewModel.AddCategoryAsync("Work");

            CategoryViewModel added = Assert.Single(viewModel.Categories);
            Assert.Equal("Work", added.Category.Name);
            Assert.Equal(1, categoryRepository.SaveCount);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task AddCategoryAsync_WithBlankName_IsNoOp(string? name)
        {
            var (viewModel, _, categoryRepository, _) = CreateSutWithCategories();

            await viewModel.AddCategoryAsync(name!);

            Assert.Empty(viewModel.Categories);
            Assert.Equal(0, categoryRepository.SaveCount);
        }

        [Fact]
        public async Task DeleteCategoryAsync_RemovesCategoryAndUncategorizesItsSnippets()
        {
            var (viewModel, repository, categoryRepository, _) = CreateSutWithCategories();
            categoryRepository.Categories.Add(new Category { Id = 1, Name = "Work", Order = 0 });
            repository.Snippets.Add(new Snippet { Id = 1, Content = "a", Order = 0, CategoryId = 1 });
            repository.Snippets.Add(new Snippet { Id = 2, Content = "b", Order = 1, CategoryId = null });
            await viewModel.LoadAsync();
            CategoryViewModel category = Assert.Single(viewModel.Categories);

            await viewModel.DeleteCategoryAsync(category);

            Assert.Empty(viewModel.Categories);
            Assert.Equal(1, categoryRepository.DeleteCount);
            Assert.All(viewModel.SnippetViewModels, s => Assert.Null(s.Snippet.CategoryId));
            Assert.Equal(2, viewModel.UncategorizedSection.Snippets.Count);
        }

        [Fact]
        public async Task AssignCategoryAsync_SetsCategoryIdAndPersists()
        {
            var (viewModel, repository, categoryRepository, _) = CreateSutWithCategories();
            categoryRepository.Categories.Add(new Category { Id = 1, Name = "Work", Order = 0 });
            repository.Snippets.Add(new Snippet { Id = 1, Content = "a", Order = 0 });
            await viewModel.LoadAsync();
            CategoryViewModel category = Assert.Single(viewModel.Categories);
            SnippetViewModel snippet = Assert.Single(viewModel.SnippetViewModels);

            await viewModel.AssignCategoryAsync(snippet, category);

            Assert.Equal(1, snippet.Snippet.CategoryId);
            Assert.Equal(1, repository.UpdateCount);
        }

        [Fact]
        public async Task AssignCategoryAsync_ByCategoryId_MovesSnippetIntoThatCategorysBucket()
        {
            // Regression test: this is the overload the edit dialog goes through
            // (SnippetViewModel.EditSnippetAsync) - it must move the snippet between
            // ICategorySection.Snippets collections, not just flip Snippet.CategoryId, or the
            // accordion still shows it under its old section and a subsequent drag into the
            // new category is treated as a no-op (CategoryId already matches).
            var (viewModel, repository, categoryRepository, _) = CreateSutWithCategories();
            categoryRepository.Categories.Add(new Category { Id = 1, Name = "Work", Order = 0 });
            repository.Snippets.Add(new Snippet { Id = 1, Content = "a", Order = 0, CategoryId = null });
            await viewModel.LoadAsync();
            CategoryViewModel category = Assert.Single(viewModel.Categories);
            SnippetViewModel snippet = Assert.Single(viewModel.UncategorizedSection.Snippets);

            await viewModel.AssignCategoryAsync(snippet, category.Category.Id);

            Assert.Equal(1, snippet.Snippet.CategoryId);
            Assert.Contains(snippet, category.Snippets);
            Assert.DoesNotContain(snippet, viewModel.UncategorizedSection.Snippets);
        }

        [Fact]
        public async Task AssignCategoryAsync_ByCategoryId_Null_MovesSnippetToUncategorizedBucket()
        {
            var (viewModel, repository, categoryRepository, _) = CreateSutWithCategories();
            categoryRepository.Categories.Add(new Category { Id = 1, Name = "Work", Order = 0 });
            repository.Snippets.Add(new Snippet { Id = 1, Content = "a", Order = 0, CategoryId = 1 });
            await viewModel.LoadAsync();
            CategoryViewModel category = Assert.Single(viewModel.Categories);
            SnippetViewModel snippet = Assert.Single(category.Snippets);

            await viewModel.AssignCategoryAsync(snippet, (int?)null);

            Assert.Null(snippet.Snippet.CategoryId);
            Assert.Contains(snippet, viewModel.UncategorizedSection.Snippets);
            Assert.DoesNotContain(snippet, category.Snippets);
        }

        [Fact]
        public async Task AssignCategoryAsync_ToUncategorizedSection_ClearsCategoryId()
        {
            var (viewModel, repository, categoryRepository, _) = CreateSutWithCategories();
            categoryRepository.Categories.Add(new Category { Id = 1, Name = "Work", Order = 0 });
            repository.Snippets.Add(new Snippet { Id = 1, Content = "a", Order = 0, CategoryId = 1 });
            await viewModel.LoadAsync();
            SnippetViewModel snippet = Assert.Single(viewModel.SnippetViewModels);

            await viewModel.AssignCategoryAsync(snippet, viewModel.UncategorizedSection);

            Assert.Null(snippet.Snippet.CategoryId);
            Assert.Contains(snippet, viewModel.UncategorizedSection.Snippets);
            Assert.Equal(1, repository.UpdateCount);
        }

        [Fact]
        public async Task AssignCategoryAsync_ToItsCurrentCategory_IsNoOp()
        {
            var (viewModel, repository, categoryRepository, _) = CreateSutWithCategories();
            categoryRepository.Categories.Add(new Category { Id = 1, Name = "Work", Order = 0 });
            repository.Snippets.Add(new Snippet { Id = 1, Content = "a", Order = 0, CategoryId = 1 });
            await viewModel.LoadAsync();
            CategoryViewModel category = Assert.Single(viewModel.Categories);
            SnippetViewModel snippet = Assert.Single(viewModel.SnippetViewModels);

            await viewModel.AssignCategoryAsync(snippet, category);

            Assert.Equal(0, repository.UpdateCount);
        }

        [Fact]
        public async Task WouldAssignCategory_ToItsCurrentCategory_IsFalse()
        {
            var (viewModel, repository, categoryRepository, _) = CreateSutWithCategories();
            categoryRepository.Categories.Add(new Category { Id = 1, Name = "Work", Order = 0 });
            repository.Snippets.Add(new Snippet { Id = 1, Content = "a", Order = 0, CategoryId = 1 });
            await viewModel.LoadAsync();
            CategoryViewModel category = Assert.Single(viewModel.Categories);
            SnippetViewModel snippet = Assert.Single(viewModel.SnippetViewModels);

            Assert.False(viewModel.WouldAssignCategory(snippet, category));
        }

        [Fact]
        public async Task WouldAssignCategory_ToUncategorizedWhenAlreadyUncategorized_IsFalse()
        {
            var (viewModel, repository, _, _) = CreateSutWithCategories();
            repository.Snippets.Add(new Snippet { Id = 1, Content = "a", Order = 0, CategoryId = null });
            await viewModel.LoadAsync();
            SnippetViewModel snippet = Assert.Single(viewModel.SnippetViewModels);

            Assert.False(viewModel.WouldAssignCategory(snippet, viewModel.UncategorizedSection));
        }

        [Fact]
        public async Task WouldAssignCategory_ToADifferentCategory_IsTrue()
        {
            var (viewModel, repository, categoryRepository, _) = CreateSutWithCategories();
            categoryRepository.Categories.Add(new Category { Id = 1, Name = "Work", Order = 0 });
            repository.Snippets.Add(new Snippet { Id = 1, Content = "a", Order = 0, CategoryId = null });
            await viewModel.LoadAsync();
            CategoryViewModel category = Assert.Single(viewModel.Categories);
            SnippetViewModel snippet = Assert.Single(viewModel.SnippetViewModels);

            Assert.True(viewModel.WouldAssignCategory(snippet, category));
        }

        [Fact]
        public async Task MoveCategoryToAsync_ReordersAndRenumbersOrderToMatchNewPositions()
        {
            var (viewModel, _, categoryRepository, _) = CreateSutWithCategories();
            await viewModel.AddCategoryAsync("a");
            await viewModel.AddCategoryAsync("b");
            await viewModel.AddCategoryAsync("c");
            CategoryViewModel dragged = viewModel.Categories[2]; // "c"
            CategoryViewModel target = viewModel.Categories[0]; // "a"

            await viewModel.MoveCategoryToAsync(dragged, target, insertBefore: true);

            Assert.Equal(new[] { "c", "a", "b" }, viewModel.Categories.Select(c => c.Category.Name));
            Assert.Equal(new[] { 0, 1, 2 }, viewModel.Categories.Select(c => c.Category.Order));
        }

        [Fact]
        public async Task WouldReorderCategory_OnItself_IsFalse()
        {
            var (viewModel, _, _, _) = CreateSutWithCategories();
            await viewModel.AddCategoryAsync("a");
            CategoryViewModel category = viewModel.Categories[0];

            Assert.False(viewModel.WouldReorderCategory(category, category, insertBefore: true));
        }
    }
}
