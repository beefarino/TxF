using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management.Automation.Provider;
using CodeOwls.PowerShell.Paths;
using CodeOwls.PowerShell.Provider.PathNodeProcessors;
using CodeOwls.PowerShell.Provider.PathNodes;
using Microsoft.KtmIntegration;

namespace CodeOwls.TxF
{
    public class TxFNodeFactory : NodeFactoryBase, INewItem, IRemoveItem, ISetItemContent, IGetItemContent
    {
        private readonly FileSystemInfo _fileSystemInfo;
        private readonly IPathNode _node;

        public TxFNodeFactory( FileSystemInfo fileSystemInfo )
        {
            _fileSystemInfo = fileSystemInfo;
            _node = new TxFPathNode(_fileSystemInfo);
        }

        public override IEnumerable<INodeFactory> Resolve(IContext context, string nodeName)
        {
            var dirInfo = _node.Item as DirectoryInfo;
            if (null == dirInfo)
            {
                return null;
            }

            return dirInfo.GetFilesTransacted(nodeName).ToList().ConvertAll(fsi => new TxFNodeFactory(fsi)).Cast<INodeFactory>();
        }

        public override IEnumerable<INodeFactory> GetNodeChildren(IContext context)
        {
            var dirInfo = _node.Item as DirectoryInfo;
            if (null == dirInfo)
            {
                return null;
            }

            var entries = dirInfo.GetFilesTransacted().ToList();
            entries.Take(2).ToList().ForEach(e =>
                {
                    if (e.ToString().EndsWith("\\.") || e.ToString().EndsWith("\\.."))
                    {
                        entries.Remove(e);
                    }
                });
            entries.ToList().ForEach(e =>
                {
                    if ((!context.TransactionAvailable() || -1 != (int)e.Attributes) && !context.Force && 0 != (e.Attributes & (FileAttributes.System | FileAttributes.Hidden)))
                    {
                        entries.Remove(e);
                    }

                });
            return entries.ConvertAll(fsi => new TxFNodeFactory(fsi)).Cast<INodeFactory>();
        }

        public override IPathNode GetNodeValue()
        {
            return _node;
        }

        public override string Name
        {
            get { return _node.Name; }
        }

        public IEnumerable<string> NewItemTypeNames
        {
            get { return new[] {"file", "folder", "directory"}; }
        }

        public object NewItemParameters { get; private set; }

        public IPathNode NewItem(IContext context, string path, string itemTypeName, object newItemValue)
        {
            var dirInfo = _node.Item as DirectoryInfo;
            if (null == dirInfo)
            {
                throw new NotSupportedException();
            }

            FileSystemInfo newItem = null;
            var newItemPath = Path.Combine(dirInfo.FullName, path);
            switch (itemTypeName.ToLowerInvariant())
            {
                case ("file"):
                    {
                        using (
                            var fs = Microsoft.KtmIntegration.TransactedFile.Open(newItemPath, FileMode.Create,
                                                                                  FileAccess.ReadWrite,
                                                                                  FileShare.ReadWrite))
                        {
                            newItem = new FileInfo(newItemPath);
                        }
                        if (null != newItemValue)
                        {
                            using (var writer = GetContentWriter(context, newItem.FullName))
                            {
                                var list = new string[] {newItemValue.ToString()}.ToList();
                                writer.Write(list);
                            }
                        }
                        break;
                        
                    }
                case ("folder"):
                case("directory"):
                    {
                        if (Microsoft.KtmIntegration.TransactedDirectory.CreateDirectory(newItemPath))
                        {
                            newItem = new DirectoryInfo(newItemPath);
                        }
                        break;
                    }
            }

            if (null == newItem)
            {
                return null;
            }

            return new TxFPathNode( newItem );
        }

        public object RemoveItemParameters { get; private set; }
        public void RemoveItem(IContext context, string path, bool recurse)
        {
            var fi = _node.Item as FileSystemInfo;
            if (fi is DirectoryInfo)
            {
                Microsoft.KtmIntegration.TransactedDirectory.RemoveDirectory(fi.FullName);
            }
            else
            {
                Microsoft.KtmIntegration.TransactedFile.Delete(fi.FullName);
            }
        }

        public IContentWriter GetContentWriter(IContext context)
        {
            return GetContentWriter(context, _fileSystemInfo.FullName);
        }

        IContentWriter GetContentWriter(IContext context, string fullPath)
        {
            Stream stream;
            if (context.TransactionAvailable())
            {
                stream = TransactedFile.Open(fullPath, FileMode.Open, FileAccess.Write, FileShare.None);
            }
            else
            {
                stream = File.Open(fullPath, FileMode.Open, FileAccess.Write, FileShare.None);
            }
            return new TxFContentWriter(stream);
        }

        public object GetContentWriterDynamicParameters(IContext context)
        {
            return null;
        }

        public IContentReader GetContentReader(IContext context)
        {
            Stream stream;
            if (context.TransactionAvailable())
            {
                stream = TransactedFile.Open(_fileSystemInfo.FullName, FileMode.Open, FileAccess.Read, FileShare.None);
            }
            else
            {
                stream = File.Open(_fileSystemInfo.FullName, FileMode.Open, FileAccess.Read, FileShare.Read);
            }
            return new TxFContentReader(stream);
        }

        public object GetContentReaderDynamicParameters(IContext context)
        {
            return null;
        }
    }
}