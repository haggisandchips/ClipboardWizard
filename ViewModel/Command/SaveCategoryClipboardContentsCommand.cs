using System;
using System.Windows.Input;

namespace ClipboardWizard.ViewModel.Command
{
    public class SaveCategoryClipboardContentsCommand : ICommand
    {
        private readonly CategoryViewModel _categoryViewModel;

        public SaveCategoryClipboardContentsCommand(CategoryViewModel categoryViewModel)
        {
            _categoryViewModel = categoryViewModel;
        }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public bool CanExecute(object parameter)
        {
            return _categoryViewModel.HasSaveableClipboardContent;
        }

        public async void Execute(object parameter)
        {
            try
            {
                await _categoryViewModel.SaveClipboardSnippetAsync();
            }
            catch (Exception ex)
            {
                CommandErrorHandler.Handle(nameof(SaveCategoryClipboardContentsCommand), ex);
            }
        }
    }
}
