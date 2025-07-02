using System;
using System.Windows;
using VCenterMigrationTool_WPF_UI.Utilities;
using VCenterMigrationTool_WPF_UI.ViewModels;
using VCenterMigrationTool_WPF_UI.Views;

namespace VCenterMigrationTool_WPF_UI
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            try
            {
                // Initialize core services
                var logViewModel = new LogViewModel();
                var logService = new LogServiceAdapter(logViewModel);
                var psManager = new PowerShellManager(logService);

                // Create main ViewModel
                var mainViewModel = new MainViewModel(psManager, logService);

                // Create and show main window programmatically
                var mainWindow = new MainWindow(mainViewModel);
                mainWindow.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Application startup failed: {ex.Message}", "Startup Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown();
            }
        }
    }
}
