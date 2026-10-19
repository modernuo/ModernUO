using System;
using Server.Network;

namespace Server.Tests.Network;

public static class PacketAssert
{
    /// <summary>
    /// Asserts that <paramref name="send"/> writes exactly the bytes of the reference <paramref name="expected"/>
    /// packet to a fresh test NetState negotiated with <paramref name="changes"/>.
    /// </summary>
    public static void Sends(Packet expected, Action<NetState> send, ProtocolChanges changes = ProtocolChanges.None) =>
        Sends(_ => expected, send, changes);

    /// <summary>
    /// As <see cref="Sends(Packet, Action{NetState}, ProtocolChanges)"/>, for a reference packet that depends on
    /// the negotiated NetState. It is built after <paramref name="changes"/> is applied and before the send.
    /// </summary>
    public static void Sends(
        Func<NetState, Packet> expected, Action<NetState> send, ProtocolChanges changes = ProtocolChanges.None
    )
    {
        using var ns = PacketTestUtilities.CreateTestNetState();
        ns.ProtocolChanges = changes;

        var expectedBytes = expected(ns).Compile();
        send(ns);

        AssertThat.Equal(ns.SendBuffer.GetReadSpan(), expectedBytes);
    }
}
