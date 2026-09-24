using ClipboardWizard.Model;
using ClipboardWizard.Service;
using ClipboardWizard.Service.Firestore;
using ClipboardWizard.View;
using ClipboardWizard.ViewModel.Command;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace ClipboardWizard.ViewModel
{
    public class WizardViewModel : INotifyPropertyChanged, ISnippetHost, ICategoryHost, IFirestoreSyncEventSink
    {
        private readonly ISnippetRepository _repository;
        private readonly ICategoryRepository _categoryRepository;
        private readonly IClipboardMonitor _clipboardMonitor;
        private readonly ISettingsService _settingsService;
        private readonly IFirestoreSyncService _firestoreSyncService;

        /// <summary>
        /// Remote snippet puts whose owning category hasn't arrived yet, keyed by categorySyncId.
        /// The category and snippet listeners are two independent Firestore listeners, so on a
        /// fresh connect there's no guarantee the category's own put is applied before its
        /// snippets' puts are - replayed once that category is actually added (see
        /// ApplyRemoteCategoryPutAsync), instead of being dropped until the next full reconnect.
        /// </summary>
        private readonly Dictionary<string, List<RemoteSnippetSnapshot>> _pendingSnippetsByCategorySyncId = new();

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

        public OpenSettingsCommand OpenSettings { get; }

        public bool HasSaveableClipboardContent => IsSaveable(_clipboardMonitor.CurrentContent);

        public event PropertyChangedEventHandler PropertyChanged;

        public WizardViewModel(
            ISnippetRepository repository,
            ICategoryRepository categoryRepository,
            IClipboardMonitor clipboardMonitor,
            ISettingsService settingsService,
            IFirestoreSyncService firestoreSyncService)
        {
            _repository = repository;
            _categoryRepository = categoryRepository;
            _clipboardMonitor = clipboardMonitor;
            _settingsService = settingsService;
            _firestoreSyncService = firestoreSyncService;

            _clipboardMonitor.ContentCopied += ClipboardMonitor_ContentCopied;

            SaveClipboardContents = new(this);
            AddSnippet = new(this);
            AddCategory = new(this);
            OpenSettings = new(this);
        }

        public async Task LoadAsync()
        {
            List<Category> categories = await _categoryRepository.LoadCategoriesAsync();
            foreach (Category category in categories)
            {
                Categories.Add(new CategoryViewModel(category, this, _firestoreSyncService));
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

        /// <summary>The Category a snippet with this CategoryId belongs to, or null for Uncategorized/unknown - used to decide whether a snippet mutation should push to Firestore.</summary>
        private Category GetOwningCategoryOrNull(int? categoryId)
        {
            return categoryId == null ? null : Categories.FirstOrDefault(c => c.Category.Id == categoryId)?.Category;
        }

        internal async Task AddNewCategoryAsync()
        {
            // The owner must be set before ShowDialog so the dialog centers over it and
            // stays modal to the correct window.
            Window owner = Application.Current.MainWindow;

            AddCategoryViewModel addCategoryViewModel = new(_firestoreSyncService);
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

            await AddCategoryAsync(addCategoryViewModel.Name, addCategoryViewModel.Shared);
        }

        internal async Task AddCategoryAsync(string name, bool shared = false)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return;
            }

            Category category = new() { Name = name.Trim(), Order = GetNextCategoryOrder(), Shared = shared };
            if (shared)
            {
                category.SyncId = Guid.NewGuid().ToString("N");
                category.ModifiedAtUtc = DateTime.UtcNow;
            }

            await _categoryRepository.SaveCategoryAsync(category);
            Categories.Add(new CategoryViewModel(category, this, _firestoreSyncService));

            if (shared)
            {
                await TryPushCategoryBulkAsync(category, Array.Empty<Snippet>());
            }
        }

        /// <summary>Local-only persistence (e.g. IsExpanded) - never pushes to Firestore. See ApplyCategoryEditAsync for renames/Shared changes, which do.</summary>
        public Task UpdateCategoryAsync(Category category)
        {
            return _categoryRepository.UpdateCategoryAsync(category);
        }

        public async Task ApplyCategoryEditAsync(CategoryViewModel categoryViewModel, string newName, bool newShared)
        {
            Category category = categoryViewModel.Category;
            bool justEnabledSharing = newShared && !category.Shared;

            category.Name = newName?.Trim();
            category.Shared = newShared;

            if (justEnabledSharing)
            {
                category.SyncId ??= Guid.NewGuid().ToString("N");
            }
            if (newShared)
            {
                category.ModifiedAtUtc = DateTime.UtcNow;
            }

            await _categoryRepository.UpdateCategoryAsync(category);

            if (justEnabledSharing)
            {
                // Initial bulk push: the category document, then every snippet currently in it.
                List<Snippet> snippets = categoryViewModel.Snippets.Select(s => s.Snippet).ToList();
                foreach (Snippet snippet in snippets)
                {
                    snippet.SyncId ??= Guid.NewGuid().ToString("N");
                    snippet.ModifiedAtUtc = DateTime.UtcNow;
                    await _repository.UpdateSnippetAsync(snippet);
                }

                await TryPushCategoryBulkAsync(category, snippets);
            }
            else if (newShared)
            {
                // Still shared (e.g. just renamed) - push the updated category doc so the change propagates.
                await TryPushCategoryAsync(category);
            }
            // Shared true -> false: nothing further. The push wrappers already gate on
            // category.Shared, so already-pushed Firestore data is simply left as-is (SPEC).
        }

        public async Task DeleteCategoryAsync(CategoryViewModel categoryViewModel, bool alsoDeleteFromFirebase = false)
        {
            // The category's snippets survive as uncategorized, not deleted with it.
            List<Task> updates = new();
            foreach (SnippetViewModel snippetViewModel in categoryViewModel.Snippets.ToList())
            {
                categoryViewModel.Snippets.Remove(snippetViewModel);
                snippetViewModel.Snippet.CategoryId = null;
                UncategorizedSection.Snippets.Add(snippetViewModel);
                updates.Add(UpdateSnippetAsync(snippetViewModel.Snippet));
            }
            updates.AddRange(RenumberSection(UncategorizedSection));
            await Task.WhenAll(updates);

            await _categoryRepository.DeleteCategoryAsync(categoryViewModel.Category);
            Categories.Remove(categoryViewModel);

            if (alsoDeleteFromFirebase && categoryViewModel.Category.SyncId != null)
            {
                await TryDeleteRemoteCategoryAsync(categoryViewModel.Category.SyncId);
            }
        }

        /// <summary>Assigns snippetViewModel to the category with this id (or Uncategorized if null) - see ISnippetHost.</summary>
        public Task AssignCategoryAsync(SnippetViewModel snippetViewModel, int? categoryId)
        {
            return AssignCategoryAsync(snippetViewModel, GetSection(categoryId));
        }

        /// <summary>Whether assigning snippetViewModel to targetSection would actually change its category - false if it's already there. Used to suppress the drop highlight, as well as by AssignCategoryAsync itself to skip the no-op case.</summary>
        public bool WouldAssignCategory(SnippetViewModel snippetViewModel, ICategorySection targetSection)
        {
            return !ReferenceEquals(GetSection(snippetViewModel.Snippet.CategoryId), targetSection);
        }

        /// <summary>Assigns snippetViewModel to targetSection (drag onto a header, or the general body of a section).</summary>
        public Task AssignCategoryAsync(SnippetViewModel snippetViewModel, ICategorySection targetSection)
        {
            if (!WouldAssignCategory(snippetViewModel, targetSection))
            {
                return Task.CompletedTask;
            }

            ICategorySection sourceSection = GetSection(snippetViewModel.Snippet.CategoryId);
            sourceSection.Snippets.Remove(snippetViewModel);
            snippetViewModel.Snippet.CategoryId = CategoryIdOf(targetSection);
            targetSection.Snippets.Add(snippetViewModel);

            List<Task> updates = new() { UpdateSnippetAsync(snippetViewModel.Snippet) };
            updates.AddRange(RenumberSection(sourceSection));
            updates.AddRange(RenumberSection(targetSection));
            return Task.WhenAll(updates);
        }

        private static int? CategoryIdOf(ICategorySection section)
        {
            return section is CategoryViewModel category ? category.Category.Id : null;
        }

        internal async Task AddNewSnippetAsync(int? categoryId = null)
        {
            // The owner must be set before ShowDialog so the dialog centers over it and
            // stays modal to the correct window.
            Window owner = Application.Current.MainWindow;

            EditSnippetViewModel editSnippetViewModel = new(Categories.Select(c => c.Category).ToList());
            if (categoryId.HasValue)
            {
                editSnippetViewModel.SelectedCategory = editSnippetViewModel.Categories.FirstOrDefault(c => c.Id == categoryId)
                    ?? EditSnippetViewModel.NoCategory;
            }

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

        internal Task SaveClipboardSnippetAsync(int? categoryId = null)
        {
            ClipboardContent content = _clipboardMonitor.CurrentContent;
            return IsSaveable(content) ? CreateSnippetAsync(content, categoryId: categoryId) : Task.CompletedTask;
        }

        /// <summary>Opens the new-snippet dialog with categoryViewModel pre-selected - see ICategoryHost.</summary>
        public Task AddSnippetAsync(CategoryViewModel categoryViewModel)
        {
            return AddNewSnippetAsync(categoryViewModel.Category.Id);
        }

        /// <summary>Saves the current clipboard contents straight into categoryViewModel - see ICategoryHost.</summary>
        public Task SaveClipboardSnippetAsync(CategoryViewModel categoryViewModel)
        {
            return SaveClipboardSnippetAsync(categoryViewModel.Category.Id);
        }

        internal async Task OpenSettingsAsync()
        {
            Window owner = Application.Current.MainWindow;

            FirestoreCredentials existing = _settingsService.Load();
            SettingsViewModel settingsViewModel = new(existing, _firestoreSyncService);
            SettingsView settingsView = new()
            {
                DataContext = settingsViewModel,
                Owner = owner
            };

            bool? result = settingsView.ShowDialog();
            if (result != true)
            {
                return;
            }

            FirestoreCredentials credentials = settingsViewModel.ToCredentials();
            _settingsService.Save(credentials);
            _firestoreSyncService.Configure(credentials);
        }

        private void ClipboardMonitor_ContentCopied(object sender, ClipboardContent content)
        {
            bool matched = RefreshSnippetStates(content);

            if (!matched && Recording && IsSaveable(content))
            {
                // Fire-and-forget: this runs off the back of an automatic clipboard event with
                // no user-facing command to report failure through, so it logs instead of
                // throwing back into the clipboard monitor's event.
                _ = TryAutoSaveAsync(content);
            }
        }

        /// <summary>
        /// Re-checks every snippet against the live clipboard. An edit can change whether the
        /// edited snippet - or another snippet that used to be the sole match - is the current
        /// Active one, so this always sweeps the whole collection rather than just the snippet
        /// that changed (see ISnippetHost.RefreshSnippetStates).
        /// </summary>
        public void RefreshSnippetStates()
        {
            RefreshSnippetStates(_clipboardMonitor.CurrentContent);
        }

        private bool RefreshSnippetStates(ClipboardContent content)
        {
            bool matched = false;

            foreach (SnippetViewModel snippetViewModel in SnippetViewModels)
            {
                bool equal = Matches(snippetViewModel.Snippet, content);
                snippetViewModel.State = equal ? State.Active : State.Inactive;

                matched |= equal;
            }

            return matched;
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

        internal async Task CreateSnippetAsync(ClipboardContent content, string description = null, int? categoryId = null)
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

            List<Task> updates = new() { SaveSnippetAsync(snippet) };

            // A snippet landing in a collapsed section would be invisible until the user
            // happens to expand it - so whichever section receives a new snippet expands too.
            if (!section.IsExpanded)
            {
                section.IsExpanded = true;
                if (section is CategoryViewModel categoryViewModel)
                {
                    updates.Add(UpdateCategoryAsync(categoryViewModel.Category));
                }
            }

            await Task.WhenAll(updates);

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

        #region Snippet persistence wrappers - the only place a snippet push is ever triggered

        /// <summary>Persists a new snippet locally, then pushes it if its category is Shared.</summary>
        private async Task SaveSnippetAsync(Snippet snippet)
        {
            StampForSyncIfOwningCategoryShared(snippet);
            await _repository.SaveSnippetAsync(snippet);
            await TryPushSnippetAsync(snippet);
        }

        public async Task UpdateSnippetAsync(Snippet snippet)
        {
            StampForSyncIfOwningCategoryShared(snippet);
            await _repository.UpdateSnippetAsync(snippet);
            await TryPushSnippetAsync(snippet);
        }

        private void StampForSyncIfOwningCategoryShared(Snippet snippet)
        {
            Category category = GetOwningCategoryOrNull(snippet.CategoryId);
            if (category is not { Shared: true })
            {
                return;
            }

            snippet.SyncId ??= Guid.NewGuid().ToString("N");
            snippet.ModifiedAtUtc = DateTime.UtcNow;
        }

        private async Task TryPushSnippetAsync(Snippet snippet)
        {
            Category category = GetOwningCategoryOrNull(snippet.CategoryId);
            if (category is not { Shared: true })
            {
                return;
            }

            try
            {
                await _firestoreSyncService.PushSnippetAsync(category, snippet);
            }
            catch (Exception ex)
            {
                Logger.LogError(nameof(TryPushSnippetAsync), ex);
            }
        }

        private async Task TryPushCategoryAsync(Category category)
        {
            try
            {
                await _firestoreSyncService.PushCategoryAsync(category);
            }
            catch (Exception ex)
            {
                Logger.LogError(nameof(TryPushCategoryAsync), ex);
            }
        }

        private async Task TryPushCategoryBulkAsync(Category category, IReadOnlyList<Snippet> snippets)
        {
            try
            {
                await _firestoreSyncService.PushCategoryBulkAsync(category, snippets);
            }
            catch (Exception ex)
            {
                Logger.LogError(nameof(TryPushCategoryBulkAsync), ex);
            }
        }

        private async Task TryDeleteRemoteSnippetAsync(Category category, Snippet snippet)
        {
            try
            {
                await _firestoreSyncService.DeleteRemoteSnippetAsync(category.SyncId, snippet.SyncId);
            }
            catch (Exception ex)
            {
                Logger.LogError(nameof(TryDeleteRemoteSnippetAsync), ex);
            }
        }

        private async Task TryDeleteRemoteCategoryAsync(string categorySyncId)
        {
            try
            {
                await _firestoreSyncService.DeleteRemoteCategoryAsync(categorySyncId);
            }
            catch (Exception ex)
            {
                Logger.LogError(nameof(TryDeleteRemoteCategoryAsync), ex);
            }
        }

        #endregion

        public async Task RemoveSnippetAsync(SnippetViewModel snippetViewModel)
        {
            Snippet snippet = snippetViewModel.Snippet;
            Category category = GetOwningCategoryOrNull(snippet.CategoryId);

            await _repository.DeleteSnippetAsync(snippet);
            SnippetViewModels.Remove(snippetViewModel);
            GetSection(snippet.CategoryId).Snippets.Remove(snippetViewModel);

            if (category is { Shared: true } && snippet.SyncId != null)
            {
                await TryDeleteRemoteSnippetAsync(category, snippet);
            }
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

                updates.Add(UpdateSnippetAsync(snippetViewModel.Snippet));
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
                    updates.Add(UpdateSnippetAsync(snippet));
                }
            }
            return updates;
        }

        private int GetNextCategoryOrder()
        {
            return Categories.Count == 0 ? 0 : Categories.Max(c => c.Category.Order) + 1;
        }

        #region IFirestoreSyncEventSink - remote-originated changes, applied on the UI thread

        public void OnConnectionStateChanged(FirestoreConnectionState state)
        {
            // No action needed: each CategoryViewModel subscribes to the same
            // IFirestoreStatusProvider directly and re-raises its own NeedsFirebaseSetup.
        }

        public Task OnRemoteCategoryPutAsync(RemoteCategorySnapshot snapshot)
        {
            return RunOnUiThreadAsync(() => ApplyRemoteCategoryPutAsync(snapshot));
        }

        public Task OnRemoteCategoryDeletedAsync(string categorySyncId)
        {
            return RunOnUiThreadAsync(() => ApplyRemoteCategoryDeletedAsync(categorySyncId));
        }

        public Task OnRemoteSnippetPutAsync(string categorySyncId, RemoteSnippetSnapshot snapshot)
        {
            return RunOnUiThreadAsync(() => ApplyRemoteSnippetPutAsync(categorySyncId, snapshot));
        }

        public Task OnRemoteSnippetDeletedAsync(string categorySyncId, string snippetSyncId)
        {
            return RunOnUiThreadAsync(() => ApplyRemoteSnippetDeletedAsync(categorySyncId, snippetSyncId));
        }

        /// <summary>
        /// Marshals onto the WPF dispatcher thread, since Firestore's Listen() callbacks run on
        /// an SDK-managed background thread and these handlers mutate data-bound ObservableCollections.
        /// Falls back to running inline when there's no live WPF Application (e.g. under the xunit
        /// test host), so this stays directly unit-testable without spinning up a Dispatcher loop.
        /// </summary>
        private static Task RunOnUiThreadAsync(Func<Task> action)
        {
            Dispatcher dispatcher = Application.Current?.Dispatcher;
            return dispatcher == null ? action() : dispatcher.InvokeAsync(action).Task.Unwrap();
        }

        private async Task ApplyRemoteCategoryPutAsync(RemoteCategorySnapshot snapshot)
        {
            CategoryViewModel existing = Categories.FirstOrDefault(c => c.Category.SyncId == snapshot.SyncId);

            if (existing == null)
            {
                // A category shared from another machine, seen here for the first time.
                Category category = new()
                {
                    Name = snapshot.Name,
                    Order = snapshot.Order,
                    Shared = true,
                    SyncId = snapshot.SyncId,
                    ModifiedAtUtc = snapshot.ModifiedAtUtc
                };
                await _categoryRepository.SaveCategoryAsync(category);
                Categories.Add(new CategoryViewModel(category, this, _firestoreSyncService));

                if (_pendingSnippetsByCategorySyncId.Remove(snapshot.SyncId, out List<RemoteSnippetSnapshot> pending))
                {
                    foreach (RemoteSnippetSnapshot pendingSnapshot in pending)
                    {
                        await ApplyRemoteSnippetPutAsync(snapshot.SyncId, pendingSnapshot);
                    }
                }
                return;
            }

            // Last-writer-wins: only apply if the incoming change is actually newer.
            if (snapshot.ModifiedAtUtc <= existing.Category.ModifiedAtUtc)
            {
                return;
            }

            existing.Category.Name = snapshot.Name;
            existing.Category.Order = snapshot.Order;
            existing.Category.ModifiedAtUtc = snapshot.ModifiedAtUtc;
            await _categoryRepository.UpdateCategoryAsync(existing.Category);
        }

        private async Task ApplyRemoteCategoryDeletedAsync(string categorySyncId)
        {
            CategoryViewModel existing = Categories.FirstOrDefault(c => c.Category.SyncId == categorySyncId);
            if (existing == null)
            {
                // Already gone locally - an echo of our own delete, or a duplicate event. No-op.
                return;
            }

            // Mirrors DeleteCategoryAsync's local effects, but skips the Firestore round trip -
            // this delete already happened remotely.
            foreach (SnippetViewModel snippetViewModel in existing.Snippets.ToList())
            {
                existing.Snippets.Remove(snippetViewModel);
                SnippetViewModels.Remove(snippetViewModel);
                snippetViewModel.Snippet.CategoryId = null;
                UncategorizedSection.Snippets.Add(snippetViewModel);
                await _repository.UpdateSnippetAsync(snippetViewModel.Snippet);
            }

            await _categoryRepository.DeleteCategoryAsync(existing.Category);
            Categories.Remove(existing);
        }

        private async Task ApplyRemoteSnippetPutAsync(string categorySyncId, RemoteSnippetSnapshot snapshot)
        {
            CategoryViewModel category = Categories.FirstOrDefault(c => c.Category.SyncId == categorySyncId);
            if (category == null)
            {
                if (!_pendingSnippetsByCategorySyncId.TryGetValue(categorySyncId, out List<RemoteSnippetSnapshot> pending))
                {
                    pending = new List<RemoteSnippetSnapshot>();
                    _pendingSnippetsByCategorySyncId[categorySyncId] = pending;
                }
                pending.Add(snapshot);
                return;
            }

            SnippetViewModel existing = category.Snippets.FirstOrDefault(s => s.Snippet.SyncId == snapshot.SyncId);

            if (existing == null)
            {
                // Genuinely new to us: for a chunked image, this is the one case that actually
                // needs the chunk read - see RemoteSnippetSnapshot.
                byte[] imageData = snapshot.FetchChunkedImageDataAsync != null
                    ? await snapshot.FetchChunkedImageDataAsync()
                    : snapshot.ImageData;

                Snippet snippet = new()
                {
                    Type = snapshot.Type,
                    Description = snapshot.Description,
                    Content = snapshot.Content,
                    ImageData = imageData,
                    Order = snapshot.Order,
                    Locked = snapshot.Locked,
                    CategoryId = category.Category.Id,
                    SyncId = snapshot.SyncId,
                    ModifiedAtUtc = snapshot.ModifiedAtUtc
                };
                await _repository.SaveSnippetAsync(snippet);

                State state = Matches(snippet, _clipboardMonitor.CurrentContent) ? State.Active : State.Inactive;
                SnippetViewModel snippetViewModel = new(snippet, state, this);
                SnippetViewModels.Add(snippetViewModel);
                category.Snippets.Add(snippetViewModel);
                return;
            }

            // Last-writer-wins: only apply if the incoming change is actually newer.
            if (snapshot.ModifiedAtUtc <= existing.Snippet.ModifiedAtUtc)
            {
                return;
            }

            existing.Snippet.Description = snapshot.Description;
            existing.Snippet.Content = snapshot.Content;
            // Image pixels are immutable once created (SPEC: replacing one means delete+recreate
            // with a new SyncId) - never re-fetched or reapplied for a SyncId we already have, so
            // an echo of our own historical push (e.g. after a restart, when LastWriterId no
            // longer matches) can't ever clobber it with a stale/incomplete network read.
            if (existing.Snippet.Type != SnippetType.Image)
            {
                existing.Snippet.ImageData = snapshot.ImageData;
            }
            existing.Snippet.Order = snapshot.Order;
            existing.Snippet.Locked = snapshot.Locked; // one-way latch (see Snippet.Locked) - a remote unlock can't happen, matching local behaviour.
            existing.Snippet.ModifiedAtUtc = snapshot.ModifiedAtUtc;
            await _repository.UpdateSnippetAsync(existing.Snippet);

            existing.State = Matches(existing.Snippet, _clipboardMonitor.CurrentContent) ? State.Active : State.Inactive;
        }

        private async Task ApplyRemoteSnippetDeletedAsync(string categorySyncId, string snippetSyncId)
        {
            CategoryViewModel category = Categories.FirstOrDefault(c => c.Category.SyncId == categorySyncId);
            SnippetViewModel existing = category?.Snippets.FirstOrDefault(s => s.Snippet.SyncId == snippetSyncId);
            if (existing == null)
            {
                // Already gone locally - an echo of our own delete, or a duplicate event. No-op.
                return;
            }

            await _repository.DeleteSnippetAsync(existing.Snippet);
            SnippetViewModels.Remove(existing);
            category.Snippets.Remove(existing);
        }

        #endregion

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
