// SPDX-License-Identifier: BSD-3-Clause
// Copyright (c) 2025, ModernUO

using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static System.Network.Windows.RIOInterop;

namespace System.Network.Windows;

/// <summary>
/// Pure C# Windows RIO (Registered I/O) implementation.
/// Eliminates the native ioring.dll dependency by calling RIO function pointers directly.
/// </summary>
public sealed unsafe class WindowsManagedRIOGroup : IIORingGroup
{
    // =========================================================================
    // Internal State Structs (NativeMemory-allocated, zero GC pressure)
    // =========================================================================

    private struct RioConnection
    {
        public nint Socket;
        public nint RequestQueue;
        public bool Active;
        public bool Reserved;
    }

    // AcceptEx address buffer size: (sizeof(SOCKADDR_STORAGE) + 16) * 2 = 288
    private const int AcceptExAddrSize = 128 + 16; // sizeof(SOCKADDR_STORAGE) = 128 on x64
    private const int AcceptExBufferSize = AcceptExAddrSize * 2;

    private struct AcceptExContext
    {
        public nint AcceptSocket;
        public nint ListenSocket;
        public fixed byte Buffer[AcceptExBufferSize];
        public OVERLAPPED Overlapped;
        public ulong UserData;
        public bool Pending;
        public int ConnSlot;
    }

    private struct ManagedSqe
    {
        public byte Opcode;
        public byte Flags;
        public int Fd;
        public uint Offset;
        public uint Len;
        public uint PollEvents;
        public ushort BufIndex;
        public ulong UserData;
        public ulong Addr; // For poll_remove target, connect addr, etc.
        public int AddrLen;
    }

    private struct PendingOp
    {
        public ulong UserData;
        public byte Opcode;
        public nint Fd;
        public uint Flags;
    }

    // RIO: outstanding recv ops per socket. One is enough - a new recv is posted on completion.
    private const uint RIO_OUTSTANDING_RECV_PER_SOCKET = 1;

    // Ceiling on configured sends-in-flight. RIO reserves per-request-queue resources from
    // non-paged pool at creation, so an unbounded value fails socket creation under load rather
    // than at startup.
    public const int MaxConfigurableOutstandingSends = 64;

    private readonly uint _outstandingSendsPerSocket;

    /// <inheritdoc/>
    public int MaxOutstandingSendsPerSocket => (int)_outstandingSendsPerSocket;

    // =========================================================================
    // Fields
    // =========================================================================

    private readonly RIOFunctions _rio;
    private delegate* unmanaged[Stdcall]<nint, nint, void*, uint, uint, uint, uint*, OVERLAPPED*, int> _fnAcceptEx;

    // Queue configuration
    private readonly uint _sqEntries;
    private readonly uint _cqEntries;
    private readonly uint _sqMask;
    private readonly uint _cqMask;

    // Submission queue (user-space)
    private readonly ManagedSqe* _sq;
    private uint _sqHead;
    private uint _sqTail;

    // Completion queue (user-space, stores Completion directly)
    private readonly Completion* _cq;
    private uint _cqHead;
    private uint _cqTail;

    // RIO state
    private readonly nint _rioCq;
    private readonly uint _rioMaxConnections;
    private readonly RioConnection* _connections;

    // Owned listeners
    private nint* _ownedListeners;
    private uint _ownedListenerCount;
    private uint _ownedListenerCapacity;

    // AcceptEx pool
    private readonly AcceptExContext* _acceptPool;
    private readonly uint _acceptPoolSize;
    private uint _pendingAcceptCount;
    private uint _acceptCheckCounter;
    private bool _acceptsFoundLastCheck;

    // Pending operations (for poll, legacy accept)
    private readonly PendingOp* _pendingOps;
    private uint _pendingCount;
    private readonly uint _pendingCapacity;

    // External buffer tracking
    private readonly uint _maxExternalBuffers;
    private readonly nint* _externalBufferIds;
    private readonly byte** _externalBufferPtrs;
    private readonly uint* _externalBufferLens;
    private uint _externalBufferCount;

    private readonly nint _completionEvent;

    // Auto-reset, and part of the WaitForCompletion wait set. Auto-reset is what makes Wake()
    // sticky: a SetEvent landing before the wait is consumed by that wait rather than lost.
    private readonly nint _wakeEvent;

    // High-resolution waitable timer, used instead of the WaitForMultipleObjects timeout so that
    // sub-millisecond waits are honoured. The plain timeout argument is quantised to the system
    // timer resolution (15.625 ms by default), which is far coarser than the loop needs.
    private readonly nint _idleTimer;

    // Signalled by the OS when any pending AcceptEx completes; see the pool setup for why one
    // shared handle suffices.
    private readonly nint _acceptEvent;

    private bool _loggedWaitFailure;
    private volatile bool _disposed;

    // =========================================================================
    // Constructor
    // =========================================================================

    public WindowsManagedRIOGroup(int maxConnections, int maxOutstandingSends = 1)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(maxConnections, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(maxOutstandingSends, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(maxOutstandingSends, MaxConfigurableOutstandingSends);

        _outstandingSendsPerSocket = (uint)maxOutstandingSends;

        // Step 1: Initialize WinSock and load RIO function table
        _rio = LoadRIOFunctions();
        if (!_rio.IsValid)
        {
            throw new InvalidOperationException("Failed to load RIO function pointers");
        }

        // Step 2: Compute queue sizes.
        // Not scaled by the outstanding-send limit: these are user-space staging rings drained each
        // loop iteration, and DequeueRioCompletions back-pressures on them. Only the RIO completion
        // queue below must hold everything outstanding, since it corrupts rather than back-pressures.
        var mc = (uint)maxConnections;
        var queueSize = BitOperations.RoundUpToPowerOf2(mc * 2);

        _sqEntries = queueSize;
        _cqEntries = queueSize;
        _sqMask = queueSize - 1;
        _cqMask = queueSize - 1;
        _rioMaxConnections = mc;

        // Step 3: Allocate all NativeMemory blocks
        _sq = (ManagedSqe*)NativeMemory.AllocZeroed(queueSize, (nuint)sizeof(ManagedSqe));
        _cq = (Completion*)NativeMemory.AllocZeroed(queueSize, (nuint)sizeof(Completion));
        _pendingOps = (PendingOp*)NativeMemory.AllocZeroed(queueSize, (nuint)sizeof(PendingOp));
        _pendingCapacity = queueSize;

        _connections = (RioConnection*)NativeMemory.AllocZeroed(mc, (nuint)sizeof(RioConnection));

        _maxExternalBuffers = mc * 2;
        _externalBufferIds = (nint*)NativeMemory.AllocZeroed(_maxExternalBuffers, (nuint)sizeof(nint));
        _externalBufferPtrs = (byte**)NativeMemory.AllocZeroed(_maxExternalBuffers, (nuint)sizeof(byte*));
        _externalBufferLens = (uint*)NativeMemory.AllocZeroed(_maxExternalBuffers, sizeof(uint));

        // Step 4: Initialize connections
        for (uint i = 0; i < mc; i++)
        {
            _connections[i].Socket = INVALID_SOCKET;
            _connections[i].RequestQueue = RIO_INVALID_RQ;
        }

        // Step 5: Initialize buffer IDs to invalid
        for (uint i = 0; i < _maxExternalBuffers; i++)
        {
            _externalBufferIds[i] = RIO_INVALID_BUFFERID;
        }

        // Step 6: Create completion notification event and RIO completion queue
        _completionEvent = CreateEventW(null, 1, 0, null); // Manual reset event
        if (_completionEvent == 0)
        {
            FreeAllMemory();
            throw new InvalidOperationException("Failed to create completion notification event");
        }

        _wakeEvent = CreateEventW(null, 0, 0, null); // Auto reset event
        if (_wakeEvent == 0)
        {
            CloseHandle(_completionEvent);
            FreeAllMemory();
            throw new InvalidOperationException("Failed to create wake event");
        }

        // Requires Windows 10 1803 / Server 2019. A failure here is not fatal: WaitForCompletion
        // falls back to the coarse WaitForMultipleObjects timeout.
        _idleTimer = CreateWaitableTimerExW(null, null, CREATE_WAITABLE_TIMER_HIGH_RESOLUTION, TIMER_ALL_ACCESS);

        // Must cover every socket holding its full complement of recv + send ops: RIO corrupts a
        // full completion queue (RIO_CORRUPT_CQ) rather than back-pressuring.
        var rioCqSize = mc * (RIO_OUTSTANDING_RECV_PER_SOCKET + _outstandingSendsPerSocket);
        if (rioCqSize < queueSize)
        {
            rioCqSize = queueSize;
        }
        if (rioCqSize > 2_000_000)
        {
            rioCqSize = 2_000_000;
        }

        var notificationCompletion = new RIO_NOTIFICATION_COMPLETION
        {
            Type = 1, // RIO_EVENT_COMPLETION
            EventHandle = _completionEvent,
            NotifyReset = 1 // Auto-reset event on RIONotify
        };
        _rioCq = _rio.RIOCreateCompletionQueue(rioCqSize, &notificationCompletion);
        if (_rioCq == RIO_INVALID_CQ)
        {
            CloseHandle(_completionEvent);
            FreeAllMemory();
            throw new InvalidOperationException("Failed to create RIO completion queue");
        }

        // Step 7: Initialize AcceptEx pool
        var acceptPoolSize = mc < 256 ? mc : 256;
        _acceptPoolSize = acceptPoolSize;
        _acceptPool = (AcceptExContext*)NativeMemory.AllocZeroed(acceptPoolSize, (nuint)sizeof(AcceptExContext));

        for (uint i = 0; i < acceptPoolSize; i++)
        {
            _acceptPool[i].AcceptSocket = INVALID_SOCKET;
            _acceptPool[i].ConnSlot = -1;
        }

        // One event shared by every pending AcceptEx, rather than one each. It exists only to wake
        // a blocked WaitForCompletion -- completion is detected by reading OVERLAPPED.Internal,
        // which needs no handle at all -- so a single signal meaning "some accept finished" is
        // enough, and the scan that follows finds all of them.
        _acceptEvent = CreateEventW(null, 1, 0, null); // Manual reset
        if (_acceptEvent == 0)
        {
            _rio.RIOCloseCompletionQueue(_rioCq);
            NativeMemory.Free(_acceptPool);
            FreeAllMemory();
            throw new InvalidOperationException("Failed to create AcceptEx completion event");
        }

        // Step 8: Initialize owned listeners array
        _ownedListenerCapacity = 16;
        _ownedListeners = (nint*)NativeMemory.AllocZeroed(16, (nuint)sizeof(nint));
    }

    // =========================================================================
    // Properties
    // =========================================================================

    /// <inheritdoc/>
    public int SubmissionQueueSpace
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (int)(_sqEntries - (_sqTail - _sqHead));
    }

    /// <inheritdoc/>
    public int CompletionQueueCount
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (int)(_cqTail - _cqHead);
    }

    /// <summary>
    /// Gets the number of registered external buffers.
    /// </summary>
    public int ExternalBufferCount => (int)_externalBufferCount;

    /// <summary>
    /// Gets the maximum number of external buffers.
    /// </summary>
    public int MaxExternalBuffers => (int)_maxExternalBuffers;

    // =========================================================================
    // GetSqe & CompleteOp (private hot-path helpers)
    // =========================================================================

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private ManagedSqe* GetSqe()
    {
        if (_sqTail - _sqHead >= _sqEntries)
        {
            throw new InvalidOperationException("Submission queue is full");
        }

        var sqe = &_sq[_sqTail & _sqMask];
        Unsafe.InitBlock(sqe, 0, (uint)sizeof(ManagedSqe));
        _sqTail++;
        return sqe;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void CompleteOp(ulong userData, int result, CompletionFlags flags = CompletionFlags.None)
    {
        _cq[_cqTail & _cqMask] = new Completion(userData, result, flags);
        _cqTail++;
    }

    // =========================================================================
    // Prepare Methods (write directly into ManagedSqe, fully inlinable)
    // =========================================================================

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PreparePollAdd(nint fd, PollMask mask, ulong userData)
    {
        var sqe = GetSqe();
        sqe->Opcode = (byte)IORingOp.PollAdd;
        sqe->Fd = (int)fd;
        sqe->PollEvents = (uint)mask;
        sqe->UserData = userData;
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PreparePollRemove(ulong userData)
    {
        var sqe = GetSqe();
        sqe->Opcode = (byte)IORingOp.PollRemove;
        sqe->Addr = userData;
        sqe->UserData = userData;
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PrepareAccept(nint listenFd, nint addr, nint addrLen, ulong userData)
    {
        var sqe = GetSqe();
        sqe->Opcode = (byte)IORingOp.Accept;
        sqe->Fd = (int)listenFd;
        sqe->UserData = userData;
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PrepareConnect(nint fd, nint addr, int addrLen, ulong userData)
    {
        var sqe = GetSqe();
        sqe->Opcode = (byte)IORingOp.Connect;
        sqe->Fd = (int)fd;
        sqe->Addr = (ulong)addr;
        sqe->AddrLen = addrLen;
        sqe->UserData = userData;
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PrepareClose(nint fd, ulong userData)
    {
        var sqe = GetSqe();
        sqe->Opcode = (byte)IORingOp.Close;
        sqe->Fd = (int)fd;
        sqe->UserData = userData;
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PrepareCancel(ulong targetUserData, ulong userData)
    {
        var sqe = GetSqe();
        sqe->Opcode = (byte)IORingOp.Cancel;
        sqe->Addr = targetUserData;
        sqe->UserData = userData;
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PrepareShutdown(nint fd, int how, ulong userData)
    {
        var sqe = GetSqe();
        sqe->Opcode = (byte)IORingOp.Shutdown;
        sqe->Fd = (int)fd;
        sqe->Len = (uint)how;
        sqe->UserData = userData;
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PrepareSendBuffer(int connId, int bufferId, int offset, int length, ulong userData)
    {
        var sqe = GetSqe();
        sqe->Opcode = (byte)IORingOp.Send;
        sqe->Flags = 0x81; // 0x80 = registered buffer, 0x01 = external buffer
        sqe->Fd = connId;
        sqe->BufIndex = (ushort)bufferId;
        sqe->Offset = (uint)offset;
        sqe->Len = (uint)length;
        sqe->UserData = userData;
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PrepareRecvBuffer(int connId, int bufferId, int offset, int length, ulong userData)
    {
        var sqe = GetSqe();
        sqe->Opcode = (byte)IORingOp.Recv;
        sqe->Flags = 0x81; // 0x80 = registered buffer, 0x01 = external buffer
        sqe->Fd = connId;
        sqe->BufIndex = (ushort)bufferId;
        sqe->Offset = (uint)offset;
        sqe->Len = (uint)length;
        sqe->UserData = userData;
    }

    // =========================================================================
    // Submit
    // =========================================================================

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Submit()
    {
        var submitted = 0;
        var toSubmit = _sqTail - _sqHead;

        for (uint i = 0; i < toSubmit; i++)
        {
            var sqe = &_sq[_sqHead & _sqMask];
            ExecuteOp(sqe);
            _sqHead++;
            submitted++;
        }

        return submitted;
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int SubmitAndWait(int waitNr)
    {
        var submitted = Submit();

        while (CompletionQueueCount < waitNr)
        {
            DequeueRioCompletions();
            ProcessPendingPolls();

            if (CompletionQueueCount >= waitNr)
            {
                break;
            }

            Thread.SpinWait(1);
        }

        return submitted;
    }

    // =========================================================================
    // Completion Notification
    // =========================================================================

    /// <inheritdoc/>
    public void WaitForCompletion(int timeoutMs)
    {
        if (_disposed)
        {
            return;
        }

        // Arm notification: resets event (NotifyReset=1) and arms CQ
        _rio.RIONotify(_rioCq);

        var handles = stackalloc nint[4];
        handles[0] = _completionEvent;
        handles[1] = _wakeEvent;
        handles[2] = _acceptEvent;
        var count = 3u;

        var waitMs = (uint)timeoutMs;

        // Prefer the high-resolution timer over the wait's own timeout. WaitForMultipleObjects
        // quantises dwMilliseconds to the system timer resolution (15.625 ms unless something
        // raised it), so a 2 ms request would routinely sleep 8x longer than asked.
        if (_idleTimer != 0 && timeoutMs > 0)
        {
            // Negative means relative, in 100ns units.
            var dueTime = -(long)timeoutMs * 10_000L;
            if (SetWaitableTimer(_idleTimer, &dueTime, 0, null, null, 0) != 0)
            {
                handles[count++] = _idleTimer;
                waitMs = INFINITE;
            }
        }

        var result = WaitForMultipleObjects(count, handles, 0, waitMs);

        if (result == WAIT_FAILED && !_loggedWaitFailure)
        {
            // Degrades to the caller spinning, which is the pre-existing behaviour, but it should
            // not do so silently. Logged once so a persistent failure cannot flood.
            _loggedWaitFailure = true;
            Console.Error.WriteLine(
                $"IORingGroup: WaitForMultipleObjects failed (error {Marshal.GetLastPInvokeError()}); the event loop will spin instead of sleeping."
            );
        }
    }

    /// <inheritdoc/>
    /// <remarks>
    /// The high-resolution waitable timer requires Windows 10 1803 / Server 2019. Without it the
    /// only tool left is the WaitForMultipleObjects timeout, which rounds up to the system timer
    /// resolution -- 15.625ms unless something has raised it process-wide.
    /// </remarks>
    public bool SupportsHighResolutionWait => _idleTimer != 0;

    /// <inheritdoc/>
    public void Wake()
    {
        if (_disposed || _wakeEvent == 0)
        {
            return;
        }

        // Auto-reset, so this latches: if it lands between the caller's idle check and its
        // WaitForCompletion, that wait returns immediately rather than losing the signal.
        SetEvent(_wakeEvent);
    }

    // =========================================================================
    // Execute Operation (dispatch SQE)
    // =========================================================================

    private void ExecuteOp(ManagedSqe* sqe)
    {
        // Handle ACCEPT via AcceptEx
        if (sqe->Opcode == (byte)IORingOp.Accept)
        {
            var listenSocket = (nint)sqe->Fd;
            if (PostAcceptEx(listenSocket, sqe->UserData))
            {
                return; // Pending
            }
            CompleteOp(sqe->UserData, -WSAGetLastError());
            return;
        }

        // External buffer operations (flags = 0x81)
        if ((sqe->Flags & 0x81) == 0x81)
        {
            ExecuteExternalBufferOp(sqe);
            return;
        }

        // RIO mode requires external buffers for RECV/SEND
        if (sqe->Opcode == (byte)IORingOp.Send || sqe->Opcode == (byte)IORingOp.Recv)
        {
            CompleteOp(sqe->UserData, -EINVAL);
            return;
        }

        // Synchronous operations
        ExecuteLegacyOp(sqe);
    }

    private void ExecuteExternalBufferOp(ManagedSqe* sqe)
    {
        var connId = sqe->Fd;
        if (connId < 0 || (uint)connId >= _rioMaxConnections)
        {
            CompleteOp(sqe->UserData, -EINVAL);
            return;
        }

        var conn = &_connections[connId];
        if (!conn->Active || conn->RequestQueue == RIO_INVALID_RQ)
        {
            CompleteOp(sqe->UserData, -ENOTCONN);
            return;
        }

        var extBufId = sqe->BufIndex;
        if (extBufId >= _maxExternalBuffers || _externalBufferIds[extBufId] == RIO_INVALID_BUFFERID)
        {
            CompleteOp(sqe->UserData, -EINVAL);
            return;
        }

        // Validate offset + length
        var offset = sqe->Offset;
        if (offset + sqe->Len > _externalBufferLens[extBufId])
        {
            CompleteOp(sqe->UserData, -EINVAL);
            return;
        }

        var buf = new RIO_BUF
        {
            BufferId = _externalBufferIds[extBufId],
            Offset = offset,
            Length = sqe->Len
        };

        var success = 0;
        var retries = 0;
        const int maxRetries = 3;

        if (sqe->Opcode == (byte)IORingOp.Recv)
        {
            while (success == 0 && retries < maxRetries)
            {
                WSASetLastError(0);
                success = _rio.RIOReceive(conn->RequestQueue, &buf, 1, 0, (void*)sqe->UserData);
                if (success == 0)
                {
                    var err = WSAGetLastError();
                    if (err == WSAENOBUFS && retries < maxRetries - 1)
                    {
                        DequeueRioCompletions();
                        retries++;
                    }
                    else
                    {
                        CompleteOp(sqe->UserData, -err);
                        return;
                    }
                }
            }
        }
        else if (sqe->Opcode == (byte)IORingOp.Send)
        {
            while (success == 0 && retries < maxRetries)
            {
                WSASetLastError(0);
                success = _rio.RIOSend(conn->RequestQueue, &buf, 1, 0, (void*)sqe->UserData);
                if (success == 0)
                {
                    var err = WSAGetLastError();
                    if (err == WSAENOBUFS && retries < maxRetries - 1)
                    {
                        DequeueRioCompletions();
                        retries++;
                    }
                    else
                    {
                        CompleteOp(sqe->UserData, -err);
                        return;
                    }
                }
            }
        }
        // Operation pending, completion will come from RIO CQ
    }

    private void ExecuteLegacyOp(ManagedSqe* sqe)
    {
        var fd = (nint)sqe->Fd;

        switch (sqe->Opcode)
        {
            case (byte)IORingOp.PollAdd:
                if (_pendingCount < _pendingCapacity)
                {
                    _pendingOps[_pendingCount++] = new PendingOp
                    {
                        UserData = sqe->UserData,
                        Opcode = sqe->Opcode,
                        Fd = fd,
                        Flags = sqe->PollEvents
                    };
                }
                break;

            case (byte)IORingOp.Close:
                var closeRes = closesocket(fd);
                CompleteOp(sqe->UserData, closeRes == 0 ? 0 : -WSAGetLastError());
                break;

            case (byte)IORingOp.Shutdown:
                var shutdownRes = shutdown(fd, (int)sqe->Len);
                CompleteOp(sqe->UserData, shutdownRes == 0 ? 0 : -WSAGetLastError());
                break;

            case (byte)IORingOp.Cancel:
                // Cancel not directly supported; complete as no-op
                CompleteOp(sqe->UserData, 0);
                break;

            default:
                CompleteOp(sqe->UserData, -ENOSYS);
                break;
        }
    }

    // =========================================================================
    // RIO Completion Dequeuing
    // =========================================================================

    private void DequeueRioCompletions()
    {
        // AcceptEx scan calls WaitForSingleObject per pending slot.
        // Adaptive throttle: always check if last scan found completions (active accept traffic,
        // e.g. DDoS or connection burst). Only throttle (every 32 calls) when last scan found nothing
        // (steady state with pre-posted accepts sitting idle).
        if (_pendingAcceptCount > 0 && (_acceptsFoundLastCheck || (_acceptCheckCounter++ & 31) == 0))
        {
            var oldCount = _pendingAcceptCount;
            CheckAcceptExCompletions();
            _acceptsFoundLastCheck = _pendingAcceptCount < oldCount;
        }

        if (_rioCq == RIO_INVALID_CQ)
        {
            return;
        }

        // Check user-space CQ space (back-pressure)
        var cqSpace = _cqEntries - (_cqTail - _cqHead);
        if (cqSpace == 0)
        {
            return;
        }

        var maxDequeue = cqSpace < 256 ? cqSpace : 256;

        var results = stackalloc RIORESULT[256];
        var count = _rio.RIODequeueCompletion(_rioCq, results, maxDequeue);

        if (count == RIO_CORRUPT_CQ)
        {
            return;
        }

        for (uint i = 0; i < count; i++)
        {
            var userData = (ulong)results[i].RequestContext;
            var res = results[i].Status == 0
                ? (int)results[i].BytesTransferred
                : -results[i].Status;
            CompleteOp(userData, res);
        }
    }

    private void ProcessPendingPolls()
    {
        for (uint i = 0; i < _pendingCount;)
        {
            var op = &_pendingOps[i];
            var completed = false;

            if (op->Opcode == (byte)IORingOp.PollAdd)
            {
                var pfd = new WSAPOLLFD
                {
                    fd = op->Fd,
                    events = LinuxToWindowsPoll(op->Flags),
                    revents = 0
                };

                var result = WSAPoll(&pfd, 1, 0);
                if (result > 0)
                {
                    CompleteOp(op->UserData, (int)WindowsToLinuxPoll(pfd.revents));
                    completed = true;
                }
                else if (result < 0)
                {
                    CompleteOp(op->UserData, -WSAGetLastError());
                    completed = true;
                }
            }

            if (completed)
            {
                _pendingOps[i] = _pendingOps[--_pendingCount];
            }
            else
            {
                i++;
            }
        }
    }

    // =========================================================================
    // PeekCompletions & AdvanceCompletionQueue
    // =========================================================================

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int PeekCompletions(Span<Completion> completions)
    {
        // Drain RIO completions + check AcceptEx + process polls
        DequeueRioCompletions();
        ProcessPendingPolls();

        var available = _cqTail - _cqHead;
        var count = Math.Min((int)available, completions.Length);

        for (var i = 0; i < count; i++)
        {
            completions[i] = _cq[(_cqHead + (uint)i) & _cqMask];
        }

        return count;
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AdvanceCompletionQueue(int count)
    {
        _cqHead += (uint)count;
    }

    // =========================================================================
    // Socket Registration
    // =========================================================================

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int RegisterSocket(nint socket)
    {
        if (socket == INVALID_SOCKET)
        {
            return -1;
        }

        // Check if already registered
        for (uint i = 0; i < _rioMaxConnections; i++)
        {
            if (_connections[i].Active && _connections[i].Socket == socket)
            {
                return (int)i;
            }
        }

        // Find free slot
        var slot = -1;
        for (uint i = 0; i < _rioMaxConnections; i++)
        {
            if (!_connections[i].Active)
            {
                slot = (int)i;
                break;
            }
        }

        if (slot < 0)
        {
            return -1;
        }

        var conn = &_connections[slot];

        WSASetLastError(0);
        conn->RequestQueue = _rio.RIOCreateRequestQueue(
            socket,
            RIO_OUTSTANDING_RECV_PER_SOCKET, 1,  // maxRecv, maxRecvBuf
            _outstandingSendsPerSocket, 1,       // maxSend, maxSendBuf
            _rioCq, _rioCq,                 // recvCq, sendCq
            (void*)slot               // context
        );

        if (conn->RequestQueue == RIO_INVALID_RQ)
        {
            return -1;
        }

        conn->Socket = socket;
        conn->Active = true;

        return slot;
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void UnregisterSocket(int connId)
    {
        if (connId < 0 || (uint)connId >= _rioMaxConnections)
        {
            return;
        }

        var conn = &_connections[connId];
        if (!conn->Active)
        {
            return;
        }

        // RQ is automatically cleaned up when socket closes
        conn->Socket = INVALID_SOCKET;
        conn->RequestQueue = RIO_INVALID_RQ;
        conn->Active = false;
    }

    // =========================================================================
    // Buffer Registration
    // =========================================================================

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int RegisterBuffer(IORingBuffer buffer)
    {
        ArgumentNullException.ThrowIfNull(buffer);

        // Find free slot
        var slot = -1;
        for (uint i = 0; i < _maxExternalBuffers; i++)
        {
            if (_externalBufferIds[i] == RIO_INVALID_BUFFERID)
            {
                slot = (int)i;
                break;
            }
        }

        if (slot < 0)
        {
            return -1;
        }

        var bufId = _rio.RIORegisterBuffer((byte*)buffer.Pointer, (uint)buffer.VirtualSize);
        if (bufId == RIO_INVALID_BUFFERID)
        {
            return -1;
        }

        _externalBufferIds[slot] = bufId;
        _externalBufferPtrs[slot] = (byte*)buffer.Pointer;
        _externalBufferLens[slot] = (uint)buffer.VirtualSize;
        _externalBufferCount++;

        return slot;
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void UnregisterBuffer(int bufferId)
    {
        if (bufferId < 0 || (uint)bufferId >= _maxExternalBuffers)
        {
            return;
        }

        if (_externalBufferIds[bufferId] != RIO_INVALID_BUFFERID)
        {
            _rio.RIODeregisterBuffer(_externalBufferIds[bufferId]);
            _externalBufferIds[bufferId] = RIO_INVALID_BUFFERID;
            _externalBufferPtrs[bufferId] = null;
            _externalBufferLens[bufferId] = 0;
            if (_externalBufferCount > 0)
            {
                _externalBufferCount--;
            }
        }
    }

    // =========================================================================
    // Listener Management
    // =========================================================================

    /// <inheritdoc/>
    public nint CreateListener(string bindAddress, ushort port, int backlog)
    {
        EnsureWinsockInitialized();

        // Listener MUST have WSA_FLAG_REGISTERED_IO for AcceptEx sockets to work with RIO
        var listener = WSASocketW(AF_INET, SOCK_STREAM, IPPROTO_TCP, null, 0, WSA_FLAG_REGISTERED_IO);
        if (listener == INVALID_SOCKET)
        {
            return -1;
        }

        // SO_REUSEADDR
        var opt = 1;
        setsockopt(listener, SOL_SOCKET, SO_REUSEADDR, &opt, sizeof(int));

        // Non-blocking
        var mode = 1u;
        ioctlsocket(listener, FIONBIO, &mode);

        // Bind
        var addr = new sockaddr_in
        {
            sin_family = AF_INET,
            sin_port = htons(port)
        };

        if (!string.IsNullOrEmpty(bindAddress) && bindAddress != "0.0.0.0")
        {
            uint addrVal;
            if (inet_pton(AF_INET, bindAddress, &addrVal) != 1)
            {
                closesocket(listener);
                return -1;
            }
            addr.sin_addr = addrVal;
        }

        if (bind(listener, &addr, sizeof(sockaddr_in)) == SOCKET_ERROR)
        {
            closesocket(listener);
            return -1;
        }

        if (listen(listener, backlog) == SOCKET_ERROR)
        {
            closesocket(listener);
            return -1;
        }

        // Track listener
        if (_ownedListenerCount >= _ownedListenerCapacity)
        {
            var newCapacity = _ownedListenerCapacity * 2;
            var newArray = (nint*)NativeMemory.Realloc(_ownedListeners, newCapacity * (nuint)sizeof(nint));
            _ownedListeners = newArray;
            _ownedListenerCapacity = newCapacity;
        }
        _ownedListeners[_ownedListenerCount++] = listener;

        // Cache AcceptEx function pointer
        if (_fnAcceptEx == null)
        {
            _fnAcceptEx = LoadAcceptEx(listener);
        }

        return listener;
    }

    /// <inheritdoc/>
    public void CloseListener(nint listener)
    {
        if (listener == INVALID_SOCKET)
        {
            return;
        }

        // Cancel pending AcceptEx operations for this listener
        for (uint i = 0; i < _acceptPoolSize; i++)
        {
            var ctx = &_acceptPool[i];
            if (ctx->Pending && ctx->ListenSocket == listener)
            {
                CancelIoEx(listener, &ctx->Overlapped);

                if (ctx->AcceptSocket != INVALID_SOCKET)
                {
                    closesocket(ctx->AcceptSocket);
                    ctx->AcceptSocket = INVALID_SOCKET;
                }

                if (ctx->ConnSlot >= 0 && (uint)ctx->ConnSlot < _rioMaxConnections)
                {
                    _connections[ctx->ConnSlot].Reserved = false;
                }

                ctx->Pending = false;
                _pendingAcceptCount--;
                ctx->ConnSlot = -1;
            }
        }

        // Remove from owned listeners
        for (uint i = 0; i < _ownedListenerCount; i++)
        {
            if (_ownedListeners[i] == listener)
            {
                for (var j = i; j < _ownedListenerCount - 1; j++)
                {
                    _ownedListeners[j] = _ownedListeners[j + 1];
                }
                _ownedListenerCount--;
                break;
            }
        }

        closesocket(listener);
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ConfigureSocket(nint socket)
    {
        if (socket == INVALID_SOCKET)
        {
            return;
        }

        // Non-blocking
        var mode = 1u;
        ioctlsocket(socket, FIONBIO, &mode);

        // TCP_NODELAY
        var opt = 1;
        setsockopt(socket, IPPROTO_TCP_LEVEL, TCP_NODELAY, &opt, sizeof(int));

        // Disable linger
        var lin = new LINGER { l_onoff = 0, l_linger = 0 };
        setsockopt(socket, SOL_SOCKET, SO_LINGER, &lin, sizeof(LINGER));
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void CloseSocket(nint socket)
    {
        if (socket != INVALID_SOCKET)
        {
            closesocket(socket);
        }
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Shutdown(nint socket, int how)
    {
        if (socket != INVALID_SOCKET)
        {
            shutdown(socket, how);
        }
    }

    // =========================================================================
    // AcceptEx Support
    // =========================================================================

    private bool PostAcceptEx(nint listenSocket, ulong userData)
    {
        // Load AcceptEx if not already done
        if (_fnAcceptEx == null)
        {
            _fnAcceptEx = LoadAcceptEx(listenSocket);
            if (_fnAcceptEx == null)
            {
                return false;
            }
        }

        // Find free slot in accept pool
        var slot = -1;
        for (uint i = 0; i < _acceptPoolSize; i++)
        {
            if (!_acceptPool[i].Pending)
            {
                slot = (int)i;
                break;
            }
        }

        if (slot < 0)
        {
            return false;
        }

        var ctx = &_acceptPool[slot];

        // Create accept socket with WSA_FLAG_OVERLAPPED | WSA_FLAG_REGISTERED_IO
        ctx->AcceptSocket = WSASocketW(AF_INET, SOCK_STREAM, IPPROTO_TCP, null, 0,
            WSA_FLAG_OVERLAPPED | WSA_FLAG_REGISTERED_IO);
        if (ctx->AcceptSocket == INVALID_SOCKET)
        {
            return false;
        }

        // Reserve connection slot (must check both Active AND Reserved)
        var connSlot = -1;
        for (uint i = 0; i < _rioMaxConnections; i++)
        {
            if (!_connections[i].Active && !_connections[i].Reserved)
            {
                connSlot = (int)i;
                break;
            }
        }

        if (connSlot < 0)
        {
            closesocket(ctx->AcceptSocket);
            ctx->AcceptSocket = INVALID_SOCKET;
            return false;
        }

        // Mark slot as reserved immediately to prevent collision
        _connections[connSlot].Reserved = true;

        ctx->ConnSlot = connSlot;
        ctx->ListenSocket = listenSocket;
        ctx->UserData = userData;
        ctx->Pending = true;
        _pendingAcceptCount++;

        // Zeroing sets Internal to 0, not STATUS_PENDING, so mark it pending explicitly. The scan
        // reads this field to detect completion, and a zero here would look like "finished".
        Unsafe.InitBlock(&ctx->Overlapped, 0, (uint)sizeof(OVERLAPPED));
        ctx->Overlapped.Internal = STATUS_PENDING;
        ctx->Overlapped.hEvent = _acceptEvent;

        // Post AcceptEx
        uint bytesReceived = 0;
        var result = _fnAcceptEx(
            listenSocket,
            ctx->AcceptSocket,
            ctx->Buffer,
            0, // Don't receive data with accept
            AcceptExAddrSize,
            AcceptExAddrSize,
            &bytesReceived,
            &ctx->Overlapped
        );

        if (result == 0) // FALSE = potentially pending
        {
            var err = WSAGetLastError();
            if (err != ERROR_IO_PENDING)
            {
                // Real error
                _connections[connSlot].Reserved = false;
                closesocket(ctx->AcceptSocket);
                ctx->AcceptSocket = INVALID_SOCKET;
                ctx->Pending = false;
                _pendingAcceptCount--;
                ctx->ConnSlot = -1;
                return false;
            }
        }

        return true;
    }

    private void CheckAcceptExCompletions()
    {
        if (_pendingAcceptCount == 0)
        {
            return;
        }

        // Reset before scanning, not after: a completion landing mid-scan re-signals the event, so
        // the next wait returns immediately and rescans. Resetting afterwards would swallow it.
        ResetEvent(_acceptEvent);

        uint found = 0;
        for (uint i = 0; i < _acceptPoolSize; i++)
        {
            var ctx = &_acceptPool[i];
            if (!ctx->Pending)
            {
                continue;
            }

            // The kernel writes the final status into Internal, so completion is a plain memory
            // read -- this is what the Win32 HasOverlappedIoCompleted macro expands to. It replaces
            // one WaitForSingleObject per pending slot, which measured 794ns on a desktop and
            // accounted for nearly all of an idle loop iteration's cost.
            if (ctx->Overlapped.Internal == STATUS_PENDING)
            {
                // Early exit: if we've checked all pending slots, stop scanning
                if (++found >= _pendingAcceptCount)
                {
                    return;
                }

                continue;
            }

            ctx->Pending = false;
            _pendingAcceptCount--;

            // Get result
            uint bytesTransferred = 0;
            var result = GetOverlappedResult(ctx->ListenSocket, &ctx->Overlapped, &bytesTransferred, 0);

            if (result != 0) // TRUE = success
            {
                // Update socket context so getpeername works
                var listenSocketCopy = ctx->ListenSocket;
                setsockopt(ctx->AcceptSocket, SOL_SOCKET, SO_UPDATE_ACCEPT_CONTEXT,
                    &listenSocketCopy, sizeof(nint));

                // Clear reservation (RQ will be created via RegisterSocket)
                var connSlot = ctx->ConnSlot;
                if (connSlot >= 0 && (uint)connSlot < _rioMaxConnections)
                {
                    _connections[connSlot].Reserved = false;
                }

                // Return socket handle as result
                CompleteOp(ctx->UserData, (int)ctx->AcceptSocket);

                // Reset context for reuse
                ctx->AcceptSocket = INVALID_SOCKET;
            }
            else
            {
                // AcceptEx failed
                var err = Marshal.GetLastPInvokeError();
                CompleteOp(ctx->UserData, -err);

                if (ctx->AcceptSocket != INVALID_SOCKET)
                {
                    closesocket(ctx->AcceptSocket);
                    ctx->AcceptSocket = INVALID_SOCKET;
                }

                var connSlot = ctx->ConnSlot;
                if (connSlot >= 0 && (uint)connSlot < _rioMaxConnections)
                {
                    _connections[connSlot].Reserved = false;
                }
            }

            ctx->ConnSlot = -1;
        }
    }

    // =========================================================================
    // Dispose
    // =========================================================================

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        // Cancel pending AcceptEx and cleanup pool
        if (_acceptPool != null)
        {
            for (uint i = 0; i < _acceptPoolSize; i++)
            {
                var ctx = &_acceptPool[i];
                if (ctx->Pending && ctx->ListenSocket != INVALID_SOCKET)
                {
                    CancelIoEx(ctx->ListenSocket, &ctx->Overlapped);
                }
                if (ctx->AcceptSocket != INVALID_SOCKET)
                {
                    closesocket(ctx->AcceptSocket);
                }
            }
            NativeMemory.Free(_acceptPool);
        }

        // Cleanup owned listeners
        if (_ownedListeners != null)
        {
            for (uint i = 0; i < _ownedListenerCount; i++)
            {
                if (_ownedListeners[i] != INVALID_SOCKET)
                {
                    closesocket(_ownedListeners[i]);
                }
            }
            NativeMemory.Free(_ownedListeners);
            _ownedListeners = null;
        }

        // Cleanup connections
        if (_connections != null)
        {
            for (uint i = 0; i < _rioMaxConnections; i++)
            {
                var conn = &_connections[i];
                if ((conn->Active || conn->Reserved) && conn->Socket != INVALID_SOCKET)
                {
                    closesocket(conn->Socket);
                }
            }
        }

        // Close RIO CQ
        if (_rioCq != RIO_INVALID_CQ)
        {
            _rio.RIOCloseCompletionQueue(_rioCq);
        }

        // Close completion notification event. _disposed was set at the top of Dispose, so a
        // concurrent Wake() observes it and returns before touching these handles.
        if (_completionEvent != 0)
        {
            CloseHandle(_completionEvent);
        }

        if (_wakeEvent != 0)
        {
            CloseHandle(_wakeEvent);
        }

        if (_idleTimer != 0)
        {
            CloseHandle(_idleTimer);
        }

        if (_acceptEvent != 0)
        {
            CloseHandle(_acceptEvent);
        }

        // Deregister external buffers
        for (uint i = 0; i < _maxExternalBuffers; i++)
        {
            if (_externalBufferIds[i] != RIO_INVALID_BUFFERID)
            {
                _rio.RIODeregisterBuffer(_externalBufferIds[i]);
            }
        }

        FreeAllMemory();
    }

    private void FreeAllMemory()
    {
        if (_sq != null) NativeMemory.Free(_sq);
        if (_cq != null) NativeMemory.Free(_cq);
        if (_connections != null) NativeMemory.Free(_connections);
        if (_pendingOps != null) NativeMemory.Free(_pendingOps);
        if (_externalBufferIds != null) NativeMemory.Free(_externalBufferIds);
        if (_externalBufferPtrs != null) NativeMemory.Free(_externalBufferPtrs);
        if (_externalBufferLens != null) NativeMemory.Free(_externalBufferLens);
    }
}
