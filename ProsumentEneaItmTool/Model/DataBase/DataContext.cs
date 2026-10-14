using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using ProsumentEneaItmTool.Domain;

namespace ProsumentEneaItmTool.Model.DataBase
{
    public class DataContext : DbContext
    {
        public DataContext(DbContextOptions<DataContext> options) : base(options)
        {
        }

        public DbSet<ImportFileRecord> ImportedRecords => Set<ImportFileRecord>();
    }

    /// <summary>
    /// Used by EF Core tools (dotnet ef migrations).
    /// </summary>
    internal class DataContextDesignTimeFactory : IDesignTimeDbContextFactory<DataContext>
    {
        public DataContext CreateDbContext(string[] args)
        {
            var options = new DbContextOptionsBuilder<DataContext>()
                .UseSqlite($"Data Source={DataBaseLocation.FileName}")
                .Options;

            return new DataContext(options);
        }
    }
}
