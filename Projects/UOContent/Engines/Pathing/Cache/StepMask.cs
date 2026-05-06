namespace Server.Engines.Pathing.Cache;

/// <summary>
/// Per-cell, per-direction static walkability data baked by <see cref="StepProbe"/>
/// and stored by <see cref="StepCache"/>. WalkMask + WalkZ_* applies under default-walker
/// rules (cantWalk=false, canSwim=false). WetMask + SwimZ_* applies under swim-only rules
/// (cantWalk=true, canSwim=true). Algorithms layer the right rules per mobile.
/// </summary>
public readonly struct StepMask(
    byte walkMask,
    byte wetMask,
    sbyte walkZN,
    sbyte walkZNE,
    sbyte walkZE,
    sbyte walkZSE,
    sbyte walkZS,
    sbyte walkZSW,
    sbyte walkZW,
    sbyte walkZNW,
    sbyte swimZN,
    sbyte swimZNE,
    sbyte swimZE,
    sbyte swimZSE,
    sbyte swimZS,
    sbyte swimZSW,
    sbyte swimZW,
    sbyte swimZNW,
    CacheHitKind hitKind = CacheHitKind.Hit
)
{
    public readonly byte WalkMask = walkMask;
    public readonly byte WetMask = wetMask;
    public readonly sbyte WalkZ_N = walkZN;
    public readonly sbyte WalkZ_NE = walkZNE;
    public readonly sbyte WalkZ_E = walkZE;
    public readonly sbyte WalkZ_SE = walkZSE;
    public readonly sbyte WalkZ_S = walkZS;
    public readonly sbyte WalkZ_SW = walkZSW;
    public readonly sbyte WalkZ_W = walkZW;
    public readonly sbyte WalkZ_NW = walkZNW;
    public readonly sbyte SwimZ_N = swimZN;
    public readonly sbyte SwimZ_NE = swimZNE;
    public readonly sbyte SwimZ_E = swimZE;
    public readonly sbyte SwimZ_SE = swimZSE;
    public readonly sbyte SwimZ_S = swimZS;
    public readonly sbyte SwimZ_SW = swimZSW;
    public readonly sbyte SwimZ_W = swimZW;
    public readonly sbyte SwimZ_NW = swimZNW;
    public readonly CacheHitKind HitKind = hitKind;

    /// <summary>
    /// True when the cache produced a usable answer (Hit / Miss_NotBuilt / Miss_DirtyRebuild).
    /// False on Fallthrough_*, in which case the caller must use the slow path for this cell.
    /// </summary>
    public bool IsHit => HitKind <= CacheHitKind.Miss_DirtyRebuild;

    public bool IsWalkable(Direction d) => (WalkMask & (1 << (int)d)) != 0;
    public bool IsSwimmable(Direction d) => (WetMask & (1 << (int)d)) != 0;

    public sbyte GetWalkZ(Direction d) => d switch
    {
        Direction.North => WalkZ_N,
        Direction.Right => WalkZ_NE,
        Direction.East  => WalkZ_E,
        Direction.Down  => WalkZ_SE,
        Direction.South => WalkZ_S,
        Direction.Left  => WalkZ_SW,
        Direction.West  => WalkZ_W,
        Direction.Up    => WalkZ_NW,
        _ => 0
    };

    public sbyte GetSwimZ(Direction d) => d switch
    {
        Direction.North => SwimZ_N,
        Direction.Right => SwimZ_NE,
        Direction.East  => SwimZ_E,
        Direction.Down  => SwimZ_SE,
        Direction.South => SwimZ_S,
        Direction.Left  => SwimZ_SW,
        Direction.West  => SwimZ_W,
        Direction.Up    => SwimZ_NW,
        _ => 0
    };
}
