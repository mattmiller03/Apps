using System.Collections.ObjectModel;
using System.IO;
using System.Net.NetworkInformation;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using Ookii.Dialogs.Wpf;
using VCenterMigrationTool_WPF_UI.Utilities;
using VCenterMigrationTool_WPF_UI.Models;

namespace VCenterMigrationTool_WPF_UI;

public partial class MainWindow : Window
{
    public ObservableCollection<LogEntry> LogEntries { get; set; } = new();
    public ObservableCollection<MigrationTask> MigrationTasks { get; set; } = new();
    public ObservableCollection<ValidationResult> ValidationResults { get; set; } = new();

    private PowerShellRunspaceManager _runspaceManager;
    private VCenterConnectionManager _connectionManager;
    private BackupManager _backupManager;
    private MigrationManager _migrationManager;
    private InventoryManager _inventoryManager;

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private static readonly char[] LineSeparators = new[] { '\r', '\n' };

    private CancellationTokenSource? _backupCancellationTokenSource;
    private bool _isBackupRunning = false;
    private bool _isPowerShellInitialized = false;
    private string _backupPath = @"C:\VCenterBackup";

    public MainWindow()
    {
        InitializeComponent();

        _runspaceManager = new PowerShellRunspaceManager();

        _connectionManager = new VCenterConnectionManager(_runspaceManager);
        _connectionManager.LogMessage += WriteLog;

        _backupManager = new BackupManager(_runspaceManager);
        _backupManager.LogMessage += WriteLog;

        _migrationManager = new MigrationManager(_runspaceManager);
        _migrationManager.LogMessage += WriteLog;

        _inventoryManager = new InventoryManager(_runspaceManager);
        _inventoryManager.LogMessage += WriteLog;

        InitializeCollections();
        InitializeBackupPath();

        Loaded += MainWindow_Loaded;
    }

    private async Task InitializePowerShellAsync()
    {
        LogTextBox.Clear();
        WriteLog("🔧 Initializing PowerShell environment...", "INFO");

        try
        {
            await _connectionManager.InitializeAsync();

            _isPowerShellInitialized = true;

            if (_connectionManager.IsPowerCLIAvailable)
                WriteLog("✅ PowerCLI is available and configured.", "INFO");
            else
                WriteLog("⚠️ PowerCLI is not available; running in simulation mode.", "WARNING");

            ConnectSourceButton.IsEnabled = true;
            ConnectDestinationButton.IsEnabled = true;
            TestConnectionButton.IsEnabled = true;
            DisconnectAllButton.IsEnabled = true;
            ExecuteBackupButton.IsEnabled = true;
        }
        catch (Exception ex)
        {
            WriteLog($"❌ Failed to initialize PowerShell environment: {ex.Message}", "ERROR");
            ConnectSourceButton.IsEnabled = false;
            ConnectDestinationButton.IsEnabled = false;
            TestConnectionButton.IsEnabled = false;
            DisconnectAllButton.IsEnabled = false;
            ExecuteBackupButton.IsEnabled = false;
        }
    }

    #region Logging

    private void WriteLog(string message, string level = "INFO")
    {
        var logEntry = new LogEntry
        {
            Timestamp = DateTime.Now,
            Level = level,
            Message = message
        };

        LogEntries.Add(logEntry);

        if (LogTextBox != null)
        {
            Dispatcher.Invoke(() =>
            {
                try
                {
                    LogTextBox.AppendText($"[{logEntry.Timestamp:HH:mm:ss}] [{logEntry.Level}] {logEntry.Message}\r\n");
                    LogTextBox.ScrollToEnd();
                }
                catch
                {
                    System.Diagnostics.Debug.WriteLine($"[{logEntry.Timestamp:HH:mm:ss}] [{logEntry.Level}] {logEntry.Message}");
                }
            });
        }
        else
        {
            System.Diagnostics.Debug.WriteLine($"[{logEntry.Timestamp:HH:mm:ss}] [{logEntry.Level}] {logEntry.Message}");
        }
    }

    private void InitializeCollections()
    {
        MigrationProgressGrid.ItemsSource = MigrationTasks;
        ValidationResultsGrid.ItemsSource = ValidationResults;
    }

    private void InitializeBackupPath()
    {
        if (BackupPathDisplayText != null)
            BackupPathDisplayText.Text = _backupPath;
    }

    #endregion

    #region Connection Handlers

    private async void ConnectSource_Click(object sender, RoutedEventArgs e)
    {
        if (!_isPowerShellInitialized)
        {
            MessageBox.Show("PowerShell environment is still initializing. Please wait.", "Not Ready", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            ConnectSourceButton.IsEnabled = false;
            ConnectSourceButton.Content = "🔄 Connecting...";

            WriteLog($"🔌 Connecting to source vCenter: {SourceVCenterTextBox.Text}", "INFO");

            var connected = await _connectionManager.ConnectToSourceVCenterAsync(
                SourceVCenterTextBox.Text,
                SourceUsernameTextBox.Text,
                SourcePasswordBox.Password);

            if (connected)
            {
                SourceStatusText.Text = $"✅ Connected to {SourceVCenterTextBox.Text}";
                SourceStatusText.Foreground = System.Windows.Media.Brushes.Green;

                var version = await _connectionManager.GetVCenterVersionAsync("source");
                SourceVersionText.Text = $"Version: {version}";

                WriteLog("✅ Successfully connected to source vCenter", "INFO");
                await UpdateInventoryTreeAsync();

                if (InventoryTreeView.Items.Count > 0 && InventoryTreeView.Items[0] is TreeViewItem rootItem)
                    rootItem.IsExpanded = true;

                UpdateMigrationButtonStates();
            }
            else
            {
                SourceStatusText.Text = "❌ Connection Failed";
                SourceStatusText.Foreground = System.Windows.Media.Brushes.Red;
                WriteLog("❌ Failed to connect to source vCenter", "ERROR");
            }
        }
        catch (Exception ex)
        {
            WriteLog($"❌ Source connection error: {ex.Message}", "ERROR");
            SourceStatusText.Text = "❌ Connection Failed";
            SourceStatusText.Foreground = System.Windows.Media.Brushes.Red;
            MessageBox.Show($"Failed to connect to source vCenter:\n{ex.Message}", "Connection Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            ConnectSourceButton.IsEnabled = true;
            ConnectSourceButton.Content = "🔌 Connect";
        }
    }

    private async void ConnectDest_Click(object sender, RoutedEventArgs e)
    {
        if (!_isPowerShellInitialized)
        {
            MessageBox.Show("PowerShell environment is still initializing. Please wait.", "Not Ready", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            ConnectDestButton.IsEnabled = false;
            ConnectDestButton.Content = "🔄 Connecting...";

            WriteLog($"🔌 Connecting to destination vCenter: {DestVCenterTextBox.Text}", "INFO");

            var connected = await _connectionManager.ConnectToDestinationVCenterAsync(
                DestVCenterTextBox.Text,
                DestUsernameTextBox.Text,
                DestPasswordBox.Password);

            if (connected)
            {
                DestStatusText.Text = $"✅ Connected to {DestVCenterTextBox.Text}";
                DestStatusText.Foreground = System.Windows.Media.Brushes.Green;

                var version = await _connectionManager.GetVCenterVersionAsync("destination");
                DestVersionText.Text = $"Version: {version}";

                WriteLog("✅ Successfully connected to destination vCenter", "INFO");
                UpdateMigrationButtonStates();
            }
            else
            {
                DestStatusText.Text = "❌ Connection Failed";
                DestStatusText.Foreground = System.Windows.Media.Brushes.Red;
                WriteLog("❌ Failed to connect to destination vCenter", "ERROR");
            }
        }
        catch (Exception ex)
        {
            WriteLog($"❌ Destination connection error: {ex.Message}", "ERROR");
            DestStatusText.Text = "❌ Connection Failed";
            DestStatusText.Foreground = System.Windows.Media.Brushes.Red;
            MessageBox.Show($"Failed to connect to destination vCenter:\n{ex.Message}", "Connection Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            ConnectDestButton.IsEnabled = true;
            ConnectDestButton.Content = "🔌 Connect";
        }
    }

    private void DisconnectAll_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            WriteLog("🔌 Disconnecting from all vCenters...", "INFO");
            DisconnectAllButton.IsEnabled = false;
            DisconnectAllButton.Content = "🔄 Disconnecting...";

            _connectionManager.DisconnectAll();

            SourceStatusText.Text = "❌ Not Connected";
            SourceStatusText.Foreground = System.Windows.Media.Brushes.Red;
            SourceVersionText.Text = "Version: Unknown";

            DestStatusText.Text = "❌ Not Connected";
            DestStatusText.Foreground = System.Windows.Media.Brushes.Red;
            DestVersionText.Text = "Version: Unknown";

            InventoryTreeView.Items.Clear();

            UpdateMigrationButtonStates();

            WriteLog("✅ Disconnected from all vCenters", "INFO");
            MessageBox.Show("Successfully disconnected from all vCenter servers.", "Disconnected", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            WriteLog($"❌ Error during disconnect: {ex.Message}", "ERROR");
            MessageBox.Show($"Error during disconnect:\n{ex.Message}", "Disconnect Error", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        finally
        {
            DisconnectAllButton.IsEnabled = true;
            DisconnectAllButton.Content = "🔌 Disconnect All";
        }
    }

    #endregion

    #region Backup Handlers

    private async void ExecuteBackup_Click(object sender, RoutedEventArgs e)
    {
        if (!_connectionManager.IsSourceConnected)
        {
            MessageBox.Show("Please connect to source vCenter first.", "Connection Required", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (string.IsNullOrEmpty(_backupPath))
        {
            MessageBox.Show("Please configure a backup path in the Tools menu first.", "Backup Path Required", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            _isBackupRunning = true;
            _backupCancellationTokenSource = new CancellationTokenSource();

            ExecuteBackupButton.IsEnabled = false;
            CancelBackupButton.IsEnabled = true;
            CancelBackupButton.Visibility = Visibility.Visible;

            WriteLog("🚀 Starting comprehensive backup process...", "INFO");
            WriteLog($"📂 Using backup path: {_backupPath}", "INFO");

            Directory.CreateDirectory(_backupPath);

            var totalTasks = GetSelectedBackupTaskCount();
            var completedTasks = 0;

            if (BackupVDSCheckBox.IsChecked == true)
            {
                CurrentBackupTaskText.Text = "Backing up Virtual Distributed Switches...";
                BackupDetailText.Text = "Initializing VDS backup process...";
                await _backupManager.BackupVDSAsync(_backupPath, _backupCancellationTokenSource.Token, UpdateBackupDetail);
                completedTasks++;
                UpdateBackupProgress(completedTasks, totalTasks);
            }

            if (BackupUsersCheckBox.IsChecked == true)
            {
                CurrentBackupTaskText.Text = "Backing up Users and Groups...";
                BackupDetailText.Text = "Initializing Users and Groups backup...";
                await _backupManager.BackupUsersAndGroupsAsync(_backupPath, _backupCancellationTokenSource.Token, UpdateBackupDetail);
                completedTasks++;
                UpdateBackupProgress(completedTasks, totalTasks);
            }

            if (BackupRolesCheckBox.IsChecked == true)
            {
                CurrentBackupTaskText.Text = "Backing up Administration Roles...";
                BackupDetailText.Text = "Initializing Roles backup...";
                await _backupManager.BackupRolesAsync(_backupPath, _backupCancellationTokenSource.Token, UpdateBackupDetail);
                completedTasks++;
                UpdateBackupProgress(completedTasks, totalTasks);
            }

            if (BackupPermissionsCheckBox.IsChecked == true)
            {
                CurrentBackupTaskText.Text = "Backing up Global Permissions...";
                BackupDetailText.Text = "Initializing Permissions backup...";
                await _backupManager.BackupPermissionsAsync(_backupPath, _backupCancellationTokenSource.Token, UpdateBackupDetail);
                completedTasks++;
                UpdateBackupProgress(completedTasks, totalTasks);
            }

            if (BackupHostConfigCheckBox.IsChecked == true)
            {
                CurrentBackupTaskText.Text = "Backing up Host Configurations...";
                BackupDetailText.Text = "Initializing Host configurations backup...";
                await _backupManager.BackupHostConfigurationsAsync(_backupPath, _backupCancellationTokenSource.Token, UpdateBackupDetail);
                completedTasks++;
                UpdateBackupProgress(completedTasks, totalTasks);
            }

            if (BackupVMConfigCheckBox.IsChecked == true)
            {
                CurrentBackupTaskText.Text = "Backing up VM Configurations...";
                BackupDetailText.Text = "Initializing VM configurations backup...";
                await _backupManager.BackupVMConfigurationsAsync(_backupPath, _backupCancellationTokenSource.Token, UpdateBackupDetail);
                completedTasks++;
                UpdateBackupProgress(completedTasks, totalTasks);
            }

            if (BackupClusterConfigCheckBox.IsChecked == true)
            {
                CurrentBackupTaskText.Text = "Backing up Cluster Configurations...";
                BackupDetailText.Text = "Initializing Cluster configurations backup...";
                await _backupManager.BackupClusterConfigurationsAsync(_backupPath, _backupCancellationTokenSource.Token, UpdateBackupDetail);
                completedTasks++;
                UpdateBackupProgress(completedTasks, totalTasks);
            }

            if (BackupResourcePoolCheckBox.IsChecked == true)
            {
                CurrentBackupTaskText.Text = "Backing up Resource Pools...";
                BackupDetailText.Text = "Initializing Resource Pools backup...";
                await _backupManager.BackupResourcePoolsAsync(_backupPath, _backupCancellationTokenSource.Token, UpdateBackupDetail);
                completedTasks++;
                UpdateBackupProgress(completedTasks, totalTasks);
            }

            if (BackupFoldersCheckBox.IsChecked == true)
            {
                CurrentBackupTaskText.Text = "Backing up VM Folders...";
                BackupDetailText.Text = "Initializing VM Folders backup...";
                await _backupManager.BackupFoldersAsync(_backupPath, _backupCancellationTokenSource.Token, UpdateBackupDetail);
                completedTasks++;
                UpdateBackupProgress(completedTasks, totalTasks);
            }

            CurrentBackupTaskText.Text = "Backup process completed successfully!";
            BackupDetailText.Text = "All selected backup tasks have been completed.";
            WriteLog("🎉 Comprehensive backup process completed successfully", "INFO");
            WriteLog($"📂 Backup saved to: {_backupPath}", "INFO");
            MessageBox.Show($"Backup completed successfully!\n\nLocation: {_backupPath}\nTasks completed: {completedTasks}/{totalTasks}",
                          "Backup Complete", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (OperationCanceledException)
        {
            WriteLog("🛑 Backup operation was cancelled", "WARNING");
            CurrentBackupTaskText.Text = "Backup process was cancelled.";
            BackupDetailText.Text = "The backup operation was stopped.";
            MessageBox.Show("Backup operation was cancelled.", "Backup Cancelled", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        catch (Exception ex)
        {
            WriteLog($"❌ Backup process failed: {ex.Message}", "ERROR");
            CurrentBackupTaskText.Text = "Backup process failed.";
            BackupDetailText.Text = $"Error: {ex.Message}";
            MessageBox.Show($"Backup failed: {ex.Message}", "Backup Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            _isBackupRunning = false;
            ExecuteBackupButton.IsEnabled = true;
            CancelBackupButton.IsEnabled = false;
            CancelBackupButton.Visibility = Visibility.Collapsed;
            _backupCancellationTokenSource?.Dispose();
            _backupCancellationTokenSource = null;
        }
    }

    private void UpdateBackupDetail(string detail)
    {
        Dispatcher.Invoke(() =>
        {
            BackupDetailText.Text = detail;
            WriteLog($"📋 {detail}", "INFO");
        });
    }

    private int GetSelectedBackupTaskCount()
    {
        int count = 0;
        if (BackupVDSCheckBox.IsChecked == true) count++;
        if (BackupUsersCheckBox.IsChecked == true) count++;
        if (BackupRolesCheckBox.IsChecked == true) count++;
        if (BackupPermissionsCheckBox.IsChecked == true) count++;
        if (BackupHostConfigCheckBox.IsChecked == true) count++;
        if (BackupVMConfigCheckBox.IsChecked == true) count++;
        if (BackupClusterConfigCheckBox.IsChecked == true) count++;
        if (BackupResourcePoolCheckBox.IsChecked == true) count++;
        if (BackupFoldersCheckBox.IsChecked == true) count++;
        return count;
    }

    private void UpdateBackupProgress(int completed, int total)
    {
        if (total > 0)
        {
            var percentage = (double)completed / total * 100;
            BackupProgressBar.Value = percentage;
            BackupProgressText.Text = $"{percentage:F0}% Complete ({completed}/{total})";
        }
    }

    private void CancelBackup_Click(object sender, RoutedEventArgs e)
    {
        if (_isBackupRunning && _backupCancellationTokenSource != null)
        {
            var result = MessageBox.Show("Are you sure you want to cancel the backup process?",
                                       "Cancel Backup",
                                       MessageBoxButton.YesNo,
                                       MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                WriteLog("🛑 User requested backup cancellation...", "WARNING");
                _backupCancellationTokenSource.Cancel();

                CancelBackupButton.IsEnabled = false;
                CurrentBackupTaskText.Text = "Cancelling backup process...";
                BackupDetailText.Text = "Please wait while the current operation completes...";
            }
        }
    }

    #endregion

    #region Inventory

    private async Task UpdateInventoryTreeAsync()
    {
        try
        {
            var inventoryData = await _inventoryManager.GetInventoryAsync();

            Dispatcher.Invoke(() =>
            {
                InventoryTreeView.Items.Clear();
                BuildInventoryTree(inventoryData);
            });

            WriteLog("✅ Inventory tree updated successfully", "INFO");
        }
        catch (Exception ex)
        {
            WriteLog($"❌ Failed to update inventory tree: {ex.Message}", "ERROR");
        }
    }

    private void BuildInventoryTree(string inventoryData)
    {
        // Your existing method to parse and build tree from inventoryData string
        // unchanged, call this with the string returned by _inventoryManager.GetInventoryAsync()
    }

    #endregion

    #region Migration

    private async void MigrateHost_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var selectedHosts = GetSelectedHosts();

            if (selectedHosts.Count == 0)
            {
                MessageBox.Show("Please select hosts to migrate", "No Selection", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var migrationTasks = await _migrationManager.PrepareMigrationAsync(MigrationType.Host, selectedHosts);

            MigrationProgressGrid.ItemsSource = migrationTasks;

            await StartMigrationAsync(migrationTasks);
        }
        catch (Exception ex)
        {
            WriteLog($"❌ Host migration failed: {ex.Message}", "ERROR");
            MessageBox.Show($"Migration failed: {ex.Message}", "Migration Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task StartMigrationAsync(List<MigrationTask> tasks)
    {
        // Your existing migration progress logic here, unchanged
    }

    private List<string> GetSelectedHosts()
    {
        // Your existing method to get selected hosts from InventoryTreeView
    }

    #endregion

    // ... other event handlers unchanged or similarly adapted ...

    protected override void OnClosed(EventArgs e)
    {
        WriteLog("🔌 Shutting down vCenter Migration Tool...", "INFO");

        if (_isBackupRunning && _backupCancellationTokenSource != null)
            _backupCancellationTokenSource.Cancel();

        _runspaceManager?.Dispose();
        _backupCancellationTokenSource?.Dispose();

        base.OnClosed(e);
    }
}
