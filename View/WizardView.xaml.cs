using ClipboardWizard.Model;
using ClipboardWizard.Service;
using ClipboardWizard.View.Control;
using ClipboardWizard.ViewModel;
using ClipboardWizard.ViewModel.Command;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ClipboardWizard.View
{
    /// <summary>
    /// Interaction logic for WizardView.xaml
    /// </summary>
    public partial class WizardView
    {
        private readonly WizardViewModel _viewModel;
        private readonly IWindowSettingsService _windowSettingsService;

        /// <summary>The section currently highlighted as a general (not tile-precise) snippet drop target, if any.</summary>
        private CategorySectionControl _highlightedSection;

        public WizardView(WizardViewModel viewModel, IWindowSettingsService windowSettingsService)
        {
            _viewModel = viewModel;
            _windowSettingsService = windowSettingsService;

            InitializeComponent();

            DataContext = _viewModel;
            RightWindowCommandsHost.DataContext = _viewModel;

            WindowSettings settings = _windowSettingsService.Load();
            ApplyWindowSettings(settings);
            _viewModel.UncategorizedSection.IsExpanded = settings?.UncategorizedExpanded ?? true;

            Closing += WizardView_Closing;
        }

        private void ApplyWindowSettings(WindowSettings settings)
        {
            if (settings == null || settings.Width <= 0 || settings.Height <= 0)
            {
                WindowStartupLocation = WindowStartupLocation.CenterScreen;
                return;
            }

            // A saved position can fall outside the current display area if a monitor was
            // disconnected or resolution changed since the last run - fall back to centering
            // rather than placing the window somewhere unreachable.
            Rect virtualScreen = new(
                SystemParameters.VirtualScreenLeft,
                SystemParameters.VirtualScreenTop,
                SystemParameters.VirtualScreenWidth,
                SystemParameters.VirtualScreenHeight);
            Rect windowBounds = new(settings.Left, settings.Top, settings.Width, settings.Height);

            if (!virtualScreen.IntersectsWith(windowBounds))
            {
                WindowStartupLocation = WindowStartupLocation.CenterScreen;
                return;
            }

            WindowStartupLocation = WindowStartupLocation.Manual;
            Left = settings.Left;
            Top = settings.Top;
            Width = settings.Width;
            Height = settings.Height;

            if (settings.IsMaximized)
            {
                WindowState = WindowState.Maximized;
            }
        }

        private void WizardView_Closing(object sender, CancelEventArgs e)
        {
            Rect bounds = WindowState == WindowState.Normal ? new Rect(Left, Top, Width, Height) : RestoreBounds;

            _windowSettingsService.Save(new WindowSettings
            {
                Left = bounds.Left,
                Top = bounds.Top,
                Width = bounds.Width,
                Height = bounds.Height,
                IsMaximized = WindowState == WindowState.Maximized,
                UncategorizedExpanded = _viewModel.UncategorizedSection.IsExpanded
            });
        }

        private void AccordionHost_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetData(DragDropFormats.Category) is CategoryViewModel categorySource)
            {
                CategoryDragOver((UIElement)sender, e, categorySource);
                return;
            }

            if (e.Data.GetData(DragDropFormats.Snippet) is SnippetViewModel snippetSource)
            {
                SnippetDragOver((UIElement)sender, e, snippetSource);
                return;
            }

            e.Effects = DragDropEffects.None;
            SnippetDropIndicator.Visibility = Visibility.Collapsed;
            CategoryDropIndicator.Visibility = Visibility.Collapsed;
            SetHighlightedSection(null);
        }

        private void CategoryDragOver(UIElement relativeTo, DragEventArgs e, CategoryViewModel source)
        {
            SnippetDropIndicator.Visibility = Visibility.Collapsed;
            SetHighlightedSection(null);

            CategorySectionControl target = FindUnderCursor<CategorySectionControl>(relativeTo, e);
            bool insertBefore = target != null && IsOverTopHalf(target, e);

            if (target?.DataContext is CategoryViewModel targetCategory
                && _viewModel.WouldReorderCategory(source, targetCategory, insertBefore))
            {
                e.Effects = DragDropEffects.Move;
                PositionCategoryIndicator(target, insertBefore);
                return;
            }

            e.Effects = DragDropEffects.None;
            CategoryDropIndicator.Visibility = Visibility.Collapsed;
        }

        private void SnippetDragOver(UIElement relativeTo, DragEventArgs e, SnippetViewModel source)
        {
            CategoryDropIndicator.Visibility = Visibility.Collapsed;

            CategorySectionControl headerTarget = FindEmptyOrCollapsedHeaderUnderCursor(relativeTo, e);
            if (headerTarget?.DataContext is ICategorySection headerSection)
            {
                if (_viewModel.WouldAssignCategory(source, headerSection))
                {
                    SetHighlightedSection(headerTarget);
                    e.Effects = DragDropEffects.Move;
                    SnippetDropIndicator.Visibility = Visibility.Collapsed;
                    return;
                }

                SetHighlightedSection(null);
                e.Effects = DragDropEffects.None;
                SnippetDropIndicator.Visibility = Visibility.Collapsed;
                return;
            }

            SnippetControl targetTile = FindUnderCursor<SnippetControl>(relativeTo, e);
            if (targetTile?.DataContext is SnippetViewModel targetSnippet)
            {
                SetHighlightedSection(null);
                bool insertBefore = IsOverLeftHalf(targetTile, e);

                if (_viewModel.WouldMoveSnippet(source, targetSnippet, insertBefore))
                {
                    e.Effects = DragDropEffects.Move;
                    PositionSnippetIndicator(targetTile, insertBefore);
                    return;
                }

                e.Effects = DragDropEffects.None;
                SnippetDropIndicator.Visibility = Visibility.Collapsed;
                return;
            }

            // Not over a specific tile - allow a general drop anywhere in a (non-empty,
            // expanded) section's body to assign/append the snippet there. No line to indicate
            // a position, so the section itself is highlighted instead, the same blue as the
            // indicators - unless it's already the snippet's own category, which wouldn't
            // change anything.
            CategorySectionControl targetSection = FindUnderCursor<CategorySectionControl>(relativeTo, e);
            bool wouldAssign = targetSection?.DataContext is ICategorySection targetGeneralSection
                && _viewModel.WouldAssignCategory(source, targetGeneralSection);

            SetHighlightedSection(wouldAssign ? targetSection : null);
            e.Effects = wouldAssign ? DragDropEffects.Move : DragDropEffects.None;
            SnippetDropIndicator.Visibility = Visibility.Collapsed;
        }

        private void SetHighlightedSection(CategorySectionControl section)
        {
            if (ReferenceEquals(_highlightedSection, section))
            {
                return;
            }

            if (_highlightedSection != null)
            {
                _highlightedSection.IsSnippetDropTarget = false;
            }

            _highlightedSection = section;

            if (_highlightedSection != null)
            {
                _highlightedSection.IsSnippetDropTarget = true;
            }
        }

        private void AccordionHost_DragLeave(object sender, DragEventArgs e)
        {
            SnippetDropIndicator.Visibility = Visibility.Collapsed;
            CategoryDropIndicator.Visibility = Visibility.Collapsed;
            SetHighlightedSection(null);
        }

        private async void AccordionHost_Drop(object sender, DragEventArgs e)
        {
            SnippetDropIndicator.Visibility = Visibility.Collapsed;
            CategoryDropIndicator.Visibility = Visibility.Collapsed;
            SetHighlightedSection(null);

            try
            {
                if (e.Data.GetData(DragDropFormats.Category) is CategoryViewModel categorySource)
                {
                    await DropCategoryAsync((UIElement)sender, e, categorySource);
                }
                else if (e.Data.GetData(DragDropFormats.Snippet) is SnippetViewModel snippetSource)
                {
                    await DropSnippetAsync((UIElement)sender, e, snippetSource);
                }
            }
            catch (Exception ex)
            {
                CommandErrorHandler.Handle(nameof(WizardView), ex);
            }
        }

        private Task DropCategoryAsync(UIElement relativeTo, DragEventArgs e, CategoryViewModel source)
        {
            CategorySectionControl targetControl = FindUnderCursor<CategorySectionControl>(relativeTo, e);

            if (targetControl?.DataContext is not CategoryViewModel target || ReferenceEquals(source, target))
            {
                return Task.CompletedTask;
            }

            return source.MoveToAsync(target, IsOverTopHalf(targetControl, e));
        }

        private Task DropSnippetAsync(UIElement relativeTo, DragEventArgs e, SnippetViewModel source)
        {
            CategorySectionControl headerTarget = FindEmptyOrCollapsedHeaderUnderCursor(relativeTo, e);
            if (headerTarget?.DataContext is ICategorySection headerSection)
            {
                return _viewModel.AssignCategoryAsync(source, headerSection);
            }

            SnippetControl targetTile = FindUnderCursor<SnippetControl>(relativeTo, e);
            if (targetTile?.DataContext is SnippetViewModel target)
            {
                return ReferenceEquals(source, target) ? Task.CompletedTask : source.MoveToAsync(target, IsOverLeftHalf(targetTile, e));
            }

            CategorySectionControl targetSection = FindUnderCursor<CategorySectionControl>(relativeTo, e);
            return targetSection?.DataContext is ICategorySection section
                ? _viewModel.AssignCategoryAsync(source, section)
                : Task.CompletedTask;
        }

        // Roughly half a tile's height - a plain exact-bounds hit test against a ~44px header
        // sitting right against a 150px tile is nearly as easy to miss as the bug itself, so
        // this header's effective target zone reaches this far above and below its own
        // rendered bounds, claiming any nearby ambiguous drop rather than requiring the cursor
        // land pixel-precisely inside it.
        private const double HeaderTargetBuffer = 75;

        /// <summary>
        /// Header hit test (with a generous buffer, see HeaderTargetBuffer) for whichever empty
        /// or collapsed section a snippet drag is over. Such a section has no tiles of its own
        /// to hit-test against, and its thin header can sit close enough to a neighbouring
        /// section's content that a plain point hit-test (which just returns whatever's
        /// literally under the cursor) lands there instead - misattributing the drop. Checked
        /// ahead of tile-precise targeting so these sections stay a reliable, if header-only,
        /// target.
        ///
        /// Two-tier, and checked against every section's WHOLE rendered area (header, the small
        /// gap below it, and its tile grid together) rather than just its header specifically -
        /// otherwise even the gap within a populated section's own control, between its header
        /// and its first tile row, isn't recognised as belonging to it, and a neighbouring empty
        /// or collapsed section's buffer can claim it. Any section whose own bounds actually
        /// contain the cursor wins outright, however close a *different* section's buffer also
        /// reaches here. If that exact-match section doesn't itself need header-only handling,
        /// this reports no header target at all, deferring to normal tile-precise/general
        /// hit-testing for it. The buffer only picks a winner (by nearest edge, among
        /// empty/collapsed sections only) when the cursor isn't inside any section's own bounds.
        /// </summary>
        private static CategorySectionControl FindEmptyOrCollapsedHeaderUnderCursor(UIElement relativeTo, DragEventArgs e)
        {
            Point cursor = e.GetPosition(relativeTo);

            CategorySectionControl closest = null;
            double closestDistance = double.MaxValue;

            foreach (CategorySectionControl section in FindSections(relativeTo))
            {
                if (section.DataContext is not ICategorySection categorySection)
                {
                    continue;
                }

                Point topLeft = section.TranslatePoint(new Point(0, 0), relativeTo);
                double top = topLeft.Y;
                double bottom = topLeft.Y + section.ActualHeight;

                bool withinX = cursor.X >= topLeft.X && cursor.X <= topLeft.X + section.ActualWidth;
                if (!withinX)
                {
                    continue;
                }

                bool needsHeaderOnlyTargeting = categorySection.Snippets.Count == 0 || !categorySection.IsExpanded;

                if (cursor.Y >= top && cursor.Y <= bottom)
                {
                    return needsHeaderOnlyTargeting ? section : null;
                }

                if (!needsHeaderOnlyTargeting
                    || cursor.Y < top - HeaderTargetBuffer
                    || cursor.Y > bottom + HeaderTargetBuffer)
                {
                    continue;
                }

                double distance = cursor.Y < top ? top - cursor.Y : cursor.Y - bottom;
                if (distance < closestDistance)
                {
                    closest = section;
                    closestDistance = distance;
                }
            }

            return closest;
        }

        private static IEnumerable<CategorySectionControl> FindSections(DependencyObject root)
        {
            int count = VisualTreeHelper.GetChildrenCount(root);
            for (int i = 0; i < count; i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(root, i);
                if (child is CategorySectionControl section)
                {
                    yield return section;
                }

                foreach (CategorySectionControl nested in FindSections(child))
                {
                    yield return nested;
                }
            }
        }

        private void PositionSnippetIndicator(SnippetControl target, bool insertBefore)
        {
            Point targetTopLeft = target.TranslatePoint(new Point(0, 0), SnippetIndicatorCanvas);
            double centerX = targetTopLeft.X + (insertBefore ? 0 : target.ActualWidth);

            Canvas.SetLeft(SnippetDropIndicator, centerX - SnippetDropIndicator.Width / 2);
            Canvas.SetTop(SnippetDropIndicator, targetTopLeft.Y);
            SnippetDropIndicator.Height = target.ActualHeight;
            SnippetDropIndicator.Visibility = Visibility.Visible;
        }

        private void PositionCategoryIndicator(CategorySectionControl target, bool insertBefore)
        {
            Point targetTopLeft = target.TranslatePoint(new Point(0, 0), CategoryIndicatorCanvas);
            double centerY = targetTopLeft.Y + (insertBefore ? 0 : target.ActualHeight);

            Canvas.SetTop(CategoryDropIndicator, centerY - CategoryDropIndicator.Height / 2);
            Canvas.SetLeft(CategoryDropIndicator, targetTopLeft.X);
            CategoryDropIndicator.Width = target.ActualWidth;
            CategoryDropIndicator.Visibility = Visibility.Visible;
        }

        private static bool IsOverLeftHalf(SnippetControl tile, DragEventArgs e)
        {
            return e.GetPosition(tile).X < tile.ActualWidth / 2;
        }

        private static bool IsOverTopHalf(CategorySectionControl section, DragEventArgs e)
        {
            return e.GetPosition(section).Y < section.ActualHeight / 2;
        }

        private static T FindUnderCursor<T>(UIElement relativeTo, DragEventArgs e) where T : DependencyObject
        {
            HitTestResult hit = VisualTreeHelper.HitTest(relativeTo, e.GetPosition(relativeTo));
            DependencyObject current = hit?.VisualHit;

            while (current != null && current is not T)
            {
                current = VisualTreeHelper.GetParent(current);
            }

            return current as T;
        }
    }
}
