namespace VCenterMigrationTool_WPF_UI.Models
{
    /// <summary>
    /// Holds the metadata for a vCenter‐to‐vCenter connection profile.
    /// Passwords are not stored here (use ICredentialManager).
    /// </summary>
    public class ConnectionProfile
    {
        public string Name { get; set; }
        public string SourceVCenter { get; set; }
        public string SourceUsername { get; set; }
        public string DestinationVCenter { get; set; }
        public string DestinationUsername { get; set; }
    }
}
