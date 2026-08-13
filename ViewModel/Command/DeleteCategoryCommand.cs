using System;
using System.Windows.Input;

namespace ClipboardWizard.ViewModel.Command
{
    public class DeleteCategoryCommand : ICommand
    {
        private readonly CategoryViewModel _categoryViewModel;

        public DeleteCategoryCommand(CategoryViewModel categoryViewModel)
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
            return true;
        }

        public async void Execute(object parameter)
        {
            try
            {
                await _categoryViewModel.DeleteCategoryAsync();
            }
            catch (Exception ex)
            {
                CommandErrorHandler.Handle(nameof(DeleteCategoryCommand), ex);
            }
        }
    }
}
