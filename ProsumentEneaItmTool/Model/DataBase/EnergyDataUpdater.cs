using Microsoft.EntityFrameworkCore;
using ProsumentEneaItmTool.Domain;

namespace ProsumentEneaItmTool.Model.DataBase
{
    internal class EnergyDataUpdater : IEnergyDataUpdater
    {
        private readonly IDataContext _dataContext;

        public EnergyDataUpdater(IDataContext dataContext)
        {
            _dataContext = dataContext;
        }

        public async Task AddOrUpdateDataAsync(List<ImportFileRecord> records)
        {
            if (records.Count == 0)
            {
                return;
            }

            var importedDates = records.Select(x => x.Date).Distinct().ToList();

            await using var transaction = await _dataContext.Database.BeginTransactionAsync();

            // Records from the imported file replace the stored ones for the same hours.
            await _dataContext.ImportedRecords
                .Where(x => importedDates.Contains(x.Date))
                .ExecuteDeleteAsync();

            await _dataContext.ImportedRecords.AddRangeAsync(records);
            await _dataContext.SaveChangesAsync();

            await transaction.CommitAsync();
        }

        public async Task ClearAllDataAsync()
        {
            await _dataContext.ImportedRecords.ExecuteDeleteAsync();
            await _dataContext.SaveChangesAsync();
        }
    }
}
