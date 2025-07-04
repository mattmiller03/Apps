using ModernWpf;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using VCenterMigrationTool_WPF_UI.Infrastructure.Interfaces;
using VCenterMigrationTool_WPF_UI.Utilities;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using VCenterMigrationTool_WPF_UI.Infrastructure;
using VCenterMigrationTool_WPF_UI.Models;

namespace VCenterMigrationTool_WPF_UI.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly IProfileManager _profileManager;
        private readonly PowerShellManager _psManager;

        // Connection Properties
        private string _sourceVCenter;
        public string SourceVCenter
        {
            get => _sourceVCenter;
            set { _sourceVCenter = value; OnPropertyChanged(); }
        }

        private string _destinationVCenter;
        public string DestinationVCenter
        {
            get => _destinationVCenter;
            set { _destinationVCenter = value; OnPropertyChanged(); }
        }

        private string _username;
        public string Username
        {
            get => _username;
            set { _username = value; OnPropertyChanged(); }
        }

        private SecureString _password;
        public SecureString Password
        {
            get => _password;
            set { _password = value; OnPropertyChanged(); }
        }

        private bool _isSourceConnected;
        public bool IsSourceConnected
        {
            get => _isSourceConnected;
            set { _isSourceConnected = value; OnPropertyChanged(); }
        }

        private string _sourceVersion;
        public string SourceVersion
        {
            get => _sourceVersion;
            set { _sourceVersion = value; OnPropertyChanged(); }
        }

        // Add other missing properties as needed...

        public MainViewModel(IProfileManager profileManager, PowerShellManager psManager)
        {
            _profileManager = profileManager;
            _psManager = psManager;

            // Initialize commands
            ConnectSourceCommand = new AsyncRelayCommand(ConnectSourceAsync);
            // Initialize other commands...
        }

        private async Task ConnectSourceAsync()
        {
            try
            {
                var result = await _psManager.TestConnectionAsync(
                    SourceVCenter,
                    Username,
                    Password);

                IsSourceConnected = result.IsConnected;
                SourceVersion = result.Version;
            }
            catch (Exception ex)
            {
                // Handle error
            }
        }

        // Implement other methods...
    }


}

