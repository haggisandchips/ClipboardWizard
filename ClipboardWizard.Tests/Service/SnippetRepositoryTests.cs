using ClipboardWizard.Model;
using ClipboardWizard.Service;

namespace ClipboardWizard.Tests.Service
{
    public class SnippetRepositoryTests : IDisposable
    {
        private readonly string _databasePath;
        private readonly SnippetRepository _repository;

        public SnippetRepositoryTests()
        {
            _databasePath = Path.Combine(Path.GetTempPath(), $"ClipboardWizardTests_{Guid.NewGuid():N}.db");
            _repository = new SnippetRepository(_databasePath);
        }

        [Fact]
        public async Task SaveAndLoad_RoundTripsAllFields()
        {
            Snippet snippet = new()
            {
                Description = "desc",
                Content = "content",
                Order = 1
            };

            await _repository.SaveSnippetAsync(snippet);
            List<Snippet> loaded = await _repository.LoadSnippetsAsync();

            Snippet reloaded = Assert.Single(loaded);
            Assert.Equal("desc", reloaded.Description);
            Assert.Equal("content", reloaded.Content);
            Assert.Equal(1, reloaded.Order);
            Assert.True(reloaded.Id > 0);
        }

        [Fact]
        public async Task LoadSnippets_ReturnsInOrder()
        {
            await _repository.SaveSnippetAsync(new Snippet { Content = "third", Order = 2 });
            await _repository.SaveSnippetAsync(new Snippet { Content = "first", Order = 0 });
            await _repository.SaveSnippetAsync(new Snippet { Content = "second", Order = 1 });

            List<Snippet> loaded = await _repository.LoadSnippetsAsync();

            Assert.Equal(new[] { "first", "second", "third" }, loaded.Select(s => s.Content));
        }

        [Fact]
        public async Task UpdateSnippet_PersistsChanges()
        {
            Snippet snippet = new() { Content = "original", Order = 0 };
            await _repository.SaveSnippetAsync(snippet);

            snippet.Content = "changed";
            await _repository.UpdateSnippetAsync(snippet);

            List<Snippet> loaded = await _repository.LoadSnippetsAsync();
            Assert.Equal("changed", Assert.Single(loaded).Content);
        }

        [Fact]
        public async Task DeleteSnippet_RemovesRow()
        {
            Snippet snippet = new() { Content = "to delete", Order = 0 };
            await _repository.SaveSnippetAsync(snippet);

            await _repository.DeleteSnippetAsync(snippet);

            Assert.Empty(await _repository.LoadSnippetsAsync());
        }

        [Fact]
        public async Task ImageSnippet_RoundTripsTypeAndImageData()
        {
            byte[] imageData = [1, 2, 3, 4, 5];
            Snippet snippet = new() { Type = SnippetType.Image, ImageData = imageData, Order = 0 };

            await _repository.SaveSnippetAsync(snippet);
            Snippet loaded = Assert.Single(await _repository.LoadSnippetsAsync());

            Assert.Equal(SnippetType.Image, loaded.Type);
            Assert.Equal(imageData, loaded.ImageData);
        }

        [Fact]
        public async Task TextSnippet_DefaultsToTextType()
        {
            Snippet snippet = new() { Content = "plain text", Order = 0 };

            await _repository.SaveSnippetAsync(snippet);
            Snippet loaded = Assert.Single(await _repository.LoadSnippetsAsync());

            Assert.Equal(SnippetType.Text, loaded.Type);
        }

        [Fact]
        public async Task Locked_PersistsAsTrueAndCannotBeReloadedAsFalse()
        {
            Snippet snippet = new() { Content = "protected", Order = 0, Locked = true };
            await _repository.SaveSnippetAsync(snippet);

            List<Snippet> loaded = await _repository.LoadSnippetsAsync();

            Assert.True(Assert.Single(loaded).Locked);
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
