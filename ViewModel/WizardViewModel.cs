using ClipboardWizard.Model;
using ClipboardWizard.Service;
using ClipboardWizard.ViewModel.Command;
using System;
using System.Collections.ObjectModel;
using static WK.Libraries.SharpClipboardNS.SharpClipboard;

namespace ClipboardWizard.ViewModel
{
    public class WizardViewModel
    {
        public ObservableCollection<SnippetViewModel> SnippetViewModels { get; set; } = new();

        public bool Recording { get; set; }

        public SaveClipboardContentsCommand SaveClipboardContents { get; private set; }

        public AddSnippetCommand AddSnippet { get; private set; }

        public WizardViewModel()
        {
            SnippetManager.LoadSnippets()
                .ConvertAll(snippet => new SnippetViewModel(snippet, State.Inactive))
                .ForEach(vm => SnippetViewModels.Add(vm));

            App.ClipboardMonitor.ClipboardChanged += ClipboardManager_ClipboardChanged;
            App.SnippetDeleted += App_SnippetDeleted;
            App.SnippetUpdated += App_SnippetUpdated;

            SaveClipboardContents = new SaveClipboardContentsCommand(this);
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

        internal void AddNewSnippet()
        {
            throw new NotImplementedException();
        }

        internal void SaveSnippet()
        {
            string content = App.ClipboardMonitor.ClipboardText;
            if(string.IsNullOrWhiteSpace(content))
            {
                return;
            }

            Snippet snippet = new Snippet() { Content = content };
            SnippetManager.SaveSnippet(snippet);

            SnippetViewModels.Add(new(snippet, State.Active));
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