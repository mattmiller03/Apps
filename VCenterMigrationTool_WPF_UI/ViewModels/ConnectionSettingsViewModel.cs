using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security;
using VCenterMigrationTool_WPF_UI.Infrastructure.Interfaces;
using VCenterMigrationTool_WPF_UI.Models;
using VCenterMigrationTool_WPF_UI.Utilities;

namespace VCenterMigrationTool_WPF_UI.ViewModels
{
    public class ConnectionSettingsViewModel : INotifyPropertyChanged
    {
        private readonly IProfileManager _profileManager;
        private readonly ICredentialManager _credentialManager;

        private string _searchFilter;
        private ConnectionProfile _selectedProfile;
        private SecureString _selectedProfilePassword;

        public ObservableCollection<ConnectionProfile> Profiles { get; }
            = new ObservableCollection<ConnectionProfile>();

        public IEnumerable<ConnectionProfile> FilteredProfiles =>
            string.IsNullOrWhiteSpace(_searchFilter)
                ? Profiles
                : Profiles.Where(p =>
                    p.Name.Contains(_searchFilter, StringComparison.OrdinalIgnoreCase) ||
                    p.SourceVCenter.Contains(_searchFilter, StringComparison.OrdinalIgnoreCase) ||
                    p.DestinationVCenter.Contains(_searchFilter, StringComparison.OrdinalIgnoreCase)
                );

        public string SearchFilter
        {
            get => _searchFilter;
            set
            {
                if (SetProperty(ref _searchFilter, value))
                    OnPropertyChanged(nameof(FilteredProfiles));
            }
        }

        public ConnectionProfile SelectedProfile
        {
            get => _selectedProfile;
            set
            {
                if (SetProperty(ref _selectedProfile, value))
                {
                    // Load the stored password when profile changes
                    SelectedProfilePassword = _selectedProfile != null
                        ? _credentialManager.GetPassword(_selectedProfile.Name)
                        : new SecureString();

                    // Reevaluate commands
                    SaveProfileCommand.RaiseCanExecuteChanged();
                    DeleteProfileCommand.RaiseCanExecuteChanged();
                }
            }
        }

        public SecureString SelectedProfilePassword
        {
            get => _selectedProfilePassword;
            set => SetProperty(ref _selectedProfilePassword, value);
        }

        // Use RelayCommand so we can call RaiseCanExecuteChanged()
        public RelayCommand NewProfileCommand { get; }
        public RelayCommand SaveProfileCommand { get; }
        public RelayCommand DeleteProfileCommand { get; }

        public ConnectionSettingsViewModel(
            IProfileManager profileManager,
            ICredentialManager credentialManager)
        {
            _profileManager = profileManager;
            _credentialManager = credentialManager;

            NewProfileCommand = new RelayCommand(ExecuteNewProfile);
            SaveProfileCommand = new RelayCommand(ExecuteSaveProfile, CanSaveProfile);
            DeleteProfileCommand = new RelayCommand(ExecuteDeleteProfile, CanDeleteProfile);

            LoadProfiles();
        }

        private void LoadProfiles()
        {
            Profiles.Clear();
            foreach (var p in _profileManager
                             .GetAllProfiles()
                             .OrderBy(x => x.Name))
            {
                Profiles.Add(p);
            }
            OnPropertyChanged(nameof(FilteredProfiles));
            SaveProfileCommand.RaiseCanExecuteChanged();
            DeleteProfileCommand.RaiseCanExecuteChanged();
        }

        private void ExecuteNewProfile()
        {
            var p = new ConnectionProfile
            {
                Name = "New Profile",
                SourceVCenter = "",
                SourceUsername = "",
                DestinationVCenter = "",
                DestinationUsername = ""
            };
            Profiles.Add(p);
            SelectedProfile = p;
        }

        private bool CanSaveProfile() =>
            SelectedProfile != null
         && !string.IsNullOrWhiteSpace(SelectedProfile.Name)
         && !string.IsNullOrWhiteSpace(SelectedProfile.SourceVCenter)
         && !string.IsNullOrWhiteSpace(SelectedProfile.SourceUsername)
         && !string.IsNullOrWhiteSpace(SelectedProfile.DestinationVCenter)
         && !string.IsNullOrWhiteSpace(SelectedProfile.DestinationUsername);

        private void ExecuteSaveProfile()
        {
            // 1) Save metadata
            _profileManager.SaveProfile(SelectedProfile);

            // 2) Save password separately
            _credentialManager.SavePassword(
                SelectedProfile.Name,
                SelectedProfile.SourceUsername,
                SelectedProfilePassword);

            LoadProfiles();
        }

        private bool CanDeleteProfile() => SelectedProfile != null;

        private void ExecuteDeleteProfile()
        {
            // You can prompt in the view if you want
            _profileManager.DeleteProfile(SelectedProfile.Name);
            _credentialManager.DeletePassword(SelectedProfile.Name);
            LoadProfiles();
            SelectedProfile = null;
        }

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));

        protected bool SetProperty<T>(
            ref T backing, T value, [CallerMemberName] string propName = null)
        {
            if (EqualityComparer<T>.Default.Equals(backing, value))
                return false;
            backing = value;
            OnPropertyChanged(propName);
            return true;
        }
        #endregion
    }
}
