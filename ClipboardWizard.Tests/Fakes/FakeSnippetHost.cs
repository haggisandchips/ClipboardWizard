using ClipboardWizard.Model;
using ClipboardWizard.ViewModel;

namespace ClipboardWizard.Tests.Fakes
{
    internal class FakeSnippetHost : ISnippetHost
    {
        public string ClipboardText { get; set; } = string.Empty;

        public IReadOnlyList<Category> Categories { get; set; } = new List<Category>();

        public List<Snippet> UpdatedSnippets { get; } = new();

        public List<SnippetViewModel> RemovedSnippets { get; } = new();

        public List<(SnippetViewModel Snippet, SnippetViewModel Target, bool InsertBefore)> MovedTo { get; } = new();

        public List<(SnippetViewModel Snippet, int? CategoryId)> AssignedCategories { get; } = new();

        public Task UpdateSnippetAsync(Snippet snippet)
        {
            UpdatedSnippets.Add(snippet);
            return Task.CompletedTask;
        }

        public Task RemoveSnippetAsync(SnippetViewModel snippetViewModel)
        {
            RemovedSnippets.Add(snippetViewModel);
            return Task.CompletedTask;
        }

        public Task AssignCategoryAsync(SnippetViewModel snippetViewModel, int? categoryId)
        {
            AssignedCategories.Add((snippetViewModel, categoryId));
            return Task.CompletedTask;
        }

        public Task MoveSnippetToAsync(SnippetViewModel snippetViewModel, SnippetViewModel targetSnippetViewModel, bool insertBefore)
        {
            MovedTo.Add((snippetViewModel, targetSnippetViewModel, insertBefore));
            return Task.CompletedTask;
        }
    }
}
