using System;
using System.Collections.Generic;
using System.Text.Json;

namespace ClipboardWizard.Model
{
    /// <summary>
    /// User-supplied credentials for the app's own Firebase/Firestore project. Named
    /// FirestoreCredentials rather than FirestoreSettings to avoid colliding with
    /// Google.Cloud.Firestore's own FirestoreSettings type (gRPC call/retry configuration).
    /// </summary>
    public class FirestoreCredentials
    {
        /// <summary>The raw GCP service-account key, exactly as downloaded from the Firebase/GCP console. A full-admin credential - see SettingsService for how it's protected at rest.</summary>
        public string ServiceAccountJson { get; set; }

        /// <summary>
        /// SyncIds of Shared categories this device has chosen not to sync/show (see Settings'
        /// Configure flow) - stored as the *unchecked* set so a brand-new remote category shows
        /// up automatically without this device having to opt into it. Not secret, unlike
        /// ServiceAccountJson - see SettingsService for why it's persisted unencrypted.
        /// </summary>
        public List<string> HiddenCategorySyncIds { get; set; } = new();

        /// <summary>The service account's project id, parsed out of ServiceAccountJson for display - null if ServiceAccountJson isn't valid/complete JSON.</summary>
        public string ProjectId
        {
            get
            {
                try
                {
                    using JsonDocument document = JsonDocument.Parse(ServiceAccountJson ?? string.Empty);
                    return document.RootElement.TryGetProperty("project_id", out JsonElement projectId)
                        ? projectId.GetString()
                        : null;
                }
                catch (JsonException)
                {
                    return null;
                }
            }
        }
    }
}
