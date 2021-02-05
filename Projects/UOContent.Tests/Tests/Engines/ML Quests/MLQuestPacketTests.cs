using System;
using Moq;
using Server.Tests;
using Server.Tests.Network;
using Xunit;

namespace Server.Engines.MLQuests
{
    public class MLQuestPacketTests
    {
        [Theory]
        [InlineData(true, 1)]
        [InlineData(false, 2)]
        public void TestRaceChanger(bool female, int raceId)
        {
            var raceMock = new Mock<Race>(
                raceId, 0, "Test Race", "Test Races", 0x1, 0x2, 0x3, 0x4, Expansion.None
            );

            var race = raceMock.Object;

            var expected = new RaceChanger(female, race).Compile();

            var ns = PacketTestUtilities.CreateTestNetState();
            ns.SendRaceChanger(female, race);

            var result = ns.SendPipe.Reader.TryRead();
            AssertThat.Equal(result.Buffer[0].AsSpan(0), expected);
        }

        [Fact]
        public void TestCloseRaceChanger()
        {
            var expected = new CloseRaceChanger().Compile();

            var ns = PacketTestUtilities.CreateTestNetState();
            ns.SendCloseRaceChanger();

            var result = ns.SendPipe.Reader.TryRead();
            AssertThat.Equal(result.Buffer[0].AsSpan(0), expected);
        }
    }
}
