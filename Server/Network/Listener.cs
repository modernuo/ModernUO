using System;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Server.Network
{
  public class Listener
  {
    private Socket m_Socket;
    private IPEndPoint m_EndPoint;
    public Listener(IPEndPoint ipep)
    {
#pragma warning disable IDE0068 // Use recommended dispose pattern
      m_Socket = new Socket(ipep.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
#pragma warning restore IDE0068 // Use recommended dispose pattern

      m_Socket.LingerState.Enabled = false;
      m_Socket.ExclusiveAddressUse = false;
      m_EndPoint = ipep;
    }

    public virtual async Task Start(MessagePump pump)
    {
      try
      {
        m_Socket.Bind(m_EndPoint);
        m_Socket.Listen(8);
      }
      catch (Exception e)
      {
        if (e is SocketException se)
        {
          if (se.ErrorCode == 10048)
          {
            // WSAEADDRINUSE
            Console.WriteLine("Listener Failed: {0}:{1} (In Use)", m_EndPoint.Address, m_EndPoint.Port);
          }
          else if (se.ErrorCode == 10049)
          {
            // WSAEADDRNOTAVAIL
            Console.WriteLine("Listener Failed: {0}:{1} (Unavailable)", m_EndPoint.Address, m_EndPoint.Port);
          }
          else
          {
            Console.WriteLine("Listener Exception:");
            Console.WriteLine(e);
          }
        }

        m_Socket = null;
        return;
      }

      DisplayListener();

      while (true)
      {
        Socket s;
        try
        {
          s = await m_Socket.AcceptAsync();
        }
        catch (SocketException ex)
        {
          NetState.TraceException(ex);
          continue;
        }

        if (VerifySocket(s))
          new NetState(s, pump);
        else
          Release(s);
      }
    }

    private void DisplayListener()
    {
      if (!(m_Socket.LocalEndPoint is IPEndPoint ipep))
        return;

      if (ipep.Address.Equals(IPAddress.Any) || ipep.Address.Equals(IPAddress.IPv6Any))
      {
        NetworkInterface[] adapters = NetworkInterface.GetAllNetworkInterfaces();
        foreach (NetworkInterface adapter in adapters)
        {
          IPInterfaceProperties properties = adapter.GetIPProperties();
          foreach (UnicastIPAddressInformation unicast in properties.UnicastAddresses)
            if (ipep.AddressFamily == unicast.Address.AddressFamily)
              Console.WriteLine("Listening: {0}:{1}", unicast.Address, ipep.Port);
        }
      }
      else
      {
        Console.WriteLine("Listening: {0}:{1}", ipep.Address, ipep.Port);
      }
    }

    private bool VerifySocket(Socket socket)
    {
      try
      {
        SocketConnectEventArgs args = new SocketConnectEventArgs(socket);

        EventSink.InvokeSocketConnect(args);

        return args.AllowConnection;
      }
      catch (Exception ex)
      {
        NetState.TraceException(ex);

        return false;
      }
    }

    private void Release(Socket socket)
    {
      try
      {
        socket.Shutdown(SocketShutdown.Both);
      }
      catch (SocketException ex)
      {
        NetState.TraceException(ex);
      }

      try
      {
        socket.Close();
      }
      catch (SocketException ex)
      {
        NetState.TraceException(ex);
      }

      try
      {
        socket.Dispose();
      }
      catch (SocketException ex)
      {
        NetState.TraceException(ex);
      }
    }

    public void Dispose()
    {
      Interlocked.Exchange<Socket>(ref m_Socket, null)?.Close();
      GC.SuppressFinalize(this);
    }
  }
}
