using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCenterMigrationTool_WPF_UI.Infrastructure.Interfaces
{
    public interface IPowerShellScriptManager
    {
        void RegisterScript(string scriptName, string scriptPath);
        string GetScriptPath(string scriptName);
    }

    public class PowerShellScriptManager : IPowerShellScriptManager
    {
        private readonly Dictionary<string, string> _scripts = new Dictionary<string, string>();

        public void RegisterScript(string scriptName, string scriptPath)
        {
            _scripts[scriptName] = scriptPath;
        }

        public string GetScriptPath(string scriptName)
        {
            if (_scripts.TryGetValue(scriptName, out var path))
            {
                return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, path);
            }
            throw new FileNotFoundException($"Script '{scriptName}' not registered");
        }
    }
}
