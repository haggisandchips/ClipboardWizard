using ClipboardWizard.Model;
using ClipboardWizard.Service;
using ClipboardWizard.View;
using ClipboardWizard.ViewModel.Command;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace ClipboardWizard.ViewModel
{
    public class WizardViewModel : INotifyPropertyChanged, ISnippetHost, ICategoryHost
    {
        private readonly ISnippetRepository _repository;
        private readonly ICategoryRepository _categoryRepository;
        private readonly IClipboardMonitor _clipboardMonitor;

        /// <summary>Every snippet, regardless of category - used for clipboard-match scanning, which doesn't care about categories.</summary>
        public ObservableCollection<SnippetViewModel> SnippetViewModels { get; } = new();

        /// <summary>Real, persisted, reorderable/deletable categories - excludes the pinned Uncategorized section.</summary>
        public ObservableCollection<CategoryViewModel> Categories { get; } = new();

        public UncategorizedSectionViewModel UncategorizedSection { get; } = new();

        IReadOnlyList<Category> ISnippetHost.Categories => Categories.Select(c => c.Category).ToList();

        private bool _recording;
        public bool Recording
        {
            get => _recording;
            set
            {
                _recording = value;
                OnPropertyChanged(nameof(Recording));
            }
        }

        public SaveClipboardContentsCommand SaveClipboardContents { get; }

        public AddSnippetCommand AddSnippet { get; }

        public AddCategoryCommand AddCategory { get; }

        /// <summary>Text content of the clipboard, for the (text-only) edit dialog's Active check.</summary>
        public string ClipboardText => _clipboardMonitor.CurrentContent is { Type: ClipboardContentType.Text } content ? content.Text ?? string.Empty : string.Empty;

        public bool HasSaveableClipboardContent => IsSaveable(_clipboardMonitor.CurrentContent);

        public event PropertyChangedEventHandler PropertyChanged;

        public WizardViewModel(ISnippetRepository repository, ICategoryRepository categoryRepository, IClipboardMonitor clipboardMonitor)
        {
            _repository = repository;
            _categoryRepository = categoryRepository;
            _clipboardMonitor = clipboardMonitor;

            _clipboardMonitor.ContentCopied += ClipboardMonitor_ContentCopied;

            SaveClipboardContents = new(this);
            AddSnippet = new(this);
            AddCategory = new(this);
        }

        public async Task LoadAsync()
        {
            List<Category> categories = await _categoryRepository.LoadCategoriesAsync();
            foreach (Category category in categories)
            {
                Categories.Add(new CategoryViewModel(category, this));
            }

            List<Snippet> snippets = await _repository.LoadSnippetsAsync();
            ClipboardContent current = _clipboardMonitor.CurrentContent;

            foreach (Snippet snippet in snippets)
            {
                State state = Matches(snippet, current) ? State.Active : State.Inactive;
                SnippetViewModel snippetViewModel = new(snippet, state, this);
                SnippetViewModels.Add(snippetViewModel);
                GetSection(snippet.CategoryId).Snippets.Add(snippetViewModel);
            }
        }

        /// <summary>The section a snippet with this CategoryId belongs in - falls back to Uncategorized if the category no longer exists.</summary>
        private ICategorySection GetSection(int? categoryId)
        {
            CategoryViewModel category = categoryId == null
                ? null
                : Categories.FirstOrDefault(c => c.Category.Id == categoryId);

            return category ?? (ICategorySection)UncategorizedSection;
        }

        internal async Task AddNewCategoryAsync()
        {
            // The owner must be set before ShowDialog so the dialog centers over it and
            // stays modal to the correct window.
            Window owner = Application.Current.MainWindow;

            AddCategoryViewModel addCategoryViewModel = new();
            AddCategoryView addCategoryView = new()
            {
                DataContext = addCategoryViewModel,
                Owner = owner
            };

            bool? result = addCategoryView.ShowDialog();

            if (result != true)
            {
                return;
            }

            await AddCategoryAsync(addCategoryViewModel.Name);
        }

        internal async Task AddCategoryAsync(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return;
            }

            Category category = new() { Name = name.Trim(), Order = GetNextCategoryOrder() };
            await _categoryRepository.SaveCategoryAsync(category);
            Categories.Add(new CategoryViewModel(category, this));
        }

        public Task UpdateCategoryAsync(Category category)
        {
            return _categoryRepository.UpdateCategoryAsync(category);
        }

        public async Task DeleteCategoryAsync(CategoryViewModel categoryViewModel)
        {
            // The category's snippets survive as uncategorized, not deleted with it.
            List<Task> updates = new();
            foreach (SnippetViewModel snippetViewModel in categoryViewModel.Snippets.ToList())
            {
                categoryViewModel.Snippets.Remove(snippetViewModel);
                snippetViewModel.Snippet.CategoryId = null;
                UncategorizedSection.Snippets.Add(snippetViewModel);
                updates.Add(_repository.UpdateSnippetAsync(snippetViewModel.Snippet));
            }
            updates.AddRange(RenumberSection(UncategorizedSection));
            await Task.WhenAll(updates);

            await _categoryRepository.DeleteCategoryAsync(categoryViewModel.Category);
            Categories.Remove(categoryViewModel);
        }

        /// <summary>Assigns snippetViewModel to targetSection (drag onto a header, or the general body of a section).</summary>
        public Task AssignCategoryAsync(SnippetViewModel snippetViewModel, ICategorySection targetSection)
        {
            ICategorySection sourceSection = GetSection(snippetViewModel.Snippet.CategoryId);
            if (ReferenceEquals(sourceSection, targetSection))
            {
                return Task.CompletedTask;
            }

            sourceSection.Snippets.Remove(snippetViewModel);
            snippetViewModel.Snippet.CategoryId = CategoryIdOf(targetSection);
            targetSection.Snippets.Add(snippetViewModel);

            List<Task> updates = new() { _repository.UpdateSnippetAsync(snippetViewModel.Snippet) };
            updates.AddRange(RenumberSection(sourceSection));
            updates.AddRange(RenumberSection(targetSection));
            return Task.WhenAll(updates);
        }

        private static int? CategoryIdOf(ICategorySection section)
        {
            return section is CategoryViewModel category ? category.Category.Id : null;
        }

        internal async Task AddNewSnippetAsync()
        {
            // The owner must be set before ShowDialog so the dialog centers over it and
            // stays modal to the correct window.
            Window owner = Application.Current.MainWindow;

            EditSnippetViewModel editSnippetViewModel = new(Categories.Select(c => c.Category).ToList());
            EditSnippetView editSnippetView = new()
            {
                DataContext = editSnippetViewModel,
                Owner = owner,
                Width = Math.Min(owner.ActualWidth * 0.6, 1600),
                Height = Math.Min(owner.ActualHeight * 0.8, 800)
            };

            bool? result = editSnippetView.ShowDialog();

            if (result != true)
            {
                return;
            }

            ClipboardContent content = new() { Type = ClipboardContentType.Text, Text = editSnippetViewModel.Content };
            await CreateSnippetAsync(content, editSnippetViewModel.Description, editSnippetViewModel.SelectedCategoryId);
        }

        internal Task SaveClipboardSnippetAsync()
        {
            ClipboardContent content = _clipboardMonitor.CurrentContent;
            return IsSaveable(content) ? CreateSnippetAsync(content) : Task.CompletedTask;
        }

        private void ClipboardMonitor_ContentCopied(object sender, ClipboardContent content)
        {
            bool matched = false;

            foreach (SnippetViewModel snippetViewModel in SnippetViewModels)
            {
                bool equal = Matches(snippetViewModel.Snippet, content);
                snippetViewModel.State = equal ? State.Active : State.Inactive;

                matched |= equal;
            }

            if (!matched && Recording && IsSaveable(content))
            {
                // Fire-and-forget: this runs off the back of an automatic clipboard event with
                // no user-facing command to report failure through, so it logs instead of
                // throwing back into the clipboard monitor's event.
                _ = TryAutoSaveAsync(content);
            }
        }

        private async Task TryAutoSaveAsync(ClipboardContent content)
        {
            try
            {
                await CreateSnippetAsync(content);
            }
            catch (Exception ex)
            {
                Logger.LogError(nameof(TryAutoSaveAsync), ex);
            }
        }

        private async Task CreateSnippetAsync(ClipboardContent content, string description = null, int? categoryId = null)
        {
            ICategorySection section = GetSection(categoryId);

            Snippet snippet = new()
            {
                Type = content.Type == ClipboardContentType.Image ? SnippetType.Image : SnippetType.Text,
                Content = content.Text,
                ImageData = content.ImageData,
                Description = description,
                CategoryId = CategoryIdOf(section),
                Order = section.Snippets.Count == 0 ? 0 : section.Snippets.Max(s => s.Snippet.Order) + 1
            };

            await _repository.SaveSnippetAsync(snippet);

            State state = Matches(snippet, _clipboardMonitor.CurrentContent) ? State.Active : State.Inactive;
            SnippetViewModel snippetViewModel = new(snippet, state, this);
            SnippetViewModels.Add(snippetViewModel);
            section.Snippets.Add(snippetViewModel);
        }

        private static bool IsSaveable(ClipboardContent content)
        {
            return content switch
            {
                { Type: ClipboardContentType.Text, Text: var text } => !string.IsNullOrWhiteSpace(text),
                { Type: ClipboardContentType.Image, ImageData: var data } => data is { Length: > 0 },
                _ => false
            };
        }

        private static bool Matches(Snippet snippet, ClipboardContent content)
        {
            if (!IsSaveable(content))
            {
                return false;
            }

            return snippet.Type switch
            {
                SnippetType.Text => content.Type == ClipboardContentType.Text
                    && string.Equals(snippet.Content, content.Text, StringComparison.Ordinal),
                SnippetType.Image => content.Type == ClipboardContentType.Image
                    && snippet.ImageData != null
                    && content.ImageData.AsSpan().SequenceEqual(snippet.ImageData),
                _ => false
            };
        }

        public Task UpdateSnippetAsync(Snippet snippet)
        {
            return _repository.UpdateSnippetAsync(snippet);
        }

        public async Task RemoveSnippetAsync(SnippetViewModel snippetViewModel)
        {
            await _repository.DeleteSnippetAsync(snippetViewModel.Snippet);
            SnippetViewModels.Remove(snippetViewModel);
            GetSection(snippetViewModel.Snippet.CategoryId).Snippets.Remove(snippetViewModel);
        }

        /// <summary>
        /// Whether dropping snippetViewModel on targetSnippetViewModel would actually change
        /// anything: always true when they're in different sections (that's a recategorize),
        /// otherwise false either side of the dragged tile itself, since inserting immediately
        /// before/after where it already sits is a no-op. Used to suppress the drop indicator,
        /// as well as by MoveSnippetToAsync itself to skip the true no-op case.
        /// </summary>
        public bool WouldMoveSnippet(SnippetViewModel snippetViewModel, SnippetViewModel targetSnippetViewModel, bool insertBefore)
        {
            ICategorySection sourceSection = GetSection(snippetViewModel.Snippet.CategoryId);
            ICategorySection targetSection = GetSection(targetSnippetViewModel.Snippet.CategoryId);

            if (!ReferenceEquals(sourceSection, targetSection))
            {
                return true;
            }

            return WouldReorder(targetSection.Snippets, snippetViewModel, targetSnippetViewModel, insertBefore);
        }

        /// <summary>
        /// Whether dropping categoryViewModel on targetCategoryViewModel (per MoveCategoryToAsync's
        /// insertBefore semantics) would actually change its position - false either side of the
        /// dragged row itself. Both parameters are always real categories: the pinned
        /// Uncategorized section is never a member of the reorderable Categories list, so it can
        /// never be targeted or dragged here.
        /// </summary>
        public bool WouldReorderCategory(CategoryViewModel categoryViewModel, CategoryViewModel targetCategoryViewModel, bool insertBefore)
        {
            return WouldReorder(Categories, categoryViewModel, targetCategoryViewModel, insertBefore);
        }

        private static bool WouldReorder<T>(IList<T> items, T item, T target, bool insertBefore)
        {
            int currentIndex = items.IndexOf(item);
            int targetIndex = items.IndexOf(target);

            if (currentIndex < 0 || targetIndex < 0 || currentIndex == targetIndex)
            {
                return false;
            }

            return GetDesiredIndex(currentIndex, targetIndex, insertBefore) != currentIndex;
        }

        // ObservableCollection.Move(old, new) removes at `old` first, which shifts everything
        // after it left by one - so the same `new` index lands *before* the target when moving
        // backward but *after* it when moving forward, unless corrected for here. insertBefore
        // must mean the same thing regardless of which direction the item is dragged from.
        private static int GetDesiredIndex(int currentIndex, int targetIndex, bool insertBefore)
        {
            int desiredIndex = insertBefore ? targetIndex : targetIndex + 1;
            return currentIndex < desiredIndex ? desiredIndex - 1 : desiredIndex;
        }

        public async Task MoveSnippetToAsync(SnippetViewModel snippetViewModel, SnippetViewModel targetSnippetViewModel, bool insertBefore)
        {
            ICategorySection sourceSection = GetSection(snippetViewModel.Snippet.CategoryId);
            ICategorySection targetSection = GetSection(targetSnippetViewModel.Snippet.CategoryId);
            bool recategorize = !ReferenceEquals(sourceSection, targetSection);

            if (!recategorize && !WouldReorder(targetSection.Snippets, snippetViewModel, targetSnippetViewModel, insertBefore))
            {
                return;
            }

            List<Task> updates = new();

            if (recategorize)
            {
                int targetIndex = targetSection.Snippets.IndexOf(targetSnippetViewModel);
                int insertIndex = insertBefore ? targetIndex : targetIndex + 1;

                sourceSection.Snippets.Remove(snippetViewModel);
                snippetViewModel.Snippet.CategoryId = CategoryIdOf(targetSection);
                targetSection.Snippets.Insert(insertIndex, snippetViewModel);

                updates.Add(_repository.UpdateSnippetAsync(snippetViewModel.Snippet));
                updates.AddRange(RenumberSection(sourceSection));
            }
            else
            {
                int currentIndex = targetSection.Snippets.IndexOf(snippetViewModel);
                int targetIndex = targetSection.Snippets.IndexOf(targetSnippetViewModel);
                targetSection.Snippets.Move(currentIndex, GetDesiredIndex(currentIndex, targetIndex, insertBefore));
            }

            updates.AddRange(RenumberSection(targetSection));
            await Task.WhenAll(updates);
        }

        public async Task MoveCategoryToAsync(CategoryViewModel categoryViewModel, CategoryViewModel targetCategoryViewModel, bool insertBefore)
        {
            if (!WouldReorderCategory(categoryViewModel, targetCategoryViewModel, insertBefore))
            {
                return;
            }

            int currentIndex = Categories.IndexOf(categoryViewModel);
            int desiredIndex = GetDesiredIndex(currentIndex, Categories.IndexOf(targetCategoryViewModel), insertBefore);

            Categories.Move(currentIndex, desiredIndex);

            List<Task> updates = new();
            for (int i = 0; i < Categories.Count; i++)
            {
                Category category = Categories[i].Category;
                if (category.Order != i)
                {
                    category.Order = i;
                    updates.Add(_categoryRepository.UpdateCategoryAsync(category));
                }
            }
            await Task.WhenAll(updates);
        }

        /// <summary>Renumbers a section's snippets' Order to match their on-screen positions, persisting only the ones that actually changed.</summary>
        private List<Task> RenumberSection(ICategorySection section)
        {
            List<Task> updates = new();
            for (int i = 0; i < section.Snippets.Count; i++)
            {
                Snippet snippet = section.Snippets[i].Snippet;
                if (snippet.Order != i)
                {
                    snippet.Order = i;
                    updates.Add(_repository.UpdateSnippetAsync(snippet));
                }
            }
            return updates;
        }

        private int GetNextCategoryOrder()
        {
            return Categories.Count == 0 ? 0 : Categories.Max(c => c.Category.Order) + 1;
        }

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
