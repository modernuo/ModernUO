namespace Server.Engines.Pathing.Cache;

public readonly struct StepMask(
    byte mask,
    sbyte destZn,
    sbyte destZne,
    sbyte destZe,
    sbyte destZse,
    sbyte destZs,
    sbyte destZsw,
    sbyte destZw,
    sbyte destZnw
)
{
    public readonly byte Mask = mask;
    public readonly sbyte DestZ_N = destZn;
    public readonly sbyte DestZ_NE = destZne;
    public readonly sbyte DestZ_E = destZe;
    public readonly sbyte DestZ_SE = destZse;
    public readonly sbyte DestZ_S = destZs;
    public readonly sbyte DestZ_SW = destZsw;
    public readonly sbyte DestZ_W = destZw;
    public readonly sbyte DestZ_NW = destZnw;

    public bool IsWalkable(Direction d) => (Mask & (1 << (int)d)) != 0;

    public sbyte GetDestZ(Direction d) => d switch
    {
        Direction.North => DestZ_N,
        Direction.Right => DestZ_NE,
        Direction.East  => DestZ_E,
        Direction.Down  => DestZ_SE,
        Direction.South => DestZ_S,
        Direction.Left  => DestZ_SW,
        Direction.West  => DestZ_W,
        Direction.Up    => DestZ_NW,
        _ => 0
    };
}
