using System.Windows;

namespace ClipboardWizard.View
{
    /// <summary>
    /// Interaction logic for EditSnippetView.xaml
    /// </summary>
    public partial class EditSnippetView
    {
        public EditSnippetView()
        {
            InitializeComponent();
        }

        private void Update_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
