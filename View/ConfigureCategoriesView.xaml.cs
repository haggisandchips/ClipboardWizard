using System.Windows;

namespace ClipboardWizard.View
{
    /// <summary>
    /// Interaction logic for ConfigureCategoriesView.xaml. Purely a checkbox-list dialog - the
    /// index-verification round trip for whatever gets unchecked happens after this closes (see
    /// SettingsView.Configure_Click), so this has no Firestore dependency of its own.
    /// </summary>
    public partial class ConfigureCategoriesView
    {
        public ConfigureCategoriesView()
        {
            InitializeComponent();
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
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
