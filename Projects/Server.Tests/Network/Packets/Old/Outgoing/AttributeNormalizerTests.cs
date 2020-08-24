using System;
using System.Buffers;
using Server.Network;
using Xunit;

namespace Server.Tests.Network.Packets
{
    public class AttributeNormalizerTests
    {
        [Fact]
        public void TestAttributeNormalizerEnabled()
        {
            AttributeNormalizer.Enabled = true;
            AttributeNormalizer.Maximum = 25;

            PacketWriter stream = new PacketWriter(4);

            const ushort cur = 50;
            const ushort max = 100;

            AttributeNormalizer.Write(stream, cur, max);

            Span<byte> expectedData = stackalloc byte[4];
            int pos = 0;
            expectedData.Write(ref pos, (ushort)AttributeNormalizer.Maximum);
            expectedData.Write(ref pos, (ushort)(cur * 25 / max));

            AssertThat.Equal(stream.ToArray(), expectedData);
        }

        [Fact]
        public void TestAttributeNormalizerReversedEnabled()
        {
            AttributeNormalizer.Enabled = true;
            AttributeNormalizer.Maximum = 25;

            PacketWriter stream = new PacketWriter(4);

            const ushort cur = 50;
            const ushort max = 100;

            AttributeNormalizer.WriteReverse(stream, cur, max);

            Span<byte> expectedData = stackalloc byte[4];
            int pos = 0;
            expectedData.Write(ref pos, (ushort)(cur * 25 / max));
            expectedData.Write(ref pos, (ushort)AttributeNormalizer.Maximum);

            AssertThat.Equal(stream.ToArray(), expectedData);
        }

        [Fact]
        public void TestAttributeNormalizerDisabled()
        {
            AttributeNormalizer.Enabled = false;

            PacketWriter stream = new PacketWriter(4);

            const ushort cur = 50;
            const ushort max = 100;

            AttributeNormalizer.Write(stream, cur, max);

            Span<byte> expectedData = stackalloc byte[4];
            int pos = 0;
            expectedData.Write(ref pos, max);
            expectedData.Write(ref pos, cur);

            AssertThat.Equal(stream.ToArray(), expectedData);
        }

        [Fact]
        public void TestAttributeNormalizerReversedDisabled()
        {
            AttributeNormalizer.Enabled = false;

            PacketWriter stream = new PacketWriter(4);

            const ushort cur = 50;
            const ushort max = 100;

            AttributeNormalizer.WriteReverse(stream, cur, max);

            Span<byte> expectedData = stackalloc byte[4];
            int pos = 0;
            expectedData.Write(ref pos, cur);
            expectedData.Write(ref pos, max);

            AssertThat.Equal(stream.ToArray(), expectedData);
        }
    }
}
