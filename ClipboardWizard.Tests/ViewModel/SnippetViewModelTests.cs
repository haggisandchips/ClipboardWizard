using ClipboardWizard.Model;
using ClipboardWizard.Tests.Fakes;
using ClipboardWizard.ViewModel;

namespace ClipboardWizard.Tests.ViewModel
{
    public class SnippetViewModelTests
    {
        [Fact]
        public void Constructor_MirrorsSnippetLockedIntoProtected()
        {
            Snippet locked = new() { Locked = true };
            SnippetViewModel viewModel = new(locked, State.Inactive, new FakeSnippetHost());

            Assert.True(viewModel.Protected);
        }

        [Fact]
        public async Task DeleteSnippetAsync_DelegatesToHost()
        {
            FakeSnippetHost host = new();
            SnippetViewModel viewModel = new(new Snippet(), State.Inactive, host);

            await viewModel.DeleteSnippetAsync();

            Assert.Single(host.RemovedSnippets, viewModel);
        }

        [Fact]
        public async Task OrderSnippetHigherAsync_DelegatesToHost()
        {
            FakeSnippetHost host = new();
            SnippetViewModel viewModel = new(new Snippet(), State.Inactive, host);

            await viewModel.OrderSnippetHigherAsync();

            Assert.Single(host.MovedUp, viewModel);
        }

        [Fact]
        public async Task OrderSnippetLowerAsync_DelegatesToHost()
        {
            FakeSnippetHost host = new();
            SnippetViewModel viewModel = new(new Snippet(), State.Inactive, host);

            await viewModel.OrderSnippetLowerAsync();

            Assert.Single(host.MovedDown, viewModel);
        }

        [Fact]
        public async Task HandleLockAsync_WhenUnlocked_PermanentlyLocksAndPersists()
        {
            FakeSnippetHost host = new();
            Snippet snippet = new();
            SnippetViewModel viewModel = new(snippet, State.Inactive, host);

            await viewModel.HandleLockAsync();

            Assert.True(snippet.Locked);
            Assert.True(viewModel.Protected);
            Assert.Single(host.UpdatedSnippets, snippet);
        }

        [Fact]
        public async Task HandleLockAsync_WhenAlreadyLocked_TemporarilyUnprotectsWithoutTouchingPersistedLock()
        {
            FakeSnippetHost host = new();
            Snippet snippet = new() { Locked = true };
            SnippetViewModel viewModel = new(snippet, State.Inactive, host);

            await viewModel.HandleLockAsync();

            Assert.True(snippet.Locked, "the permanent lock must never be reverted");
            Assert.False(viewModel.Protected, "the transient window should open immediately");
            Assert.Empty(host.UpdatedSnippets);
        }

        // EditSnippetAsync itself isn't unit tested: it shows a real modal dialog via
        // Application.Current.MainWindow, which needs a running WPF Application. The rules
        // around what's editable per snippet type live in EditSnippetViewModel instead
        // (EditSnippetViewModelTests), which is what EditSnippetAsync's post-dialog logic
        // reads back from.

        [Fact]
        public async Task HandleLockAsync_ReProtectsAutomaticallyAfterTheUnlockWindow()
        {
            FakeSnippetHost host = new();
            Snippet snippet = new() { Locked = true };
            SnippetViewModel viewModel = new(snippet, State.Inactive, host);

            await viewModel.HandleLockAsync();
            Assert.False(viewModel.Protected);

            await Task.Delay(TimeSpan.FromSeconds(3.5));

            Assert.True(viewModel.Protected);
        }
    }
}
