using System.IO.Abstractions.TestingHelpers;
using ProsumentEneaItmTool.Model.DataBase;

namespace ProsumentEneaItmTool.Tests.Model.DataBase
{
    internal class DataBaseLocationTests
    {
        private const string DataBasePath = @"C:\Users\test\AppData\Local\ProsumentEneaItmTool\datastorage.sqlite";
        private const string InstallFolder = @"C:\Program Files\ProsumentEneaItmTool";
        private const string WorkFolder = @"C:\work";

        [Test]
        public void Prepare_CreatesFolder_WhenNoDatabaseAnywhere()
        {
            var fileSystem = new MockFileSystem();

            var copied = DataBaseLocation.Prepare(fileSystem, DataBasePath, [InstallFolder]);

            Assert.Multiple(() =>
            {
                Assert.That(copied, Is.Null);
                Assert.That(fileSystem.Directory.Exists(Path.GetDirectoryName(DataBasePath)), Is.True);
                Assert.That(fileSystem.File.Exists(DataBasePath), Is.False);
            });
        }

        [Test]
        public void Prepare_CopiesLegacyDatabaseWithWal()
        {
            var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
            {
                [Path.Combine(InstallFolder, "datastorage.sqlite")] = new("legacy"),
                [Path.Combine(InstallFolder, "datastorage.sqlite-wal")] = new("wal"),
            });

            var copied = DataBaseLocation.Prepare(fileSystem, DataBasePath, [InstallFolder]);

            Assert.Multiple(() =>
            {
                Assert.That(copied, Is.EqualTo(Path.Combine(InstallFolder, "datastorage.sqlite")));
                Assert.That(fileSystem.File.ReadAllText(DataBasePath), Is.EqualTo("legacy"));
                Assert.That(fileSystem.File.ReadAllText(DataBasePath + "-wal"), Is.EqualTo("wal"));
                // The old file is kept as a backup.
                Assert.That(fileSystem.File.Exists(Path.Combine(InstallFolder, "datastorage.sqlite")), Is.True);
            });
        }

        [Test]
        public void Prepare_PicksNewestLegacyDatabase()
        {
            var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
            {
                [Path.Combine(InstallFolder, "datastorage.sqlite")] = new("old") { LastWriteTime = new DateTime(2024, 1, 1) },
                [Path.Combine(WorkFolder, "datastorage.sqlite")] = new("new") { LastWriteTime = new DateTime(2025, 1, 1) },
            });

            DataBaseLocation.Prepare(fileSystem, DataBasePath, [InstallFolder, WorkFolder]);

            Assert.That(fileSystem.File.ReadAllText(DataBasePath), Is.EqualTo("new"));
        }

        [Test]
        public void Prepare_DoesNotOverwriteExistingDatabase()
        {
            var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
            {
                [DataBasePath] = new("current"),
                [Path.Combine(InstallFolder, "datastorage.sqlite")] = new("legacy"),
            });

            var copied = DataBaseLocation.Prepare(fileSystem, DataBasePath, [InstallFolder]);

            Assert.Multiple(() =>
            {
                Assert.That(copied, Is.Null);
                Assert.That(fileSystem.File.ReadAllText(DataBasePath), Is.EqualTo("current"));
            });
        }
    }
}
