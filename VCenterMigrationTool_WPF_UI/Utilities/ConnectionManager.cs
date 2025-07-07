using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using VCenterMigrationTool_WPF_UI.Infrastructure.Interfaces;
using System;
using System.Diagnostics;
using System.IO;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using System.Security;
using System.Runtime.CompilerServices;
using VCenterMigrationTool_WPF_UI.Infrastructure;
using VCenterMigrationTool_WPF_UI.Models;

namespace VCenterMigrationTool_WPF_UI.Utilities
{
    public class ConnectionManager : INotifyPropertyChanged
    {
        private readonly IProfileManager _profileManager;
        private readonly PowerShellManager _powerShellManager;

        // Your existing properties and methods...
        public ObservableCollection<ConnectionProfile> ServerProfiles { get; }
        public ConnectionProfile SelectedSourceProfile { get; set; }
        public ConnectionProfile SelectedDestinationProfile { get; set; }

        public ICommand OpenProfileManagerCommand { get; }
        public ICommand TestSourceConnectionCommand { get; }
        // Other commands and methods...
    }
}
