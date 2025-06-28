using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using VCenterMigrationTool_WPF_UI.Models;
using VCenterMigrationTool_WPF_UI.Utilities;

namespace VCenterMigrationTool_WPF_UI.ViewModels
{
    public class ConnectionSettingsViewModel : INotifyPropertyChanged
    {
        private readonly IConnectionManager _connectionManager;
        private readonly IDialogService _dialogService;  // Inject dialog service

        private ConnectionProfile _connectionProfile = new();
        private ConnectionSettings? _selectedProfile;
        private bool _isLoading = false;
        private bool? _dialogResult;

        public ConnectionSettingsViewModel(IConnectionManager connectionManager, IDialogService dialogService)
        {
            _connectionManager = connectionManager ?? throw new ArgumentNullException(nameof(connectionManager));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));

            // Initialize Commands
            NewProfileCommand = new RelayCommand(NewProfile);
            DeleteProfileCommand = new RelayCommand(DeleteProfile, CanDeleteProfile);
            BrowsePathCommand = new RelayCommand(BrowsePath);
            SaveProfileCommand = new RelayCommand(async () => await SaveProfile());
            LoadProfileCommand = new RelayCommand(async () => await LoadProfile(), CanLoadProfile);
            CancelCommand = new RelayCommand(Cancel);

            // Load the connection profiles
            LoadProfilesAsync();
        }

        public ICommand NewProfileCommand { get; }
        public ICommand DeleteProfileCommand { get; }
        public ICommand BrowsePathCommand { get; }
        public ICommand SaveProfileCommand { get; }
        public ICommand LoadProfileCommand { get; }
        public ICommand CancelCommand { get; }

        public ObservableCollection<ConnectionSettings> Profiles { get; } = new ObservableCollection<ConnectionSettings>();

        public ConnectionSettings? SelectedProfile
        {
            get { return _selectedProfile; }
            set
            {
                if (_selectedProfile != value)
                {
                    _selectedProfile = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool? DialogResult
        {
            get { return _dialogResult; }
            set { _dialogResult = value; OnPropertyChanged(); }
        }

        private async void LoadProfilesAsync()
        {
            try
            {
                _isLoading = true;
                _connectionProfile = await _connectionManager.LoadConnectionProfilesAsync();

                // Clear existing profiles and add the loaded profiles
                Profiles.Clear();
                foreach (var profile in _connectionProfile.Profiles)
                {
                    Profiles.Add(profile);
                }

                // Select the last used profile or first profile
                var lastUsedProfile = Profiles
                    .FirstOrDefault(p => p.ProfileName == _connectionProfile.LastUsedProfile)
                    ?? Profiles.FirstOrDefault();

                SelectedProfile = lastUsedProfile;
            }
            catch (Exception ex)
            {
                // Log or display error message
                Console.WriteLine($"Error loading connection profiles: {ex.Message}");
            }
            finally
            {
                _isLoading = false;
            }
        }

        private void NewProfile()
        {
            var newProfile = new ConnectionSettings
            {
                ProfileName = $"Profile {Profiles.Count + 1}",
                SourceServer = "vcenter7.domain.local",
                SourceUsername = "administrator@vsphere.local",
                DestinationServer = "vcenter8.domain.local",
                DestinationUsername = "administrator@vsphere.local",
                BackupPath = @"C:\VCenterMigration\Backup",
                LastUsed = DateTime.Now
            };

            Profiles.Add(newProfile);
            SelectedProfile = newProfile;
        }

        private bool CanDeleteProfile()
        {
            return SelectedProfile != null && Profiles.Count > 1; // Disable if it's the last profile
        }

        private void DeleteProfile()
        {
            if (SelectedProfile != null)
            {
                Profiles.Remove(SelectedProfile);
                SelectedProfile = Profiles.FirstOrDefault(); // Select the first profile if any
            }
        }

        private void BrowsePath()
        {
            string description = "Select default backup path";
            string selectedPath = SelectedProfile?.BackupPath ?? string.Empty; // Use selected profile's path

            string? newPath = _dialogService.ShowFolderBrowserDialog(description, selectedPath);

            if (!string.IsNullOrEmpty(newPath))
            {
                if (SelectedProfile != null)
                {
                    SelectedProfile.BackupPath = newPath;
                }
            }
        }

        private async Task SaveProfile()
        {
            try
            {
                if (SelectedProfile != null)
                {
                    _connectionProfile.LastUsedProfile = SelectedProfile.ProfileName;
                }
                _connectionProfile.Profiles.Clear();
                foreach (var profile in Profiles)
                {
                    _connectionProfile.Profiles.Add(profile);
                }
                await _connectionManager.SaveConnectionProfilesAsync(_connectionProfile);
            }
            catch (Exception ex)
            {
                // Log or display error message
                Console.WriteLine($"Error saving connection profiles: {ex.Message}");
            }
        }

        private bool CanLoadProfile()
        {
            return SelectedProfile != null;
        }

        private async Task LoadProfile()
        {
            await SaveProfile();
            DialogResult = true; // Set DialogResult to true to indicate success
        }

        private void Cancel()
        {
            DialogResult = false; // Set DialogResult to false to indicate cancellation
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
