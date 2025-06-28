using System;
using System.Collections.ObjectModel;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

namespace VCenterMigrationTool_WPF_UI.Utilities
{
    /// <summary>
    /// Manages PowerShell runspace and script execution.
    /// </summary>
    public class PowerShellRunspaceManager : IDisposable
    {
        private Runspace _runspace;

        public Runspace Runspace
        {
            get { return _runspace; }
        }

        public PowerShellRunspaceManager()
        {
            var initialSessionState = InitialSessionState.CreateDefault();
            _runspace = RunspaceFactory.CreateRunspace(initialSessionState);
            _runspace.Open();
        }

        /// <summary>
        /// Executes a PowerShell script asynchronously with parameters.
        /// </summary>
        public async Task<Collection<PSObject>> InvokeScriptAsync(string script,
            System.Collections.Generic.Dictionary<string, object>? parameters = null,
            CancellationToken cancellationToken = default)
        {
            if (_runspace == null || _runspace.RunspaceStateInfo.State != RunspaceState.Opened)
                throw new InvalidOperationException("PowerShell runspace is not initialized or not opened.");

            try
            {
                using PowerShell ps = PowerShell.Create();
                ps.Runspace = _runspace;

                ps.AddScript(script);

                if (parameters != null)
                {
                    foreach (var param in parameters)
                    {
                        ps.AddParameter(param.Key, param.Value);
                    }
                }

                // BeginInvoke the script asynchronously
                IAsyncResult asyncResult = ps.BeginInvoke();

                // Wait for the script to complete or the cancellation token to be cancelled
                while (!asyncResult.IsCompleted)
                {
                    // Check for cancellation
                    if (cancellationToken.IsCancellationRequested)
                    {
                        ps.Stop(); // Attempt to stop the PowerShell script
                        throw new OperationCanceledException("The operation was cancelled.", cancellationToken);
                    }

                    // Wait a short time before checking again
                    await Task.Delay(100);
                }

                // End the invoke and retrieve the results
                Collection<PSObject> results = new Collection<PSObject>(ps.EndInvoke(asyncResult).ToList());

                // Check for errors
                if (ps.Streams.Error.Count > 0)
                {
                    string errors = string.Join(Environment.NewLine, ps.Streams.Error.Select(e => e.ToString()));
                    throw new Exception($"PowerShell script execution failed with errors: {errors}");
                }
                return results;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"PowerShell execution failed: {ex.Message}", ex);
            }
        }


        /// <summary>
        /// Executes a PowerShell script synchronously.
        /// </summary>
        public Collection<PSObject> InvokeScript(string scriptPath, System.Collections.Generic.Dictionary<string, object> parameters)
        {
            if (_runspace == null || _runspace.RunspaceStateInfo.State != RunspaceState.Opened)
                throw new InvalidOperationException("PowerShell runspace is not initialized or not opened.");

            using PowerShell ps = PowerShell.Create();
            ps.Runspace = _runspace;
            ps.AddCommand(scriptPath);

            foreach (var param in parameters)
                ps.AddParameter(param.Key, param.Value);

            var results = ps.Invoke();

            if (ps.HadErrors)
            {
                var errors = string.Join(Environment.NewLine, ps.Streams.Error);
                throw new Exception($"PowerShell errors: {errors}");
            }

            return results;
        }

        public void Dispose()
        {
            _runspace?.Dispose();
        }
    }
}
