using System.Windows;
using VCenterMigrationTool_WPF_UI.Utilities;
using VCenterMigrationTool_WPF_UI.ViewModels;
using VCenterMigrationTool_WPF_UI.Models;

namespace VCenterMigrationTool_WPF_UI
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Create the ViewModels
            LogViewModel logViewModel = new LogViewModel();
            LogServiceAdapter logService = new LogServiceAdapter(logViewModel);
            PowerShellManager psManager = new PowerShellManager(logService);
            MainViewModel mainViewModel = new MainViewModel(psManager, logService);

            // Create the Main Window and set the DataContext
            MainWindow mainWindow = new MainWindow();
            mainWindow.DataContext = mainViewModel;

            // Show the Main Window
            mainWindow.Show();
        }
    }
}
