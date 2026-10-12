using System;

namespace Server.BotAI.V2;

/// <summary>
/// How dangerous a piece of ground is to a bot walking across it, in the four words the planners, the hunt and the
/// dashboard share.
///
/// <para>
/// <b>Ordered, and the order is the meaning.</b> A comparison (<c>level &gt;= Hostile</c>) is how every caller asks "may
/// something attack me here", so the values must never be renumbered.
/// </para>
/// </summary>
public enum BotZoneLevel : byte
{
    /// <summary>Nothing here attacks first: passive animals or nothing at all. A hunting ground, or a safe passage.</summary>
    Clear = 0,

    /// <summary>
    /// Potentially dangerous: the aggro fringe round a hostile patrol, where its creatures can see a bot and come for it. A
    /// cell's answer, never a zone's own level: a zone is its patrol, and its fringe is the band round it. A haunt whose
    /// creatures are all dead now is still <see cref="Hostile"/> at a reduced threat (<see cref="BotZone.Empty"/>), because
    /// the spawner fills it again within minutes.
    /// </summary>
    Edge = 1,

    /// <summary>Inside the patrol area of creatures that attack on sight.</summary>
    Hostile = 2,

    /// <summary>Hostile, and far stronger than a typical bot — or a company — can take (<see cref="BotZones.DeadlyVsBot"/>).</summary>
    Deadly = 3
}

/// <summary>
/// One zone as the last build left it: where it is, what lives in it, how strong that is against a bot, how sure the shard
/// is of its shape, and who has died in it. Immutable: a build makes new ones and the old ones are dropped whole, so a zone
/// read by a planner on the loop, or by the dashboard's thread, never changes under the reader.
/// </summary>
public sealed class BotZone
{
    /// <summary>
    /// Stable across builds: a zone that grows, shrinks or drifts keeps its number, matched by overlap with the zone that was
    /// there before (<see cref="BotZoneBuild"/>). New only when nothing of the old map lies under it.
    /// </summary>
    public int Id { get; init; }

    public BotZoneLevel Level { get; init; }

    /// <summary>Words for it: what lives there and near what. "orcs and orcish mages, NE of Britain".</summary>
    public string Label { get; init; }

    /// <summary>One sentence: why this level. "14 attack on sight (3 casters); up to 6 can close at once: ×5.4 a bot".</summary>
    public string Why { get; init; }

    /// <summary>
    /// Nobody alive in it now: its threat is what its spawners keep when full, weighed by <see cref="BotZones.EmptyWeight"/>,
    /// because they fill it again within minutes.
    /// </summary>
    public bool Empty { get; init; }

    /// <summary>A sea zone: creatures that cannot leave the water. Never merged with the land, and never on the tour.</summary>
    public bool Water { get; init; }

    /// <summary>
    /// Wanderers: attackers found farther from their spawner than its range (a chase, a lure), or with no spawner at all, drawn
    /// where they stand and nowhere else. Real danger, no patrol: never on the tour, and the fight here is theirs alone.
    /// </summary>
    public bool Stray { get; init; }

    /// <summary>
    /// An event's creatures (08.10.2026): an invasion's waves, a relic's warden, The Shadow on a quay. Painted while any of them
    /// lives and gone with the last; a wanderer in every other way (<see cref="Stray"/> is set too), so never on the tour.
    /// </summary>
    public bool Event { get; init; }

    /// <summary>The middle of the patrol area, in tiles.</summary>
    public int CenterX { get; init; }

    public int CenterY { get; init; }

    /// <summary>The patrol area's box, in tiles, ends inclusive.</summary>
    public int X1 { get; init; }

    public int Y1 { get; init; }

    public int X2 { get; init; }

    public int Y2 { get; init; }

    /// <summary>Cells of patrol (core) and of aggro fringe round it, four tiles to a side each.</summary>
    public int CoreCells { get; init; }

    public int AggroCells { get; init; }

    /// <summary>Living creatures in it now, and how many its spawners keep when full.</summary>
    public int Live { get; init; }

    public int Max { get; init; }

    /// <summary>Of the living, those that attack on sight, and the casters among them.</summary>
    public int Aggressive { get; init; }

    public int Casters { get; init; }

    /// <summary>The most of them standing within one convergence reach of each other: how many can come at once.</summary>
    public int Converge { get; init; }

    /// <summary>What lives here by kind, strongest share first.</summary>
    public BotZoneKindCount[] Kinds { get; init; } = [];

    /// <summary>The kinds left out of <see cref="Kinds"/> by the cap.</summary>
    public int OtherKinds { get; init; }

    /// <summary>Sum of the living attackers' strength (<see cref="BotThreat.Power"/>), the strongest one, and the fight met.</summary>
    public double PowerTotal { get; init; }

    public double Worst { get; init; }

    /// <summary>
    /// The fight a bot meets at the worst place in it: the strongest in full and the rest that can converge on it at
    /// <see cref="BotThreat.Secondary"/>, taken round every living attacker. The declared fight when none is alive.
    /// </summary>
    public double Threat { get; init; }

    /// <summary><see cref="Threat"/> over the median living bot's strength: above one, a lone bot of middling strength loses.</summary>
    public double VsBot { get; init; }

    /// <summary>
    /// The fight its spawners promise at one place when full: the strongest kind in full and the share of the rest that a
    /// convergence disc holds of the spawner's range, at the secondary weight. The floor of the fight raster inside it, weighed
    /// by <see cref="BotZones.EmptyWeight"/>, where nobody stands near now.
    /// </summary>
    public double Declared { get; init; }

    /// <summary>Tiles: the spawners' declared walking range, the farthest a creature of it was seen awake from home, and the aggro reach.</summary>
    public int DeclaredRange { get; init; }

    public int ObservedRange { get; init; }

    public int AggroRange { get; init; }

    /// <summary>0 to 1: how far the shape is learned rather than declared. See <see cref="BotHaunt.Confidence"/>.</summary>
    public double Confidence { get; init; }

    /// <summary>Awake sightings, awake seconds of its ground, and when a creature of it was last seen awake (never: min).</summary>
    public long Samples { get; init; }

    public double AwakeSeconds { get; init; }

    public DateTime LastSeen { get; init; }

    /// <summary>Bots killed inside it or in its fringe in the deaths window.</summary>
    public int Deaths { get; init; }

    /// <summary>Spawners feeding it.</summary>
    public int Haunts { get; init; }

    /// <summary>Where the tour stands to watch it: a spawner of it nearest the middle, a real tile with its own height.</summary>
    public Point3D Stand { get; init; }

    /// <summary>Whether <see cref="Stand"/> is a spawner's own tile (a height the engine chose) rather than a guess.</summary>
    public bool StandIsReal { get; init; }

    /// <summary>Its patrol cells (raster indices), for the next build to match numbers on. Never written after the build.</summary>
    internal int[] CellList { get; init; }

    public override string ToString() => $"zone {Id} ({BotZones.Word(Level)}, {Label})";
}

/// <summary>One kind of creature in a zone.</summary>
public readonly record struct BotZoneKindCount(string Kind, int Live, int Max, double Power, bool Aggressive, bool Caster);

/// <summary>
/// Everything one build published, swapped in whole through <see cref="BotZones"/>'s one volatile reference. The raster is
/// the answer to "what is here", read per node by the planners; the zones are the answer to "what is that".
/// </summary>
public sealed class BotZoneState
{
    /// <summary>Four tiles to a cell, as the shift the queries use.</summary>
    public const int Shift = 2;

    public const int Cell = 1 << Shift;

    /// <summary>A cell's owner is in the low fifteen bits (zone index + 1); this bit says the cell is fringe, not patrol.</summary>
    public const ushort Fringe = 0x8000;

    public const ushort IndexMask = 0x7FFF;

    public Map Map { get; init; }

    /// <summary>The raster's size in cells.</summary>
    public int Width { get; init; }

    public int Height { get; init; }

    /// <summary>Per cell: 0 for nobody's ground, else the owning zone's index + 1, with <see cref="Fringe"/> set on aggro fringe.</summary>
    public ushort[] Cells { get; init; }

    /// <summary>
    /// Per cell, in units of <see cref="FightUnit"/>: the fight a walker meets <em>there</em> — the strongest living attacker
    /// within <see cref="BotZones.ConvergeTiles"/> of the cell in full and the rest within it at the secondary weight, never
    /// below the owning zone's promised fight at one place (<see cref="BotZone.Declared"/>) weighed by
    /// <see cref="BotZones.EmptyWeight"/>. Nought on nobody's ground and on hunting grounds. Patrick, 02.10.2026: "the
    /// strength is what can close on one walker at a place, not the sum over a peninsula." Null before build 389.
    /// </summary>
    public ushort[] Fight { get; init; }

    public const int FightUnit = 4;

    public BotZone[] Zones { get; init; } = [];

    /// <summary>The next unused zone number, carried from build to build so a number is never given twice in a session.</summary>
    public int NextId { get; init; }

    public DateTime At { get; init; }

    public double RefBotPower { get; init; }

    public int Clear { get; init; }

    public int Edge { get; init; }

    public int Hostile { get; init; }

    public int Deadly { get; init; }

    public int WaterZones { get; init; }

    public int Spawners { get; init; }

    public int Creatures { get; init; }

    public int ObservedCells { get; init; }

    /// <summary>Zones that kept a number from the build before, and zones new in this one.</summary>
    public int Kept { get; init; }

    public int Born { get; init; }

    /// <summary>
    /// How local the map is (02.10.2026): cells painted as patrol or fringe by the land's attackers, the land's attackers' zones
    /// and their patrol cells and living attackers, wanderers' zones, the largest zone, the towns whose square lies under a
    /// patrol or a fringe, and the guarded cells a patrol or fringe would have covered and did not.
    /// </summary>
    public int PaintedCells { get; init; }

    public int LandZones { get; init; }

    public int LandCoreCells { get; init; }

    public int LandAggressive { get; init; }

    public int StrayZones { get; init; }

    /// <summary>
    /// What observation added (08.10.2026): spawners whose patrol grew past their declared range on what was seen, the event
    /// zones standing, and cells the wanderers' trails painted.
    /// </summary>
    public int GrownHaunts { get; init; }

    public int EventZones { get; init; }

    public int TrailCells { get; init; }

    public BotZone Largest { get; init; }

    public int TownsInside { get; init; }

    public int GuardedKept { get; init; }

    /// <summary>Deaths in the window, by where they fell on this build's map.</summary>
    public int DeathsDeadly { get; init; }

    public int DeathsHostile { get; init; }

    public int DeathsEdge { get; init; }

    public int DeathsClear { get; init; }

    /// <summary>The "zones" array of the dashboard's JSON, written by the build so the loop only splices it in.</summary>
    public byte[] ZonesUtf8 { get; init; } = "[]"u8.ToArray();

    /// <summary>Milliseconds: the whole build, the rings in it, and whether it ran off the loop.</summary>
    public double BuildMs { get; init; }

    public double ShapesMs { get; init; }

    /// <summary>Where the build's time went, phase by phase, in milliseconds: for the log.</summary>
    public string Phases { get; init; } = "";

    public bool OffLoop { get; init; }

    /// <summary>The world it was built for. See <see cref="BotZoneInput.Generation"/>.</summary>
    public int Generation { get; init; }

    /// <summary>The zone owning a cell, core or fringe, or null.</summary>
    public BotZone Owner(int x, int y)
    {
        var cx = x >> Shift;
        var cy = y >> Shift;

        if ((uint)cx >= (uint)Width || (uint)cy >= (uint)Height)
        {
            return null;
        }

        var v = Cells[cy * Width + cx];

        return v == 0 ? null : Zones[(v & IndexMask) - 1];
    }
}
