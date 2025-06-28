using System.IO;
using System.Text.Json;
using VCenterMigrationTool_WPF_UI.Models;
using VCenterMigrationTool_WPF_UI.Utilities;

namespace VCenterMigrationTool_WPF_UI
{
    public static class ConnectionManager
    {
        private static readonly string SettingsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "VCenterMigrationTool",
            "ConnectionProfiles.json"
        );

        public static async Task<ConnectionProfile> LoadConnectionProfilesAsync()
        {
            try
            {
                if (!File.Exists(SettingsPath))
                    return new ConnectionProfile();

                string json = await File.ReadAllTextAsync(SettingsPath);
                var profile = JsonSerializer.Deserialize<ConnectionProfile>(json);

                if (profile != null)
                {
                    // Decrypt passwords
                    foreach (var settings in profile.Profiles)
                    {
                        if (!string.IsNullOrEmpty(settings.SourcePassword))
                            settings.SourcePassword = SecurityHelper.DecryptString(settings.SourcePassword);

                        if (!string.IsNullOrEmpty(settings.DestinationPassword))
                            settings.DestinationPassword = SecurityHelper.DecryptString(settings.DestinationPassword);
                    }
                    return profile;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading profiles: {ex.Message}");
            }

            return new ConnectionProfile();
        }

        public static async Task SaveConnectionProfilesAsync(ConnectionProfile profile)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);

                // Clone to avoid modifying original
                var profileToSave = JsonSerializer.Deserialize<ConnectionProfile>(
                    JsonSerializer.Serialize(profile));

                if (profileToSave != null)
                {
                    // Encrypt passwords before saving
                    foreach (var settings in profileToSave.Profiles)
                    {
                        settings.SourcePassword = settings.SaveSourcePassword
                            ? SecurityHelper.EncryptString(settings.SourcePassword)
                            : string.Empty;

                        settings.DestinationPassword = settings.SaveDestinationPassword
                            ? SecurityHelper.EncryptString(settings.DestinationPassword)
                            : string.Empty;
                    }

                    await File.WriteAllTextAsync(
                        SettingsPath,
                        JsonSerializer.Serialize(profileToSave, new JsonSerializerOptions { WriteIndented = true })
                    );
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to save profiles: {ex.Message}", ex);
            }
        }
    }
}