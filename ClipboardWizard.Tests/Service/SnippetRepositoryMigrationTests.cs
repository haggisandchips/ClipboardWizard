using ClipboardWizard.Model;
using ClipboardWizard.Service;
using SQLite;

namespace ClipboardWizard.Tests.Service
{
    /// <summary>
    /// Verifies that opening a database created before Type/ImageData existed doesn't lose
    /// data or throw - sqlite-net-pcl adds missing columns via ALTER TABLE automatically, and
    /// existing rows should come back as SnippetType.Text with no ImageData, which is what
    /// they always were.
    /// </summary>
    public class SnippetRepositoryMigrationTests : IDisposable
    {
        private readonly string _databasePath;

        public SnippetRepositoryMigrationTests()
        {
            _databasePath = Path.Combine(Path.GetTempPath(), $"ClipboardWizardMigrationTests_{Guid.NewGuid():N}.db");
        }

        [Fact]
        public async Task OpeningAPreImageSchemaDatabase_DefaultsExistingRowsToTextType()
        {
            using (SQLiteConnection legacyConnection = new(_databasePath))
            {
                legacyConnection.Execute(
                    "CREATE TABLE Snippet (Id INTEGER PRIMARY KEY AUTOINCREMENT, \"Order\" INTEGER, Description TEXT, Content TEXT, Locked INTEGER)");
                legacyConnection.Execute(
                    "INSERT INTO Snippet (\"Order\", Description, Content, Locked) VALUES (0, NULL, 'legacy content', 0)");
            }

            SnippetRepository repository = new(_databasePath);
            Snippet loaded = Assert.Single(await repository.LoadSnippetsAsync());

            Assert.Equal("legacy content", loaded.Content);
            Assert.Equal(SnippetType.Text, loaded.Type);
            Assert.Null(loaded.ImageData);
        }

        public void Dispose()
        {
            try
            {
                File.Delete(_databasePath);
            }
            catch (IOException)
            {
            }
        }
    }
}
