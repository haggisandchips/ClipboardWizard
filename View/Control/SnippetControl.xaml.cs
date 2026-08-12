using ClipboardWizard.Model;
using ClipboardWizard.Service;
using ClipboardWizard.ViewModel;
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace ClipboardWizard.View.Control
{
    /// <summary>
    /// Interaction logic for SnippetControl.xaml. Drag SOURCE only - WizardView owns drop
    /// handling and the shared drop-position indicator, since a boundary between two tiles
    /// is one shared insertion point, not something each tile should track separately.
    /// </summary>
    public partial class SnippetControl : UserControl
    {
        private Point _dragStartPoint;

        public SnippetControl()
        {
            InitializeComponent();
        }

        private void Root_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _dragStartPoint = e.GetPosition(null);
        }

        private void Root_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed || DataContext is not SnippetViewModel snippetViewModel)
            {
                return;
            }

            Vector dragged = _dragStartPoint - e.GetPosition(null);

            if (Math.Abs(dragged.X) < SystemParameters.MinimumHorizontalDragDistance &&
                Math.Abs(dragged.Y) < SystemParameters.MinimumVerticalDragDistance)
            {
                return;
            }

            DataObject dragData = new(DragDropFormats.Snippet, snippetViewModel);
            _ = DragDrop.DoDragDrop(this, dragData, DragDropEffects.Move);
        }
    }

    internal class StyleConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            State? state = values[0] as State?;

            Style activeStyle = values[1] as Style;
            Style inactiveStyle = values[2] as Style;

            switch (state)
            {
                case State.Active:
                    return activeStyle;
                default:
                    return inactiveStyle;
            }
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    /// <summary>
    /// The tile's text label: description-or-content for Text snippets, description only
    /// (possibly empty) for Image snippets, since their content isn't text at all.
    /// </summary>
    internal class ContentConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not Snippet snippet)
            {
                return string.Empty;
            }

            if (snippet.Type == SnippetType.Image)
            {
                return snippet.Description ?? string.Empty;
            }

            return string.IsNullOrEmpty(snippet.Description) ? snippet.Content : snippet.Description;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    internal class ImageBytesConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is byte[] bytes ? ImageCodec.DecodeToBitmapImage(bytes) : null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    internal class IsNotEmptyConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return !string.IsNullOrEmpty(value as string);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
