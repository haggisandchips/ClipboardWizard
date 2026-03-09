using ClipboardWizard.Model;
using System;
using System.Windows.Input;

namespace ClipboardWizard.ViewModel.Command
{
    public class OrderHigherCommand : ICommand
    {
        private SnippetViewModel _snippetViewModel;

        public OrderHigherCommand(SnippetViewModel snippetViewModel)
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
            return !_snippetViewModel.Locked;
        }

        public void Execute(object parameter)
        {
            _snippetViewModel.OrderSnippetHigher();
        }
    }
}
