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
            records.ForEach(async x =>
            {
                var toRemove = await _dataContext.ImportedRecords.Where(y => y.Date.Equals(x.Date)).ToListAsync();
                _dataContext.ImportedRecords.RemoveRange(toRemove);
            });

            await _dataContext.ImportedRecords.AddRangeAsync(records);
            await _dataContext.SaveChangesAsync();
        }
    }
}
