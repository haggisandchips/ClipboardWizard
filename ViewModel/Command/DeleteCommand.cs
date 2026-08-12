using System;
using System.Windows.Input;

namespace ClipboardWizard.ViewModel.Command
{
    public class DeleteCommand : ICommand
    {
        private readonly SnippetViewModel _snippetViewModel;

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
            return !_snippetViewModel.Protected;
        }

        public async void Execute(object parameter)
        {
            try
            {
                await _snippetViewModel.DeleteSnippetAsync();
            }
            catch (Exception ex)
            {
                CommandErrorHandler.Handle(nameof(DeleteCommand), ex);
            }
        }
    }
}
