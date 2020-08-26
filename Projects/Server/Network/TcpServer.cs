/*************************************************************************
 * ModernUO                                                              *
 * Copyright (C) 2019-2020 - ModernUO Development Team                   *
 * Email: hi@modernuo.com                                                *
 * File: TcpServer.cs                                                    *
 * Created: 2020/04/12 - Updated: 2020/04/12                             *
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
using System.Net;
using System.Net.NetworkInformation;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Server.Network
{
    public static class TcpServer
    {
        private static IPAddress[] m_ListeningAddresses;

        // Make this thread safe
        public static List<NetState> Instances { get; } = new List<NetState>();

        public static IWebHostBuilder CreateWebHostBuilder(string[] args = null) =>
            WebHost.CreateDefaultBuilder(args)
                .UseSetting(WebHostDefaults.SuppressStatusMessagesKey, "True")
                .ConfigureServices(services => { services.AddSingleton<IMessagePumpService>(new MessagePumpService()); })
                .UseKestrel(
                    options =>
                    {
                        foreach (var ipep in ServerConfiguration.Listeners)
                        {
                            options.Listen(ipep, builder => { builder.UseConnectionHandler<ServerConnectionHandler>(); });
                            m_ListeningAddresses = GetListeningAddresses(ipep);
                            DisplayListener(ipep);
                        }

                        // Webservices here
                    }
                )
                .UseLibuv()
                .UseStartup<ServerStartup>();

        public static IPAddress[] GetListeningAddresses(IPEndPoint ipep)
        {
            if (m_ListeningAddresses != null)
                return m_ListeningAddresses;

            var list = new List<IPAddress>();
            foreach (var adapter in NetworkInterface.GetAllNetworkInterfaces())
            {
                var properties = adapter.GetIPProperties();
                foreach (var unicast in properties.UnicastAddresses)
                    if (ipep.AddressFamily == unicast.Address.AddressFamily)
                        list.Add(unicast.Address);
            }

            return list.ToArray();
        }

        private static void DisplayListener(IPEndPoint ipep)
        {
            if (ipep.Address.Equals(IPAddress.Any) || ipep.Address.Equals(IPAddress.IPv6Any))
                foreach (var ip in m_ListeningAddresses)
                    Console.WriteLine("Listening: {0}:{1}", ip, ipep.Port);
            else
                Console.WriteLine("Listening: {0}:{1}", ipep.Address, ipep.Port);
        }
    }
}
