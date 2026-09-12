using System.Runtime.CompilerServices;

namespace Server.Network;

public static class OutgoingPackets
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool CannotSendPackets(this NetState ns) =>
        // Do not check for NetState.Running: packets sent between Disconnect() and the Slice() that hands it to the
        // socket are meant to be delivered (kicks with a message, the play-server ack). Once the socket has the
        // disconnect (DisconnectPending) the transport drains what is already buffered and closes; anything written
        // after that keeps the buffer from draining and is never delivered, so it is dropped here.
        ns == null || ns.SocketHandle == 0 || ns._socket.DisconnectPending || ns.BlockAllPackets;
}
