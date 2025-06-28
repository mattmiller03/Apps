using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;

namespace VCenterMigrationTool_WPF_UI.Models
{
    public class ConnectionSettings : INotifyPropertyChanged, ICloneable
    {
        private string _sourceServer = string.Empty;
        private string _sourceUsername = string.Empty;
        private string _sourcePassword = string.Empty;
        private bool _saveSourcePassword = false;
        private string _destinationServer = string.Empty;
        private string _destinationUsername = string.Empty;
        private string _destinationPassword = string.Empty;
        private bool _saveDestinationPassword = false;
        private string _backupPath = @"C:\VCenterMigration\Backup";
        private DateTime _lastUsed = DateTime.Now;
        private string _profileName = "Default";

        [Required(ErrorMessage = "Source Server is required")]
        public string SourceServer
        {
            get { return _sourceServer; }
            set
            {
                if (_sourceServer != value)
                {
                    _sourceServer = value;
                    OnPropertyChanged();
                }
            }
        }

        [Required(ErrorMessage = "Source Username is required")]
        public string SourceUsername
        {
            get { return _sourceUsername; }
            set
            {
                if (_sourceUsername != value)
                {
                    _sourceUsername = value;
                    OnPropertyChanged();
                }
            }
        }

        public string SourcePassword
        {
            get { return _sourcePassword; }
            set
            {
                if (_sourcePassword != value)
                {
                    _sourcePassword = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool SaveSourcePassword
        {
            get { return _saveSourcePassword; }
            set
            {
                if (_saveSourcePassword != value)
                {
                    _saveSourcePassword = value;
                    OnPropertyChanged();
                }
            }
        }

        [Required(ErrorMessage = "Destination Server is required")]
        public string DestinationServer
        {
            get { return _destinationServer; }
            set
            {
                if (_destinationServer != value)
                {
                    _destinationServer = value;
                    OnPropertyChanged();
                }
            }
        }

        [Required(ErrorMessage = "Destination Username is required")]
        public string DestinationUsername
        {
            get { return _destinationUsername; }
            set
            {
                if (_destinationUsername != value)
                {
                    _destinationUsername = value;
                    OnPropertyChanged();
                }
            }
        }

        public string DestinationPassword
        {
            get { return _destinationPassword; }
            set
            {
                if (_destinationPassword != value)
                {
                    _destinationPassword = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool SaveDestinationPassword
        {
            get { return _saveDestinationPassword; }
            set
            {
                if (_saveDestinationPassword != value)
                {
                    _saveDestinationPassword = value;
                    OnPropertyChanged();
                }
            }
        }

        public string BackupPath
        {
            get { return _backupPath; }
            set
            {
                if (_backupPath != value)
                {
                    _backupPath = value;
                    OnPropertyChanged();
                }
            }
        }

        public DateTime LastUsed
        {
            get { return _lastUsed; }
            set
            {
                if (_lastUsed != value)
                {
                    _lastUsed = value;
                    OnPropertyChanged();
                }
            }
        }

        [Required(ErrorMessage = "Profile Name is required")]
        public string ProfileName
        {
            get { return _profileName; }
            set
            {
                if (_profileName != value)
                {
                    _profileName = value;
                    OnPropertyChanged();
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public object Clone()
        {
            return new ConnectionSettings
            {
                SourceServer = this.SourceServer,
                SourceUsername = this.SourceUsername,
                SourcePassword = this.SourcePassword,
                SaveSourcePassword = this.SaveSourcePassword,
                DestinationServer = this.DestinationServer,
                DestinationUsername = this.DestinationUsername,
                DestinationPassword = this.DestinationPassword,
                SaveDestinationPassword = this.SaveDestinationPassword,
                BackupPath = this.BackupPath,
                LastUsed = this.LastUsed,
                ProfileName = this.ProfileName
            };
        }
    }

    public class ConnectionProfile
    {
        public ObservableCollection<ConnectionSettings> Profiles { get; set; } = new ObservableCollection<ConnectionSettings>();
        public string LastUsedProfile { get; set; } = "Default";
    }
}
