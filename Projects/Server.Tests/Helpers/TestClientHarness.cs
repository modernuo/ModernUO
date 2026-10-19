using System;
using System.Buffers;
using Server.Network;
using Server.Tests.Network;

namespace Server.Tests;

/// <summary>
/// A logged-in test client (NetState + Mobile) and byte-pattern probes over what it was sent.
/// The probes search the raw send buffer, so they only see packets queued since the NetState was created.
/// </summary>
internal static class TestClientHarness
{
    public static (NetState, Mobile) CreateClient(Point3D location)
    {
        var ns = PacketTestUtilities.CreateTestNetState();
        ns.Account = new MockAccount();

        var mobile = new Mobile(World.NewMobile);
        mobile.DefaultMobileInit();
        ns.Mobile = mobile;
        mobile.NetState = ns;
        mobile.MoveToWorld(location, Map.Felucca);
        return (ns, mobile);
    }

    public static void DisposeClient(NetState ns, Mobile mobile)
    {
        ns.Mobile = null;
        ns.Dispose();
        mobile.Delete();
    }

    public static bool ReceivedRemove(NetState ns, Serial serial) => CountRemoves(ns, serial) > 0;

    public static int CountRemoves(NetState ns, Serial serial)
    {
        Span<byte> expected = stackalloc byte[OutgoingEntityPackets.RemoveEntityLength];
        OutgoingEntityPackets.CreateRemoveEntity(expected, serial);

        var span = ns.SendBuffer.GetReadSpan();
        var count = 0;

        while (true)
        {
            var index = span.IndexOf(expected);

            if (index < 0)
            {
                break;
            }

            count++;
            span = span[(index + expected.Length)..];
        }

        return count;
    }

    public static bool ReceivedWorldItem(NetState ns, Item item)
    {
        Span<byte> expected = stackalloc byte[OutgoingEntityPackets.MaxWorldEntityPacketLength];
        var length = OutgoingItemPackets.CreateWorldItem(expected, item);
        return ns.SendBuffer.GetReadSpan().IndexOf(expected[..length]) >= 0;
    }

    public static bool ReceivedContentUpdate(NetState ns, Serial serial) => ReceivedPacketForSerial(ns, 0x25, serial);

    public static bool ReceivedEquipUpdate(NetState ns, Serial serial) => ReceivedPacketForSerial(ns, 0x2E, serial);

    public static bool ReceivedOplInfo(NetState ns, Serial serial) => ReceivedPacketForSerial(ns, 0xDC, serial);

    // Each of these packets is fixed-length with the serial immediately after the packet ID.
    private static bool ReceivedPacketForSerial(NetState ns, byte packetId, Serial serial)
    {
        Span<byte> expected = stackalloc byte[5];
        var writer = new SpanWriter(expected);
        writer.Write(packetId);
        writer.Write(serial);
        return ns.SendBuffer.GetReadSpan().IndexOf(expected) >= 0;
    }
}
