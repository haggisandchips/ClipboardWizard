using ClipboardWizard.Model;
using ClipboardWizard.Service;
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace ClipboardWizard.View.Control
{
    /// <summary>
    /// Interaction logic for SnippetControl.xaml
    /// </summary>
    public partial class SnippetControl : UserControl
    {
        public SnippetControl()
        {
            InitializeComponent();
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
