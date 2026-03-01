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

    private static readonly Queue<NetState> _disposed = [];
    private static readonly TimeSpan ConnectingSocketIdleLimit = TimeSpan.FromMilliseconds(5000); // 5 seconds

    // Socket manager handles buffer pools, socket lifecycle, and I/O operations
    private static RingSocketManager _socketManager;

    // NetState storage indexed by RingSocket.Id
    private static readonly NetState[] _netStates = new NetState[MaxConnections];

    // Events buffer for ProcessCompletions
    private static readonly RingSocketEvent[] _events = new RingSocketEvent[MaxConnections * 2];

    // Listener management
    private static nint[] _listeners = Array.Empty<nint>();
    private static int _pendingAcceptCount;
    private const int PendingAcceptsPerListener = 32;

    private const long AliveCheckIntervalMs = 5000;
    private static long _nextAliveCheck;

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
        // Skip if already configured
        if (_socketManager != null)
        {
            return;
        }

        // Initialize IP rate limiter
        _ipRateLimiter = new IPRateLimiter(10, 10000, 1000, 2.0, 3_600_000, Core.ClosingTokenSource.Token);

        // Initialize IORingGroup
        var ring = IORingGroup.Create(queueSize: MaxConnections * 2, maxConnections: MaxConnections);

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
            var remoteIP = SocketHelper.GetRemoteAddress(clientSocket);

            if (remoteIP != null)
            {
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
                    // Allow event handlers to reject the connection
                    var args = new SocketConnectEventArgs(remoteIP);
                    EventSink.InvokeSocketConnect(args);

                    if (args.AllowConnection)
                    {
                        ring.ConfigureSocket(clientSocket);
                        CreateFromSocket(clientSocket, remoteIP);
                        goto ReplenishAccepts;
                    }

                    logger.Debug("{Address} Rejected by socket handler", remoteIP);
                }
            }

            ring.CloseSocket(clientSocket);
        }
        else if (result != -4) // EINTR
        {
            logger.Debug("Accept error: {Result}", result);
        }

        ReplenishAccepts:
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
    internal static NetState CreateFromSocket(nint socketHandle, IPAddress address)
    {
        // Use socket manager to create managed socket (handles buffers, registration, recv posting)
        var socket = _socketManager.CreateSocket(socketHandle);
        if (socket == null)
        {
            logger.Debug("Failed to create socket (resources exhausted)");
            _socketManager.Ring.CloseSocket(socketHandle);
            return null;
        }

        // Create NetState and map by socket ID
        var ns = new NetState(socket, address);
        return _netStates[socket.Id] = ns;
    }

    private static void DisconnectUnattachedSockets()
    {
        var now = Core.Now;

        // Process connecting queue with lazy removal - O(1) operations
        while (_connectingQueue.TryPeek(out var ns))
        {
            // Lazy removal: skip already-authenticated or disconnected connections
            if (!ns.Running || ns.Account != null)
            {
                _connectingQueue.Dequeue();
                continue;
            }

            // If the socket has been connected for less than the limit, we can stop
            // (queue is ordered by connection time, so remaining entries are newer)
            if (now - ns.ConnectedOn < ConnectingSocketIdleLimit)
            {
                break;
            }

            _connectingQueue.Dequeue();

            // Socket must have finished the entire authentication process or be forcibly disconnected
            if (!ns.SentFirstPacket || !ns.Seeded)
            {
                ns.Disconnect(null);

                // Force immediate cleanup - these are unauthenticated connections
                // where graceful disconnect can get stuck with pending sends.
                if (ns._socket is { DisconnectPending: true })
                {
                    _socketManager.DisconnectImmediate(ns._socket);
                }
            }
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
        var curTicks = Core.TickCount;
        DisconnectUnattachedSockets();

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

        // Process all completions through the manager FIRST
        // This ensures DataReceived events are processed and HandleReceive runs,
        // which may call Send() and add to _flushPending
        var eventCount = _socketManager.ProcessCompletions(_events);

        for (var i = 0; i < eventCount; i++)
        {
            ref var evt = ref _events[i];

            switch (evt.Type)
            {
                case RingSocketEventType.Accept:
                    {
                        // Handle accept - AcceptedSocketHandle contains the result
                        HandleAcceptCompletion((int)evt.AcceptedSocketHandle);
                        break;
                    }

                case RingSocketEventType.DataReceived:
                    {
                        var nsRecv = _netStates[evt.Socket.Id];
                        // Verify generation via object identity to avoid stale completion issues
                        if (nsRecv != null && nsRecv._socket == evt.Socket)
                        {
                            nsRecv.NextActivityCheck = curTicks + 30000;
                            HandleDataReceived(nsRecv, evt.BytesTransferred);
                        }
                        break;
                    }

                case RingSocketEventType.DataSent:
                    {
                        var nsSend = _netStates[evt.Socket.Id];
                        // Verify generation via object identity
                        if (nsSend != null && nsSend._socket == evt.Socket)
                        {
                            // Update activity check on successful send
                            nsSend.NextActivityCheck = curTicks + 30000;
                        }
                        break;
                    }

                case RingSocketEventType.Disconnected:
                    {
                        var nsDisc = _netStates[evt.Socket.Id];
                        // Verify generation via object identity
                        if (nsDisc != null && nsDisc._socket == evt.Socket)
                        {
                            HandleDisconnected(nsDisc);
                        }
                        break;
                    }
            }
        }

        // Process flush queue AFTER event processing
        // This ensures sends triggered by HandleReceive (via packet handlers like SendPlayServerAck)
        // are queued in the SAME Slice, not the next one
        while (_flushPending.TryDequeue(out var ns))
        {
            // Reset flag to allow re-queueing if more data is added later
            ns._flushQueued = false;

            if (ns.Running)
            {
                ns._socket?.QueueSend();
            }
        }

        // CRITICAL: Process send queue NOW to post pending sends
        // This ensures PostSend() runs and sets SendPending=true BEFORE disconnect checks
        // Without this, Disconnect() would see SendPending=false even though data is queued
        _socketManager.ProcessSendQueue();

        // Process pending disconnects AFTER flush queue AND send queue processing
        // This ensures the traditional order: Game Logic (Sends/Disconnects) → Receives → Flush → Disconnect
        // Any Send() calls made after Disconnect() in the same tick are flushed before disconnect
        while (_pendingDisconnects.TryDequeue(out var ns))
        {
            // Reset flag to allow re-queueing if reconnect happens
            ns._disconnectQueued = false;

            if (ns.Running && ns._socket != null)
            {
                // RingSocket.Disconnect() handles graceful disconnect:
                // - Waits for pending sends to flush (if SendBuffer.ReadableBytes > 0)
                // - Waits for in-flight I/O to complete
                // - Ensures buffers aren't released while kernel is still using them
                ns._socket.Disconnect();
            }
        }

        // Submit any queued operations
        _socketManager.Submit();

        // Process disposes
        while (_disposed.TryDequeue(out var ns))
        {
            ns.DisposeInternal();
        }

        // Check for dead connections AFTER processing all completions.
        // Recv completions reset NextActivityCheck, so after a server stall,
        // buffered client pings update timestamps before this check fires.
        if (curTicks - _nextAliveCheck >= 0)
        {
            _nextAliveCheck = curTicks + AliveCheckIntervalMs;
            CheckAllAlive();
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
        ns.DecryptRecvBuffer(bytesReceived);

        // Process packets
        ns.HandleReceive();
    }

    private static void HandleDisconnected(NetState ns)
    {
        var slotId = ns._socket.Id;

        // IMPORTANT: Check if the slot still points to this NetState
        // During quick reconnect, the slot might have been reused for a new connection
        var currentNs = _netStates[slotId];
        if (currentNs != ns)
        {
            // Slot was already reused - don't clear it!
            // Just mark this NetState as not running and queue for dispose
            ns._running = false;
            _disposed.Enqueue(ns);
            return;
        }

        // Clear the NetState slot
        _netStates[slotId] = null;

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
