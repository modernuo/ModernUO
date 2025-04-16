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
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Server.Logging;

namespace Server.Network;

public static class TcpServer
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(TcpServer));

    // AccountLoginReject BadComm
    private static readonly byte[] _socketRejected = [0x82, 0xFF];

    public static IPEndPoint[] ListeningAddresses { get; private set; }
    public static Socket[] Listeners { get; private set; }

    private static IPRateLimiter _ipRateLimiter;

    public static void Start()
    {
        _ipRateLimiter = new IPRateLimiter(10, 10000, 1000, 2.0, 3_600_000, Core.ClosingTokenSource.Token);
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
            logger.Information("Listening: {Address}", ipep);
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
            listener.Listen(256);
            return listener;
        }
        catch (SocketException se)
        {
            // WSAEADDRINUSE
            if (se.ErrorCode == 10048)
            {
                logger.Warning("Listener: {Address} Exception: {Reason}", ipep, "Currently in use");
            }
            // WSAEADDRNOTAVAIL
            else if (se.ErrorCode == 10049)
            {
                logger.Warning("Listener {Address} Exception: {Reason}", ipep, "Unavailable");
            }
            else
            {
                logger.Warning(se, "Listener {Address} Exception: {Reason}", ipep, se.Message);
            }
        }

        return null;
    }

    private static async ValueTask BeginAcceptingSockets(Socket listener)
    {
        while (!Core.Closing)
        {
            try
            {
                var socket = await listener.AcceptAsync(Core.ClosingTokenSource.Token);
                var remoteIP = ((IPEndPoint)socket.RemoteEndPoint)!.Address;

                if (!_ipRateLimiter.Verify(remoteIP, out var totalAttempts))
                {
                    logger.Debug("{Address} Past IP limit threshold ({TotalAttempts})", remoteIP, totalAttempts);
                }
                else if (Firewall.IsBlocked(remoteIP))
                {
                    logger.Debug("{Address} Firewalled", remoteIP);
                }
                else
                {
                    _ = Task.Run(() => ProcessSocketConnection(socket), Core.ClosingTokenSource.Token);
                }
            }
            catch
            {
                // ignored
            }
        }
    }

    [ThreadStatic]
    private static byte[] _firstBytes;

    private static async ValueTask ProcessSocketConnection(Socket socket)
    {
        _firstBytes ??= GC.AllocateUninitializedArray<byte>(128);

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(Core.ClosingTokenSource.Token);
        cts.CancelAfter(TimeSpan.FromMilliseconds(500));

        try
        {
            var bytesRead = await socket.ReceiveAsync(_firstBytes, SocketFlags.Peek, cts.Token);

            var isValid =
                // Sometimes when newer clients are connecting to the game server the first 4 bytes are sent separately
                bytesRead == 4 ||
                // Older clients only send the 4 byte seed first then 0x80
                (UOClient.MinRequired == null || UOClient.MinRequired < ClientVersion.Version6050) &&
                bytesRead >= 66 && _firstBytes[4] == 0x80 ||
                // Newer clients
                (UOClient.MaxRequired == null || UOClient.MaxRequired >= ClientVersion.Version6050) && (
                    // Account Login - 0xEF + 0x80 (83 bytes)
                    bytesRead >= 83 && _firstBytes[0] == 0xEF && _firstBytes[21] == 0x80 ||
                    bytesRead == 21 && _firstBytes[0] == 0xEF ||
                    // Game Login - 4 bytes + 0x91 (69 bytes)
                    bytesRead >= 69 && _firstBytes[4] == 0x91
                );

            // TODO: Validate client version is v4 -> v7 for 0xEF packet
            // TODO: Validate Account Login seed matches Game Login seed
            // TODO: Validate AuthId for 0x91 packet
            // TODO: Validate username is ascii and not empty
            // TODO: Validate password is ascii and not empty
            if (isValid)
            {
                var args = new SocketConnectEventArgs(socket);
                EventSink.InvokeSocketConnect(args);

                if (args.AllowConnection)
                {
                    Core.LoopContext.Post(() => _ = new NetState(socket), EventLoopContext.Priority.High);
                    return;
                }

                logger.Debug("{Address} Rejected by socket handler", ((IPEndPoint)socket.RemoteEndPoint)!.Address);

                cts.TryReset();
                cts.CancelAfter(TimeSpan.FromMilliseconds(500));
                await socket.SendAsync(_socketRejected, SocketFlags.None, cts.Token);
                CloseSocket(socket);
            }
            else
            {
                ForceCloseSocket(socket);
            }
        }
        catch
        {
            ForceCloseSocket(socket);
        }
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
            socket.Disconnect(false);
        }
        finally
        {
            socket.Close(0);
        }
    }
}
