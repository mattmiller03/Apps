using System;
using Microsoft.Extensions.DependencyInjection;
using System.Security;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using VCenterMigrationTool_WPF_UI.Infrastructure;
using VCenterMigrationTool_WPF_UI.Models;
using VCenterMigrationTool_WPF_UI.Utilities;

namespace VCenterMigrationTool_WPF_UI.Models;  // Updated namespace to match Models folder convention

public class MigrationTask
{
    public string ObjectName { get; set; } = string.Empty;  // Added default value
    public string ObjectType { get; set; } = string.Empty;  // Added default value
    public MigrationStatus Status { get; set; }
    public double Progress { get; set; }
    public DateTime StartTime { get; set; }
    public TimeSpan Duration { get; set; }
    public string Details { get; set; } = string.Empty;  // Added default value
}

public enum MigrationStatus
{
    Pending,
    InProgress,
    Completed,
    Failed,
    Cancelled,
    Queued
}

// Added MigrationType enum
public enum MigrationType
{
    Host,
    VM,
    Cluster
}
