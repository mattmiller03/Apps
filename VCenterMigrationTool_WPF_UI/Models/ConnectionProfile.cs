using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using VCenterMigrationTool_WPF_UI.Infrastructure;
using VCenterMigrationTool_WPF_UI.Models;
using VCenterMigrationTool_WPF_UI.Utilities;

namespace VCenterMigrationTool_WPF_UI.Models
{
    public class ConnectionProfile : INotifyPropertyChanged
    {
        public string ProfileName { get; set; }
        public string ServerAddress { get; set; }
        public string Username { get; set; }
        public SecureString SecurePassword { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime LastModified { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
