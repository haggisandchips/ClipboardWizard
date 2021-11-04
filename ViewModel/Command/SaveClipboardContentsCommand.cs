using ClipboardWizard.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace ClipboardWizard.ViewModel.Command
{
    public class SaveClipboardContentsCommand : ICommand
    {
        private WizardViewModel _wizardViewModel;

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
            string content = App.ClipboardMonitor.ClipboardText;

            return !string.IsNullOrWhiteSpace(content);
        }

        public void Execute(object parameter)
        {
            _wizardViewModel.SaveSnippet();
        }
    }
}
