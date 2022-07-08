using System.Runtime.CompilerServices;

namespace Server.Network;

public static class OutgoingPackets
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool CannotSendPackets(this NetState ns) => ns?.Connection == null || ns.BlockAllPackets;
}
