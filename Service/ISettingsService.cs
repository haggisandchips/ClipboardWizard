using ClipboardWizard.Model;

namespace ClipboardWizard.Service
{
    public interface ISettingsService
    {
        /// <summary>Returns the last-saved Firestore credentials, or null if none have been saved yet.</summary>
        FirestoreCredentials Load();

        void Save(FirestoreCredentials settings);
    }
}
