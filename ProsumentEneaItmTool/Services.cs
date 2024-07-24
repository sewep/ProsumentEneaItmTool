using System.IO.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using ProsumentEneaItmTool.Model.DataBase;
using ProsumentEneaItmTool.Model.ImportSource;
using ProsumentEneaItmTool.UI;

namespace ProsumentEneaItmTool
{
    internal static class Services
    {
        public static ServiceCollection SetAppModules(this ServiceCollection services)
        {
            services.AddSingleton<IFileSystem>((s) => new FileSystem());
            services.AddTransient<MainWindowVM>();
            services.AddTransient(s => new MainWindow() { DataContext = s.GetService<MainWindowVM>() });

            services.AddDbContext<DataContext>();

            services.AddTransient<IFileEneaCsvLoader, FileEneaCsvLoader>();

            return services;
        }
    }
}
