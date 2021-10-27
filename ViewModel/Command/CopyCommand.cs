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
            // TODO Comment out until styling sorted for disabled mode and false only applies to playback mode
            //return (parameter as SnippetModel)?.State == State.Inactive;
        }

        public void Execute(object parameter)
        {
            Clipboard.SetText((parameter as SnippetViewModel).Snippet.Content);
        }
    }
}
