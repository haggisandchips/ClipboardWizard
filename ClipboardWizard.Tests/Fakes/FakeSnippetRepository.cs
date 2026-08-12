using ClipboardWizard.Model;
using ClipboardWizard.Service;

namespace ClipboardWizard.Tests.Fakes
{
    /// <summary>
    /// In-memory stand-in for ISnippetRepository so ViewModel tests don't touch disk and can
    /// assert on exactly what would have been persisted.
    /// </summary>
    internal class FakeSnippetRepository : ISnippetRepository
    {
        private int _nextId = 1;

        public List<Snippet> Snippets { get; } = new();

        public int SaveCount { get; private set; }

        public int UpdateCount { get; private set; }

        public int DeleteCount { get; private set; }

        public Task<List<Snippet>> LoadSnippetsAsync()
        {
            return Task.FromResult(Snippets.OrderBy(s => s.Order).ToList());
        }

        public Task SaveSnippetAsync(Snippet snippet)
        {
            snippet.Id = _nextId++;
            Snippets.Add(snippet);
            SaveCount++;
            return Task.CompletedTask;
        }

        public Task UpdateSnippetAsync(Snippet snippet)
        {
            UpdateCount++;
            return Task.CompletedTask;
        }

        public Task DeleteSnippetAsync(Snippet snippet)
        {
            Snippets.RemoveAll(s => s.Id == snippet.Id);
            DeleteCount++;
            return Task.CompletedTask;
        }
    }
}
