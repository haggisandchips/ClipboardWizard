using System.Windows;
using WK.Libraries.SharpClipboardNS;

namespace ClipboardWizard
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static readonly SharpClipboard ClipboardMonitor = new();
    }
}
