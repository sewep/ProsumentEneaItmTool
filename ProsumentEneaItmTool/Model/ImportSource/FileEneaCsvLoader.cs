using System.IO;
using System.IO.Abstractions;
using ProsumentEneaItmTool.Domain;

namespace ProsumentEneaItmTool.Model.ImportSource
{
    internal class FileEneaCsvLoader : IFileEneaCsvLoader
    {
        private readonly IFileSystem _fileSystem;

        public FileEneaCsvLoader(IFileSystem fileSystem)
        {
            _fileSystem = fileSystem;
        }

        public ParseResult Load(IReadOnlyCollection<string> filePaths)
        {
            var records = new List<ImportFileRecord>();
            var warnings = new List<string>();

            foreach (var filePath in filePaths)
            {
                var fileName = _fileSystem.Path.GetFileName(filePath);
                var prefix = filePaths.Count > 1 ? $"{fileName}: " : string.Empty;

                ParseResult fileResult;
                try
                {
                    var fileContent = EneaCsvFileParser.Decode(_fileSystem.File.ReadAllBytes(filePath));
                    fileResult = EneaCsvFileParser.Parse(fileContent);
                }
                catch (Exception e) when (e is IOException or UnauthorizedAccessException)
                {
                    warnings.Add($"{fileName}: nie udało się odczytać pliku ({e.Message})");
                    continue;
                }

                if (fileResult.Records.Count == 0)
                {
                    warnings.Add($"{fileName}: nie znaleziono danych pomiarowych");
                }

                // Overlapping files: hours from a later file replace the same hours from earlier ones,
                // as during an import to the database. A repeated hour within one file (DST change) is kept.
                var fileDates = fileResult.Records.Select(x => x.Date).ToHashSet();
                records.RemoveAll(x => fileDates.Contains(x.Date));
                records.AddRange(fileResult.Records);

                warnings.AddRange(fileResult.Warnings.Select(x => prefix + x));
            }

            return new ParseResult(records, warnings);
        }
    }
}
