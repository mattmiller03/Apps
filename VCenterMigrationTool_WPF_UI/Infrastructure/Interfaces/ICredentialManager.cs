using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using VCenterMigrationTool_WPF_UI.Infrastructure;
using VCenterMigrationTool_WPF_UI.Models;
using VCenterMigrationTool_WPF_UI.Utilities;

namespace VCenterMigrationTool_WPF_UI.Infrastructure.Interfaces
{
    public interface ICredentialManager
    {
        void SavePassword(string profileName, string username, SecureString password);
        SecureString GetPassword(string profileName);
        void DeletePassword(string profileName);
    }

    // Windows Credential Manager implementation
    public class WindowsCredentialManager : ICredentialManager
    {
        public void SavePassword(string profileName, string username, SecureString password)
        {
            var credential = new Credential
            {
                Target = $"VCenterMigration_{profileName}",
                Username = username,
                SecurePassword = password,
                Persistence = Persistence.LocalMachine
            };
            credential.Save();
        }

        public SecureString GetPassword(string profileName)
        {
            var credential = Credential.Load($"VCenterMigration_{profileName}");
            return credential?.SecurePassword;
        }

        public void DeletePassword(string profileName)
        {
            Credential.Delete($"VCenterMigration_{profileName}");
        }
    }
}
