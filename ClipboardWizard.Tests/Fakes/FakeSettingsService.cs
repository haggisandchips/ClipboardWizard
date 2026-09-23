using ClipboardWizard.Model;
using ClipboardWizard.Service;

namespace ClipboardWizard.Tests.Fakes
{
    internal class FakeSettingsService : ISettingsService
    {
        public FirestoreCredentials? Saved { get; private set; }

        public FirestoreCredentials? Load() => Saved;

        public void Save(FirestoreCredentials settings) => Saved = settings;
    }
}
