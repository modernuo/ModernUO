using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

namespace Server
{
    public static class NativeReader
    {
        private static readonly INativeReader m_NativeReader;

        static NativeReader() => m_NativeReader = Core.Unix ? new NativeReaderUnix() : new NativeReaderWin32();

        public static unsafe int Read(FileStream source, void* buffer, int length) => Read(source, buffer, 0, length);

        public static unsafe int Read(FileStream source, void* buffer, int bufferIndex, int length) =>
            m_NativeReader.Read(source, buffer, bufferIndex, length);
    }

    public interface INativeReader
    {
        unsafe int Read(FileStream source, void* buffer, int bufferIndex, int length);
    }

    public sealed class NativeReaderWin32 : INativeReader
    {
        internal class UnsafeNativeMethods
        {
            [DllImport("kernel32")]
            internal static extern unsafe bool ReadFile(IntPtr hFile, void* lpBuffer, uint nNumberOfBytesToRead, ref uint lpNumberOfBytesRead, NativeOverlapped* lpOverlapped);
        }

        public unsafe int Read(FileStream source, void* buffer, int bufferIndex, int length) => InternalRead(source, buffer, bufferIndex, length);

        internal static unsafe int InternalRead(FileStream source, void* buffer, int bufferIndex, int length)
        {
            var byteCount = 0U;

            if (UnsafeNativeMethods.ReadFile(source.SafeFileHandle!.DangerousGetHandle(), (byte*)buffer + bufferIndex, (uint)length, ref byteCount, null))
            {
                return (int)byteCount;
            }

            return -1;
        }
    }

    public sealed class NativeReaderUnix : INativeReader
    {
        internal class UnsafeNativeMethods
        {
            [DllImport("libc")]
            internal static extern unsafe int read(IntPtr ptr, void* buffer, int length);
        }

        public unsafe int Read(FileStream source, void* buffer, int bufferIndex, int length) =>
            InternalRead(source, buffer, bufferIndex, length);

        internal unsafe int InternalRead(FileStream source, void* buffer, int bufferIndex, int length) =>
            UnsafeNativeMethods.read(source.SafeFileHandle!.DangerousGetHandle(), (byte*)buffer + bufferIndex, length);
    }
}
