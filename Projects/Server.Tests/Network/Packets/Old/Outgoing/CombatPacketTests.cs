using System;
using Server.Network;
using Xunit;

namespace Server.Tests.Network.Packets
{
  public class CombatPacketTests
  {
    [Fact]
    public void TestSwing()
    {
      Serial attacker = 0x1000;
      Serial defender = 0x2000;

      Span<byte> data = new Swing(attacker, defender).Compile();

      Span<byte> expectedData = stackalloc byte[]
      {
        0x2F, // Packet ID
        0x00, // Unknown
        0x00, 0x00, 0x00, 0x00, // Attacker
        0x00, 0x00, 0x00, 0x00, // Defender
      };

      attacker.CopyTo(expectedData.Slice(2, 4));
      defender.CopyTo(expectedData.Slice(6, 4));

      AssertThat.Equal(data, expectedData);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void TestSetWarMode(bool warmode)
    {
      Span<byte> data = new SetWarMode(warmode).Compile();

      Span<byte> expectedData = stackalloc byte[]
      {
        0x72, // Packet ID
        warmode ? (byte)0x01 : (byte)0x00, // Mode
        0x00, 0x32, 0x00 // Unknown
      };

      AssertThat.Equal(data, expectedData);
    }

    [Fact]
    public void TestChangeCombatant()
    {
      Serial combatant = 0x1000;

      Span<byte> data = new ChangeCombatant(combatant).Compile();

      Span<byte> expectedData = stackalloc byte[]
      {
        0xAA, // Packet ID
        0x00, 0x00, 0x00, 0x00 // Combatant
      };

      combatant.CopyTo(expectedData.Slice(1, 4));

      AssertThat.Equal(data, expectedData);
    }
  }
}
