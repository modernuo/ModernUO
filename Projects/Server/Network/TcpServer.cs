/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2023 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: TcpServer.cs                                                    *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;
using Server.Logging;
using Server.Misc;

namespace Server.Network;

public static class TcpServer
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(TcpServer));

    private const long MaximumSocketIdleDelay = 2000; // 2 seconds
    private const long ListenerErrorMessageDelay = 10000; // 10 seconds

    private static long _nextMaximumSocketsReachedMessage;
    private static readonly SemaphoreSlim _queueSemaphore = new(0);
    private static readonly ConcurrentQueue<Socket> _connectingQueue = [];
    private static Thread _processConnectionsThread;

    // Sanity. 256 * 1024 * 4096 = ~1.3GB of ram
    public static int MaxConnections { get; set; }

    public static IPEndPoint[] ListeningAddresses { get; private set; }
    public static Socket[] Listeners { get; private set; }
    public static Thread[] ListenerThreads { get; private set; }

    // By default should sort T1 then T2
    public static readonly SortedSet<(long, NetState)> _socketsConnecting = [];

    public static ConcurrentQueue<NetState> ConnectedQueue { get; } = [];

    public static void Configure()
    {
        MaxConnections = ServerConfiguration.GetOrUpdateSetting("tcpServer.maxConnections", 4096);
    }

    public static void Start()
    {
        HashSet<IPEndPoint> listeningAddresses = [];
        List<Socket> listeners = [];
        List<Thread> listenerThreads = [];

        foreach (var ipep in ServerConfiguration.Listeners)
        {
            var listener = CreateListener(ipep);
            if (listener == null)
            {
                continue;
            }

            bool added;

            if (ipep.Address.Equals(IPAddress.Any) || ipep.Address.Equals(IPAddress.IPv6Any))
            {
                var beforeCount = listeningAddresses.Count;
                listeningAddresses.UnionWith(GetListeningAddresses(ipep));
                added = listeningAddresses.Count > beforeCount;
            }
            else
            {
                added = listeningAddresses.Add(ipep);
            }

            if (added)
            {
                listeners.Add(listener);

                var listenerThread = new Thread(() => BeginAcceptingSockets(listener));
                listenerThreads.Add(listenerThread);
                listenerThread.Start();
            }
        }

        foreach (var ipep in listeningAddresses)
        {
            logger.Information("Listening: {Address}:{Port}", ipep.Address, ipep.Port);
        }

        ListeningAddresses = listeningAddresses.ToArray();
        Listeners = listeners.ToArray();
        ListenerThreads = listenerThreads.ToArray();

        _processConnectionsThread = new Thread(ProcessConnections);
        _processConnectionsThread.Start();
    }

    public static IEnumerable<IPEndPoint> GetListeningAddresses(IPEndPoint ipep) =>
        NetworkInterface.GetAllNetworkInterfaces().SelectMany(adapter =>
            adapter.GetIPProperties().UnicastAddresses
                .Where(uip => ipep.AddressFamily == uip.Address.AddressFamily)
                .Select(uip => new IPEndPoint(uip.Address, ipep.Port))
        );

    public static Socket CreateListener(IPEndPoint ipep)
    {
        var listener = new Socket(ipep.AddressFamily, SocketType.Stream, ProtocolType.Tcp)
        {
            LingerState = new LingerOption(false, 0),
            ExclusiveAddressUse = true,
            NoDelay = true,
            Blocking = false,
            SendBufferSize = 64 * 1024,
            ReceiveBufferSize = 64 * 1024
        };

        try
        {
            listener.Bind(ipep);
            listener.Listen(128);
            return listener;
        }
        catch (SocketException se)
        {
            // WSAEADDRINUSE
            if (se.ErrorCode == 10048)
            {
                logger.Warning("Listener: {Address}:{Port}: Failed (In Use)", ipep.Address, ipep.Port);
            }
            // WSAEADDRNOTAVAIL
            else if (se.ErrorCode == 10049)
            {
                logger.Warning("Listener {Address}:{Port}: Failed (Unavailable)", ipep.Address, ipep.Port);
            }
            else
            {
                logger.Warning(se, "Listener Exception:");
            }
        }

        return null;
    }

    private static async void BeginAcceptingSockets(Socket listener)
    {
        var cancellationToken = Core.ClosingTokenSource.Token;

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var socket = await listener.AcceptAsync(cancellationToken);
                _connectingQueue.Enqueue(socket);
                _queueSemaphore.Release();
            }
            catch
            {
                // ignored
            }
        }

        listener.Close();
    }

    private static async void ProcessConnections()
    {
        var ipLimiter = IPLimiter.Enabled;
        var cancellationToken = Core.ClosingTokenSource.Token;

        while (!cancellationToken.IsCancellationRequested)
        {
            await _queueSemaphore.WaitAsync(cancellationToken);

            Firewall.ProcessQueue();

            if (_connectingQueue.TryDequeue(out var socket))
            {
                try
                {
                    // Clear out any sockets that have been connecting for too long
                    while (_socketsConnecting.Count > 0)
                    {
                        var socketTime = _socketsConnecting.Max;
                        if (Core.TickCount - socketTime.Item1 <= MaximumSocketIdleDelay)
                        {
                            break;
                        }

                        var socketToCheck = socketTime.Item2;
                        if (socketToCheck.Running && !socketToCheck.Seeded)
                        {
                            socketToCheck.Disconnect(null);
                        }

                        _socketsConnecting.Remove(socketTime);
                    }

                    var remoteIP = ((IPEndPoint)socket.RemoteEndPoint).Address;

                    if (NetState.Instances.Count >= MaxConnections)
                    {
                        var ticks = Core.TickCount;

                        if (ticks - _nextMaximumSocketsReachedMessage > 0)
                        {
                            if (socket.RemoteEndPoint is IPEndPoint ipep)
                            {
                                var ip = ipep.Address.ToString();
                                logger.Warning("{Address} Failed (Maximum connections reached)", ip);
                            }

                            _nextMaximumSocketsReachedMessage = ticks + ListenerErrorMessageDelay;
                        }

                        socket.Close();
                        continue;
                    }

                    if (ipLimiter && !IPLimiter.Verify(remoteIP))
                    {
                        TraceDisconnect("Past IP limit threshold", remoteIP);
                        logger.Debug("{Address} Past IP limit threshold", remoteIP);
                        socket.Close();
                        continue;
                    }

                    if (Firewall.IsBlocked(remoteIP))
                    {
                        TraceDisconnect("Firewalled", remoteIP);
                        logger.Debug("{Address} Firewalled", remoteIP);
                        socket.Close();
                    }

                    var ns = new NetState(socket);
                    _socketsConnecting.Add((Core.TickCount, ns));
                    ConnectedQueue.Enqueue(ns);
                }
                catch
                {
                    // ignored
                }
            }
        }
    }

    private static void TraceDisconnect(string reason, IPAddress ip)
    {
        try
        {
            using StreamWriter op = new StreamWriter("network-socket-disconnects.log", true);
            op.WriteLine($"# {Core.Now}");

            op.WriteLine($"Address: {ip}");
            op.WriteLine(reason);

            op.WriteLine();
            op.WriteLine();
        }
        catch
        {
            // ignored
        }
    }
}
