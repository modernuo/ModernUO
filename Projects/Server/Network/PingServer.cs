using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using Server.Logging;

namespace Server.Network;

public static class PingServer
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(PingServer));

    private const int MaxConnectionsPerLoop = 250;

    private const long _listenerErrorMessageDelay = 10000; // 10 seconds
    private static long _nextMaximumSocketsReachedMessage;

    public static int MaxConnections { get; set; }

    private static ConcurrentQueue<(UdpClient, UdpReceiveResult)> _udpResponseQueue = new();

    public static UdpClient[] Listeners { get; private set; }

    public static bool Enabled { get; private set; }

    public static int Port { get; private set; }

    public static void Configure()
    {
        Enabled = ServerConfiguration.GetOrUpdateSetting("pingServer.enabled", true);
        Port = ServerConfiguration.GetSetting("pingServer.port", 12000);
        MaxConnections = ServerConfiguration.GetSetting("pingServer.maxConnections", 2048);
    }

    public static void Start()
    {
        if (!Enabled)
        {
            return;
        }

        HashSet<IPEndPoint> listeningAddresses = new HashSet<IPEndPoint>();
        List<UdpClient> listeners = new List<UdpClient>();

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
                listeningAddresses.UnionWith(TcpServer.GetListeningAddresses(ipep));
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
            logger.Information("Listening: {Address}:{Port} (Pings)", ipep.Address, ipep.Port);
        }

        Listeners = listeners.ToArray();
    }

    public static void Slice()
    {
        if (!Enabled)
        {
            return;
        }

        int count = 0;

        while (++count <= MaxConnectionsPerLoop && _udpResponseQueue.TryDequeue(out var udpTuple))
        {
            var (listener, result) = udpTuple;
            SendResponse(listener, result.Buffer, result.RemoteEndPoint);
        }
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

    private static async void BeginAcceptingSockets(UdpClient listener)
    {
        while (true)
        {
            if (!Enabled || Core.Closing)
            {
                return;
            }

            try
            {
                var result = await listener.ReceiveAsync(Core.ClosingTokenSource.Token);

                if (_udpResponseQueue.Count >= MaxConnections)
                {
                    var ticks = Core.TickCount;

                    if (ticks - _nextMaximumSocketsReachedMessage > 0)
                    {
                        if (listener.Client.RemoteEndPoint is IPEndPoint ipep)
                        {
                            var ip = ipep.Address.ToString();
                            logger.Warning("Ping Listener {Address}: Failed (Maximum connections reached)", ip);
                        }

                        _nextMaximumSocketsReachedMessage = ticks + _listenerErrorMessageDelay;
                    }
                }

                _udpResponseQueue.Enqueue((listener, result));
            }
            catch
            {
                // ignored
            }
        }
    }

    private static async void SendResponse(UdpClient listener, byte[] data, IPEndPoint ipep)
    {
        try
        {
            await listener.SendAsync(data, ipep, Core.ClosingTokenSource.Token);
        }
        catch
        {
            // ignored
        }
    }
}
