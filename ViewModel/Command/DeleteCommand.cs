using ClipboardWizard.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace ClipboardWizard.ViewModel.Command
{
    public class DeleteCommand : ICommand
    {
        private SnippetViewModel _snippetViewModel;

        public DeleteCommand(SnippetViewModel snippetViewModel)
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
            _snippetViewModel.DeleteSnippet(parameter as SnippetViewModel);
        }
    }
}
