// SPDX-License-Identifier: BSD-3-Clause
// Copyright (c) 2025, ModernUO

using System.Network.IORing;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Network.Linux;

/// <summary>
/// Linux syscall bindings for io_uring.
/// Works on both x64 and ARM64 - syscall numbers and constants are identical
/// for io_uring (added after Linux unified new syscall numbers across architectures).
/// </summary>
public static partial class LinuxIORing
{
    // Syscall numbers (same on x64 and ARM64 for io_uring)
    private const int SYS_io_uring_setup = 425;
    private const int SYS_io_uring_enter = 426;
    private const int SYS_io_uring_register = 427;

    // mmap constants
    public const int PROT_READ = 0x1;
    public const int PROT_WRITE = 0x2;
    public const int MAP_SHARED = 0x01;
    public const int MAP_POPULATE = 0x8000;

    // io_uring_register opcodes
    public const uint IORING_REGISTER_BUFFERS = 0;
    public const uint IORING_UNREGISTER_BUFFERS = 1;
    public const uint IORING_REGISTER_FILES = 2;
    public const uint IORING_UNREGISTER_FILES = 3;
    public const uint IORING_REGISTER_EVENTFD = 4;

    // eventfd flags
    public const int EFD_NONBLOCK = 0x800;

    // poll constants
    public const short POLLIN = 0x0001;

    [StructLayout(LayoutKind.Sequential)]
    public struct pollfd
    {
        public int fd;
        public short events;
        public short revents;
    }

    // mmap offsets for io_uring
    public const ulong IORING_OFF_SQ_RING = 0;
    public const ulong IORING_OFF_CQ_RING = 0x8000000;
    public const ulong IORING_OFF_SQES = 0x10000000;

    // Socket constants
    public const int AF_INET = 2;
    public const int SOCK_STREAM = 1;
    public const int SOCK_NONBLOCK = 0x800;
    public const int IPPROTO_TCP = 6;
    public const int SOL_SOCKET = 1;
    public const int SO_REUSEADDR = 2;
    public const int SO_LINGER = 13;
    public const int TCP_NODELAY = 1;
    public const int F_SETFL = 4;
    public const int F_GETFL = 3;
    public const int O_NONBLOCK = 0x800;

    // libc bindings - memory
    [LibraryImport("libc", SetLastError = true)]
    public static partial nint mmap(nint addr, nuint length, int prot, int flags, int fd, long offset);

    [LibraryImport("libc", SetLastError = true)]
    public static partial int munmap(nint addr, nuint length);

    [LibraryImport("libc", SetLastError = true)]
    public static partial int close(int fd);

    [LibraryImport("libc", SetLastError = true)]
    public static partial int shutdown(int sockfd, int how);

    // libc bindings - sockets
    [LibraryImport("libc", SetLastError = true)]
    public static partial int socket(int domain, int type, int protocol);

    [LibraryImport("libc", SetLastError = true)]
    public static partial int bind(int sockfd, nint addr, int addrlen);

    [LibraryImport("libc", SetLastError = true)]
    public static partial int listen(int sockfd, int backlog);

    [LibraryImport("libc", SetLastError = true)]
    public static partial int setsockopt(int sockfd, int level, int optname, nint optval, int optlen);

    [LibraryImport("libc", SetLastError = true)]
    public static partial int fcntl(int fd, int cmd, int arg);

    [LibraryImport("libc", SetLastError = true)]
    public static partial int eventfd(uint initval, int flags);

    [LibraryImport("libc", SetLastError = true)]
    public static partial int poll(nint fds, nuint nfds, int timeout);

    [LibraryImport("libc", SetLastError = true)]
    public static partial long read(int fd, nint buf, nuint count);

    [LibraryImport("libc", SetLastError = true)]
    public static partial long write(int fd, nint buf, nuint count);

    /// <summary>
    /// io_uring_setup syscall wrapper.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe int io_uring_setup(uint entries, ref io_uring_params p)
    {
        fixed (io_uring_params* pp = &p)
        {
            return (int)syscall_raw(SYS_io_uring_setup, entries, (nint)pp);
        }
    }

    /// <summary>
    /// io_uring_enter syscall wrapper.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int io_uring_enter(int fd, uint to_submit, uint min_complete, uint flags) =>
        (int)syscall_raw(SYS_io_uring_enter, fd, to_submit, min_complete, flags, 0);

    /// <summary>
    /// io_uring_register syscall wrapper.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int io_uring_register(int fd, uint opcode, nint arg, uint nr_args) =>
        (int)syscall_raw(SYS_io_uring_register, fd, opcode, arg, nr_args);

    // Raw syscall implementation
    [LibraryImport("libc", EntryPoint = "syscall", SetLastError = true)]
    private static partial long syscall_raw(int number, uint arg1, nint arg2);

    [LibraryImport("libc", EntryPoint = "syscall", SetLastError = true)]
    private static partial long syscall_raw(int number, int arg1, uint arg2, uint arg3, uint arg4, nint arg5);

    [LibraryImport("libc", EntryPoint = "syscall", SetLastError = true)]
    private static partial long syscall_raw(int number, int arg1, uint arg2, nint arg3, uint arg4);
}
