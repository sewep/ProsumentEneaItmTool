using ProsumentEneaItmTool.Domain;

namespace ProsumentEneaItmTool.Model.DataBase
{
    internal interface IEnergyDataExtractor
    {
        Task<List<ImportFileRecord>> GetAllItemsAsync();
        Task<(DateTime, DateTime)> GetDateRangesAsync();
        Task<List<ImportFileRecord>> GetItemsByDateRangesAsync(DateTime dateFrom, DateTime dateTo);
    }
}