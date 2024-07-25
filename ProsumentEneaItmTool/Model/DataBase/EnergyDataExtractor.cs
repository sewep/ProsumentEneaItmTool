using Microsoft.EntityFrameworkCore;
using ProsumentEneaItmTool.Domain;

namespace ProsumentEneaItmTool.Model.DataBase
{
    internal class EnergyDataExtractor : IEnergyDataExtractor
    {
        private readonly IDataContext _dataContext;

        public EnergyDataExtractor(IDataContext dataContext)
        {
            _dataContext = dataContext;
        }

        public async Task<List<ImportFileRecord>> GetAllItemsAsync()
        {
            return await _dataContext.ImportedRecords.ToListAsync();
        }

        public async Task<List<ImportFileRecord>> GetItemsByDateRangesAsync(DateTime dateFrom, DateTime dateTo)
        {
            dateFrom = new DateTime(dateFrom.Year, dateFrom.Month, dateFrom.Day, 0, 0, 0);
            dateTo = new DateTime(dateTo.Year, dateTo.Month, dateTo.Day, 23, 59, 59, 999);

            return await _dataContext.ImportedRecords
                .Where(record => record.Date >= dateFrom && record.Date <= dateTo)
                .ToListAsync();
        }

        public async Task<(DateTime, DateTime)> GetDateRangesAsync()
        {
            if (!await _dataContext.ImportedRecords.AnyAsync())
            {
                var today = DateTime.Today;
                var from = new DateTime(today.Year, today.Month, today.Day, 0, 0, 0);
                var to = new DateTime(today.Year, today.Month, today.Day, 23, 59, 59, 999);
                return (from, to);
            }

            var dateFrom = await _dataContext.ImportedRecords.MinAsync(x => x.Date);
            var dateTo = await _dataContext.ImportedRecords.MaxAsync(x => x.Date);

            return (dateFrom, dateTo);
        }
    }
}
