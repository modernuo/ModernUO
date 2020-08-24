using System;
using System.Buffers;
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

            Span<byte> expectedData = stackalloc byte[10];
            int pos = 0;

            expectedData.Write(ref pos, (byte)0x2F); // Packet ID
#if NO_LOCAL_INIT
      expectedData.Write(ref pos, (byte)0);
#else
            pos++;
#endif

            expectedData.Write(ref pos, attacker);
            expectedData.Write(ref pos, defender);

            AssertThat.Equal(data, expectedData);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void TestSetWarMode(bool warmode)
        {
            Span<byte> data = new SetWarMode(warmode).Compile();

            Span<byte> expectedData = stackalloc byte[5];
            int pos = 0;

            expectedData.Write(ref pos, (byte)0x72); // Packet ID
            expectedData.Write(ref pos, warmode);

#if NO_LOCAL_INIT
      expectedData.Write(ref pos, (byte)0);
#else
            pos++;
#endif

            expectedData.Write(ref pos, (byte)0x32);

#if NO_LOCAL_INIT
      expectedData.Write(ref pos, (byte)0);
#else
            pos++;
#endif

            AssertThat.Equal(data, expectedData);
        }

        [Fact]
        public void TestChangeCombatant()
        {
            Serial combatant = 0x1000;

            Span<byte> data = new ChangeCombatant(combatant).Compile();

            Span<byte> expectedData = stackalloc byte[5];
            int pos = 0;

            expectedData.Write(ref pos, (byte)0xAA); // Packet ID
            expectedData.Write(ref pos, combatant);

            AssertThat.Equal(data, expectedData);
        }
    }
}
