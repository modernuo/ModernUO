using System;
using System.Buffers;
using Server.Network;
using Xunit;

namespace Server.Tests.Network.Packets
{
  public class PlayerPacketTests : IClassFixture<ServerFixture>
  {
    [Fact]
    public void TestStatLockInfo()
    {
      Mobile m = new Mobile(0x1);
      m.DefaultMobileInit();

      Span<byte> data = new StatLockInfo(m).Compile();

      Span<byte> expectedData = stackalloc byte[12];
      int pos = 0;

      expectedData.Write(ref pos, (byte)0xBF); // Packet ID
      expectedData.Write(ref pos, (ushort)12); // Length
      expectedData.Write(ref pos, (ushort)0x19); // Sub-packet
      expectedData.Write(ref pos, (byte)2); // Command
      expectedData.Write(ref pos, m.Serial);
      expectedData.Write(ref pos, (ushort)(((int)m.StrLock << 4) | ((int)m.DexLock << 2) | (int)m.IntLock));

      AssertThat.Equal(data, expectedData);
    }

    [Fact]
    public void TestChangeUpdateRange()
    {
      byte range = 10;
      Span<byte> data = new ChangeUpdateRange(range).Compile();

      Span<byte> expectedData = stackalloc byte[2];
      int pos = 0;

      expectedData.Write(ref pos, (byte)0xC8); // Packet ID
      expectedData.Write(ref pos, range);

      AssertThat.Equal(data, expectedData);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void TestDeathStatus(bool isDead)
    {
      Span<byte> data = new DeathStatus(isDead).Compile();

      Span<byte> expectedData = stackalloc byte[2];
      int pos = 0;

      const byte dead = 0;
      const byte alive = 2;
      expectedData.Write(ref pos, (byte)0x2C); // Packet ID
      expectedData.Write(ref pos, isDead ? dead : alive);

      AssertThat.Equal(data, expectedData);
    }

    [Theory]
    [InlineData(0, true)]
    [InlineData(0, false)]
    [InlineData(100, true)]
    [InlineData(1000, false)]
    public void TestSpecialAbility(int abilityId, bool active)
    {
      Span<byte> data = new ToggleSpecialAbility(abilityId, active).Compile();

      Span<byte> expectedData = stackalloc byte[8];
      int pos = 0;

      expectedData.Write(ref pos, (byte)0xBF); // Packet ID
      expectedData.Write(ref pos, (ushort)0x8); // Length
      expectedData.Write(ref pos, (ushort)0x25); // Sub-packet
      expectedData.Write(ref pos, (ushort)abilityId);
      expectedData.Write(ref pos, active);

      AssertThat.Equal(data, expectedData);
    }

    [Theory]
    [InlineData("This is a header", "This is a body", "This is a footer")]
    [InlineData(null, null, null)]
    public void TestDisplayProfile(string header, string body, string footer)
    {
      Serial m = 0x1000;

      Span<byte> data = new DisplayProfile(m, header, body, footer).Compile();

      header ??= "";
      body ??= "";
      footer ??= "";

      int length = 12 + header.Length + footer.Length * 2 + body.Length * 2;

      Span<byte> expectedData = stackalloc byte[length];
      int pos = 0;

      expectedData.Write(ref pos, (byte)0xB8); // Packet ID
      expectedData.Write(ref pos, (ushort)length); // Length
      expectedData.Write(ref pos, m); // Mobile Serial or Serial.Zero
      expectedData.WriteAsciiNull(ref pos, header);
      expectedData.WriteBigUniNull(ref pos, footer);
      expectedData.WriteBigUniNull(ref pos, body);

      AssertThat.Equal(data, expectedData);
    }

    [Theory]
    [InlineData(LRReason.CannotLift)]
    [InlineData(LRReason.TryToSteal)]
    public void TestLiftRej(LRReason reason)
    {
      Span<byte> data = new LiftRej(reason).Compile();

      Span<byte> expectedData = stackalloc byte[2];
      int pos = 0;

      expectedData.Write(ref pos, (byte)0x27); // Packet ID
      expectedData.Write(ref pos, (byte)reason);

      AssertThat.Equal(data, expectedData);
    }

    [Fact]
    public void TestLogoutAck()
    {
      Span<byte> data = new LogoutAck().Compile();

      Span<byte> expectedData = stackalloc byte[2];
      int pos = 0;

      expectedData.Write(ref pos, (byte)0xD1); // Packet ID
      expectedData.Write(ref pos, (byte)0x1); // 1 - Ack

      AssertThat.Equal(data, expectedData);
    }

    [Theory]
    [InlineData(1, 2, 3)]
    [InlineData(4, 5, 6)]
    public void TestWeather(int type, int density, int temp)
    {
      Span<byte> data = new Weather(type, density, temp).Compile();

      Span<byte> expectedData = stackalloc byte[4];
      int pos = 0;

      expectedData.Write(ref pos, (byte)0x65); // Packet ID
      expectedData.Write(ref pos, (byte)type);
      expectedData.Write(ref pos, (byte)density);
      expectedData.Write(ref pos, (byte)temp);

      AssertThat.Equal(data, expectedData);
    }

    [Fact]
    public void TestRemoveEntity()
    {
      Serial e = 0x1000;
      Span<byte> data = new RemoveEntity(e).Compile();

      Span<byte> expectedData = stackalloc byte[5];
      int pos = 0;

      expectedData.Write(ref pos, (byte)0x1D); // Packet ID
      expectedData.Write(ref pos, e);

      AssertThat.Equal(data, expectedData);
    }
  }
}
