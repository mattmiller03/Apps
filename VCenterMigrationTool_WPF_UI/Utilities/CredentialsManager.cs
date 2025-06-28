using System;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using VCenterMigrationTool_WPF_UI.Models;
using VCenterMigrationTool_WPF_UI.Utilities;

namespace VCenterMigrationTool_WPF_UI.Utilities
{
    public static class CredentialsManager
    {
        private static MigrationCredentials? _sessionCredentials;

        public static void SaveCredentials(MigrationCredentials credentials)
        {
            _sessionCredentials = credentials;
        }

        public static MigrationCredentials? LoadCredentials()
        {
            return _sessionCredentials;
        }

        public static void ClearCredentials()
        {
            _sessionCredentials?.SSOAdminPassword?.Dispose();
            _sessionCredentials?.ESXiPassword?.Dispose();
            _sessionCredentials?.ServiceAccountPassword?.Dispose();
            _sessionCredentials?.DomainAdminPassword?.Dispose();
            _sessionCredentials = null;
        }

        // Converts SecureString to plaintext (uses SecurityHelper for optional encryption)
        public static string SecureStringToString(SecureString secureString)
        {
            if (secureString == null)
                return string.Empty;

            IntPtr ptr = System.Runtime.InteropServices.Marshal.SecureStringToBSTR(secureString);
            try
            {
                string plainText = System.Runtime.InteropServices.Marshal.PtrToStringBSTR(ptr);
                return SecurityHelper.EncryptString(plainText); // Encrypt in memory for extra safety
            }
            finally
            {
                System.Runtime.InteropServices.Marshal.ZeroFreeBSTR(ptr);
            }
        }

        // Converts encrypted string back to SecureString
        public static SecureString StringToSecureString(string encryptedText)
        {
            var secureString = new SecureString();
            if (string.IsNullOrEmpty(encryptedText))
                return secureString;

            string decrypted = SecurityHelper.DecryptString(encryptedText);
            foreach (char c in decrypted)
                secureString.AppendChar(c);

            secureString.MakeReadOnly();
            return secureString;
        }
    }
}
