using System;
using System.IO;
using System.Management.Automation.Provider;
using CodeOwls.PowerShell.Paths.Processors;
using CodeOwls.PowerShell.Provider;
using CodeOwls.PowerShell.Provider.PathNodeProcessors;

namespace CodeOwls.TxF
{
    [CmdletProvider( "TxF", ProviderCapabilities.Filter | ProviderCapabilities.Transactions | ProviderCapabilities.ShouldProcess )]
    public class TxFProvider : ProviderWithTransactions
    {
        protected override IPathNodeProcessor PathNodeProcessor
        {
            get { return new TxFPathNodeProcessor(); }
        }

        protected override System.Management.Automation.PSDriveInfo NewDrive(System.Management.Automation.PSDriveInfo drive)
        {
            var rootPath = drive.Root;
            if (! Directory.Exists(rootPath))
            {
                throw new InvalidOperationException("A TxF provider drive must be rooted on an existing file system directory.  Please specify an existing file system directory path in the -root parameter of new-psdrive.");
            }

            return base.NewDrive(drive);
        }
        public override IDisposable NewSession()
        {
            return new NullDisposable();
        }

        class NullDisposable : IDisposable
        {
            public void Dispose()
            {                
            }
        }
    }
}