using ClipboardWizard.Model;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ClipboardWizard.ViewModel
{
    /// <summary>
    /// The operations a SnippetViewModel needs from its owning WizardViewModel: persisting
    /// its own changes and keeping the on-screen collection (order, membership) in sync.
    /// Replaces a previous design based on static, app-wide events, which had no clear
    /// ownership and could be subscribed to from anywhere.
    /// </summary>
    public interface ISnippetHost
    {
        /// <summary>Available categories, for the edit dialog's assignment dropdown.</summary>
        IReadOnlyList<Category> Categories { get; }

        /// <summary>
        /// Re-checks every snippet's Active/Inactive state against the current clipboard
        /// contents. Editing a snippet's content can change which snippet (if any) is the one
        /// matching the clipboard, so this sweeps the whole collection through the same
        /// matching logic every other clipboard-driven state change uses, rather than just
        /// setting the edited snippet's own State in isolation.
        /// </summary>
        void RefreshSnippetStates();

        Task UpdateSnippetAsync(Snippet snippet);

        Task RemoveSnippetAsync(SnippetViewModel snippetViewModel);

        /// <summary>
        /// Assigns snippetViewModel to the category with this id (or Uncategorized if null),
        /// moving it into that section's on-screen collection as well as persisting the change -
        /// the edit dialog's category picker works in ids, unlike the ICategorySection instances
        /// drag-and-drop already has in hand.
        /// </summary>
        Task AssignCategoryAsync(SnippetViewModel snippetViewModel, int? categoryId);

        /// <summary>
        /// Drag-and-drop reordering: moves snippetViewModel to sit immediately before or after
        /// targetSnippetViewModel, regardless of which direction it was dragged from.
        /// </summary>
        Task MoveSnippetToAsync(SnippetViewModel snippetViewModel, SnippetViewModel targetSnippetViewModel, bool insertBefore);
    }
}
