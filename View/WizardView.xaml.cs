using ClipboardWizard.View.Control;
using ClipboardWizard.ViewModel;
using ClipboardWizard.ViewModel.Command;
using System;
using System.Collections.Specialized;
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

        public UniformGrid SnippetsUniformGrid { get; private set; }

        public WizardView(WizardViewModel viewModel)
        {
            _viewModel = viewModel;

            InitializeComponent();

            DataContext = _viewModel;
            RightWindowCommandsHost.DataContext = _viewModel;

            _viewModel.SnippetViewModels.CollectionChanged += Snippets_CollectionChanged;
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

            if (target == null || !e.Data.GetDataPresent(DragDropFormats.Snippet))
            {
                DropIndicator.Visibility = Visibility.Collapsed;
                return;
            }

            e.Effects = DragDropEffects.Move;
            PositionIndicator(target, IsOverLeftHalf(target, e));
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
