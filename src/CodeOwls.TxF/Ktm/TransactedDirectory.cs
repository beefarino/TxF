namespace Microsoft.KtmIntegration
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Transactions;

    using Microsoft.KtmIntegration;
    using Microsoft.Win32.SafeHandles;
    using System.Runtime.Serialization.Formatters.Binary;

    [System.Security.SuppressUnmanagedCodeSecurity]
    public static class TransactedDirectory
    {
        // TODO - BCL match - add overload that has a 'searchOption' argument
        public static FileSystemInfo[] GetFiles(string path, string searchPattern)
        {
            using (TransactionScope scope = new TransactionScope())
            using (KtmTransactionHandle ktmTx = KtmTransactionHandle.CreateKtmTransactionHandle())
            {
                string dirSpec = System.IO.Path.Combine(path, searchPattern);
                List<FileSystemInfo> files = new List<FileSystemInfo>();
                List<FileSystemInfo> directories = new List<FileSystemInfo>();

                NativeMethods.WIN32_FIND_DATA findFileData = new NativeMethods.WIN32_FIND_DATA();
                SafeFileHandle hFind = FindFirstFileTransacted(dirSpec, ktmTx, out findFileData);
                try
                {
                    // List all the other files in the directory.
                    do
                    {
                        var fullPath = Path.Combine(path, findFileData.cFileName);
                        if (0x10 == (0x10 & findFileData.dwFileAttributes))
                        {
                            directories.Add(new DirectoryInfo(fullPath));
                        }
                        else
                        {
                            var fileInfo = new FileInfo(fullPath);

                            if (fileInfo.Exists)
                            {
                                var creationTime = findFileData.ftCreationTime.ToDateTime();
                                var lastAccessTime = findFileData.ftLastAccessTime.ToDateTime();
                                var lastWriteTime = findFileData.ftLastWriteTime.ToDateTime();

                                fileInfo.CreationTime = creationTime;
                                fileInfo.LastAccessTime = lastAccessTime;
                                fileInfo.LastWriteTime = lastWriteTime;
                            }

                            files.Add(fileInfo);
                        }
                    } 
                    while (NativeMethods.FindNextFile(hFind, out findFileData));
                    int error = Marshal.GetLastWin32Error();

                    if (error != NativeMethods.ERROR_NO_MORE_FILES)
                    {
                        NativeMethods.HandleCOMError(error);
                    }

                    scope.Complete();
                    directories.AddRange(files);
                    return directories.ToArray();
                }
                finally
                {
                    // Ignore failures from this api just as the BCL does...
                    NativeMethods.FindClose(hFind);
                    hFind.SetHandleAsInvalid();
                }
            }
        }

        // Creates a secondary RM under a given 'path'
        public static void CreateTxFResource(string path)
        {
            // Create the directory / ensure it is empty to begin with...
            Directory.CreateDirectory(path);

            SafeFileHandle handle = TransactedDirectory.GetDirectoryHandle(path);

            using (handle)
            {
                int bytesReturned = 0;

                // Issue the IO ctrl asking to create a secondary RM...
                bool result = NativeMethods.DeviceIoControl(
                    handle,
                    NativeMethods.FSCTL_TXFS_CREATE_SECONDARY_RM,
                    IntPtr.Zero,
                    0,
                    IntPtr.Zero,
                    0,
                    out bytesReturned,
                    IntPtr.Zero);
                if (!result)
                {
                    throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
                }
            }
        }

        // Starts the secondary RM under a given 'path'
        public static SafeFileHandle StartTxFResource(string path)
        {
            // If the user did not give an absolute path, it should become relative to our current directory
            if (!Path.IsPathRooted(path))
            {
                path = Path.Combine(Directory.GetCurrentDirectory(), path);
            }

            // Create the resource if not present to make the OM a bit easier to use
            if (!Directory.Exists(path))
            {
                CreateTxFResource(path);
            }

            // This handle must remain open for the duration of your work inside <path>
            SafeFileHandle handle = TransactedDirectory.GetDirectoryHandle(path);

            string txfWorkDir = Path.Combine(path, "TxfLog");
            Directory.CreateDirectory(txfWorkDir);

            const string prepend = "\\??\\";
            const string defaultRmLogPath = "TxfLog::TxfLog";
            const string defaultTmLogPath = "TxfLog::KtmLog";

            string rmLogName = prepend + Path.Combine(txfWorkDir, defaultRmLogPath);
            UInt16 rmLogNameLength = (UInt16)(rmLogName.Length * 2 + 2);

            string tmLogName = prepend + Path.Combine(txfWorkDir, defaultTmLogPath);
            UInt16 tmLogNameLength = (UInt16)(tmLogName.Length * 2 + 2);

            NativeMethods.TXFS_START_RM_INFORMATION startInfo = new NativeMethods.TXFS_START_RM_INFORMATION();
            UInt16 startInfoSize = (UInt16)Marshal.SizeOf(startInfo);

            startInfo.LogPathLength = rmLogNameLength;
            startInfo.TmLogPathLength = tmLogNameLength;
            startInfo.TmLogPathOffset = (UInt32)(startInfoSize + startInfo.LogPathLength);

            // TODO - Suckage - Wow... I have nothing to say about this...
            // Convert to an unmanaged buffer and then convert back to a managed byte[] for easy manipulation...
            IntPtr buffer = Marshal.AllocHGlobal(startInfoSize);
            Marshal.StructureToPtr(startInfo, buffer, false);

            byte[] startInfoBuffer = new byte[startInfoSize];
            Marshal.Copy(buffer, startInfoBuffer, 0, startInfoSize);

            // Create the path name buffer
            UnicodeEncoding unicode = new UnicodeEncoding();
            byte[] rawPathBuffer = unicode.GetBytes((rmLogName + "\0\0\0" + tmLogName + '\0').ToCharArray());

            // Now make the one true buffer which will get passed to the win32 api...
            byte[] fullBuffer = new byte[startInfoBuffer.Length + rawPathBuffer.Length - 4];
            Array.Copy(startInfoBuffer, fullBuffer, startInfoBuffer.Length - 4);
            Array.Copy(rawPathBuffer, 0, fullBuffer, startInfoBuffer.Length - 4, rawPathBuffer.Length);

            // And of course it needs to be unmanaged so the GC doesn't swipe it out from underneath us...
            IntPtr win32Buffer = Marshal.AllocHGlobal(fullBuffer.Length);
            Marshal.Copy(fullBuffer, 0, win32Buffer, fullBuffer.Length);

            int bytesReturned = 0;
            bool result = NativeMethods.DeviceIoControl(
                handle,
                NativeMethods.FSCTL_TXFS_START_RM,
                win32Buffer,
                fullBuffer.Length,
                IntPtr.Zero,
                0,
                out bytesReturned,
                IntPtr.Zero);
            if (!result)
            {
                throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
            }

            //
            //  Now we tell TxF to perform undo processing to make the volume consistent.
            //  TxfRollForwardUndo requires no parameters, other than the rm handle (volume 
            //  root dir.)  After undo processing is complete, TxF performs any additional
            //  required initialization, and upon return of this routine, is ready to 
            //  do work.
            //

            // TODO - Correctness - Is 16k really large enough for everyone? FsUtil uses 4k...
            int inBytesLength = 16 * 1024;
            IntPtr inBytes = Marshal.AllocHGlobal(inBytesLength);

            bool needRecovery = true;
            result = NativeMethods.DeviceIoControl(
                handle,
                NativeMethods.FSCTL_TXFS_ROLLFORWARD_REDO,
                inBytes,
                inBytesLength,
                inBytes,
                inBytesLength,
                out bytesReturned,
                IntPtr.Zero);
            if (!result)
            {
                int status = Marshal.GetLastWin32Error();
                if (status == NativeMethods.ERROR_RECOVERY_NOT_NEEDED)
                {
                    needRecovery = false;
                }
                else
                {
                    // Everything else is an error...
                    throw new System.ComponentModel.Win32Exception(status);
                }
            }

            if (needRecovery)
            {
                result = NativeMethods.DeviceIoControl(
                    handle,
                    NativeMethods.FSCTL_TXFS_ROLLFORWARD_UNDO,
                    IntPtr.Zero,
                    0,
                    IntPtr.Zero,
                    0,
                    out bytesReturned,
                    IntPtr.Zero);
                if (!result)
                {
                    throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
                }
            }

            return handle;
        }

        // Stops the secondary RM under a given 'path'
        public static void StopTxFResource(string path)
        {
            SafeFileHandle handle = TransactedDirectory.GetDirectoryHandle(path);

            using (handle)
            {
                int bytesReturned = 0;

                // Issue the IO ctrl asking to stop a secondary RM...
                bool result = NativeMethods.DeviceIoControl(
                    handle,
                    NativeMethods.FSCTL_TXFS_SHUTDOWN_RM,
                    IntPtr.Zero,
                    0,
                    IntPtr.Zero,
                    0,
                    out bytesReturned,
                    IntPtr.Zero);
                if (!result)
                {
                    throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
                }
            }
        }


        public static bool Exists(string path)
        {
            using (TransactionScope scope = new TransactionScope())
            using (KtmTransactionHandle ktmTx = KtmTransactionHandle.CreateKtmTransactionHandle())
            {
                bool result = false;
                NativeMethods.WIN32_FILE_ATTRIBUTE_DATA data = new NativeMethods.WIN32_FILE_ATTRIBUTE_DATA();
                if (NativeMethods.GetFileAttributesTransacted(path, 0, out data, ktmTx))
                {
                    result = NativeMethods.INVALID_FILE_ATTRIBUTES != data.FileAttributes && NativeMethods.FILE_ATTRIBUTE_DIRECTORY == (NativeMethods.FILE_ATTRIBUTE_DIRECTORY & data.FileAttributes);
                }
                scope.Complete();
                return result;
            }
        }

        public static bool CreateDirectory(string path)
        {
            using (TransactionScope scope = new TransactionScope())
            using (KtmTransactionHandle ktmTx = KtmTransactionHandle.CreateKtmTransactionHandle())
            {
                // Get a native handle to the directory
                bool result = NativeMethods.CreateDirectoryTransacted(
                    null,
                    path,
                    IntPtr.Zero,
                    ktmTx
                    );

                scope.Complete();
                return result;
            }
        }

        public static bool RemoveDirectory(string path)
        {
            using (TransactionScope scope = new TransactionScope())
            using (KtmTransactionHandle ktmTx = KtmTransactionHandle.CreateKtmTransactionHandle())
            {
                bool result = NativeMethods.RemoveDirectoryTransacted(
                    path,
                    ktmTx
                    );

                scope.Complete();
                return result;
            }
        }
        //
        // Private Helpers
        //

        private static SafeFileHandle GetDirectoryHandle(string path)
        {
            // Get a native handle to the directory
            SafeFileHandle handle = NativeMethods.CreateFile(
                path,
                NativeMethods.FileAccess.GENERIC_WRITE,
                NativeMethods.FileShare.FILE_SHARE_NONE,
                IntPtr.Zero,
                NativeMethods.FileMode.OPEN_EXISTING,
                NativeMethods.FILE_FLAG_BACKUP_SEMANTICS, // Returns the directory handle
                IntPtr.Zero);
            if (handle.IsInvalid)
            {
                throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
            }

            return handle;
        }

        private static SafeFileHandle FindFirstFileTransacted(string dirSpec, KtmTransactionHandle ktmTx, out NativeMethods.WIN32_FIND_DATA findFileData)
        {
            SafeFileHandle hFile = NativeMethods.FindFirstFileTransacted(
                dirSpec,
                NativeMethods.FINDEX_INFO_LEVELS.FindExInfoStandard,
                out findFileData,
                NativeMethods.FINDEX_SEARCH_OPS.FindExSearchNameMatch,
                IntPtr.Zero,
                0,
                ktmTx);

            if (hFile.IsInvalid)
            {
                NativeMethods.HandleCOMError(Marshal.GetLastWin32Error());
            }

            return hFile;
        }
    }
}
