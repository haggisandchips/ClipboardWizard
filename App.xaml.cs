using ClipboardWizard.Service;
using ClipboardWizard.View;
using ClipboardWizard.ViewModel;
using System;
using System.IO;
using System.Windows;
using WK.Libraries.SharpClipboardNS;

namespace ClipboardWizard
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private async void Application_Startup(object sender, StartupEventArgs e)
        {
            string folderPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string applicationName = System.Reflection.Assembly.GetExecutingAssembly().GetName().Name;
            string localAppPath = Path.Combine(folderPath, applicationName);

            Logger.LogPath = Path.Combine(localAppPath, "error.log");

            try
            {
                string databasePath = Path.Combine(localAppPath, "Snippets.db");

                ISnippetRepository repository = new SnippetRepository(databasePath);
                IClipboardMonitor clipboardMonitor = new SharpClipboardMonitor(new SharpClipboard());

                WizardViewModel viewModel = new(repository, clipboardMonitor);
                await viewModel.LoadAsync();

                WizardView wizardView = new(viewModel)
                {
                    WindowStartupLocation = WindowStartupLocation.CenterScreen
                };
                wizardView.Show();
            }
            catch (Exception ex)
            {
                Logger.LogError(nameof(Application_Startup), ex);

                MessageBox.Show(
                    $"Clipboard Wizard couldn't start: {ex.Message}",
                    "Clipboard Wizard",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                Shutdown(-1);
            }
        }
    }
}
