using System.IO;
using CodeOwls.PowerShell.Provider.PathNodes;

namespace CodeOwls.TxF
{
    public class TxFPathNode : PathNode
    {
        public TxFPathNode( FileSystemInfo fileSystemInfo) : base( fileSystemInfo, fileSystemInfo.Name, fileSystemInfo is DirectoryInfo)
        {
        }
    }
}