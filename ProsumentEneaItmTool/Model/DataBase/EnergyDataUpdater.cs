using Microsoft.EntityFrameworkCore;
using ProsumentEneaItmTool.Domain;

namespace ProsumentEneaItmTool.Model.DataBase
{
    internal class EnergyDataUpdater : IEnergyDataUpdater
    {
        private readonly IDbContextFactory<DataContext> _dataContextFactory;

        public EnergyDataUpdater(IDbContextFactory<DataContext> dataContextFactory)
        {
            _dataContextFactory = dataContextFactory;
        }

        public async Task AddOrUpdateDataAsync(List<ImportFileRecord> records)
        {
            if (records.Count == 0)
            {
                return;
            }

            await using var dataContext = await _dataContextFactory.CreateDbContextAsync();

            var importedDates = records.Select(x => x.Date).Distinct().ToList();

            await using var transaction = await dataContext.Database.BeginTransactionAsync();

            // Records from the imported file replace the stored ones for the same hours.
            await dataContext.ImportedRecords
                .Where(x => importedDates.Contains(x.Date))
                .ExecuteDeleteAsync();

            await dataContext.ImportedRecords.AddRangeAsync(records);
            await dataContext.SaveChangesAsync();

            await transaction.CommitAsync();
        }

        public async Task ClearAllDataAsync()
        {
            await using var dataContext = await _dataContextFactory.CreateDbContextAsync();

            await dataContext.ImportedRecords.ExecuteDeleteAsync();
        }
    }
}
