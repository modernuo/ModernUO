using System.Net.Sockets;
using System.Threading;
using Server.Network;

namespace Server.Tests
{
    public static class PipeTestNetState
    {
        public static NetState CreateOutgoing(out Pipe<byte> outgoing)
        {
            var socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            outgoing = new Pipe<byte>(new byte[NetState.OutgoingPipeSize]);
            return new NetState(socket, new Pipe<byte>(new byte[0x1]), outgoing, Thread.CurrentThread);
        }

        public static NetState CreateIncoming(out Pipe<byte> incoming)
        {
            var socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            incoming = new Pipe<byte>(new byte[NetState.IncomingPipeSize]);
            return new NetState(socket, incoming, new Pipe<byte>(new byte[0x1]), Thread.CurrentThread);
        }
    }
}
