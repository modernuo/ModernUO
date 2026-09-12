using System.Runtime.CompilerServices;

namespace Server.Network;

public static class OutgoingPackets
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool CannotSendPackets(this NetState ns) =>
        // Running is not checked: sends between Disconnect() and the Slice() handoff must still go out.
        // After the handoff the socket is draining (DisconnectPending) or closing (!Connected); new writes
        // only keep the buffer from draining.
        ns == null || ns.SocketHandle == 0 || !ns._socket.Connected || ns._socket.DisconnectPending || ns.BlockAllPackets;
}
