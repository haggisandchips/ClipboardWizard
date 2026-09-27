using ClipboardWizard.Model;
using ClipboardWizard.Service;

namespace ClipboardWizard.Tests.Service
{
    public class SettingsServiceTests : IDisposable
    {
        private readonly string _filePath;
        private readonly SettingsService _service;

        public SettingsServiceTests()
        {
            _filePath = Path.Combine(Path.GetTempPath(), $"ClipboardWizardSettingsTests_{Guid.NewGuid():N}.json");
            _service = new SettingsService(_filePath);
        }

        [Fact]
        public void Load_ReturnsNull_WhenNoFileExists()
        {
            Assert.Null(_service.Load());
        }

        [Fact]
        public void SaveAndLoad_RoundTripsServiceAccountJsonViaDpapi()
        {
            const string json = /*lang=json,strict*/ "{\"type\":\"service_account\",\"project_id\":\"example-project\"}";
            _service.Save(new FirestoreCredentials { ServiceAccountJson = json });

            FirestoreCredentials loaded = _service.Load();

            Assert.Equal(json, loaded.ServiceAccountJson);
            Assert.Equal("example-project", loaded.ProjectId);
        }

        [Fact]
        public void SaveAndLoad_RoundTripsHiddenCategorySyncIds()
        {
            const string json = /*lang=json,strict*/ "{\"type\":\"service_account\",\"project_id\":\"example-project\"}";
            _service.Save(new FirestoreCredentials { ServiceAccountJson = json, HiddenCategorySyncIds = new List<string> { "cat-1", "cat-2" } });

            FirestoreCredentials loaded = _service.Load();

            Assert.Equal(new[] { "cat-1", "cat-2" }, loaded.HiddenCategorySyncIds);
        }

        [Fact]
        public void Load_OnFileFromBeforeHiddenCategorySyncIdsExisted_DefaultsToEmptyList()
        {
            const string json = /*lang=json,strict*/ "{\"type\":\"service_account\",\"project_id\":\"example-project\"}";
            // Simulates an upgrade: save without ever touching HiddenCategorySyncIds, then strip
            // it from the on-disk JSON entirely (older builds never wrote the property at all).
            _service.Save(new FirestoreCredentials { ServiceAccountJson = json });
            string onDisk = File.ReadAllText(_filePath);
            File.WriteAllText(_filePath, onDisk.Replace("\"HiddenCategorySyncIds\":[]", "\"HiddenCategorySyncIds\":null"));

            FirestoreCredentials loaded = _service.Load();

            Assert.Empty(loaded.HiddenCategorySyncIds);
        }

        [Fact]
        public void Save_DoesNotWriteServiceAccountJsonInPlaintext()
        {
            const string json = "{\"type\":\"service_account\",\"project_id\":\"super-secret-project\"}";
            _service.Save(new FirestoreCredentials { ServiceAccountJson = json });

            string onDisk = File.ReadAllText(_filePath);

            Assert.DoesNotContain("super-secret-project", onDisk);
        }

        public void Dispose()
        {
            try
            {
                File.Delete(_filePath);
            }
            catch (IOException)
            {
            }
        }
    }
}
