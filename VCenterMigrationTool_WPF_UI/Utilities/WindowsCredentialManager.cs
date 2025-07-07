using System.Security;
using CredentialManagement;
using VCenterMigrationTool_WPF_UI.Infrastructure.Interfaces;

namespace VCenterMigrationTool_WPF_UI.Utilities
{
    public class WindowsCredentialManager : ICredentialManager
    {
        public void SavePassword(string profileName, string username, SecureString password)
        {
            var cred = new Credential
            {
                Target = $"VCenterMigration_{profileName}",
                Username = username,
                SecurePassword = password,
                Persistence = Persistence.LocalMachine
            };
            cred.Save();
        }

        public SecureString GetPassword(string profileName)
        {
            var cred = Credential.Load($"VCenterMigration_{profileName}");
            return cred?.SecurePassword;
        }

        public void DeletePassword(string profileName)
        {
            Credential.Delete($"VCenterMigration_{profileName}");
        }
    }
}
