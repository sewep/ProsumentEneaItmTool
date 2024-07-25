using Microsoft.EntityFrameworkCore;
using ProsumentEneaItmTool.Domain;

namespace ProsumentEneaItmTool.Model.DataBase
{
    public interface IDataContext
    {
        DbSet<ImportFileRecord> ImportedRecords { get; }

        Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default);

        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}