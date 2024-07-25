using ProsumentEneaItmTool.Domain;

namespace ProsumentEneaItmTool.Model.DataBase
{
    internal interface IEnergyDataUpdater
    {
        Task AddOrUpdateDataAsync(List<ImportFileRecord> records);
    }
}