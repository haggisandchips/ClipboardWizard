using ClipboardWizard.Model;
using ClipboardWizard.Tests.Fakes;
using ClipboardWizard.ViewModel;
using ClipboardWizard.ViewModel.Command;

namespace ClipboardWizard.Tests.ViewModel.Command
{
    public class EditCommandTests
    {
        [Fact]
        public void CanExecute_TrueForTextSnippet()
        {
            SnippetViewModel viewModel = new(new Snippet { Type = SnippetType.Text }, State.Inactive, new FakeSnippetHost());
            EditCommand command = new(viewModel);

            Assert.True(command.CanExecute(null));
        }

        [Fact]
        public void CanExecute_FalseForImageSnippet()
        {
            SnippetViewModel viewModel = new(new Snippet { Type = SnippetType.Image }, State.Inactive, new FakeSnippetHost());
            EditCommand command = new(viewModel);

            Assert.False(command.CanExecute(null));
        }
    }
}
