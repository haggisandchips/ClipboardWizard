namespace ClipboardWizard.Service.Firestore
{
    /// <summary>Result of SettingsView's "Test Connection" button - shown inline, not via MessageBox (see CommandErrorHandler's convention for command exceptions vs. inline validation feedback).</summary>
    public class FirestoreTestResult
    {
        public bool Success { get; }

        public string Message { get; }

        public FirestoreTestResult(bool success, string message)
        {
            Success = success;
            Message = message;
        }

        public static FirestoreTestResult Ok(string message) => new(true, message);

        public static FirestoreTestResult Failed(string message) => new(false, message);
    }
}
