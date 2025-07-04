using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using VCenterMigrationTool_WPF_UI;
using Microsoft.Extensions.DependencyInjection;
using System.Security;
using VCenterMigrationTool_WPF_UI.Infrastructure;
using VCenterMigrationTool_WPF_UI.Models;
using VCenterMigrationTool_WPF_UI.Utilities;

public class ConnectionSettingsViewModel : INotifyPropertyChanged
{
    private readonly IProfileManager _profileManager;
    private string _searchFilter;

    public ObservableCollection<ConnectionProfile> Profiles { get; } = new ObservableCollection<ConnectionProfile>();

    public IEnumerable<ConnectionProfile> FilteredProfiles =>
        string.IsNullOrWhiteSpace(_searchFilter)
            ? Profiles
            : Profiles.Where(p => p.ProfileName.Contains(_searchFilter, StringComparison.OrdinalIgnoreCase) ||
                                 p.ServerAddress.Contains(_searchFilter, StringComparison.OrdinalIgnoreCase));

    public string SearchFilter
    {
        get => _searchFilter;
        set
        {
            _searchFilter = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(FilteredProfiles));
        }
    }

    private ConnectionProfile _selectedProfile;
    public ConnectionProfile SelectedProfile
    {
        get => _selectedProfile;
        set
        {
            _selectedProfile = value;
            OnPropertyChanged();
            ClearPasswordField();
        }
    }

    // Commands
    public ICommand NewProfileCommand { get; }
    public ICommand SaveProfileCommand { get; }
    public ICommand DeleteProfileCommand { get; }

    public ConnectionSettingsViewModel(IProfileManager profileManager)
    {
        _profileManager = profileManager;

        NewProfileCommand = new RelayCommand(NewProfile);
        SaveProfileCommand = new RelayCommand(SaveProfile, CanSaveProfile);
        DeleteProfileCommand = new RelayCommand(DeleteProfile, CanDeleteProfile);

        LoadProfiles();
    }

    private void LoadProfiles()
    {
        Profiles.Clear();
        foreach (var profile in _profileManager.GetAllProfiles().OrderBy(p => p.ProfileName))
        {
            Profiles.Add(profile);
        }
        OnPropertyChanged(nameof(FilteredProfiles));
    }

    private void NewProfile()
    {
        SelectedProfile = new ConnectionProfile
        {
            ProfileName = "New Profile",
            CreatedDate = DateTime.Now,
            LastModified = DateTime.Now
        };
        Profiles.Add(SelectedProfile);
        OnPropertyChanged(nameof(FilteredProfiles));
    }

    private bool CanSaveProfile()
    {
        return SelectedProfile != null &&
               !string.IsNullOrWhiteSpace(SelectedProfile.ProfileName) &&
               !string.IsNullOrWhiteSpace(SelectedProfile.ServerAddress) &&
               !string.IsNullOrWhiteSpace(SelectedProfile.Username);
    }

    private bool CanDeleteProfile() => SelectedProfile != null;

    private void SaveProfile()
    {
        if (SelectedProfile == null) return;

        SelectedProfile.LastModified = DateTime.Now;
        _profileManager.SaveProfile(SelectedProfile);
        LoadProfiles(); // Refresh list
    }

    private void DeleteProfile()
    {
        if (SelectedProfile == null) return;

        if (MessageBox.Show($"Delete profile '{SelectedProfile.ProfileName}'?",
            "Confirm Delete", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
        {
            _profileManager.DeleteProfile(SelectedProfile.ProfileName);
            LoadProfiles();
            SelectedProfile = null;
        }
    }

    private void ClearPasswordField()
    {
        if (Application.Current.MainWindow is MainWindow mainWindow)
        {
            var pwdBox = mainWindow.FindName("pwdPassword") as PasswordBox;
            pwdBox?.Clear();
        }
    }

    public event PropertyChangedEventHandler PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

