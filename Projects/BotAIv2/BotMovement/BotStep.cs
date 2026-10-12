using System;
using Server.Engines.Pathing.Cache;
using Server.Items;
using CalcMoves = Server.Movement.Movement;

namespace Server.BotAI.V2;

/// <summary>
/// One tile, and everything the engine knows about stepping off it.
///
/// <para>
/// This is the whole foundation, and the reason the rest of the movement code can be simple. The engine
/// caches, per tile, an eight-direction mask — "can I step this way, and what height do I land on" —
/// computed from the very rules <c>MovementImpl</c> enforces. It needs no mobile and has no distance
/// ceiling. The first version spent months building a compensation layer around the belief that no such
/// thing existed: a lattice of guessed legs, an arc sweep that walked at obstacles until something gave,
/// ring probes hunting for a way out of enclosures, and a memory of walls learned by bots leaning on
/// them for twenty-five seconds at a time. One night's log holds 1728 abandoned errands. A fence is not
/// discovered. It is seen.
/// </para>
///
/// <para>
/// <b>Everything here mirrors an engine rule exactly, and every deviation in the first version cost an
/// evening.</b> A planner that simplifies a rule is not faster — it is a planner whose paths the bot
/// cannot walk, and the difference shows up as a bot pressed against a railing insisting its route is
/// fine.
/// </para>
///
/// <para>
/// What is deliberately <b>not</b> here: creatures, dropped items and shut doors. They move. A planner
/// that treats a skeleton standing in a doorway as a wall teaches itself that the doorway is one. Those
/// belong to the moment the step is taken — see <see cref="BotWalk"/>.
/// </para>
/// </summary>
public static class BotStep
{
    /// <summary>How far up one step may take a bot, as the engine's movement code has it.</summary>
    public const int StepUp = 2;

    /// <summary>
    /// How far a floor may be from the bot's own height and still be somewhere it could plausibly walk
    /// to. Two storeys: enough for stairs, a cellar or a hillside, not enough for a roof.
    /// </summary>
    public const int StandingReach = 24;

    /// <summary>
    /// How high above or below a point to look for the floor that belongs to it.
    ///
    /// Narrow on purpose, and this narrowness is the whole of the roof fix. The first version asked
    /// <c>map.CanSpawnMobile(x, y, -128, 127, ...)</c> — the entire world vertically — and
    /// <c>CanSpawnMobile</c> answers with <em>any</em> standable surface it finds. In a built-up world
    /// that regularly means a roof. The point looked legal, the search planned through it, the engine
    /// refused, and the bill was twenty-five seconds and one abandoned errand <b>per approach
    /// direction</b>.
    /// </summary>
    public const int GroundReach = 8;

    /// <summary>
    /// Height bands the search folds Z into, exactly as the engine's own search does. Two floors of a
    /// building get separate cells; the ordinary jitter of walking over uneven ground does not multiply
    /// the search.
    /// </summary>
    private const int ZPlaneHeight = 20;

    /// <summary>The vertical space a standing person occupies.</summary>
    private const int PersonHeight = BotArrival.PersonHeight;

    /// <summary>
    /// One integer naming a standing cell. Used as the key by the search, by the reach ledger and by the
    /// item memo, so all three agree on what "the same place" means.
    /// </summary>
    public static int Cell(int x, int y, sbyte z) => ((z + 128) / ZPlaneHeight << 26) | (x << 13) | y;

    /// <summary>
    /// The eight-direction step mask for one tile: from the cache where it can answer, and from the same
    /// rules it was baked with where it cannot.
    ///
    /// Correctness never depends on the cache being warm — a cold shard is only slower. That property is
    /// worth stating because it is what makes this trustworthy at boot, when nothing is baked yet and the
    /// first bots are already walking.
    /// </summary>
    public static StepMask Mask(Map map, int x, int y, sbyte z)
    {
        var lookup = StepCache.Instance.TryGetMask(map, x, y, z);

        if (lookup.IsHit)
        {
            // Inside the cache's own tolerance, and still wrong.
            //
            // A tile is baked at one standing height and served to any query within a step of it. Sound
            // for the engine, whose search tracks the height it baked; not sound here, because
            // reachability flips exactly at a step — climbing four units is legal from Z=2 and illegal
            // from Z=0, and both queries are inside the tolerance. Bots at the graveyard gate were handed
            // a diagonal onto a Z=3 kerb, believed it, and failed that step for ever. The plan was right,
            // about a bot standing two units higher than this one.
            if (!ClimbsTooHigh(lookup, z))
            {
                return lookup;
            }

            return StepProbe.ComputeMaskAt(map, x, y, z);
        }

        switch (lookup.HitKind)
        {
            case CacheHitKind.Fallthrough_OffMap:
                {
                    return default;
                }
            case CacheHitKind.Fallthrough_Multi:
                {
                    // A house or a boat covers this tile. Its own cache always has an answer, and that is
                    // what lets bots walk indoors at all.
                    return MultiMaskCache.Instance.GetMask(map, x, y, z);
                }
            default:
                {
                    // Chunk not built yet, stacked floors, or a query height the bake does not cover. The
                    // prober built the cache in the first place, so this is the same answer, computed
                    // rather than remembered.
                    return StepProbe.ComputeMaskAt(map, x, y, z);
                }
        }
    }

    /// <summary>
    /// Whether a cached answer offers a climb no bot standing at <paramref name="z"/> could make — the
    /// symptom of a tile baked from a different height than the one being asked about. Falling any
    /// distance is legal, so only upward steps are suspect.
    /// </summary>
    private static bool ClimbsTooHigh(in StepMask mask, sbyte z)
    {
        var walk = mask.WalkMask;

        for (var d = 0; d < 8; d++)
        {
            if ((walk & (1 << d)) != 0 && mask.GetWalkZ((Direction)d) - z > StepUp)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Whether an item standing on a tile keeps a bot off it. Mirrors the item half of the engine's own
    /// obstacle test, with one deliberate exception for doors.
    ///
    /// <para>
    /// <b>Items are not optional, and leaving them out was the single most expensive omission in the
    /// first version.</b> <see cref="StepCache"/> bakes land and statics. This shard's world is built
    /// with <c>[Decorate</c> — that is, out of <em>items</em>: the graveyard railing, the headstones, the
    /// crates. The planner confidently drew straight lines through precisely the fences the whole
    /// exercise existed for, and the bot then failed the step and abandoned the errand, insisting the
    /// route was sound.
    /// </para>
    ///
    /// <para>
    /// <b>A shut door is not a wall</b>, because a bot opens it. Counting one would declare the inside of
    /// every building in the world unreachable and no bot would enter a bank again. A locked one is a
    /// wall, because it cannot — and pretending otherwise routes the whole population through the back
    /// of a shop it has no key to, over and over.
    /// </para>
    /// </summary>
    /// <param name="doorsShut">
    /// When true, even an unlocked shut door counts as a wall. This is how "is this spot out on the
    /// street" gets answered — without it, a bot asked for somewhere outdoors is handed a point in
    /// somebody's pantry.
    /// </param>
    public static bool BlockedByItems(Map map, int x, int y, sbyte z, bool doorsShut = false)
    {
        var ourTop = z + PersonHeight;

        foreach (var item in map.GetItemsAt(x, y))
        {
            var data = item.ItemData;

            if (!data.ImpassableSurface)
            {
                continue;
            }

            var id = item.ItemID & TileData.MaxItemValue;

            if (data.Door || id is 0x692 or 0x846 or 0x873 || id is >= 0x6F5 and <= 0x6F6)
            {
                if (item is BaseDoor { Locked: true, Open: false })
                {
                    return true;
                }

                if (doorsShut && item is BaseDoor { Open: false })
                {
                    return true;
                }

                continue;
            }

            var itemZ = item.Z;

            if (itemZ + data.CalcHeight > z && ourTop > itemZ)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Whether one of the two tiles flanking a diagonal is blocked, at the height a bot would land on it.
    ///
    /// <para>
    /// The engine refuses a player a diagonal unless <b>both</b> flanking tiles pass the full test — and
    /// full includes items. A half version of this rule, checking statics only, meant that every diagonal
    /// past a headstone failed. In a graveyard made of decorations that is a diagonal every few tiles.
    /// </para>
    /// </summary>
    public static bool FlankBlocked(Map map, int x, int y, int dir, in StepMask mask, bool doorsShut = false)
    {
        var fx = x;
        var fy = y;

        CalcMoves.Offset((Direction)dir, ref fx, ref fy);

        return BlockedByItems(map, fx, fy, mask.GetWalkZ((Direction)dir), doorsShut);
    }

    /// <summary>
    /// The floor at a point nearest to a height already known to be sensible — the bot's own feet, or the
    /// terrain.
    ///
    /// This and <see cref="LowestGround"/> are the only two ways anything in v2 turns an (x, y) into a
    /// place a bot can stand, and both are narrow by construction. See <see cref="GroundReach"/> for what
    /// the wide version cost.
    /// </summary>
    public static bool Ground(Map map, int x, int y, int nearZ, int tolerance, out sbyte z)
    {
        z = 0;

        if (!OnMap(map, x, y))
        {
            return false;
        }

        Span<sbyte> surfaces = stackalloc sbyte[16];

        var count = StepProbe.ComputeStandableSurfaceZs(map, x, y, surfaces);

        if (count == 0)
        {
            return false;
        }

        var best = int.MaxValue;
        var found = false;

        for (var i = 0; i < count; i++)
        {
            var gap = Math.Abs(surfaces[i] - nearZ);

            if (gap > tolerance || gap >= best)
            {
                continue;
            }

            best = gap;
            z = surfaces[i];
            found = true;
        }

        return found;
    }

    /// <summary>
    /// The lowest floor at a point — a building's ground storey, the path under a bridge, the bottom of a
    /// stairwell.
    ///
    /// The fallback for anywhere the terrain height is no guide, and safe in the way a wide search is not:
    /// <b>a roof is never the lowest thing at a point.</b>
    /// </summary>
    public static bool LowestGround(Map map, int x, int y, out sbyte z)
    {
        z = 0;

        if (!OnMap(map, x, y))
        {
            return false;
        }

        Span<sbyte> surfaces = stackalloc sbyte[16];

        if (StepProbe.ComputeStandableSurfaceZs(map, x, y, surfaces) == 0)
        {
            return false;
        }

        z = surfaces[0];

        return true;
    }

    /// <summary>
    /// The floor a point ought to mean: near the terrain first, and the lowest storey only if that fails.
    ///
    /// The one call anything outside this folder should need. Wrapping the order up here is the point —
    /// the first version's roof bugs were all somebody reaching for the wide search directly because it
    /// was the one that always answered.
    /// </summary>
    public static bool Settle(Map map, int x, int y, out sbyte z)
    {
        z = 0;

        if (!OnMap(map, x, y))
        {
            return false;
        }

        return Ground(map, x, y, map.GetAverageZ(x, y), GroundReach, out z) || LowestGround(map, x, y, out z);
    }

    /// <summary>
    /// Whether the column at a point is water.
    ///
    /// <para>
    /// <b>A different question from <see cref="Settle"/>, and the difference is what put orcs in the sea.</b>
    /// Water land carries <c>Impassable</c> as well as <c>Wet</c>, so settling already refuses the middle of a
    /// lake — what it cannot refuse is the beach one tile short of it. Anything that puts things down at an
    /// offset from a settled point, rather than on it, has to ask about the tiles it is not standing on, and
    /// this is that question.
    /// </para>
    ///
    /// <para>
    /// Statics are read as well as land, because water is both: a lake is land tiles, while a moat, a
    /// fountain and the water under a dock are statics laid over ground. A bridge deck answers wet here, and
    /// that is meant — the deck is standable and the ten tiles around it are not.
    /// </para>
    /// </summary>
    public static bool Wet(Map map, int x, int y)
    {
        if (!OnMap(map, x, y))
        {
            return false;
        }

        var land = map.Tiles.GetLandTile(x, y);

        if (!land.Ignored && (TileData.LandTable[land.ID & TileData.MaxLandValue].Flags & TileFlag.Wet) != 0)
        {
            return true;
        }

        foreach (var tile in map.Tiles.GetStaticTiles(x, y))
        {
            if ((TileData.ItemTable[tile.ID & TileData.MaxItemValue].Flags & TileFlag.Wet) != 0)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Whether every tile within <paramref name="reach"/> of a point is on the map and out of the water.
    ///
    /// <para>
    /// The test for siting something that occupies a place rather than a square. One wet tile answers no,
    /// because the thing being sited scatters its parts across the whole square and the parts that land in
    /// water are precisely the ones nothing can move again: a creature that cannot swim, standing in the sea,
    /// is blocked in all eight directions for as long as it exists.
    /// </para>
    /// </summary>
    public static bool Dry(Map map, int x, int y, int reach)
    {
        for (var dy = -reach; dy <= reach; dy++)
        {
            for (var dx = -reach; dx <= reach; dx++)
            {
                if (!OnMap(map, x + dx, y + dy) || Wet(map, x + dx, y + dy))
                {
                    return false;
                }
            }
        }

        return true;
    }

    public static bool OnMap(Map map, int x, int y) =>
        map != null && map != Map.Internal && x >= 0 && y >= 0 && x < map.Width && y < map.Height;
}
