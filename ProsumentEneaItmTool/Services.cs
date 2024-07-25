using System.IO.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using ProsumentEneaItmTool.Model.Calculations;
using ProsumentEneaItmTool.Model.DataBase;
using ProsumentEneaItmTool.Model.ImportSource;
using ProsumentEneaItmTool.UI;

namespace ProsumentEneaItmTool
{
    internal static class Services
    {
        public static ServiceCollection SetAppModules(this ServiceCollection services)
        {
            services.AddTransient<MainWindowVM>();
            services.AddTransient(s => new MainWindow() { DataContext = s.GetService<MainWindowVM>() });

            services.AddDbContext<DataContext>();
            services.AddSingleton<IDataContext>((s) => s.GetService<DataContext>()!);
            services.AddTransient<IEnergyDataExtractor, EnergyDataExtractor>();
            services.AddTransient<IEnergyDataUpdater, EnergyDataUpdater>();

            services.AddSingleton<IFileSystem>((s) => new FileSystem());
            services.AddTransient<IFileEneaCsvLoader, FileEneaCsvLoader>();
            services.AddSingleton<IPowerCalculation, PowerCalculation>();

            return services;
        }
    }
}
