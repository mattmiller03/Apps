using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;
using Newtonsoft.Json;
using Wpf.Ui.Mvvm;  // for ApplicationThemeManager
using Ookii.Dialogs.Wpf;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Net.NetworkInformation;
using System.Security;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using VCenterMigrationTool_WPF_UI.Infrastructure;
using VCenterMigrationTool_WPF_UI.Models;
using VCenterMigrationTool_WPF_UI.Utilities;
using VCenterMigrationTool_WPF_UI.ViewModels;

namespace VCenterMigrationTool_WPF_UI.Views.Pages
{
    public partial class MainWindow : Window
    {
        private readonly Logger _logger;
        private readonly PowerShellManager _psManager;
        private readonly DashBoardViewModel _viewModel;
        private CancellationTokenSource _cts;

        public MainWindow()
        {
            InitializeComponent();
            _cts = new CancellationTokenSource();

            // Resolve services from DI container
            var app = (App)Application.Current;
            _logger = app.Services.GetRequiredService<Logger>();
            _psManager = app.Services.GetRequiredService<PowerShellManager>();
            _viewModel = app.Services.GetRequiredService<DashBoardViewModel>();
        }

        protected override void OnContentRendered(EventArgs e)
        {
            base.OnContentRendered(e);

            // Wire up logger to UI
            _logger.MessageWritten += (msg, level) =>
            {
                Dispatcher.Invoke(() =>
                {
                    LogTextBox.AppendText($"[{DateTime.Now:HH:mm:ss}][{level}] {msg}{Environment.NewLine}");
                    LogTextBox.ScrollToEnd();
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

        private async void ConnectVCenter_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Validate input
                if (string.IsNullOrWhiteSpace(_viewModel.SourceVCenter) ||
                    string.IsNullOrWhiteSpace(_viewModel.Username) ||
                    string.IsNullOrWhiteSpace(_viewModel.Password))
                {
                    ShowMessage("Please enter all connection credentials", "Validation Error", MessageBoxImage.Warning);
                    return;
                }

                _logger.Info($"Attempting to connect to vCenter: {_viewModel.SourceVCenter}");
                UpdateStatus("Connecting to vCenter...");

                // Secure password handling
                var securePassword = new SecureString();
                foreach (char c in _viewModel.Password) securePassword.AppendChar(c);
                securePassword.MakeReadOnly();

                // Execute connection script
                var result = await _psManager.ExecuteScriptAsync(
                    "Test-Connectivity_7149aebb-0.ps1",
                    _cts.Token,
                    ("Server", _viewModel.SourceVCenter),
                    ("Username", _viewModel.Username),
                    ("Password", securePassword),
                    ("ConnectionType", "Source"));

                var connectionResult = JsonConvert.DeserializeObject<ConnectionResult>(result);

                if (connectionResult.IsConnected)
                {
                    _viewModel.IsSourceConnected = true;
                    _viewModel.SourceVersion = connectionResult.Version;

                    _logger.Info($"Successfully connected to vCenter {_viewModel.SourceVCenter} (v{connectionResult.Version})");
                    UpdateStatus($"Connected to {_viewModel.SourceVCenter}");

                    // Load inventory after successful connection
                    _ = Task.Run(async () =>
                    {
                        await LoadInitialInventory();
                    });
                }
                else
                {
                    _logger.Error($"Connection failed: {connectionResult.ErrorMessage}");
                    ShowMessage($"Connection failed: {connectionResult.ErrorMessage}", "Connection Error", MessageBoxImage.Error);
                    UpdateStatus("Connection failed");
                }
            }
            catch (OperationCanceledException)
            {
                _logger.Info("Connection attempt cancelled");
                UpdateStatus("Connection cancelled");
            }
            catch (Exception ex)
            {
                _logger.Error($"Connection error: {ex.Message}");
                ShowMessage($"Connection error: {ex.Message}", "Error", MessageBoxImage.Error);
                UpdateStatus("Connection error");
            }
        }
        #endregion

        #region Core Methods
        private async Task RunBackupTask(string operationName, Func<string, CancellationToken, Task> backupOperation)
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
            Dispatcher.Invoke(() => CurrentBackupTaskText.Text = message);
        }

        private void ShowMessage(string text, string caption, MessageBoxImage icon)
        {
            Dispatcher.Invoke(() =>
                MessageBox.Show(this, text, caption, MessageBoxButton.OK, icon));
        }
        #endregion

        private void DisconnectAll_Click(object sender, RoutedEventArgs e)
        {
            _logger.Info("Disconnecting all connections...");
        }

        private void ValidatePrerequisites_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _logger.Info("Starting comprehensive prerequisite validation...");
                UpdateStatus("Validating environment...");

                var result = await _psManager.ExecuteScriptAsync(
                    "Validate-Prerequisites.ps1",
                    _cts.Token,
                    ("SourceVCenter", _viewModel.SourceVCenter),
                    ("DestinationVCenter", _viewModel.DestinationVCenter),
                    ("CheckStorage", _viewModel.ValidateStorage),
                    ("CheckNetworking", _viewModel.ValidateNetworking));

                var validation = JsonConvert.DeserializeObject<PrerequisiteResult>(result);

                // Update UI with validation results
                _viewModel.PrerequisiteResults = validation;

                if (validation.AllChecksPassed)
                {
                    _logger.Info("All prerequisites validated successfully");
                    ShowMessage("Validation passed - Environment is ready for migration",
                               "Validation Complete", MessageBoxImage.Information);
                }
                else
                {
                    _logger.Warn($"Validation completed with {validation.FailedChecks.Count} issues");
                    ShowMessage($"Validation completed with {validation.FailedChecks.Count} issues\n" +
                              string.Join("\n", validation.FailedChecks.Select(f => $"- {f}")),
                              "Validation Warnings", MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Prerequisite validation failed: {ex.Message}");
                ShowMessage($"Validation failed: {ex.Message}", "Error", MessageBoxImage.Error);
            }
            finally
            {
                UpdateStatus("Ready");
            }
        }

        private void TestConnectivity_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _logger.Info("Initiating comprehensive connectivity tests");
                UpdateStatus("Testing connections...");

                var result = await _psManager.ExecuteScriptAsync(
                    "Test-Connectivity_7149aebb-0.ps1",
                    _cts.Token,
                    ("SourceEndpoint", _viewModel.SourceVCenter),
                    ("DestinationEndpoint", _viewModel.DestinationVCenter),
                    ("TestPorts", "443,902,903"),
                    ("TimeoutSeconds", 10));

                var connectivity = JsonConvert.DeserializeObject<ConnectivityTestResult>(result);

                // Update UI with connection test results
                _viewModel.ConnectivityResults = connectivity;

                StringBuilder sb = new StringBuilder();
                sb.AppendLine("Connectivity Test Results:");
                sb.AppendLine($"- Ping: {(connectivity.PingSuccess ? "✔" : "✖")}");
                sb.AppendLine($"- API: {(connectivity.ApiAccess ? "✔" : "✖")}");
                sb.AppendLine($"- Ports: {string.Join(", ", connectivity.OpenPorts)}");

                _logger.Info($"Connectivity results:\n{sb}");
                ShowMessage(sb.ToString(), "Connectivity Test Complete",
                           connectivity.AllTestsPassed ? MessageBoxImage.Information : MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                _logger.Error($"Connectivity test failed: {ex.Message}");
                ShowMessage($"Connection test failed: {ex.Message}", "Error", MessageBoxImage.Error);
            }
            finally
            {
                UpdateStatus("Ready");
            }
        }

        private void PowerCLIConfig_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _logger.Info("Configuring PowerCLI settings...");
                UpdateStatus("Configuring PowerCLI...");

                await _psManager.ExecuteScriptAsync(
                    "Configure-PowerCLI.ps1",
                    _cts.Token,
                    ("InvalidCertAction", "Ignore"),
                    ("CEIPSetting", "false"),
                    ("DefaultServerMode", "Multiple"));

                _logger.Info("PowerCLI configuration updated");
                UpdateStatus("PowerCLI configured");
                ShowMessage("PowerCLI settings configured successfully", "Success", MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                _logger.Error($"PowerCLI configuration failed: {ex.Message}");
                UpdateStatus("Configuration failed");
                ShowMessage($"PowerCLI configuration failed: {ex.Message}", "Error", MessageBoxImage.Error);
            }
        }

        private void ConnectionSettings_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _logger.Info("Opening advanced connection settings");

                var settingsWindow = new ConnectionSettingsWindow(_viewModel.ConnectionSettings)
                {
                    Owner = this,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner
                };

                if (settingsWindow.ShowDialog() == true)
                {
                    // Save settings to configuration
                    _viewModel.ConnectionSettings = settingsWindow.Settings;
                    _configManager.SaveConnectionSettings(_viewModel.ConnectionSettings);

                    // Apply timeout setting immediately
                    _psManager.CommandTimeout = _viewModel.ConnectionSettings.CommandTimeoutSeconds;

                    _logger.Info("Connection settings updated and saved");
                    ShowMessage("Connection settings saved successfully", "Settings Updated", MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Connection settings error: {ex.Message}");
                ShowMessage($"Failed to update settings: {ex.Message}", "Error", MessageBoxImage.Error);
            }
        }

        private void LoadConnectionProfile_Click(object sender, RoutedEventArgs e)
        {

        }

        private void SaveCurrentSettings_Click(object sender, RoutedEventArgs e)
        {

        }

        private void ExportConfig_Click(object sender, RoutedEventArgs e)
        {

        }

        private void ImportConfig_Click(object sender, RoutedEventArgs e)
        {

        }

        private void Documentation_Click(object sender, RoutedEventArgs e)
        {

        }

        private void ConnectSource_Click(object sender, RoutedEventArgs e)
        {

        }

        private void ConnectDest_Click(object sender, RoutedEventArgs e)
        {

        }

        private void TestConnection_Click(object sender, RoutedEventArgs e)
        {

        }

        private void SelectAllBackup_Click(object sender, RoutedEventArgs e)
        {

        }

        private void ClearAllBackup_Click(object sender, RoutedEventArgs e)
        {

        }

        private void BrowseBackup_Click(object sender, RoutedEventArgs e)
        {

        }

        private void ExecuteBackup_Click(object sender, RoutedEventArgs e)
        {

        }

        private void CancelBackup_Click(object sender, RoutedEventArgs e)
        {

        }

        private void ScopeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void MigrateHost_Click(object sender, RoutedEventArgs e)
        {

        }

        private void MigrateVM_Click(object sender, RoutedEventArgs e)
        {

        }

        private void MigrateCluster_Click(object sender, RoutedEventArgs e)
        {

        }

        private void BatchMigrate_Click(object sender, RoutedEventArgs e)
        {

        }

        private void Rollback_Click(object sender, RoutedEventArgs e)
        {

        }

        private void PauseMigration_Click(object sender, RoutedEventArgs e)
        {

        }

        private void StopMigration_Click(object sender, RoutedEventArgs e)
        {

        }

        private void ExportReport_Click(object sender, RoutedEventArgs e)
        {

        }

        private void ClearLog_Click(object sender, RoutedEventArgs e)
        {

        }

        private void SaveLog_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Configure default log directory with rotation/archive subfolder
                string logsDirectory = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    "vCenterMigrationTool",
                    "Logs");

                string archiveDirectory = Path.Combine(logsDirectory, "Archive");

                // Ensure directories exist
                Directory.CreateDirectory(logsDirectory);
                Directory.CreateDirectory(archiveDirectory);

                // Configure save dialog
                var saveDialog = new SaveFileDialog
                {
                    InitialDirectory = logsDirectory,
                    Filter = "Log Files (*.log)|*.log|Text Files (*.txt)|*.txt|All Files (*.*)|*.*",
                    DefaultExt = ".log",
                    FileName = $"MigrationLog_{DateTime.Now:yyyyMMdd_HHmmss}",
                    Title = "Save Log File with Rotation",
                    AddExtension = true,
                    OverwritePrompt = true
                };

                if (saveDialog.ShowDialog() == true)
                {
                    // Perform log rotation if file exists
                    if (File.Exists(saveDialog.FileName))
                    {
                        RotateLogFile(saveDialog.FileName, archiveDirectory);
                    }

                    // Save current log content
                    File.WriteAllText(saveDialog.FileName, LogTextBox.Text);

                    // Log and notify success
                    _logger.Info($"Logs saved to: {saveDialog.FileName}");
                    ShowMessage($"Logs successfully saved to:{Environment.NewLine}{saveDialog.FileName}",
                               "Save Successful",
                               MessageBoxImage.Information);

                    // Optional: Auto-clean old archives (keeps last 30 days)
                    CleanOldArchives(archiveDirectory, daysToKeep: 30);
                }
                else
                {
                    _logger.Info("Log save operation cancelled by user");
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to save logs: {ex.Message}");
                ShowMessage($"Failed to save logs:{Environment.NewLine}{ex.Message}",
                           "Error",
                           MessageBoxImage.Error);
            }
        }

        // Helper method for log rotation
        private void RotateLogFile(string currentLogPath, string archiveDirectory)
        {
            try
            {
                string fileName = Path.GetFileNameWithoutExtension(currentLogPath);
                string extension = Path.GetExtension(currentLogPath);
                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");

                string archivePath = Path.Combine(
                    archiveDirectory,
                    $"{fileName}_ARCHIVED_{timestamp}{extension}");

                File.Move(currentLogPath, archivePath);
                _logger.Info($"Rotated previous log to: {archivePath}");
            }
            catch (Exception ex)
            {
                _logger.Error($"Log rotation failed: {ex.Message}");
                throw;
            }
        }

        // Helper method to clean old archives
        private void CleanOldArchives(string archiveDirectory, int daysToKeep)
        {
            try
            {
                var cutoffDate = DateTime.Now.AddDays(-daysToKeep);
                var oldFiles = Directory.GetFiles(archiveDirectory)
                    .Select(f => new FileInfo(f))
                    .Where(f => f.LastWriteTime < cutoffDate)
                    .ToList();

                foreach (var file in oldFiles)
                {
                    try
                    {
                        file.Delete();
                        _logger.Info($"Cleaned old log archive: {file.FullName}");
                    }
                    catch (Exception ex)
                    {
                        _logger.Warn($"Could not delete old log {file.Name}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Warn($"Archive cleanup failed: {ex.Message}");
            }
        }

        private void SelectAllValidation_Click(object sender, RoutedEventArgs e)
        {

        }

        private void ClearAllValidation_Click(object sender, RoutedEventArgs e)
        {

        }

        private void RunValidation_Click(object sender, RoutedEventArgs e)
        {

        }

        private void BackupIdentityCheckBox_Checked(object sender, RoutedEventArgs e)
        {

        }

        public class PrerequisiteResult
        {
            public bool AllChecksPassed { get; set; }
            public List<string> PassedChecks { get; set; } = new List<string>();
            public List<string> FailedChecks { get; set; } = new List<string>();
            public Dictionary<string, string> DetailedResults { get; set; } = new Dictionary<string, string>();
        }

        public class ConnectivityTestResult
        {
            public bool PingSuccess { get; set; }
            public bool ApiAccess { get; set; }
            public List<int> OpenPorts { get; set; } = new List<int>();
            public bool AllTestsPassed => PingSuccess && ApiAccess && OpenPorts.Count >= 3;
        }

        public class PowerCLIConfigResult
        {
            public string CertificateAction { get; set; }
            public bool CEIPEnabled { get; set; }
            public string ServerMode { get; set; }
            public string LogLevel { get; set; }
            public DateTime ConfiguredAt { get; set; } = DateTime.Now;
        }

        private async Task LoadInitialInventory()
        {
            try
            {
                _logger.Info("Loading initial inventory...");
                UpdateStatus("Loading inventory...");

                var result = await _psManager.ExecuteScriptAsync(
                    "VC-ResourcePool-Export_230b2aca-4.ps1",
                    _cts.Token,
                    ("Action", "ListOnly"));

                var inventory = JsonConvert.DeserializeObject<InventorySummary>(result);
                _viewModel.Inventory = inventory;

                _logger.Info($"Loaded {inventory.ResourcePools.Count} resource pools, {inventory.VMs.Count} VMs");
                UpdateStatus("Inventory loaded");
            }
            catch (Exception ex)
            {
                _logger.Error($"Inventory load failed: {ex.Message}");
                UpdateStatus("Inventory load failed");
            }
        }

        public class ConnectionTestResult
        {
            public string ConnectionType { get; set; }
            public string Server { get; set; }
            public bool ConnectionStatus { get; set; }
            public string Version { get; set; }
            public bool NetworkAccess { get; set; }
            public bool ApiAccess { get; set; }
            public string Timestamp { get; set; }
            public string ErrorMessage { get; set; }
        }

        public class InventorySummary
        {
            public List<ResourcePoolInfo> ResourcePools { get; set; } = new List<ResourcePoolInfo>();
            public List<VMInfo> VMs { get; set; } = new List<VMInfo>();
            public List<HostInfo> Hosts { get; set; } = new List<HostInfo>();
        }

        public class ResourcePoolInfo
        {
            public string Name { get; set; }
            public string Path { get; set; }
            public double CpuLimitMHz { get; set; }
            public double MemoryLimitGB { get; set; }
        }

        public class VMInfo
        {
            public string Name { get; set; }
            public string PowerState { get; set; }
            public int NumCpu { get; set; }
            public double MemoryGB { get; set; }
        }
    }
}
