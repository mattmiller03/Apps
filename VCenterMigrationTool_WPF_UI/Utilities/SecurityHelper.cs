using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace VCenterMigrationTool_WPF_UI.Utilities
{
    public static class SecurityHelper
    {
        // Use a more secure key management approach in production
        private static readonly byte[] EncryptionKey = Encoding.UTF8.GetBytes("VCenterMigration2024!SecureKey123".PadRight(32).Substring(0, 32));
        private static readonly byte[] IV = new byte[16]; // Initialization Vector

        /// <summary>
        /// Encrypts a string using AES-256
        /// </summary>
        public static string EncryptString(string plainText)
        {
            if (string.IsNullOrEmpty(plainText))
                return plainText;

            try
            {
                using (Aes aes = Aes.Create())
                {
                    aes.Key = EncryptionKey;
                    aes.IV = IV;

                    ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

                    using (MemoryStream ms = new MemoryStream())
                    using (CryptoStream cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                    {
                        using (StreamWriter sw = new StreamWriter(cs))
                        {
                            sw.Write(plainText);
                        }
                        return Convert.ToBase64String(ms.ToArray());
                    }
                }
            }
            catch
            {
                // Fallback: return original if encryption fails (avoid breaking the app)
                return plainText;
            }
        }

        /// <summary>
        /// Decrypts an AES-256 encrypted string
        /// </summary>
        public static string DecryptString(string cipherText)
        {
            if (string.IsNullOrEmpty(cipherText))
                return cipherText;

            try
            {
                byte[] buffer = Convert.FromBase64String(cipherText);

                using (Aes aes = Aes.Create())
                {
                    aes.Key = EncryptionKey;
                    aes.IV = IV;

                    ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

                    using (MemoryStream ms = new MemoryStream(buffer))
                    using (CryptoStream cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                    using (StreamReader sr = new StreamReader(cs))
                    {
                        return sr.ReadToEnd();
                    }
                }
            }
            catch
            {
                // Fallback: return original if decryption fails
                return cipherText;
            }
        }
    }
}