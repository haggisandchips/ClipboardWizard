using System;
using System.Windows.Input;

namespace ClipboardWizard.ViewModel.Command
{
    public class OrderHigherCommand : ICommand
    {
        private readonly SnippetViewModel _snippetViewModel;

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
            return !_snippetViewModel.First;
        }

        public async void Execute(object parameter)
        {
            try
            {
                await _snippetViewModel.OrderSnippetHigherAsync();
            }
            catch (Exception ex)
            {
                CommandErrorHandler.Handle(nameof(OrderHigherCommand), ex);
            }
        }
    }
}
