using System;
using System.Security;

namespace VCenterMigrationTool_WPF_UI.Models
{
    public class MigrationCredentials
    {
        public string? SSOAdminUsername { get; set; }
        public SecureString? SSOAdminPassword { get; set; }
        public string? ESXiUsername { get; set; }
        public SecureString? ESXiPassword { get; set; }
        public string? ServiceAccountUsername { get; set; }
        public SecureString? ServiceAccountPassword { get; set; }
        public string? DomainAdminUsername { get; set; }
        public SecureString? DomainAdminPassword { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.Now;
    }
}
