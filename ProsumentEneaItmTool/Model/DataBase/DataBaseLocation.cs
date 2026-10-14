using System.IO;
using System.IO.Abstractions;

namespace ProsumentEneaItmTool.Model.DataBase
{
    internal static class DataBaseLocation
    {
        public const string FileName = "datastorage.sqlite";
        private const string AppFolderName = "ProsumentEneaItmTool";

        /// <summary>
        /// Database in the user's local application data, writable also when the app is installed in Program Files.
        /// </summary>
        public static string GetDefaultPath()
        {
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            return Path.Combine(localAppData, AppFolderName, FileName);
        }

        /// <summary>
        /// Folders where older versions kept the database (relative path, so the working directory).
        /// </summary>
        public static IEnumerable<string> GetLegacyFolders()
        {
            return new[] { Environment.CurrentDirectory, AppContext.BaseDirectory }
                .Select(Path.GetFullPath)
                .Distinct(StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Creates the database folder and, when there is no database yet, copies the newest one found in legacy folders.
        /// </summary>
        /// <returns>Path of the copied legacy database or null.</returns>
        public static string? Prepare(IFileSystem fileSystem, string dataBasePath, IEnumerable<string> legacyFolders)
        {
            var folder = fileSystem.Path.GetDirectoryName(dataBasePath)!;
            fileSystem.Directory.CreateDirectory(folder);

            if (fileSystem.File.Exists(dataBasePath))
            {
                return null;
            }

            var legacyFile = legacyFolders
                .Select(x => fileSystem.Path.Combine(x, FileName))
                .Where(x => !string.Equals(fileSystem.Path.GetFullPath(x), fileSystem.Path.GetFullPath(dataBasePath), StringComparison.OrdinalIgnoreCase))
                .Where(fileSystem.File.Exists)
                .OrderByDescending(fileSystem.File.GetLastWriteTimeUtc)
                .FirstOrDefault();

            if (legacyFile is null)
            {
                return null;
            }

            fileSystem.File.Copy(legacyFile, dataBasePath);

            // Not checkpointed changes of a database in WAL mode.
            var legacyWal = legacyFile + "-wal";
            if (fileSystem.File.Exists(legacyWal))
            {
                fileSystem.File.Copy(legacyWal, dataBasePath + "-wal");
            }

            return legacyFile;
        }
    }
}
