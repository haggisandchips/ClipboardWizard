using ClipboardWizard.Model;
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
        // same protection. It IS gated by content type: there's no sensible way to "edit" an
        // image's pixels in a text box, so image snippets are delete-and-re-add only.
        public bool CanExecute(object parameter)
        {
            return _snippetViewModel.Snippet.Type == SnippetType.Text;
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
