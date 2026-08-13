using ClipboardWizard.ViewModel;

namespace ClipboardWizard.Tests.ViewModel
{
    public class AddCategoryViewModelTests
    {
        [Theory]
        [InlineData(null, false)]
        [InlineData("", false)]
        [InlineData("   ", false)]
        [InlineData("Work", true)]
        public void IsValid_ReflectsWhetherNameIsBlank(string? name, bool expected)
        {
            AddCategoryViewModel viewModel = new()
            {
                Name = name!
            };

            Assert.Equal(expected, viewModel.IsValid);
        }
    }
}
