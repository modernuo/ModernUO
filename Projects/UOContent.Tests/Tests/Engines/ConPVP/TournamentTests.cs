using System;
using Server;
using Server.Engines.ConPVP;
using Server.Gumps;
using Server.Network;
using Server.Tests.Network;
using Xunit;

namespace Server.Engines.ConPVP.Tests;

[Collection("Sequential UOContent Tests")]
public class TournamentTests
{
    // Button ids are 1 + type + index * 7.
    [Theory]
    [InlineData(8, 3)]  // Participants, then the first participant
    [InlineData(15, 4)] // Rounds, then the first round
    public void TestBracketListPagesOpen(int listButton, int firstRowButton)
    {
        var ns = PacketTestUtilities.CreateTestNetState();
        var from = new Mobile((Serial)0x1040u);
        from.DefaultMobileInit();
        from.NetState = ns;
        ns.Mobile = from;

        try
        {
            var tourney = new Tournament();
            tourney.Participants.Add(new TourneyParticipant(from));
            tourney.Pyramid.Levels.Add(new PyramidLevel());

            TournamentBracketGump.DisplayTo(from, tourney, TourneyBracketGumpType.Index);
            var gump = ns.FindGump<TournamentBracketGump>();

            gump.OnResponse(ns, Button(listButton));
            gump.OnResponse(ns, Button(firstRowButton));

            Assert.Same(gump, ns.FindGump<TournamentBracketGump>());
        }
        finally
        {
            from.NetState = null;
            from.Delete();
            ns.Dispose();
        }
    }

    private static RelayInfo Button(int buttonId) =>
        new(buttonId, ReadOnlySpan<int>.Empty, ReadOnlySpan<ushort>.Empty, ReadOnlySpan<Range>.Empty, ReadOnlySpan<byte>.Empty);

    [Fact]
    public void TestSuddenDeathSurvivesSaveAndLoad()
    {
        var tourney = new Tournament
        {
            SuddenDeath = TimeSpan.FromMinutes(3),
            SignupPeriod = TimeSpan.FromMinutes(7)
        };

        var writer = new BufferWriter(true);
        tourney.Serialize(writer);

        var buffer = new byte[writer.Position];
        writer.Buffer.AsSpan(0, (int)writer.Position).CopyTo(buffer);

        var copy = new Tournament(new BufferReader(buffer));

        Assert.Equal(TimeSpan.FromMinutes(3), copy.SuddenDeath);
        Assert.Equal(TimeSpan.FromMinutes(7), copy.SignupPeriod);
    }
}
