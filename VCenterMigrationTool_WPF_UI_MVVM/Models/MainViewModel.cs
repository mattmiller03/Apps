using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using System;
using System.Text.Json;
using System.Windows;
using VCenterMigrationTool_WPF_UI.Utilities;
using VCenterMigrationTool_WPF_UI.ViewModels;
using System.IO; // Add this namespace
using System.Threading.Tasks; // Add this namespace
using VCenterMigrationTool_WPF_UI.Models;


namespace VCenterMigrationTool_WPF_UI.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly PowerShellManager _powerShellManager;
        private readonly ILogService _logService;
        [ObservableProperty]
        private ConnectionSettingsViewModel _connectionSettingsViewModel;

        public MainViewModel(PowerShellManager powerShellManager, ILogService logService)
        {
            _powerShellManager = powerShellManager ?? throw new ArgumentNullException(nameof(powerShellManager));
            _logService = logService ?? throw new ArgumentNullException(nameof(logService));
            ConnectionSettingsViewModel = new ConnectionSettingsViewModel(_powerShellManager, _logService);
            // Initialize other ViewModels and data as needed
        }

        [RelayCommand]
        public async Task ImportConfigurationAsync()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "Configuration Files (*.json;*.xml)|*.json;*.xml|All files (*.*)|*.*",
                Title = "Import Configuration"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                string filePath = openFileDialog.FileName;

                try
                {
                    string json = await File.ReadAllTextAsync(filePath);

                    // Deserialize the configuration data (replace with your actual data structure)
                    // For example, if your configuration is a ConnectionProfile:
                    ConnectionProfile loadedProfile = JsonSerializer.Deserialize<ConnectionProfile>(json);

                    if (loadedProfile != null)
                    {
                        // Apply the loaded configuration to your application
                        await ApplyConfigurationAsync(loadedProfile);
                        _logService.LogMessage($"Configuration imported successfully from {filePath}.", "INFO");
                    }
                    else
                    {
                        _logService.LogMessage($"Failed to import configuration from {filePath}: Invalid file format.", "WARNING");
                        MessageBox.Show($"Failed to import configuration from {filePath}: Invalid file format.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                catch (Exception ex)
                {
                    _logService.LogMessage($"Failed to import configuration from {filePath}: {ex.Message}", "ERROR");
                    MessageBox.Show($"Failed to import configuration from {filePath}: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async Task ApplyConfigurationAsync(ConnectionProfile loadedProfile)
        {
            // Apply the loaded configuration to your application
            // For example, update the Profiles collection in ConnectionSettingsViewModel:
            if (ConnectionSettingsViewModel != null)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    ConnectionSettingsViewModel.Profiles.Clear();
                    foreach (var profile in loadedProfile.Profiles)
                    {
                        ConnectionSettingsViewModel.Profiles.Add(profile);
                    }
                });
            }

            _logService.LogMessage($"Configuration applied successfully.", "INFO");
            await Task.CompletedTask;
        }

        [RelayCommand]
        public async Task ExportConfigurationAsync()
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Filter = "Configuration Files (*.json)|*.json|All files (*.*)|*.*",
                Title = "Export Configuration",
                FileName = "VCenterMigrationConfig.json" // Default file name
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                string filePath = saveFileDialog.FileName;

                try
                {
                    // Get the configuration data (replace with your actual data structure)
                    // For example, if your configuration is a ConnectionProfile:
                    ConnectionProfile profileToExport = new ConnectionProfile { Profiles = ConnectionSettingsViewModel.Profiles };

                    // Serialize the configuration data
                    string json = JsonSerializer.Serialize(profileToExport, new JsonSerializerOptions { WriteIndented = true });

                    // Write the JSON data to the file
                    await File.WriteAllTextAsync(filePath, json);

                    _logService.LogMessage($"Configuration exported successfully to {filePath}.", "INFO");
                }
                catch (Exception ex)
                {
                    _logService.LogMessage($"Failed to export configuration to {filePath}: {ex.Message}", "ERROR");
                    MessageBox.Show($"Failed to export configuration to {filePath}: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        [RelayCommand]
        public void OpenConnectionSettings()
        {
            // Use Application.Current.Dispatcher.Invoke to execute on the UI thread
            Application.Current.Dispatcher.Invoke(() =>
            {
                ConnectionSettingsWindow settingsWindow = new ConnectionSettingsWindow(); // No ViewModel parameter
                settingsWindow.DataContext = ConnectionSettingsViewModel; // Set the DataContext
                settingsWindow.ShowDialog(); // Use ShowDialog to open it as a modal window
            });
        }
    }
}