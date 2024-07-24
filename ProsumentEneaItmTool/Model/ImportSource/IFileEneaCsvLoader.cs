using ProsumentEneaItmTool.Domain;

namespace ProsumentEneaItmTool.Model.ImportSource
{
    internal interface IFileEneaCsvLoader
    {
        IEnumerable<ImportFileRecord> Load();
    }
}