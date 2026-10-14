using System.Globalization;
using System.IO.Abstractions;
using System.Windows;
using System.Windows.Markup;
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
        private static readonly CultureInfo _appCulture = new("pl-PL");

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Polish number and date formats regardless of the Windows settings, also in bindings.
            CultureInfo.DefaultThreadCurrentCulture = _appCulture;
            CultureInfo.DefaultThreadCurrentUICulture = _appCulture;
            CultureInfo.CurrentCulture = _appCulture;
            CultureInfo.CurrentUICulture = _appCulture;
            FrameworkElement.LanguageProperty.OverrideMetadata(
                typeof(FrameworkElement),
                new FrameworkPropertyMetadata(XmlLanguage.GetLanguage(_appCulture.IetfLanguageTag)));

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
