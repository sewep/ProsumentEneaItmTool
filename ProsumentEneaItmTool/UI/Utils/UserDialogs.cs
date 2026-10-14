using System.Windows;
using Microsoft.Win32;

namespace ProsumentEneaItmTool.UI.Utils
{
    internal interface IUserDialogs
    {
        void ShowError(string message);
        void ShowWarning(string message);
        void ShowInformation(string message);
        bool Confirm(string message);

        /// <summary>
        /// Returns selected files or null when the user cancels.
        /// </summary>
        string[]? AskForCsvFilesToOpen();

        /// <summary>
        /// Returns the selected path or null when the user cancels.
        /// </summary>
        string? AskForCsvFileToSave(string defaultFileName);
    }

    internal class WpfUserDialogs : IUserDialogs
    {
        private const string AppTitle = "Prosument Enea Itm Tool";
        private const string CsvFilter = "Pliki CSV (*.csv)|*.csv|Wszystkie pliki (*.*)|*.*";

        public void ShowError(string message)
        {
            MessageBox.Show(message, $"{AppTitle} - błąd", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        public void ShowWarning(string message)
        {
            MessageBox.Show(message, $"{AppTitle} - uwagi", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        public void ShowInformation(string message)
        {
            MessageBox.Show(message, AppTitle, MessageBoxButton.OK, MessageBoxImage.Information);
        }

        public bool Confirm(string message)
        {
            return MessageBox.Show(message, AppTitle, MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No) == MessageBoxResult.Yes;
        }

        public string[]? AskForCsvFilesToOpen()
        {
            OpenFileDialog dialog = new()
            {
                Filter = CsvFilter,
                Multiselect = true,
                RestoreDirectory = true,
                Title = "Wybierz pliki CSV z Enea"
            };

            return dialog.ShowDialog() == true ? dialog.FileNames : null;
        }

        public string? AskForCsvFileToSave(string defaultFileName)
        {
            SaveFileDialog dialog = new()
            {
                Filter = CsvFilter,
                FileName = defaultFileName,
                DefaultExt = ".csv",
                RestoreDirectory = true,
                Title = "Eksport wyliczeń do CSV"
            };

            return dialog.ShowDialog() == true ? dialog.FileName : null;
        }
    }
}
