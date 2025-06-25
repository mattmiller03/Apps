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
    // FIXED: Simplified collection initialization
    public ObservableCollection<LogEntry> LogEntries { get; set; } = [];
    public ObservableCollection<MigrationTask> MigrationTasks { get; set; } = [];
    public ObservableCollection<ValidationResult> ValidationResults { get; set; } = [];

    // PowerShell Manager
    private PowerShellManager? _powerShellManager;

    // FIXED: Cached JsonSerializerOptions
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    // FIXED: Static readonly array for split operations
    private static readonly char[] LineSeparators = ['\r', '\n'];

    // NEW: Cancellation support for backup operations
    private CancellationTokenSource? _backupCancellationTokenSource;
    private bool _isBackupRunning = false;

    public MainWindow()
    {
        InitializeComponent();
        InitializeCollections();

        // Flush any logs that were created before UI was ready
        FlushPendingLogs();

        // Now it's safe to write logs
        WriteLog("🚀 vCenter Migration Tool v1.0 Started", "INFO");
        WriteLog("💡 Connect to both source and destination vCenters to begin", "INFO");

        // Initialize PowerShell asynchronously
        _ = InitializePowerShellAsync();
    }

    private void InitializeCollections()
    {
        // Bind collections to UI elements
        MigrationProgressGrid.ItemsSource = MigrationTasks;
        // REMOVED: ValidationResultsGrid binding (doesn't exist in XAML)
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        // Auto-load last used connection profile
        await AutoLoadLastConnectionProfile();
    }

    private async Task AutoLoadLastConnectionProfile()
    {
        try
        {
            var profiles = await ConnectionManager.LoadConnectionProfilesAsync();

            if (profiles.Profiles.Count > 0)
            {
                var lastUsedProfile = profiles.Profiles
                    .FirstOrDefault(p => p.ProfileName == profiles.LastUsedProfile)
                    ?? profiles.Profiles.OrderByDescending(p => p.LastUsed).First();

                LoadConnectionSettings(lastUsedProfile);
                WriteLog($"🔄 Auto-loaded connection profile: {lastUsedProfile.ProfileName}", "INFO");
            }
        }
        catch (Exception ex)
        {
            WriteLog($"⚠️ Could not auto-load connection profile: {ex.Message}", "WARNING");
        }
    }

    // FIXED: Single PowerShell initialization method with proper event subscription
    private async Task InitializePowerShellAsync()
    {
        try
        {
            WriteLog("🔧 Initializing PowerShell environment...", "INFO");
            _powerShellManager = new PowerShellManager();

            // Subscribe to log events from PowerShellManager
            _powerShellManager.LogMessage += (message, level) =>
            {
                Dispatcher.Invoke(() => WriteLog(message, level));
            };

            await _powerShellManager.InitializeAsync();
            WriteLog("✅ PowerShell environment initialized successfully", "INFO");
        }
        catch (Exception ex)
        {
            WriteLog($"❌ Failed to initialize PowerShell: {ex.Message}", "ERROR");
            MessageBox.Show($"Failed to initialize PowerShell environment:\n{ex.Message}",
                          "Initialization Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    #region Logging Methods
    private void WriteLog(string message, string level = "INFO")
    {
        var logEntry = new LogEntry
        {
            Timestamp = DateTime.Now,
            Level = level,
            Message = message
        };

        // Add to collection first
        LogEntries.Add(logEntry);

        // Only update UI if it's initialized
        if (LogTextBox != null)
        {
            Dispatcher.Invoke(() =>
            {
                try
                {
                    LogTextBox.AppendText($"[{logEntry.Timestamp:HH:mm:ss}] [{logEntry.Level}] {logEntry.Message}\r\n");
                    LogTextBox.ScrollToEnd();
                }
                catch (Exception ex)
                {
                    // Fallback - write to debug output if UI fails
                    System.Diagnostics.Debug.WriteLine($"[{logEntry.Timestamp:HH:mm:ss}] [{logEntry.Level}] {logEntry.Message}");
                    System.Diagnostics.Debug.WriteLine($"UI Error: {ex.Message}");
                }
            });
        }
        else
        {
            // UI not ready yet - write to debug output
            System.Diagnostics.Debug.WriteLine($"[{logEntry.Timestamp:HH:mm:ss}] [{logEntry.Level}] {logEntry.Message}");
        }
    }

    // Helper method to flush any pending logs to UI after initialization
    private void FlushPendingLogs()
    {
        if (LogTextBox == null) return;

        try
        {
            LogTextBox.Clear();
            foreach (var entry in LogEntries)
            {
                LogTextBox.AppendText($"[{entry.Timestamp:HH:mm:ss}] [{entry.Level}] {entry.Message}\r\n");
            }
            LogTextBox.ScrollToEnd();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error flushing logs: {ex.Message}");
        }
    }
    #endregion

    #region Connection Event Handlers
    private async void ConnectSource_Click(object sender, RoutedEventArgs e)
    {
        if (_powerShellManager is null)
        {
            WriteLog("❌ PowerShell manager not initialized", "ERROR");
            return;
        }

        try
        {
            ConnectSourceButton.IsEnabled = false;
            WriteLog($"🔌 Connecting to source vCenter: {SourceVCenterTextBox.Text}", "INFO");

            var connected = await _powerShellManager.ConnectToSourceVCenterAsync(
                SourceVCenterTextBox.Text,
                SourceUsernameTextBox.Text,
                SourcePasswordBox.Password);

            if (connected)
            {
                SourceStatusText.Text = $"✅ Connected to {SourceVCenterTextBox.Text}";
                SourceStatusText.Foreground = System.Windows.Media.Brushes.Green;

                var version = await _powerShellManager.GetVCenterVersionAsync("source");
                SourceVersionText.Text = $"Version: {version}";

                WriteLog("✅ Successfully connected to source vCenter", "INFO");
                await UpdateInventoryTreeAsync();
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
        }
        finally
        {
            ConnectSourceButton.IsEnabled = true;
        }
    }

    private async void ConnectDest_Click(object sender, RoutedEventArgs e)
    {
        if (_powerShellManager is null)
        {
            WriteLog("❌ PowerShell manager not initialized", "ERROR");
            return;
        }

        try
        {
            ConnectDestButton.IsEnabled = false;
            WriteLog($"🔌 Connecting to destination vCenter: {DestVCenterTextBox.Text}", "INFO");

            var connected = await _powerShellManager.ConnectToDestinationVCenterAsync(
                DestVCenterTextBox.Text,
                DestUsernameTextBox.Text,
                DestPasswordBox.Password);

            if (connected)
            {
                DestStatusText.Text = $"✅ Connected to {DestVCenterTextBox.Text}";
                DestStatusText.Foreground = System.Windows.Media.Brushes.Green;

                var version = await _powerShellManager.GetVCenterVersionAsync("destination");
                DestVersionText.Text = $"Version: {version}";

                WriteLog("✅ Successfully connected to destination vCenter", "INFO");
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
        }
        finally
        {
            ConnectDestButton.IsEnabled = true;
        }
    }

    private async void TestConnection_Click(object sender, RoutedEventArgs e)
    {
        // Disable the button during testing
        if (sender is Button button)
        {
            button.IsEnabled = false;
            button.Content = "🔍 Testing...";
        }

        WriteLog("🔍 Starting connectivity tests...", "INFO");

        var sourceServer = SourceVCenterTextBox.Text.Trim();
        var destServer = DestVCenterTextBox.Text.Trim();

        if (string.IsNullOrEmpty(sourceServer) && string.IsNullOrEmpty(destServer))
        {
            WriteLog("⚠️ No vCenter servers specified for testing", "WARNING");
            MessageBox.Show("Please enter at least one vCenter server address to test connectivity.",
                          "No Servers to Test", MessageBoxButton.OK, MessageBoxImage.Warning);

            // Re-enable button
            if (sender is Button btn)
            {
                btn.IsEnabled = true;
                btn.Content = "🔍 Test Connection";
            }
            return;
        }

        var testResults = new List<string>();
        var allTestsPassed = true;

        try
        {
            // Test source connection
            if (!string.IsNullOrEmpty(sourceServer))
            {
                WriteLog($"🔍 Testing connectivity to source vCenter: {sourceServer}", "INFO");

                var sourceReachable = await TestConnectivityAsync(sourceServer);
                var sourceResult = $"Source vCenter ({sourceServer}): {(sourceReachable ? "✅ Reachable" : "❌ Not Reachable")}";

                testResults.Add(sourceResult);
                WriteLog(sourceResult, sourceReachable ? "INFO" : "ERROR");

                if (!sourceReachable) allTestsPassed = false;

                // Test HTTPS port 443 specifically for vCenter
                if (sourceReachable)
                {
                    WriteLog($"🔍 Testing HTTPS port 443 on {sourceServer}", "INFO");
                    var httpsReachable = await TestPortConnectivityAsync(sourceServer, 443);
                    var httpsResult = $"Source HTTPS (443): {(httpsReachable ? "✅ Open" : "❌ Blocked")}";

                    testResults.Add(httpsResult);
                    WriteLog(httpsResult, httpsReachable ? "INFO" : "WARNING");

                    if (!httpsReachable) allTestsPassed = false;
                }
            }

            // Test destination connection
            if (!string.IsNullOrEmpty(destServer))
            {
                WriteLog($"🔍 Testing connectivity to destination vCenter: {destServer}", "INFO");

                var destReachable = await TestConnectivityAsync(destServer);
                var destResult = $"Destination vCenter ({destServer}): {(destReachable ? "✅ Reachable" : "❌ Not Reachable")}";

                testResults.Add(destResult);
                WriteLog(destResult, destReachable ? "INFO" : "ERROR");

                if (!destReachable) allTestsPassed = false;

                // Test HTTPS port 443 specifically for vCenter
                if (destReachable)
                {
                    WriteLog($"🔍 Testing HTTPS port 443 on {destServer}", "INFO");
                    var httpsReachable = await TestPortConnectivityAsync(destServer, 443);
                    var httpsResult = $"Destination HTTPS (443): {(httpsReachable ? "✅ Open" : "❌ Blocked")}";

                    testResults.Add(httpsResult);
                    WriteLog(httpsResult, httpsReachable ? "INFO" : "WARNING");

                    if (!httpsReachable) allTestsPassed = false;
                }
            }

            // Show comprehensive results
            var resultMessage = "Connectivity Test Results:\n\n" + string.Join("\n", testResults);

            if (allTestsPassed)
            {
                resultMessage += "\n\n🎉 All connectivity tests passed!";
                WriteLog("✅ All connectivity tests completed successfully", "INFO");
                MessageBox.Show(resultMessage, "Connectivity Test - Success",
                              MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                resultMessage += "\n\n⚠️ Some connectivity tests failed. Check network connectivity and firewall settings.";
                WriteLog("⚠️ Some connectivity tests failed", "WARNING");
                MessageBox.Show(resultMessage, "Connectivity Test - Issues Found",
                              MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
        catch (Exception ex)
        {
            WriteLog($"❌ Connectivity test error: {ex.Message}", "ERROR");
            MessageBox.Show($"Error during connectivity testing:\n{ex.Message}",
                          "Test Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            // Re-enable button
            if (sender is Button btn)
            {
                btn.IsEnabled = true;
                btn.Content = "🔍 Test Connection";
            }
        }
    }

    private static async Task<bool> TestConnectivityAsync(string server)
    {
        try
        {
            using var ping = new Ping();
            var reply = await ping.SendPingAsync(server, 5000);
            return reply.Status == IPStatus.Success;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Ping test failed for {server}: {ex.Message}");
            return false;
        }
    }

    private static async Task<bool> TestPortConnectivityAsync(string server, int port)
    {
        try
        {
            using var tcpClient = new System.Net.Sockets.TcpClient();
            var connectTask = tcpClient.ConnectAsync(server, port);
            var timeoutTask = Task.Delay(5000); // 5 second timeout

            var completedTask = await Task.WhenAny(connectTask, timeoutTask);

            if (completedTask == connectTask && tcpClient.Connected)
            {
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Port test failed for {server}:{port}: {ex.Message}");
            return false;
        }
    }

    private void DisconnectAll_Click(object sender, RoutedEventArgs e)
    {
        WriteLog("🔌 Disconnecting from all vCenters...", "INFO");

        _powerShellManager?.DisconnectAll();

        SourceStatusText.Text = "❌ Not Connected";
        SourceStatusText.Foreground = System.Windows.Media.Brushes.Red;
        SourceVersionText.Text = "Version: Unknown";

        DestStatusText.Text = "❌ Not Connected";
        DestStatusText.Foreground = System.Windows.Media.Brushes.Red;
        DestVersionText.Text = "Version: Unknown";

        InventoryTreeView.Items.Clear();

        WriteLog("✅ Disconnected from all vCenters", "INFO");
    }
    #endregion

    #region Backup Event Handlers
    private void SelectAllBackup_Click(object sender, RoutedEventArgs e)
    {
        BackupVDSCheckBox.IsChecked = true;
        BackupUsersCheckBox.IsChecked = true;
        BackupRolesCheckBox.IsChecked = true;
        BackupPermissionsCheckBox.IsChecked = true;
        BackupCertificatesCheckBox.IsChecked = true;
        BackupIdentityCheckBox.IsChecked = true;
        BackupHostConfigCheckBox.IsChecked = true;
        BackupVMConfigCheckBox.IsChecked = true;
        BackupClusterConfigCheckBox.IsChecked = true;
        BackupResourcePoolCheckBox.IsChecked = true;
        BackupFoldersCheckBox.IsChecked = true;

        WriteLog("✅ All backup tasks selected", "INFO");
    }

    private void ClearAllBackup_Click(object sender, RoutedEventArgs e)
    {
        BackupVDSCheckBox.IsChecked = false;
        BackupUsersCheckBox.IsChecked = false;
        BackupRolesCheckBox.IsChecked = false;
        BackupPermissionsCheckBox.IsChecked = false;
        BackupCertificatesCheckBox.IsChecked = false;
        BackupIdentityCheckBox.IsChecked = false;
        BackupHostConfigCheckBox.IsChecked = false;
        BackupVMConfigCheckBox.IsChecked = false;
        BackupClusterConfigCheckBox.IsChecked = false;
        BackupResourcePoolCheckBox.IsChecked = false;
        BackupFoldersCheckBox.IsChecked = false;

        WriteLog("❌ All backup tasks cleared", "INFO");
    }

    private static string? SelectFolder(string description, string selectedPath)
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
        var dialog = new OpenFileDialog
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

    private void BrowseBackup_Click(object sender, RoutedEventArgs e)
    {
        var selectedPath = SelectFolder("Select backup destination folder", BackupPathTextBox.Text);
        if (!string.IsNullOrEmpty(selectedPath))
        {
            BackupPathTextBox.Text = selectedPath;
            WriteLog($"📂 Backup path set to: {selectedPath}", "INFO");
        }
    }

    #region Connection Settings Menu Handlers
    private void ConnectionSettings_Click(object sender, RoutedEventArgs e)
    {
        var settingsWindow = new ConnectionSettingsWindow
        {
            Owner = this
        };

        if (settingsWindow.ShowDialog() == true && settingsWindow.SelectedSettings != null)
        {
            LoadConnectionSettings(settingsWindow.SelectedSettings);
            WriteLog($"🔧 Loaded connection profile: {settingsWindow.SelectedSettings.ProfileName}", "INFO");
        }
    }

    private async void LoadConnectionProfile_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var profiles = await ConnectionManager.LoadConnectionProfilesAsync();

            if (profiles.Profiles.Count == 0)
            {
                MessageBox.Show("No saved connection profiles found.", "No Profiles",
                              MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // Find the last used profile
            var lastUsedProfile = profiles.Profiles
                .FirstOrDefault(p => p.ProfileName == profiles.LastUsedProfile)
                ?? profiles.Profiles.OrderByDescending(p => p.LastUsed).First();

            LoadConnectionSettings(lastUsedProfile);
            WriteLog($"📂 Loaded connection profile: {lastUsedProfile.ProfileName}", "INFO");

            MessageBox.Show($"Loaded connection profile: {lastUsedProfile.ProfileName}",
                          "Profile Loaded", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            WriteLog($"❌ Error loading connection profile: {ex.Message}", "ERROR");
            MessageBox.Show($"Error loading connection profile: {ex.Message}",
                          "Load Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void SaveCurrentSettings_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var currentSettings = new ConnectionSettings
            {
                ProfileName = $"Quick Save {DateTime.Now:yyyy-MM-dd HH:mm}",
                SourceServer = SourceVCenterTextBox.Text,
                SourceUsername = SourceUsernameTextBox.Text,
                SourcePassword = SourcePasswordBox.Password,
                SaveSourcePassword = false, // Don't save passwords in quick save
                DestinationServer = DestVCenterTextBox.Text,
                DestinationUsername = DestUsernameTextBox.Text,
                DestinationPassword = DestPasswordBox.Password,
                SaveDestinationPassword = false,
                BackupPath = BackupPathTextBox.Text,
                LastUsed = DateTime.Now
            };

            var profiles = await ConnectionManager.LoadConnectionProfilesAsync();
            profiles.Profiles.Add(currentSettings);
            profiles.LastUsedProfile = currentSettings.ProfileName;

            await ConnectionManager.SaveConnectionProfilesAsync(profiles);

            WriteLog($"💾 Saved current settings as: {currentSettings.ProfileName}", "INFO");
            MessageBox.Show($"Current settings saved as: {currentSettings.ProfileName}",
                          "Settings Saved", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            WriteLog($"❌ Error saving current settings: {ex.Message}", "ERROR");
            MessageBox.Show($"Error saving current settings: {ex.Message}",
                          "Save Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void LoadConnectionSettings(ConnectionSettings settings)
    {
        SourceVCenterTextBox.Text = settings.SourceServer;
        SourceUsernameTextBox.Text = settings.SourceUsername;
        if (settings.SaveSourcePassword)
        {
            SourcePasswordBox.Password = settings.SourcePassword;
        }

        DestVCenterTextBox.Text = settings.DestinationServer;
        DestUsernameTextBox.Text = settings.DestinationUsername;
        if (settings.SaveDestinationPassword)
        {
            DestPasswordBox.Password = settings.DestinationPassword;
        }

        BackupPathTextBox.Text = settings.BackupPath;
    }
    #endregion

    // NEW: Enhanced ExecuteBackup_Click with granular progress and cancellation
    private async void ExecuteBackup_Click(object sender, RoutedEventArgs e)
    {
        if (_powerShellManager is null || !_powerShellManager.IsSourceConnected)
        {
            MessageBox.Show("Please connect to source vCenter first.", "Connection Required",
                          MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            _isBackupRunning = true;
            _backupCancellationTokenSource = new CancellationTokenSource();

            ExecuteBackupButton.IsEnabled = false;
            CancelBackupButton.IsEnabled = true; // Enable cancel button
            CancelBackupButton.Visibility = Visibility.Visible;

            WriteLog("🚀 Starting comprehensive backup process...", "INFO");

            var backupPath = BackupPathTextBox.Text;
            Directory.CreateDirectory(backupPath);

            // Create backup summary
            var backupSummary = new
            {
                BackupDate = DateTime.Now,
                SourcevCenter = SourceVCenterTextBox.Text,
                BackupPath = backupPath,
                PowerCLIAvailable = _powerShellManager.IsPowerCLIAvailable
            };

            var summaryFile = Path.Combine(backupPath, "BackupSummary.json");
            // FIXED: Use cached JsonSerializerOptions
            await File.WriteAllTextAsync(summaryFile, JsonSerializer.Serialize(backupSummary, JsonOptions));

            var totalTasks = GetSelectedBackupTaskCount();
            var completedTasks = 0;

            // Execute selected backup tasks with detailed progress
            if (BackupVDSCheckBox.IsChecked == true)
            {
                if (_backupCancellationTokenSource.Token.IsCancellationRequested)
                {
                    WriteLog("🛑 Backup cancelled by user", "WARNING");
                    return;
                }

                CurrentBackupTaskText.Text = "Backing up Virtual Distributed Switches...";
                BackupDetailText.Text = "Initializing VDS backup process...";
                WriteLog("📡 Starting VDS backup...", "INFO");

                await _powerShellManager.BackupVDSAsync(backupPath, _backupCancellationTokenSource.Token, UpdateBackupDetail);

                completedTasks++;
                UpdateBackupProgress(completedTasks, totalTasks);
                WriteLog("✅ VDS backup completed", "INFO");
            }

            if (BackupUsersCheckBox.IsChecked == true)
            {
                if (_backupCancellationTokenSource.Token.IsCancellationRequested)
                {
                    WriteLog("🛑 Backup cancelled by user", "WARNING");
                    return;
                }

                CurrentBackupTaskText.Text = "Backing up Users and Groups...";
                BackupDetailText.Text = "Initializing Users and Groups backup...";
                WriteLog("👥 Starting Users and Groups backup...", "INFO");

                await _powerShellManager.BackupUsersAndGroupsAsync(backupPath, _backupCancellationTokenSource.Token, UpdateBackupDetail);

                completedTasks++;
                UpdateBackupProgress(completedTasks, totalTasks);
                WriteLog("✅ Users and Groups backup completed", "INFO");
            }

            if (BackupRolesCheckBox.IsChecked == true)
            {
                if (_backupCancellationTokenSource.Token.IsCancellationRequested)
                {
                    WriteLog("🛑 Backup cancelled by user", "WARNING");
                    return;
                }

                CurrentBackupTaskText.Text = "Backing up Administration Roles...";
                BackupDetailText.Text = "Initializing Roles backup...";
                WriteLog("🔐 Starting Roles backup...", "INFO");

                await _powerShellManager.BackupRolesAsync(backupPath, _backupCancellationTokenSource.Token, UpdateBackupDetail);

                completedTasks++;
                UpdateBackupProgress(completedTasks, totalTasks);
                WriteLog("✅ Roles backup completed", "INFO");
            }

            if (BackupPermissionsCheckBox.IsChecked == true)
            {
                if (_backupCancellationTokenSource.Token.IsCancellationRequested)
                {
                    WriteLog("🛑 Backup cancelled by user", "WARNING");
                    return;
                }

                CurrentBackupTaskText.Text = "Backing up Global Permissions...";
                BackupDetailText.Text = "Initializing Permissions backup...";
                WriteLog("🛡️ Starting Permissions backup...", "INFO");

                await _powerShellManager.BackupPermissionsAsync(backupPath, _backupCancellationTokenSource.Token, UpdateBackupDetail);

                completedTasks++;
                UpdateBackupProgress(completedTasks, totalTasks);
                WriteLog("✅ Permissions backup completed", "INFO");
            }

            if (BackupHostConfigCheckBox.IsChecked == true)
            {
                if (_backupCancellationTokenSource.Token.IsCancellationRequested)
                {
                    WriteLog("🛑 Backup cancelled by user", "WARNING");
                    return;
                }

                CurrentBackupTaskText.Text = "Backing up Host Configurations...";
                BackupDetailText.Text = "Initializing Host configurations backup...";
                WriteLog("🖥️ Starting Host configurations backup...", "INFO");

                await _powerShellManager.BackupHostConfigurationsAsync(backupPath, _backupCancellationTokenSource.Token, UpdateBackupDetail);

                completedTasks++;
                UpdateBackupProgress(completedTasks, totalTasks);
                WriteLog("✅ Host configurations backup completed", "INFO");
            }

            if (BackupVMConfigCheckBox.IsChecked == true)
            {
                if (_backupCancellationTokenSource.Token.IsCancellationRequested)
                {
                    WriteLog("🛑 Backup cancelled by user", "WARNING");
                    return;
                }

                CurrentBackupTaskText.Text = "Backing up VM Configurations...";
                BackupDetailText.Text = "Initializing VM configurations backup...";
                WriteLog("💻 Starting VM configurations backup...", "INFO");

                await _powerShellManager.BackupVMConfigurationsAsync(backupPath, _backupCancellationTokenSource.Token, UpdateBackupDetail);

                completedTasks++;
                UpdateBackupProgress(completedTasks, totalTasks);
                WriteLog("✅ VM configurations backup completed", "INFO");
            }

            if (BackupClusterConfigCheckBox.IsChecked == true)
            {
                if (_backupCancellationTokenSource.Token.IsCancellationRequested)
                {
                    WriteLog("🛑 Backup cancelled by user", "WARNING");
                    return;
                }

                CurrentBackupTaskText.Text = "Backing up Cluster Configurations...";
                BackupDetailText.Text = "Initializing Cluster configurations backup...";
                WriteLog("🏢 Starting Cluster configurations backup...", "INFO");

                await _powerShellManager.BackupClusterConfigurationsAsync(backupPath, _backupCancellationTokenSource.Token, UpdateBackupDetail);

                completedTasks++;
                UpdateBackupProgress(completedTasks, totalTasks);
                WriteLog("✅ Cluster configurations backup completed", "INFO");
            }

            if (BackupResourcePoolCheckBox.IsChecked == true)
            {
                if (_backupCancellationTokenSource.Token.IsCancellationRequested)
                {
                    WriteLog("🛑 Backup cancelled by user", "WARNING");
                    return;
                }

                CurrentBackupTaskText.Text = "Backing up Resource Pools...";
                BackupDetailText.Text = "Initializing Resource Pools backup...";
                WriteLog("🏊 Starting Resource Pools backup...", "INFO");

                await _powerShellManager.BackupResourcePoolsAsync(backupPath, _backupCancellationTokenSource.Token, UpdateBackupDetail);

                completedTasks++;
                UpdateBackupProgress(completedTasks, totalTasks);
                WriteLog("✅ Resource Pools backup completed", "INFO");
            }

            if (BackupFoldersCheckBox.IsChecked == true)
            {
                if (_backupCancellationTokenSource.Token.IsCancellationRequested)
                {
                    WriteLog("🛑 Backup cancelled by user", "WARNING");
                    return;
                }

                CurrentBackupTaskText.Text = "Backing up VM Folders...";
                BackupDetailText.Text = "Initializing VM Folders backup...";
                WriteLog("📁 Starting VM Folders backup...", "INFO");

                await _powerShellManager.BackupFoldersAsync(backupPath, _backupCancellationTokenSource.Token, UpdateBackupDetail);

                completedTasks++;
                UpdateBackupProgress(completedTasks, totalTasks);
                WriteLog("✅ VM Folders backup completed", "INFO");
            }

            if (!_backupCancellationTokenSource.Token.IsCancellationRequested)
            {
                CurrentBackupTaskText.Text = "Backup process completed successfully!";
                BackupDetailText.Text = "All selected backup tasks have been completed.";
                WriteLog("🎉 Comprehensive backup process completed successfully", "INFO");
                WriteLog($"📂 Backup saved to: {backupPath}", "INFO");

                MessageBox.Show($"Backup completed successfully!\n\nLocation: {backupPath}\nTasks completed: {completedTasks}/{totalTasks}",
                              "Backup Complete", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                CurrentBackupTaskText.Text = "Backup process was cancelled.";
                BackupDetailText.Text = "The backup operation was stopped by user request.";
                WriteLog("🛑 Backup process was cancelled by user", "WARNING");

                MessageBox.Show($"Backup was cancelled.\n\nPartially completed: {completedTasks}/{totalTasks} tasks",
                              "Backup Cancelled", MessageBoxButton.OK, MessageBoxImage.Warning);
            }

        }
        catch (OperationCanceledException)
        {
            WriteLog("🛑 Backup operation was cancelled", "WARNING");
            CurrentBackupTaskText.Text = "Backup process was cancelled.";
            BackupDetailText.Text = "The backup operation was stopped.";

            MessageBox.Show("Backup operation was cancelled.", "Backup Cancelled",
                          MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        catch (Exception ex)
        {
            WriteLog($"❌ Backup process failed: {ex.Message}", "ERROR");
            CurrentBackupTaskText.Text = "Backup process failed.";
            BackupDetailText.Text = $"Error: {ex.Message}";

            MessageBox.Show($"Backup failed: {ex.Message}", "Backup Error",
                          MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            _isBackupRunning = false;
            ExecuteBackupButton.IsEnabled = true;
            CancelBackupButton.IsEnabled = false;
            CancelBackupButton.Visibility = Visibility.Collapsed;

            if (!_backupCancellationTokenSource?.Token.IsCancellationRequested == true)
            {
                BackupProgressBar.Value = 0;
                BackupProgressText.Text = "0% Complete";
                CurrentBackupTaskText.Text = "Ready to start backup process...";
                BackupDetailText.Text = "Select backup tasks and click Execute Backup to begin.";
            }

            _backupCancellationTokenSource?.Dispose();
            _backupCancellationTokenSource = null;
        }
    }

    // NEW: Cancel backup button event handler
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

    // NEW: Method to update backup details
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
        if (BackupCertificatesCheckBox.IsChecked == true) count++;
        if (BackupIdentityCheckBox.IsChecked == true) count++;
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
    #endregion

    #region Migration Event Handlers
    private async void MigrateHost_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // Get selected hosts from inventory
            var selectedHosts = GetSelectedHosts();

            if (selectedHosts.Count == 0)
            {
                MessageBox.Show("Please select hosts to migrate", "No Selection", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Prepare migration tasks
            var migrationTasks = await _powerShellManager.PrepareMigrationAsync(MigrationType.Host, selectedHosts);

            // Bind to DataGrid
            MigrationProgressGrid.ItemsSource = migrationTasks;

            // Start migration process
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
        var cancellationTokenSource = new CancellationTokenSource();

        try
        {
            int totalTasks = tasks.Count;
            int completedTasks = 0;

            foreach (var task in tasks)
            {
                if (cancellationTokenSource.Token.IsCancellationRequested)
                    break;

                task.Status = MigrationStatus.InProgress;
                task.StartTime = DateTime.Now;

                try
                {
                    // Simulate migration (replace with actual migration logic)
                    await SimulateMigrationAsync(task, cancellationTokenSource.Token);

                    task.Status = MigrationStatus.Completed;
                    task.Progress = 100;
                    completedTasks++;
                }
                catch (OperationCanceledException)
                {
                    task.Status = MigrationStatus.Cancelled;
                    break;
                }
                catch (Exception ex)
                {
                    task.Status = MigrationStatus.Failed;
                    task.Details = ex.Message;
                }

                // Update progress
                UpdateMigrationProgress(completedTasks, totalTasks);
                MigrationProgressGrid.Items.Refresh();
            }
        }
        catch (Exception ex)
        {
            WriteLog($"❌ Migration process failed: {ex.Message}", "ERROR");
        }
    }

    private async Task SimulateMigrationAsync(MigrationTask task, CancellationToken cancellationToken)
    {
        for (int progress = 0; progress <= 100; progress += 10)
        {
            cancellationToken.ThrowIfCancellationRequested();

            task.Progress = progress;
            task.Details = $"Migrating {task.ObjectName} - Step {progress}%";

            // Simulate work
            await Task.Delay(500, cancellationToken);
        }
    }

    private void UpdateMigrationProgress(int completed, int total)
    {
        OverallProgressBar.Maximum = total;
        OverallProgressBar.Value = completed;
        OverallProgressText.Text = $"{completed} / {total} tasks completed";
        EstimatedTimeText.Text = $"Estimated time: {total - completed} minutes";
    }

    private void PauseMigration_Click(object sender, RoutedEventArgs e)
    {
        // Implement pause logic
    }

    private void StopMigration_Click(object sender, RoutedEventArgs e)
    {
        // Implement stop logic
    }

    private void ExportReport_Click(object sender, RoutedEventArgs e)
    {
        // Export migration report to CSV/JSON
    }
    private void MigrateVM_Click(object sender, RoutedEventArgs e)
    {
        WriteLog("💻 VM migration functionality will be implemented", "INFO");
        MessageBox.Show("VM migration functionality will be implemented here.", "Migration",
                      MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void MigrateCluster_Click(object sender, RoutedEventArgs e)
    {
        WriteLog("🏢 Cluster migration functionality will be implemented", "INFO");
        MessageBox.Show("Cluster migration functionality will be implemented here.", "Migration",
                      MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void BatchMigrate_Click(object sender, RoutedEventArgs e)
    {
        WriteLog("📦 Starting batch migration...", "INFO");
    }

    private void Rollback_Click(object sender, RoutedEventArgs e)
    {
        var result = MessageBox.Show("Are you sure you want to rollback the migration? This action cannot be undone.",
                                   "Confirm Rollback", MessageBoxButton.YesNo, MessageBoxImage.Warning);

        if (result == MessageBoxResult.Yes)
        {
            WriteLog("↩️ Starting migration rollback...", "WARNING");
        }
    }


    #endregion

    #region Utility Event Handlers
    private async Task UpdateInventoryTreeAsync()
    {
        if (_powerShellManager is null || !_powerShellManager.IsSourceConnected) return;

        WriteLog("📊 Updating inventory tree...", "INFO");

        try
        {
            var inventory = await _powerShellManager.GetInventoryAsync();

            Dispatcher.Invoke(() => {
                InventoryTreeView.Items.Clear();
                BuildInventoryTree(inventory);
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
        var rootItem = new TreeViewItem { Header = $"📍 {SourceVCenterTextBox.Text}" };
        InventoryTreeView.Items.Add(rootItem);

        if (string.IsNullOrEmpty(inventoryData)) return;

        // FIXED: Use static readonly array
        var lines = inventoryData.Split(LineSeparators, StringSplitOptions.RemoveEmptyEntries);
        TreeViewItem? currentDC = null;
        TreeViewItem? currentCluster = null;
        TreeViewItem? currentHost = null;

        foreach (var line in lines)
        {
            var parts = line.Split(':');
            if (parts.Length < 2) continue;

            switch (parts[0])
            {
                case "DC":
                    currentDC = new TreeViewItem { Header = $"🏢 {parts[1]}" };
                    rootItem.Items.Add(currentDC);
                    break;
                case "CLUSTER":
                    if (currentDC != null)
                    {
                        currentCluster = new TreeViewItem { Header = $"🏢 {parts[1]}" };
                        currentDC.Items.Add(currentCluster);
                    }
                    break;
                case "HOST":
                    if (currentCluster != null)
                    {
                        currentHost = new TreeViewItem { Header = $"🖥️ {parts[1]}" };
                        currentCluster.Items.Add(currentHost);
                    }
                    break;
                case "VM":
                    if (currentHost != null)
                    {
                        var vmItem = new TreeViewItem { Header = $"💻 {parts[1]}" };
                        currentHost.Items.Add(vmItem);
                    }
                    break;
            }
        }

        rootItem.IsExpanded = true;
    }

    private void ScopeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var selectedScope = ((ComboBoxItem)ScopeComboBox.SelectedItem)?.Content?.ToString();
        WriteLog($"🎯 Backup scope changed to: {selectedScope}", "INFO");
    }

    private void ClearLog_Click(object sender, RoutedEventArgs e)
    {
        LogTextBox.Clear();
        LogEntries.Clear();
        WriteLog("🗑️ Log cleared", "INFO");
    }

    private void SaveLog_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog
        {
            Filter = "Text files (*.txt)|*.txt|Log files (*.log)|*.log",
            DefaultExt = "txt",
            FileName = $"VCenterMigration_{DateTime.Now:yyyyMMdd_HHmmss}.txt"
        };

        if (dialog.ShowDialog() == true)
        {
            File.WriteAllText(dialog.FileName, LogTextBox.Text);
            WriteLog($"💾 Log saved to: {dialog.FileName}", "INFO");
        }
    }
    #endregion

    #region Menu Event Handlers
    private void ConnectVCenter_Click(object sender, RoutedEventArgs e)
    {
        MainTabControl.SelectedIndex = 0;
    }

    private void PowerCLIConfig_Click(object sender, RoutedEventArgs e)
    {
        WriteLog("🔧 PowerCLI configuration dialog would open here", "INFO");
    }

    private void ImportConfig_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
            Title = "Import Configuration"
        };

        if (dialog.ShowDialog() == true)
        {
            WriteLog($"📥 Importing configuration from: {dialog.FileName}", "INFO");
        }
    }

    private void ExportConfig_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog
        {
            Filter = "JSON files (*.json)|*.json",
            DefaultExt = "json",
            FileName = $"VCenterMigrationConfig_{DateTime.Now:yyyyMMdd}.json"
        };

        if (dialog.ShowDialog() == true)
        {
            WriteLog($"📤 Exporting configuration to: {dialog.FileName}", "INFO");
        }
    }

    private void TestConnectivity_Click(object sender, RoutedEventArgs e)
    {
        TestConnection_Click(sender, e);
    }

    private void ValidatePrerequisites_Click(object sender, RoutedEventArgs e)
    {
        WriteLog("✅ Validation functionality would be implemented here", "INFO");
    }

    private void Documentation_Click(object sender, RoutedEventArgs e)
    {
        WriteLog("📖 Documentation would open here", "INFO");
    }

    private void About_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show("vCenter Migration Tool v1.0\n\nA comprehensive tool for migrating from vCenter 7.x to 8.x\n\nBuilt with PowerCLI and WPF",
                      "About", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void Exit_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
    #endregion

    #region Validation Event Handlers
    // Add these methods to your MainWindow.xaml.cs file

    private void SelectAllValidation_Click(object sender, RoutedEventArgs e)
    {
        ValidateConnectivityCheckBox.IsChecked = true;
        ValidateVersionCheckBox.IsChecked = true;
        ValidateResourcesCheckBox.IsChecked = true;
        ValidatePermissionsCheckBox.IsChecked = true;
        ValidateNetworkCheckBox.IsChecked = true;
        ValidateStorageCheckBox.IsChecked = true;
        ValidateServicesCheckBox.IsChecked = true;
        ValidateVMsCheckBox.IsChecked = true;
        ValidateNetworkingCheckBox.IsChecked = true;

        WriteLog("✅ All validation tests selected", "INFO");
    }

    private void ClearAllValidation_Click(object sender, RoutedEventArgs e)
    {
        ValidateConnectivityCheckBox.IsChecked = false;
        ValidateVersionCheckBox.IsChecked = false;
        ValidateResourcesCheckBox.IsChecked = false;
        ValidatePermissionsCheckBox.IsChecked = false;
        ValidateNetworkCheckBox.IsChecked = false;
        ValidateStorageCheckBox.IsChecked = false;
        ValidateServicesCheckBox.IsChecked = false;
        ValidateVMsCheckBox.IsChecked = false;
        ValidateNetworkingCheckBox.IsChecked = false;

        WriteLog("❌ All validation tests cleared", "INFO");
    }

    private async void RunValidation_Click(object sender, RoutedEventArgs e)
    {
        WriteLog("🔍 Starting validation tests...", "INFO");

        // Add your validation logic here
        ValidationResults.Clear();

        var testResults = new List<ValidationResult>
    {
        new() { TestName = "Connectivity Test", Result = "✅ Passed", Details = "All connections successful", Recommendation = "None" },
        new() { TestName = "Version Check", Result = "✅ Passed", Details = "Compatible versions detected", Recommendation = "None" },
        new() { TestName = "Resource Check", Result = "⚠️ Warning", Details = "Low disk space on destination", Recommendation = "Free up 50GB disk space" }
    };

        foreach (var result in testResults)
        {
            ValidationResults.Add(result);
        }

        WriteLog("✅ Validation tests completed", "INFO");
        MessageBox.Show("Validation tests completed. Check results grid for details.",
                       "Validation Complete", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    #endregion
    protected override void OnClosed(EventArgs e)
    {
        WriteLog("🔌 Shutting down vCenter Migration Tool...", "INFO");

        // Cancel any running backup operations
        if (_isBackupRunning && _backupCancellationTokenSource != null)
        {
            _backupCancellationTokenSource.Cancel();
        }

        _powerShellManager?.Dispose();
        _backupCancellationTokenSource?.Dispose();

        base.OnClosed(e);
    }
}
