using ClipboardWizard.Service.Firestore;
using ClipboardWizard.ViewModel;
using ClipboardWizard.ViewModel.Command;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Navigation;

namespace ClipboardWizard.View
{
    /// <summary>
    /// Interaction logic for SettingsView.xaml
    /// </summary>
    public partial class SettingsView
    {
        private static readonly Regex UrlRegex = new(@"https?://\S+", RegexOptions.Compiled);

        public SettingsView()
        {
            InitializeComponent();
            DataContextChanged += SettingsView_DataContextChanged;
        }

        private void SettingsView_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue is SettingsViewModel oldViewModel)
            {
                oldViewModel.PropertyChanged -= ViewModel_PropertyChanged;
            }

            if (e.NewValue is SettingsViewModel newViewModel)
            {
                newViewModel.PropertyChanged += ViewModel_PropertyChanged;
                UpdateTestResultInlines(newViewModel.TestResultMessage);
            }
        }

        private void ViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(SettingsViewModel.TestResultMessage) && DataContext is SettingsViewModel viewModel)
            {
                UpdateTestResultInlines(viewModel.TestResultMessage);
            }
        }

        /// <summary>
        /// Rebuilds the status message as plain text with any URL (e.g. the index-creation link
        /// in a missing-index error) turned into a real clickable Hyperlink - a plain
        /// Text-bound TextBlock can't be selected/copied by the user, and a raw link is useless
        /// if it can't be followed.
        /// </summary>
        private void UpdateTestResultInlines(string message)
        {
            TestResultTextBlock.Inlines.Clear();
            if (string.IsNullOrEmpty(message))
            {
                return;
            }

            int lastIndex = 0;
            foreach (Match match in UrlRegex.Matches(message))
            {
                if (match.Index > lastIndex)
                {
                    TestResultTextBlock.Inlines.Add(new Run(message[lastIndex..match.Index]));
                }

                Hyperlink hyperlink = new(new Run(match.Value)) { NavigateUri = new Uri(match.Value) };
                hyperlink.RequestNavigate += Hyperlink_RequestNavigate;
                TestResultTextBlock.Inlines.Add(hyperlink);

                lastIndex = match.Index + match.Value.Length;
            }

            if (lastIndex < message.Length)
            {
                TestResultTextBlock.Inlines.Add(new Run(message[lastIndex..]));
            }
        }

        private static void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
            e.Handled = true;
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

        private async void Configure_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (DataContext is not SettingsViewModel viewModel)
                {
                    return;
                }

                IReadOnlyList<RemoteCategorySnapshot> categories = await viewModel.ConfigureAsync();
                if (categories == null)
                {
                    // Connection test itself failed - already reported in the status message,
                    // per SPEC-hide-shared-categories.md's Implementation section.
                    return;
                }

                ConfigureCategoriesViewModel pickerViewModel = new(categories, viewModel.HiddenCategorySyncIds);
                ConfigureCategoriesView pickerView = new()
                {
                    DataContext = pickerViewModel,
                    Owner = this
                };

                if (pickerView.ShowDialog() != true)
                {
                    return;
                }

                await viewModel.ConfirmCategorySelectionAsync(pickerViewModel.GetHiddenCategorySyncIds());
            }
            catch (Exception ex)
            {
                CommandErrorHandler.Handle(nameof(Configure_Click), ex);
            }
        }

        private void Window_Activated(object sender, EventArgs e)
        {
            ServiceAccountJsonTextBox.Focus();
        }
    }
}
