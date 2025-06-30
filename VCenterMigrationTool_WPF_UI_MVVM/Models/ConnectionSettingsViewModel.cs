using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VCenterMigrationTool_WPF_UI.Models;
using VCenterMigrationTool_WPF_UI.Utilities;
using VCenterMigrationTool_WPF_UI.ViewModels;

namespace VCenterMigrationTool_WPF_UI.ViewModels
{
    public partial class ConnectionSettingsViewModel : ObservableObject
    {
        [ObservableProperty] private ConnectionSettings currentSettings = new();
        [ObservableProperty] private ConnectionSettings selectedProfile;
        [ObservableProperty] private string sourceStatus;
        [ObservableProperty] private string sourceVersion;
        [ObservableProperty] private string destStatus;
        [ObservableProperty] private string destVersion;
        [ObservableProperty] private bool isSourceConnected;
        [ObservableProperty] private bool isDestConnected;

        private readonly PowerShellManager _psManager; // Or inject via constructor

        private bool? _dialogResult;
        public bool? DialogResult
        {
            get => _dialogResult;
            set => SetProperty(ref _dialogResult, value);
        }

        public ObservableCollection<ConnectionSettings> Profiles { get; } = new();

        public ConnectionSettingsViewModel(PowerShellManager psManager = null)
        {
            _psManager = psManager ?? new PowerShellManager();
        }

        [RelayCommand]
        public async Task ConnectSourceAsync()
        {
            SourceStatus = "Connecting...";
            IsSourceConnected = false;
            try
            {
                var connected = await _psManager.ConnectToSourceVCenterAsync(
                    CurrentSettings.SourceServer,
                    CurrentSettings.SourceUsername,
                    CurrentSettings.SourcePassword
                );
                if (connected)
                {
                    IsSourceConnected = true;
                    SourceStatus = $"✅ Connected to {CurrentSettings.SourceServer}";
                    SourceVersion = await _psManager.GetVCenterVersionAsync("source");
                }
                else
                {
                    SourceStatus = "❌ Connection Failed";
                }
            }
            catch (Exception ex)
            {
                SourceStatus = "❌ Connection Failed";
                // Optionally log error (see LogViewModel integration)
            }
        }

        [RelayCommand]
        public async Task ConnectDestAsync()
        {
            DestStatus = "Connecting...";
            IsDestConnected = false;
            try
            {
                var connected = await _psManager.ConnectToDestinationVCenterAsync(
                    CurrentSettings.DestinationServer,
                    CurrentSettings.DestinationUsername,
                    CurrentSettings.DestinationPassword
                );
                if (connected)
                {
                    IsDestConnected = true;
                    DestStatus = $"✅ Connected to {CurrentSettings.DestinationServer}";
                    DestVersion = await _psManager.GetVCenterVersionAsync("destination");
                }
                else
                {
                    DestStatus = "❌ Connection Failed";
                }
            }
            catch (Exception ex)
            {
                DestStatus = "❌ Connection Failed";
                // Optionally log error
            }
        }
        [RelayCommand]
        public void NewProfile() { /* ... */ }
        [RelayCommand]
        public void SaveProfile()
        {
            // ... your save logic ...
            DialogResult = true; // triggers window close
        }

        [RelayCommand]
        public void DeleteProfile() { /* ... */ }
        [RelayCommand]
        public void DisconnectAll()
        {
            _psManager.DisconnectAll();
            IsSourceConnected = false;
            IsDestConnected = false;
            SourceStatus = "❌ Not Connected";
            DestStatus = "❌ Not Connected";
            SourceVersion = "Version: Unknown";
            DestVersion = "Version: Unknown";
        }

        // Add more commands for Save/Load profile, TestConnection, etc.
    }
}
