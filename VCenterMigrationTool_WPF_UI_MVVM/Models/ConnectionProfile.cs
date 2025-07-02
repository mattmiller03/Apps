using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;

namespace VCenterMigrationTool_WPF_UI.Models
{
    public partial class ConnectionProfile : ObservableObject, ICloneable
    {
        [ObservableProperty]
        private List<ConnectionSettings> _profiles = new();

        public object Clone()
        {
            var clonedProfile = new ConnectionProfile();

            foreach (var profile in Profiles)
            {
                clonedProfile.Profiles.Add((ConnectionSettings)profile.Clone());
            }

            return clonedProfile;
        }
    }
}
