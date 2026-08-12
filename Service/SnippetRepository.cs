using ClipboardWizard.Model;
using SQLite;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace ClipboardWizard.Service
{
    public class SnippetRepository : ISnippetRepository
    {
        private readonly SQLiteAsyncConnection _connection;
        private readonly Task _initialization;

        public SnippetRepository(string databasePath)
        {
            string directory = Path.GetDirectoryName(databasePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            _connection = new SQLiteAsyncConnection(databasePath);
            _initialization = _connection.CreateTableAsync<Snippet>();
        }

        public async Task<List<Snippet>> LoadSnippetsAsync()
        {
            await _initialization;

            return await _connection.Table<Snippet>()
                .OrderBy(snippet => snippet.Order)
                .ToListAsync();
        }

        public async Task SaveSnippetAsync(Snippet snippet)
        {
            await _initialization;
            _ = await _connection.InsertAsync(snippet);
        }

        public async Task UpdateSnippetAsync(Snippet snippet)
        {
            await _initialization;
            _ = await _connection.UpdateAsync(snippet);
        }

        public async Task DeleteSnippetAsync(Snippet snippet)
        {
            await _initialization;
            _ = await _connection.DeleteAsync(snippet);
        }
    }
}
