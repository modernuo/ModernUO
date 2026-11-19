using System;
using System.Buffers;
using System.Buffers.Binary;
using Server.Network;
using Xunit;

namespace Server.Tests.Network;

public class PacketLengthTests
{
    [Fact]
    public void LargestPacket_KeepsItsLength()
    {
        var writer = new SpanWriter(new byte[ushort.MaxValue]);
        writer.Write((byte)0xDD);
        writer.Seek(ushort.MaxValue, System.IO.SeekOrigin.Begin);
        writer.WritePacketLength();

        Assert.Equal(ushort.MaxValue, BinaryPrimitives.ReadUInt16BigEndian(writer.Span[1..]));
    }

#if DEBUG
    [Fact]
    public void OversizedPacket_ThrowsInsteadOfWrappingItsLength()
    {
        Assert.Throws<InvalidOperationException>(
            () =>
            {
                var writer = new SpanWriter(new byte[ushort.MaxValue + 1]);
                writer.Write((byte)0xDD);
                writer.Seek(ushort.MaxValue + 1, System.IO.SeekOrigin.Begin);
                writer.WritePacketLength();
            }
        );
    }
#endif
}
