using ClipboardWizard.Model;
using ClipboardWizard.Service;
using System;
using System.Collections.ObjectModel;
using static WK.Libraries.SharpClipboardNS.SharpClipboard;

namespace ClipboardWizard.ViewModel
{
    public class WizardViewModel
    {
        public ObservableCollection<SnippetViewModel> Snippets { get; set; } = new();

        public bool Recording { get; set; }

        private SnippetViewModel _activeSnippet;

        public WizardViewModel()
        {
            SnippetManager.LoadSnippets()
                .ConvertAll(snippet => new SnippetViewModel(snippet, State.Inactive))
                .ForEach(vm => Snippets.Add(vm));

            App.ClipboardMonitor.ClipboardChanged += ClipboardManager_ClipboardChanged;
            App.SnippetDeleted += App_SnippetDeleted;
        }

        private void App_SnippetDeleted(object sender, App.SnippetDeletedEventArgs e)
        {
            SnippetViewModel snippetViewModel = e.SnippetViewModel;
            _ = Snippets.Remove(snippetViewModel);

            if (_activeSnippet == snippetViewModel)
            {
                _activeSnippet = null;
            }
        }

        private void ClipboardManager_ClipboardChanged(object sender, ClipboardChangedEventArgs e)
        {
            if (e.ContentType != ContentTypes.Text)
            {
                return;
            }

            string content = e.Content.ToString();
            if (string.IsNullOrWhiteSpace(content))
            {
                if (_activeSnippet == null)
                {
                    return;
                }

                _activeSnippet.State = State.Inactive;
                _activeSnippet = null;
            }

            if (_activeSnippet != null)
            {
                if (_activeSnippet.Snippet.Content.Equals(content, StringComparison.Ordinal))
                {
                    return;
                }

                _activeSnippet.State = State.Inactive;
                _activeSnippet = null;
            }

            foreach (SnippetViewModel snippet in Snippets)
            {
                if (snippet.Snippet.Content.Equals(content, StringComparison.Ordinal))
                {
                    snippet.State = State.Active;
                    _activeSnippet = snippet;
                    break;
                }
            }

            if (_activeSnippet == null && Recording)
            {
                Snippet snippet = new Snippet() { Content = content };
                SnippetManager.SaveSnippet(snippet);

                _activeSnippet = new(snippet, State.Active);
                Snippets.Add(_activeSnippet);
            }
        }
    }
}