using ClipboardWizard.Model;
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
        string ClipboardText { get; }

        Task UpdateSnippetAsync(Snippet snippet);

        Task RemoveSnippetAsync(SnippetViewModel snippetViewModel);

        Task MoveSnippetUpAsync(SnippetViewModel snippetViewModel);

        Task MoveSnippetDownAsync(SnippetViewModel snippetViewModel);
    }
}
