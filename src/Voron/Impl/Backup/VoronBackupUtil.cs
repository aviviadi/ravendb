using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using Sparrow;
using Voron.Impl.FileHeaders;
using Voron.Util;

namespace Voron.Impl.Backup
{
    internal static unsafe class VoronBackupUtil
    {
        internal static void CopyHeaders(CompressionLevel compression, ZipArchive package, DataCopier copier, StorageEnvironmentOptions storageEnvironmentOptions, string basePath)
        {
            var success = false;
            IntPtr headerPtr = IntPtr.Zero;
            foreach (var headerFileName in HeaderAccessor.HeaderFileNames)
            {
                try
                {
                    var header = stackalloc FileHeader[1];
                    headerPtr = new IntPtr(header);
                    Memory.RegisterVerification(headerPtr, new UIntPtr((ulong)sizeof(FileHeader)), "stackalloc");

                    if (!storageEnvironmentOptions.ReadHeader(headerFileName, header))
                        continue;

                    success = true;

                    var headerPart = package.CreateEntry(Path.Combine(basePath, headerFileName), compression);
                    Debug.Assert(headerPart != null);

                    using (var headerStream = headerPart.Open())
                    {
                        copier.ToStream((byte*)header, sizeof(FileHeader), headerStream);
                    }
                }
                finally
                {                    
                    Memory.UnregisterVerification(headerPtr, new UIntPtr((ulong)sizeof(FileHeader)), "stackalloc");
                }
            }

            if (!success)
                throw new InvalidDataException($"Failed to read both file headers (headers.one & headers.two) from path: {basePath}, possible corruption.");
        }
    }
}
