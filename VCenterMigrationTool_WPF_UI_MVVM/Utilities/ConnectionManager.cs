using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using VCenterMigrationTool_WPF_UI.Models;
using VCenterMigrationTool_WPF_UI.Utilities;

namespace VCenterMigrationTool_WPF_UI
{
    public static class ConnectionManager
    {
        private static readonly string SettingsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "VCenterMigrationTool",
            "ConnectionProfiles.json"
        );

        // In production, use a more secure key management approach (e.g., DPAPI)
        private static readonly string EncryptionKey = "VCenterMigrationTool2024";

        public static async Task<ConnectionProfile> LoadConnectionProfilesAsync(ILogService logService)
        {
            try
            {
                if (!File.Exists(SettingsPath))
                {
                    return new ConnectionProfile();
                }

                var json = await File.ReadAllTextAsync(SettingsPath);
                var profile = JsonSerializer.Deserialize<ConnectionProfile>(json);

                if (profile != null)
                {
                    // Decrypt passwords
                    foreach (var settings in profile.Profiles)
                    {
                        settings.SourcePassword = DecryptString(settings.SourcePassword, logService);
                        settings.DestinationPassword = DecryptString(settings.DestinationPassword, logService);
                    }
                    return profile;
                }
            }
            catch (Exception ex)
            {
                logService.LogMessage($"Error loading connection profiles: {ex.Message}", "ERROR");
            }

            return new ConnectionProfile();
        }

        public static async Task SaveConnectionProfilesAsync(List<ConnectionSettings> profiles, ILogService logService)
        {
            try
            {
                // Create directory if it doesn't exist
                var directory = Path.GetDirectoryName(SettingsPath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // Clone the profiles to avoid modifying the original
                var profilesToSave = profiles.Select(p => (ConnectionSettings)p.Clone()).ToList();

                // Create a new ConnectionProfile object
                var profileToSave = new ConnectionProfile { Profiles = new System.Collections.ObjectModel.ObservableCollection<ConnectionSettings>(profilesToSave) };

                // Encrypt passwords before saving
                foreach (var settings in profileToSave.Profiles)
                {
                    if (settings.SaveSourcePassword && !string.IsNullOrEmpty(settings.SourcePassword))
                    {
                        settings.SourcePassword = EncryptString(settings.SourcePassword, logService);
                    }
                    else
                    {
                        settings.SourcePassword = string.Empty;
                    }

                    if (settings.SaveDestinationPassword && !string.IsNullOrEmpty(settings.DestinationPassword))
                    {
                        settings.DestinationPassword = EncryptString(settings.DestinationPassword, logService);
                    }
                    else
                    {
                        settings.DestinationPassword = string.Empty;
                    }
                }

                var options = new JsonSerializerOptions { WriteIndented = true };
                var json = JsonSerializer.Serialize(profileToSave, options);
                await File.WriteAllTextAsync(SettingsPath, json);
                logService.LogMessage("Connection profiles saved successfully.", "INFO");
            }
            catch (Exception ex)
            {
                logService.LogMessage($"Failed to save connection profiles: {ex.Message}", "ERROR");
                throw new InvalidOperationException($"Failed to save connection profiles: {ex.Message}", ex);
            }
        }

        private static string EncryptString(string plainText, ILogService logService)
        {
            try
            {
                using Aes aes = Aes.Create();
                aes.Key = Encoding.UTF8.GetBytes(EncryptionKey.PadRight(32).Substring(0, 32));
                aes.GenerateIV(); // Generate a random IV for each encryption
                byte[] iv = aes.IV;

                using MemoryStream memoryStream = new MemoryStream();
                using (CryptoStream cryptoStream = new CryptoStream(memoryStream, aes.CreateEncryptor(aes.Key, iv), CryptoStreamMode.Write))
                {
                    using StreamWriter streamWriter = new StreamWriter(cryptoStream);
                    streamWriter.Write(plainText);
                }

                byte[] encryptedData = memoryStream.ToArray();

                // Prepend the IV to the encrypted data
                byte[] combinedData = new byte[iv.Length + encryptedData.Length];
                Array.Copy(iv, 0, combinedData, 0, iv.Length);
                Array.Copy(encryptedData, 0, combinedData, iv.Length, encryptedData.Length);

                return Convert.ToBase64String(combinedData);
            }
            catch (Exception ex)
            {
                logService.LogMessage($"Encryption failed: {ex.Message}", "WARNING");
                return plainText; // Return original if encryption fails
            }
        }

        private static string DecryptString(string cipherText, ILogService logService)
        {
            try
            {
                byte[] buffer = Convert.FromBase64String(cipherText);

                using Aes aes = Aes.Create();
                aes.Key = Encoding.UTF8.GetBytes(EncryptionKey.PadRight(32).Substring(0, 32));

                // Extract the IV from the beginning of the cipherText
                byte[] iv = new byte[aes.IV.Length];
                Array.Copy(buffer, 0, iv, 0, iv.Length);
                aes.IV = iv;

                // Extract the actual cipher text (without the IV)
                byte[] cipherBytes = new byte[buffer.Length - iv.Length];
                Array.Copy(buffer, iv.Length, cipherBytes, 0, cipherBytes.Length);

                using MemoryStream memoryStream = new MemoryStream(cipherBytes);
                using (CryptoStream cryptoStream = new CryptoStream(memoryStream, aes.CreateDecryptor(aes.Key, iv), CryptoStreamMode.Read))
                {
                    using StreamReader streamReader = new StreamReader(cryptoStream);
                    return streamReader.ReadToEnd();
                }
            }
            catch (Exception ex)
            {
                logService.LogMessage($"Decryption failed: {ex.Message}", "WARNING");
                return cipherText; // Return original if decryption fails
            }
        }
    }
}
