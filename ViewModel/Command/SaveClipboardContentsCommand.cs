using System;
using System.Windows.Input;

namespace ClipboardWizard.ViewModel.Command
{
    public class SaveClipboardContentsCommand : ICommand
    {
        private readonly WizardViewModel _wizardViewModel;

        public SaveClipboardContentsCommand(WizardViewModel wizardViewModel)
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
            return !string.IsNullOrWhiteSpace(_wizardViewModel.ClipboardText);
        }

        public async void Execute(object parameter)
        {
            try
            {
                await _wizardViewModel.SaveClipboardSnippetAsync();
            }
            catch (Exception ex)
            {
                CommandErrorHandler.Handle(nameof(SaveClipboardContentsCommand), ex);
            }
        }
    }
}
