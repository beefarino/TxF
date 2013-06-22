using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using CodeOwls.PowerShell.Paths.Processors;
using CodeOwls.PowerShell.Provider.PathNodeProcessors;
using CodeOwls.PowerShell.Provider.PathNodes;

namespace CodeOwls.TxF
{
    public class TxFPathNodeProcessor : IPathNodeProcessor
    {
        public IEnumerable<INodeFactory> ResolvePath(IContext context, string path)
        {
//            string driveName;
//            var isAbsolute = context.SessionState.Path.IsPSAbsolute(path, out driveName);
//            if (isAbsolute)
//            {
//                string fileSystemDriveName = driveName.TrimStart('x');
//                path = Regex.Replace(path, "^" + Regex.Escape( driveName + ":" ), fileSystemDriveName + ":");
//            }

            FileSystemInfo fileSystemInfo = null;
            if (Microsoft.KtmIntegration.TransactedFile.Exists(path))
            {
                fileSystemInfo = new FileInfo( path );
            }
            else if (Microsoft.KtmIntegration.TransactedDirectory.Exists(path))
            {
                fileSystemInfo = new DirectoryInfo(path);
            }
            else
            {
                return null;
            }

            return new[]{new TxFNodeFactory( fileSystemInfo )};
        }
    }
}