using System;
using System.Collections.Generic;
using Server.Items;

namespace Server.BotAI.V2;

/// <summary>
/// One guild's colony on an island: the square it landed on, the store its colonists fill, who is on its land and since
/// when, and what its ship has carried home. See <see cref="BotColonies"/>.
/// </summary>
public sealed class BotColony
{
    /// <summary>The guild's name, as everything on this shard keys a guild.</summary>
    public string Guild;

    /// <summary>The island's dock: where the ship berths and where the run home sets out from.</summary>
    public string Island;

    public Map Map;

    /// <summary>The middle of the square the colony stands on, on standing ground: where the store is and the claim is made.</summary>
    public Point3D Site;

    /// <summary>The store: a chest on the site whose contents are the guild's. Resolved from <see cref="StoreSerial"/> after a restart.</summary>
    public Container Store;

    public Serial StoreSerial;

    /// <summary>When it was founded, by the wall clock: kept across a restart.</summary>
    public DateTime FoundedAt;

    /// <summary>When it was founded or read back, as a tick: for this session's clocks.</summary>
    public long Since;

    /// <summary>Whether the square has been the guild's at one of the looks. Until then it is a camp, not ground.</summary>
    public bool Claimed;

    /// <summary>When the claim was last tried, for the retry clock.</summary>
    public long ClaimTried;

    public int ClaimTries;

    /// <summary>When a colonist was last seen on the colony's land.</summary>
    public long LastSeen;

    /// <summary>When the last run home set out (or the colony was founded).</summary>
    public long LastRun;

    /// <summary>Who of the guild is on the colony's land, and since when (this session's tick). The shift clock.</summary>
    public readonly Dictionary<Serial, long> Shift = [];

    /// <summary>Colonists on the land at the last look.</summary>
    public int Colonists;

    // ---- What came of it: kept across a restart -------------------------------------------------------------------------

    public long OreIn;

    public long IngotsIn;

    public long LogsIn;

    /// <summary>Colonist-milliseconds spent on the colony's land, for the yield per hour.</summary>
    public long ColonistMs;

    public long ColonistMinutes => ColonistMs / 60000;

    public long Runs;

    public long CargoUnits;

    public long CargoGold;

    public long ChestGold;

    public long CrewGold;

    public long CaptainGold;

    public long Plundered;

    public string Last;

    public (int Map, int X, int Y) Key => BotQuad.Key(Map, Site);

    /// <summary>Units of goods in the store now: ore, bars, logs and boards.</summary>
    public int Stock()
    {
        if (Store is not { Deleted: false })
        {
            return 0;
        }

        return Store.GetAmount(typeof(BaseOre)) + Store.GetAmount(typeof(BaseIngot)) + Store.GetAmount(typeof(Log))
               + Store.GetAmount(typeof(Board));
    }

    /// <summary>What the colony has put in its store per colonist-hour, ore and bars and wood together.</summary>
    public double YieldPerHour => ColonistMs <= 0 ? 0.0 : (OreIn + IngotsIn + LogsIn) * 3600000.0 / ColonistMs;

    public override string ToString() => $"{Guild}'s colony on {Island} at ({Site.X}, {Site.Y})";
}
