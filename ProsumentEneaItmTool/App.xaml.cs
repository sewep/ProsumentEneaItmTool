using System.Windows;
using Microsoft.Extensions.DependencyInjection;
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

            var serviceProvider = new ServiceCollection()
                .SetAppModules()
                .BuildServiceProvider();

            serviceProvider.GetRequiredService<MainWindow>().Show();
        }
    }
}
