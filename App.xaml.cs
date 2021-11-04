using ClipboardWizard.Model;
using ClipboardWizard.View;
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
        private static string _folderPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        private static string _applicationName = System.Reflection.Assembly.GetExecutingAssembly().GetName().Name;
        private static string _databaseName = "Snippets.db";

        public static string localAppPath = Path.Combine(_folderPath, _applicationName);
        public static string databasePath = Path.Combine(localAppPath, _databaseName);

        public static readonly SharpClipboard ClipboardMonitor = new();

        public static event EventHandler<SnippetChangedEventArgs> SnippetDeleted;
        public static event EventHandler<SnippetChangedEventArgs> SnippetUpdated;

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            InitializeDatabaseFolder();

            WizardView wizardView = new();
            wizardView.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            wizardView.Show();
        }

        private void InitializeDatabaseFolder()
        {
            if (!Directory.Exists(App.localAppPath))
            {
                _ = Directory.CreateDirectory(App.localAppPath);
            }
        }

        public static void UpdateSnippet(SnippetViewModel snippetViewModel)
        {
            SnippetUpdated?.Invoke(null, new() { SnippetViewModel = snippetViewModel });
        }

        public static void DeleteSnippet(SnippetViewModel snippetViewModel)
        {
            SnippetDeleted?.Invoke(null, new() { SnippetViewModel = snippetViewModel });
        }

        public class SnippetChangedEventArgs
        {
            public SnippetViewModel SnippetViewModel { get; set; }
        }
    }
}
