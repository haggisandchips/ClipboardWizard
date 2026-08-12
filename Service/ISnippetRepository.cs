using ClipboardWizard.Model;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ClipboardWizard.Service
{
    public interface ISnippetRepository
    {
        Task<List<Snippet>> LoadSnippetsAsync();

        Task SaveSnippetAsync(Snippet snippet);

        Task UpdateSnippetAsync(Snippet snippet);

        Task DeleteSnippetAsync(Snippet snippet);
    }
}
