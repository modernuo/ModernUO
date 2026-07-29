using System.Collections.Generic;
using Server;
using Server.Factions;
using Xunit;

namespace UOContent.Tests;

/// <summary>
/// PlayerState.Rank is read from GetProperties, so it must stay a plain field read. These pin what
/// that requires: the rank is never null, and it is correct without anyone having read it first.
/// </summary>
[Collection("Sequential UOContent Tests")]
public class FactionRankTests
{
    // The faction ctor builds its own Definition, so no world state is needed.
    private static Faction NewFaction() => new CouncilOfMages();

    private static PlayerState AddMember(Faction faction, List<PlayerState> owner)
    {
        var state = new PlayerState(new Mobile(), faction, owner);
        owner.Add(state);
        return state;
    }

    [Fact]
    public void Rank_IsPopulatedBeforeAnythingReadsIt()
    {
        var faction = NewFaction();
        var state = new PlayerState(new Mobile(), faction, []);

        // Nothing recomputes on read, so the ctor must leave a usable value or Rank.Title NREs.
        Assert.NotNull(state.Rank);
        Assert.NotNull(state.Rank.Title);
    }

    [Fact]
    public void Rank_IsTheLowestRank_ForAnUnrankedMember()
    {
        var faction = NewFaction();
        var owner = new List<PlayerState>();
        var a = AddMember(faction, owner);
        var b = AddMember(faction, owner);

        a.UpdateRank();
        b.UpdateRank();

        var lowest = faction.Definition.Ranks[^1];

        Assert.Equal(lowest.Rank, a.Rank.Rank);
        Assert.Equal(lowest.Rank, b.Rank.Rank);
    }

    [Fact]
    public void SettingRankIndex_UpdatesRankWithoutAnyoneReadingIt()
    {
        var faction = NewFaction();
        var owner = new List<PlayerState>();
        var top = AddMember(faction, owner);
        var bottom = AddMember(faction, owner);

        faction.ZeroRankOffset = 2;

        top.RankIndex = 0;
        bottom.RankIndex = 1;

        // No read triggered these, yet the ordering is reflected.
        Assert.True(top.Rank.Rank > bottom.Rank.Rank);
    }

    [Fact]
    public void ReadingRank_IsStableAndSideEffectFree()
    {
        var faction = NewFaction();
        var owner = new List<PlayerState>();
        var state = AddMember(faction, owner);

        faction.ZeroRankOffset = 1;
        state.RankIndex = 0;

        var first = state.Rank;
        var second = state.Rank;

        Assert.Same(first, second);
    }

    [Fact]
    public void RankIndexOutOfSyncWithZeroRankOffset_StillResolvesARank()
    {
        var faction = NewFaction();
        var owner = new List<PlayerState>();
        var a = AddMember(faction, owner);
        AddMember(faction, owner);

        // A negative percent used to match no rank at all, leaving Rank null.
        faction.ZeroRankOffset = 1;
        a.RankIndex = 5;

        Assert.NotNull(a.Rank);
    }
}
