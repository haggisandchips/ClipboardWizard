using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;

namespace ClipboardWizard.ViewModel
{
    /// <summary>
    /// A section of the accordion: either a real, persisted CategoryViewModel, or the single
    /// pinned "Uncategorized" bucket (UncategorizedSectionViewModel). Lets CategorySectionControl
    /// render and interact with both uniformly.
    /// </summary>
    public interface ICategorySection : INotifyPropertyChanged
    {
        string Name { get; }

        bool IsExpanded { get; set; }

        /// <summary>True for the Uncategorized section: can't be deleted or dragged to reorder.</summary>
        bool IsPinned { get; }

        ObservableCollection<SnippetViewModel> Snippets { get; }

        /// <summary>Null when IsPinned, since Uncategorized can't be deleted.</summary>
        ICommand Delete { get; }
    }
}
