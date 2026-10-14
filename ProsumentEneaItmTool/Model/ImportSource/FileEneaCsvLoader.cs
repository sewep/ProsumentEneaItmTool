using System.IO.Abstractions;
using Microsoft.Win32;

namespace ProsumentEneaItmTool.Model.ImportSource
{
    internal class FileEneaCsvLoader : IFileEneaCsvLoader
    {
        private readonly IFileSystem _fileSystem;

        public FileEneaCsvLoader(IFileSystem fileSystem)
        {
            _fileSystem = fileSystem;
        }

        public ParseResult? Load()
        {
            OpenFileDialog openFileDialog = new()
            {
                Filter = "Excel csv (*.csv)|*.csv|All files (*.*)|*.*",
                FilterIndex = 1,
                RestoreDirectory = true
            };


            if (openFileDialog.ShowDialog() == true)
            {
                var fileContent = EneaCsvFileParser.Decode(_fileSystem.File.ReadAllBytes(openFileDialog.FileName));

                return EneaCsvFileParser.Parse(fileContent);
            }

            return null;
        }
    }
}
