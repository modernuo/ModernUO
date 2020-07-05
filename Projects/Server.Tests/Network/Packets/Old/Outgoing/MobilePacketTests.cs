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
      AttributeNormalizerUtilities.Write(m.Hits, m.HitsMax, false, expectedData.Slice(5));

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
      AttributeNormalizerUtilities.Write(m.Hits, m.HitsMax, true, expectedData.Slice(5));

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
      AttributeNormalizerUtilities.Write(m.Mana, m.ManaMax, false, expectedData.Slice(5));

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
      AttributeNormalizerUtilities.Write(m.Mana, m.ManaMax, true, expectedData.Slice(5));

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
      AttributeNormalizerUtilities.Write(m.Stam, m.StamMax, false, expectedData.Slice(5));

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
        0xA3, // Packet ID
        0x00, 0x00, 0x00, 0x00, // Serial
        0x00, 0x00, // Max Hits
        0x00, 0x00 // Current Hits
      };

      int max = AttributeNormalizer.Maximum;

      m.Serial.CopyTo(expectedData.Slice(1, 4));
      AttributeNormalizerUtilities.Write(m.Stam, m.StamMax, true, expectedData.Slice(5));

      AssertThat.Equal(data, expectedData);
    }

    [Fact]
    public void TestMobileAttributes()
    {
      var m = new Mobile(0x1);
      m.DefaultMobileInit();

      Span<byte> data = new MobileAttributes(m).Compile();

      Span<byte> expectedData = stackalloc byte[]
      {
        0x2D, // Packet ID
        0x00, 0x00, 0x00, 0x00, // Serial
        0x00, 0x00, // Max Hits
        0x00, 0x00, // Current Hits
        0x00, 0x00, // Max Mana
        0x00, 0x00, // Current Mana
        0x00, 0x00, // Max Stam
        0x00, 0x00 // Current Stam
      };

      m.Serial.CopyTo(expectedData.Slice(1, 4));
      AttributeNormalizerUtilities.Write(m.Hits, m.HitsMax, false, expectedData.Slice(5));
      AttributeNormalizerUtilities.Write(m.Mana, m.ManaMax, false, expectedData.Slice(9));
      AttributeNormalizerUtilities.Write(m.Stam, m.StamMax, false, expectedData.Slice(13));

      AssertThat.Equal(data, expectedData);
    }

    [Fact]
    public void TestMobileAttributesN()
    {
      var m = new Mobile(0x1);
      m.DefaultMobileInit();

      Span<byte> data = new MobileAttributesN(m).Compile();

      Span<byte> expectedData = stackalloc byte[]
      {
        0x2D, // Packet ID
        0x00, 0x00, 0x00, 0x00, // Serial
        0x00, 0x00, // Max Hits
        0x00, 0x00, // Current Hits
        0x00, 0x00, // Max Mana
        0x00, 0x00, // Current Mana
        0x00, 0x00, // Max Stam
        0x00, 0x00 // Current Stam
      };

      int max = AttributeNormalizer.Maximum;

      m.Serial.CopyTo(expectedData.Slice(1, 4));
      AttributeNormalizerUtilities.Write(m.Hits, m.HitsMax, true, expectedData.Slice(5));
      AttributeNormalizerUtilities.Write(m.Mana, m.ManaMax, true, expectedData.Slice(9));
      AttributeNormalizerUtilities.Write(m.Stam, m.StamMax, true, expectedData.Slice(13));

      AssertThat.Equal(data, expectedData);
    }

    [Fact]
    public void TestMobileName()
    {
      var m = new Mobile(0x1)
      {
        Name = "Some Really Long Mobile Name That Gets Cut off",
      };
      m.DefaultMobileInit();

      Span<byte> data = new MobileName(m).Compile();

      Span<byte> expectedData = stackalloc byte[37];
      int pos = 0;
      ((byte)0x98).CopyTo(ref pos, expectedData);
      ((ushort)0x25).CopyTo(ref pos, expectedData);
      m.Serial.CopyTo(ref pos, expectedData);
      (m.Name ?? "").CopyRawASCIITo(ref pos, 29, expectedData);
      ((byte)0x0).CopyTo(ref pos, expectedData); // Null terminator

      AssertThat.Equal(data, expectedData);
    }

    [Fact]
    public void TestMobileAnimation()
    {
      Serial mobile = 0x1;
      int action = 200;
      int frameCount = 5;
      int repeatCount = 1;
      bool reverse = false;
      bool repeat = false;
      byte delay = 5;

      Span<byte> data = new MobileAnimation(
        mobile,
        action,
        frameCount,
        repeatCount,
        !reverse,
        repeat,
        delay
      ).Compile();

      Span<byte> expectedData = stackalloc byte[14];
      int pos = 0;

      ((byte)0x6E).CopyTo(ref pos, expectedData);
      mobile.CopyTo(ref pos, expectedData);
      ((ushort)action).CopyTo(ref pos, expectedData);
      ((ushort)frameCount).CopyTo(ref pos, expectedData);
      ((ushort)repeatCount).CopyTo(ref pos, expectedData);
      reverse.CopyTo(ref pos, expectedData);
      repeat.CopyTo(ref pos, expectedData);
      delay.CopyTo(ref pos, expectedData);

      AssertThat.Equal(data, expectedData);
    }

    [Fact]
    public void TestNewMobileAnimation()
    {
      Serial mobile = 0x1;
      int action = 200;
      int frameCount = 5;
      byte delay = 5;

      Span<byte> data = new NewMobileAnimation(
        mobile,
        action,
        frameCount,
        delay
      ).Compile();

      Span<byte> expectedData = stackalloc byte[10];
      int pos = 0;

      ((byte)0xE2).CopyTo(ref pos, expectedData);
      mobile.CopyTo(ref pos, expectedData);
      ((ushort)action).CopyTo(ref pos, expectedData);
      ((ushort)frameCount).CopyTo(ref pos, expectedData);
      delay.CopyTo(ref pos, expectedData);

      AssertThat.Equal(data, expectedData);
    }

    [Fact]
    public void TestMobileStatusCompact()
    {
      var m = new Mobile(0x1);
      m.DefaultMobileInit();

      bool canBeRenamed = false;

      Span<byte> data = new MobileStatusCompact(canBeRenamed, m).Compile();

      Span<byte> expectedData = stackalloc byte[43];
      int pos = 0;

      ((byte)0x11).CopyTo(ref pos, expectedData);
      ((ushort)expectedData.Length).CopyTo(ref pos, expectedData);
      m.Serial.CopyTo(ref pos, expectedData);
      (m.Name ?? "").CopyASCIIFixedTo(ref pos, 30, expectedData);

      AssertThat.Equal(data, expectedData);
    }
  }
}
