using System;
using System.Windows;

namespace ClipboardWizard.View
{
    /// <summary>
    /// Interaction logic for AddCategoryView.xaml
    /// </summary>
    public partial class AddCategoryView
    {
        public AddCategoryView()
        {
            InitializeComponent();
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void Window_Activated(object sender, EventArgs e)
        {
            NameTextBox.Focus();
        }
    }
}
