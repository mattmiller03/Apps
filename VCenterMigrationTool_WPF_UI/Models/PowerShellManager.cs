using System;
using System.Collections.Generic;
using System.Management.Automation;
using System.Text;
using System.Threading.Tasks;
using VCenterMigrationTool_WPF_UI.Infrastructure.Interfaces;

namespace VCenterMigrationTool_WPF_UI.Models
{
    public class PowerShellManager
    {
        private readonly ILogger _logger;
        private readonly IPowerShellScriptManager _scriptMgr;

        public PowerShellManager(ILogger logger, IPowerShellScriptManager scriptMgr)
        {
            _logger = logger;
            _scriptMgr = scriptMgr;
        }

        public async Task<string> ExecuteAsync(string scriptName, Dictionary<string, object> parameters)
        {
            var scriptPath = _scriptMgr.GetScriptPath(scriptName);
            using var ps = PowerShell.Create();
            ps.AddCommand(scriptPath);

            foreach (var kvp in parameters)
                ps.AddParameter(kvp.Key, kvp.Value);

            var results = await Task.Run(() => ps.Invoke());
            if (ps.Streams.Error.Count > 0)
                foreach (var err in ps.Streams.Error)
                    _logger.Error(err.ToString());

            var sb = new StringBuilder();
            foreach (var r in results)
                sb.AppendLine(r.ToString());

            return sb.ToString();
        }
    }
}
