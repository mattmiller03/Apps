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

namespace VCenterMigrationTool_WPF_UI.Models
{
    public class PrerequisiteValidation
    {
        public bool VersionCompatible { get; set; }
        public bool StorageAvailable { get; set; }
        public bool NetworkAccessible { get; set; }
        public List<string> Warnings { get; set; } = new List<string>();
    }
}
