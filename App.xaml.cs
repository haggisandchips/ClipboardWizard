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
        private static string folderPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        private static string applicationName = System.Reflection.Assembly.GetExecutingAssembly().GetName().Name;
        private static string databaseName = "Snippets.db";

        public static string localAppPath = Path.Combine(folderPath, applicationName);
        public static string databasePath = Path.Combine(localAppPath, databaseName);

        public static readonly SharpClipboard ClipboardMonitor = new();

        public static event EventHandler<SnippetDeletedEventArgs> SnippetDeleted;

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

        public static void DeleteSnippet(SnippetViewModel snippetViewModel)
        {
            SnippetDeleted?.Invoke(null, new() { SnippetViewModel = snippetViewModel });
        }

        public class SnippetDeletedEventArgs
        {
            public SnippetViewModel SnippetViewModel { get; set; }
        }
    }
}
