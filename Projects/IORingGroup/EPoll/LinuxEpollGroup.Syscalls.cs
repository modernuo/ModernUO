// SPDX-License-Identifier: BSD-3-Clause
// Copyright (c) 2025, ModernUO

using System.Runtime.InteropServices;

namespace System.Network.EPoll;

public sealed unsafe partial class LinuxEpollGroup
{
    // epoll constants
    private const int EPOLL_CLOEXEC = 0x80000;
    private const int EFD_NONBLOCK = 0x800;

    // Socket constants (duplicated from LinuxIORing for backend independence)
    private const int AF_INET = 2;
    private const int SOCK_STREAM = 1;
    private const int SOCK_NONBLOCK = 0x800;
    private const int IPPROTO_TCP = 6;
    private const int SOL_SOCKET = 1;
    private const int SO_REUSEADDR = 2;
    private const int SO_LINGER = 13;
    private const int SO_ERROR = 4;
    private const int TCP_NODELAY = 1;
    private const int F_GETFL = 3;
    private const int F_SETFL = 4;
    private const int O_NONBLOCK = 0x800;
    private const int EAGAIN = 11;
    private const int EWOULDBLOCK = EAGAIN;
    private const int EINPROGRESS = 115;
    private const int EINTR = 4;

    private static partial class Syscalls
    {
        [LibraryImport("libc", SetLastError = true)]
        public static partial int epoll_create1(int flags);

        [LibraryImport("libc", SetLastError = true)]
        public static partial int epoll_ctl(int epfd, int op, int fd, nint ev);

        [LibraryImport("libc", SetLastError = true)]
        public static partial int epoll_wait(int epfd, nint events, int maxevents, int timeout);

        [LibraryImport("libc", SetLastError = true)]
        public static partial int close(int fd);

        [LibraryImport("libc", SetLastError = true)]
        public static partial int shutdown(int sockfd, int how);

        [LibraryImport("libc", SetLastError = true)]
        public static partial int socket(int domain, int type, int protocol);

        [LibraryImport("libc", SetLastError = true)]
        public static partial int bind(int sockfd, nint addr, int addrlen);

        [LibraryImport("libc", SetLastError = true)]
        public static partial int listen(int sockfd, int backlog);

        [LibraryImport("libc", SetLastError = true)]
        public static partial int accept(int sockfd, nint addr, int* addrlen);

        [LibraryImport("libc", SetLastError = true)]
        public static partial int connect(int sockfd, nint addr, int addrlen);

        [LibraryImport("libc", SetLastError = true)]
        public static partial nint send(int sockfd, nint buf, nuint len, int flags);

        [LibraryImport("libc", SetLastError = true)]
        public static partial nint recv(int sockfd, nint buf, nuint len, int flags);

        [LibraryImport("libc", SetLastError = true)]
        public static partial int setsockopt(int sockfd, int level, int optname, nint optval, int optlen);

        [LibraryImport("libc", SetLastError = true)]
        public static partial int getsockopt(int sockfd, int level, int optname, nint optval, ref int optlen);

        [LibraryImport("libc", SetLastError = true)]
        public static partial int fcntl(int fd, int cmd, int arg);

        [LibraryImport("libc", SetLastError = true)]
        public static partial int eventfd(uint initval, int flags);

        [LibraryImport("libc", SetLastError = true)]
        public static partial long read(int fd, nint buf, nuint count);

        [LibraryImport("libc", SetLastError = true)]
        public static partial long write(int fd, nint buf, nuint count);
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct sockaddr_in
    {
        public ushort sin_family;
        public ushort sin_port;
        public uint sin_addr;
        public ulong sin_zero;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct LingerOption
    {
        public int OnOff;
        public int Seconds;
    }
}
