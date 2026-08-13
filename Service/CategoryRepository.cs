using ClipboardWizard.Model;
using SQLite;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace ClipboardWizard.Service
{
    public class CategoryRepository : ICategoryRepository
    {
        private readonly SQLiteAsyncConnection _connection;
        private readonly Task _initialization;

        public CategoryRepository(string databasePath)
        {
            string directory = Path.GetDirectoryName(databasePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            _connection = new SQLiteAsyncConnection(databasePath);
            _initialization = _connection.CreateTableAsync<Category>();
        }

        public async Task<List<Category>> LoadCategoriesAsync()
        {
            await _initialization;

            return await _connection.Table<Category>()
                .OrderBy(category => category.Order)
                .ToListAsync();
        }

        public async Task SaveCategoryAsync(Category category)
        {
            await _initialization;
            _ = await _connection.InsertAsync(category);
        }

        public async Task UpdateCategoryAsync(Category category)
        {
            await _initialization;
            _ = await _connection.UpdateAsync(category);
        }

        public async Task DeleteCategoryAsync(Category category)
        {
            await _initialization;
            _ = await _connection.DeleteAsync(category);
        }
    }
}
