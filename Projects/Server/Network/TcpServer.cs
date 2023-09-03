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
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using Server.Logging;

namespace Server.Network;

public static class TcpServer
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(TcpServer));

    private const int MaxConnectionsPerLoop = 250;

    // Sanity. 256 * 1024 * 4096 = ~1.3GB of ram
    public static int MaxConnections { get; set; }

    private const long _listenerErrorMessageDelay = 10000; // 10 seconds
    private static long _nextMaximumSocketsReachedMessage;

    // AccountLoginReject BadComm
    private static readonly byte[] _socketRejected = { 0x82, 0xFF };

    public static IPEndPoint[] ListeningAddresses { get; private set; }
    public static Socket[] Listeners { get; private set; }
    public static HashSet<NetState> Instances { get; } = new(2048);

    private static readonly ConcurrentQueue<NetState> _connectedQueue = new();

    public static void Configure()
    {
        MaxConnections = ServerConfiguration.GetOrUpdateSetting("tcpServer.maxConnections", 4096);
    }

    public static void Start()
    {
        HashSet<IPEndPoint> listeningAddresses = new HashSet<IPEndPoint>();
        List<Socket> listeners = new List<Socket>();

        foreach (var ipep in ServerConfiguration.Listeners)
        {
            var listener = CreateListener(ipep);
            if (listener == null)
            {
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
            BeginAcceptingSockets(listener);
        }

        foreach (var ipep in listeningAddresses)
        {
            logger.Information("Listening: {Address}:{Port}", ipep.Address, ipep.Port);
        }

        ListeningAddresses = listeningAddresses.ToArray();
        Listeners = listeners.ToArray();
    }

    public static void Shutdown()
    {
        foreach (var listener in Listeners)
        {
            listener.Close();
        }
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
            listener.Listen(32);
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

    public static void Slice()
    {
        int count = 0;

        while (++count <= MaxConnectionsPerLoop && _connectedQueue.TryDequeue(out var ns))
        {
            Instances.Add(ns);
            ns.LogInfo($"Connected. [{Instances.Count} Online]");
        }
    }

    private static async void BeginAcceptingSockets(Socket listener)
    {
        while (true)
        {
            try
            {
                var socket = await listener.AcceptAsync();

                var rejected = false;
                if (Instances.Count >= MaxConnections)
                {
                    rejected = true;

                    var ticks = Core.TickCount;

                    if (ticks - _nextMaximumSocketsReachedMessage > 0)
                    {
                        if (socket.RemoteEndPoint is IPEndPoint ipep)
                        {
                            var ip = ipep.Address.ToString();
                            logger.Warning("Listener {Address}: Failed (Maximum connections reached)", ip);
                            NetState.TraceDisconnect("Maximum connections reached.", ip);
                        }

                        _nextMaximumSocketsReachedMessage = ticks + _listenerErrorMessageDelay;
                    }
                }

                var args = new SocketConnectEventArgs(socket);
                EventSink.InvokeSocketConnect(args);

                if (!args.AllowConnection)
                {
                    rejected = true;
                    if (socket.RemoteEndPoint is IPEndPoint ipep)
                    {
                        var ip = ipep.Address.ToString();
                        NetState.TraceDisconnect("Rejected by socket event handler", ip);
                    }
                }

                if (rejected)
                {
                    socket.Send(_socketRejected, SocketFlags.None);
                    socket.Shutdown(SocketShutdown.Both);
                    socket.Close();
                }
                else
                {
                    var ns = new NetState(socket);
                    _connectedQueue.Enqueue(ns);
                }
            }
            catch
            {
                // ignored
            }
        }
    }
}
