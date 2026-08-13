using ClipboardWizard.Model;
using ClipboardWizard.Service;

namespace ClipboardWizard.Tests.Service
{
    public class CategoryRepositoryTests : IDisposable
    {
        private readonly string _databasePath;
        private readonly CategoryRepository _repository;

        public CategoryRepositoryTests()
        {
            _databasePath = Path.Combine(Path.GetTempPath(), $"ClipboardWizardCategoryTests_{Guid.NewGuid():N}.db");
            _repository = new CategoryRepository(_databasePath);
        }

        [Fact]
        public async Task SaveAndLoad_RoundTripsAllFields()
        {
            Category category = new() { Name = "Work", Order = 1 };

            await _repository.SaveCategoryAsync(category);
            List<Category> loaded = await _repository.LoadCategoriesAsync();

            Category reloaded = Assert.Single(loaded);
            Assert.Equal("Work", reloaded.Name);
            Assert.Equal(1, reloaded.Order);
            Assert.True(reloaded.Id > 0);
        }

        [Fact]
        public async Task LoadCategories_ReturnsInOrder()
        {
            await _repository.SaveCategoryAsync(new Category { Name = "third", Order = 2 });
            await _repository.SaveCategoryAsync(new Category { Name = "first", Order = 0 });
            await _repository.SaveCategoryAsync(new Category { Name = "second", Order = 1 });

            List<Category> loaded = await _repository.LoadCategoriesAsync();

            Assert.Equal(new[] { "first", "second", "third" }, loaded.Select(c => c.Name));
        }

        [Fact]
        public async Task UpdateCategory_PersistsChanges()
        {
            Category category = new() { Name = "original", Order = 0 };
            await _repository.SaveCategoryAsync(category);

            category.Name = "changed";
            await _repository.UpdateCategoryAsync(category);

            List<Category> loaded = await _repository.LoadCategoriesAsync();
            Assert.Equal("changed", Assert.Single(loaded).Name);
        }

        [Fact]
        public async Task DeleteCategory_RemovesRow()
        {
            Category category = new() { Name = "to delete", Order = 0 };
            await _repository.SaveCategoryAsync(category);

            await _repository.DeleteCategoryAsync(category);

            Assert.Empty(await _repository.LoadCategoriesAsync());
        }

        public void Dispose()
        {
            try
            {
                File.Delete(_databasePath);
            }
            catch (IOException)
            {
            }
        }
    }
}
