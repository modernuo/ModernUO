using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Server.Network;

namespace Server.Tests.Network
{
    public class MockSocket : ISocket
    {
        public EndPoint LocalEndPoint { get; set; }
        public EndPoint RemoteEndPoint { get; set; }
        public Task<int> SendAsync(IList<ArraySegment<byte>> buffers, SocketFlags flags) => Task.Run(() => 0);

        public Task<int> ReceiveAsync(IList<ArraySegment<byte>> buffers, SocketFlags flags) => Task.Run(() => 0);

        public void Shutdown(SocketShutdown how)
        {
        }
    }
}
