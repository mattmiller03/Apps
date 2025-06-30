using System.Windows;
using VCenterMigrationTool_WPF_UI.Models;
using VCenterMigrationTool_WPF_UI.Utilities;
using VCenterMigrationTool_WPF_UI.ViewModels;

namespace VCenterMigrationTool_WPF_UI.Utilities
{
    public interface IDialogService
    {
        string? ShowFolderBrowserDialog(string description, string selectedPath);
        void ShowMessage(string message, string title, MessageBoxButton button, MessageBoxImage icon);
    }
}