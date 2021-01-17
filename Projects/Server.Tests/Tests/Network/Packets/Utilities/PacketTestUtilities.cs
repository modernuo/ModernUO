using System;
using System.Collections.Generic;
using System.Net.Sockets;
using Moq;
using Server.Network;

namespace Server.Tests.Network
{
    public static class PacketTestUtilities
    {
        public static Span<byte> Compile(this Packet p) =>
            p.Compile(false, out var length).AsSpan(0, length);

        public static NetState CreateTestNetState()
        {
            var socket = new Mock<ISocket>();
            socket
                .Setup(s => s.SendAsync(It.IsAny<IList<ArraySegment<byte>>>(), SocketFlags.None))
                .ReturnsAsync(() => 0);

            socket
                .Setup(s => s.ReceiveAsync(It.IsAny<IList<ArraySegment<byte>>>(), SocketFlags.None))
                .ReturnsAsync(() => 0);

            return new NetState(socket.Object);
        }
    }
}
