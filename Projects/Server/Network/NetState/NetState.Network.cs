/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2025 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: NetState.Network.cs                                             *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.  *
 *************************************************************************/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Network;
using System.Runtime.CompilerServices;

namespace Server.Network;

/// <summary>
/// Network infrastructure for IORingGroup-based socket I/O.
/// </summary>
public partial class NetState
{
    // Buffer sizes
    private const int RecvBufferSize = 1024 * 64;   // 64KB recv buffers
    private const int SendBufferSize = 1024 * 256;  // 256KB send buffers
    private const int MaxConnections = 4096;        // Max concurrent connections

    // IORingGroup I/O
    private static IIORingGroup _ring;
    private static IORingBufferPool _recvBufferPool;
    private static IORingBufferPool _sendBufferPool;
    private static readonly NetState[] _netStates = new NetState[MaxConnections];
    private static readonly Completion[] _completions = new Completion[MaxConnections];

    // Listener management
    private static nint[] _listeners = Array.Empty<nint>();
    private static int _pendingAcceptCount;
    private const int PendingAcceptsPerListener = 32;

    // Generation tracking to detect stale completions after slot reuse
    // Each slot has a generation counter that increments when the slot is reused
    private static readonly ushort[] _slotGenerations = new ushort[MaxConnections];

    // UserData encoding:
    // [8 bits: opType][16 bits: generation][8 bits: reserved][32 bits: index]
    private const ulong OpAccept = 1UL << 56;
    private const ulong OpRecv = 2UL << 56;
    private const ulong OpSend = 3UL << 56;
    private const ulong OpMask = 0xFF00_0000_0000_0000UL;
    private const ulong GenMask = 0x00FF_FF00_0000_0000UL;
    private const int GenShift = 40;

    /// <summary>
    /// Gets the IORingGroup instance for socket operations.
    /// </summary>
    public static IIORingGroup Ring => _ring;

    /// <summary>
    /// Gets the listening addresses that the server is bound to.
    /// </summary>
    public static IPEndPoint[] ListeningAddresses { get; private set; }

    private static IPRateLimiter _ipRateLimiter;

    /// <summary>
    /// Configures the IORingGroup and buffer pools.
    /// </summary>
    private static void ConfigureNetwork()
    {
        // Initialize IP rate limiter
        _ipRateLimiter = new IPRateLimiter(10, 10000, 1000, 2.0, 3_600_000, Core.ClosingTokenSource.Token);

        // Initialize IORingGroup
        _ring = IORingGroup.Create(queueSize: 4096);

        _recvBufferPool = new IORingBufferPool(
            _ring,
            slabSize: 256,
            bufferSize: RecvBufferSize,
            initialSlabs: 8,
            maxSlabs: 32
        );

        _sendBufferPool = new IORingBufferPool(
            _ring,
            slabSize: 64,
            bufferSize: SendBufferSize,
            initialSlabs: 8,
            maxSlabs: 32
        );
    }

    /// <summary>
    /// Starts the network server on configured listening addresses.
    /// </summary>
    public static void Start()
    {
        HashSet<IPEndPoint> listeningAddresses = [];
        List<nint> listeners = [];

        for (var i = 0; i < ServerConfiguration.Listeners.Count; i++)
        {
            var ipep = ServerConfiguration.Listeners[i];
            var listener = _ring.CreateListener(ipep.Address.ToString(), (ushort)ipep.Port, 256);
            if (listener == -1)
            {
                logger.Warning("Failed to create listener for {Address}", ipep);
                continue;
            }

            if (ipep.Address.Equals(IPAddress.Any) || ipep.Address.Equals(IPAddress.IPv6Any))
            {
                listeningAddresses.UnionWith(GetListeningAddresses(ipep));
            }
            else
            {
                listeningAddresses.Add(ipep);
            }

            listeners.Add(listener);
        }

        foreach (var ipep in listeningAddresses)
        {
            logger.Information("Listening: {Address}", ipep);
        }

        ListeningAddresses = listeningAddresses.ToArray();

        // Register listeners to start accepting connections
        RegisterListeners(listeners.ToArray());
    }

    /// <summary>
    /// Shuts down the network server and closes all listeners.
    /// </summary>
    public static void Shutdown()
    {
        CloseListeners();
    }

    /// <summary>
    /// Gets the actual listening addresses for a wildcard endpoint.
    /// </summary>
    public static IEnumerable<IPEndPoint> GetListeningAddresses(IPEndPoint ipep) =>
        NetworkInterface.GetAllNetworkInterfaces().SelectMany(adapter =>
            adapter.GetIPProperties().UnicastAddresses
                .Where(uip => ipep.AddressFamily == uip.Address.AddressFamily)
                .Select(uip => new IPEndPoint(uip.Address, ipep.Port))
        );

    /// <summary>
    /// Registers listeners with the ring and starts accepting connections.
    /// </summary>
    private static void RegisterListeners(nint[] listeners)
    {
        _listeners = listeners;
        logger.Information("[DEBUG] RegisterListeners: {Count} listeners", _listeners.Length);

        // Queue initial accept operations for each listener
        for (var i = 0; i < _listeners.Length; i++)
        {
            var listener = _listeners[i];
            logger.Information("[DEBUG] Listener[{Index}] = {Handle}", i, listener);
            for (var j = 0; j < PendingAcceptsPerListener; j++)
            {
                _ring.PrepareAccept(listener, 0, 0, OpAccept);
                _pendingAcceptCount++;
            }
        }
        logger.Information("[DEBUG] Initial pending accepts: {Count}", _pendingAcceptCount);
    }

    /// <summary>
    /// Closes all listeners.
    /// </summary>
    private static void CloseListeners()
    {
        foreach (var listener in _listeners)
        {
            _ring.CloseListener(listener);
        }

        _listeners = [];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong EncodeUserData(ulong opType, int index, ushort generation) =>
        opType | ((ulong)generation << GenShift) | (uint)index;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static (ulong opType, int index, ushort generation) DecodeUserData(ulong userData) =>
        (userData & OpMask, (int)(userData & 0xFFFFFFFF), (ushort)((userData & GenMask) >> GenShift));

    private static int FindFreeSlot()
    {
        for (int i = 0; i < MaxConnections; i++)
        {
            if (_netStates[i] == null)
            {
                return i;
            }
        }
        return -1;
    }

    private static void HandleAcceptCompletion(int result)
    {
        _pendingAcceptCount--;

        logger.Information("[DEBUG] HandleAcceptCompletion: result={Result}, pendingAccepts={Pending}", result, _pendingAcceptCount);

        // EAGAIN (-11) means no connection pending - just re-queue
        if (result == -11)
        {
            goto ReplenishAccepts;
        }

        if (result >= 0)
        {
            var clientSocket = (nint)result;

            // Get remote IP address
            var remoteIP = SocketHelper.GetRemoteAddress(clientSocket);
            logger.Information("[DEBUG] Accept got socket={Socket}, remoteIP={RemoteIP}", clientSocket, remoteIP);

            if (remoteIP != null)
            {
                // Validation checks
                if (_ipRateLimiter != null && !_ipRateLimiter.Verify(remoteIP, out var totalAttempts))
                {
                    logger.Debug("{Address} Past IP limit threshold ({TotalAttempts})", remoteIP, totalAttempts);
                }
                else if (Firewall.IsBlocked(remoteIP))
                {
                    logger.Debug("{Address} Firewalled", remoteIP);
                }
                else
                {
                    // Configure socket and create NetState
                    logger.Information("[DEBUG] Configuring socket and creating NetState for {RemoteIP}", remoteIP);
                    _ring.ConfigureSocket(clientSocket);
                    CreateFromSocket(clientSocket, remoteIP);
                    goto ReplenishAccepts;
                }
            }
            else
            {
                logger.Warning("[DEBUG] Failed to get remote IP for socket {Socket}", clientSocket);
            }

            // Rejected - close the socket
            _ring.CloseSocket(clientSocket);
        }
        else if (result != -4) // EINTR
        {
            logger.Debug("Accept error: {Result}", result);
        }

        ReplenishAccepts:
        // Replenish accepts to maintain pending count
        var targetAccepts = _listeners.Length * PendingAcceptsPerListener;
        var replenished = 0;
        while (_pendingAcceptCount < targetAccepts && _listeners.Length > 0)
        {
            var listenerIndex = _pendingAcceptCount % _listeners.Length;
            logger.Information("[DEBUG] PrepareAccept: listener[{Index}]={Handle}, pendingCount={Count}",
                listenerIndex, _listeners[listenerIndex], _pendingAcceptCount);
            _ring.PrepareAccept(_listeners[listenerIndex], 0, 0, OpAccept);
            _pendingAcceptCount++;
            replenished++;
        }
        if (replenished > 0)
        {
            logger.Information("[DEBUG] Replenished {Count} accepts, total pending={Total}", replenished, _pendingAcceptCount);
        }
    }

    /// <summary>
    /// Creates a NetState from an accepted socket handle.
    /// </summary>
    private static void CreateFromSocket(nint socket, IPAddress address)
    {
        logger.Information("[DEBUG] CreateFromSocket: socket={Socket}, address={Address}", socket, address);

        // Find free slot
        var netStateIndex = FindFreeSlot();
        if (netStateIndex < 0)
        {
            logger.Debug("No free connection slots, closing socket");
            _ring.CloseSocket(socket);
            return;
        }

        logger.Information("[DEBUG] Found free slot: {Index}", netStateIndex);

        // Acquire buffers from pools
        if (!_recvBufferPool.TryAcquire(out var recvBuffer))
        {
            logger.Debug("Recv buffer pool exhausted, closing socket");
            _ring.CloseSocket(socket);
            return;
        }

        if (!_sendBufferPool.TryAcquire(out var sendBuffer))
        {
            _recvBufferPool.Release(recvBuffer!);
            logger.Debug("Send buffer pool exhausted, closing socket");
            _ring.CloseSocket(socket);
            return;
        }

        logger.Information("[DEBUG] Acquired buffers: recv={RecvId}, send={SendId}", recvBuffer!.BufferId, sendBuffer!.BufferId);

        // Register socket with ring
        var connId = _ring.RegisterSocket(socket);
        if (connId < 0)
        {
            _recvBufferPool.Release(recvBuffer!);
            _sendBufferPool.Release(sendBuffer!);
            logger.Debug("Failed to register socket with ring");
            _ring.CloseSocket(socket);
            return;
        }

        // Increment generation for this slot to invalidate any stale completions
        var generation = ++_slotGenerations[netStateIndex];
        logger.Information("[DEBUG] Registered socket with connId={ConnId}", connId);

        // Create NetState
        var ns = new NetState(socket, address, netStateIndex, connId, recvBuffer, sendBuffer, generation);
        _netStates[netStateIndex] = ns;

        logger.Information("[DEBUG] Created NetState, posting initial recv");

        // Post initial recv
        PostRecv(ns);
    }

    private static void PostRecv(NetState ns)
    {
        if (ns._recvPending || !ns._running)
        {
            return;
        }

        var writeSpan = ns._recvBuffer.GetWriteSpan(out var offset);
        var available = writeSpan.Length;
        if (available == 0)
        {
            return;
        }

        _ring.PrepareRecvBuffer(
            ns._connId,
            ns._recvBuffer.BufferId,
            offset,
            available,
            EncodeUserData(OpRecv, ns._netStateIndex, ns._generation)
        );
        ns._recvPending = true;
    }

    private static void PostSend(NetState ns)
    {
        if (ns._sendPending || !ns._running)
        {
            return;
        }

        var readSpan = ns._sendBuffer.GetReadSpan(out var offset);
        var available = readSpan.Length;
        if (available == 0)
        {
            return;
        }

        _ring.PrepareSendBuffer(
            ns._connId,
            ns._sendBuffer.BufferId,
            offset,
            available,
            EncodeUserData(OpSend, ns._netStateIndex, ns._generation)
        );
        ns._sendPending = true;
    }

    private static void HandleRecvCompletion(int index, ushort generation, int bytesReceived)
    {
        logger.Information("[DEBUG] HandleRecvCompletion: index={Index}, bytesReceived={Bytes}", index, bytesReceived);

        var ns = _netStates[index];
        if (ns == null || !ns._running)
        {
            logger.Information("[DEBUG] HandleRecvCompletion: NetState null");
            return;
        }

        // Check generation to detect stale completions after slot reuse
        if (ns._generation != generation)
        {
            logger.Information("[DEBUG] HandleRecvCompletion: disconnect pending, checking if done");
            return;
        }

        ns._recvPending = false;

        if (bytesReceived <= 0)
        {
            logger.Information("[DEBUG] HandleRecvCompletion: bytesReceived <= 0, disconnecting");
            ns.Disconnect(bytesReceived == 0 ? string.Empty : "Recv error");
            // Check if we were waiting for this recv to complete before disconnecting
            // (e.g., disconnect was requested while both recv and send were in-flight)
            ns.CheckPendingDisconnect();
            return;
        }

        // Data already in recv buffer - advance write position
        ns._recvBuffer.CommitWrite(bytesReceived);
        ns.DecodeRecvBuffer(bytesReceived);

        logger.Information("[DEBUG] HandleRecvCompletion: calling HandleReceive, protocolState={State}", ns._protocolState);
        ns.HandleReceive();

        // Queue next recv if there's space
        if (ns._running && ns._recvBuffer.GetWriteSpan(out _).Length > 0)
        {
            PostRecv(ns);
        }
    }

    private static void HandleSendCompletion(int index, ushort generation, int bytesSent)
    {
        var ns = _netStates[index];
        if (ns == null)
        {
            return;
        }

        // Check generation to detect stale completions after slot reuse
        if (ns._generation != generation)
        {
            return;
        }

        ns._sendPending = false;

        // Check if this was a pending disconnect - I/O is now complete
        if (ns._disconnectPending)
        {
            logger.Information("[DEBUG] HandleSendCompletion: disconnect pending, checking if done");
            return;
        }

        if (!ns._running)
        {
            return;
        }

        if (bytesSent <= 0)
        {
            ns.Disconnect(bytesSent == 0 ? string.Empty : "Send error");
            // Check if we were waiting for this send to complete before disconnecting
            // (e.g., disconnect was requested while both recv and send were in-flight)
            ns.CheckPendingDisconnect();
            return;
        }

        // Advance read position - data has been sent
        ns._sendBuffer.CommitRead(bytesSent);
        ns.NextActivityCheck = Core.TickCount + 90000;

        // If more data to send, queue another send
        if (ns._running && ns._sendBuffer.GetReadSpan(out _).Length > 0)
        {
            PostSend(ns);
        }
        else
        {
            // Check if we're waiting to disconnect after all sends complete
            ns.CheckPendingDisconnect();
        }
    }

    private static void DisconnectUnattachedSockets()
    {
        var now = Core.Now;

        // Clear out any sockets that have been connecting for too long
        while (_connecting.Count > 0)
        {
            var ns = _connecting.Min;
            var socketTime = ns.ConnectedOn;

            // If the socket has been connected for less than the limit, we can stop checking
            if (now - socketTime < ConnectingSocketIdleLimit)
            {
                break;
            }

            // Socket must have finished the entire authentication process or be forcibly disconnected.
            if (!ns.Running || !ns.SentFirstPacket || !ns.Seeded || ns.Account == null)
            {
                // Not sending a message because it will fill up the logs.
                ns.Disconnect(null);
            }

            _connecting.Remove(ns);
        }
    }

    public static void FlushAll()
    {
        while (_flushPending.TryDequeue(out var ns))
        {
            if (ns == null)
            {
                continue;
            }

            // Reset flag to allow re-queueing if more data is added later
            ns._flushQueued = false;

            if (ns.Running)
            {
                PostSend(ns);
            }
        }

        // Submit any pending operations
        _ring?.Submit();
    }

    private static int _sliceCounter;

    public static void Slice()
    {
        // Periodic status log every ~1000 slices
        if (++_sliceCounter % 1000000 == 0)
        {
            logger.Information("[DEBUG] Slice #{Count}: pendingAccepts={Pending}, listeners={Listeners}",
                _sliceCounter, _pendingAcceptCount, _listeners.Length);
        }

        // DisconnectUnattachedSockets();

        // Process throttled states
        while (_throttled.Count > 0)
        {
            var ns = _throttled.Dequeue();
            if (ns.Running)
            {
                ns.HandleReceive(true);
            }
        }

        // This is enqueued by HandleReceive if already throttled and still throttled
        while (_throttledPending.Count > 0)
        {
            _throttled.Enqueue(_throttledPending.Dequeue());
        }

        // Process flush queue - queue send operations for states with pending data
        while (_flushPending.TryDequeue(out var ns))
        {
            // Reset flag to allow re-queueing if more data is added later
            ns._flushQueued = false;

            if (ns.Running)
            {
                PostSend(ns);
            }
        }

        // Submit any queued operations
        _ring.Submit();

        // Get completions (non-blocking)
        var count = _ring.PeekCompletions(_completions);

        if (count > 0)
        {
            logger.Information("[DEBUG] Slice: got {Count} completions", count);
        }

        for (var i = 0; i < count; i++)
        {
            ref var cqe = ref _completions[i];
            var (opType, index, generation) = DecodeUserData(cqe.UserData);

            logger.Information("[DEBUG] Completion: opType={OpType}, index={Index}, result={Result}",
                opType == OpAccept ? "Accept" : opType == OpRecv ? "Recv" : opType == OpSend ? "Send" : "Unknown",
                index, cqe.Result);

            switch (opType)
            {
                case OpAccept:
                    HandleAcceptCompletion(cqe.Result);
                    break;
                case OpRecv:
                    HandleRecvCompletion(index, generation, cqe.Result);
                    break;
                case OpSend:
                    HandleSendCompletion(index, generation, cqe.Result);
                    break;
            }
        }

        _ring.AdvanceCompletionQueue(count);

        // Process disposes
        while (_disposed.TryDequeue(out var ns))
        {
            ns.Dispose();
        }
    }

    public static void CheckAllAlive()
    {
        try
        {
            var curTicks = Core.TickCount;

            foreach (var ns in Instances)
            {
                ns.CheckAlive(curTicks);
            }
        }
        catch (Exception ex)
        {
            TraceException(ex);
        }
    }
}
