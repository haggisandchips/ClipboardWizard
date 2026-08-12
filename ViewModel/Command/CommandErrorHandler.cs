using ClipboardWizard.Service;
using System;
using System.Windows;

namespace ClipboardWizard.ViewModel.Command
{
    /// <summary>
    /// Shared failure path for ICommand.Execute implementations. Execute is void-returning so
    /// commands run as async void - any exception that escapes one would otherwise be
    /// unhandled and take the app down, so every command funnels failures through here
    /// instead: log for diagnosis, tell the user the action didn't happen.
    /// </summary>
    internal static class CommandErrorHandler
    {
        public static void Handle(string context, Exception exception)
        {
            Logger.LogError(context, exception);

            MessageBox.Show(
                $"That didn't work: {exception.Message}",
                "Clipboard Wizard",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }
}
