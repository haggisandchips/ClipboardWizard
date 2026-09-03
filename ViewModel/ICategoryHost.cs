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

        /// <summary>Whether the current clipboard contents could be saved as a snippet - backs the per-category quick-add button's enabled state.</summary>
        bool HasSaveableClipboardContent { get; }

        /// <summary>Opens the new-snippet dialog with categoryViewModel pre-selected - the per-category "New Snippet" button.</summary>
        Task AddSnippetAsync(CategoryViewModel categoryViewModel);

        /// <summary>Saves the current clipboard contents straight into categoryViewModel - the per-category "Add" button.</summary>
        Task SaveClipboardSnippetAsync(CategoryViewModel categoryViewModel);
    }
}
