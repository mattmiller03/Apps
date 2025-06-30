using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VCenterMigrationTool_WPF_UI.Models;
using VCenterMigrationTool_WPF_UI.Utilities;
using VCenterMigrationTool_WPF_UI.ViewModels;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Microsoft.Win32; // Add this namespace
using System.IO;          // For File operations
using System.Text.Json;

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

        private readonly PowerShellManager _psManager; // Or inject via constructor
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
            CurrentSettings = new ConnectionSettings { ProfileName = "New Profile" }; // Create a new ConnectionSettings
            SelectedProfile = null; // Clear the selected profile
        }

        [RelayCommand]
        public async Task SaveProfileAsync()
        {
            // Validate the current settings
            var results = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
            var context = new ValidationContext(CurrentSettings, serviceProvider: null, items: null);
            bool isValid = Validator.TryValidateObject(CurrentSettings, context, (ICollection<System.ComponentModel.DataAnnotations.ValidationResult>)results, validateAllProperties: true);

            if (!isValid)
            {
                string errorMessage = string.Join(Environment.NewLine, results.Select(r => r.ErrorMessage));
                _logService.LogMessage($"Validation failed: {errorMessage}", "ERROR");
                MessageBox.Show(errorMessage, "Validation Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Check if a profile with the same name already exists
            var existingProfile = Profiles.FirstOrDefault(p => p.ProfileName == CurrentSettings.ProfileName);

            if (existingProfile != null)
            {
                // Prompt the user to overwrite
                MessageBoxResult result = MessageBox.Show($"A profile with the name '{CurrentSettings.ProfileName}' already exists. Do you want to overwrite it?",
                                                            "Overwrite Profile?",
                                                            MessageBoxButton.YesNo,
                                                            MessageBoxImage.Question);

                if (result == MessageBoxResult.No)
                {
                    return; // Don't save if the user chooses not to overwrite
                }

                // Remove the existing profile
                Profiles.Remove(existingProfile);
            }

            // Clone the current settings to avoid modifying the original
            ConnectionSettings newProfile = (ConnectionSettings)CurrentSettings.Clone();

            // Add the new profile to the Profiles collection
            Profiles.Add(newProfile);

            // Save the Profiles collection to persistent storage
            await SaveConnectionProfilesAsync();

            SelectedProfile = newProfile;

            DialogResult = true; // Triggers window close
        }

        private async Task SaveConnectionProfilesAsync()
        {
            try
            {
                // Use ConnectionManager to save the profiles
                ConnectionProfile profile = new ConnectionProfile { Profiles = Profiles }; // Create a ConnectionProfile object
                await ConnectionManager.SaveConnectionProfilesAsync(profile.Profiles.ToList(), _logService);
                _logService.LogMessage("Connection profiles saved successfully.", "INFO");
            }
            catch (Exception ex)
            {
                _logService.LogMessage($"Failed to save connection profiles: {ex.Message}", "ERROR");
                MessageBox.Show($"Failed to save connection profiles: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        public async Task LoadProfileAsync()
        {
            IsLoadingProfile = true; // Disable the button
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
                IsLoadingProfile = false; // Re-enable the button
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

            // Prompt the user to confirm the deletion
            MessageBoxResult result = MessageBox.Show($"Are you sure you want to delete the profile '{SelectedProfile.ProfileName}'?",
                                                        "Delete Profile?",
                                                        MessageBoxButton.YesNo,
                                                        MessageBoxImage.Question);

            if (result == MessageBoxResult.No)
            {
                return; // Don't delete if the user chooses not to confirm
            }

            // Remove the selected profile from the Profiles collection
            Profiles.Remove(SelectedProfile);

            // Save the Profiles collection to persistent storage
            await SaveConnectionProfilesAsync();

            // Clear the SelectedProfile and CurrentSettings
            SelectedProfile = null;
            CurrentSettings = new ConnectionSettings(); // Or create a new empty one

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
        public void LoadSelectedProfile()  // Renamed this method
        {
            if (SelectedProfile != null)
            {
                // Copy the selected profile's settings to the CurrentSettings
                CurrentSettings = (ConnectionSettings)SelectedProfile.Clone();

                _logService.LogMessage($"Profile '{SelectedProfile.ProfileName}' loaded successfully.", "INFO");

                DialogResult = true; // Close the window
            }
            else
            {
                MessageBox.Show("Please select a profile to load.", "Load Profile", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        [RelayCommand]
        public void Cancel()
        {
            DialogResult = false; // Set DialogResult to false when Cancel is clicked
        }

        private async Task LoadConnectionProfiles()
        {
            try
            {
                ConnectionProfile loadedProfile = await ConnectionManager.LoadConnectionProfilesAsync(_logService);

                if (loadedProfile != null)
                {
                    // Clear existing profiles before adding the loaded ones
                    Profiles.Clear();

                    // Add the loaded profiles to the ObservableCollection
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

        // Add more commands for Save/Load profile, TestConnection, etc.
    }
}
