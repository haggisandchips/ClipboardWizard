using ClipboardWizard.Model;
using ClipboardWizard.Service.Firestore;
using ClipboardWizard.Tests.Fakes;
using ClipboardWizard.ViewModel;

namespace ClipboardWizard.Tests.ViewModel
{
    public class SettingsViewModelTests
    {
        [Fact]
        public void IsConfigured_StartsTrue_ForAnUnchangedExistingKey()
        {
            SettingsViewModel viewModel = new(new FirestoreCredentials { ServiceAccountJson = "{}" }, new FakeFirestoreSyncService());

            Assert.True(viewModel.IsConfigured);
        }

        [Fact]
        public void EditingServiceAccountJson_ClearsIsConfigured()
        {
            SettingsViewModel viewModel = new(new FirestoreCredentials { ServiceAccountJson = "{}" }, new FakeFirestoreSyncService());

            viewModel.ServiceAccountJson = "{\"project_id\":\"changed\"}";

            Assert.False(viewModel.IsConfigured);
        }

        [Fact]
        public async Task ConfirmCategorySelectionAsync_NothingUnchecked_SkipsVerificationAndConfigures()
        {
            FakeFirestoreSyncService firestoreSyncService = new();
            SettingsViewModel viewModel = new(new FirestoreCredentials(), firestoreSyncService);
            viewModel.ServiceAccountJson = "{\"project_id\":\"p\"}";

            bool result = await viewModel.ConfirmCategorySelectionAsync(new HashSet<string>());

            Assert.True(result);
            Assert.True(viewModel.IsConfigured);
            Assert.Empty(viewModel.HiddenCategorySyncIds);
        }

        [Fact]
        public async Task ConfirmCategorySelectionAsync_VerificationSucceeds_StagesHiddenSetAndConfigures()
        {
            FakeFirestoreSyncService firestoreSyncService = new() { NextVerifyResult = FirestoreTestResult.Ok("ok") };
            SettingsViewModel viewModel = new(new FirestoreCredentials(), firestoreSyncService);
            viewModel.ServiceAccountJson = "{\"project_id\":\"p\"}";

            bool result = await viewModel.ConfirmCategorySelectionAsync(new HashSet<string> { "cat-1" });

            Assert.True(result);
            Assert.True(viewModel.IsConfigured);
            Assert.True(new HashSet<string> { "cat-1" }.SetEquals(viewModel.HiddenCategorySyncIds));
        }

        [Fact]
        public async Task ConfirmCategorySelectionAsync_VerificationFails_LeavesIsConfiguredFalse()
        {
            FakeFirestoreSyncService firestoreSyncService = new() { NextVerifyResult = FirestoreTestResult.Failed("needs an index") };
            SettingsViewModel viewModel = new(new FirestoreCredentials(), firestoreSyncService);
            viewModel.ServiceAccountJson = "{\"project_id\":\"p\"}";

            bool result = await viewModel.ConfirmCategorySelectionAsync(new HashSet<string> { "cat-1" });

            Assert.False(result);
            Assert.False(viewModel.IsConfigured);
            Assert.Contains("needs an index", viewModel.TestResultMessage);
        }

        [Fact]
        public async Task ConfirmCategorySelectionAsync_VerificationFails_StillStagesHiddenSet_SoRetryDoesntNeedReselecting()
        {
            FakeFirestoreSyncService firestoreSyncService = new() { NextVerifyResult = FirestoreTestResult.Failed("needs an index") };
            SettingsViewModel viewModel = new(new FirestoreCredentials(), firestoreSyncService);
            viewModel.ServiceAccountJson = "{\"project_id\":\"p\"}";

            await viewModel.ConfirmCategorySelectionAsync(new HashSet<string> { "cat-1" });

            Assert.True(new HashSet<string> { "cat-1" }.SetEquals(viewModel.HiddenCategorySyncIds));
        }

        [Theory]
        [InlineData(null, false)]
        [InlineData("", false)]
        [InlineData("{}", false)]
        [InlineData("{\"project_id\":\"p\"}", true)]
        public void CanSave_RequiresValidKeyAndConfigured(string? serviceAccountJson, bool isValid)
        {
            SettingsViewModel viewModel = new(new FirestoreCredentials { ServiceAccountJson = serviceAccountJson }, new FakeFirestoreSyncService());

            Assert.Equal(isValid, viewModel.IsValid);
            // IsConfigured starts true (unchanged key), so CanSave tracks IsValid alone here.
            Assert.Equal(isValid, viewModel.CanSave);
        }
    }
}
