using ClipboardWizard.Service;
using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Input;

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
                Clipboard.SetDataObject(snippetViewModel.Snippet.Content);
            }
            catch (COMException)
            {
                try
                {
                    Thread.Sleep(100);
                    Clipboard.SetDataObject(snippetViewModel.Snippet.Content);
                }
                catch (COMException ex)
                {
                    CommandErrorHandler.Handle(nameof(CopyCommand), ex);
                }
            }
        }
    }
}
