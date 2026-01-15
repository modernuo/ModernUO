/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2024 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: PingServer.cs                                                   *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using Server.Logging;

namespace Server.Network;

public static class PingServer
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(PingServer));

    public static int MaxQueued { get; set; }

    public static UdpClient[] Listeners { get; private set; }

    public static bool Enabled { get; private set; }

    public static int Port { get; private set; }

    public static void Configure()
    {
        Enabled = ServerConfiguration.GetOrUpdateSetting("pingServer.enabled", true);
        Port = ServerConfiguration.GetSetting("pingServer.port", 12000);
        MaxQueued = ServerConfiguration.GetSetting("pingServer.maxConnections", 2048);
    }

    public static void Start()
    {
        if (!Enabled)
        {
            return;
        }

        HashSet<IPEndPoint> listeningAddresses = [];
        List<UdpClient> listeners = [];

        foreach (var serverIpep in ServerConfiguration.Listeners)
        {
            var ipep = new IPEndPoint(serverIpep.Address, Port);

            var listener = CreateListener(ipep);
            if (listener == null)
            {
                continue;
            }

            if (ipep.Address.Equals(IPAddress.Any) || ipep.Address.Equals(IPAddress.IPv6Any))
            {
                listeningAddresses.UnionWith(NetState.GetListeningAddresses(ipep));
            }
            else
            {
                listeningAddresses.Add(ipep);
            }

            listeners.Add(listener);
            BeginAcceptingUdpRequest(listener);
        }

        foreach (var ipep in listeningAddresses)
        {
            logger.Information("Listening: {Address}:{Port} (Pings)", ipep.Address, ipep.Port);
        }

        Listeners = listeners.ToArray();
    }

    public static UdpClient CreateListener(IPEndPoint ipep)
    {
        var listener = new Socket(ipep.AddressFamily, SocketType.Dgram, ProtocolType.Udp)
        {
            ExclusiveAddressUse = false
        };

        try
        {
            listener.Bind(ipep);

            return new UdpClient
            {
                Client = listener
            };
        }
        catch (SocketException se)
        {
            // WSAEADDRINUSE
            if (se.ErrorCode == 10048)
            {
                logger.Warning("Ping Listener: {Address}:{Port}: Failed (In Use)", ipep.Address, ipep.Port);
            }
            // WSAEADDRNOTAVAIL
            else if (se.ErrorCode == 10049)
            {
                logger.Warning("Ping Listener {Address}:{Port}: Failed (Unavailable)", ipep.Address, ipep.Port);
            }
            else
            {
                logger.Warning(se, "Ping Listener Exception:");
            }
        }

        return null;
    }

    private static async void BeginAcceptingUdpRequest(object state)
    {
        if (state is not UdpClient listener)
        {
            return;
        }

        var cancellationToken = Core.ClosingTokenSource.Token;

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var result = await listener.ReceiveAsync(cancellationToken);
                await listener.SendAsync(result.Buffer, result.RemoteEndPoint, cancellationToken);
            }
            catch
            {
                // ignored
            }
        }
    }

    public static void Shutdown()
    {
        foreach (var listener in Listeners)
        {
            listener.Close();
        }
    }
}
