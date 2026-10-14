using System.IO.Abstractions;
using System.Net.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ProsumentEneaItmTool.Model.Calculations;
using ProsumentEneaItmTool.Model.DataBase;
using ProsumentEneaItmTool.Model.Export;
using ProsumentEneaItmTool.Model.ImportSource;
using ProsumentEneaItmTool.Model.NetBilling;
using ProsumentEneaItmTool.UI;
using ProsumentEneaItmTool.UI.Utils;

namespace ProsumentEneaItmTool
{
    internal static class Services
    {
        public static ServiceCollection SetAppModules(this ServiceCollection services, string dataBasePath)
        {
            services.AddTransient<MainWindowVM>();
            services.AddTransient(s => new MainWindow() { DataContext = s.GetService<MainWindowVM>() });
            services.AddSingleton<IUserDialogs, WpfUserDialogs>();

            services.AddDbContextFactory<DataContext>(options => options.UseSqlite($"Data Source={dataBasePath}"));
            services.AddTransient<IEnergyDataExtractor, EnergyDataExtractor>();
            services.AddTransient<IEnergyDataUpdater, EnergyDataUpdater>();

            services.AddSingleton<IFileSystem>((s) => new FileSystem());
            services.AddTransient<IFileEneaCsvLoader, FileEneaCsvLoader>();
            services.AddTransient<ICsvExporter, CsvExporter>();
            services.AddSingleton<IPowerCalculation, PowerCalculation>();

            services.AddSingleton(_ => new HttpClient { Timeout = TimeSpan.FromSeconds(30) });
            services.AddTransient<IRcePriceSource, PseRceClient>();
            services.AddTransient<IMarketPriceStore, MarketPriceStore>();
            services.AddTransient<IRcePriceUpdater, RcePriceUpdater>();
            services.AddTransient<NetBillingVM>();

            return services;
        }
    }
}
