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
using System.Threading.Tasks;
using Server.Logging;
using Server.Misc;

namespace Server.Network;

public static class TcpServer
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(TcpServer));

    // AccountLoginReject BadComm
    private static readonly byte[] _socketRejected = [0x82, 0xFF];

    private static readonly AutoResetEvent _startValidating = new(false);
    private static readonly ConcurrentQueue<Socket> _validatingSocket = [];
    private static readonly ConcurrentQueue<Socket> _validatedSocket = [];

    public static IPEndPoint[] ListeningAddresses { get; private set; }
    public static Socket[] Listeners { get; private set; }

    public static void Initialize()
    {
        // Reset validating connections when the world is done saving
        EventSink.WorldSave += () => _startValidating.Set();
    }

    public static void Start()
    {
        HashSet<IPEndPoint> listeningAddresses = [];
        List<Socket> listeners = [];
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

        var validatorThread = new Thread(ValidateSockets);
        validatorThread.Start();
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

    private static async void ValidateSockets()
    {
        List<Task<Socket>> receiveTasks = new List<Task<Socket>>(1024);
        while (_startValidating.WaitOne())
        {
            if (!World.Running || World.Loading)
            {
                continue;
            }

            // TODO: Max count
            while (_validatingSocket.TryDequeue(out var socket))
            {
                receiveTasks.Add(ValidateSocketAsync(socket));
            }

            if (receiveTasks.Count == 0)
            {
                continue;
            }

            var sockets = await Task.WhenAll(receiveTasks);

            for (int i = 0; i < sockets.Length; i++)
            {
                var socket = sockets[i];
                if (socket != null)
                {
                    _validatedSocket.Enqueue(socket);
                }
            }

            receiveTasks.Clear();

            if (Core.Closing)
            {
                return;
            }
        }
    }

    private static async Task<Socket> ValidateSocketAsync(Socket socket)
    {
        var firstBytes = new byte[5];
        try
        {
            await socket.ReceiveAsync(firstBytes, SocketFlags.Peek).WaitAsync(TimeSpan.FromMilliseconds(20));

            // Validates the first 5 bytes of the socket are valid
            var passed = firstBytes[0] == 0xEF || firstBytes[5] == 0x91;

            if (!passed)
            {
                ForceCloseSocket(socket);
                return null;
            }

            return socket;
        }
        catch
        {
            ForceCloseSocket(socket);
            return null;
        }
    }

    private static async void BeginAcceptingSockets(Socket listener)
    {
        var cancellationToken = Core.ClosingTokenSource.Token;
        Socket socket = null;
        while (!Core.Closing)
        {
            try
            {
                socket = await listener.AcceptAsync(cancellationToken);
                var remoteIP = ((IPEndPoint)socket.RemoteEndPoint)!.Address;

                if (!IPLimiter.Verify(remoteIP))
                {
                    TraceDisconnect("Past IP limit threshold", remoteIP);
                    logger.Debug("{Address} Past IP limit threshold", remoteIP);
                    ForceCloseSocket(socket);
                    continue;
                }

                if (Firewall.IsBlocked(remoteIP))
                {
                    TraceDisconnect("Firewalled", remoteIP);
                    logger.Debug("{Address} Firewalled", remoteIP);
                    ForceCloseSocket(socket);
                    continue;
                }

                _validatingSocket.Enqueue(socket);
                _startValidating.Set();
            }
            catch
            {
                CloseSocket(socket);
            }

            while (_validatedSocket.TryDequeue(out var validatedSocket))
            {
                var args = new SocketConnectEventArgs(validatedSocket);
                EventSink.InvokeSocketConnect(args);

                if (!args.AllowConnection)
                {
                    TraceDisconnect(
                        "Rejected by socket event handler",
                        ((IPEndPoint)validatedSocket.RemoteEndPoint)!.Address
                    );

                    // Reject the connection
                    validatedSocket.Send(_socketRejected, SocketFlags.None);
                    CloseSocket(validatedSocket);
                    continue;
                }

                _ = new NetState(validatedSocket);
            }
        }

        // Server is closing, trigger the last validation so that can close too.
        _startValidating.Set();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void CloseSocket(Socket socket)
    {
        try
        {
            socket.Shutdown(SocketShutdown.Both);
        }
        finally
        {
            socket.Close();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void ForceCloseSocket(Socket socket)
    {
        try
        {
            socket.Shutdown(SocketShutdown.Both);
        }
        finally
        {
            socket.Close(0);
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
