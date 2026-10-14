using Microsoft.EntityFrameworkCore;
using ProsumentEneaItmTool.Domain;

namespace ProsumentEneaItmTool.Model.DataBase
{
    internal class EnergyDataExtractor : IEnergyDataExtractor
    {
        private readonly IDbContextFactory<DataContext> _dataContextFactory;

        public EnergyDataExtractor(IDbContextFactory<DataContext> dataContextFactory)
        {
            _dataContextFactory = dataContextFactory;
        }

        public async Task<List<ImportFileRecord>> GetAllItemsAsync()
        {
            await using var dataContext = await _dataContextFactory.CreateDbContextAsync();

            return await dataContext.ImportedRecords.AsNoTracking().OrderBy(record => record.Date).ToListAsync();
        }

        public async Task<List<ImportFileRecord>> GetItemsByDateRangesAsync(DateTime dateFrom, DateTime dateTo)
        {
            await using var dataContext = await _dataContextFactory.CreateDbContextAsync();

            dateFrom = new DateTime(dateFrom.Year, dateFrom.Month, dateFrom.Day, 0, 0, 0);
            dateTo = new DateTime(dateTo.Year, dateTo.Month, dateTo.Day, 23, 59, 59, 999);

            return await dataContext.ImportedRecords
                .AsNoTracking()
                .Where(record => record.Date >= dateFrom && record.Date <= dateTo)
                .OrderBy(record => record.Date)
                .ToListAsync();
        }

        public async Task<(DateTime, DateTime)> GetDateRangesAsync()
        {
            await using var dataContext = await _dataContextFactory.CreateDbContextAsync();

            if (!await dataContext.ImportedRecords.AnyAsync())
            {
                var today = DateTime.Today;
                var from = new DateTime(today.Year, today.Month, today.Day, 0, 0, 0);
                var to = new DateTime(today.Year, today.Month, today.Day, 23, 59, 59, 999);
                return (from, to);
            }

            var dateFrom = await dataContext.ImportedRecords.MinAsync(x => x.Date);
            var dateTo = await dataContext.ImportedRecords.MaxAsync(x => x.Date);

            return (dateFrom, dateTo);
        }
    }
}
