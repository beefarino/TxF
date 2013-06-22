using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace CodeOwls.TxF
{
    public static class Extensions
    {
        public static IEnumerable<FileSystemInfo> GetFilesTransacted(this DirectoryInfo directory)
        {
            return directory.GetFilesTransacted("*");
        }

        public static IEnumerable<FileSystemInfo> GetFilesTransacted(this DirectoryInfo directory, string pattern)
        {
            var items = Microsoft.KtmIntegration.TransactedDirectory.GetFiles(directory.FullName, pattern);
            return items;
        }
    }
}
