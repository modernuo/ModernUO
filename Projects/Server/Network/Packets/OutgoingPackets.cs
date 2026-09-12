using System.Runtime.CompilerServices;

namespace Server.Network;

public static class OutgoingPackets
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool CannotSendPackets(this NetState ns) =>
        // Running stays true until the transport reports the socket closed, so it says nothing about whether a
        // packet can still go out. The send window is between Disconnect() and the Slice() that hands it to the
        // socket (kicks with a message, the play-server ack). After the handoff the socket is either draining
        // (DisconnectPending) or already closing (Connected false, the Disconnected event lands next Slice); new
        // writes would only keep the buffer from draining, so they are dropped here to bound the remaining output.
        ns == null || ns.SocketHandle == 0 || !ns._socket.Connected || ns._socket.DisconnectPending || ns.BlockAllPackets;
}
