using System.Security;

namespace VCenterMigrationTool_WPF_UI.Infrastructure.Interfaces
{
    public interface ICredentialManager
    {
        void SavePassword(string profileName, string username, SecureString password);
        SecureString GetPassword(string profileName);
        void DeletePassword(string profileName);
    }
}
