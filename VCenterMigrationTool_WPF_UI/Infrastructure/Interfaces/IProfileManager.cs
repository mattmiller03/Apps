using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using System.Security;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using VCenterMigrationTool_WPF_UI.Infrastructure;
using VCenterMigrationTool_WPF_UI.Models;
using VCenterMigrationTool_WPF_UI.Utilities;

namespace VCenterMigrationTool_WPF_UI.Infrastructure.Interfaces
{
    public interface IProfileManager
    {
        IEnumerable<ConnectionProfile> GetAllProfiles();
        ConnectionProfile GetProfile(string name);
        void SaveProfile(ConnectionProfile profile);
        void DeleteProfile(string name);
    }
}
