using Microsoft.EntityFrameworkCore;
using ProsumentEneaItmTool.Domain;

namespace ProsumentEneaItmTool.Model.DataBase
{
    public class DataContext : DbContext
    {
        private readonly string _dbFile = "datastorage.sqlite";

        public DataContext()
        {
            Database.Migrate();
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite($"Data Source={_dbFile}");
        }

        public DbSet<ImportFileRecord> ImportedRecords => Set<ImportFileRecord>();
    }
}
