using System.Collections.Generic;
using VCenterMigrationTool_WPF_UI.Models;

namespace VCenterMigrationTool_WPF_UI.Infrastructure.Interfaces
{
    /// <summary>
    /// Manages loading/saving of ConnectionProfile metadata.
    /// Passwords should be handled separately via ICredentialManager.
    /// </summary>
    public interface IProfileManager
    {
        IEnumerable<ConnectionProfile> GetAllProfiles();
        ConnectionProfile GetProfile(string profileName);
        void SaveProfile(ConnectionProfile profile);
        void DeleteProfile(string profileName);
    }
}
