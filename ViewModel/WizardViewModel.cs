using ClipboardWizard.Model;
using ClipboardWizard.Service;
using System;
using System.Collections.ObjectModel;
using static WK.Libraries.SharpClipboardNS.SharpClipboard;

namespace ClipboardWizard.ViewModel
{
    public class WizardViewModel
    {
        public ObservableCollection<SnippetViewModel> SnippetViewModels { get; set; } = new();

        public bool Recording { get; set; }

        public WizardViewModel()
        {
            SnippetManager.LoadSnippets()
                .ConvertAll(snippet => new SnippetViewModel(snippet, State.Inactive))
                .ForEach(vm => SnippetViewModels.Add(vm));

            App.ClipboardMonitor.ClipboardChanged += ClipboardManager_ClipboardChanged;
            App.SnippetDeleted += App_SnippetDeleted;
            App.SnippetUpdated += App_SnippetUpdated;
        }

        private void App_SnippetDeleted(object sender, App.SnippetChangedEventArgs e)
        {
            SnippetViewModel snippetViewModel = e.SnippetViewModel;
            _ = SnippetViewModels.Remove(snippetViewModel);
        }

        private void App_SnippetUpdated(object sender, App.SnippetChangedEventArgs e)
        {
            SnippetViewModel snippetViewModel = e.SnippetViewModel;

            string content = App.ClipboardMonitor.ClipboardText;

            bool equal = !string.IsNullOrWhiteSpace(content) && snippetViewModel.Snippet.Content.Equals(content, StringComparison.Ordinal);
            snippetViewModel.State = equal ? State.Active : State.Inactive;
        }

        private void ClipboardManager_ClipboardChanged(object sender, ClipboardChangedEventArgs e)
        {
            if (e.ContentType != ContentTypes.Text)
            {
                return;
            }

            string content = e.Content.ToString();

            bool nullOrWhitespace = string.IsNullOrWhiteSpace(content);
            bool matched = false;

            foreach (SnippetViewModel snippetViewModel in SnippetViewModels)
            {
                bool equal = !nullOrWhitespace && snippetViewModel.Snippet.Content.Equals(content, StringComparison.Ordinal);
                snippetViewModel.State = equal ? State.Active : State.Inactive;

                matched |= equal;
            }

            if (!nullOrWhitespace && !matched && Recording)
            {
                Snippet snippet = new Snippet() { Content = content };
                SnippetManager.SaveSnippet(snippet);

                SnippetViewModels.Add(new(snippet, State.Active));
            }
        }
    }
}