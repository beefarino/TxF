using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Text;
using System.Threading.Tasks;

namespace CodeOwls.Txf.Tests
{
    public class PowerShellTestHost
    {
        public static IEnumerable<object> Invoke(string script)
        {
            var state = InitialSessionState.CreateDefault();
            state.ImportPSModule( new[]{"./codeowls.txf.dll"} );

            using (var rs = RunspaceFactory.CreateRunspace(state))
            {
                rs.Open();
                
                using (var ps = PowerShell.Create())
                {
                    ps.Runspace = rs;
                    ps.AddScript(script);
                    var results = ps.Invoke().ToList().ConvertAll(pso => pso.BaseObject);

                    var errors = ps.Streams.Error.ReadAll();
                    if (errors.Any())
                    {
                        throw errors.First().Exception;
                    }
                    
                    return results;
                }
            }
        }
    }
}
