namespace Microsoft.KtmIntegration
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Transactions;

    using Microsoft.KtmIntegration;
    using Microsoft.Win32.SafeHandles;

    // TODO - BCL match - throw nicer exceptions
    // TODO - Semantics - Some operations pass back a boolean success value. Should we always
    // complete the scope and instead allow the caller to perform necessary rollback actions?
    [System.Security.SuppressUnmanagedCodeSecurity]
    public static class TransactedFile
    {
        public static bool Exists(string path)
        {
            using (TransactionScope scope = new TransactionScope())
            using (KtmTransactionHandle ktmTx = KtmTransactionHandle.CreateKtmTransactionHandle())
            {
                bool result = false;
                NativeMethods.WIN32_FILE_ATTRIBUTE_DATA data = new NativeMethods.WIN32_FILE_ATTRIBUTE_DATA();
                if (NativeMethods.GetFileAttributesTransacted(path, 0, out data, ktmTx))
                {
                    result = NativeMethods.INVALID_FILE_ATTRIBUTES != data.FileAttributes &&
                             0 == (data.FileAttributes & NativeMethods.FILE_ATTRIBUTE_DIRECTORY);
                }
                scope.Complete();
                return result;
            }
        }

        public static FileStream Open(string path, FileMode mode, FileAccess access, FileShare share)
        {
            using (TransactionScope scope = new TransactionScope())
            using (KtmTransactionHandle ktmTx = KtmTransactionHandle.CreateKtmTransactionHandle())
            {
                NativeMethods.FileMode internalMode = TranslateFileMode(mode);
                NativeMethods.FileShare internalShare = TranslateFileShare(share);
                NativeMethods.FileAccess internalAccess = TranslateFileAccess(access);

                SafeFileHandle hFile = NativeMethods.CreateFileTransacted(
                    path,
                    internalAccess,
                    internalShare,
                    IntPtr.Zero,
                    internalMode,
                    0,
                    IntPtr.Zero,
                    ktmTx,
                    IntPtr.Zero,
                    IntPtr.Zero);
                if (hFile.IsInvalid)
                {
                    NativeMethods.HandleCOMError(Marshal.GetLastWin32Error());
                }

                FileStream stream = new FileStream(hFile, access);
                scope.Complete();

                return stream;
            }
        }

        public static bool Copy(string sourceFileName, string destFileName)
        {
            return Copy(sourceFileName, destFileName, false);
        }

        public static bool Copy(string sourceFileName, string destFileName, bool overwrite)
        {
            using (TransactionScope scope = new TransactionScope())
            using (KtmTransactionHandle ktmTx = KtmTransactionHandle.CreateKtmTransactionHandle())
            {
                NativeMethods.CopyFileFlags copyFlags = NativeMethods.CopyFileFlags.COPY_FILE_FAIL_IF_EXISTS;
                if (overwrite)
                {
                    copyFlags = 0; // TODO - Correctness - Which flag value is this really supposed to be? Works though...
                }

                bool pbCancel = false;
                bool status = NativeMethods.CopyFileTransacted(
                    sourceFileName,
                    destFileName,
                    IntPtr.Zero,
                    IntPtr.Zero,
                    ref pbCancel,
                    copyFlags,
                    ktmTx);

                if (!status)
                {
                    NativeMethods.HandleCOMError(Marshal.GetLastWin32Error());
                }

                scope.Complete();
                return status;
            }
        }

        public static bool Move(string sourceFileName, string destFileName)
        {
            using (TransactionScope scope = new TransactionScope())
            using (KtmTransactionHandle ktmTx = KtmTransactionHandle.CreateKtmTransactionHandle())
            {
                // Allow copying to different volumes and to replace existing files
                NativeMethods.MoveFileFlags moveFlags =
                    NativeMethods.MoveFileFlags.MOVEFILE_COPY_ALLOWED |
                    NativeMethods.MoveFileFlags.MOVEFILE_REPLACE_EXISTING;

                bool status = NativeMethods.MoveFileTransacted(
                    sourceFileName,
                    destFileName,
                    IntPtr.Zero,
                    IntPtr.Zero,
                    moveFlags,
                    ktmTx);

                if (!status)
                {
                    NativeMethods.HandleCOMError(Marshal.GetLastWin32Error());
                }

                scope.Complete();
                return status;
            }
        }

        public static void Delete(string path)
        {
            using (TransactionScope scope = new TransactionScope())
            using (KtmTransactionHandle ktmTx = KtmTransactionHandle.CreateKtmTransactionHandle())
            {
                bool status = NativeMethods.DeleteFileTransacted(
                    path,
                    ktmTx);

                if (!status)
                {
                    // Match the BCL behavior and disregard non-existant files...
                    int error = Marshal.GetLastWin32Error();
                    if (error != NativeMethods.ERROR_FILE_NOT_FOUND)
                    {
                        NativeMethods.HandleCOMError(error);
                    }
                }

                scope.Complete();
            }
        }

        internal static NativeMethods.FileMode TranslateFileMode(FileMode mode)
        {
            if (mode != FileMode.Append)
            {
                // 1:1 mapping for these
                return (NativeMethods.FileMode)(int)mode;
            }
            else
            {
                // Append is mapped to OpenOrCreate
                return (NativeMethods.FileMode)(int)FileMode.OpenOrCreate;
            }
        }

        internal static NativeMethods.FileAccess TranslateFileAccess(FileAccess access)
        {
            return access == FileAccess.Read ? NativeMethods.FileAccess.GENERIC_READ : NativeMethods.FileAccess.GENERIC_WRITE;
        }

        internal static NativeMethods.FileShare TranslateFileShare(FileShare share)
        {
            // Complete 1:1 mapping
            return (NativeMethods.FileShare)(int)share;
        }
    }
}
