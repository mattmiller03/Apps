using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCenterMigrationTool_WPF_UI.Models
{
    public class AppSettings
    {
        public string DefaultSourceVCenter { get; set; }
        public string DefaultDestinationVCenter { get; set; }
        public int ConnectionTimeoutSeconds { get; set; } = 30;
        public bool EnableDetailedLogging { get; set; } = true;
        public string LogDirectory { get; set; } = "Logs";
        // Add other settings as needed
    }
}
