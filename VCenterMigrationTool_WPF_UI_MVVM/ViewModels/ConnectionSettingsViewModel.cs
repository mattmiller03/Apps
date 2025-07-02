using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using VCenterMigrationTool_WPF_UI.Models;
using VCenterMigrationTool_WPF_UI.Utilities;
using VCenterMigrationTool_WPF_UI.Views;

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
        [ObservableProperty] private bool _isLoadingProfile;

        private readonly PowerShellManager _psManager;
        private readonly ILogService _logService;

        private bool? _dialogResult;
        public bool? DialogResult
        {
            get => _dialogResult;
            set => SetProperty(ref _dialogResult, value);
        }

        public ObservableCollection<ConnectionSettings> Profiles { get; } = new();

        public ConnectionSettingsViewModel(PowerShellManager psManager, ILogService logService)
        {
            _psManager = psManager ?? throw new ArgumentNullException(nameof(psManager));
            _logService = logService ?? throw new ArgumentNullException(nameof(logService));
            LoadConnectionProfiles();
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
                _logService.LogMessage($"Connection to source failed: {ex.Message}", "ERROR");
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
                _logService.LogMessage($"Connection to destination failed: {ex.Message}", "ERROR");
            }
        }

        [RelayCommand]
        public void NewProfile()
        {
            CurrentSettings = new ConnectionSettings { ProfileName = "New Profile" };
            SelectedProfile = null;
        }

        [RelayCommand]
        public async Task SaveProfileAsync()
        {
            try
            {
                if (!CurrentSettings.IsValid())
                {
                    var errors = string.Join("\n", CurrentSettings.GetErrors().Select(e => e.ErrorMessage));
                    MessageBox.Show($"Please fix the following errors:\n{errors}", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                CurrentSettings.LastUsed = DateTime.Now;

                var clonedProfile = (ConnectionSettings)CurrentSettings.Clone();

                var existingProfile = Profiles.FirstOrDefault(p => p.ProfileName.Equals(clonedProfile.ProfileName, StringComparison.OrdinalIgnoreCase));

                if (existingProfile != null)
                {
                    var result = MessageBox.Show(
                        $"A profile named '{clonedProfile.ProfileName}' already exists. Do you want to overwrite it?",
                        "Profile Exists",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
                    {
                        Profiles.Remove(existingProfile);
                    }
                    else
                    {
                        return;
                    }
                }

                Profiles.Add(clonedProfile);

                var connectionProfile = new ConnectionProfile
                {
                    Profiles = Profiles.ToList()
                };

                await ConnectionManager.SaveConnectionProfilesAsync(connectionProfile, _logService);

                _logService.LogMessage($"Profile '{clonedProfile.ProfileName}' saved successfully.", "INFO");
                MessageBox.Show($"Profile '{clonedProfile.ProfileName}' saved successfully.", "Save Profile", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                _logService.LogMessage($"Failed to save profile: {ex.Message}", "ERROR");
                MessageBox.Show($"Failed to save profile: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        public async Task LoadProfileAsync()
        {
            IsLoadingProfile = true;
            try
            {
                OpenFileDialog openFileDialog = new OpenFileDialog
                {
                    Filter = "Connection Profile Files (*.json)|*.json",
                    Title = "Load Connection Profile"
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    string filePath = openFileDialog.FileName;

                    try
                    {
                        string json = await File.ReadAllTextAsync(filePath);
                        ConnectionSettings loadedSettings = JsonSerializer.Deserialize<ConnectionSettings>(json);

                        if (loadedSettings != null)
                        {
                            CurrentSettings = loadedSettings;
                            _logService.LogMessage($"Profile loaded successfully from {filePath}.", "INFO");
                        }
                        else
                        {
                            _logService.LogMessage($"Failed to load profile from {filePath}: Invalid file format.", "WARNING");
                            MessageBox.Show($"Failed to load profile from {filePath}: Invalid file format.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logService.LogMessage($"Failed to load profile from {filePath}: {ex.Message}", "ERROR");
                        MessageBox.Show($"Failed to load profile from {filePath}: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            finally
            {
                IsLoadingProfile = false;
            }
        }

        [RelayCommand]
        public async Task DeleteProfileAsync()
        {
            if (SelectedProfile == null)
            {
                MessageBox.Show("Please select a profile to delete.", "Delete Profile", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            MessageBoxResult result = MessageBox.Show($"Are you sure you want to delete the profile '{SelectedProfile.ProfileName}'?",
                                                        "Delete Profile?",
                                                        MessageBoxButton.YesNo,
                                                        MessageBoxImage.Question);

            if (result == MessageBoxResult.No)
            {
                return;
            }

            Profiles.Remove(SelectedProfile);

            var connectionProfile = new ConnectionProfile
            {
                Profiles = Profiles.ToList()
            };

            await ConnectionManager.SaveConnectionProfilesAsync(connectionProfile, _logService);

            SelectedProfile = null;
            CurrentSettings = new ConnectionSettings();

            _logService.LogMessage($"Profile deleted successfully.", "INFO");
        }

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

        [RelayCommand]
        public void BrowsePath()
        {
            var dialog = new SaveFileDialog
            {
                InitialDirectory = CurrentSettings.BackupPath,
                Filter = "Directories|*.d",
                FileName = "select"
            };
            if (dialog.ShowDialog() == true)
            {
                CurrentSettings.BackupPath = dialog.FileName;
            }
        }

        [RelayCommand]
        public void LoadSelectedProfile()
        {
            if (SelectedProfile != null)
            {
                CurrentSettings = (ConnectionSettings)SelectedProfile.Clone();
                SelectedProfile.LastUsed = DateTime.Now;
                CurrentSettings.LastUsed = DateTime.Now;

                _logService.LogMessage($"Profile '{SelectedProfile.ProfileName}' loaded successfully.", "INFO");

                DialogResult = true;
            }
            else
            {
                MessageBox.Show("Please select a profile to load.", "Load Profile", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        [RelayCommand]
        public void Cancel()
        {
            DialogResult = false;
        }

        private async Task LoadConnectionProfiles()
        {
            try
            {
                ConnectionProfile loadedProfile = await ConnectionManager.LoadConnectionProfilesAsync(_logService);

                if (loadedProfile != null)
                {
                    Profiles.Clear();

                    foreach (var profile in loadedProfile.Profiles)
                    {
                        Profiles.Add(profile);
                    }
                }
                _logService.LogMessage("Connection profiles loaded successfully.", "INFO");
            }
            catch (Exception ex)
            {
                _logService.LogMessage($"Failed to load connection profiles: {ex.Message}", "ERROR");
                MessageBox.Show($"Failed to load connection profiles: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        partial void OnSelectedProfileChanged(ConnectionSettings value)
        {
            if (value != null)
            {
                CurrentSettings = (ConnectionSettings)value.Clone();
            }
        }
    }
}
