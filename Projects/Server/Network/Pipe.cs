/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2023 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: Pipe.cs                                                         *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Server.Network;

public partial class Pipe : IDisposable
{
    public class PipeWriter
    {
        private readonly Pipe _pipe;

        internal PipeWriter(Pipe pipe) => _pipe = pipe;

        public unsafe Span<byte> AvailableToWrite()
        {
            var read = _pipe._readIdx;
            var write = _pipe._writeIdx;

            uint sz;
            if (read <= write)
            {
                sz = _pipe.Size - write + read - 1;
            }
            else
            {
                sz = read - write - 1;
            }

            return new Span<byte>((void*)(_pipe._buffer + write), (int)sz);
        }

        public void Advance(uint count)
        {
            var read = _pipe._readIdx;
            var write = _pipe._writeIdx;

            if (count == 0)
            {
                return;
            }

            if (count > _pipe.Size - 1)
            {
                throw new EndOfPipeException("Unable to advance beyond the end of the pipe.");
            }

            if (read <= write)
            {
                if (count > read + _pipe.Size - write - 1)
                {
                    throw new EndOfPipeException("Unable to advance beyond the end of the pipe.");
                }

                var sz = Math.Min(count, _pipe.Size - write);

                write += sz;
                if (write > _pipe.Size - 1)
                {
                    write = 0;
                }
                count -= sz;

                if (count > 0)
                {
                    if (count >= read)
                    {
                        throw new EndOfPipeException("Unable to advance beyond the end of the pipe.");
                    }

                    write = count;
                }
            }
            else
            {
                if (count > read - write - 1)
                {
                    throw new EndOfPipeException("Unable to advance beyond the end of the pipe.");
                }

                write += count;
            }

            // It's never valid to advance the write pointer to become equal to
            // the read pointer. Check that here.
            if (write == read)
            {
                throw new EndOfPipeException("Unable to advance beyond the end of the pipe.");
            }

            _pipe._writeIdx = write;
        }

        public void Close() => _pipe._closed = true;

        public bool IsClosed => _pipe._closed;
    }

    public class PipeReader
    {
        private readonly Pipe _pipe;

        internal PipeReader(Pipe pipe) => _pipe = pipe;

        public unsafe Span<byte> AvailableToRead()
        {
            var read = _pipe._readIdx;
            var write = _pipe._writeIdx;

            uint sz;
            if (read <= write)
            {
                sz = write - read;
            }
            else
            {
                sz = _pipe.Size - read + write;
            }

            return new Span<byte>((void*)(_pipe._buffer + read), (int)sz);
        }

        public void Advance(uint count)
        {
            var read = _pipe._readIdx;
            var write = _pipe._writeIdx;

            if (read <= write)
            {
                if (count > write - read)
                {
                    throw new EndOfPipeException("Unable to advance beyond the end of the pipe.");
                }

                read += count;
            }
            else
            {
                var sz = Math.Min(count, _pipe.Size - read);

                read += sz;
                if (read > _pipe.Size - 1)
                {
                    read = 0;
                }
                count -= sz;

                if (count > 0)
                {
                    if (count > write)
                    {
                        throw new EndOfPipeException("Unable to advance beyond the end of the pipe.");
                    }

                    read = count;
                }
            }

            if (read == write)
            {
                // If the read pointer catches up to the write pointer, then the pipe is empty.
                // As a performance optimization, set both to 0. This should improve the chances cache lines are hit.
                _pipe._readIdx = 0;
                _pipe._writeIdx = 0;
            }
            else
            {
                _pipe._readIdx = read;
            }
        }

        public void Close() => _pipe._closed = true;

        public bool IsClosed => _pipe._closed;
    }

    private IntPtr _handle; // Doubles as the file descriptor for linux/darwin
    private IntPtr _buffer;
    private readonly uint _bufferSize;
    private uint _writeIdx;
    private uint _readIdx;
    private bool _closed;

    public PipeWriter Writer { get; }
    public PipeReader Reader { get; }

    public uint Size => _bufferSize;

    public bool Closed => _closed;

    public Pipe(uint size)
    {
        var pageSize = (uint)Environment.SystemPageSize;

        // Virtual allocation requires multiples of system page size
        // So let's adjust the requested size rounded to the next available page size
        var adjustedSize = (size + pageSize - 1) & ~(pageSize - 1);

        if (Core.IsWindows)
        {
            // Reserve a region of virtual memory. We need twice the size so we can later mirror.
            var region = NativeMethods_Windows.VirtualAlloc2(
                IntPtr.Zero,
                IntPtr.Zero,
                adjustedSize * 2,
                NativeMethods_Windows.MEM_RESERVE | NativeMethods_Windows.MEM_RESERVE_PLACEHOLDER,
                NativeMethods_Windows.PAGE_NOACCESS,
                IntPtr.Zero,
                0
            );

            if (region == IntPtr.Zero)
            {
                throw new InvalidOperationException($"Allocating virtual memory failed. ({Marshal.GetLastPInvokeError()})");
            }

            // Releases half of the region so we can map the same memory region twice
            var freed = NativeMethods_Windows.VirtualFree(
                region,
                adjustedSize,
                NativeMethods_Windows.MEM_RELEASE | NativeMethods_Windows.MEM_PRESERVE_PLACEHOLDER
            );

            if (!freed)
            {
                throw new InvalidOperationException($"Creating virtual placeholder failed. ({Marshal.GetLastPInvokeError()})");
            }

            // Create a file descriptor
            _handle = NativeMethods_Windows.CreateFileMappingW(
                NativeMethods_Windows.InvalidHandleValue,
                IntPtr.Zero,
                NativeMethods_Windows.PAGE_READWRITE,
                0,
                adjustedSize,
                null
            );

            if (_handle == IntPtr.Zero)
            {
                throw new InvalidOperationException($"Creating file mapping failed. ({Marshal.GetLastPInvokeError()})");
            }

            // Map the region to the first half of the virtual space
            _buffer = NativeMethods_Windows.MapViewOfFile3(
                _handle,
                IntPtr.Zero,
                region,
                0,
                adjustedSize,
                NativeMethods_Windows.MEM_REPLACE_PLACEHOLDER,
                NativeMethods_Windows.PAGE_READWRITE,
                IntPtr.Zero,
                0
            );

            if (_buffer == IntPtr.Zero)
            {
                throw new InvalidOperationException($"Mapping file view failed. ({Marshal.GetLastPInvokeError()})");
            }

            // Map the same region to the second half of the virtual space
            var view2 = NativeMethods_Windows.MapViewOfFile3(
                _handle,
                IntPtr.Zero,
                new IntPtr(_buffer + adjustedSize),
                0,
                adjustedSize,
                NativeMethods_Windows.MEM_REPLACE_PLACEHOLDER,
                NativeMethods_Windows.PAGE_READWRITE,
                IntPtr.Zero,
                0
            );

            if (view2 == IntPtr.Zero)
            {
                throw new InvalidOperationException($"Mapping file view mirror failed. ({Marshal.GetLastPInvokeError()})");
            }
        }
        else if (Core.IsLinux || Core.IsDarwin)
        {
            var anon = Core.IsLinux ? NativeMethods_Linux.MAP_ANONYMOUS : NativeMethods_Linux.MAP_ANON;

            int fd;

            if (Core.IsLinux)
            {
                // Create a memory-backed file descriptor
                fd = NativeMethods_Linux.memfd_create("mirrored_ring_buffer", 0);
            }
            else
            {
                var fdName = $"/muo/ring/{GetHashCode()}";
                fd = NativeMethods_Linux.shm_open(fdName, NativeMethods_Linux.O_CREAT | NativeMethods_Linux.O_RDWR, 0600);

                // Unlink immediately to emulate memfd_create() functionality
                NativeMethods_Linux.shm_unlink(fdName);
            }

            if (fd == NativeMethods_Linux.InvalidPtrValue)
            {
                throw new InvalidOperationException($"Creating file descriptor failed. ({Marshal.GetLastPInvokeError()})");
            }

            // Set the size of the file descriptor
            if (NativeMethods_Linux.ftruncate(fd, (int)adjustedSize) != 0)
            {
                throw new InvalidOperationException($"Setting file descriptor size failed. ({Marshal.GetLastPInvokeError()})");
            }

            // Get virtual address space, must be double the size so we can map twice
            _buffer = NativeMethods_Linux.mmap(IntPtr.Zero, adjustedSize * 2,
                NativeMethods_Linux.PROT_READ | NativeMethods_Linux.PROT_WRITE,
                NativeMethods_Linux.MAP_PRIVATE | anon, NativeMethods_Linux.InvalidFileDescriptor, 0);

            if (_buffer == NativeMethods_Linux.InvalidPtrValue)
            {
                throw new InsufficientMemoryException($"Allocating virtual memory failed. ({Marshal.GetLastPInvokeError()})");
            }

            // Map the file descriptor to the first half of the virtual space
            var view1 = NativeMethods_Linux.mmap(_buffer, adjustedSize,
                NativeMethods_Linux.PROT_READ | NativeMethods_Linux.PROT_WRITE,
                NativeMethods_Linux.MAP_SHARED | NativeMethods_Linux.MAP_FIXED, fd, 0);

            if (view1 == NativeMethods_Linux.InvalidPtrValue)
            {
                throw new InvalidOperationException($"Mapping memory failed. ({Marshal.GetLastPInvokeError()})");
            }

            // Map the file descriptor to the second half of the virtual space
            var view2 = NativeMethods_Linux.mmap(new IntPtr(_buffer + adjustedSize), adjustedSize,
                NativeMethods_Linux.PROT_READ | NativeMethods_Linux.PROT_WRITE,
                NativeMethods_Linux.MAP_SHARED | NativeMethods_Linux.MAP_FIXED, fd, 0);

            if (view2 == NativeMethods_Linux.InvalidPtrValue)
            {
                throw new InvalidOperationException($"Mapping mirrored memory failed. ({Marshal.GetLastPInvokeError()})");
            }

            _handle = fd;
        }

        _bufferSize = adjustedSize;
        _writeIdx = 0;
        _readIdx = 0;
        _closed = false;

        Writer = new PipeWriter(this);
        Reader = new PipeReader(this);
    }

    private static partial class NativeMethods_Windows
    {
        private const string Kernel32 = "kernel32.dll";
        private const string KernelBase = "kernelbase.dll";
        public const IntPtr InvalidHandleValue = -1;

        [LibraryImport(Kernel32, SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
        public static partial IntPtr CreateFileMappingW(
            IntPtr hFile, IntPtr lpFileMappingAttributes, uint flProtect, uint dwMaximumSizeHigh, uint dwMaximumSizeLow,
            string lpName
        );

        [LibraryImport(KernelBase, SetLastError = true)]
        public static partial IntPtr MapViewOfFile3(
            IntPtr hFileMappingObject, IntPtr processHandle, IntPtr pvBaseAddress, ulong ullOffset, ulong ullSize,
            uint allocFlags, uint dwDesiredAccess,
            IntPtr hExtendedParameter, int parameterCount
        );

        [LibraryImport(Kernel32, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool UnmapViewOfFile(IntPtr lpBaseAddress);

        [LibraryImport(Kernel32, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool CloseHandle(IntPtr hObject);

        [LibraryImport(KernelBase, SetLastError = true)]
        public static partial IntPtr VirtualAlloc2(
            IntPtr process,
            IntPtr address,
            ulong size,
            uint allocationType,
            uint protect,
            IntPtr extendedParameters,
            uint parameterCount
        );

        [LibraryImport(Kernel32, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool VirtualFree(IntPtr lpAddress, uint dwSize, uint dwFreeType);

        public const uint MEM_PRESERVE_PLACEHOLDER = 0x02;
        public const uint MEM_RESERVE = 0x2000;
        public const uint MEM_REPLACE_PLACEHOLDER = 0x4000;
        public const uint MEM_RELEASE = 0x8000;
        public const uint MEM_RESERVE_PLACEHOLDER = 0x40000;
        public const uint PAGE_NOACCESS = 0x01;
        public const uint PAGE_READWRITE = 0x04;
    }

    private static partial class NativeMethods_Linux
    {
        private const string LibC = "libc";
        public const IntPtr InvalidPtrValue = -1;
        public const int InvalidFileDescriptor = -1;

        // For MacOS
        [LibraryImport(LibC, SetLastError = true, StringMarshalling = StringMarshalling.Utf8)]
        public static partial int shm_open(string name, int oflag, int mode);

        [LibraryImport(LibC, SetLastError = true, StringMarshalling = StringMarshalling.Utf8)]
        public static partial int shm_unlink(string name);

        [LibraryImport(LibC, SetLastError = true, StringMarshalling = StringMarshalling.Utf8)]
        public static partial int memfd_create(string name, uint flags);

        [LibraryImport(LibC, SetLastError = true)]
        public static partial int ftruncate(int fd, int length);

        [LibraryImport(LibC, SetLastError = true)]
        public static partial int close(int fd);

        [LibraryImport(LibC, SetLastError = true)]
        public static partial IntPtr mmap(IntPtr addr, ulong length, int prot, int flags, int fd, int offset);

        [LibraryImport(LibC, SetLastError = true)]
        public static partial int munmap(IntPtr addr, ulong length);

        public const int PROT_READ = 0x1;
        public const int PROT_WRITE = 0x2;
        public const int MAP_PRIVATE = 0x02;
        public const int MAP_SHARED = 0x01;
        public const int MAP_FIXED = 0x10;
        public const int MAP_ANONYMOUS = 0x20;

        // Darwin
        public const int O_RDWR = 0x2;
        public const int O_CREAT = 0x200;
        public const int MAP_ANON = 0x1000;
    }

    private void ReleaseUnmanagedResources()
    {
        if (_buffer == IntPtr.Zero)
        {
            return;
        }

        if (Core.IsWindows)
        {
            if (_handle != IntPtr.Zero)
            {
                NativeMethods_Windows.CloseHandle(_handle);
                _handle = IntPtr.Zero;
            }

            if (_buffer != IntPtr.Zero)
            {
                NativeMethods_Windows.UnmapViewOfFile(_buffer);
                NativeMethods_Windows.UnmapViewOfFile(new IntPtr(_buffer + _bufferSize));
            }
        }
        else if (Core.IsLinux || Core.IsDarwin)
        {
            if (_handle != NativeMethods_Linux.InvalidFileDescriptor)
            {
#pragma warning disable CA2020
                NativeMethods_Linux.close((int)_handle);
#pragma warning restore CA2020

                _handle = NativeMethods_Linux.InvalidFileDescriptor;
            }

            if (_buffer != IntPtr.Zero)
            {
                NativeMethods_Linux.munmap(_buffer, _bufferSize);
                NativeMethods_Linux.munmap(new IntPtr(_buffer + _bufferSize), _bufferSize);
            }
        }

        _buffer = IntPtr.Zero;
    }

    public void Dispose()
    {
        ReleaseUnmanagedResources();
        GC.SuppressFinalize(this);
    }

    ~Pipe()
    {
        ReleaseUnmanagedResources();
    }
}

public class EndOfPipeException : IOException
{
    public EndOfPipeException(string message) : base(message)
    {
    }
}
