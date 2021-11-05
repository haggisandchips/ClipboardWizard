using System;
using System.Windows.Input;

namespace ClipboardWizard.ViewModel.Command
{
    public class AddSnippetCommand : ICommand
    {
        private WizardViewModel _wizardViewModel;

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

        public void Execute(object parameter)
        {
            _wizardViewModel.AddNewSnippet();
        }
    }
}
