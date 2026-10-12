using System;

namespace Server.BotAI.V2;

/// <summary>
/// The numbers a build works by, copied off the dials when its input is gathered, so a build running off the loop never reads
/// a property the door may be changing under it.
/// </summary>
public sealed class BotZoneSettings
{
    public int DeclaredMost { get; init; }

    public int LiveRadius { get; init; }

    public int LooseRadius { get; init; }

    public int PassiveRadius { get; init; }

    public int WaterRadius { get; init; }

    public int WaterGroup { get; init; }

    public int ObservedMargin { get; init; }

    public double ObservedWins { get; init; }

    public int PerceptionMost { get; init; }

    public int WaterMeleeReach { get; init; }

    public int WaterCasterReach { get; init; }

    public int RootedReach { get; init; }

    public int ConvergeTiles { get; init; }

    public double Secondary { get; init; }

    public double DeadlyVsBot { get; init; }

    public double EmptyWeight { get; init; }

    public int DeathsPromote { get; init; }

    public int DeathsWindowHours { get; init; }

    public int ConfidentSamples { get; init; }

    public int ClearLeastLive { get; init; }

    public int MostPoints { get; init; }

    public int ClearMostPoints { get; init; }

    public int SeaMostPoints { get; init; }

    public int MostRings { get; init; }

    public int MostKinds { get; init; }

    public double SimplifyTiles { get; init; }

    public int SmoothPasses { get; init; }

    /// <summary>
    /// The locality of a zone (02.10.2026, build 389). Two spawners are one zone when their discs touch within
    /// <see cref="JoinGap"/> tiles and the zone they would make is no wider than <see cref="ZoneMostTiles"/>; a creature farther
    /// than its spawner's range plus <see cref="StrayBeyond"/> from home is a wanderer and no part of its spawner's patrol; an
    /// observed cell belongs to the nearest spawner whose range plus <see cref="ObservedSlack"/> covers it, and to nobody otherwise.
    /// Defaults here, so an input built without the dials (the harness) still groups.
    /// </summary>
    public int JoinGap { get; init; } = 8;

    public int ZoneMostTiles { get; init; } = 192;

    public int StrayBeyond { get; init; } = 16;

    public int ObservedSlack { get; init; } = 8;

    /// <summary>
    /// How far past its declared range a camp's patrol may grow on what was seen (08.10.2026), and the most cells one
    /// wanderer's trail paints. Nought: neither grows. See <see cref="BotZones.ReachBeyond"/> and <see cref="BotZones.WanderMostCells"/>.
    /// </summary>
    public int ReachBeyond { get; init; }

    public int WanderMostCells { get; init; }
}

/// <summary>A creature kind as the build sees it: plain values, no type and no creature.</summary>
public readonly record struct BotZoneKindIn(
    string Name, double Power, bool Aggressive, bool Caster, int Perception, bool Water, bool Rooted, bool Swims, int Strike = 0
)
{
    /// <summary>Whether this kind, kept by a spawner standing on wet or dry ground, is the sea's (07.10.2026). See BotZoneKind.Swims.</summary>
    public bool WaterAt(bool wet) => Water || Swims && wet;
}

/// <summary>One haunt as the build sees it.</summary>
public sealed class BotZoneHauntIn
{
    public int X { get; init; }

    public int Y { get; init; }

    public int Z { get; init; }

    public int Radius { get; init; }

    public int Count { get; init; }

    public bool Running { get; init; }

    public bool InTown { get; init; }

    /// <summary>The spawner's tile is water: a swimmer it keeps is the sea's (07.10.2026).</summary>
    public bool Wet { get; init; }

    /// <summary>Kind indices of the entries, and how many of each a full spawner is expected to hold.</summary>
    public int[] Kinds { get; init; } = [];

    public double[] Expected { get; init; } = [];

    public double Confidence { get; init; }

    public double AwakeSeconds { get; init; }

    public long Samples { get; init; }

    public int ObservedRange { get; init; }

    /// <summary>
    /// The high percentile of how far from home its creatures were seen awake and calm (no chase), capped at the range plus
    /// <see cref="BotZoneSettings.ReachBeyond"/>; -1 when too little was seen (08.10.2026). See <see cref="BotHaunt.ReachAt"/>.
    /// </summary>
    public int Reach { get; init; } = -1;

    public DateTime LastAwake { get; init; }
}

/// <summary>
/// One living creature where it stands now, with its own traits (an instance may differ from its kind). <see cref="Event"/>: the
/// index of the event it belongs to, or -1. <see cref="TrailFrom"/> and <see cref="TrailCount"/>: its recent places in
/// <see cref="BotZoneInput.Trail"/>, newest first (08.10.2026).
/// </summary>
public readonly record struct BotZoneLiveIn(
    int X, int Y, int Kind, int Haunt, bool Awake, float Power, bool Aggressive, bool Caster, int Perception, bool Water, bool Rooted,
    int Strike = 0, int Event = -1, int TrailFrom = 0, int TrailCount = 0
);

/// <summary>
/// An event's creatures as one temporary zone (08.10.2026): what it is, its field (a point and the leash round it), painted while
/// any of them lives. See <see cref="BotZones"/>' gather.
/// </summary>
public readonly record struct BotZoneEventIn(string Label, int X, int Y, int Radius);

/// <summary>A named place: a town (a point) or a dungeon (a box).</summary>
public readonly record struct BotZonePlaceIn(string Name, int X1, int Y1, int X2, int Y2);

/// <summary>
/// Everything one build needs, gathered on the loop and never touched by it again: plain values and fresh arrays, the
/// previous state (immutable) for matching numbers, and the one back buffer no published state refers to.
/// </summary>
public sealed class BotZoneInput
{
    public Map Map { get; init; }

    public int Width { get; init; }

    public int Height { get; init; }

    public BotZoneSettings Settings { get; init; }

    public BotZoneKindIn[] Kinds { get; init; } = [];

    public BotZoneHauntIn[] Haunts { get; init; } = [];

    public BotZoneLiveIn[] Live { get; init; } = [];

    /// <summary>The wanderers' recent places, x and y in turn; each living creature points into it. See <see cref="BotZoneLiveIn"/>.</summary>
    public int[] Trail { get; init; } = [];

    /// <summary>The events standing now whose creatures live. See <see cref="BotZoneEventIn"/>.</summary>
    public BotZoneEventIn[] Events { get; init; } = [];

    /// <summary>Observed cells (index into the raster) and their faded weight of awake sightings.</summary>
    public int[] Observed { get; init; } = [];

    public float[] ObservedWeight { get; init; } = [];

    /// <summary>Where bots died within the window.</summary>
    public (int X, int Y)[] Deaths { get; init; } = [];

    public BotZonePlaceIn[] Towns { get; init; } = [];

    public BotZonePlaceIn[] Deeps { get; init; } = [];

    public double RefBotPower { get; init; }

    public BotZoneState Previous { get; init; }

    public ushort[] Buffer { get; init; }

    /// <summary>The back buffer for the fight raster (<see cref="BotZoneState.Fight"/>); null, and the build allocates one.</summary>
    public ushort[] FightBuffer { get; init; }

    /// <summary>
    /// Per cell, nought or one: whether the cell lies in a guarded region, where the guards kill what attacks and no zone paints.
    /// Null: nothing guarded (the harness, or the dial off).
    /// </summary>
    public byte[] Guarded { get; init; }

    public DateTime At { get; init; }

    /// <summary>Whether this build runs off the loop; carried into the state it publishes, for the log.</summary>
    public bool OffLoop { get; init; }

    /// <summary>The world this was gathered in; a build finishing after a reload is recognised by it and dropped.</summary>
    public int Generation { get; init; }
}
