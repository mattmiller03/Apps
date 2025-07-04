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

namespace VCenterMigrationTool_WPF_UI.Models;

public class DatacenterInfo
{
    public string Name { get; set; } = string.Empty;
    public string Id { get; set; } = string.Empty;
}

public class ClusterInfo
{
    public string Name { get; set; } = string.Empty;
    public string Id { get; set; } = string.Empty;
}

public class HostInfo
{
    public string Name { get; set; } = string.Empty;
    public string Id { get; set; } = string.Empty;
    public string ConnectionState { get; set; } = string.Empty;
}

public class VMInfo
{
    public string Name { get; set; } = string.Empty;
    public string Id { get; set; } = string.Empty;
    public string PowerState { get; set; } = string.Empty;
}

