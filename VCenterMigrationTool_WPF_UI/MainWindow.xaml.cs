using Microsoft.Extensions.DependencyInjection;
using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using VCenterMigrationTool_WPF_UI.Infrastructure;
using VCenterMigrationTool_WPF_UI.Utilities;
using VCenterMigrationTool_WPF_UI.ViewModels;

namespace VCenterMigrationTool_WPF_UI
{
    public partial class MainWindow : Window
    {
        private readonly Logger _logger;
        private readonly PowerShellManager _psManager;
        private readonly MainViewModel _viewModel;
        private CancellationTokenSource _cts;

        public MainWindow()
        {
            InitializeComponent();
            _cts = new CancellationTokenSource();

            // Resolve services from DI container
            var app = (App)Application.Current;
            _logger = app.Services.GetRequiredService<Logger>();
            _psManager = app.Services.GetRequiredService<PowerShellManager>();
            _viewModel = app.Services.GetRequiredService<MainViewModel>();
        }

        protected override void OnContentRendered(EventArgs e)
        {
            base.OnContentRendered(e);

            // Wire up logger to UI
            _logger.MessageWritten += (msg, level) =>
            {
                Dispatcher.Invoke(() =>
                {
                    LogBox.AppendText($"[{DateTime.Now:HH:mm:ss}][{level}] {msg}{Environment.NewLine}");
                    LogBox.ScrollToEnd();
                });
            };

            _logger.Info("Application initialized and ready");
        }

        protected override void OnClosed(EventArgs e)
        {
            _cts?.Cancel();
            _cts?.Dispose();
            base.OnClosed(e);
        }

        #region Event Handlers
        private void Exit_Click(object sender, RoutedEventArgs e) => Close();

        private void About_Click(object sender, RoutedEventArgs e) =>
            MessageBox.Show("vCenter Migration Tool v1.0", "About",
                          MessageBoxButton.OK, MessageBoxImage.Information);

        private async void BackupUsersBtn_Click(object sender, RoutedEventArgs e) =>
            await RunBackupTask("Backing up Users & Groups", _psManager.BackupUsersAndGroupsAsync);

        private async void BackupRolesBtn_Click(object sender, RoutedEventArgs e) =>
            await RunBackupTask("Backing up Roles", _psManager.BackupRolesAsync);

        private async void BackupPermsBtn_Click(object sender, RoutedEventArgs e) =>
            await RunBackupTask("Backing up Permissions", _psManager.BackupPermissionsAsync);
        #endregion

        #region Core Methods
        private async Task RunBackupTask(string operationName,
                                       Func<string, CancellationToken, Task> backupOperation)
        {
            try
            {
                _cts = new CancellationTokenSource(); // Reset cancellation token
                var backupPath = PickBackupFolder();

                if (string.IsNullOrEmpty(backupPath))
                {
                    _logger.Warn($"{operationName} cancelled - no folder selected");
                    return;
                }

                UpdateStatus($"{operationName} in progress...");
                _logger.Info($"Starting {operationName}");

                await backupOperation(backupPath, _cts.Token);

                _logger.Info($"{operationName} completed successfully");
                ShowMessage($"{operationName} completed!", "Success", MessageBoxImage.Information);
            }
            catch (OperationCanceledException)
            {
                _logger.Warn($"{operationName} was cancelled");
                ShowMessage($"{operationName} was cancelled", "Cancelled", MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                _logger.Error($"{operationName} failed: {ex.Message}");
                ShowMessage($"{operationName} failed: {ex.Message}", "Error", MessageBoxImage.Error);
            }
            finally
            {
                UpdateStatus("Ready");
            }
        }
        #endregion

        #region Helpers
        private string? PickBackupFolder()
        {
            using var dialog = new CommonOpenFileDialog
            {
                IsFolderPicker = true,
                Title = "Select Backup Folder"
            };

            return dialog.ShowDialog() == CommonFileDialogResult.Ok
                ? dialog.FileName
                : null;
        }

        private void UpdateStatus(string message)
        {
            Dispatcher.Invoke(() => StatusBar.Text = message);
        }

        private void ShowMessage(string text, string caption, MessageBoxImage icon)
        {
            Dispatcher.Invoke(() =>
                MessageBox.Show(this, text, caption, MessageBoxButton.OK, icon));
        }
        #endregion
    }
}
