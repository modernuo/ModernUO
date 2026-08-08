// SPDX-License-Identifier: BSD-3-Clause
// Copyright (c) 2025, ModernUO

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Network.Windows;

/// <summary>
/// Native struct definitions, P/Invoke declarations, and RIO function table loading
/// for the pure C# Windows RIO implementation.
/// </summary>
internal static unsafe class RIOInterop
{
    // =========================================================================
    // Constants
    // =========================================================================

    public const int AF_INET = 2;
    public const int SOCK_STREAM = 1;
    public const int IPPROTO_TCP = 6;

    public const uint WSA_FLAG_OVERLAPPED = 0x01;
    public const uint WSA_FLAG_REGISTERED_IO = 0x100;

    public const int SOL_SOCKET = 0xFFFF;
    public const int SO_REUSEADDR = 0x0004;
    public const int SO_LINGER = 0x0080;
    public const int SO_UPDATE_ACCEPT_CONTEXT = 0x700B;
    public const int TCP_NODELAY = 0x0001;
    public const int IPPROTO_TCP_LEVEL = 6;

    public const uint FIONBIO = 0x8004667E;

    public const nint INVALID_SOCKET = -1;
    public const int SOCKET_ERROR = -1;

    public const nint RIO_INVALID_CQ = 0;
    public const nint RIO_INVALID_RQ = 0;
    public static readonly nint RIO_INVALID_BUFFERID = unchecked((nint)0xFFFFFFFF);
    public const uint RIO_CORRUPT_CQ = 0xFFFFFFFF;

    public const int WSAEWOULDBLOCK = 10035;
    public const int WSAENOBUFS = 10055;
    public const int WSAENOTSOCK = 10038;

    public const int ERROR_IO_PENDING = 997;
    public const int ERROR_NOT_SUPPORTED = 50;
    public const int ERROR_OUTOFMEMORY = 14;

    public const int EINVAL = 22;
    public const int ENOTCONN = 107;
    public const int ENOSYS = 38;

    public const uint SIO_GET_MULTIPLE_EXTENSION_FUNCTION_POINTER = 0xC8000024;
    public const uint SIO_GET_EXTENSION_FUNCTION_POINTER = 0xC8000006;

    public const uint WAIT_OBJECT_0 = 0;
    public const uint WAIT_TIMEOUT = 258;
    public const uint WAIT_FAILED = 0xFFFFFFFF;
    public const uint INFINITE = 0xFFFFFFFF;

    // CreateWaitableTimerExW. The high-resolution flag requires Windows 10 1803 / Server 2019 and
    // is what lets a sub-millisecond wait be honoured without raising the system timer resolution
    // process-wide via timeBeginPeriod.
    public const uint CREATE_WAITABLE_TIMER_HIGH_RESOLUTION = 0x00000002;
    public const uint TIMER_ALL_ACCESS = 0x1F0003;

    public const int ERROR_IO_INCOMPLETE = 996;

    // OVERLAPPED.Internal holds this while an operation is outstanding; the kernel overwrites it
    // with the final NTSTATUS on completion. Polling it is what HasOverlappedIoCompleted does.
    public const nuint STATUS_PENDING = 0x103;

    // Linux-style poll masks (used by IIORingGroup interface)
    public const short POLLRDNORM = 0x0100;
    public const short POLLRDBAND = 0x0200;
    public const short POLLWRNORM = 0x0010;
    public const short POLLERR = 0x0001;
    public const short POLLHUP = 0x0002;
    public const short POLLNVAL = 0x0004;

    // =========================================================================
    // GUIDs
    // =========================================================================

    public static readonly Guid WSAID_MULTIPLE_RIO = new(
        0x8509e081, 0x96dd, 0x4005, 0xb1, 0x65, 0x9e, 0x2e, 0xe8, 0xc7, 0x9e, 0x3f
    );

    public static readonly Guid WSAID_ACCEPTEX = new(
        0xb5367df1, 0xcbac, 0x11cf, 0x95, 0xca, 0x00, 0x80, 0x5f, 0x48, 0xa1, 0x92
    );

    // =========================================================================
    // Native Structs
    // =========================================================================

    /// <summary>
    /// RIO completion result (24 bytes) - returned by RIODequeueCompletion.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct RIORESULT
    {
        public int Status;
        public uint BytesTransferred;
        public long SocketContext;
        public long RequestContext;
    }

    /// <summary>
    /// RIO buffer descriptor (12 bytes) - passed to RIOSend/RIOReceive.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct RIO_BUF
    {
        public nint BufferId;
        public uint Offset;
        public uint Length;
    }

    /// <summary>
    /// Win32 OVERLAPPED structure for AcceptEx.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct OVERLAPPED
    {
        public nuint Internal;
        public nuint InternalHigh;
        public uint Offset;
        public uint OffsetHigh;
        public nint hEvent;
    }

    /// <summary>
    /// WSADATA structure for WSAStartup.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Size = 408)]
    public struct WSADATA
    {
        public ushort wVersion;
        public ushort wHighVersion;
    }

    /// <summary>
    /// IPv4 socket address structure.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct sockaddr_in
    {
        public short sin_family;
        public ushort sin_port;
        public uint sin_addr;
        public ulong sin_zero;
    }

    /// <summary>
    /// Socket linger option.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct LINGER
    {
        public ushort l_onoff;
        public ushort l_linger;
    }

    /// <summary>
    /// WSAPOLLFD for WSAPoll.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct WSAPOLLFD
    {
        public nint fd;
        public short events;
        public short revents;
    }

    // =========================================================================
    // RIO Function Table
    // =========================================================================

    /// <summary>
    /// RIO_EXTENSION_FUNCTION_TABLE layout matching mswsock.h.
    /// Must be loaded via WSAIoctl with SIO_GET_MULTIPLE_EXTENSION_FUNCTION_POINTER.
    /// </summary>
    /// <remarks>
    /// The table contains 32-bit cbSize then function pointers. On x64 each pointer is 8 bytes.
    /// We store just the function pointers we need after loading.
    /// </remarks>
    [StructLayout(LayoutKind.Sequential)]
    public struct RIO_EXTENSION_FUNCTION_TABLE
    {
        public uint cbSize;
        public nint RIOReceive;
        public nint RIOReceiveEx;
        public nint RIOSend;
        public nint RIOSendEx;
        public nint RIOCloseCompletionQueue;
        public nint RIOCreateCompletionQueue;
        public nint RIOCreateRequestQueue;
        public nint RIODequeueCompletion;
        public nint RIODeregisterBuffer;
        public nint RIONotify;
        public nint RIORegisterBuffer;
        public nint RIOResizeCompletionQueue;
        public nint RIOResizeRequestQueue;
    }

    /// <summary>
    /// Holds the extracted RIO function pointers as unmanaged delegates.
    /// </summary>
    public struct RIOFunctions
    {
        public delegate* unmanaged[Stdcall]<nint, RIO_BUF*, uint, uint, void*, int> RIOReceive;
        public delegate* unmanaged[Stdcall]<nint, RIO_BUF*, uint, uint, void*, int> RIOSend;
        public delegate* unmanaged[Stdcall]<nint, void> RIOCloseCompletionQueue;
        public delegate* unmanaged[Stdcall]<uint, void*, nint> RIOCreateCompletionQueue;
        public delegate* unmanaged[Stdcall]<nint, uint, uint, uint, uint, nint, nint, void*, nint> RIOCreateRequestQueue;
        public delegate* unmanaged[Stdcall]<nint, RIORESULT*, uint, uint> RIODequeueCompletion;
        public delegate* unmanaged[Stdcall]<byte*, uint, nint> RIORegisterBuffer;
        public delegate* unmanaged[Stdcall]<nint, void> RIODeregisterBuffer;
        public delegate* unmanaged[Stdcall]<nint, int> RIONotify;

        public bool IsValid =>
            RIOReceive != null &&
            RIOSend != null &&
            RIOCloseCompletionQueue != null &&
            RIOCreateCompletionQueue != null &&
            RIOCreateRequestQueue != null &&
            RIODequeueCompletion != null &&
            RIORegisterBuffer != null &&
            RIODeregisterBuffer != null &&
            RIONotify != null;
    }

    /// <summary>
    /// RIO notification completion structure for event-based CQ notification.
    /// Passed to RIOCreateCompletionQueue to enable RIONotify support.
    /// </summary>
    [StructLayout(LayoutKind.Explicit, Size = 32)]
    public struct RIO_NOTIFICATION_COMPLETION
    {
        /// <summary>1 = RIO_EVENT_COMPLETION</summary>
        [FieldOffset(0)] public int Type;
        [FieldOffset(8)] public nint EventHandle;
        /// <summary>If TRUE, event is auto-reset when RIONotify is called.</summary>
        [FieldOffset(16)] public int NotifyReset;
    }

    // AcceptEx function pointer is stored per-ring instance in WindowsManagedRIOGroup

    // =========================================================================
    // Initialization
    // =========================================================================

    private static readonly object _initLock = new();
    private static bool _wsaInitialized;

    /// <summary>
    /// Initializes WinSock if not already done.
    /// </summary>
    public static void EnsureWinsockInitialized()
    {
        if (_wsaInitialized)
        {
            return;
        }

        lock (_initLock)
        {
            if (_wsaInitialized)
            {
                return;
            }

            WSADATA wsaData;
            var result = WSAStartup(0x0202, &wsaData);
            if (result != 0)
            {
                throw new InvalidOperationException($"WSAStartup failed: {result}");
            }

            _wsaInitialized = true;
        }
    }

    /// <summary>
    /// Loads the RIO function table by creating a temporary RIO socket and calling WSAIoctl.
    /// </summary>
    public static RIOFunctions LoadRIOFunctions()
    {
        EnsureWinsockInitialized();

        var sock = WSASocketW(AF_INET, SOCK_STREAM, IPPROTO_TCP, null, 0, WSA_FLAG_REGISTERED_IO);
        if (sock == INVALID_SOCKET)
        {
            throw new InvalidOperationException($"Failed to create RIO socket: WSA error {WSAGetLastError()}");
        }

        try
        {
            var table = new RIO_EXTENSION_FUNCTION_TABLE();
            var guid = WSAID_MULTIPLE_RIO;
            uint bytes = 0;

            var ret = WSAIoctl(
                sock,
                SIO_GET_MULTIPLE_EXTENSION_FUNCTION_POINTER,
                &guid, (uint)sizeof(Guid),
                &table, (uint)sizeof(RIO_EXTENSION_FUNCTION_TABLE),
                &bytes, null, null
            );

            if (ret != 0)
            {
                throw new InvalidOperationException($"Failed to load RIO function table: WSA error {WSAGetLastError()}");
            }

            return new RIOFunctions
            {
                RIOReceive = (delegate* unmanaged[Stdcall]<nint, RIO_BUF*, uint, uint, void*, int>)table.RIOReceive,
                RIOSend = (delegate* unmanaged[Stdcall]<nint, RIO_BUF*, uint, uint, void*, int>)table.RIOSend,
                RIOCloseCompletionQueue = (delegate* unmanaged[Stdcall]<nint, void>)table.RIOCloseCompletionQueue,
                RIOCreateCompletionQueue = (delegate* unmanaged[Stdcall]<uint, void*, nint>)table.RIOCreateCompletionQueue,
                RIOCreateRequestQueue = (delegate* unmanaged[Stdcall]<nint, uint, uint, uint, uint, nint, nint, void*, nint>)table.RIOCreateRequestQueue,
                RIODequeueCompletion = (delegate* unmanaged[Stdcall]<nint, RIORESULT*, uint, uint>)table.RIODequeueCompletion,
                RIORegisterBuffer = (delegate* unmanaged[Stdcall]<byte*, uint, nint>)table.RIORegisterBuffer,
                RIODeregisterBuffer = (delegate* unmanaged[Stdcall]<nint, void>)table.RIODeregisterBuffer,
                RIONotify = (delegate* unmanaged[Stdcall]<nint, int>)table.RIONotify,
            };
        }
        finally
        {
            closesocket(sock);
        }
    }

    /// <summary>
    /// Loads the AcceptEx function pointer from a listening socket.
    /// </summary>
    public static delegate* unmanaged[Stdcall]<nint, nint, void*, uint, uint, uint, uint*, OVERLAPPED*, int> LoadAcceptEx(nint listenSocket)
    {
        var guid = WSAID_ACCEPTEX;
        nint fnPtr;
        uint bytes = 0;

        var ret = WSAIoctl(
            listenSocket,
            SIO_GET_EXTENSION_FUNCTION_POINTER,
            &guid, (uint)sizeof(Guid),
            &fnPtr, (uint)sizeof(nint),
            &bytes, null, null
        );

        if (ret != 0)
        {
            return null;
        }

        return (delegate* unmanaged[Stdcall]<nint, nint, void*, uint, uint, uint, uint*, OVERLAPPED*, int>)fnPtr;
    }

    // =========================================================================
    // Poll Mask Translation (Linux style <-> Windows WSAPoll)
    // =========================================================================

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static short LinuxToWindowsPoll(uint linuxMask)
    {
        short winMask = 0;
        if ((linuxMask & 0x0001) != 0) winMask |= POLLRDNORM;  // POLLIN
        if ((linuxMask & 0x0002) != 0) winMask |= POLLRDBAND;  // POLLPRI
        if ((linuxMask & 0x0004) != 0) winMask |= POLLWRNORM;  // POLLOUT
        if ((linuxMask & 0x0008) != 0) winMask |= POLLERR;     // POLLERR
        if ((linuxMask & 0x0010) != 0) winMask |= POLLHUP;     // POLLHUP
        if ((linuxMask & 0x0020) != 0) winMask |= POLLNVAL;    // POLLNVAL
        return winMask;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint WindowsToLinuxPoll(short winMask)
    {
        uint linuxMask = 0;
        if ((winMask & POLLRDNORM) != 0) linuxMask |= 0x0001;  // POLLIN
        if ((winMask & POLLRDBAND) != 0) linuxMask |= 0x0002;  // POLLPRI
        if ((winMask & POLLWRNORM) != 0) linuxMask |= 0x0004;  // POLLOUT
        if ((winMask & POLLERR) != 0)    linuxMask |= 0x0008;  // POLLERR
        if ((winMask & POLLHUP) != 0)    linuxMask |= 0x0010;  // POLLHUP
        if ((winMask & POLLNVAL) != 0)   linuxMask |= 0x0020;  // POLLNVAL
        return linuxMask;
    }

    // =========================================================================
    // P/Invoke: ws2_32.dll
    // =========================================================================

    [DllImport("ws2_32.dll")]
    public static extern int WSAStartup(ushort wVersionRequired, WSADATA* lpWSAData);

    [DllImport("ws2_32.dll")]
    public static extern nint WSASocketW(
        int af, int type, int protocol,
        void* lpProtocolInfo, uint g, uint dwFlags
    );

    [DllImport("ws2_32.dll")]
    public static extern int WSAIoctl(
        nint s, uint dwIoControlCode,
        void* lpvInBuffer, uint cbInBuffer,
        void* lpvOutBuffer, uint cbOutBuffer,
        uint* lpcbBytesReturned,
        void* lpOverlapped, void* lpCompletionRoutine
    );

    [DllImport("ws2_32.dll")]
    public static extern int closesocket(nint s);

    [DllImport("ws2_32.dll")]
    public static extern int shutdown(nint s, int how);

    [DllImport("ws2_32.dll")]
    public static extern int ioctlsocket(nint s, uint cmd, uint* argp);

    [DllImport("ws2_32.dll")]
    public static extern int setsockopt(nint s, int level, int optname, void* optval, int optlen);

    [DllImport("ws2_32.dll")]
    public static extern int bind(nint s, sockaddr_in* name, int namelen);

    [DllImport("ws2_32.dll")]
    public static extern int listen(nint s, int backlog);

    [DllImport("ws2_32.dll")]
    public static extern int WSAPoll(WSAPOLLFD* fdarray, uint nfds, int timeout);

    [DllImport("ws2_32.dll")]
    public static extern int WSAGetLastError();

    [DllImport("ws2_32.dll")]
    public static extern void WSASetLastError(int iError);

    [DllImport("ws2_32.dll", CharSet = CharSet.Ansi, ExactSpelling = true)]
    public static extern int inet_pton(int family, string src, uint* dst);

    [DllImport("ws2_32.dll")]
    public static extern ushort htons(ushort hostshort);

    // =========================================================================
    // P/Invoke: kernel32.dll
    // =========================================================================

    [DllImport("kernel32.dll")]
    public static extern nint CreateEventW(void* lpEventAttributes, int bManualReset, int bInitialState, void* lpName);

    [DllImport("kernel32.dll")]
    public static extern int ResetEvent(nint hEvent);

    [DllImport("kernel32.dll")]
    public static extern int CloseHandle(nint hObject);

    [DllImport("kernel32.dll")]
    public static extern uint WaitForSingleObject(nint hHandle, uint dwMilliseconds);

    [DllImport("kernel32.dll")]
    public static extern int SetEvent(nint hEvent);

    [DllImport("kernel32.dll")]
    public static extern uint WaitForMultipleObjects(uint nCount, nint* lpHandles, int bWaitAll, uint dwMilliseconds);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern nint CreateWaitableTimerExW(void* lpTimerAttributes, void* lpTimerName, uint dwFlags, uint dwDesiredAccess);

    [DllImport("kernel32.dll")]
    public static extern int SetWaitableTimer(
        nint hTimer,
        long* lpDueTime,
        int lPeriod,
        void* pfnCompletionRoutine,
        void* lpArgToCompletionRoutine,
        int fResume
    );

    [DllImport("kernel32.dll")]
    public static extern int GetOverlappedResult(nint hFile, OVERLAPPED* lpOverlapped, uint* lpNumberOfBytesTransferred, int bWait);

    [DllImport("kernel32.dll")]
    public static extern int CancelIoEx(nint hFile, OVERLAPPED* lpOverlapped);
}
