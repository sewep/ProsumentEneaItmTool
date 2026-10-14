namespace ProsumentEneaItmTool.Model.ImportSource
{
    internal interface IFileEneaCsvLoader
    {
        /// <summary>
        /// Asks the user for a file and parses it. Returns null when the user cancels.
        /// </summary>
        ParseResult? Load();
    }
}