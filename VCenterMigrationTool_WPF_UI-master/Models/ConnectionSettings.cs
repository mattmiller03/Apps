using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCenterMigrationTool_WPF_UI;

public class ConnectionSettings
{
    public string SourceServer { get; set; } = string.Empty;
    public string SourceUsername { get; set; } = string.Empty;
    public string SourcePassword { get; set; } = string.Empty; // Note: In production, encrypt this
    public bool SaveSourcePassword { get; set; } = false;

    public string DestinationServer { get; set; } = string.Empty;
    public string DestinationUsername { get; set; } = string.Empty;
    public string DestinationPassword { get; set; } = string.Empty; // Note: In production, encrypt this
    public bool SaveDestinationPassword { get; set; } = false;

    public string BackupPath { get; set; } = @"C:\VCenterMigration\Backup";
    public DateTime LastUsed { get; set; } = DateTime.Now;
    public string ProfileName { get; set; } = "Default";
}

public class ConnectionProfile
{
    public List<ConnectionSettings> Profiles { get; set; } = new();
    public string LastUsedProfile { get; set; } = "Default";
}

