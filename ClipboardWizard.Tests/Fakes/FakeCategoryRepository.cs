using ClipboardWizard.Model;
using ClipboardWizard.Service;

namespace ClipboardWizard.Tests.Fakes
{
    /// <summary>In-memory stand-in for ICategoryRepository, mirroring FakeSnippetRepository.</summary>
    internal class FakeCategoryRepository : ICategoryRepository
    {
        private int _nextId = 1;

        public List<Category> Categories { get; } = new();

        public int SaveCount { get; private set; }

        public int UpdateCount { get; private set; }

        public int DeleteCount { get; private set; }

        public Task<List<Category>> LoadCategoriesAsync()
        {
            return Task.FromResult(Categories.OrderBy(c => c.Order).ToList());
        }

        public Task SaveCategoryAsync(Category category)
        {
            category.Id = _nextId++;
            Categories.Add(category);
            SaveCount++;
            return Task.CompletedTask;
        }

        public Task UpdateCategoryAsync(Category category)
        {
            UpdateCount++;
            return Task.CompletedTask;
        }

        public Task DeleteCategoryAsync(Category category)
        {
            Categories.RemoveAll(c => c.Id == category.Id);
            DeleteCount++;
            return Task.CompletedTask;
        }
    }
}
