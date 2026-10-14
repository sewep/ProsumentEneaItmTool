using System.IO.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ProsumentEneaItmTool.Model.Calculations;
using ProsumentEneaItmTool.Model.DataBase;
using ProsumentEneaItmTool.Model.ImportSource;
using ProsumentEneaItmTool.UI;

namespace ProsumentEneaItmTool
{
    internal static class Services
    {
        public static ServiceCollection SetAppModules(this ServiceCollection services, string dataBasePath)
        {
            services.AddTransient<MainWindowVM>();
            services.AddTransient(s => new MainWindow() { DataContext = s.GetService<MainWindowVM>() });

            services.AddDbContextFactory<DataContext>(options => options.UseSqlite($"Data Source={dataBasePath}"));
            services.AddTransient<IEnergyDataExtractor, EnergyDataExtractor>();
            services.AddTransient<IEnergyDataUpdater, EnergyDataUpdater>();

            services.AddSingleton<IFileSystem>((s) => new FileSystem());
            services.AddTransient<IFileEneaCsvLoader, FileEneaCsvLoader>();
            services.AddSingleton<IPowerCalculation, PowerCalculation>();

            return services;
        }
    }
}
