using Ookii.Dialogs.Wpf;
using System.IO;
using System.Windows;
using Microsoft.Win32;

namespace VCenterMigrationTool_WPF_UI.Utilities
{
    public class DialogService : IDialogService
    {
        public string? ShowFolderBrowserDialog(string description, string selectedPath)
        {
            try
            {
                var dialog = new VistaFolderBrowserDialog
                {
                    Description = description,
                    UseDescriptionForTitle = true,
                    SelectedPath = selectedPath
                };

                if (dialog.ShowDialog() == true)
                {
                    return dialog.SelectedPath;
                }
            }
            catch
            {
                // Fallback to basic method
                return SelectFolderFallback(description, selectedPath);
            }

            return null;
        }

        private static string? SelectFolderFallback(string description, string selectedPath)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                ValidateNames = false,
                CheckFileExists = false,
                CheckPathExists = true,
                FileName = "Select Folder",
                Title = description,
                InitialDirectory = selectedPath
            };

            if (dialog.ShowDialog() == true)
            {
                return Path.GetDirectoryName(dialog.FileName);
            }

            return null;
        }

        public void ShowMessage(string message, string title, MessageBoxButton button, MessageBoxImage icon)
        {
            MessageBox.Show(message, title, button, icon);
        }
    }
}