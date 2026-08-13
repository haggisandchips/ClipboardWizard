using ClipboardWizard.Model;
using ClipboardWizard.Service;
using ClipboardWizard.View.Control;
using ClipboardWizard.ViewModel;
using ClipboardWizard.ViewModel.Command;
using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
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

        public UniformGrid SnippetsUniformGrid { get; private set; }

        public WizardView(WizardViewModel viewModel, IWindowSettingsService windowSettingsService)
        {
            _viewModel = viewModel;
            _windowSettingsService = windowSettingsService;

            InitializeComponent();

            DataContext = _viewModel;
            RightWindowCommandsHost.DataContext = _viewModel;

            _viewModel.SnippetViewModels.CollectionChanged += Snippets_CollectionChanged;

            ApplyWindowSettings(_windowSettingsService.Load());
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
                IsMaximized = WindowState == WindowState.Maximized
            });
        }

        private void Snippets_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            RecalculateColumns();
        }

        private void SnippetsUniformGrid_Loaded(object sender, RoutedEventArgs e)
        {
            SnippetsUniformGrid = sender as UniformGrid;
            SizeChanged += Window_SizedChangedEvent;

            RecalculateColumns();
        }

        private void Window_SizedChangedEvent(object sender, SizeChangedEventArgs e)
        {
            RecalculateColumns();
        }

        private void RecalculateColumns()
        {
            if (SnippetsUniformGrid == null)
            {
                return;
            }

            int columns = Math.Min((int)(SnippetsUniformGrid.ActualWidth / 200), _viewModel.SnippetViewModels.Count);

            SnippetsUniformGrid.Columns = columns > 0 ? columns : 1;
        }

        private void SnippetsHost_DragOver(object sender, DragEventArgs e)
        {
            SnippetControl target = FindTileUnderCursor((UIElement)sender, e);

            if (target?.DataContext is SnippetViewModel targetViewModel
                && e.Data.GetData(DragDropFormats.Snippet) is SnippetViewModel source)
            {
                bool insertBefore = IsOverLeftHalf(target, e);

                if (_viewModel.WouldReorder(source, targetViewModel, insertBefore))
                {
                    e.Effects = DragDropEffects.Move;
                    PositionIndicator(target, insertBefore);
                    return;
                }
            }

            e.Effects = DragDropEffects.None;
            DropIndicator.Visibility = Visibility.Collapsed;
        }

        private void SnippetsHost_DragLeave(object sender, DragEventArgs e)
        {
            DropIndicator.Visibility = Visibility.Collapsed;
        }

        private async void SnippetsHost_Drop(object sender, DragEventArgs e)
        {
            SnippetControl targetControl = FindTileUnderCursor((UIElement)sender, e);
            DropIndicator.Visibility = Visibility.Collapsed;

            if (targetControl?.DataContext is not SnippetViewModel target
                || e.Data.GetData(DragDropFormats.Snippet) is not SnippetViewModel source
                || ReferenceEquals(source, target))
            {
                return;
            }

            try
            {
                await source.MoveToAsync(target, IsOverLeftHalf(targetControl, e));
            }
            catch (Exception ex)
            {
                CommandErrorHandler.Handle(nameof(WizardView), ex);
            }
        }

        private void PositionIndicator(SnippetControl target, bool insertBefore)
        {
            Point targetTopLeft = target.TranslatePoint(new Point(0, 0), IndicatorCanvas);
            double centerX = targetTopLeft.X + (insertBefore ? 0 : target.ActualWidth);

            Canvas.SetLeft(DropIndicator, centerX - DropIndicator.Width / 2);
            Canvas.SetTop(DropIndicator, targetTopLeft.Y);
            DropIndicator.Height = target.ActualHeight;
            DropIndicator.Visibility = Visibility.Visible;
        }

        private static bool IsOverLeftHalf(SnippetControl tile, DragEventArgs e)
        {
            return e.GetPosition(tile).X < tile.ActualWidth / 2;
        }

        private static SnippetControl FindTileUnderCursor(UIElement relativeTo, DragEventArgs e)
        {
            HitTestResult hit = VisualTreeHelper.HitTest(relativeTo, e.GetPosition(relativeTo));
            DependencyObject current = hit?.VisualHit;

            while (current != null && current is not SnippetControl)
            {
                current = VisualTreeHelper.GetParent(current);
            }

            return current as SnippetControl;
        }
    }
}
