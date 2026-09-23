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

        /// <summary>alsoDeleteFromFirebase is only meaningful when categoryViewModel.Category.Shared - see CategoryViewModel.DeleteCategoryAsync's second confirmation prompt.</summary>
        Task DeleteCategoryAsync(CategoryViewModel categoryViewModel, bool alsoDeleteFromFirebase = false);

        /// <summary>
        /// Applies a rename and/or Shared toggle from the New/Edit Category dialog. A false-&gt;true
        /// Shared transition bulk-pushes the category and every snippet currently in it; true-&gt;false
        /// does nothing further (already-pushed Firestore data is left as-is, per SPEC).
        /// </summary>
        Task ApplyCategoryEditAsync(CategoryViewModel categoryViewModel, string newName, bool newShared);

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
