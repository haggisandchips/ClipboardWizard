using ClipboardWizard.Model;
using ClipboardWizard.ViewModel;

namespace ClipboardWizard.Tests.Fakes
{
    internal class FakeCategoryHost : ICategoryHost
    {
        public List<(CategoryViewModel Category, bool AlsoDeleteFromFirebase)> DeletedCategories { get; } = new();

        public List<Category> UpdatedCategories { get; } = new();

        public List<(CategoryViewModel Category, CategoryViewModel Target, bool InsertBefore)> MovedTo { get; } = new();

        public List<CategoryViewModel> AddSnippetCalls { get; } = new();

        public List<CategoryViewModel> SaveClipboardSnippetCalls { get; } = new();

        public bool HasSaveableClipboardContent { get; set; }

        public Task AddSnippetAsync(CategoryViewModel categoryViewModel)
        {
            AddSnippetCalls.Add(categoryViewModel);
            return Task.CompletedTask;
        }

        public Task SaveClipboardSnippetAsync(CategoryViewModel categoryViewModel)
        {
            SaveClipboardSnippetCalls.Add(categoryViewModel);
            return Task.CompletedTask;
        }

        public List<(CategoryViewModel Category, string NewName, bool NewShared)> Edits { get; } = new();

        public Task UpdateCategoryAsync(Category category)
        {
            UpdatedCategories.Add(category);
            return Task.CompletedTask;
        }

        public Task DeleteCategoryAsync(CategoryViewModel categoryViewModel, bool alsoDeleteFromFirebase)
        {
            DeletedCategories.Add((categoryViewModel, alsoDeleteFromFirebase));
            return Task.CompletedTask;
        }

        public Task ApplyCategoryEditAsync(CategoryViewModel categoryViewModel, string newName, bool newShared)
        {
            Edits.Add((categoryViewModel, newName, newShared));
            return Task.CompletedTask;
        }

        public Task MoveCategoryToAsync(CategoryViewModel categoryViewModel, CategoryViewModel targetCategoryViewModel, bool insertBefore)
        {
            MovedTo.Add((categoryViewModel, targetCategoryViewModel, insertBefore));
            return Task.CompletedTask;
        }
    }
}
