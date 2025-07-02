using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using VCenterMigrationTool_WPF_UI.Models;
using VCenterMigrationTool_WPF_UI.Utilities;
using VCenterMigrationTool_WPF_UI.Views;

namespace VCenterMigrationTool_WPF_UI.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly PowerShellManager _powerShellManager;
        private readonly ILogService _logService;

        [ObservableProperty]
        private ConnectionSettingsViewModel _connectionSettingsViewModel;

        [ObservableProperty]
        private MigrationViewModel _migrationViewModel;

        public MainViewModel(PowerShellManager powerShellManager, ILogService logService)
        {
            _powerShellManager = powerShellManager;
            _logService = logService;
            _connectionSettingsViewModel = new ConnectionSettingsViewModel(_powerShellManager, _logService);
            _migrationViewModel = new MigrationViewModel(_powerShellManager, _logService);
        }

        [RelayCommand]
        private void OpenConnectionSettings()
        {
            try
            {
                var window = new ConnectionSettingsWindow(_connectionSettingsViewModel);
                window.ShowDialog();
                _logService.LogMessage("Connection settings opened", "INFO");
            }
            catch (Exception ex)
            {
                _logService.LogMessage($"Failed to open settings: {ex.Message}", "ERROR");
            }
        }

        [RelayCommand]
        private async Task ImportConfigurationAsync()
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "JSON Files (*.json)|*.json",
                Title = "Import Configuration"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    var json = await File.ReadAllTextAsync(openFileDialog.FileName);
                    var profile = JsonSerializer.Deserialize<ConnectionProfile>(json);

                    if (profile != null)
                    {
                        // Apply configuration through ConnectionSettingsViewModel
                        await Application.Current.Dispatcher.InvokeAsync(() =>
                        {
                            _connectionSettingsViewModel.Profiles.Clear();
                            foreach (var p in profile.Profiles)
                            {
                                _connectionSettingsViewModel.Profiles.Add(p);
                            }
                        });
                        _logService.LogMessage("Configuration imported", "INFO");
                    }
                }
                catch (Exception ex)
                {
                    _logService.LogMessage($"Import failed: {ex.Message}", "ERROR");
                }
            }
        }

        [RelayCommand]
        private async Task ExportConfigurationAsync()
        {
            var saveFileDialog = new SaveFileDialog
            {
                Filter = "JSON Files (*.json)|*.json",
                FileName = "VCenterConfig.json"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    var profile = new ConnectionProfile
                    {
                        Profiles = _connectionSettingsViewModel.Profiles.ToList() // Explicit conversion
                    };
                    var json = JsonSerializer.Serialize(profile, new JsonSerializerOptions { WriteIndented = true });
                    await File.WriteAllTextAsync(saveFileDialog.FileName, json);
                    _logService.LogMessage("Configuration exported", "INFO");
                }
                catch (Exception ex)
                {
                    _logService.LogMessage($"Export failed: {ex.Message}", "ERROR");
                }
            }
        }
    }
}
