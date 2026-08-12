using System;
using System.Windows.Input;

namespace ClipboardWizard.ViewModel.Command
{
    public class OrderLowerCommand : ICommand
    {
        private readonly SnippetViewModel _snippetViewModel;

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
            return !_snippetViewModel.Last;
        }

        public async void Execute(object parameter)
        {
            try
            {
                await _snippetViewModel.OrderSnippetLowerAsync();
            }
            catch (Exception ex)
            {
                CommandErrorHandler.Handle(nameof(OrderLowerCommand), ex);
            }
        }
    }
}
