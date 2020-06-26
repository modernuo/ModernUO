using System;
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

      const short cur = 50;
      const short max = 100;

      AttributeNormalizer.Write(stream, cur, max);

      Span<byte> expectedData = stackalloc byte[]
      {
        0x00, 0x00, // Maximum Normalized
        0x00, 0x00 // Current Normalized
      };

      ((short)AttributeNormalizer.Maximum).CopyTo(expectedData.Slice(0, 2));
      ((short)(cur * 25 / max)).CopyTo(expectedData.Slice(2, 2));

      AssertThat.Equal(stream.ToArray(), expectedData);
    }

    [Fact]
    public void TestAttributeNormalizerReversedEnabled()
    {
      AttributeNormalizer.Enabled = true;
      AttributeNormalizer.Maximum = 25;

      PacketWriter stream = new PacketWriter(4);

      const short cur = 50;
      const short max = 100;

      AttributeNormalizer.WriteReverse(stream, cur, max);

      Span<byte> expectedData = stackalloc byte[]
      {
        0x00, 0x00, // Current Normalized
        0x00, 0x00 // Maximum Normalized
      };

      ((short)AttributeNormalizer.Maximum).CopyTo(expectedData.Slice(2, 2));
      ((short)(cur * 25 / max)).CopyTo(expectedData.Slice(0, 2));

      AssertThat.Equal(stream.ToArray(), expectedData);
    }

    [Fact]
    public void TestAttributeNormalizerDisabled()
    {
      AttributeNormalizer.Enabled = false;

      PacketWriter stream = new PacketWriter(4);

      const short cur = 50;
      const short max = 100;

      AttributeNormalizer.Write(stream, cur, max);

      Span<byte> expectedData = stackalloc byte[]
      {
        0x00, 0x00, // Maximum
        0x00, 0x00 // Current
      };

      max.CopyTo(expectedData.Slice(0, 2));
      cur.CopyTo(expectedData.Slice(2, 2));

      AssertThat.Equal(stream.ToArray(), expectedData);
    }

    [Fact]
    public void TestAttributeNormalizerReversedDisabled()
    {
      AttributeNormalizer.Enabled = false;

      PacketWriter stream = new PacketWriter(4);

      const short cur = 50;
      const short max = 100;

      AttributeNormalizer.WriteReverse(stream, cur, max);

      Span<byte> expectedData = stackalloc byte[]
      {
        0x00, 0x00, // Current
        0x00, 0x00 // Maximum
      };

      max.CopyTo(expectedData.Slice(2, 2));
      cur.CopyTo(expectedData.Slice(0, 2));

      AssertThat.Equal(stream.ToArray(), expectedData);
    }
  }
}
