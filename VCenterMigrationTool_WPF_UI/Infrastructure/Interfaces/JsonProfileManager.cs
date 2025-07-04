using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCenterMigrationTool_WPF_UI.Infrastructure.Interfaces
{
    public class JsonProfileManager : IProfileManager
    {
        private readonly string _profilesPath;
        private readonly ICredentialManager _credentialManager;

        public JsonProfileManager(ICredentialManager credentialManager)
        {
            _credentialManager = credentialManager;
            _profilesPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "VCenterMigrationTool",
                "profiles.json");
        }

        public IEnumerable<ConnectionProfile> GetAllProfiles()
        {
            if (!File.Exists(_profilesPath))
                return Enumerable.Empty<ConnectionProfile>();

            var json = File.ReadAllText(_profilesPath);
            var profiles = JsonConvert.DeserializeObject<List<ConnectionProfile>>(json)
                         ?? new List<ConnectionProfile>();

            // Load passwords from secure storage
            foreach (var profile in profiles)
            {
                profile.SecurePassword = _credentialManager.GetPassword(profile.ProfileName);
            }

            return profiles;
        }

        public void SaveProfile(ConnectionProfile profile)
        {
            var profiles = GetAllProfiles().ToList();
            var existing = profiles.FirstOrDefault(p => p.ProfileName == profile.ProfileName);

            if (existing != null)
            {
                profiles.Remove(existing);
            }

            profiles.Add(profile);
            SaveAllProfiles(profiles);

            // Save password securely
            if (profile.SecurePassword != null)
            {
                _credentialManager.SavePassword(
                    profile.ProfileName,
                    profile.Username,
                    profile.SecurePassword);
            }
        }

        private void SaveAllProfiles(IEnumerable<ConnectionProfile> profiles)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_profilesPath));

            // Don't serialize passwords to JSON
            var sanitizedProfiles = profiles.Select(p => new {
                p.ProfileName,
                p.ServerAddress,
                p.Username,
                p.CreatedDate,
                p.LastModified
            }).ToList();

            var json = JsonConvert.SerializeObject(sanitizedProfiles, Formatting.Indented);
            File.WriteAllText(_profilesPath, json);
        }

        public void DeleteProfile(string name)
        {
            var profiles = GetAllProfiles().ToList();
            var profile = profiles.FirstOrDefault(p => p.ProfileName == name);

            if (profile != null)
            {
                profiles.Remove(profile);
                SaveAllProfiles(profiles);
                _credentialManager.DeletePassword(name);
            }
        }
    }
}
