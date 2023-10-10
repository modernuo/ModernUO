using System;
using Server.Tests;
using Server.Tests.Network;
using Xunit;

namespace Server.Engines.MLQuests;

public class MLQuestPacketTests
{
    private class MockedRace : Race
    {
        public MockedRace(
            int raceID,
            int raceIndex,
            string name,
            string pluralName,
            int maleBody,
            int femaleBody,
            int maleGhostBody,
            int femaleGhostBody,
            Expansion requiredExpansion
        ) : base(
            raceID,
            raceIndex,
            name,
            pluralName,
            maleBody,
            femaleBody,
            maleGhostBody,
            femaleGhostBody,
            requiredExpansion
        )
        {
        }

        public override bool ValidateHair(bool female, int itemID) => throw new NotImplementedException();

        public override int RandomHair(bool female) => throw new NotImplementedException();

        public override bool ValidateFacialHair(bool female, int itemID) => throw new NotImplementedException();

        public override int RandomFacialHair(bool female) => throw new NotImplementedException();

        public override int ClipSkinHue(int hue) => throw new NotImplementedException();

        public override int RandomSkinHue() => throw new NotImplementedException();

        public override int ClipHairHue(int hue) => throw new NotImplementedException();

        public override int RandomHairHue() => throw new NotImplementedException();
    }

    [Theory]
    [InlineData(true, 1)]
    [InlineData(false, 2)]
    public void TestRaceChanger(bool female, int raceId)
    {
        var race = new MockedRace(
            raceId,
            0,
            "Test Race",
            "Test Races",
            0x1,
            0x2,
            0x3,
            0x4,
            Expansion.None
        );

        var expected = new RaceChanger(female, race).Compile();

        var ns = PacketTestUtilities.CreateTestNetState();
        ns.SendRaceChanger(female, race);

        var result = ns.SendPipe.Reader.AvailableToRead();
        AssertThat.Equal(result, expected);
    }

    [Fact]
    public void TestCloseRaceChanger()
    {
        var expected = new CloseRaceChanger().Compile();

        var ns = PacketTestUtilities.CreateTestNetState();
        ns.SendCloseRaceChanger();

        var result = ns.SendPipe.Reader.AvailableToRead();
        AssertThat.Equal(result, expected);
    }
}
