using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using VCenterMigrationTool_WPF_UI.Models;
using VCenterMigrationTool_WPF_UI.Utilities;
using VCenterMigrationTool_WPF_UI.ViewModels;


namespace VCenterMigrationTool_WPF_UI;

public static class ConnectionManager
{
    private static readonly string SettingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "VCenterMigrationTool",
        "ConnectionProfiles.json"
    );

    private static readonly string EncryptionKey = "VCenterMigrationTool2024"; // In production, use a more secure key

    public static async Task<ConnectionProfile> LoadConnectionProfilesAsync()
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
                    if (!string.IsNullOrEmpty(settings.SourcePassword))
                    {
                        settings.SourcePassword = DecryptString(settings.SourcePassword);
                    }
                    if (!string.IsNullOrEmpty(settings.DestinationPassword))
                    {
                        settings.DestinationPassword = DecryptString(settings.DestinationPassword);
                    }
                }
                return profile;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading connection profiles: {ex.Message}");
        }

        return new ConnectionProfile();
    }

    public static async Task SaveConnectionProfilesAsync(ConnectionProfile profile)
    {
        try
        {
            // Create directory if it doesn't exist
            var directory = Path.GetDirectoryName(SettingsPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // Clone the profile to avoid modifying the original
            var profileToSave = JsonSerializer.Deserialize<ConnectionProfile>(
                JsonSerializer.Serialize(profile));

            if (profileToSave != null)
            {
                // Encrypt passwords before saving
                foreach (var settings in profileToSave.Profiles)
                {
                    if (settings.SaveSourcePassword && !string.IsNullOrEmpty(settings.SourcePassword))
                    {
                        settings.SourcePassword = EncryptString(settings.SourcePassword);
                    }
                    else
                    {
                        settings.SourcePassword = string.Empty;
                    }

                    if (settings.SaveDestinationPassword && !string.IsNullOrEmpty(settings.DestinationPassword))
                    {
                        settings.DestinationPassword = EncryptString(settings.DestinationPassword);
                    }
                    else
                    {
                        settings.DestinationPassword = string.Empty;
                    }
                }

                var options = new JsonSerializerOptions { WriteIndented = true };
                var json = JsonSerializer.Serialize(profileToSave, options);
                await File.WriteAllTextAsync(SettingsPath, json);
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to save connection profiles: {ex.Message}", ex);
        }
    }

    private static string EncryptString(string plainText)
    {
        try
        {
            byte[] iv = new byte[16];
            byte[] array;

            using (Aes aes = Aes.Create())
            {
                aes.Key = Encoding.UTF8.GetBytes(EncryptionKey.PadRight(32).Substring(0, 32));
                aes.IV = iv;

                ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

                using (MemoryStream memoryStream = new MemoryStream())
                {
                    using (CryptoStream cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write))
                    {
                        using (StreamWriter streamWriter = new StreamWriter(cryptoStream))
                        {
                            streamWriter.Write(plainText);
                        }
                        array = memoryStream.ToArray();
                    }
                }
            }

            return Convert.ToBase64String(array);
        }
        catch
        {
            return plainText; // Return original if encryption fails
        }
    }

    private static string DecryptString(string cipherText)
    {
        try
        {
            byte[] iv = new byte[16];
            byte[] buffer = Convert.FromBase64String(cipherText);

            using (Aes aes = Aes.Create())
            {
                aes.Key = Encoding.UTF8.GetBytes(EncryptionKey.PadRight(32).Substring(0, 32));
                aes.IV = iv;
                ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

                using (MemoryStream memoryStream = new MemoryStream(buffer))
                {
                    using (CryptoStream cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read))
                    {
                        using (StreamReader streamReader = new StreamReader(cryptoStream))
                        {
                            return streamReader.ReadToEnd();
                        }
                    }
                }
            }
        }
        catch
        {
            return cipherText; // Return original if decryption fails
        }
    }
}
