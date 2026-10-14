using Microsoft.EntityFrameworkCore;
using ProsumentEneaItmTool.Domain;

namespace ProsumentEneaItmTool.Model.DataBase
{
    public class DataContext : DbContext, IDataContext
    {
        private readonly string _dbFile = "datastorage.sqlite";

        public DataContext()
        {
            Database.Migrate();
        }

        public DataContext(DbContextOptions<DataContext> options) : base(options)
        {
            Database.Migrate();
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlite($"Data Source={_dbFile}");
            }
        }

        public DbSet<ImportFileRecord> ImportedRecords => Set<ImportFileRecord>();
    }
}
