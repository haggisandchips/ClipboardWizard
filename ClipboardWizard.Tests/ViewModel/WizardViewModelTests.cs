using ClipboardWizard.Model;
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
            clipboard.CurrentText = "match";

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
    }
}
