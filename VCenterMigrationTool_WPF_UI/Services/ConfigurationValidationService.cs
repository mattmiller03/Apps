// Services/ConfigurationValidationService.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using VCenterMigrationTool.Models;

namespace VCenterMigrationTool.Services
{
    public interface IConfigurationValidationService
    {
        Task<ValidationResult> ValidateSourceConfigurationAsync(VCenterConfiguration config);
        Task<ValidationResult> ValidateDestinationConfigurationAsync(VCenterConfiguration config);
        Task<ValidationResult> ValidateMigrationSettingsAsync(MigrationSettings settings);
    }

    public class ConfigurationValidationService : IConfigurationValidationService
    {
        public async Task<ValidationResult> ValidateSourceConfigurationAsync(VCenterConfiguration config)
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(config.ServerAddress))
                errors.Add("Source server address is required");

            if (string.IsNullOrWhiteSpace(config.Username))
                errors.Add("Source username is required");

            if (string.IsNullOrWhiteSpace(config.Password))
                errors.Add("Source password is required");

            if (!string.IsNullOrWhiteSpace(config.ServerAddress))
            {
                var isReachable = await IsServerReachableAsync(config.ServerAddress);
                if (!isReachable)
                    errors.Add($"Source server {config.ServerAddress} is not reachable");
            }

            return new ValidationResult
            {
                IsValid = !errors.Any(),
                Errors = errors
            };
        }

        public async Task<ValidationResult> ValidateDestinationConfigurationAsync(VCenterConfiguration config)
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(config.ServerAddress))
                errors.Add("Destination server address is required");

            if (string.IsNullOrWhiteSpace(config.Username))
                errors.Add("Destination username is required");

            if (string.IsNullOrWhiteSpace(config.Password))
                errors.Add("Destination password is required");

            if (!string.IsNullOrWhiteSpace(config.ServerAddress))
            {
                var isReachable = await IsServerReachableAsync(config.ServerAddress);
                if (!isReachable)
                    errors.Add($"Destination server {config.ServerAddress} is not reachable");
            }

            return new ValidationResult
            {
                IsValid = !errors.Any(),
                Errors = errors
            };
        }

        public async Task<ValidationResult> ValidateMigrationSettingsAsync(MigrationSettings settings)
        {
            var errors = new List<string>();

            if (settings.MaxConcurrentMigrations <= 0)
                errors.Add("Max concurrent migrations must be greater than 0");

            if (settings.MaxConcurrentMigrations > 10)
                errors.Add("Max concurrent migrations should not exceed 10 for optimal performance");

            if (settings.TimeoutMinutes <= 0)
                errors.Add("Timeout must be greater than 0 minutes");

            return await Task.FromResult(new ValidationResult
            {
                IsValid = !errors.Any(),
                Errors = errors
            });
        }

        private async Task<bool> IsServerReachableAsync(string serverAddress)
        {
            try
            {
                using var ping = new Ping();
                var reply = await ping.SendPingAsync(serverAddress, 5000);
                return reply.Status == IPStatus.Success;
            }
            catch
            {
                return false;
            }
        }
    }

    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
    }
}