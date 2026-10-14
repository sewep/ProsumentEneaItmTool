using System.IO.Abstractions;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ProsumentEneaItmTool.Model.DataBase;
using ProsumentEneaItmTool.UI;

namespace ProsumentEneaItmTool
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var dataBasePath = DataBaseLocation.GetDefaultPath();

            var serviceProvider = new ServiceCollection()
                .SetAppModules(dataBasePath)
                .BuildServiceProvider();

            try
            {
                DataBaseLocation.Prepare(serviceProvider.GetRequiredService<IFileSystem>(), dataBasePath, DataBaseLocation.GetLegacyFolders());

                using var dataContext = serviceProvider.GetRequiredService<IDbContextFactory<DataContext>>().CreateDbContext();
                dataContext.Database.Migrate();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Nie udało się otworzyć bazy danych:\r\n{dataBasePath}\r\n\r\n{ex}",
                    "Błąd",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                Shutdown(1);
                return;
            }

            serviceProvider.GetRequiredService<MainWindow>().Show();
        }
    }
}
