using System.Collections.Generic;

namespace VCenterMigrationTool_WPF_UI.Models
{
    public class ConnectionProfile
    {
        /// <summary>
        /// The name of the profile that was last used by the application.
        /// </summary>
        public string LastUsedProfile { get; set; } = string.Empty;

        /// <summary>
        /// A list of all saved connection settings profiles.
        /// </summary>
        public List<ConnectionSettings> Profiles { get; set; } = new();
    }
}
