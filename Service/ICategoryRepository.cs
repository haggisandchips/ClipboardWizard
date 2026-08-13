using ClipboardWizard.Model;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ClipboardWizard.Service
{
    public interface ICategoryRepository
    {
        Task<List<Category>> LoadCategoriesAsync();

        Task SaveCategoryAsync(Category category);

        Task UpdateCategoryAsync(Category category);

        Task DeleteCategoryAsync(Category category);
    }
}
