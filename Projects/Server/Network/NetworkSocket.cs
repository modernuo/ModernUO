/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2020 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: NetworkSocket.cs                                                *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Server.Network
{
    public class NetworkSocket : ISocket
    {
        private readonly Socket _connection;

        public Socket Connection
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _connection;
        }

        public EndPoint LocalEndPoint
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _connection.LocalEndPoint;
        }

        public EndPoint RemoteEndPoint
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _connection.RemoteEndPoint;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NetworkSocket(Socket connection) => _connection = connection;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Task<int> SendAsync(IList<ArraySegment<byte>> buffers, SocketFlags flags) =>
            _connection.SendAsync(buffers, flags);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Send(IList<ArraySegment<byte>> buffers, SocketFlags flags) => _connection.Send(buffers, flags);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Task<int> ReceiveAsync(IList<ArraySegment<byte>> buffers, SocketFlags flags) =>
            _connection.ReceiveAsync(buffers, flags);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Shutdown(SocketShutdown how) => _connection.Shutdown(how);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Close() => _connection.Close();
    }
}
