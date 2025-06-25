using System.Windows;
using System.Windows.Controls;
using Ookii.Dialogs.Wpf;

namespace VCenterMigrationTool_WPF_UI;

public partial class ConnectionSettingsWindow : Window
{
    private ConnectionProfile _connectionProfile = new();
    private ConnectionSettings? _currentSettings;
    private bool _isLoading = false;

    public ConnectionSettings? SelectedSettings { get; private set; }

    public ConnectionSettingsWindow()
    {
        InitializeComponent();
        _ = LoadProfilesAsync();
    }

    private async Task LoadProfilesAsync()
    {
        try
        {
            _isLoading = true;
            _connectionProfile = await ConnectionManager.LoadConnectionProfilesAsync();

            if (_connectionProfile.Profiles.Count == 0)
            {
                // Create default profile
                _connectionProfile.Profiles.Add(new ConnectionSettings
                {
                    ProfileName = "Default",
                    SourceServer = "vcenter7.domain.local",
                    SourceUsername = "administrator@vsphere.local",
                    DestinationServer = "vcenter8.domain.local",
                    DestinationUsername = "administrator@vsphere.local",
                    BackupPath = @"C:\VCenterMigration\Backup"
                });
            }

            ProfilesListBox.ItemsSource = _connectionProfile.Profiles;

            // Select the last used profile or first profile
            var lastUsedProfile = _connectionProfile.Profiles
                .FirstOrDefault(p => p.ProfileName == _connectionProfile.LastUsedProfile)
                ?? _connectionProfile.Profiles.First();

            ProfilesListBox.SelectedItem = lastUsedProfile;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading connection profiles: {ex.Message}",
                          "Load Error", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        finally
        {
            _isLoading = false;
        }
    }

    private void ProfilesListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isLoading || ProfilesListBox.SelectedItem is not ConnectionSettings settings)
            return;

        _isLoading = true;
        _currentSettings = settings;

        ProfileNameTextBox.Text = settings.ProfileName;
        SourceServerTextBox.Text = settings.SourceServer;
        SourceUsernameTextBox.Text = settings.SourceUsername;
        SourcePasswordBox.Password = settings.SourcePassword;
        SaveSourcePasswordCheckBox.IsChecked = settings.SaveSourcePassword;

        DestServerTextBox.Text = settings.DestinationServer;
        DestUsernameTextBox.Text = settings.DestinationUsername;
        DestPasswordBox.Password = settings.DestinationPassword;
        SaveDestPasswordCheckBox.IsChecked = settings.SaveDestinationPassword;

        BackupPathTextBox.Text = settings.BackupPath;

        _isLoading = false;
    }

    private void ProfileSettings_Changed(object sender, RoutedEventArgs e)
    {
        if (_isLoading || _currentSettings == null) return;

        _currentSettings.ProfileName = ProfileNameTextBox.Text;
        _currentSettings.SourceServer = SourceServerTextBox.Text;
        _currentSettings.SourceUsername = SourceUsernameTextBox.Text;
        _currentSettings.SourcePassword = SourcePasswordBox.Password;
        _currentSettings.SaveSourcePassword = SaveSourcePasswordCheckBox.IsChecked ?? false;

        _currentSettings.DestinationServer = DestServerTextBox.Text;
        _currentSettings.DestinationUsername = DestUsernameTextBox.Text;
        _currentSettings.DestinationPassword = DestPasswordBox.Password;
        _currentSettings.SaveDestinationPassword = SaveDestPasswordCheckBox.IsChecked ?? false;

        _currentSettings.BackupPath = BackupPathTextBox.Text;
        _currentSettings.LastUsed = DateTime.Now;

        // Refresh the list display
        ProfilesListBox.Items.Refresh();
    }

    private void NewProfile_Click(object sender, RoutedEventArgs e)
    {
        var newProfile = new ConnectionSettings
        {
            ProfileName = $"Profile {_connectionProfile.Profiles.Count + 1}",
            SourceServer = "vcenter7.domain.local",
            SourceUsername = "administrator@vsphere.local",
            DestinationServer = "vcenter8.domain.local",
            DestinationUsername = "administrator@vsphere.local",
            BackupPath = @"C:\VCenterMigration\Backup",
            LastUsed = DateTime.Now
        };

        _connectionProfile.Profiles.Add(newProfile);
        ProfilesListBox.Items.Refresh();
        ProfilesListBox.SelectedItem = newProfile;
    }

    private void DeleteProfile_Click(object sender, RoutedEventArgs e)
    {
        if (ProfilesListBox.SelectedItem is not ConnectionSettings settings) return;

        if (_connectionProfile.Profiles.Count <= 1)
        {
            MessageBox.Show("Cannot delete the last profile. At least one profile must exist.",
                          "Cannot Delete", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var result = MessageBox.Show($"Are you sure you want to delete the profile '{settings.ProfileName}'?",
                                   "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            _connectionProfile.Profiles.Remove(settings);
            ProfilesListBox.Items.Refresh();
            ProfilesListBox.SelectedIndex = 0;
        }
    }

    private void BrowsePath_Click(object sender, RoutedEventArgs e)
    {
        var selectedPath = SelectFolder("Select default backup path", BackupPathTextBox.Text);
        if (!string.IsNullOrEmpty(selectedPath))
        {
            BackupPathTextBox.Text = selectedPath;
            ProfileSettings_Changed(sender, e);
        }
    }

    private static string? SelectFolder(string description, string selectedPath)
    {
        try
        {
            var dialog = new Ookii.Dialogs.Wpf.VistaFolderBrowserDialog
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
        var dialog = new Microsoft.Win32.OpenFileDialog
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
            return System.IO.Path.GetDirectoryName(dialog.FileName);
        }

        return null;
    }

    private async void SaveProfile_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (_currentSettings != null)
            {
                _connectionProfile.LastUsedProfile = _currentSettings.ProfileName;
            }

            await ConnectionManager.SaveConnectionProfilesAsync(_connectionProfile);
            MessageBox.Show("Connection profiles saved successfully!",
                          "Save Complete", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error saving connection profiles: {ex.Message}",
                          "Save Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void LoadProfile_Click(object sender, RoutedEventArgs e)
    {
        if (ProfilesListBox.SelectedItem is not ConnectionSettings settings) return;

        try
        {
            _connectionProfile.LastUsedProfile = settings.ProfileName;
            await ConnectionManager.SaveConnectionProfilesAsync(_connectionProfile);

            SelectedSettings = settings;
            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading profile: {ex.Message}",
                          "Load Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
