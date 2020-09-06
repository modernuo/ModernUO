/***************************************************************************
 *                              NativeReader.cs
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

using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace Server
{
    public static class NativeReader
    {
        private static readonly INativeReader m_NativeReader;

        static NativeReader()
        {
            if (Core.Unix)
                m_NativeReader = new NativeReaderUnix();
            else
                m_NativeReader = new NativeReaderWin32();
        }

        public static unsafe void Read(IntPtr p, void* buffer, int length)
        {
            m_NativeReader.Read(p, buffer, length);
        }
    }

    public interface INativeReader
    {
        unsafe void Read(IntPtr p, void* buffer, int length);
    }

    public sealed class NativeReaderWin32 : INativeReader
    {
        public unsafe void Read(IntPtr p, void* buffer, int length)
        {
            uint lpNumberOfBytesRead = 0;
            UnsafeNativeMethods.ReadFile(p, buffer, (uint)length, ref lpNumberOfBytesRead, null);
        }

        internal static class UnsafeNativeMethods
        {
            /*[DllImport("kernel32")]
            internal unsafe static extern int _lread(IntPtr hFile, void* lpBuffer, int wBytes);*/

            [DllImport("kernel32")]
            internal static extern unsafe bool ReadFile(
                IntPtr hFile, void* lpBuffer, uint nNumberOfBytesToRead,
                ref uint lpNumberOfBytesRead, NativeOverlapped* lpOverlapped
            );
        }
    }

    public sealed class NativeReaderUnix : INativeReader
    {
        public unsafe void Read(IntPtr p, void* buffer, int length)
        {
            _ = UnsafeNativeMethods.read(p, buffer, length);
        }

        internal static class UnsafeNativeMethods
        {
            [DllImport("libc")]
            internal static extern unsafe int read(IntPtr p, void* buffer, int length);
        }
    }
}
