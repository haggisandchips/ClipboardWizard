using ClipboardWizard.ViewModel;
using ClipboardWizard.ViewModel.Command;
using Microsoft.Win32;
using System;
using System.IO;
using System.Windows;

namespace ClipboardWizard.View
{
    /// <summary>
    /// Interaction logic for SettingsView.xaml
    /// </summary>
    public partial class SettingsView
    {
        public SettingsView()
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

        private void Browse_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new()
            {
                Filter = "Service account key (*.json)|*.json|All files (*.*)|*.*",
                Title = "Select a Firebase service-account key"
            };

            if (dialog.ShowDialog(this) != true)
            {
                return;
            }

            try
            {
                if (DataContext is SettingsViewModel viewModel)
                {
                    viewModel.ServiceAccountJson = File.ReadAllText(dialog.FileName);
                }
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                CommandErrorHandler.Handle(nameof(Browse_Click), ex);
            }
        }

        private async void TestConnection_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (DataContext is SettingsViewModel viewModel)
                {
                    await viewModel.TestConnectionAsync();
                }
            }
            catch (Exception ex)
            {
                CommandErrorHandler.Handle(nameof(TestConnection_Click), ex);
            }
        }

        private void Window_Activated(object sender, EventArgs e)
        {
            ServiceAccountJsonTextBox.Focus();
        }
    }
}
