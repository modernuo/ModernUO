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

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using Server.Logging;
using Server.Misc;

namespace Server.Network;

public static class TcpServer
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(TcpServer));

    // AccountLoginReject BadComm
    private static readonly byte[] _socketRejected = [0x82, 0xFF];

    private static Dictionary<string, List<string>> countryCidrRanges = new Dictionary<string, List<string>>();

    static TcpServer()
    {
        LoadCidrRanges("ip-ranges.txt");
    }

    public static IPEndPoint[] ListeningAddresses { get; private set; }
    public static Socket[] Listeners { get; private set; }

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

    private static async void BeginAcceptingSockets(Socket listener)
    {
        while (!Core.Closing)
        {
            Socket socket = null;
            try
            {
                socket = await listener.AcceptAsync();
                var remoteIP = ((IPEndPoint)socket.RemoteEndPoint)!.Address;

                if (!IPLimiter.Verify(remoteIP))
                {
                    TraceDisconnect("Past IP limit threshold", remoteIP);
                    logger.Debug("{Address} Past IP limit threshold", remoteIP);
                }
                else if (Firewall.IsBlocked(remoteIP))
                {
                    TraceDisconnect("Firewalled", remoteIP);
                    logger.Debug("{Address} Firewalled", remoteIP);
                }
                else
                {
                    var args = new SocketConnectEventArgs(socket);
                    EventSink.InvokeSocketConnect(args);

                    if (args.AllowConnection)
                    {
                        _ = new NetState(socket);
                        continue;
                    }

                    TraceDisconnect("Rejected by socket event handler", remoteIP);

                    // Reject the connection
                    socket.Send(_socketRejected, SocketFlags.None);
                }

                CloseSocket(socket);
            }
            catch
            {
                CloseSocket(socket);
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
        finally
        {
            socket.Close();
        }
    }

    private static void TraceDisconnect(string reason, IPAddress ip)
    {
        try
        {
            using StreamWriter op = new StreamWriter("network-socket-disconnects.log", true);
            op.WriteLine($"# {Core.Now}");

            op.WriteLine($"Address: {ip}");
            var country = GetCountryForIp(ip);

            if (country != null)
            {
                op.WriteLine($"Country: {country}");
            }
            
            op.WriteLine(reason);

            op.WriteLine();
            op.WriteLine();
        }
        catch
        {
            // ignored
        }
    }

    private static void LoadCidrRanges(string filePath)
    {
        foreach (var line in File.ReadLines(filePath))
        {
            var parts = line.Split(' ');
            if (parts.Length == 2)
            {
                var country = parts[0];
                var cidr = parts[1];
                if (!countryCidrRanges.ContainsKey(country))
                {
                    countryCidrRanges[country] = new List<string>();
                }
                countryCidrRanges[country].Add(cidr);
            }
        }
    }

    private static bool IsIpInCidrRange(string cidr, IPAddress ip)
    {
        var parts = cidr.Split('/');
        var baseAddress = IPAddress.Parse(parts[0]);
        var prefixLength = int.Parse(parts[1]);

        var ipBytes = ip.GetAddressBytes();
        var baseAddressBytes = baseAddress.GetAddressBytes();

        var byteCount = prefixLength / 8;
        var bitCount = prefixLength % 8;

        for (int i = 0; i < byteCount; i++)
        {
            if (ipBytes[i] != baseAddressBytes[i])
            {
                return false;
            }
        }

        if (bitCount > 0)
        {
            int mask = 0xFF << (8 - bitCount);
            if ((ipBytes[byteCount] & mask) != (baseAddressBytes[byteCount] & mask))
            {
                return false;
            }
        }

        return true;
    }

    private static string GetCountryForIp(IPAddress ip)
    {
        foreach (var kvp in countryCidrRanges)
        {
            var country = kvp.Key;
            var cidrRanges = kvp.Value;
            foreach (var cidr in cidrRanges)
            {
                if (IsIpInCidrRange(cidr, ip))
                {
                    return country;
                }
            }
        }
        return null;
    }
}
