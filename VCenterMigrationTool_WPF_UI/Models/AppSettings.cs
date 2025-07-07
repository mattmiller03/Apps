using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCenterMigrationTool_WPF_UI.Models
{
    /// <summary>
    /// Bind this to the "AppSettings" section in appsettings.json
    /// </summary>
    public class AppSettings
    {
        /// <summary>
        /// Relative path (from exe folder) to your PS scripts.
        /// </summary>
        public string ScriptsFolder { get; set; }

        /// <summary>
        /// Where profiles.json lives if you want to override.
        /// </summary>
        public string ProfileStorePath { get; set; }
        // TODO: add any other settings you read from appsettings.json,
        // e.g. connection timeouts, log levels, etc.

        public string DefaultSourceVCenter { get; set; }
        public string DefaultDestinationVCenter { get; set; }
        public int ConnectionTimeoutSeconds { get; set; } = 30;
        public bool EnableDetailedLogging { get; set; } = true;
        public string LogDirectory { get; set; } = "Logs";
        // Add other settings as needed
    }
}
