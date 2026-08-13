using ClipboardWizard.Model;
using ClipboardWizard.ViewModel;

namespace ClipboardWizard.Tests.ViewModel
{
    public class EditSnippetViewModelTests
    {
        private static readonly List<Category> NoCategories = new();

        [Fact]
        public void NewSnippet_IsNewAndUsesSaveLabel()
        {
            EditSnippetViewModel viewModel = new(NoCategories);

            Assert.True(viewModel.IsNew);
            Assert.Equal("Save", viewModel.ActionButtonText);
            Assert.Equal("New Snippet", viewModel.Title);
        }

        [Fact]
        public void NewSnippet_DefaultsToNoCategory()
        {
            EditSnippetViewModel viewModel = new(NoCategories);

            Assert.Same(EditSnippetViewModel.NoCategory, viewModel.SelectedCategory);
            Assert.Null(viewModel.SelectedCategoryId);
        }

        [Fact]
        public void ExistingTextSnippet_IsNotNewAndUsesUpdateLabelAndCopiesFields()
        {
            Snippet snippet = new() { Type = SnippetType.Text, Description = "d", Content = "c" };

            EditSnippetViewModel viewModel = new(snippet, NoCategories);

            Assert.False(viewModel.IsNew);
            Assert.Equal(SnippetType.Text, viewModel.Type);
            Assert.Equal("Update", viewModel.ActionButtonText);
            Assert.Equal("Edit Snippet", viewModel.Title);
            Assert.Equal("d", viewModel.Description);
            Assert.Equal("c", viewModel.Content);
        }

        [Fact]
        public void ExistingSnippet_WithNoCategoryId_SelectsNoCategory()
        {
            Category category = new() { Id = 1, Name = "Work" };
            Snippet snippet = new() { Type = SnippetType.Text, Content = "c", CategoryId = null };

            EditSnippetViewModel viewModel = new(snippet, new List<Category> { category });

            Assert.Same(EditSnippetViewModel.NoCategory, viewModel.SelectedCategory);
        }

        [Fact]
        public void ExistingSnippet_WithMatchingCategoryId_SelectsThatCategory()
        {
            Category category = new() { Id = 1, Name = "Work" };
            Snippet snippet = new() { Type = SnippetType.Text, Content = "c", CategoryId = 1 };

            EditSnippetViewModel viewModel = new(snippet, new List<Category> { category });

            Assert.Same(category, viewModel.SelectedCategory);
            Assert.Equal(1, viewModel.SelectedCategoryId);
        }

        [Fact]
        public void ExistingImageSnippet_CopiesDescriptionAndImageData_ButContentIsIrrelevant()
        {
            byte[] imageData = [1, 2, 3];
            Snippet snippet = new() { Type = SnippetType.Image, Description = "a photo", ImageData = imageData };

            EditSnippetViewModel viewModel = new(snippet, NoCategories);

            Assert.Equal(SnippetType.Image, viewModel.Type);
            Assert.Equal("a photo", viewModel.Description);
            Assert.Equal(imageData, viewModel.ImageData);
        }

        [Theory]
        [InlineData(null, false)]
        [InlineData("", false)]
        [InlineData("   ", false)]
        [InlineData("content", true)]
        public void IsValid_ForTextSnippet_ReflectsWhetherContentIsBlank(string? content, bool expected)
        {
            EditSnippetViewModel viewModel = new(NoCategories)
            {
                Content = content!
            };

            Assert.Equal(expected, viewModel.IsValid);
        }

        [Fact]
        public void IsValid_ForImageSnippet_IsAlwaysTrue()
        {
            // There's nothing to validate for an image edit - only the (optional)
            // description changes, never the content.
            Snippet snippet = new() { Type = SnippetType.Image, ImageData = [1] };
            EditSnippetViewModel viewModel = new(snippet, NoCategories);

            Assert.True(viewModel.IsValid);
        }
    }
}
