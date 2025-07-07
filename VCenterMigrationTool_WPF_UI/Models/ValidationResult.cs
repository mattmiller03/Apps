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
using VCenterMigrationTool_WPF_UI.Utilities;

namespace VCenterMigrationTool_WPF_UI.Models;

public class ValidationResult
{
    public string TestName { get; set; } = string.Empty;
    public string Result { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
    public string Recommendation { get; set; } = string.Empty;
}