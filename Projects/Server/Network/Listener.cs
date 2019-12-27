/***************************************************************************
 *                                Listener.cs
 *                            -------------------
 *   begin                : May 1, 2002
 *   copyright            : (C) The RunUO Software Team
 *   email                : info@runuo.com
 *
 *   $Id$
 *
 ***************************************************************************/

/***************************************************************************
 *
 *   This program is free software; you can redistribute it and/or modify
 *   it under the terms of the GNU General Public License as published by
 *   the Free Software Foundation; either version 2 of the License, or
 *   (at your option) any later version.
 *
 ***************************************************************************/

using System;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Libuv;
using Libuv.Internal;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;

namespace Server.Network
{
  public class Listener
  {
    private static readonly LibuvFunctions functions = new LibuvFunctions();

    private readonly IPEndPoint m_EndPoint;
    private LibuvConnectionListener m_Listener;

    public Listener(IPEndPoint ipep)
    {
      m_EndPoint = ipep;
      LibuvTransportContext transport = new LibuvTransportContext
      {
        Options = new LibuvTransportOptions(),
        AppLifetime = new ApplicationLifetime(
          LoggerFactory.Create(builder => { builder.AddConsole(); }).CreateLogger<ApplicationLifetime>()
        ),
        Log = new LibuvTrace(LoggerFactory.Create(builder => { builder.AddConsole(); }).CreateLogger("network"))
      };

      m_Listener = new LibuvConnectionListener(functions, transport, ipep);
    }

    public virtual async Task Start(MessagePump pump)
    {
      try
      {
        await m_Listener.BindAsync();
      }
      catch (AddressInUseException)
      {
        Console.WriteLine("Listener Failed: {0}:{1} (In Use)", m_EndPoint.Address, m_EndPoint.Port);
        m_Listener = null;
        return;
      }
      catch (Exception e)
      {
        Console.WriteLine("Listener Exception:");
        Console.WriteLine(e);

        m_Listener = null;
        return;
      }

      DisplayListener();

      while (true)
      {
        ConnectionContext context;
        try
        {
          context = await m_Listener.AcceptAsync();
        }
        catch (SocketException ex)
        {
          NetState.TraceException(ex);
          continue;
        }

        if (VerifySocket(context))
          _ = new NetState(context, pump);
        else
          Release(context);
      }
    }

    private void DisplayListener()
    {
      if (!(m_Listener.EndPoint is IPEndPoint ipep))
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
        Console.WriteLine("Listening: {0}:{1}", ipep.Address, ipep.Port);
    }

    private static bool VerifySocket(ConnectionContext context)
    {
      try
      {
        SocketConnectEventArgs args = new SocketConnectEventArgs(context);

        EventSink.InvokeSocketConnect(args);

        return args.AllowConnection;
      }
      catch (Exception ex)
      {
        NetState.TraceException(ex);
        return false;
      }
    }

    private static void Release(ConnectionContext context)
    {
      try
      {
        context.Abort(new ConnectionAbortedException("Failed socket verification."));
      }
      catch (Exception ex)
      {
        NetState.TraceException(ex);
      }

      try
      {
        // TODO: Is this needed?
        context.DisposeAsync();
      }
      catch (Exception ex)
      {
        NetState.TraceException(ex);
      }
    }

    public async Task Dispose()
    {
      LibuvConnectionListener listener = Interlocked.Exchange(ref m_Listener, null);
      if (listener != null)
        try
        {
          await listener.UnbindAsync();
          await listener.DisposeAsync();
        }
        catch (Exception ex)
        {
          Console.WriteLine("Listener: Failed to dispose.");
          Console.WriteLine(ex);
        }

      GC.SuppressFinalize(this);
    }
  }
}
