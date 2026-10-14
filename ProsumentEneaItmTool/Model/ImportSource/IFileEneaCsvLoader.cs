namespace ProsumentEneaItmTool.Model.ImportSource
{
    internal interface IFileEneaCsvLoader
    {
        /// <summary>
        /// Reads and parses the given Enea report files. Problems are returned as warnings.
        /// </summary>
        ParseResult Load(IReadOnlyCollection<string> filePaths);
    }
}
