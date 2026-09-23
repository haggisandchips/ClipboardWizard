using ClipboardWizard.Model;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace ClipboardWizard.Service
{
    public class SettingsService : ISettingsService
    {
        private readonly string _filePath;

        public SettingsService(string filePath)
        {
            _filePath = filePath;
        }

        /// <summary>On-disk shape - the service-account JSON is DPAPI-protected before it ever reaches disk, since it's a full-admin credential.</summary>
        private class PersistedSettings
        {
            public string ProtectedServiceAccountJsonBase64 { get; set; }
        }

        public FirestoreCredentials Load()
        {
            try
            {
                if (!File.Exists(_filePath))
                {
                    return null;
                }

                PersistedSettings persisted = JsonSerializer.Deserialize<PersistedSettings>(File.ReadAllText(_filePath));
                if (string.IsNullOrEmpty(persisted?.ProtectedServiceAccountJsonBase64))
                {
                    return null;
                }

                byte[] protectedBytes = Convert.FromBase64String(persisted.ProtectedServiceAccountJsonBase64);
                byte[] plainBytes = ProtectedData.Unprotect(protectedBytes, null, DataProtectionScope.CurrentUser);

                return new FirestoreCredentials { ServiceAccountJson = Encoding.UTF8.GetString(plainBytes) };
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException or FormatException or CryptographicException)
            {
                // Best-effort, like the rest of this app's local persistence - a corrupt or
                // unreadable settings file (including one DPAPI can no longer decrypt, e.g.
                // after a Windows user profile reset) just means Settings comes up empty, not
                // a startup failure.
                Logger.LogError(nameof(Load), ex);
                return null;
            }
        }

        public void Save(FirestoreCredentials settings)
        {
            try
            {
                string directory = Path.GetDirectoryName(_filePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                byte[] plainBytes = Encoding.UTF8.GetBytes(settings.ServiceAccountJson ?? string.Empty);
                byte[] protectedBytes = ProtectedData.Protect(plainBytes, null, DataProtectionScope.CurrentUser);

                PersistedSettings persisted = new() { ProtectedServiceAccountJsonBase64 = Convert.ToBase64String(protectedBytes) };
                File.WriteAllText(_filePath, JsonSerializer.Serialize(persisted));
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or CryptographicException)
            {
                Logger.LogError(nameof(Save), ex);
            }
        }
    }
}
