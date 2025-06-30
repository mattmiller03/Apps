using System.Collections.ObjectModel;
using VCenterMigrationTool_WPF_UI.Models;
using VCenterMigrationTool_WPF_UI.Utilities;
using VCenterMigrationTool_WPF_UI.ViewModels;

namespace VCenterMigrationTool_WPF_UI.Models
{
    public class ConnectionProfile
    {
        public ObservableCollection<ConnectionSettings> Profiles { get; set; } = new ObservableCollection<ConnectionSettings>();
        public string LastUsedProfile { get; set; } = "Default";
    }
}