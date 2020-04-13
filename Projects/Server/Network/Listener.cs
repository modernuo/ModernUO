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

namespace Server.Network
{
  public class Listener
  {
    // private void DisplayListener()
    // {
    //   if (!(m_Listener.EndPoint is IPEndPoint ipep))
    //     return;
    //
    //   if (ipep.Address.Equals(IPAddress.Any) || ipep.Address.Equals(IPAddress.IPv6Any))
    //   {
    //     NetworkInterface[] adapters = NetworkInterface.GetAllNetworkInterfaces();
    //     foreach (NetworkInterface adapter in adapters)
    //     {
    //       IPInterfaceProperties properties = adapter.GetIPProperties();
    //       foreach (UnicastIPAddressInformation unicast in properties.UnicastAddresses)
    //         if (ipep.AddressFamily == unicast.Address.AddressFamily)
    //           Console.WriteLine("Listening: {0}:{1}", unicast.Address, ipep.Port);
    //     }
    //   }
    //   else
    //     Console.WriteLine("Listening: {0}:{1}", ipep.Address, ipep.Port);
    // }
  }
}
