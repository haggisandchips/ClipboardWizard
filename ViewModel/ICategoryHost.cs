using ClipboardWizard.Model;
using System.Threading.Tasks;

namespace ClipboardWizard.ViewModel
{
    /// <summary>
    /// The operations a CategoryViewModel needs from its owning WizardViewModel, mirroring
    /// ISnippetHost's role for SnippetViewModel.
    /// </summary>
    public interface ICategoryHost
    {
        Task UpdateCategoryAsync(Category category);

        Task DeleteCategoryAsync(CategoryViewModel categoryViewModel);

        /// <summary>
        /// Drag-and-drop reordering: moves categoryViewModel to sit immediately before or after
        /// targetCategoryViewModel, regardless of which direction it was dragged from.
        /// </summary>
        Task MoveCategoryToAsync(CategoryViewModel categoryViewModel, CategoryViewModel targetCategoryViewModel, bool insertBefore);
    }
}
