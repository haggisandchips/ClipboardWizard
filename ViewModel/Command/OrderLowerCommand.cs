using ClipboardWizard.Model;
using System;
using System.Windows.Input;

namespace ClipboardWizard.ViewModel.Command
{
    public class OrderLowerCommand : ICommand
    {
        private SnippetViewModel _snippetViewModel;

        public OrderLowerCommand(SnippetViewModel snippetViewModel)
        {
            _snippetViewModel = snippetViewModel;
        }

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
            _snippetViewModel.OrderSnippetLower();
        }
    }
}
