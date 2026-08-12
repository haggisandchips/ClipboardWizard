using System;
using System.Windows.Input;

namespace ClipboardWizard.ViewModel.Command
{
    public class AddSnippetCommand : ICommand
    {
        private readonly WizardViewModel _wizardViewModel;

        public AddSnippetCommand(WizardViewModel wizardViewModel)
        {
            _wizardViewModel = wizardViewModel;
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
                await _wizardViewModel.AddNewSnippetAsync();
            }
            catch (Exception ex)
            {
                CommandErrorHandler.Handle(nameof(AddSnippetCommand), ex);
            }
        }
    }
}
