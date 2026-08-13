using ClipboardWizard.Model;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace ClipboardWizard.ViewModel
{
    public class EditSnippetViewModel : INotifyPropertyChanged
    {
        /// <summary>
        /// Sentinel category option meaning "uncategorized" - never persisted, matched by
        /// reference rather than Id so it can't collide with a real category's Id.
        /// </summary>
        public static readonly Category NoCategory = new() { Name = "(none)" };

        public bool IsNew { get; }

        /// <summary>
        /// Content is only ever text-editable for Text snippets - an Image snippet's picture
        /// can't be retyped in a text box, so editing one only offers its Description.
        /// </summary>
        public SnippetType Type { get; }

        public string Title => IsNew ? "New Snippet" : "Edit Snippet";

        public string ActionButtonText => IsNew ? "Save" : "Update";

        private string _description;
        public string Description
        {
            get => _description;
            set
            {
                _description = value;
                OnPropertyChanged(nameof(Description));
            }
        }

        private string _content;
        public string Content
        {
            get => _content;
            set
            {
                _content = value;
                OnPropertyChanged(nameof(Content));
                OnPropertyChanged(nameof(IsValid));
            }
        }

        /// <summary>Read-only preview source for Image snippets; never edited here.</summary>
        public byte[] ImageData { get; }

        public bool IsValid => Type != SnippetType.Text || !string.IsNullOrWhiteSpace(Content);

        /// <summary>Assignable categories for the picker, with NoCategory always first.</summary>
        public IReadOnlyList<Category> Categories { get; }

        private Category _selectedCategory;
        public Category SelectedCategory
        {
            get => _selectedCategory;
            set
            {
                _selectedCategory = value;
                OnPropertyChanged(nameof(SelectedCategory));
            }
        }

        public int? SelectedCategoryId => ReferenceEquals(SelectedCategory, NoCategory) ? null : SelectedCategory?.Id;

        public event PropertyChangedEventHandler PropertyChanged;

        public EditSnippetViewModel(IReadOnlyList<Category> categories)
        {
            IsNew = true;
            Type = SnippetType.Text;
            Categories = BuildCategoryOptions(categories);
            SelectedCategory = NoCategory;
        }

        public EditSnippetViewModel(Snippet snippet, IReadOnlyList<Category> categories)
        {
            IsNew = false;
            Type = snippet.Type;
            Description = snippet.Description;
            Content = snippet.Content;
            ImageData = snippet.ImageData;
            Categories = BuildCategoryOptions(categories);
            SelectedCategory = Categories.FirstOrDefault(category => category.Id == snippet.CategoryId) ?? NoCategory;
        }

        private static IReadOnlyList<Category> BuildCategoryOptions(IReadOnlyList<Category> categories)
        {
            List<Category> options = new() { NoCategory };
            options.AddRange(categories);
            return options;
        }

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
