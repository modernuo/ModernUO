namespace Server.BotAI.V2;

/// <summary>
/// Ground a single plan is to keep out of. Tactical and short-lived, never a statement about the world.
///
/// Two shapes, because the population produced exactly two reasons:
///
/// <para>
/// <b>One tile, because somebody is standing on it.</b> The measurement was unambiguous: (1371, 1477, 10)
/// is the gate to the Britain graveyard, and two bots parked on it accounted for seventy-seven refused
/// steps in two minutes — twice, on two different days, with two different bots. A bot at an auction or
/// going through a corpse can stand still for minutes, and if it happens to be in a doorway then nobody
/// gets through the doorway. Excluding the tile from the next plan is the difference between walking round
/// a person and asking them to move for the twentieth time.
/// </para>
///
/// <para>
/// <b>One square, because something in it nearly killed the bot.</b> This is what makes a route bend
/// without the destination moving: the goal is untouchable, the way to it is not.
/// </para>
///
/// <para>
/// <b>And a few squares of ground priced rather than shut, because something the bot cannot beat stands in them (03.10.2026,
/// sight13).</b> The reach of a creature the walker has seen (<see cref="BotSight"/>): a step into it costs more, so the
/// search goes round when a way round is not dear, and through when it is the only way — the walk itself then waits or turns
/// back. Never a wall: the goal may stand inside one.
/// </para>
///
/// <para>
/// Neither may ever be recorded as knowledge about the ground. A search that failed because of an avoid
/// has proved nothing — which is why <see cref="BotPath"/> refuses to write a pocket into
/// <see cref="BotReach"/> from any search that carried one.
/// </para>
/// </summary>
public readonly struct BotAvoid
{
    private readonly int _tileX;
    private readonly int _tileY;
    private readonly int _x1;
    private readonly int _y1;
    private readonly int _x2;
    private readonly int _y2;
    private readonly Map _towns;
    private readonly BotReaches _reaches;

    private BotAvoid(int tileX, int tileY, int x1, int y1, int x2, int y2, Map towns = null, BotReaches reaches = null)
    {
        _tileX = tileX;
        _tileY = tileY;
        _x1 = x1;
        _y1 = y1;
        _x2 = x2;
        _y2 = y2;
        _towns = towns;
        _reaches = reaches is { Count: > 0 } ? reaches : null;

        HasTile = tileX >= 0;
        HasSquare = x2 >= x1 && x1 >= 0;
    }

    /// <summary>Nothing excluded. The ordinary case, and the only one that may teach the reach ledger.</summary>
    public static BotAvoid None => new(-1, -1, -1, -1, -1, -1);

    public bool HasTile { get; }

    public bool HasSquare { get; }

    /// <summary>True when this plan is unconstrained, and therefore its refusal means something.</summary>
    public bool Empty => !HasTile && !HasSquare && _towns == null && _reaches == null;

    /// <summary>Whether this plan prices the reach of creatures the walker has seen. See <see cref="Reaches"/>.</summary>
    public bool HasReaches => _reaches != null;

    /// <summary>Whether this plan keeps out of the guarded towns. See <see cref="Towns"/>.</summary>
    public bool HasTowns => _towns != null;

    /// <summary>The tile somebody is standing on.</summary>
    public static BotAvoid Tile(Point3D where) => new(where.X, where.Y, -1, -1, -1, -1);

    /// <summary>A patch of ground to route around, inclusive of its edges.</summary>
    public static BotAvoid Square(int x1, int y1, int x2, int y2) => new(-1, -1, x1, y1, x2, y2);

    /// <summary>This exclusion plus a tile, for a plan that has both reasons.</summary>
    public BotAvoid And(Point3D tile) => new(tile.X, tile.Y, _x1, _y1, _x2, _y2, _towns, _reaches);

    /// <summary>This exclusion plus a square.</summary>
    public BotAvoid And(int x1, int y1, int x2, int y2) => new(_tileX, _tileY, x1, y1, x2, y2, _towns, _reaches);

    /// <summary>This exclusion plus the reach of creatures the walker has seen, priced per step. See <see cref="BotSight"/>.</summary>
    public BotAvoid Reaches(BotReaches reaches) => new(_tileX, _tileY, _x1, _y1, _x2, _y2, _towns, reaches);

    /// <summary>What a step onto (x, y) costs over the ground's own price, in the search's units; nought off every reach.</summary>
    public int Penalty(int x, int y) => _reaches?.Penalty(x, y) ?? 0;

    /// <summary>Whether (x, y) lies in the reach of a creature this plan prices.</summary>
    public bool InReach(int x, int y) => _reaches?.Within(x, y, 0) == true;

    /// <summary>
    /// This exclusion plus every guarded town on the map: the road of a red, whom the guards kill on sight. Asked of the
    /// engine's regions tile by tile, so the road bends where a ward's edge really is. See <c>BotOutlaw.Road</c>.
    /// </summary>
    public BotAvoid Towns(Map map) => new(_tileX, _tileY, _x1, _y1, _x2, _y2, map, _reaches);

    public bool Blocks(int x, int y)
    {
        if (HasTile && _tileX == x && _tileY == y)
        {
            return true;
        }

        if (HasSquare && x >= _x1 && x <= _x2 && y >= _y1 && y <= _y2)
        {
            return true;
        }

        return _towns != null && BotOutlaw.Guarded(_towns, x, y);
    }
}
