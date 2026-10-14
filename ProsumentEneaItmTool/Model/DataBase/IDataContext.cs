using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using ProsumentEneaItmTool.Domain;

namespace ProsumentEneaItmTool.Model.DataBase
{
    public interface IDataContext
    {
        DbSet<ImportFileRecord> ImportedRecords { get; }

        DatabaseFacade Database { get; }

        Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default);

        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}