using System.Windows;

namespace ProsumentEneaItmTool.UI.Utils
{
    internal interface IUserDialogs
    {
        void ShowError(string message);
        void ShowWarning(string message);
        bool Confirm(string message);
    }

    internal class MessageBoxUserDialogs : IUserDialogs
    {
        private const string AppTitle = "Prosument Enea Itm Tool";

        public void ShowError(string message)
        {
            MessageBox.Show(message, $"{AppTitle} - błąd", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        public void ShowWarning(string message)
        {
            MessageBox.Show(message, $"{AppTitle} - uwagi", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        public bool Confirm(string message)
        {
            return MessageBox.Show(message, AppTitle, MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No) == MessageBoxResult.Yes;
        }
    }
}
