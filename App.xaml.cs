using ClipboardWizard.Service;
using ClipboardWizard.View;
using ClipboardWizard.ViewModel;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using Velopack;
using Velopack.Sources;
using WK.Libraries.SharpClipboardNS;

namespace ClipboardWizard
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private const string ReleaseRepoUrl = "https://github.com/haggisandchips/ClipboardWizard";

        public App()
        {
            // Must run before anything else: when this executable is invoked by Velopack
            // itself (install/update/uninstall hooks), this handles that and exits - it only
            // falls through to the rest of App's startup on a normal launch.
            VelopackApp.Build().Run();
        }

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

                // Fire-and-forget: an update check is a nice-to-have, not something that
                // should delay startup or take the app down if the network is unavailable.
                _ = CheckForUpdatesAsync();
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

        private static async Task CheckForUpdatesAsync()
        {
            try
            {
                UpdateManager manager = new(new GithubSource(ReleaseRepoUrl, null, false));

                if (!manager.IsInstalled)
                {
                    // Running from a plain dotnet build/publish, not a Velopack install -
                    // there's nothing to update in place.
                    return;
                }

                UpdateInfo updateInfo = await manager.CheckForUpdatesAsync();
                if (updateInfo == null)
                {
                    return;
                }

                await manager.DownloadUpdatesAsync(updateInfo);

                MessageBoxResult result = MessageBox.Show(
                    "A new version of Clipboard Wizard has been downloaded. Restart now to apply it?",
                    "Clipboard Wizard",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Information);

                if (result == MessageBoxResult.Yes)
                {
                    manager.ApplyUpdatesAndRestart(updateInfo.TargetFullRelease);
                }
                // Otherwise: already downloaded, and Velopack applies it automatically the
                // next time the app is restarted normally.
            }
            catch (Exception ex)
            {
                Logger.LogError(nameof(CheckForUpdatesAsync), ex);
            }
        }
    }
}
