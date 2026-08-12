using ClipboardWizard.Model;
using ClipboardWizard.ViewModel;

namespace ClipboardWizard.Tests.ViewModel
{
    public class EditSnippetViewModelTests
    {
        [Fact]
        public void NewSnippet_IsNewAndUsesSaveLabel()
        {
            EditSnippetViewModel viewModel = new();

            Assert.True(viewModel.IsNew);
            Assert.Equal("Save", viewModel.ActionButtonText);
            Assert.Equal("New Snippet", viewModel.Title);
        }

        [Fact]
        public void ExistingSnippet_IsNotNewAndUsesUpdateLabelAndCopiesFields()
        {
            Snippet snippet = new() { Description = "d", Content = "c" };

            EditSnippetViewModel viewModel = new(snippet);

            Assert.False(viewModel.IsNew);
            Assert.Equal("Update", viewModel.ActionButtonText);
            Assert.Equal("Edit Snippet", viewModel.Title);
            Assert.Equal("d", viewModel.Description);
            Assert.Equal("c", viewModel.Content);
        }

        [Theory]
        [InlineData(null, false)]
        [InlineData("", false)]
        [InlineData("   ", false)]
        [InlineData("content", true)]
        public void IsValid_ReflectsWhetherContentIsBlank(string? content, bool expected)
        {
            EditSnippetViewModel viewModel = new()
            {
                Content = content!
            };

            Assert.Equal(expected, viewModel.IsValid);
        }
    }
}
