using System;
using System.Windows.Input;

namespace ClipboardWizard.ViewModel.Command
{
    public class LockCommand : ICommand
    {
        private readonly SnippetViewModel _snippetViewModel;

        public LockCommand(SnippetViewModel snippetViewModel)
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
            return _snippetViewModel.Protected || !_snippetViewModel.Snippet.Locked;
        }

        public async void Execute(object parameter)
        {
            try
            {
                await _snippetViewModel.HandleLockAsync();
            }
            catch (Exception ex)
            {
                CommandErrorHandler.Handle(nameof(LockCommand), ex);
            }
        }
    }
}
