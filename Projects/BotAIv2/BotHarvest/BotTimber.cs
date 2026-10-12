using System;
using System.Collections.Generic;
using Server.Engines.Harvest;
using Server.Items;
using Server.Regions;
using Server.Targeting;

namespace Server.BotAI.V2;

/// <summary>
/// What a woodcutter needs to know: the axe, the trees within reach, and how much wood is worth a trip.
///
/// <para>
/// <b>Lumberjacking stood in the Gatherer's skill list from the day the class was written and nothing on
/// this shard ever used it.</b> One line, one hundred points, and not a single errand that swung an axe — so
/// the only wood on the island was whatever a carpenter had on his shelf. That mattered the moment arrows
/// became something a bot could make: an arrow is a shaft and a feather, a shaft is a log, and a trade whose
/// raw material can only be bought is a trade that stops when the shopkeeper's stock does.
/// </para>
///
/// <para>
/// <b>Almost all of this is the miner's, and that is the point.</b> <c>BotOre.Examine</c> asks the engine
/// which harvest definition a tile belongs to and <c>BotOre.Swing</c> hands the target over — neither knows
/// or cares that it was written for rock. Trees are static tiles rather than land, which <c>Examine</c>
/// already handles because most stone is a static too. What is left for this file is the axe, the ring
/// search and the arithmetic of when a trip is worth taking.
/// </para>
/// </summary>
public static class BotTimber
{
    /// <summary>How far a woodcutter looks for a tree before it needs a journey.</summary>
    /// <summary>
    /// How far around itself a bot looks for a tree.
    ///
    /// <para>
    /// <b>Widened from twelve to eighty on 05.09.2026, together with the rule that puts the woods outside
    /// the walls.</b> Twelve tiles is a glance, and a glance taken from the market square finds the town's
    /// ornamental trees or nothing: "85 had no tree within 12 tiles" was the ordinary answer. Once town
    /// timber stops counting, the number has to be large enough to see past the wall from inside it, or the
    /// two rules together would simply end the trade. Eighty is BotGround.Patience — a minute's run — and
    /// this is the same journey the miner makes to a seam.
    /// </para>
    /// </summary>
    public static int Reach { get; set; } = 80;

    /// <summary>Trees passed over for standing inside a town. See Find.</summary>
    public static long Townbound { get; private set; }

    /// <summary>
    /// Trees passed over for standing on ground that has refused the population. Its own bucket, because
    /// "inside the walls" and "nobody can get there" are different facts and the day one of them moves the
    /// other must not move with it.
    /// </summary>
    public static long Fenced { get; private set; }

    /// <summary>
    /// <b>A trunk a walk was refused at is left alone for half an hour (30.09.2026, build 366).</b> The refusal a chop's
    /// failed road writes (<c>BotRefused</c>) is a square of eight tiles that anybody arriving in it clears, and the woods
    /// by The Crown's hall at (523, 2113) have bots arriving all the time: Balgrim failed "no way through" at (552, 2106),
    /// (544, 2097) and (540, 2097) at 17:02 on 363 and at the same three trunks again at 17:09. A trunk is a tile, and
    /// only somebody standing at it proves it can be reached. See <see cref="Unreachable"/>, <see cref="Reached"/>.
    /// </summary>
    public static int TreeRestMs { get; set; } = 1800000;

    /// <summary>Trees passed over for a walk refused at the trunk lately. See <see cref="TreeRestMs"/>.</summary>
    public static long Unwalkable { get; private set; }

    private static readonly Dictionary<(int Map, int X, int Y), long> _unreachable = [];

    /// <summary>A walk to this trunk was refused. From <c>BotChop.Bend</c>.</summary>
    public static void Unreachable(Map map, IPoint3D tree)
    {
        if (map == null || tree == null)
        {
            return;
        }

        if (_unreachable.Count > 4096)
        {
            _unreachable.Clear();
        }

        _unreachable[(map.MapID, tree.X, tree.Y)] = Core.TickCount;
    }

    /// <summary>Somebody stands within swinging reach of this trunk, so it can be reached.</summary>
    public static void Reached(Map map, IPoint3D tree)
    {
        if (map != null && tree != null && _unreachable.Count > 0)
        {
            _unreachable.Remove((map.MapID, tree.X, tree.Y));
        }
    }

    private static bool Unwalked(Map map, IPoint3D tree) =>
        _unreachable.Count > 0 && _unreachable.TryGetValue((map.MapID, tree.X, tree.Y), out var at)
        && Core.TickCount - at < TreeRestMs;

    /// <summary>
    /// How near the trunk the engine insists on before it will let an axe swing.
    ///
    /// <para>
    /// Two, and taken as a constant rather than read off the definition because <c>Lumberjacking</c> keeps
    /// its single definition private — unlike <c>Mining</c>, which exposes <c>OreAndStone</c> because it has
    /// two and has to be told apart. A number copied from the engine is a number that can drift, so it is
    /// named here with where it came from: <c>Lumberjacking.cs</c>, <c>MaxRange = 2</c>.
    /// </para>
    /// </summary>
    public static int SwingReach { get; set; } = 2;

    /// <summary>How many logs make a trip worth taking at all.</summary>
    public static int Worthwhile { get; set; } = 20;

    /// <summary>What a log is reckoned at, so a chopping trip can be priced against a mining one.</summary>
    public static int Worth { get; set; } = 3;

    /// <summary>The engine's lumberjacking system, or null before content has built it.</summary>
    public static HarvestSystem System => Lumberjacking.System;

    /// <summary>
    /// The axe.
    ///
    /// <para>
    /// A hatchet first, because it is the small one and the one a crafter is issued; anything else with an
    /// edge will do, and the engine is the judge of that — every axe in the game is a lumberjacking tool and
    /// a bot that happens to be carrying a battle axe can cut with it, provided it can lift it.
    /// </para>
    ///
    /// <para>
    /// <b>An axe this body cannot hold is not its axe.</b> Lumberjacking wants the axe in the hand (see
    /// <c>BotChop.Wield</c>), and the engine will not put a weapon in the hand of a player weaker than the
    /// weapon asks. This used to return any axe in the pack, so a healer with strength 25 carrying a double
    /// axe, which asks 45, was offered the woods and failed on the first beat — "it cannot get the axe into
    /// its hand" — five times in one second, the offer falling a little each time until a restock outbid it:
    /// Marek, 09:46:43 on 15.09.2026. Asked with <c>BotMobile.Suits</c>, the question the re-arm already puts
    /// to every weapon, so "strong enough to hold it" is one answer in two places rather than two answers.
    /// </para>
    /// </summary>
    public static Item Tool(Mobile bot) => Tool(bot, out _);

    /// <summary>The axe, as above, and whether an axe was passed over only for being too heavy.</summary>
    /// <param name="heavy">
    /// True when the pack holds no axe this body can lift and at least one it cannot, so the proposer can
    /// count "has no axe" and "has one it cannot hold" apart.
    /// </param>
    public static Item Tool(Mobile bot, out bool heavy)
    {
        heavy = false;

        if (bot == null)
        {
            return null;
        }

        // <b>The hand before the pack, because this trade puts it there itself.</b> Lumberjacking is the one
        // harvest that refuses an axe out of a pack — see BotChop.Wield — so the very act of starting to cut
        // moves the hatchet onto a layer, where a pack-only search cannot see it. It read as "nothing to cut
        // with" on the swing after the first: 14 of 31 trips ended that way at 14:31 on 04.09.2026, on bots
        // that were holding the axe at the time. The same reading BotShopper.Wanting makes about a pickaxe —
        // held or worn, a tool is still a tool.
        var held = bot.FindItemOnLayer(Layer.OneHanded) ?? bot.FindItemOnLayer(Layer.TwoHanded);

        if (held is Hatchet or BaseAxe)
        {
            return held;
        }

        var pack = bot.Backpack;

        if (pack == null)
        {
            return null;
        }

        var hatchet = pack.FindItemByType<Hatchet>();

        if (hatchet != null && Lifts(bot, hatchet))
        {
            return hatchet;
        }

        var axe = pack.FindItemByType<BaseAxe>();

        if (axe == null || Lifts(bot, axe))
        {
            return axe;
        }

        // The first axe found is too heavy, which is rare — so the search that captures the body, and allocates
        // for it, is rare too.
        axe = pack.FindItemByType<BaseAxe>(true, each => Lifts(bot, each));
        heavy = axe == null;

        return axe;
    }

    /// <summary>Whether this body can hold this axe. See <see cref="Tool(Mobile)"/>.</summary>
    private static bool Lifts(Mobile bot, Item axe) => bot is not BotMobile body || body.Suits(axe);

    /// <summary>How much wood the bot is carrying.</summary>
    public static int Logs(Mobile bot) => bot?.Backpack?.GetAmount(typeof(Log)) ?? 0;

    /// <summary>
    /// How many logs a woodcutter keeps back for itself before the rest goes to the population.
    ///
    /// A fletcher wants wood of its own, and a woodcutter that sold every log and then bought one back off a
    /// stall would be paying the market to hold its own timber. Twenty, which is a round of arrows.
    ///
    /// <para>
    /// <b>And only for a bot that can actually use it, which is the correction that made this work at all.</b>
    /// Most woodcutters are gatherers and carry no fletcher's tools, so a flat twenty meant a cutter reached
    /// its keep-back and stopped — `0 logs went straight into somebody's order and 0 onto a stall` in a whole
    /// window, with a fletcher's funded order for exactly twenty standing on the board unfilled. A keep-back
    /// that blocks a paid order is not a keep-back, it is a hoard. The cook's meat is kept by the same rule:
    /// see <c>BotOven.Spares</c>, which asks for a skillet before it holds anything back.
    /// </para>
    /// </summary>
    public static int Keeps { get; set; } = 20;

    /// <summary>How much this particular bot keeps: the full stock if it can fletch, nothing if it cannot.</summary>
    public static int KeptBy(Mobile bot) => BotFletching.Kit(bot) != null ? Keeps : 0;

    /// <summary>Logs put where somebody can reach them. For the summary.</summary>
    public static long Ordered { get; private set; }

    /// <summary>The same, onto a stall rather than into a standing order.</summary>
    public static long Listed { get; private set; }

    /// <summary>
    /// Counts the cut wood: what a want within a carrier's reach waits for, and the rest, kept to sell. Nothing moves (01.10.2026;
    /// until then a funded order first, a stall second).
    ///
    /// <para>
    /// <b>Woodcutting had no ending.</b> It swung, it counted, and it stopped — and the logs rode home in a
    /// pack, so on 05.09.2026 the shard read <em>133 of 212 fletchers could not find wood</em> while
    /// woodcutters walked past them carrying it, and exactly one log was listed in a whole session. Mining
    /// has finished this way since it was written; this is that ending, on the other gathering trade.
    /// </para>
    /// </summary>
    public static (int Ordered, int Listed) Store(IBotWilful bot)
    {
        var body = bot?.Self;
        var pack = body?.Backpack;

        if (pack == null)
        {
            return (0, 0);
        }

        var ordered = 0;
        var listed = 0;

        // A snapshot: offering a stack moves it out of the pack, which mutates the list being read.
        List<Item> carried = [.. pack.Items];

        for (var i = 0; i < carried.Count; i++)
        {
            if (carried[i] is not Log wood || wood.Deleted || !wood.Movable)
            {
                continue;
            }

            var held = Math.Max(1, wood.Amount);
            var spare = held - KeptBy(body);

            if (spare <= 0)
            {
                continue;
            }

            // Counted, not moved: what a want within a carrier's reach waits for rides in the pack to it (BotCourier), the rest to the
            // bank box by the porter (01.10.2026; until then a want was filled and the rest put on a stall from here).
            var went = BotMarket.Answering(bot, typeof(Log), spare);

            ordered += went;
            listed += spare - went;
        }

        Ordered += ordered;
        Listed += listed;

        return (ordered, listed);
    }

    /// <summary>Forgotten with the world.</summary>
    public static void ForgetTrade()
    {
        Ordered = 0;
        Listed = 0;
    }

    /// <summary>
    /// The nearest tree within reach, as the target the engine expects, or null.
    ///
    /// <para>
    /// Nearest and nothing cleverer, unlike the miner's search. Ore comes in veins worth different money and
    /// choosing between them pays; a log is a log, so the only question a woodcutter has is which tree is
    /// closest, and every beat spent answering a richer one would be a beat not spent cutting.
    /// </para>
    /// </summary>
    public static IPoint3D Find(Mobile bot, HashSet<(int X, int Y)> shun = null)
    {
        var system = System;
        var map = bot?.Map;

        if (system == null || map == null || map == Map.Internal)
        {
            return null;
        }

        var origin = bot.Location;

        for (var radius = 1; radius <= Reach; radius++)
        {
            for (var dx = -radius; dx <= radius; dx++)
            {
                for (var dy = -radius; dy <= radius; dy++)
                {
                    // The edge of each ring only; the inside was covered by the ring before it.
                    if (Math.Abs(dx) != radius && Math.Abs(dy) != radius)
                    {
                        continue;
                    }

                    var found = BotOre.Examine(map, origin.X + dx, origin.Y + dy, system);

                    if (found == null)
                    {
                        continue;
                    }

                    // A tree this trip has already given up on. See BotChop._shunned.
                    if (shun != null && shun.Contains((found.X, found.Y)))
                    {
                        continue;
                    }

                    if (Unwalked(map, found))
                    {
                        Unwalkable++;

                        continue;
                    }

                    // <b>Nobody fells timber inside the walls, by Patrick's order of 05.09.2026, and it is
                    // the same rule the rock has kept since 03.09.</b> A guarded region is a town: the trees
                    // in it are somebody's garden, and a population that harvests its own market square is a
                    // population that never learns where the woods are. The ore side states the reasoning at
                    // length in BotGround.NoteSeam — asked of the tile rather than of the moment, so the
                    // answer is the same for everybody and costs one lookup.
                    if (Region.Find(new Point3D(found.X, found.Y, found.Z), map)?.IsPartOf<GuardedRegion>() == true)
                    {
                        Townbound++;

                        continue;
                    }

                    // <b>And the nearest tree is the same tree every time.</b> This walks rings outward from
                    // the bot's feet and returns the first trunk it meets, so a tree in a fenced garden is
                    // handed to the same woodsman on every review for as long as it stands there: Nessa
                    // failed at (1444, 1458, 2) forty times in fifty-five minutes on 08.09.2026, one of the
                    // 126 chop errands that session which ended in no way through. The refusal is written by
                    // whoever gives up on the road and cleared by whoever arrives, so this skips a trunk
                    // nobody could get to and takes it up again the moment anybody proves otherwise.
                    if (BotRefused.Refusing(map, new Point3D(found.X, found.Y, found.Z)))
                    {
                        Fenced++;

                        continue;
                    }

                    return found;
                }
            }
        }

        return null;
    }

    /// <summary>One swing. The engine rolls it exactly as it would for a player.</summary>
    public static bool Swing(Mobile bot, Item tool, IPoint3D target) =>
        BotOre.Swing(bot, tool, System, target);
}
