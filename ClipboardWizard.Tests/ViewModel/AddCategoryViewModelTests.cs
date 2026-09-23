using ClipboardWizard.Model;
using ClipboardWizard.Service.Firestore;
using ClipboardWizard.Tests.Fakes;
using ClipboardWizard.ViewModel;

namespace ClipboardWizard.Tests.ViewModel
{
    public class AddCategoryViewModelTests
    {
        [Theory]
        [InlineData(null, false)]
        [InlineData("", false)]
        [InlineData("   ", false)]
        [InlineData("Work", true)]
        public void IsValid_ReflectsWhetherNameIsBlank(string? name, bool expected)
        {
            AddCategoryViewModel viewModel = new(new FakeFirestoreStatusProvider())
            {
                Name = name!
            };

            Assert.Equal(expected, viewModel.IsValid);
        }

        [Fact]
        public void NeedsFirebaseSetup_FalseWhenNotShared()
        {
            AddCategoryViewModel viewModel = new(new FakeFirestoreStatusProvider())
            {
                Shared = false
            };

            Assert.False(viewModel.NeedsFirebaseSetup);
        }

        [Fact]
        public void NeedsFirebaseSetup_TrueWhenSharedButNotConnected()
        {
            AddCategoryViewModel viewModel = new(new FakeFirestoreStatusProvider())
            {
                Shared = true
            };

            Assert.True(viewModel.NeedsFirebaseSetup);
        }

        [Fact]
        public void NeedsFirebaseSetup_FalseWhenSharedAndConnected()
        {
            FakeFirestoreStatusProvider status = new() { State = FirestoreConnectionState.Connected };
            AddCategoryViewModel viewModel = new(status)
            {
                Shared = true
            };

            Assert.False(viewModel.NeedsFirebaseSetup);
        }

        [Fact]
        public void EditConstructor_PopulatesNameAndShared()
        {
            Category category = new() { Name = "Work", Shared = true };

            AddCategoryViewModel viewModel = new(category, new FakeFirestoreStatusProvider());

            Assert.False(viewModel.IsNew);
            Assert.Equal("Work", viewModel.Name);
            Assert.True(viewModel.Shared);
        }
    }
}
