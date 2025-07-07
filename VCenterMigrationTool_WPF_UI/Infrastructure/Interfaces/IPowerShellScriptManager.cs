using System;

namespace VCenterMigrationTool_WPF_UI.Infrastructure.Interfaces
{
    public interface IPowerShellScriptManager
    {
        /// <summary>
        /// Register a script by name (e.g. "HOST-Migrate") so GetScriptPath can resolve it later.
        /// </summary>
        void RegisterScript(string scriptName, string scriptPath);

        /// <summary>
        /// Returns the fully‐qualified disk path for a previously registered script name.
        /// </summary>
        string GetScriptPath(string scriptName);
    }
}
