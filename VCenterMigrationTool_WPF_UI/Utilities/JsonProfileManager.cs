using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using VCenterMigrationTool_WPF_UI.Infrastructure.Interfaces;
using VCenterMigrationTool_WPF_UI.Models;

namespace VCenterMigrationTool_WPF_UI.Utilities
{
    /// <summary>
    /// Simple JSON‐backed store for ConnectionProfile list.
    /// </summary>
    public class JsonProfileManager : IProfileManager
    {
        private readonly string _filePath;
        private readonly List<ConnectionProfile> _profiles;

        public JsonProfileManager()
        {
            _filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "profiles.json");
            if (File.Exists(_filePath))
            {
                var json = File.ReadAllText(_filePath);
                _profiles = JsonSerializer
                  .Deserialize<List<ConnectionProfile>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                  ?? new List<ConnectionProfile>();
            }
            else
            {
                _profiles = new List<ConnectionProfile>();
            }
        }

        public IEnumerable<ConnectionProfile> GetAllProfiles()
            => _profiles.ToList();

        public ConnectionProfile GetProfile(string profileName)
            => _profiles.FirstOrDefault(p => p.Name == profileName);

        public void SaveProfile(ConnectionProfile profile)
        {
            var existing = _profiles.FirstOrDefault(p => p.Name == profile.Name);
            if (existing != null)
                _profiles.Remove(existing);

            _profiles.Add(profile);
            Persist();
        }

        public void DeleteProfile(string profileName)
        {
            var existing = _profiles.FirstOrDefault(p => p.Name == profileName);
            if (existing != null)
            {
                _profiles.Remove(existing);
                Persist();
            }
        }

        private void Persist()
        {
            var json = JsonSerializer.Serialize(_profiles, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_filePath, json);
        }
    }
}
