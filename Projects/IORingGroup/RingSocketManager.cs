// SPDX-License-Identifier: BSD-3-Clause
// Copyright (c) 2025, ModernUO

using System.Runtime.CompilerServices;

namespace System.Network;

/// <summary>
/// Event types returned by <see cref="RingSocketManager.ProcessCompletions"/>.
/// </summary>
public enum RingSocketEventType : byte
{
    /// <summary>No event (placeholder).</summary>
    None = 0,

    /// <summary>Data was received into the socket's RecvBuffer.</summary>
    DataReceived = 1,

    /// <summary>Data was sent from the socket's SendBuffer.</summary>
    DataSent = 2,

    /// <summary>Socket was disconnected (graceful close or error).</summary>
    Disconnected = 3,

    /// <summary>A new connection was accepted. Application should call CreateSocket.</summary>
    Accept = 4
}

/// <summary>
/// An event from socket I/O operations.
/// </summary>
public readonly struct RingSocketEvent
{
    /// <summary>The type of event.</summary>
    public RingSocketEventType Type { get; private init; }

    /// <summary>The socket this event relates to (null for Accept events).</summary>
    public RingSocket Socket { get; private init; }

    /// <summary>Number of bytes transferred (for DataReceived/DataSent).</summary>
    public int BytesTransferred { get; private init; }

    /// <summary>Error code if disconnect was due to error (0 for graceful close).</summary>
    public int Error { get; private init; }

    /// <summary>The accepted socket handle (for Accept events).</summary>
    public nint AcceptedSocketHandle { get; private init; }

    /// <summary>
    /// Creates a data received event.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static RingSocketEvent Received(RingSocket socket, int bytes) => new()
    {
        Type = RingSocketEventType.DataReceived,
        Socket = socket,
        BytesTransferred = bytes
    };

    /// <summary>
    /// Creates a data sent event.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static RingSocketEvent Sent(RingSocket socket, int bytes) => new()
    {
        Type = RingSocketEventType.DataSent,
        Socket = socket,
        BytesTransferred = bytes
    };

    /// <summary>
    /// Creates a disconnected event.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static RingSocketEvent Disconnected(RingSocket socket, int error = 0) => new()
    {
        Type = RingSocketEventType.Disconnected,
        Socket = socket,
        Error = error
    };

    /// <summary>
    /// Creates an accept event.
    /// </summary>
    /// <param name="socketHandle">The accepted socket handle, or negative for error.</param>
    public static RingSocketEvent Accepted(nint socketHandle) => new()
    {
        Type = RingSocketEventType.Accept,
        AcceptedSocketHandle = socketHandle
    };
}

/// <summary>
/// Manages RingSocket instances with automatic buffer lifecycle and graceful disconnect.
/// </summary>
/// <remarks>
/// <para><b>Threading:</b> This class is <b>not thread-safe</b>. All method calls —
/// <see cref="ProcessCompletions"/>, <see cref="Submit"/>, <see cref="CreateSocket"/>,
/// <see cref="DisconnectImmediate"/>, and <see cref="ProcessSendQueue"/> — must be made
/// from a single thread (the ring's processing thread). The internal send and disconnect
/// queues are plain <see cref="System.Collections.Generic.Queue{T}"/> with no synchronization.
/// This is intentional: single-threaded access eliminates lock contention and enables
/// zero-allocation hot paths.</para>
/// <para>
/// RingSocketManager handles all the complexity of zero-copy I/O:
/// <list type="bullet">
/// <item>Socket ID allocation with O(1) amortized slot finding</item>
/// <item>Generation tracking to detect stale completions</item>
/// <item>Automatic buffer acquisition and release</item>
/// <item>Graceful disconnect ensuring buffers aren't released during I/O</item>
/// <item>Flush queue management for batched sends</item>
/// </list>
/// </para>
/// <para>
/// Typical usage:
/// <code>
/// // Create manager
/// var manager = new RingSocketManager(ring, maxSockets: 4096);
///
/// // On accept completion
/// var socket = manager.CreateSocket(acceptedHandle);
/// _appState[socket.Id] = new MyConnectionState(socket);
///
/// // In main loop
/// int eventCount = manager.ProcessCompletions(events);
/// for (int i = 0; i &lt; eventCount; i++)
/// {
///     var state = _appState[events[i].Socket.Id];
///     switch (events[i].Type)
///     {
///         case DataReceived: state.OnDataReceived(); break;
///         case DataSent: /* flush-and-forget, nothing to do */ break;
///         case Disconnected: state.OnDisconnected(); _appState[events[i].Socket.Id] = null; break;
///     }
/// }
/// manager.Submit();
/// </code>
/// </para>
/// </remarks>
public sealed class RingSocketManager : IDisposable
{
    private readonly IIORingGroup _ring;
    private readonly IORingBufferPool _recvBufferPool;
    private readonly IORingBufferPool _sendBufferPool;
    private readonly int _maxSockets;

    // Socket storage
    private readonly RingSocket?[] _sockets;
    private readonly ushort[] _generations;
    private int _nextFreeSlot;

    // Completions buffer
    private readonly Completion[] _completions;

    // Send queue (flush-and-forget)
    private readonly Queue<RingSocket> _sendQueue = new();

    // Disconnect queue
    private readonly Queue<RingSocket> _disconnectQueue = new();

    private bool _disposed;

    /// <summary>
    /// Gets the underlying ring.
    /// </summary>
    public IIORingGroup Ring => _ring;

    /// <summary>
    /// Gets the maximum number of sockets this manager can handle.
    /// </summary>
    public int MaxSockets => _maxSockets;

    /// <summary>
    /// Gets the current number of connected sockets.
    /// </summary>
    public int ConnectedCount { get; private set; }

    /// <summary>
    /// Creates a new socket manager.
    /// </summary>
    /// <param name="ring">The IORingGroup for I/O operations.</param>
    /// <param name="maxSockets">Maximum number of concurrent sockets.</param>
    /// <param name="recvBufferSize">Size of each receive buffer (default 64KB).</param>
    /// <param name="sendBufferSize">Size of each send buffer (default 256KB).</param>
    /// <param name="initialBufferSlabs">Initial buffer pool slabs (default 8).</param>
    /// <param name="maxBufferSlabs">Maximum buffer pool slabs (default 32).</param>
    public RingSocketManager(
        IIORingGroup ring,
        int maxSockets,
        int recvBufferSize = 64 * 1024,
        int sendBufferSize = 256 * 1024,
        int initialBufferSlabs = 8,
        int maxBufferSlabs = 32)
    {
        _ring = ring ?? throw new ArgumentNullException(nameof(ring));

        if (maxSockets <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxSockets), "Must be positive");
        }

        _maxSockets = maxSockets;
        MaxOutstandingSendsPerSocket = Math.Max(1, ring.MaxOutstandingSendsPerSocket);
        _sockets = new RingSocket?[maxSockets];
        _generations = new ushort[maxSockets];
        _completions = new Completion[maxSockets];
        _nextFreeSlot = 0;

        // Create buffer pools
        // Estimate slab size based on max sockets
        var slabSize = Math.Max(64, maxSockets / maxBufferSlabs);

        _recvBufferPool = new IORingBufferPool(
            ring,
            slabSize: slabSize,
            bufferSize: recvBufferSize,
            initialSlabs: initialBufferSlabs,
            maxSlabs: maxBufferSlabs
        );

        _sendBufferPool = new IORingBufferPool(
            ring,
            slabSize: slabSize / 4, // Fewer send buffers typically needed
            bufferSize: sendBufferSize,
            initialSlabs: initialBufferSlabs / 2,
            maxSlabs: maxBufferSlabs
        );
    }

    /// <summary>
    /// Creates a new managed socket from an accepted socket handle.
    /// </summary>
    /// <param name="socketHandle">The accepted OS socket handle.</param>
    /// <returns>The created RingSocket, or null if resources exhausted.</returns>
    /// <remarks>
    /// This method:
    /// <list type="number">
    /// <item>Finds a free slot</item>
    /// <item>Acquires recv and send buffers from pools</item>
    /// <item>Registers the socket with the ring</item>
    /// <item>Posts an initial recv operation</item>
    /// </list>
    /// If any step fails, resources are cleaned up and null is returned.
    /// </remarks>
    public RingSocket? CreateSocket(nint socketHandle)
    {
        var slotId = FindFreeSlot();
        if (slotId < 0)
        {
            return null;
        }

        if (!_recvBufferPool.TryAcquire(out var recvBuffer))
        {
            return null;
        }

        if (!_sendBufferPool.TryAcquire(out var sendBuffer))
        {
            _recvBufferPool.Release(recvBuffer!);
            return null;
        }

        var connId = _ring.RegisterSocket(socketHandle);
        if (connId < 0)
        {
            _recvBufferPool.Release(recvBuffer!);
            _sendBufferPool.Release(sendBuffer!);
            return null;
        }

        var generation = ++_generations[slotId];

        var socket = new RingSocket(
            this,
            slotId,
            socketHandle,
            connId,
            generation,
            recvBuffer!,
            sendBuffer!
        );

        _sockets[slotId] = socket;
        ConnectedCount++;

        PostRecv(socket);

        return socket;
    }

    /// <summary>
    /// Gets a socket by ID, validating the generation.
    /// </summary>
    /// <param name="socketId">The socket ID.</param>
    /// <param name="generation">The expected generation.</param>
    /// <returns>The socket if valid, null if stale or invalid.</returns>
    public RingSocket? GetSocket(int socketId, ushort generation)
    {
        if (socketId < 0 || socketId >= _maxSockets)
        {
            return null;
        }

        var socket = _sockets[socketId];
        if (socket == null || socket.Generation != generation)
        {
            return null;
        }

        return socket;
    }

    /// <summary>
    /// Processes completions from the ring and returns application events.
    /// </summary>
    /// <param name="events">Buffer to receive events. Should be at least maxSockets in size.</param>
    /// <returns>Number of events written.</returns>
    /// <remarks>
    /// This method:
    /// <list type="bullet">
    /// <item>Processes the send queue (posts pending sends)</item>
    /// <item>Peeks completions from the ring</item>
    /// <item>Filters stale completions using generation</item>
    /// <item>Updates buffer positions on recv/send completion</item>
    /// <item>Handles graceful disconnect coordination</item>
    /// <item>Posts follow-up recv operations</item>
    /// <item>Processes disconnects and releases resources</item>
    /// </list>
    /// Call <see cref="Submit"/> after this to submit queued operations.
    /// </remarks>
    public int ProcessCompletions(Span<RingSocketEvent> events)
    {
        var eventCount = 0;

        ProcessSendQueue();

        var completionCount = _ring.PeekCompletions(_completions);

        for (var i = 0; i < completionCount; i++)
        {
            ref var cqe = ref _completions[i];
            var (opType, socketId, generation) = IORingUserData.Decode(cqe.UserData);

            if (opType == IORingUserData.OpAccept)
            {
                if (eventCount < events.Length)
                {
                    events[eventCount++] = RingSocketEvent.Accepted(cqe.Result);
                }
                continue;
            }

            // Get socket and validate generation
            if (socketId < 0 || socketId >= _maxSockets)
            {
                continue;
            }

            var socket = _sockets[socketId];
            if (socket == null || socket.Generation != generation)
            {
                // Stale completion - ignore silently
                continue;
            }

            switch (opType)
            {
                case IORingUserData.OpRecv:
                {
                    eventCount += HandleRecvCompletion(socket, cqe.Result, events, eventCount);
                    break;
                }

                case IORingUserData.OpSend:
                {
                    eventCount += HandleSendCompletion(socket, cqe.Result, events, eventCount);
                    break;
                }

                case IORingUserData.OpShutdown:
                {
                    // Shutdown completion - nothing to do, FIN was sent
                    // The recv will complete with 0 when client responds with FIN
                    break;
                }
            }
        }

        _ring.AdvanceCompletionQueue(completionCount);

        // CRITICAL: Submit all prepared operations BEFORE processing disconnects.
        // Operations prepared by ProcessSendQueue() and completion handlers (PostRecv/PostSend)
        // reference socket registrations and buffers that ProcessDisconnectQueue will release.
        // Submitting now ensures those operations are sent to RIO before resources are freed.
        _ring.Submit();

        // Process disconnect queue
        ProcessDisconnectQueue(events, ref eventCount);

        return eventCount;
    }

    /// <summary>
    /// Submits pending operations to the ring.
    /// Call this after <see cref="ProcessCompletions"/>.
    /// </summary>
    /// <returns>Number of operations submitted.</returns>
    public int Submit()
    {
        return _ring.Submit();
    }

    /// <summary>
    /// Waits for I/O completions or until the specified timeout expires.
    /// Delegates to the underlying ring's platform-native wait mechanism.
    /// </summary>
    /// <param name="timeoutMs">Maximum time to wait in milliseconds.</param>
    public void WaitForCompletion(int timeoutMs)
    {
        _ring.WaitForCompletion(timeoutMs);
    }

    /// <summary>
    /// Queues a socket for sending.
    /// Called by RingSocket.QueueSend().
    /// </summary>
    internal void QueueSend(RingSocket socket)
    {
        if (socket.SendQueued)
        {
            return;
        }

        socket.SendQueued = true;
        _sendQueue.Enqueue(socket);
    }

    /// <summary>
    /// Disconnects a socket immediately and queues for resource release.
    /// Use this when you need to force-close a socket without waiting for graceful disconnect.
    /// </summary>
    public void DisconnectImmediate(RingSocket socket)
    {
        socket.Connected = false;
        QueueForDisconnect(socket);
    }

    /// <summary>
    /// Queues a socket for disconnection, preventing double-queueing.
    /// </summary>
    private void QueueForDisconnect(RingSocket socket)
    {
        // Prevent double-queueing which would cause double buffer release
        if (socket.DisconnectQueued)
        {
            return;
        }

        socket.DisconnectQueued = true;
        _disconnectQueue.Enqueue(socket);
    }

    /// <summary>
    /// Initiates socket shutdown by sending FIN asynchronously.
    /// The pending recv will complete naturally when client responds with FIN.
    /// </summary>
    internal void CloseSocketHandle(RingSocket socket)
    {
        _ring.PrepareShutdown(
            socket.Handle,
            1, // SHUT_WR
            IORingUserData.Encode(IORingUserData.OpShutdown, socket.Id, socket.Generation)
        );
    }

    private int FindFreeSlot()
    {
        // Start from hint and scan forward
        var start = _nextFreeSlot;
        for (var i = 0; i < _maxSockets; i++)
        {
            var slot = (start + i) % _maxSockets;
            if (_sockets[slot] == null)
            {
                _nextFreeSlot = (slot + 1) % _maxSockets;
                return slot;
            }
        }
        return -1;
    }

    private void PostRecv(RingSocket socket)
    {
        if (socket.RecvPending || !socket.Connected)
        {
            return;
        }

        var recvBuffer = socket.RecvBuffer;
        var writeLength = recvBuffer.WritableBytes;
        if (writeLength == 0)
        {
            return;
        }

        _ring.PrepareRecvBuffer(
            socket.ConnectionId,
            recvBuffer.BufferId,
            recvBuffer.WriteOffset,
            writeLength,
            IORingUserData.Encode(IORingUserData.OpRecv, socket.Id, socket.Generation)
        );
        socket.RecvPending = true;
    }

    /// <summary>
    /// Maximum sends in flight per socket, taken from the ring so it can never exceed what the
    /// platform reserved at request-queue creation.
    /// </summary>
    public int MaxOutstandingSendsPerSocket { get; }

    private void PostSend(RingSocket socket)
    {
        if (!socket.Connected)
        {
            return;
        }

        var sendBuffer = socket.SendBuffer;

        // Drain everything queued, up to the outstanding limit. Posting from SendOffset rather than
        // ReadOffset allows a second send while the first is still outstanding.
        while (socket.SendsInFlight < MaxOutstandingSendsPerSocket)
        {
            var sendLength = sendBuffer.SendableBytes;
            if (sendLength == 0)
            {
                return;
            }

            _ring.PrepareSendBuffer(
                socket.ConnectionId,
                sendBuffer.BufferId,
                sendBuffer.SendOffset,
                sendLength,
                IORingUserData.Encode(IORingUserData.OpSend, socket.Id, socket.Generation)
            );

            sendBuffer.CommitSend(sendLength);
            socket.PushInFlight(sendLength);
        }
    }

    /// <summary>
    /// Processes the send queue, posting any queued sends.
    /// Called automatically at the start of ProcessCompletions, but can also be
    /// called manually to ensure sends are posted before checking disconnect state.
    /// </summary>
    public void ProcessSendQueue()
    {
        while (_sendQueue.Count > 0)
        {
            var socket = _sendQueue.Dequeue();
            socket.SendQueued = false;

            // Post send even if DisconnectPending - we need to drain the buffer before disconnecting
            if (socket.Connected)
            {
                PostSend(socket);
            }
        }
    }

    private int HandleRecvCompletion(RingSocket socket, int result, Span<RingSocketEvent> events, int eventIndex)
    {
        socket.RecvPending = false;

        if (result <= 0)
        {
            if (!socket.DisconnectPending)
            {
                socket.Disconnect();
            }

            if (socket.CheckDisconnect())
            {
                QueueForDisconnect(socket);
            }
            return 0;
        }

        if (!socket.Connected)
        {
            if (socket.CheckDisconnect())
            {
                QueueForDisconnect(socket);
            }
            return 0;
        }

        socket.RecvBuffer.CommitWrite(result);

        if (socket is { DisconnectPending: false, RecvBuffer.WritableBytes: > 0 })
        {
            PostRecv(socket);
        }

        if (socket.CheckDisconnect())
        {
            QueueForDisconnect(socket);
        }

        if (eventIndex >= events.Length)
        {
            return 0;
        }

        events[eventIndex] = RingSocketEvent.Received(socket, result);
        return 1;
    }

    /// <summary>
    /// When disconnect is pending and sends have drained but recv is still in flight,
    /// close the write side (send FIN) so the pending recv can complete.
    /// This bridges the gap between Path A and Path B in RingSocket.Disconnect().
    /// </summary>
    private void TrySendFinForPendingDisconnect(RingSocket socket)
    {
        if (socket is { DisconnectPending: true, SendPending: false,
                SendBuffer.ReadableBytes: <= 0, RecvPending: true, HandleClosed: false })
        {
            CloseSocketHandle(socket);
        }
    }

    private int HandleSendCompletion(RingSocket socket, int result, Span<RingSocketEvent> events, int eventIndex)
    {
        var posted = socket.SendsInFlight > 0 ? socket.PopInFlight() : 0;

        if (result <= 0)
        {
            if (!socket.DisconnectPending)
            {
                socket.Disconnect();
            }

            if (socket.CheckDisconnect())
            {
                QueueForDisconnect(socket);
            }
            else
            {
                TrySendFinForPendingDisconnect(socket);
            }
            return 0;
        }

        // Short send: routine on send() semantics, absent on RIO. Recoverable only while nothing
        // else is outstanding; sends already posted beyond the gap cannot be repaired in order, and
        // a corrupted stream is worse than a dropped connection.
        if (result != posted)
        {
            if (socket.SendsInFlight > 0)
            {
                socket.Disconnect();

                if (socket.CheckDisconnect())
                {
                    QueueForDisconnect(socket);
                }

                return 0;
            }

            socket.SendBuffer.CommitShortSend(result);

            if (socket is { Connected: true, SendBuffer.SendableBytes: > 0 })
            {
                PostSend(socket);
            }

            if (socket.CheckDisconnect())
            {
                QueueForDisconnect(socket);
            }
            else
            {
                TrySendFinForPendingDisconnect(socket);
            }

            return eventIndex < events.Length ? EmitSent(socket, result, events, eventIndex) : 0;
        }

        socket.SendBuffer.CommitRead(result);

        // Continue sending if more data (even if DisconnectPending - drain the buffer).
        // SendableBytes, not ReadableBytes: the latter now also counts bytes still in flight.
        if (socket is { Connected: true, SendBuffer.SendableBytes: > 0 })
        {
            PostSend(socket);
        }

        if (socket.CheckDisconnect())
        {
            QueueForDisconnect(socket);
        }
        else
        {
            TrySendFinForPendingDisconnect(socket);
        }

        if (eventIndex < events.Length)
        {
            events[eventIndex] = RingSocketEvent.Sent(socket, result);
            return 1;
        }

        return 0;
    }

    private static int EmitSent(RingSocket socket, int result, Span<RingSocketEvent> events, int eventIndex)
    {
        events[eventIndex] = RingSocketEvent.Sent(socket, result);
        return 1;
    }

    private void ProcessDisconnectQueue(Span<RingSocketEvent> events, ref int eventCount)
    {
        while (_disconnectQueue.Count > 0)
        {
            var socket = _disconnectQueue.Dequeue();

            // Always unregister from RIO if registered (ConnectionId >= 0) and not already unregistered
            // This is critical for DisconnectImmediate() which sets Connected=false before queueing
            if (socket is { ConnectionId: >= 0, RioUnregistered: false })
            {
                _ring.UnregisterSocket(socket.ConnectionId);
                socket.RioUnregistered = true;
            }

            if (!socket.HandleClosed)
            {
                _ring.CloseSocket(socket.Handle);
                socket.HandleClosed = true;
            }

            _recvBufferPool.Release(socket.RecvBuffer);
            _sendBufferPool.Release(socket.SendBuffer);

            _sockets[socket.Id] = null;
            ConnectedCount--;

            if (eventCount < events.Length)
            {
                events[eventCount++] = RingSocketEvent.Disconnected(socket);
            }
        }
    }

    /// <summary>
    /// Disposes the manager and all managed sockets.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        // Close all sockets (close first, then unregister for graceful close)
        for (var i = 0; i < _maxSockets; i++)
        {
            var socket = _sockets[i];
            if (socket != null)
            {
                _ring.CloseSocket(socket.Handle);
                _ring.UnregisterSocket(socket.ConnectionId);
                _sockets[i] = null;
            }
        }

        // Dispose buffer pools
        _recvBufferPool.Dispose();
        _sendBufferPool.Dispose();
    }
}
