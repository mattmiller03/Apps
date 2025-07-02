using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using VCenterMigrationTool_WPF_UI.Models;
using VCenterMigrationTool_WPF_UI.Utilities;
using System.Linq;
using System.Windows;

namespace VCenterMigrationTool_WPF_UI.Utilities
{
    public static class ConnectionManager
    {
        private static readonly string SettingsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "VCenterMigrationTool",
            "ConnectionProfiles.json");

        public static async Task<ConnectionProfile> LoadConnectionProfilesAsync(ILogService logService)
        {
            try
            {
                if (!File.Exists(SettingsPath))
                    return new ConnectionProfile();

                var json = await File.ReadAllTextAsync(SettingsPath);
                return JsonSerializer.Deserialize<ConnectionProfile>(json) ?? new ConnectionProfile();
            }
            catch (Exception ex)
            {
                logService.LogMessage($"Error loading profiles: {ex.Message}", "ERROR");
                return new ConnectionProfile();
            }
        }

        public static async Task SaveConnectionProfilesAsync(
            IEnumerable<ConnectionSettings> profiles,
            ILogService logService)
        {
            try
            {
                var directory = Path.GetDirectoryName(SettingsPath);
                if (!Directory.Exists(directory))
                    Directory.CreateDirectory(directory);

                var options = new JsonSerializerOptions { WriteIndented = true };
                var json = JsonSerializer.Serialize(profiles, options);
                await File.WriteAllTextAsync(SettingsPath, json);

                logService.LogMessage($"Saved {profiles.Count()} connection profiles.", "INFO");
            }
            catch (Exception ex)
            {
                logService.LogMessage($"Failed to save profiles: {ex.Message}", "ERROR");
                throw;
            }
        }


    }
}
