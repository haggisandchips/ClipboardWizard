using ClipboardWizard.Model;
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

    internal class ContentConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Snippet snippet = value as Snippet;

            return string.IsNullOrEmpty(snippet.Description) ? snippet.Content : snippet.Description;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
