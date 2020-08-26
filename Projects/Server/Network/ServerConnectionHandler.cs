/*************************************************************************
 * ModernUO                                                              *
 * Copyright (C) 2019-2020 - ModernUO Development Team                   *
 * Email: hi@modernuo.com                                                *
 * File: ServerConnectionHandler.cs                                      *
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
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.Extensions.Logging;

namespace Server.Network
{
    public class ServerConnectionHandler : ConnectionHandler
    {
        private readonly ILogger<ServerConnectionHandler> _logger;
        private readonly IMessagePumpService _messagePumpService;

        public ServerConnectionHandler(
            IMessagePumpService messagePumpService,
            ILogger<ServerConnectionHandler> logger
        )
        {
            _messagePumpService = messagePumpService;
            _logger = logger;
        }

        public override async Task OnConnectedAsync(ConnectionContext connection)
        {
            if (!VerifySocket(connection))
            {
                Release(connection);
                return;
            }

            var ns = new NetState(connection);
            TcpServer.Instances.Add(ns);
            Console.WriteLine($"Client: {ns}: Connected. [{TcpServer.Instances.Count} Online]");

            await ns.ProcessIncoming(_messagePumpService).ConfigureAwait(false);
        }

        private static bool VerifySocket(ConnectionContext connection)
        {
            try
            {
                var args = new SocketConnectEventArgs(connection);

                EventSink.InvokeSocketConnect(args);

                return args.AllowConnection;
            }
            catch (Exception ex)
            {
                NetState.TraceException(ex);
                return false;
            }
        }

        private static void Release(ConnectionContext connection)
        {
            try
            {
                connection.Abort(new ConnectionAbortedException("Failed socket verification."));
            }
            catch (Exception ex)
            {
                NetState.TraceException(ex);
            }

            try
            {
                // TODO: Is this needed?
                connection.DisposeAsync();
            }
            catch (Exception ex)
            {
                NetState.TraceException(ex);
            }
        }
    }
}
