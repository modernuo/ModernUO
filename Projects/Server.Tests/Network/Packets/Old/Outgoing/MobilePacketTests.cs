using System;
using Server.Network;
using Xunit;

namespace Server.Tests.Network.Packets
{
  public class MobilePacketTests : IClassFixture<ServerFixture>
  {
    [Fact]
    public void TestDeathAnimation()
    {
      Serial killed = 0x1;
      Serial corpse = 0x1000;

      Span<byte> data = new DeathAnimation(killed, corpse).Compile();

      Span<byte> expectedData = stackalloc byte[]
      {
        0xAF, // Packet ID
        0x00, 0x00, 0x00, 0x00, // Serial of killed
        0x00, 0x00, 0x00, 0x00, // Serial of corpse
        0x00, 0x00, 0x00, 0x00
      };

      killed.CopyTo(expectedData.Slice(1, 4));
      corpse.CopyTo(expectedData.Slice(5, 4));

      AssertThat.Equal(data, expectedData);
    }

    [Fact]
    public void TestBondStatus()
    {
      Serial petSerial = 0x1;
      bool bonded = true;

      Span<byte> data = new BondedStatus(petSerial, bonded).Compile();

      Span<byte> expectedData = stackalloc byte[]
      {
        0xBF, // Packet ID
        0x00, 0x0B, // Length
        0x00, 0x19, // Sub-packet
        0x00, // Sub command
        0x00, 0x00, 0x00, 0x00, // Serial
        (byte)(bonded ? 0x1 : 0x0)
      };

      petSerial.CopyTo(expectedData.Slice(6, 4));

      AssertThat.Equal(data, expectedData);
    }

    [Fact]
    public void TestMobileMoving()
    {
      var m = new Mobile(0x1);
      m.DefaultMobileInit();

      int noto = 10;

      Span<byte> data = new MobileMoving(m, noto).Compile();

      Span<byte> expectedData = stackalloc byte[]
      {
        0x77, // Packet ID
        0x00, 0x00, 0x00, 0x00, // Serial
        0x00, 0x00, // Body
        0x00, 0x00, // X
        0x00, 0x00, // Y
        0x00, // Z
        0x00, // Direction
        0x00, 0x00, // Hue
        0x00, // Flags
        0x00 // Noto
      };

      m.Serial.CopyTo(expectedData.Slice(1, 4));
      ((ushort)m.Body).CopyTo(expectedData.Slice(5, 2));
      ((ushort)m.X).CopyTo(expectedData.Slice(7, 2));
      ((ushort)m.Y).CopyTo(expectedData.Slice(9, 2));
      expectedData[11] = (byte)m.Z;
      expectedData[12] = (byte)m.Direction;
      ((ushort)m.Hue).CopyTo(expectedData.Slice(13, 2));
      expectedData[15] = (byte)m.GetPacketFlags();
      expectedData[16] = (byte)noto;

      AssertThat.Equal(data, expectedData);
    }

    [Fact]
    public void TestMobileMovingOld()
    {
      var m = new Mobile(0x1);
      m.DefaultMobileInit();

      int noto = 10;

      Span<byte> data = new MobileMoving(m, noto).Compile();

      Span<byte> expectedData = stackalloc byte[]
      {
        0x77, // Packet ID
        0x00, 0x00, 0x00, 0x00, // Serial
        0x00, 0x00, // Body
        0x00, 0x00, // X
        0x00, 0x00, // Y
        0x00, // Z
        0x00, // Direction
        0x00, 0x00, // Hue
        0x00, // Flags
        0x00 // Noto
      };

      m.Serial.CopyTo(expectedData.Slice(1, 4));
      ((ushort)m.Body).CopyTo(expectedData.Slice(5, 2));
      ((ushort)m.X).CopyTo(expectedData.Slice(7, 2));
      ((ushort)m.Y).CopyTo(expectedData.Slice(9, 2));
      expectedData[11] = (byte)m.Z;
      expectedData[12] = (byte)m.Direction;
      int hue = m.SolidHueOverride >= 0 ? m.SolidHueOverride : m.Hue;
      ((ushort)hue).CopyTo(expectedData.Slice(13, 2));
      expectedData[15] = (byte)m.GetPacketFlags();
      expectedData[16] = (byte)noto;

      AssertThat.Equal(data, expectedData);
    }

    [Fact]
    public void TestMobileHits()
    {
      var m = new Mobile(0x1);
      m.DefaultMobileInit();

      Span<byte> data = new MobileHits(m).Compile();

      Span<byte> expectedData = stackalloc byte[]
      {
        0xA1, // Packet ID
        0x00, 0x00, 0x00, 0x00, // Serial
        0x00, 0x00, // Max Hits
        0x00, 0x00 // Current Hits
      };

      m.Serial.CopyTo(expectedData.Slice(1, 4));
      ((ushort)m.HitsMax).CopyTo(expectedData.Slice(5, 2));
      ((ushort)m.Hits).CopyTo(expectedData.Slice(7, 2));

      AssertThat.Equal(data, expectedData);
    }

    [Fact]
    public void TestMobileHitsN()
    {
      var m = new Mobile(0x1);
      m.DefaultMobileInit();

      Span<byte> data = new MobileHitsN(m).Compile();

      Span<byte> expectedData = stackalloc byte[]
      {
        0xA1, // Packet ID
        0x00, 0x00, 0x00, 0x00, // Serial
        0x00, 0x00, // Max Hits
        0x00, 0x00 // Current Hits
      };

      int max = AttributeNormalizer.Maximum;

      m.Serial.CopyTo(expectedData.Slice(1, 4));
      ((ushort)AttributeNormalizer.Maximum).CopyTo(expectedData.Slice(5, 2));
      ((ushort)(m.Hits * max / m.HitsMax)).CopyTo(expectedData.Slice(7, 2));

      AssertThat.Equal(data, expectedData);
    }

    [Fact]
    public void TestMobileMana()
    {
      var m = new Mobile(0x1);
      m.DefaultMobileInit();

      Span<byte> data = new MobileMana(m).Compile();

      Span<byte> expectedData = stackalloc byte[]
      {
        0xA2, // Packet ID
        0x00, 0x00, 0x00, 0x00, // Serial
        0x00, 0x00, // Max Hits
        0x00, 0x00 // Current Hits
      };

      m.Serial.CopyTo(expectedData.Slice(1, 4));
      ((ushort)m.ManaMax).CopyTo(expectedData.Slice(5, 2));
      ((ushort)m.Mana).CopyTo(expectedData.Slice(7, 2));

      AssertThat.Equal(data, expectedData);
    }

    [Fact]
    public void TestMobileManaN()
    {
      var m = new Mobile(0x1);
      m.DefaultMobileInit();

      Span<byte> data = new MobileManaN(m).Compile();

      Span<byte> expectedData = stackalloc byte[]
      {
        0xA2, // Packet ID
        0x00, 0x00, 0x00, 0x00, // Serial
        0x00, 0x00, // Max Hits
        0x00, 0x00 // Current Hits
      };

      int max = AttributeNormalizer.Maximum;

      m.Serial.CopyTo(expectedData.Slice(1, 4));
      ((ushort)AttributeNormalizer.Maximum).CopyTo(expectedData.Slice(5, 2));
      ((ushort)(m.Mana * max / m.ManaMax)).CopyTo(expectedData.Slice(7, 2));

      AssertThat.Equal(data, expectedData);
    }

    [Fact]
    public void TestMobileStam()
    {
      var m = new Mobile(0x1);
      m.DefaultMobileInit();

      Span<byte> data = new MobileStam(m).Compile();

      Span<byte> expectedData = stackalloc byte[]
      {
        0xA3, // Packet ID
        0x00, 0x00, 0x00, 0x00, // Serial
        0x00, 0x00, // Max Hits
        0x00, 0x00 // Current Hits
      };

      m.Serial.CopyTo(expectedData.Slice(1, 4));
      ((ushort)m.StamMax).CopyTo(expectedData.Slice(5, 2));
      ((ushort)m.Stam).CopyTo(expectedData.Slice(7, 2));

      AssertThat.Equal(data, expectedData);
    }

    [Fact]
    public void TestMobileStamN()
    {
      var m = new Mobile(0x1);
      m.DefaultMobileInit();

      Span<byte> data = new MobileStamN(m).Compile();

      Span<byte> expectedData = stackalloc byte[]
      {
        0xA2, // Packet ID
        0x00, 0x00, 0x00, 0x00, // Serial
        0x00, 0x00, // Max Hits
        0x00, 0x00 // Current Hits
      };

      int max = AttributeNormalizer.Maximum;

      m.Serial.CopyTo(expectedData.Slice(1, 4));
      ((ushort)AttributeNormalizer.Maximum).CopyTo(expectedData.Slice(5, 2));
      ((ushort)(m.Stam * max / m.StamMax)).CopyTo(expectedData.Slice(7, 2));

      AssertThat.Equal(data, expectedData);
    }
  }
}
