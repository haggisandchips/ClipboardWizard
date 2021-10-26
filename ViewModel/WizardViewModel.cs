using ClipboardWizard.Model;
using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using static WK.Libraries.SharpClipboardNS.SharpClipboard;

namespace ClipboardWizard.ViewModel
{
    public class WizardViewModel
    {
        public ObservableCollection<SnippetModel> Snippets { get; set; } = new();

        public bool Recording { get; set; }

        private SnippetModel activeSnippet;

        public WizardViewModel()
        {
            App.ClipboardMonitor.ClipboardChanged += ClipboardManager_ClipboardChanged;
        }

        private void ClipboardManager_ClipboardChanged(object sender, ClipboardChangedEventArgs e)
        {
            if (e.ContentType != ContentTypes.Text) { return; }

            string content = e.Content.ToString();
            if (string.IsNullOrWhiteSpace(content))
            {
                if (activeSnippet != null)
                {
                    activeSnippet.State = State.Inactive;
                    activeSnippet = null;
                }

                return;
            }

            if (activeSnippet != null)
            {
                if (activeSnippet.Content.Equals(content, StringComparison.Ordinal)) { return; }

                activeSnippet.State = State.Inactive;
                activeSnippet = null;
            }

            foreach (SnippetModel snippet in Snippets)
            {
                if (snippet.Content.Equals(content, StringComparison.Ordinal))
                {
                    snippet.State = State.Active;
                    activeSnippet = snippet;
                    break;
                }
            }

            if (activeSnippet == null)
            {
                activeSnippet = new(content, State.Active);
                Snippets.Add(activeSnippet);
            }
        }
    }
}
