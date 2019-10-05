using System.Net;
using System.Net.Sockets;

namespace Server
{
  public class SocketOptions
  {
    private const bool NagleEnabled = false; // Should the Nagle algorithm be enabled? This may reduce performance

    private static IPEndPoint[] m_ListenerEndPoints =
    {
      new IPEndPoint(IPAddress.Any, 2593) // Default: Listen on port 2593 on all IP addresses

      // Examples:
      // new IPEndPoint( IPAddress.Any, 80 ), // Listen on port 80 on all IP addresses
      // new IPEndPoint( IPAddress.Parse( "1.2.3.4" ), 2593 ), // Listen on port 2593 on IP address 1.2.3.4
    };

    public static void Initialize()
    {
      EventSink.SocketConnect += EventSink_SocketConnect;
    }

    public static void RegisterListeners()
    {
      for (int i = 0; i < m_ListenerEndPoints.Length; i++)
        Core.MessagePump.AddListener(m_ListenerEndPoints[i]);
    }

    private static void EventSink_SocketConnect(SocketConnectEventArgs e)
    {
      if (!e.AllowConnection)
        return;

      if (!NagleEnabled)
        e.Socket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.NoDelay, 1); // RunUO uses its own algorithm
    }
  }
}
