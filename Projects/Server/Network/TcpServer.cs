/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2024 - ModernUO Development Team                       *
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

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Threading;
using Server.Logging;
using Server.Misc;

namespace Server.Network;

public static class TcpServer
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(TcpServer));

    private const long ListenerErrorMessageDelay = 10000; // 10 seconds

    private static long _nextMaximumSocketsReachedMessage;
    private static readonly SemaphoreSlim _queueSemaphore = new(0);
    private static readonly ConcurrentQueue<Socket> _connectingQueue = [];
    private static Thread _processConnectionsThread;

    // Sanity. 256 * 1024 * 4096 = ~1.3GB of ram
    public static int MaxConnections { get; set; }

    public static IPEndPoint[] ListeningAddresses { get; private set; }
    public static Socket[] Listeners { get; private set; }

    public static ConcurrentQueue<NetState> ConnectedQueue { get; } = [];

    public static void Configure()
    {
        MaxConnections = ServerConfiguration.GetOrUpdateSetting("tcpServer.maxConnections", 4096);
    }

    public static void Start()
    {
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
            listener.Listen(256);
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

    private static async void BeginAcceptingSockets(object state)
    {
        if (state is not Socket listener)
        {
            return;
        }

        var cancellationToken = Core.ClosingTokenSource.Token;

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var socket = await listener.AcceptAsync(cancellationToken);
                _connectingQueue.Enqueue(socket);
                _queueSemaphore.Release();
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch
            {
                // ignored
            }
        }

        listener.Close();
    }

    private static void ProcessConnections()
    {
        var cancellationToken = Core.ClosingTokenSource.Token;
        HashSet<IPEndPoint> listeningAddresses = [];
        List<Socket> listeners = [];

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

                new Thread(BeginAcceptingSockets).Start(listener);
            }
        }

        foreach (var ipep in listeningAddresses)
        {
            logger.Information("Listening: {Address}:{Port}", ipep.Address, ipep.Port);
        }

        ListeningAddresses = listeningAddresses.ToArray();
        Listeners = listeners.ToArray();

        while (true)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    _queueSemaphore.Wait(cancellationToken);

                    Firewall.ProcessQueue();

                    if (_connectingQueue.TryDequeue(out var socket))
                    {
                        ProcessConnection(socket);
                    }
                }

                return;
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (Exception e)
            {
                logger.Error(e, "Error occurred in ProcessConnections");
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void CloseSocket(Socket socket)
    {
        try
        {
            socket.Shutdown(SocketShutdown.Both);
        }
        catch
        {
            // ignored
        }

        socket.Close();
    }

    private static void ProcessConnection(Socket socket)
    {
        try
        {
            var remoteIP = ((IPEndPoint)socket.RemoteEndPoint)!.Address;

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

                CloseSocket(socket);
                return;
            }

            var firewalled = Firewall.IsBlocked(remoteIP);
            if (!firewalled)
            {
                var socketConnectedArgs = new SocketConnectedEventArgs(socket);
                EventSink.InvokeSocketConnected(socketConnectedArgs);
                firewalled = !socketConnectedArgs.ConnectionAllowed;
            }

            if (firewalled)
            {
                TraceDisconnect("Firewalled", remoteIP);
                logger.Debug("{Address} Firewalled", remoteIP);

                CloseSocket(socket);
                return;
            }

            if (!IPLimiter.Verify(remoteIP))
            {
                TraceDisconnect("Past IP limit threshold", remoteIP);
                logger.Debug("{Address} Past IP limit threshold", remoteIP);

                CloseSocket(socket);
                return;
            }

            var ns = new NetState(socket);
            ConnectedQueue.Enqueue(ns);
        }
        catch
        {
            // ignored
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

    public static class EventSink
    {
        // IMPORTANT: This is executed asynchronously! Do not run any game thread code on these delegates!
        public static event Action<SocketConnectedEventArgs> SocketConnected;

        internal static void InvokeSocketConnected(SocketConnectedEventArgs context) =>
            SocketConnected?.Invoke(context);
    }

    public class SocketConnectedEventArgs
    {
        public Socket Socket { get; }

        public bool ConnectionAllowed { get; set; } = true;

        internal SocketConnectedEventArgs(Socket socket) => Socket = socket;
    }
}
