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
        /// <summary>Local-only persistence for fields that never sync (e.g. IsExpanded) - never pushes to Firestore. Renames/Shared changes go through ApplyCategoryEditAsync instead.</summary>
        Task UpdateCategoryAsync(Category category);

        /// <summary>
        /// All-or-nothing: also deletes the category from Firestore when
        /// categoryViewModel.Category.Shared, since Shared can only be set at creation (see
        /// AddCategoryViewModel) and never toggled off, so there's no lesser "un-share" action
        /// this could be confused with - see CategoryViewModel.DeleteCategoryAsync's prompt.
        /// </summary>
        Task DeleteCategoryAsync(CategoryViewModel categoryViewModel);

        /// <summary>
        /// Applies a rename from the Edit Category dialog. Shared can only be set when a
        /// category is created (see WizardViewModel.AddCategoryAsync) and never changes
        /// afterward, so this only ever renames - pushing the updated name too, if the category
        /// is already Shared.
        /// </summary>
        Task ApplyCategoryEditAsync(CategoryViewModel categoryViewModel, string newName);

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
