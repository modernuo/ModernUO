/***************************************************************************
 *                             FileOperations.cs
 *                            -------------------
 *   begin                : May 1, 2002
 *   copyright            : (C) The RunUO Software Team
 *   email                : info@runuo.com
 *
 *   $Id$
 *
 ***************************************************************************/

/***************************************************************************
 *
 *   This program is free software; you can redistribute it and/or modify
 *   it under the terms of the GNU General Public License as published by
 *   the Free Software Foundation; either version 2 of the License, or
 *   (at your option) any later version.
 *
 ***************************************************************************/

using System.IO;

#if WINDOWS
using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
#endif

namespace Server
{
    public static class FileOperations
    {
        public const int KB = 1024;
        public const int MB = 1024 * KB;

        public static int BufferSize { get; set; } = 1 * MB;

        public static int Concurrency { get; set; } = 1;

#if WINDOWS
    public static bool Unbuffered { get; set; } = true;
#endif

        public static bool AreSynchronous => Concurrency < 1;

        public static FileStream OpenSequentialStream(string path, FileMode mode, FileAccess access, FileShare share)
        {
            var options = FileOptions.SequentialScan;

            if (Concurrency > 0)
                options |= FileOptions.Asynchronous;

#if !WINDOWS
            return new FileStream(path, mode, access, share, BufferSize, options);
#else
      if (Unbuffered)
        options |= NoBuffering;
      else
        return new FileStream(path, mode, access, share, BufferSize, options);

      var fileHandle =
        UnsafeNativeMethods.CreateFile(path, (int)access, share, IntPtr.Zero, mode, (int)options, IntPtr.Zero);

      if (fileHandle.IsInvalid) throw new IOException();

      return new UnbufferedFileStream(fileHandle, access, BufferSize, Concurrency > 0);
#endif
        }

#if WINDOWS
    private class UnbufferedFileStream : FileStream
    {
      private readonly SafeFileHandle fileHandle;

      public UnbufferedFileStream(SafeFileHandle fileHandle, FileAccess access, int bufferSize, bool isAsync)
        : base(fileHandle, access, bufferSize, isAsync) =>
        this.fileHandle = fileHandle;

      public override void Write(byte[] array, int offset, int count)
      {
        base.Write(array, offset, BufferSize);
      }

      public override IAsyncResult BeginWrite(byte[] array, int offset, int numBytes, AsyncCallback userCallback,
        object stateObject) =>
        base.BeginWrite(array, offset, BufferSize, userCallback, stateObject);

      protected override void Dispose(bool disposing)
      {
        if (!fileHandle.IsClosed) fileHandle.Close();

        base.Dispose(disposing);
      }
    }
#endif

#if WINDOWS
    private const FileOptions NoBuffering = (FileOptions)0x20000000;

    internal static class UnsafeNativeMethods
    {
      [DllImport("Kernel32", CharSet = CharSet.Unicode, SetLastError = true)]
      internal static extern SafeFileHandle CreateFile(string lpFileName, int dwDesiredAccess, FileShare dwShareMode,
        IntPtr securityAttrs, FileMode dwCreationDisposition, int dwFlagsAndAttributes, IntPtr hTemplateFile);
    }
#endif
    }
}
