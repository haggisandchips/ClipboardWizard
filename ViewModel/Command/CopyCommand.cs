using ClipboardWizard.Model;
using System;
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
            Clipboard.SetDataObject((parameter as SnippetViewModel).Snippet.Content);
        }
    }
}
