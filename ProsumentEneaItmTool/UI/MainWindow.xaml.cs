using System.IO;
using System.Windows;

namespace ProsumentEneaItmTool.UI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private static string[] DroppedCsvFiles(DragEventArgs e)
        {
            if (e.Data.GetData(DataFormats.FileDrop) is not string[] files)
            {
                return [];
            }

            return files
                .Where(x => string.Equals(Path.GetExtension(x), ".csv", StringComparison.OrdinalIgnoreCase))
                .ToArray();
        }

        private void Window_DragOver(object sender, DragEventArgs e)
        {
            var canImport = DataContext is MainWindowVM vm
                && DroppedCsvFiles(e).Length > 0
                && vm.ImportFilesCommand.CanExecute(null);

            e.Effects = canImport ? DragDropEffects.Copy : DragDropEffects.None;
            e.Handled = true;
        }

        private void Window_Drop(object sender, DragEventArgs e)
        {
            if (DataContext is MainWindowVM vm)
            {
                var files = DroppedCsvFiles(e);
                if (files.Length > 0)
                {
                    vm.ImportFilesCommand.Execute(files);
                }
            }

            e.Handled = true;
        }
    }
}
