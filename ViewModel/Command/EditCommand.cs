using System;
using System.Windows.Input;

namespace ClipboardWizard.ViewModel.Command
{
    public class EditCommand : ICommand
    {
        private readonly SnippetViewModel _snippetViewModel;

        public EditCommand(SnippetViewModel snippetViewModel)
        {
            _snippetViewModel = snippetViewModel;
        }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        // Editing is intentionally never gated by the lock: it isn't a single accidental
        // click away from destroying content the way delete is, so it doesn't need the
        // same protection.
        public bool CanExecute(object parameter)
        {
            return true;
        }

        public async void Execute(object parameter)
        {
            try
            {
                await _snippetViewModel.EditSnippetAsync();
            }
            catch (Exception ex)
            {
                CommandErrorHandler.Handle(nameof(EditCommand), ex);
            }
        }
    }
}
