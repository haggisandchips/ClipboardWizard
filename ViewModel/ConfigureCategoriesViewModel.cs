using ClipboardWizard.Service.Firestore;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;

namespace ClipboardWizard.ViewModel
{
    /// <summary>Backs the Settings "Configure" category picker - one row per Shared category in the remote database, checked unless it's in the currently-persisted hidden set.</summary>
    public class ConfigureCategoriesViewModel : INotifyPropertyChanged
    {
        /// <summary>Firestore's `not-in` operator accepts at most 10 values - see SPEC-hide-shared-categories.md.</summary>
        internal const int MaxHiddenCount = 10;

        public ObservableCollection<CategoryPickerItem> Categories { get; } = new();

        public bool CanConfirm => Categories.Count(c => !c.IsChecked) <= MaxHiddenCount;

        public string TooManyHiddenMessage => $"At most {MaxHiddenCount} categories can be unchecked at once.";

        public event PropertyChangedEventHandler PropertyChanged;

        public ConfigureCategoriesViewModel(IReadOnlyList<RemoteCategorySnapshot> remoteCategories, IReadOnlySet<string> hiddenCategorySyncIds)
        {
            foreach (RemoteCategorySnapshot remoteCategory in remoteCategories)
            {
                CategoryPickerItem item = new(remoteCategory.SyncId, remoteCategory.Name, !hiddenCategorySyncIds.Contains(remoteCategory.SyncId));
                item.PropertyChanged += (_, _) => OnPropertyChanged(nameof(CanConfirm));
                Categories.Add(item);
            }
        }

        public HashSet<string> GetHiddenCategorySyncIds()
        {
            return Categories.Where(c => !c.IsChecked).Select(c => c.SyncId).ToHashSet();
        }

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class CategoryPickerItem : INotifyPropertyChanged
    {
        public string SyncId { get; }

        public string Name { get; }

        private bool _isChecked;
        public bool IsChecked
        {
            get => _isChecked;
            set
            {
                _isChecked = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsChecked)));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public CategoryPickerItem(string syncId, string name, bool isChecked)
        {
            SyncId = syncId;
            Name = name;
            _isChecked = isChecked;
        }
    }
}
