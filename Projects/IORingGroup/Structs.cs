// SPDX-License-Identifier: BSD-3-Clause
// Copyright (c) 2025, ModernUO

using System.Runtime.InteropServices;

namespace System.Network;

/// <summary>
/// Represents a completed I/O operation from the completion queue.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public readonly struct Completion
{
    /// <summary>
    /// User-provided data associated with this operation.
    /// </summary>
    public readonly ulong UserData;

    /// <summary>
    /// Result of the operation. Positive values indicate bytes transferred,
    /// negative values indicate errors (negated errno on Linux, HRESULT on Windows).
    /// </summary>
    public readonly int Result;

    /// <summary>
    /// Flags providing additional information about the completion.
    /// </summary>
    public readonly CompletionFlags Flags;

    public Completion(ulong userData, int result, CompletionFlags flags = CompletionFlags.None)
    {
        UserData = userData;
        Result = result;
        Flags = flags;
    }
}

/// <summary>
/// Flags returned with completions providing additional context.
/// </summary>
[Flags]
public enum CompletionFlags : uint
{
    None = 0,
    /// <summary>
    /// Buffer index is valid in the completion.
    /// </summary>
    Buffer = 1 << 0,
    /// <summary>
    /// More completions are available and should be processed.
    /// </summary>
    More = 1 << 1,
    /// <summary>
    /// Socket has been disconnected (hangup).
    /// </summary>
    SockNoData = 1 << 2,
    /// <summary>
    /// Notification-only completion, no actual I/O performed.
    /// </summary>
    Notif = 1 << 3,
}

/// <summary>
/// Poll event mask for poll operations, matching Linux poll(2) semantics.
/// </summary>
[Flags]
public enum PollMask : uint
{
    None = 0,
    /// <summary>
    /// Data available for reading.
    /// </summary>
    In = 0x0001,
    /// <summary>
    /// Urgent data available for reading.
    /// </summary>
    Pri = 0x0002,
    /// <summary>
    /// Writing is possible.
    /// </summary>
    Out = 0x0004,
    /// <summary>
    /// Error condition.
    /// </summary>
    Err = 0x0008,
    /// <summary>
    /// Hang up (peer closed connection).
    /// </summary>
    Hup = 0x0010,
    /// <summary>
    /// Invalid request (fd not open).
    /// </summary>
    Nval = 0x0020,
    /// <summary>
    /// Normal data available for reading.
    /// </summary>
    RdNorm = 0x0040,
    /// <summary>
    /// Priority band data available for reading.
    /// </summary>
    RdBand = 0x0080,
    /// <summary>
    /// Writing normal data is possible.
    /// </summary>
    WrNorm = 0x0100,
    /// <summary>
    /// Writing priority data is possible.
    /// </summary>
    WrBand = 0x0200,
    /// <summary>
    /// Extension: stream socket peer closed connection, or shut down writing half.
    /// </summary>
    RdHup = 0x2000,
}

/// <summary>
/// Operation codes for submission queue entries.
/// </summary>
public enum IORingOp : byte
{
    Nop = 0,
    PollAdd = 6,
    PollRemove = 7,
    Accept = 13,
    Connect = 16,
    Send = 26,
    Recv = 27,
    Close = 19,
    Cancel = 28,
    Shutdown = 34,
    SendZc = 53,  // Zero-copy send
}

/// <summary>
/// Flags for submission queue entries.
/// </summary>
[Flags]
public enum SubmissionFlags : byte
{
    None = 0,
    /// <summary>
    /// Use fixed file (pre-registered file descriptor).
    /// </summary>
    FixedFile = 1 << 0,
    /// <summary>
    /// Issue operation after inflight operations complete.
    /// </summary>
    IoDrain = 1 << 1,
    /// <summary>
    /// Link operations together.
    /// </summary>
    IoLink = 1 << 2,
    /// <summary>
    /// Hard link operations (fail subsequent if this fails).
    /// </summary>
    IoHardlink = 1 << 3,
    /// <summary>
    /// Always go async (don't try sync).
    /// </summary>
    Async = 1 << 4,
    /// <summary>
    /// Use registered buffer.
    /// </summary>
    BufferSelect = 1 << 5,
    /// <summary>
    /// Don't post CQE if request succeeds.
    /// </summary>
    CqeSkipSuccess = 1 << 6,
}

/// <summary>
/// Message flags for send/recv operations.
/// </summary>
[Flags]
public enum MsgFlags
{
    None = 0,
    /// <summary>
    /// Process out-of-band data.
    /// </summary>
    Oob = 0x0001,
    /// <summary>
    /// Peek at incoming message.
    /// </summary>
    Peek = 0x0002,
    /// <summary>
    /// Send without using routing tables.
    /// </summary>
    DontRoute = 0x0004,
    /// <summary>
    /// Data completes record.
    /// </summary>
    Eor = 0x0008,
    /// <summary>
    /// Data discarded before delivery.
    /// </summary>
    Trunc = 0x0010,
    /// <summary>
    /// Control data lost before delivery.
    /// </summary>
    Ctrunc = 0x0020,
    /// <summary>
    /// Wait for full request or error.
    /// </summary>
    WaitAll = 0x0040,
    /// <summary>
    /// Do not block.
    /// </summary>
    DontWait = 0x0080,
}
