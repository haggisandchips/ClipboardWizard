using ClipboardWizard.Model;
using ClipboardWizard.Service;
using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace ClipboardWizard.ViewModel.Command
{
    public class CopyCommand : ICommand
    {
        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {
            if (parameter is not SnippetViewModel snippetViewModel)
            {
                return;
            }

            // The clipboard is a shared OS resource that other processes can briefly hold a
            // lock on (e.g. antivirus scanners); one retry after a short delay clears the
            // vast majority of these without bothering the user.
            try
            {
                SetClipboard(snippetViewModel.Snippet);
            }
            catch (COMException)
            {
                try
                {
                    Thread.Sleep(100);
                    SetClipboard(snippetViewModel.Snippet);
                }
                catch (COMException ex)
                {
                    CommandErrorHandler.Handle(nameof(CopyCommand), ex);
                }
            }
        }

        private static void SetClipboard(Snippet snippet)
        {
            if (snippet.Type == SnippetType.Image)
            {
                BitmapImage image = ImageCodec.DecodeToBitmapImage(snippet.ImageData);
                if (image != null)
                {
                    Clipboard.SetImage(image);
                }
            }
            else
            {
                Clipboard.SetDataObject(snippet.Content);
            }
        }
    }
}
