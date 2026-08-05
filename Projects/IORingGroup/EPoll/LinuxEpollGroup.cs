// SPDX-License-Identifier: BSD-3-Clause
// Copyright (c) 2025, ModernUO

using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Network.EPoll;

/// <summary>
/// Linux epoll implementation of IIORingGroup.
/// </summary>
/// <remarks>
/// Unlike io_uring which is completion-based, epoll is readiness-based.
/// This implementation bridges the gap:
/// - Prepare* methods store pending operations in fixed arrays by connection ID
/// - Submit() arms epoll via epoll_ctl for those pending operations
/// - PeekCompletions() polls epoll_wait, executes actual I/O (recv/send/accept), produces completions
/// - SubmitAndWait(waitNr) calls Submit() then blocks on epoll_wait until enough completions
/// </remarks>
public sealed unsafe partial class LinuxEpollGroup : IIORingGroup
{
    private readonly int _epollFd;
    private readonly int _maxConnections;

    // Connection ID management (dense slot allocation)
    private readonly int[] _connIdToFd;       // connId -> FD, initialized to -1
    private readonly int[] _freeSlots;        // free stack
    private int _freeSlotCount;

    // Pending operations (fixed arrays indexed by connection ID)
    private readonly PendingOp[] _pendingRecvs;
    private readonly PendingOp[] _pendingSends;
    private readonly bool[] _hasRecv;
    private readonly bool[] _hasSend;

    // Recv/send interest bits (EPOLLIN | EPOLLOUT) currently armed in epoll for
    // each connection, or 0 if disarmed. Recv and send share a single fd, so a
    // single EPOLL_CTL_MOD sets the combined mask. EPOLLONESHOT disarms the whole
    // fd on any event, so this is reset to 0 the moment an event is delivered and
    // Submit() re-arms whatever direction is still pending.
    private readonly uint[] _armedIo;

    // Accept operations (dictionary keyed by listener FD — typically 1-2 listeners)
    private readonly Dictionary<int, PendingOp> _pendingAccepts = new();
    private readonly HashSet<int> _acceptsSubmitted = new();

    // User-space completion queue (ring buffer)
    private readonly Completion[] _cqEntries;  // size = queueSize * 2
    private int _cqHead;
    private int _cqTail;
    private readonly int _cqMask;             // queueSize * 2 - 1

    // epoll_wait event buffer (raw bytes for arch-specific struct)
    private readonly byte[] _eventBuffer;
    private readonly int _epollEventSize;     // 12 on x64, 16 on ARM64
    private readonly int _maxEvents;

    // External buffer tracking
    private readonly int _maxExternalBuffers;
    private readonly nint[] _externalBufferPtrs;
    private readonly int[] _externalBufferLengths;
    private int _externalBufferCount;

    // Wake eventfd, registered level-triggered in the epoll set. Its counter latches, so a Wake()
    // landing before the caller blocks makes epoll_wait return immediately instead of losing it.
    private readonly int _wakeFd = -1;

    // Sentinel epoll data for the wake fd. The high bit is clear so IsAcceptData never matches it,
    // and it is checked before the connId path, so it cannot collide with a real connection.
    private const long WakeEventData = long.MaxValue;

    private volatile bool _disposed;

    private struct PendingOp
    {
        public byte Opcode;
        public int Fd;
        public nint Addr;
        public nint Addr2;
        public int Len;
        public int Flags;
        public ulong UserData;
    }

    public LinuxEpollGroup(int queueSize = IORingGroup.DefaultQueueSize, int maxConnections = IORingGroup.DefaultMaxConnections)
    {
        if (!IORingGroup.IsPowerOfTwo(queueSize))
        {
            throw new ArgumentException("Queue size must be a power of 2", nameof(queueSize));
        }

        _maxConnections = maxConnections;
        _maxEvents = queueSize;

        // Determine epoll_event struct size based on architecture
        _epollEventSize = RuntimeInformation.ProcessArchitecture == Architecture.Arm64 ? 16 : 12;

        // Allocate completion queue (ring buffer)
        _cqMask = queueSize * 2 - 1;
        _cqEntries = new Completion[queueSize * 2];

        // Allocate epoll_wait event buffer
        _eventBuffer = new byte[_maxEvents * _epollEventSize];

        // Allocate connection ID management arrays
        _connIdToFd = new int[maxConnections];
        _freeSlots = new int[maxConnections];
        _pendingRecvs = new PendingOp[maxConnections];
        _pendingSends = new PendingOp[maxConnections];
        _hasRecv = new bool[maxConnections];
        _hasSend = new bool[maxConnections];
        _armedIo = new uint[maxConnections];

        // Initialize free stack (all slots available, lowest first for pop order)
        for (var i = 0; i < maxConnections; i++)
        {
            _connIdToFd[i] = -1;
            _freeSlots[i] = maxConnections - 1 - i; // stack: top = 0, bottom = maxConnections-1
        }
        _freeSlotCount = maxConnections;

        // Initialize external buffer tracking (maxConnections * 2 for recv + send buffer per connection)
        _maxExternalBuffers = maxConnections * 2;
        _externalBufferPtrs = new nint[_maxExternalBuffers];
        _externalBufferLengths = new int[_maxExternalBuffers];

        // Create epoll instance
        _epollFd = Syscalls.epoll_create1(EPOLL_CLOEXEC);
        if (_epollFd < 0)
        {
            var errno = Marshal.GetLastPInvokeError();
            throw new InvalidOperationException($"epoll_create1() failed: errno {errno}");
        }

        // Register the wake eventfd. Level-triggered on purpose: the counter stays readable until
        // drained, so a wake cannot be missed between epoll_wait calls. A failure here is not
        // fatal — Wake() becomes a no-op and callers fall back to their own timeout.
        _wakeFd = Syscalls.eventfd(0, EFD_NONBLOCK);
        if (_wakeFd >= 0)
        {
            var evBuf = stackalloc byte[16];

            // Zero explicitly — see EpollCtlMod for why the padding cannot be left to chance.
            *(ulong*)evBuf = 0;
            *(ulong*)(evBuf + 8) = 0;

            *(uint*)evBuf = (uint)epoll_events.EPOLLIN;
            if (_epollEventSize == 12)
            {
                *(long*)(evBuf + 4) = WakeEventData;
            }
            else
            {
                *(long*)(evBuf + 8) = WakeEventData;
            }

            Syscalls.epoll_ctl(_epollFd, (int)epoll_op.EPOLL_CTL_ADD, _wakeFd, (nint)evBuf);
        }
    }

    // =============================================================================
    // Properties
    // =============================================================================

    /// <inheritdoc/>
    public int SubmissionQueueSpace
    {
        get
        {
            var pendingCount = 0;
            for (var i = 0; i < _maxConnections; i++)
            {
                uint want = 0;
                if (_hasRecv[i])
                {
                    want |= (uint)epoll_events.EPOLLIN;
                }

                if (_hasSend[i])
                {
                    want |= (uint)epoll_events.EPOLLOUT;
                }

                if (want != 0 && want != _armedIo[i])
                {
                    pendingCount++;
                }
            }

            foreach (var kvp in _pendingAccepts)
            {
                if (!_acceptsSubmitted.Contains(kvp.Key))
                {
                    pendingCount++;
                }
            }

            return _maxEvents - pendingCount;
        }
    }

    /// <inheritdoc/>
    public int CompletionQueueCount
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _cqTail - _cqHead;
    }

    // =============================================================================
    // Prepare Methods
    // =============================================================================

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PreparePollAdd(nint fd, PollMask mask, ulong userData)
    {
        // Store as a pending accept entry (poll operations use the accept dictionary)
        _pendingAccepts[(int)fd] = new PendingOp
        {
            Opcode = (byte)IORingOp.PollAdd,
            Fd = (int)fd,
            Flags = (int)mask,
            UserData = userData
        };
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PreparePollRemove(ulong userData)
    {
        // Scan and remove from _pendingAccepts
        int? toRemove = null;
        foreach (var kvp in _pendingAccepts)
        {
            if (kvp.Value.UserData == userData)
            {
                toRemove = kvp.Key;
                break;
            }
        }

        if (toRemove.HasValue)
        {
            _acceptsSubmitted.Remove(toRemove.Value);
            _pendingAccepts.Remove(toRemove.Value);
        }
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PrepareAccept(nint listenFd, nint addr, nint addrLen, ulong userData)
    {
        _pendingAccepts[(int)listenFd] = new PendingOp
        {
            Opcode = (byte)IORingOp.Accept,
            Fd = (int)listenFd,
            Addr = addr,
            Addr2 = addrLen,
            UserData = userData
        };
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PrepareConnect(nint fd, nint addr, int addrLen, ulong userData)
    {
        // Attempt synchronous connect on a non-blocking socket
        var result = Syscalls.connect((int)fd, addr, addrLen);
        if (result == 0)
        {
            // Immediate success (rare for TCP)
            AddCompletion(userData, 0);
        }
        else
        {
            var errno = Marshal.GetLastPInvokeError();
            if (errno == EINPROGRESS)
            {
                // Connection in progress — store as pending send with Connect opcode
                var connId = FindConnIdByFd((int)fd);
                if (connId >= 0)
                {
                    _pendingSends[connId] = new PendingOp
                    {
                        Opcode = (byte)IORingOp.Connect,
                        Fd = (int)fd,
                        UserData = userData
                    };
                    _hasSend[connId] = true;
                }
                else
                {
                    AddCompletion(userData, -EINPROGRESS);
                }
            }
            else
            {
                AddCompletion(userData, -errno);
            }
        }
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PrepareClose(nint fd, ulong userData)
    {
        // Close is synchronous — execute immediately
        var result = Syscalls.close((int)fd);
        AddCompletion(userData, result == 0 ? 0 : -Marshal.GetLastPInvokeError());
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PrepareCancel(ulong targetUserData, ulong userData)
    {
        // Scan arrays, remove matching, add completion
        for (var i = 0; i < _maxConnections; i++)
        {
            if (_hasRecv[i] && _pendingRecvs[i].UserData == targetUserData)
            {
                _hasRecv[i] = false;
            }

            if (_hasSend[i] && _pendingSends[i].UserData == targetUserData)
            {
                _hasSend[i] = false;
            }
        }

        int? toRemove = null;
        foreach (var kvp in _pendingAccepts)
        {
            if (kvp.Value.UserData == targetUserData)
            {
                toRemove = kvp.Key;
                break;
            }
        }

        if (toRemove.HasValue)
        {
            _acceptsSubmitted.Remove(toRemove.Value);
            _pendingAccepts.Remove(toRemove.Value);
        }

        AddCompletion(userData, 0);
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PrepareShutdown(nint fd, int how, ulong userData)
    {
        // Shutdown is synchronous
        var result = Syscalls.shutdown((int)fd, how);
        AddCompletion(userData, result == 0 ? 0 : -Marshal.GetLastPInvokeError());
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    // One pending send per connection: this overwrites the slot unconditionally, so a second send
    // posted before the first completes discards it and wedges the socket. Upheld by
    // MaxOutstandingSendsPerSocket == 1; raising it requires a real queue here.
    private void PrepareSend(int connId, nint buf, int len, MsgFlags flags, ulong userData)
    {
        _pendingSends[connId] = new PendingOp
        {
            Opcode = (byte)IORingOp.Send,
            Fd = _connIdToFd[connId],
            Addr = buf,
            Len = len,
            Flags = (int)flags,
            UserData = userData
        };
        _hasSend[connId] = true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void PrepareRecv(int connId, nint buf, int len, MsgFlags flags, ulong userData)
    {
        _pendingRecvs[connId] = new PendingOp
        {
            Opcode = (byte)IORingOp.Recv,
            Fd = _connIdToFd[connId],
            Addr = buf,
            Len = len,
            Flags = (int)flags,
            UserData = userData
        };
        _hasRecv[connId] = true;
    }

    // =============================================================================
    // Submit / Wait / Peek
    // =============================================================================

    /// <inheritdoc/>
    public int Submit()
    {
        var submitted = 0;

        // Arm each connection for the full set of directions it currently wants.
        // Recv and send share one fd, so we always MOD the combined mask — arming
        // one direction must never drop interest in the other. EPOLLONESHOT means
        // the kernel disarms the fd after a single event, so _armedIo is reset to 0
        // on delivery and the still-pending direction is re-armed here next time.
        for (var i = 0; i < _maxConnections; i++)
        {
            var fd = _connIdToFd[i];
            if (fd < 0)
            {
                continue;
            }

            uint want = 0;
            if (_hasRecv[i])
            {
                want |= (uint)epoll_events.EPOLLIN;
            }

            if (_hasSend[i])
            {
                want |= (uint)epoll_events.EPOLLOUT;
            }

            // Nothing pending, or already armed for exactly these directions.
            if (want == 0 || want == _armedIo[i])
            {
                continue;
            }

            var events = want | (uint)(epoll_events.EPOLLET | epoll_events.EPOLLONESHOT | epoll_events.EPOLLRDHUP);

            // Arm epoll with connId as data
            if (EpollCtlMod(fd, events, (long)i))
            {
                _armedIo[i] = want;
                submitted++;
            }
        }

        // Arm pending accepts
        foreach (var kvp in _pendingAccepts)
        {
            var listenFd = kvp.Key;
            if (_acceptsSubmitted.Contains(listenFd))
            {
                continue;
            }

            var events = (uint)(epoll_events.EPOLLIN | epoll_events.EPOLLET | epoll_events.EPOLLONESHOT);
            var data = EncodeAcceptData(listenFd);

            if (EpollCtlMod(listenFd, events, data))
            {
                _acceptsSubmitted.Add(listenFd);
                submitted++;
            }
        }

        return submitted;
    }

    /// <inheritdoc/>
    public int SubmitAndWait(int waitNr)
    {
        var submitted = Submit();

        while (CompletionQueueCount < waitNr)
        {
            PollAndExecute(timeoutMs: -1);
        }

        return submitted;
    }

    /// <inheritdoc/>
    public void WaitForCompletion(int timeoutMs)
    {
        // Already have completions ready to hand out — don't block.
        if (CompletionQueueCount > 0)
        {
            return;
        }

        // Block up to timeoutMs for readiness, executing any ready I/O into completions.
        // Lets the caller sleep instead of busy-polling PeekCompletions.
        PollAndExecute(timeoutMs);
    }

    /// <inheritdoc/>
    public void Wake()
    {
        if (_disposed || _wakeFd < 0)
        {
            return;
        }

        // The eventfd counter latches, so this cannot be lost if it lands between the caller's
        // idle check and its epoll_wait. EAGAIN means the counter is already saturated, which
        // still counts as signalled.
        ulong one = 1;
        Syscalls.write(_wakeFd, (nint)(&one), 8);
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int PeekCompletions(Span<Completion> completions)
    {
        // Poll epoll for ready events and execute I/O
        PollAndExecute(timeoutMs: 0);

        // Copy completions to output
        var available = CompletionQueueCount;
        var count = Math.Min(available, completions.Length);

        for (var i = 0; i < count; i++)
        {
            var index = (_cqHead + i) & _cqMask;
            completions[i] = _cqEntries[index];
        }

        return count;
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AdvanceCompletionQueue(int count)
    {
        _cqHead += count;
    }

    // =============================================================================
    // Internal Event Processing
    // =============================================================================

    /// <summary>
    /// Polls epoll for ready events and executes the corresponding I/O operations.
    /// </summary>
    /// <param name="timeoutMs">epoll_wait timeout: -1 blocks indefinitely, 0 is non-blocking, &gt;0 waits up to that many milliseconds.</param>
    private void PollAndExecute(int timeoutMs)
    {
        int eventCount;
        fixed (byte* evPtr = _eventBuffer)
        {
            eventCount = Syscalls.epoll_wait(_epollFd, (nint)evPtr, _maxEvents, timeoutMs);
        }

        if (eventCount < 0)
        {
            var errno = Marshal.GetLastPInvokeError();
            if (errno == EINTR)
            {
                // Interrupted by a signal — the caller re-polls. Returning here
                // (rather than looping internally) keeps SubmitAndWait responsive.
                return;
            }

            // A hard error (e.g. EBADF) would otherwise spin SubmitAndWait's wait
            // loop forever. Surface it instead of silently swallowing it.
            throw new InvalidOperationException($"epoll_wait() failed: errno {errno}");
        }

        if (eventCount == 0)
        {
            return;
        }

        fixed (byte* evPtr = _eventBuffer)
        {
            for (var i = 0; i < eventCount; i++)
            {
                var ptr = evPtr + i * _epollEventSize;

                // events is always at offset 0 (uint)
                var events = *(uint*)ptr;

                // data (long) is at offset 4 on x64 (packed), offset 8 on ARM64 (aligned)
                long data;
                if (_epollEventSize == 12)
                {
                    data = *(long*)(ptr + 4);
                }
                else
                {
                    data = *(long*)(ptr + 8);
                }

                if (data == WakeEventData)
                {
                    // Drain the counter so the level-triggered fd stops reporting ready.
                    ulong drain;
                    Syscalls.read(_wakeFd, (nint)(&drain), 8);
                    continue;
                }

                if (IsAcceptData(data))
                {
                    // Accept event
                    var listenFd = DecodeAcceptData(data);
                    _acceptsSubmitted.Remove(listenFd);

                    if (_pendingAccepts.TryGetValue(listenFd, out var acceptOp))
                    {
                        _pendingAccepts.Remove(listenFd);
                        ExecuteAccept(ref acceptOp);
                    }
                }
                else
                {
                    // Connection event — data is connId
                    var connId = (int)data;
                    if (connId < 0 || connId >= _maxConnections)
                    {
                        continue;
                    }

                    // EPOLLONESHOT disarmed the whole fd on this delivery, so clear
                    // the armed mask for both directions. Whatever remains pending
                    // (after the handlers below run) is re-armed by the next Submit().
                    _armedIo[connId] = 0;

                    // Handle error conditions
                    if ((events & ((uint)epoll_events.EPOLLERR | (uint)epoll_events.EPOLLHUP)) != 0)
                    {
                        var errorResult = (events & (uint)epoll_events.EPOLLERR) != 0 ? -32 : 0; // -EPIPE = -32

                        if (_hasRecv[connId])
                        {
                            var recvUserData = _pendingRecvs[connId].UserData;
                            _hasRecv[connId] = false;
                            AddCompletion(recvUserData, errorResult);
                        }

                        if (_hasSend[connId])
                        {
                            var sendUserData = _pendingSends[connId].UserData;
                            _hasSend[connId] = false;
                            AddCompletion(sendUserData, errorResult);
                        }

                        continue;
                    }

                    // Handle EPOLLIN (recv ready)
                    if ((events & (uint)epoll_events.EPOLLIN) != 0 && _hasRecv[connId])
                    {
                        ExecuteRecv(connId);
                    }

                    // Handle EPOLLOUT (send ready or connect complete)
                    if ((events & (uint)epoll_events.EPOLLOUT) != 0 && _hasSend[connId])
                    {
                        if (_pendingSends[connId].Opcode == (byte)IORingOp.Connect)
                        {
                            ExecuteConnectComplete(connId);
                        }
                        else
                        {
                            ExecuteSend(connId);
                        }
                    }

                    // Handle EPOLLRDHUP (remote hangup) — complete recv with 0 if still pending
                    // Use else-if to avoid duplicate completions when EPOLLIN and EPOLLRDHUP fire together
                    else if ((events & (uint)epoll_events.EPOLLRDHUP) != 0 && _hasRecv[connId])
                    {
                        var recvUserData = _pendingRecvs[connId].UserData;
                        _hasRecv[connId] = false;
                        AddCompletion(recvUserData, 0);
                    }
                }
            }
        }
    }

    private void ExecuteAccept(ref PendingOp op)
    {
        var addrLen = op.Addr2 != 0 ? (int*)op.Addr2 : null;
        var result = Syscalls.accept(op.Fd, op.Addr, addrLen);

        if (result < 0)
        {
            var errno = Marshal.GetLastPInvokeError();
            if (errno == EAGAIN || errno == EWOULDBLOCK)
            {
                // No connection ready yet — re-queue the accept
                _pendingAccepts[op.Fd] = op;
                return;
            }

            AddCompletion(op.UserData, -errno);
        }
        else
        {
            AddCompletion(op.UserData, result);
        }
    }

    private void ExecuteRecv(int connId)
    {
        ref var op = ref _pendingRecvs[connId];
        var result = Syscalls.recv(op.Fd, op.Addr, (nuint)op.Len, op.Flags);

        if (result < 0)
        {
            var errno = Marshal.GetLastPInvokeError();
            if (errno == EAGAIN || errno == EWOULDBLOCK)
            {
                // No data ready yet — keep pending (don't clear _hasRecv), return.
                // _armedIo was cleared in PollAndExecute, so Submit() re-arms EPOLLIN.
                return;
            }

            _hasRecv[connId] = false;
            AddCompletion(op.UserData, -errno);
        }
        else
        {
            _hasRecv[connId] = false;
            AddCompletion(op.UserData, (int)result);
        }
    }

    private void ExecuteSend(int connId)
    {
        ref var op = ref _pendingSends[connId];
        var result = Syscalls.send(op.Fd, op.Addr, (nuint)op.Len, op.Flags);

        if (result < 0)
        {
            var errno = Marshal.GetLastPInvokeError();
            if (errno == EAGAIN || errno == EWOULDBLOCK)
            {
                // Buffer full — keep pending, return.
                // _armedIo was cleared in PollAndExecute, so Submit() re-arms EPOLLOUT.
                return;
            }

            _hasSend[connId] = false;
            AddCompletion(op.UserData, -errno);
        }
        else
        {
            _hasSend[connId] = false;
            AddCompletion(op.UserData, (int)result);
        }
    }

    private void ExecuteConnectComplete(int connId)
    {
        ref var op = ref _pendingSends[connId];
        int error = 0;
        var len = sizeof(int);
        Syscalls.getsockopt(op.Fd, SOL_SOCKET, SO_ERROR, (nint)(&error), ref len);

        _hasSend[connId] = false;
        AddCompletion(op.UserData, error == 0 ? 0 : -error);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void AddCompletion(ulong userData, int result)
    {
        var cqIndex = _cqTail & _cqMask;
        _cqEntries[cqIndex] = new Completion(userData, result);
        _cqTail++;
    }

    // =============================================================================
    // Epoll Helpers
    // =============================================================================

    /// <summary>
    /// Arms epoll for the given fd. Tries EPOLL_CTL_MOD first, falls back to EPOLL_CTL_ADD on ENOENT.
    /// </summary>
    private bool EpollCtlMod(int fd, uint events, long data)
    {
        // Build epoll_event in a stack buffer
        var evBuf = stackalloc byte[16]; // max size (ARM64)

        // Zero explicitly. The module is compiled with [module: SkipLocalsInit], so the
        // stackalloc arrives with garbage, and the ARM64 layout below writes only bytes
        // 0-3 and 8-15 — leaving the alignment padding at 4-7 to leak to the kernel.
        *(ulong*)evBuf = 0;
        *(ulong*)(evBuf + 8) = 0;

        // events at offset 0
        *(uint*)evBuf = events;

        // data at offset 4 (packed/x64) or offset 8 (aligned/ARM64)
        if (_epollEventSize == 12)
        {
            *(long*)(evBuf + 4) = data;
        }
        else
        {
            *(long*)(evBuf + 8) = data;
        }

        var result = Syscalls.epoll_ctl(_epollFd, (int)epoll_op.EPOLL_CTL_MOD, fd, (nint)evBuf);
        if (result < 0)
        {
            var errno = Marshal.GetLastPInvokeError();
            if (errno == 2) // ENOENT
            {
                result = Syscalls.epoll_ctl(_epollFd, (int)epoll_op.EPOLL_CTL_ADD, fd, (nint)evBuf);
                if (result < 0)
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        return true;
    }

    // =============================================================================
    // Accept Data Encoding
    // =============================================================================

    /// <summary>
    /// Encodes a listener FD into the data field with bit 63 set to distinguish accept events.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static long EncodeAcceptData(int fd)
    {
        ulong ufd = (uint)fd;
        return (long)(ufd | (1UL << 63));
    }

    /// <summary>
    /// Returns true if the data field represents an accept event (bit 63 set).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsAcceptData(long data) => (data & unchecked((long)(1UL << 63))) != 0;

    /// <summary>
    /// Decodes the listener FD from an accept data field (masks off bit 63).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int DecodeAcceptData(long data) => (int)(data & 0x7FFFFFFF);

    /// <summary>
    /// Linear scan to find a connId by FD. Only used for PrepareConnect which is rare.
    /// </summary>
    private int FindConnIdByFd(int fd)
    {
        for (var i = 0; i < _maxConnections; i++)
        {
            if (_connIdToFd[i] == fd)
            {
                return i;
            }
        }

        return -1;
    }

    // =============================================================================
    // Listener and Socket Management
    // =============================================================================

    /// <inheritdoc/>
    public nint CreateListener(string bindAddress, ushort port, int backlog)
    {
        // Create non-blocking TCP socket
        var fd = Syscalls.socket(AF_INET, SOCK_STREAM | SOCK_NONBLOCK, IPPROTO_TCP);
        if (fd < 0)
        {
            return -1;
        }

        // Enable SO_REUSEADDR for quick restart (bind over TIME_WAIT sockets).
        // Matches the io_uring and Darwin listeners; without it a fast restart
        // fails to bind while the previous port is still in TIME_WAIT.
        var optval = 1;
        Syscalls.setsockopt(fd, SOL_SOCKET, SO_REUSEADDR, (nint)(&optval), sizeof(int));

        // Enable TCP_NODELAY (disable Nagle)
        optval = 1;
        Syscalls.setsockopt(fd, IPPROTO_TCP, TCP_NODELAY, (nint)(&optval), sizeof(int));

        // Disable SO_LINGER
        var linger = new LingerOption { OnOff = 0, Seconds = 0 };
        Syscalls.setsockopt(fd, SOL_SOCKET, SO_LINGER, (nint)(&linger), sizeof(LingerOption));

        // Parse and bind address
        var addr = new sockaddr_in
        {
            sin_family = AF_INET,
            sin_port = BinaryPrimitives.ReverseEndianness(port),
            sin_addr = ParseIPv4(bindAddress)
        };

        if (Syscalls.bind(fd, (nint)(&addr), sizeof(sockaddr_in)) < 0)
        {
            Syscalls.close(fd);
            return -1;
        }

        if (Syscalls.listen(fd, backlog) < 0)
        {
            Syscalls.close(fd);
            return -1;
        }

        return fd;
    }

    /// <inheritdoc/>
    public void CloseListener(nint listener)
    {
        if (listener >= 0)
        {
            var fd = (int)listener;
            _pendingAccepts.Remove(fd);
            _acceptsSubmitted.Remove(fd);
            Syscalls.close(fd);
        }
    }

    /// <inheritdoc/>
    public void ConfigureSocket(nint socket)
    {
        var fd = (int)socket;

        // Set non-blocking
        var flags = Syscalls.fcntl(fd, F_GETFL, 0);
        if (flags >= 0)
        {
            Syscalls.fcntl(fd, F_SETFL, flags | O_NONBLOCK);
        }

        // TCP_NODELAY (disable Nagle)
        var optval = 1;
        Syscalls.setsockopt(fd, IPPROTO_TCP, TCP_NODELAY, (nint)(&optval), sizeof(int));

        // Disable SO_LINGER
        var linger = new LingerOption { OnOff = 0, Seconds = 0 };
        Syscalls.setsockopt(fd, SOL_SOCKET, SO_LINGER, (nint)(&linger), sizeof(LingerOption));
    }

    /// <inheritdoc/>
    public int RegisterSocket(nint socket)
    {
        if (_freeSlotCount <= 0)
        {
            return -1;
        }

        var fd = (int)socket;

        // Pop from free stack
        var connId = _freeSlots[--_freeSlotCount];
        _connIdToFd[connId] = fd;
        _armedIo[connId] = 0;

        // Add to epoll with EPOLLET|EPOLLONESHOT but no initial event mask
        // This registers the fd; actual interest will be set by Submit()
        var evBuf = stackalloc byte[16];

        // Zero explicitly — see EpollCtlMod for why the padding cannot be left to chance.
        *(ulong*)evBuf = 0;
        *(ulong*)(evBuf + 8) = 0;

        *(uint*)evBuf = (uint)(epoll_events.EPOLLET | epoll_events.EPOLLONESHOT);
        if (_epollEventSize == 12)
        {
            *(long*)(evBuf + 4) = connId;
        }
        else
        {
            *(long*)(evBuf + 8) = connId;
        }

        Syscalls.epoll_ctl(_epollFd, (int)epoll_op.EPOLL_CTL_ADD, fd, (nint)evBuf);

        return connId;
    }

    /// <inheritdoc/>
    public void UnregisterSocket(int connId)
    {
        if (connId < 0 || connId >= _maxConnections)
        {
            return;
        }

        var fd = _connIdToFd[connId];
        if (fd < 0)
        {
            return;
        }

        // Remove from epoll
        Syscalls.epoll_ctl(_epollFd, (int)epoll_op.EPOLL_CTL_DEL, fd, 0);

        // Clear pending ops
        _hasRecv[connId] = false;
        _hasSend[connId] = false;
        _armedIo[connId] = 0;

        // Return slot to free stack
        _connIdToFd[connId] = -1;
        _freeSlots[_freeSlotCount++] = connId;
    }

    /// <inheritdoc/>
    public void CloseSocket(nint socket)
    {
        if (socket >= 0)
        {
            Syscalls.close((int)socket);
        }
    }

    /// <inheritdoc/>
    public void Shutdown(nint socket, int how)
    {
        if (socket >= 0)
        {
            Syscalls.shutdown((int)socket, how);
        }
    }

    // =============================================================================
    // Registered Buffer Operations
    // =============================================================================

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int RegisterBuffer(IORingBuffer buffer)
    {
        ArgumentNullException.ThrowIfNull(buffer);

        if (_externalBufferCount >= _maxExternalBuffers)
        {
            throw new InvalidOperationException($"Maximum of {_maxExternalBuffers} external buffers reached");
        }

        // Find first free slot
        for (var i = 0; i < _maxExternalBuffers; i++)
        {
            if (_externalBufferPtrs[i] == 0)
            {
                _externalBufferPtrs[i] = buffer.Pointer;
                _externalBufferLengths[i] = buffer.VirtualSize;
                _externalBufferCount++;
                return i;
            }
        }

        throw new InvalidOperationException("No free buffer slots available");
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void UnregisterBuffer(int bufferId)
    {
        if (bufferId < 0 || bufferId >= _maxExternalBuffers)
        {
            return;
        }

        if (_externalBufferPtrs[bufferId] != 0)
        {
            _externalBufferPtrs[bufferId] = 0;
            _externalBufferLengths[bufferId] = 0;
            _externalBufferCount--;
        }
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PrepareSendBuffer(int connId, int bufferId, int offset, int length, ulong userData)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(bufferId);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(bufferId, _maxExternalBuffers);

        var bufPtr = _externalBufferPtrs[bufferId];
        if (bufPtr == 0)
        {
            throw new InvalidOperationException($"Buffer {bufferId} is not registered");
        }

        if (offset + length > _externalBufferLengths[bufferId])
        {
            throw new ArgumentOutOfRangeException(nameof(offset), "Offset + length exceeds buffer size");
        }

        PrepareSend(connId, bufPtr + offset, length, MsgFlags.None, userData);
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PrepareRecvBuffer(int connId, int bufferId, int offset, int length, ulong userData)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(bufferId);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(bufferId, _maxExternalBuffers);

        var bufPtr = _externalBufferPtrs[bufferId];
        if (bufPtr == 0)
        {
            throw new InvalidOperationException($"Buffer {bufferId} is not registered");
        }

        if (offset + length > _externalBufferLengths[bufferId])
        {
            throw new ArgumentOutOfRangeException(nameof(offset), "Offset + length exceeds buffer size");
        }

        PrepareRecv(connId, bufPtr + offset, length, MsgFlags.None, userData);
    }

    /// <summary>
    /// Gets the number of registered external buffers.
    /// </summary>
    public int ExternalBufferCount => _externalBufferCount;

    // =============================================================================
    // Helpers
    // =============================================================================

    private static uint ParseIPv4(string address)
    {
        if (address == "0.0.0.0")
        {
            return 0; // INADDR_ANY
        }

        var parts = address.Split('.');
        if (parts.Length != 4)
        {
            return 0;
        }

        return (uint)(
            byte.Parse(parts[0]) |
            (byte.Parse(parts[1]) << 8) |
            (byte.Parse(parts[2]) << 16) |
            (byte.Parse(parts[3]) << 24)
        );
    }

    // =============================================================================
    // Dispose
    // =============================================================================

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        // Clear pending ops
        for (var i = 0; i < _maxConnections; i++)
        {
            _hasRecv[i] = false;
            _hasSend[i] = false;
            _armedIo[i] = 0;
        }

        _pendingAccepts.Clear();
        _acceptsSubmitted.Clear();

        // _disposed was set at the top of Dispose, so a concurrent Wake() observes it and returns
        // before touching this fd.
        if (_wakeFd >= 0)
        {
            Syscalls.close(_wakeFd);
        }

        if (_epollFd >= 0)
        {
            Syscalls.close(_epollFd);
        }
    }
}
