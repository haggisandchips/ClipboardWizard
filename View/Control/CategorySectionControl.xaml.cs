using ClipboardWizard.ViewModel;
using ClipboardWizard.ViewModel.Command;
using System;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace ClipboardWizard.View.Control
{
    /// <summary>
    /// Interaction logic for CategorySectionControl.xaml. Renders one accordion section - see
    /// the XAML comment for why it only sources drags rather than also handling drops.
    /// </summary>
    public partial class CategorySectionControl : UserControl
    {
        private static readonly Brush NormalBackground = new SolidColorBrush(Color.FromRgb(0xE3, 0xE3, 0xE3));

        private Point _dragStartPoint;
        private bool _dragging;
        private UniformGrid _snippetsGrid;
        private bool _isSnippetDropTarget;

        public CategorySectionControl()
        {
            InitializeComponent();

            DataContextChanged += CategorySectionControl_DataContextChanged;
        }

        /// <summary>
        /// Set by WizardView while a dragged snippet is hovering generally over this section
        /// (not over one of its own tiles, which has its own line indicator) - highlights the
        /// header the same blue as the reorder indicators, instead of leaving it unclear which
        /// section a drop would land in.
        /// </summary>
        public bool IsSnippetDropTarget
        {
            get => _isSnippetDropTarget;
            set
            {
                _isSnippetDropTarget = value;
                HeaderBorder.Background = value ? Brushes.DodgerBlue : NormalBackground;
            }
        }

        private void CategorySectionControl_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue is ICategorySection oldSection)
            {
                oldSection.Snippets.CollectionChanged -= Snippets_CollectionChanged;
            }

            if (e.NewValue is ICategorySection newSection)
            {
                newSection.Snippets.CollectionChanged += Snippets_CollectionChanged;
            }
        }

        private void Snippets_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            RecalculateColumns();
        }

        private void SnippetsUniformGrid_Loaded(object sender, RoutedEventArgs e)
        {
            _snippetsGrid = sender as UniformGrid;
            SizeChanged += (_, _) => RecalculateColumns();

            RecalculateColumns();
        }

        private void RecalculateColumns()
        {
            if (_snippetsGrid == null || DataContext is not ICategorySection section)
            {
                return;
            }

            int columns = Math.Min((int)(_snippetsGrid.ActualWidth / 200), section.Snippets.Count);

            _snippetsGrid.Columns = columns > 0 ? columns : 1;
        }

        private void Header_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _dragStartPoint = e.GetPosition(null);
            _dragging = false;
        }

        private void Header_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (_dragging || e.LeftButton != MouseButtonState.Pressed || DataContext is not CategoryViewModel categoryViewModel)
            {
                // Only real categories can be dragged to reorder - the pinned Uncategorized
                // section (ICategorySection but not a CategoryViewModel) never starts a drag.
                return;
            }

            Vector dragged = _dragStartPoint - e.GetPosition(null);

            if (Math.Abs(dragged.X) < SystemParameters.MinimumHorizontalDragDistance &&
                Math.Abs(dragged.Y) < SystemParameters.MinimumVerticalDragDistance)
            {
                return;
            }

            _dragging = true;
            DataObject dragData = new(DragDropFormats.Category, categoryViewModel);
            _ = DragDrop.DoDragDrop(this, dragData, DragDropEffects.Move);
        }

        private async void Header_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            bool wasDragging = _dragging;
            _dragging = false;

            if (wasDragging
                || DataContext is not ICategorySection section
                || (e.OriginalSource is DependencyObject source && IsOnAnyOf(source, DeleteButton, SaveClipboardContentsButton, AddSnippetButton)))
            {
                // A drag already happened, or this press/release was on one of the header's own
                // buttons - either way, it isn't a toggle.
                return;
            }

            try
            {
                if (section is CategoryViewModel categoryViewModel)
                {
                    await categoryViewModel.ToggleExpandedAsync();
                }
                else
                {
                    section.IsExpanded = !section.IsExpanded;
                }
            }
            catch (Exception ex)
            {
                CommandErrorHandler.Handle(nameof(CategorySectionControl), ex);
            }
        }

        private static bool IsOnAnyOf(DependencyObject element, params DependencyObject[] ancestors)
        {
            while (element != null)
            {
                foreach (DependencyObject ancestor in ancestors)
                {
                    if (ReferenceEquals(element, ancestor))
                    {
                        return true;
                    }
                }

                element = VisualTreeHelper.GetParent(element);
            }

            return false;
        }
    }
}
