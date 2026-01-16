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

    // Socket manager handles buffer pools, socket lifecycle, and I/O operations
    private static RingSocketManager _socketManager;

    // NetState storage indexed by RingSocket.Id
    private static readonly NetState[] _netStates = new NetState[MaxConnections];

    // Events buffer for ProcessCompletions
    private static readonly RingSocketEvent[] _events = new RingSocketEvent[MaxConnections];

    // Completions buffer for accept handling (accepts are handled separately)
    private static readonly Completion[] _completions = new Completion[64];

    // Listener management
    private static nint[] _listeners = Array.Empty<nint>();
    private static int _pendingAcceptCount;
    private const int PendingAcceptsPerListener = 32;

    /// <summary>
    /// Gets the IORingGroup instance for socket operations.
    /// </summary>
    public static IIORingGroup Ring => _socketManager?.Ring;

    /// <summary>
    /// Gets the listening addresses that the server is bound to.
    /// </summary>
    public static IPEndPoint[] ListeningAddresses { get; private set; }

    private static IPRateLimiter _ipRateLimiter;

    /// <summary>
    /// Configures the IORingGroup and socket manager.
    /// </summary>
    private static void ConfigureNetwork()
    {
        // Initialize IP rate limiter
        _ipRateLimiter = new IPRateLimiter(10, 10000, 1000, 2.0, 3_600_000, Core.ClosingTokenSource.Token);

        // Initialize IORingGroup
        var ring = IORingGroup.Create(queueSize: 4096);

        // Create socket manager which handles buffer pools and socket lifecycle
        _socketManager = new RingSocketManager(
            ring,
            maxSockets: MaxConnections,
            recvBufferSize: RecvBufferSize,
            sendBufferSize: SendBufferSize,
            initialBufferSlabs: 8,
            maxBufferSlabs: 32
        );
    }

    /// <summary>
    /// Starts the network server on configured listening addresses.
    /// </summary>
    public static void Start()
    {
        HashSet<IPEndPoint> listeningAddresses = [];
        List<nint> listeners = [];

        var ring = _socketManager.Ring;
        for (var i = 0; i < ServerConfiguration.Listeners.Count; i++)
        {
            var ipep = ServerConfiguration.Listeners[i];
            var listener = ring.CreateListener(ipep.Address.ToString(), (ushort)ipep.Port, 256);
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

        var ring = _socketManager.Ring;

        // Queue initial accept operations for each listener
        for (var i = 0; i < _listeners.Length; i++)
        {
            var listener = _listeners[i];
            for (var j = 0; j < PendingAcceptsPerListener; j++)
            {
                ring.PrepareAccept(listener, 0, 0, IORingUserData.EncodeAccept());
                _pendingAcceptCount++;
            }
        }
    }

    /// <summary>
    /// Closes all listeners.
    /// </summary>
    private static void CloseListeners()
    {
        var ring = _socketManager?.Ring;
        if (ring == null)
        {
            return;
        }

        foreach (var listener in _listeners)
        {
            ring.CloseListener(listener);
        }

        _listeners = [];
    }

    private static void HandleAcceptCompletion(int result)
    {
        _pendingAcceptCount--;

        var ring = _socketManager.Ring;

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
                    ring.ConfigureSocket(clientSocket);
                    CreateFromSocket(clientSocket, remoteIP);
                    goto ReplenishAccepts;
                }
            }

            // Rejected - close the socket
            ring.CloseSocket(clientSocket);
        }
        else if (result != -4) // EINTR
        {
            logger.Debug("Accept error: {Result}", result);
        }

        ReplenishAccepts:
        // Replenish accepts to maintain pending count
        var targetAccepts = _listeners.Length * PendingAcceptsPerListener;
        while (_pendingAcceptCount < targetAccepts && _listeners.Length > 0)
        {
            var listenerIndex = _pendingAcceptCount % _listeners.Length;
            ring.PrepareAccept(_listeners[listenerIndex], 0, 0, IORingUserData.EncodeAccept());
            _pendingAcceptCount++;
        }
    }

    /// <summary>
    /// Creates a NetState from an accepted socket handle.
    /// </summary>
    private static void CreateFromSocket(nint socketHandle, IPAddress address)
    {
        // Use socket manager to create managed socket (handles buffers, registration, recv posting)
        var socket = _socketManager.CreateSocket(socketHandle);
        if (socket == null)
        {
            logger.Debug("Failed to create socket (resources exhausted)");
            _socketManager.Ring.CloseSocket(socketHandle);
            return;
        }

        // Create NetState and map by socket ID
        var ns = new NetState(socket, address);
        _netStates[socket.Id] = ns;
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
                ns._socket?.QueueSend();
            }
        }

        // Submit any pending operations
        _socketManager?.Submit();
    }

    public static void Slice()
    {
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
                ns._socket?.QueueSend();
            }
        }

        var ring = _socketManager.Ring;

        // Process accept completions first (handled separately from socket events)
        ProcessAcceptCompletions(ring);

        // Process socket events (recv/send/disconnect)
        var eventCount = _socketManager.ProcessCompletions(_events);

        for (var i = 0; i < eventCount; i++)
        {
            ref var evt = ref _events[i];
            var ns = _netStates[evt.Socket.Id];

            if (ns == null)
            {
                continue;
            }

            switch (evt.Type)
            {
                case RingSocketEventType.DataReceived:
                    HandleDataReceived(ns, evt.BytesTransferred);
                    break;

                case RingSocketEventType.DataSent:
                    // Update activity check on successful send
                    ns.NextActivityCheck = Core.TickCount + 90000;
                    break;

                case RingSocketEventType.Disconnected:
                    HandleDisconnected(ns);
                    break;
            }
        }

        // Submit any queued operations
        _socketManager.Submit();

        // Process disposes
        while (_disposed.TryDequeue(out var ns))
        {
            ns.Dispose();
        }
    }

    private static void ProcessAcceptCompletions(IIORingGroup ring)
    {
        // Peek completions to find accepts
        // Note: We don't advance here - RingSocketManager.ProcessCompletions will advance ALL completions
        // including accepts (which it skips processing)
        var count = ring.PeekCompletions(_completions);

        for (var i = 0; i < count; i++)
        {
            ref var cqe = ref _completions[i];
            var opType = IORingUserData.GetOpType(cqe.UserData);

            if (opType == IORingUserData.OpAccept)
            {
                HandleAcceptCompletion(cqe.Result);
            }
        }
    }

    private static void HandleDataReceived(NetState ns, int bytesReceived)
    {
        if (!ns._running)
        {
            return;
        }

        // Data is already committed to buffer by RingSocketManager
        // Decode if encryption is enabled
        ns.DecodeRecvBuffer(bytesReceived);

        // Process packets
        ns.HandleReceive();
    }

    private static void HandleDisconnected(NetState ns)
    {
        // Clear the NetState slot
        _netStates[ns._socket.Id] = null;

        // Mark as not running and queue for dispose
        ns._running = false;
        _disposed.Enqueue(ns);
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
