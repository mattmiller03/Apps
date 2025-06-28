using System;

namespace VCenterMigrationTool_WPF_UI.Models
{
    public class ConnectionSettings
    {
        public string ProfileName { get; set; } = string.Empty;
        public string SourceServer { get; set; } = string.Empty;
        public string SourceUsername { get; set; } = string.Empty;
        public string SourcePassword { get; set; } = string.Empty;
        public bool SaveSourcePassword { get; set; }

        public string DestinationServer { get; set; } = string.Empty;
        public string DestinationUsername { get; set; } = string.Empty;
        public string DestinationPassword { get; set; } = string.Empty;
        public bool SaveDestinationPassword { get; set; }

        public string BackupPath { get; set; } = string.Empty;
        public DateTime LastUsed { get; set; }
    }
}
