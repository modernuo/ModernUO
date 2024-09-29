using System;
using System.Net;
using System.Net.Sockets;

namespace Server.Network.Sockets
{
    public sealed class TcpSocket : ISocket
    {
        public Socket Socket { get; }
        public bool Connected => Socket.Connected;
        public IPEndPoint? LocalEndPoint => Socket.LocalEndPoint as IPEndPoint;
        public IPEndPoint? RemoteEndPoint => Socket.RemoteEndPoint as IPEndPoint;

        public TcpSocket(Socket socket)
        {
            Socket = socket;
        }

        public int Send(ReadOnlySpan<byte> buffer) => Socket.Send(buffer, SocketFlags.None);
        
        public int Receive(Span<byte> buffer, out bool forceClose)
        {
            int bytesRead = Socket.Receive(buffer, SocketFlags.None);
            forceClose = bytesRead == 0;

            return bytesRead;
        }

        public void Close() => Socket.Close();
    }
}
