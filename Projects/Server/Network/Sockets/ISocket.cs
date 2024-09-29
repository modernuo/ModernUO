using System;
using System.Net;
using System.Net.Sockets;

namespace Server.Network.Sockets
{
    public interface ISocket
    {
        public Socket Socket { get; }
        public bool Connected { get; }
        public IPEndPoint? LocalEndPoint { get; }
        public IPEndPoint? RemoteEndPoint { get; }

        public int Send(ReadOnlySpan<byte> buffer);
        public int Receive(Span<byte> buffer, out bool forceClose);
        public void Close();
    }
}
