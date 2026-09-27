using ClipboardWizard.Service.Firestore;
using ClipboardWizard.ViewModel;

namespace ClipboardWizard.Tests.ViewModel
{
    public class ConfigureCategoriesViewModelTests
    {
        private static List<RemoteCategorySnapshot> MakeCategories(int count)
        {
            List<RemoteCategorySnapshot> categories = new();
            for (int i = 0; i < count; i++)
            {
                categories.Add(new RemoteCategorySnapshot { SyncId = $"cat-{i}", Name = $"Category {i}" });
            }
            return categories;
        }

        [Fact]
        public void Constructor_SeedsIsCheckedFromHiddenSet()
        {
            List<RemoteCategorySnapshot> categories = MakeCategories(3);
            ConfigureCategoriesViewModel viewModel = new(categories, new HashSet<string> { "cat-1" });

            Assert.True(viewModel.Categories.Single(c => c.SyncId == "cat-0").IsChecked);
            Assert.False(viewModel.Categories.Single(c => c.SyncId == "cat-1").IsChecked);
            Assert.True(viewModel.Categories.Single(c => c.SyncId == "cat-2").IsChecked);
        }

        [Fact]
        public void GetHiddenCategorySyncIds_ReturnsOnlyUncheckedRows()
        {
            ConfigureCategoriesViewModel viewModel = new(MakeCategories(3), new HashSet<string>());
            viewModel.Categories.Single(c => c.SyncId == "cat-1").IsChecked = false;

            HashSet<string> hidden = viewModel.GetHiddenCategorySyncIds();

            Assert.Equal(new HashSet<string> { "cat-1" }, hidden);
        }

        [Fact]
        public void CanConfirm_TrueAtExactlyTheLimit()
        {
            ConfigureCategoriesViewModel viewModel = new(MakeCategories(ConfigureCategoriesViewModel.MaxHiddenCount), new HashSet<string>());
            foreach (CategoryPickerItem item in viewModel.Categories)
            {
                item.IsChecked = false;
            }

            Assert.True(viewModel.CanConfirm);
        }

        [Fact]
        public void CanConfirm_FalseOneOverTheLimit()
        {
            ConfigureCategoriesViewModel viewModel = new(MakeCategories(ConfigureCategoriesViewModel.MaxHiddenCount + 1), new HashSet<string>());
            foreach (CategoryPickerItem item in viewModel.Categories)
            {
                item.IsChecked = false;
            }

            Assert.False(viewModel.CanConfirm);
        }
    }
}
