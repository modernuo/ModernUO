// SPDX-License-Identifier: BSD-3-Clause
// Copyright (c) 2025, ModernUO

using System.Runtime.InteropServices;

namespace System.Network.IORing;

/// <summary>
/// io_uring submission queue entry (SQE) - matches Linux kernel struct.
/// </summary>
[StructLayout(LayoutKind.Explicit, Size = 64)]
internal struct io_uring_sqe
{
    [FieldOffset(0)] public byte opcode;
    [FieldOffset(1)] public byte flags;
    [FieldOffset(2)] public ushort ioprio;
    [FieldOffset(4)] public int fd;
    [FieldOffset(8)] public ulong off;        // offset or addr2
    [FieldOffset(16)] public ulong addr;      // pointer or value
    [FieldOffset(24)] public uint len;
    [FieldOffset(28)] public uint op_flags;   // union: rw_flags, poll_events, etc.
    [FieldOffset(32)] public ulong user_data;
    [FieldOffset(40)] public ushort buf_index;
    [FieldOffset(42)] public ushort buf_group;
    [FieldOffset(44)] public uint personality;
    [FieldOffset(48)] public int splice_fd_in;
    [FieldOffset(52)] public ulong addr3;
}

/// <summary>
/// io_uring completion queue entry (CQE) - matches Linux kernel struct.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct io_uring_cqe
{
    public ulong user_data;
    public int res;
    public uint flags;
}

/// <summary>
/// io_uring_params for io_uring_setup syscall.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct io_uring_params
{
    public uint sq_entries;
    public uint cq_entries;
    public uint flags;
    public uint sq_thread_cpu;
    public uint sq_thread_idle;
    public uint features;
    public uint wq_fd;
    public uint resv0;
    public uint resv1;
    public uint resv2;
    public io_sqring_offsets sq_off;
    public io_cqring_offsets cq_off;
}

[StructLayout(LayoutKind.Sequential)]
public struct io_sqring_offsets
{
    public uint head;
    public uint tail;
    public uint ring_mask;
    public uint ring_entries;
    public uint flags;
    public uint dropped;
    public uint array;
    public uint resv1;
    public ulong resv2;
}

[StructLayout(LayoutKind.Sequential)]
public struct io_cqring_offsets
{
    public uint head;
    public uint tail;
    public uint ring_mask;
    public uint ring_entries;
    public uint overflow;
    public uint cqes;
    public uint flags;
    public uint resv1;
    public ulong resv2;
}

/// <summary>
/// io_uring operation codes.
/// </summary>
internal enum IORING_OP : byte
{
    NOP = 0,
    READV = 1,
    WRITEV = 2,
    FSYNC = 3,
    READ_FIXED = 4,
    WRITE_FIXED = 5,
    POLL_ADD = 6,
    POLL_REMOVE = 7,
    SYNC_FILE_RANGE = 8,
    SENDMSG = 9,
    RECVMSG = 10,
    TIMEOUT = 11,
    TIMEOUT_REMOVE = 12,
    ACCEPT = 13,
    ASYNC_CANCEL = 14,
    LINK_TIMEOUT = 15,
    CONNECT = 16,
    FALLOCATE = 17,
    OPENAT = 18,
    CLOSE = 19,
    FILES_UPDATE = 20,
    STATX = 21,
    READ = 22,
    WRITE = 23,
    FADVISE = 24,
    MADVISE = 25,
    SEND = 26,
    RECV = 27,
    CANCEL = 28,
    SHUTDOWN = 34,
}

/// <summary>
/// io_uring setup flags.
/// </summary>
[Flags]
internal enum IORING_SETUP : uint
{
    IOPOLL = 1 << 0,
    SQPOLL = 1 << 1,
    SQ_AFF = 1 << 2,
    CQSIZE = 1 << 3,
    CLAMP = 1 << 4,
    ATTACH_WQ = 1 << 5,
    R_DISABLED = 1 << 6,
}

/// <summary>
/// io_uring enter flags.
/// </summary>
[Flags]
internal enum IORING_ENTER : uint
{
    GETEVENTS = 1 << 0,
    SQ_WAKEUP = 1 << 1,
    SQ_WAIT = 1 << 2,
    EXT_ARG = 1 << 3,
}

/// <summary>
/// io_uring SQE flags.
/// </summary>
[Flags]
internal enum IOSQE : byte
{
    FIXED_FILE = 1 << 0,
    IO_DRAIN = 1 << 1,
    IO_LINK = 1 << 2,
    IO_HARDLINK = 1 << 3,
    ASYNC = 1 << 4,
    BUFFER_SELECT = 1 << 5,
    CQE_SKIP_SUCCESS = 1 << 6,
}
