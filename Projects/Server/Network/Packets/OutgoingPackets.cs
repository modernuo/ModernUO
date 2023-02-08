using System.Runtime.CompilerServices;

namespace Server.Network;

public static class OutgoingPackets
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool CannotSendPackets(this NetState ns) =>
        // Do not check for NetState.Running. Packets are sent to a "disconnected" socket as part of the OnDisconnect events
        // up until the Connection is nulled. Closing the connection is done synchronously, therefore packets will not be sent
        // once the Mobile.NetState is null.
        ns?.Connection == null || ns.BlockAllPackets;
}
