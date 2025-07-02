using System;
using System.ComponentModel.DataAnnotations;
using CommunityToolkit.Mvvm.ComponentModel;

namespace VCenterMigrationTool_WPF_UI.Models
{
    public partial class ConnectionSettings : ObservableValidator, ICloneable
    {
        [ObservableProperty]
        [Required(ErrorMessage = "Profile name is required")]
        private string _profileName = string.Empty;

        [ObservableProperty]
        [Required(ErrorMessage = "Source server is required")]
        private string _sourceServer = string.Empty;

        [ObservableProperty]
        [Required(ErrorMessage = "Source username is required")]
        private string _sourceUsername = string.Empty;

        [ObservableProperty]
        private string _sourcePassword = string.Empty;

        [ObservableProperty]
        [Required(ErrorMessage = "Destination server is required")]
        private string _destinationServer = string.Empty;

        [ObservableProperty]
        [Required(ErrorMessage = "Destination username is required")]
        private string _destinationUsername = string.Empty;

        [ObservableProperty]
        private string _destinationPassword = string.Empty;

        [ObservableProperty]
        private string _backupPath = string.Empty;

        [ObservableProperty]
        private DateTime? _lastUsed;

        [ObservableProperty]
        private bool _isDefault;

        [ObservableProperty]
        private string _description = string.Empty;

        // Remove the SaveSourcePassword and SaveDestinationPassword methods
        // Instead, use direct property setters

        public bool IsValid()
        {
            ValidateAllProperties();
            return !HasErrors;
        }

        public object Clone()
        {
            return new ConnectionSettings
            {
                ProfileName = this.ProfileName,
                SourceServer = this.SourceServer,
                SourceUsername = this.SourceUsername,
                SourcePassword = this.SourcePassword,
                DestinationServer = this.DestinationServer,
                DestinationUsername = this.DestinationUsername,
                DestinationPassword = this.DestinationPassword,
                BackupPath = this.BackupPath,
                LastUsed = this.LastUsed,
                IsDefault = this.IsDefault,
                Description = this.Description
            };
        }
    }
}
