using ClipboardWizard.Model;
using ClipboardWizard.ViewModel;

namespace ClipboardWizard.Tests.Fakes
{
    internal class FakeCategoryHost : ICategoryHost
    {
        public List<CategoryViewModel> DeletedCategories { get; } = new();

        public List<Category> UpdatedCategories { get; } = new();

        public List<(CategoryViewModel Category, CategoryViewModel Target, bool InsertBefore)> MovedTo { get; } = new();

        public Task UpdateCategoryAsync(Category category)
        {
            UpdatedCategories.Add(category);
            return Task.CompletedTask;
        }

        public Task DeleteCategoryAsync(CategoryViewModel categoryViewModel)
        {
            DeletedCategories.Add(categoryViewModel);
            return Task.CompletedTask;
        }

        public Task MoveCategoryToAsync(CategoryViewModel categoryViewModel, CategoryViewModel targetCategoryViewModel, bool insertBefore)
        {
            MovedTo.Add((categoryViewModel, targetCategoryViewModel, insertBefore));
            return Task.CompletedTask;
        }
    }
}
