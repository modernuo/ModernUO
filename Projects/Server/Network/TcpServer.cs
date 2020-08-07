/*************************************************************************
 * ModernUO                                                              *
 * Copyright (C) 2019-2020 - ModernUO Development Team                   *
 * Email: hi@modernuo.com                                                *
 * File: TcpServer.cs                                                    *
 * Created: 2020/04/12 - Updated: 2020/08/06                             *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * This program is distributed in the hope that it will be useful,       *
 * but WITHOUT ANY WARRANTY; without even the implied warranty of        *
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the         *
 * GNU General Public License for more details.                          *
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

namespace Server.Network
{
  public static class TcpServer
  {
    public static IPEndPoint[] ListeningAddresses { get; private set; }
    public static TcpListener[] Listeners { get; private set; }
    public static List<NetState> ConnectedClients { get; } = new List<NetState>(128);

    public static void Start()
    {
      HashSet<IPEndPoint> listeningAddresses = new HashSet<IPEndPoint>();
      List<TcpListener> listeners = new List<TcpListener>();

      foreach (var ipep in ServerConfiguration.Listeners)
      {
        var listener = CreateListener(ipep);
        if (listener == null) continue;

        if (ipep.Address.Equals(IPAddress.Any) || ipep.Address.Equals(IPAddress.IPv6Any))
          listeningAddresses.UnionWith(GetListeningAddresses(ipep));
        else
          listeningAddresses.Add(ipep);

        listeners.Add(listener);
        listener.BeginAcceptingSockets();
      }

      foreach (var ipep in listeningAddresses)
        Console.WriteLine("Listening: {0}:{1}", ipep.Address, ipep.Port);

      ListeningAddresses = listeningAddresses.ToArray();
      Listeners = listeners.ToArray();
    }

    public static IEnumerable<IPEndPoint> GetListeningAddresses(IPEndPoint ipep) =>
      NetworkInterface.GetAllNetworkInterfaces().SelectMany(adapter =>
        adapter.GetIPProperties().UnicastAddresses
          .Where(uip => ipep.AddressFamily == uip.Address.AddressFamily)
          .Select(uip => new IPEndPoint(uip.Address, ipep.Port))
      );

    public static TcpListener CreateListener(IPEndPoint ipep)
    {
      var listener = new TcpListener(ipep);
      listener.Server.ExclusiveAddressUse = false;

      try
      {
        listener.Start(8);
        return listener;
      }
      catch (SocketException se)
      {
        // WSAEADDRINUSE
        if (se.ErrorCode == 10048)
        {
          Console.WriteLine("Listener Failed: {0}:{1} (In Use)", ipep.Address, ipep.Port);
        }
        // WSAEADDRNOTAVAIL
        else if (se.ErrorCode == 10049)
        {
          Console.WriteLine("Listener Failed: {0}:{1} (Unavailable)", ipep.Address, ipep.Port);
        }
        else
        {
          Console.WriteLine("Listener Exception:");
          Console.WriteLine(se);
        }
      }

      return null;
    }

    private static async void BeginAcceptingSockets(this TcpListener listener)
    {
      while (true)
      {
        var socket = await listener.AcceptSocketAsync();

        NetState ns = new NetState(socket);
        ConnectedClients.Add(ns);
        ns.Start();

        if (ns.Running)
          Console.WriteLine("Client: {0}: Connected. [{1} Online]", ns, ConnectedClients.Count);
      }
    }
  }
}
