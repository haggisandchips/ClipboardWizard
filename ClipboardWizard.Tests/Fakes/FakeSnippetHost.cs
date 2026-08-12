using ClipboardWizard.Model;
using ClipboardWizard.ViewModel;

namespace ClipboardWizard.Tests.Fakes
{
    internal class FakeSnippetHost : ISnippetHost
    {
        public string ClipboardText { get; set; } = string.Empty;

        public List<Snippet> UpdatedSnippets { get; } = new();

        public List<SnippetViewModel> RemovedSnippets { get; } = new();

        public List<SnippetViewModel> MovedUp { get; } = new();

        public List<SnippetViewModel> MovedDown { get; } = new();

        public List<(SnippetViewModel Snippet, SnippetViewModel Target, bool InsertBefore)> MovedTo { get; } = new();

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

        public Task MoveSnippetUpAsync(SnippetViewModel snippetViewModel)
        {
            MovedUp.Add(snippetViewModel);
            return Task.CompletedTask;
        }

        public Task MoveSnippetDownAsync(SnippetViewModel snippetViewModel)
        {
            MovedDown.Add(snippetViewModel);
            return Task.CompletedTask;
        }

        public Task MoveSnippetToAsync(SnippetViewModel snippetViewModel, SnippetViewModel targetSnippetViewModel, bool insertBefore)
        {
            MovedTo.Add((snippetViewModel, targetSnippetViewModel, insertBefore));
            return Task.CompletedTask;
        }
    }
}
