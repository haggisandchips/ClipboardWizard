using ClipboardWizard.ViewModel;
using System.ComponentModel;

namespace ClipboardWizard.Tests.ViewModel
{
    public class UncategorizedSectionViewModelTests
    {
        [Fact]
        public void DefaultsToExpandedAndPinned()
        {
            UncategorizedSectionViewModel viewModel = new();

            Assert.True(viewModel.IsExpanded);
            Assert.True(viewModel.IsPinned);
            Assert.Null(viewModel.Delete);
            Assert.Equal("Uncategorized", viewModel.Name);
        }

        [Fact]
        public void IsExpanded_RaisesPropertyChangedOnlyWhenValueActuallyChanges()
        {
            UncategorizedSectionViewModel viewModel = new();
            List<string?> raised = new();
            viewModel.PropertyChanged += (_, e) => raised.Add(e.PropertyName);

            viewModel.IsExpanded = true; // already true - no-op
            Assert.Empty(raised);

            viewModel.IsExpanded = false;
            Assert.Equal(new[] { nameof(UncategorizedSectionViewModel.IsExpanded) }, raised);
        }
    }
}
