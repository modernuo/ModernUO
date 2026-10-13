using System;
using System.Collections.Generic;
using Server.Logging;
using Server.Regions;
using Server.Text;

namespace Server.BotAI.V2;

/// <summary>
/// Who exists. Builds the population at every world load, hands the clock its list, and puts the fallen
/// back on their feet.
///
/// <para>
/// <b>Rebuilt from configuration rather than restored from the save, and that is a design choice with a
/// reason.</b> A bot's state lives in objects — a bond, a journey, a ledger of what paid — and none of it is
/// worth a save format. Loading a saved bot means rebuilding all of it anyway, and the one thing that
/// <em>would</em> come back intact is its pack, so handing out the kit again would produce a bot with two of
/// everything. So bots that arrive from a save are deleted and the population is raised fresh. What survives
/// a restart is what should: names, and the configuration that says who exists.
/// </para>
///
/// <para>
/// The population is small on purpose. Nothing here holds a client, and content that assumes one is the
/// standing hazard of putting <see cref="PlayerMobile"/>-derived bots in a world; a handful of them makes
/// that discoverable, a hundred and fifty makes it a log to read.
/// </para>
/// </summary>
public static class BotPopulation
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotPopulation));

    /// <summary>Which facet the population lives on.</summary>
    public static Map Home { get; set; }

    /// <summary>
    /// Where on it. Britain by default, and specifically the point this shard's own location list calls
    /// Britain — <c>Data/Locations/felucca.json</c> — rather than a coordinate somebody remembered.
    ///
    /// It matters more than it looks: the first bot to want work sweeps the ground around itself for seams,
    /// fires and counters, so where the population is born decides what it can do at all.
    /// </summary>
    public static Point3D Where { get; set; } = new(1592, 1680, 10);

    /// <summary>
    /// How far around that point bots are scattered, so they do not all arrive on one tile.
    ///
    /// <para>
    /// <b>Six put forty-nine bots into a thirteen-tile square, and the rescue kept dropping them into it.</b>
    /// On the night of 07-08.09.2026 fifty-four bots were carried home for being able to reach nothing, and
    /// fifteen of them - twenty-eight per cent - then reported that they could reach nothing from home
    /// either, seconds apart, standing on the population's own doorstep on ground the shard itself called
    /// good. What refuses the roads there is the other bots. Ten makes the patch twenty-one tiles across and
    /// the same population eleven per cent of it rather than twenty-nine.
    /// </para>
    /// </summary>
    public static int Spread { get; set; } = 10;

    /// <summary>Placements that had to take a tile with no way off it. See <see cref="TryPlace"/>.</summary>
    public static long Boxedin { get; private set; }

    /// <summary>
    /// How far from home this population is allowed to want anything. Two hundred tiles: Britain and its
    /// outskirts.
    ///
    /// <para>
    /// <b>A bound on wanting, not on walking.</b> Nothing stops a bot being chased across a field; what this
    /// does is keep every <em>offer</em> inside one town, so the population does not spread itself across a
    /// continent it cannot survive. A seam, a forge, a counter or a shop outside it is simply not proposed.
    /// </para>
    ///
    /// <para>
    /// It is deliberately crude and deliberately temporary. The real answer is travel — stones to the banks of
    /// the big cities — and when that exists this becomes a bound per city instead of one bound in total.
    /// </para>
    /// </summary>
    public static int Roam { get; set; } = 200;

    /// <summary>
    /// How far around the population's home a bot with no guild seat of its own may be born.
    ///
    /// <para>
    /// <b>Patrick's order of 18.09.2026: a population raised from nothing appears in different places around
    /// Britain, not in one field.</b> Before this, every bot without a seat was put within <see cref="Spread"/> of
    /// the single home tile, so the first minutes of a new world were eighty strangers standing on one patch,
    /// bidding for the same work at the same place and walking out along the same road. The tight patch is still
    /// the fallback — a bot that cannot be fitted anywhere out there is put at home rather than not raised.
    /// </para>
    /// </summary>
    public static int Scatter { get; set; } = 350;

    /// <summary>
    /// Whether this place is somewhere the population may work.
    ///
    /// With no home configured there is no bound at all: an unconfigured population is not a population that
    /// may not work, it is one nobody has placed yet.
    /// </summary>
    /// <para>
    /// <b>The dungeons are the population's ground too (29.09.2026).</b> The roam is measured from home, and every dungeon lies
    /// four thousand tiles from it; with the cave mouths open (BotGates) a bot in a cave is a bot at work, and the quarry it
    /// meets there is quarry.
    /// </para>
    /// <para>
    /// <b>And every reachable town's surroundings (29.09.2026).</b> Patrick: the bots scatter over the world and travel. A
    /// traveller in Trinsic hunts what lives round Trinsic and sells at Trinsic's counters; the roam round home is the roam
    /// round every town the gates say can be walked to (<see cref="BotTowns.Within"/>).
    /// </para>
    /// <para>
    /// <b>And the towns bots live in, from the first beat (29.09.2026, residences).</b> The towns' own test answers only once
    /// the gates have, seconds after the boot; a bot that came back from the save standing in its own town is not shut out
    /// of it meanwhile (<see cref="BotResidence.Around"/>).
    /// </para>
    public static bool Within(Map map, Point3D where) =>
        Home == null || map == Home && (Utility.InRange(Where, where, Roam) || BotDungeon.Under(where) || BotTowns.Within(where) || BotResidence.Around(where)
                                        || BotColonies.Within(map, where));

    /// <summary>
    /// <b>The leash (29.09.2026).</b> <see cref="Roam"/> is one bound for the whole population — a thousand tiles in the
    /// configuration, set for a grown population and kept through the wipe — and fifty newcomers were being sent to
    /// seams, woods and fights up to a thousand tiles from Britain, into the trolls', ettins' and gazers' ground, at two
    /// deaths a minute. A bot's own bound is the roam scaled by how far along its trade it is (<c>BotMobile.Progress</c>, the
    /// share of what its class is aiming for) against <see cref="GrownProgress"/>, never under <see cref="LeashFloor"/> of
    /// it: a newcomer at a quarter of its trade stays within three hundred tiles of its seat, a bot halfway within five
    /// hundred, a grown one has the roam. Progress rather than power, because power is hit points times the damage of
    /// a blow, and a smith holding a hammer reads strong. Measured from where the bot lives (<see cref="HomeOf"/>: its
    /// guild's seat — The Needle lives at Cove, eight hundred tiles from Britain — or its town). Asked by the samplers
    /// that choose a place or a prey for one bot; the shops, the corpses and the flight keep the plain roam.
    /// </summary>
    public static double GrownProgress { get; set; } = 1.0;

    /// <summary>The least share of the roam a bot keeps however weak. Three-tenths.</summary>
    public static double LeashFloor { get; set; } = 0.3;

    /// <summary>Choices refused by a bot's leash that the roam would have allowed. See <see cref="Leash"/>.</summary>
    public static long Leashed { get; private set; }

    /// <summary>How far from its seat this bot may be sent, in tiles. See <see cref="GrownProgress"/>.</summary>
    public static int Leash(Mobile body)
    {
        if (body is not BotMobile bot || GrownProgress <= 0.0)
        {
            return Roam;
        }

        var share = Math.Clamp(bot.Progress / GrownProgress, Math.Clamp(LeashFloor, 0.0, 1.0), 1.0);

        return (int)Math.Round(Roam * share);
    }

    /// <summary>
    /// <b>A place on another land is not a place (29.09.2026).</b> On build 294 the dashboard's "fixed" purpose read 95.7 % of
    /// searches reached and the mine's largest failure was "no way through to (x, y)": seams on the cliffs west of the
    /// Trinsic road, each one a full search burnt to the clock proving there was no road up. The tiered graph knows in
    /// one lookup which land a tile is on (<c>BotGates.LandOf</c>, the graph's component) and whether a gate joins two
    /// lands; a sampler asks that before it offers the place, and a search is never spent on a cliff. Counted as
    /// <see cref="Unjoined"/>.
    /// </summary>
    public static bool Reachable(Map map, Point3D from, Point3D to)
    {
        // <b>The reach map before the lands (01.10.2026).</b> The graph's land of a place is the land of the first tile within two
        // that has somewhere to stand, and a place whose tile stands in a pocket no gate leaves has no land at all — and Joined
        // answers "yes" for want of one: a seam on a cliff top, a spot in a walled yard. The reach map knows each tile: a place
        // with no open ground within SampleReach tiles is not offered. Counted as Unreached. See BotReachMap.
        if (BotReachMap.Refuses(map, from, to, BotReachMap.SampleReach))
        {
            Unreached++;
            BotReachMap.NoteSampler();

            return false;
        }

        if (BotGates.Joined(map, from, to))
        {
            return true;
        }

        Unjoined++;

        return false;
    }

    /// <summary>Places refused for lying on a land no gate joins to the bot's. See <see cref="Reachable"/>.</summary>
    public static long Unjoined { get; private set; }

    /// <summary>Places refused because the reach map has no open ground near them (01.10.2026). See <see cref="Reachable"/>.</summary>
    public static long Unreached { get; private set; }

    /// <summary>
    /// <b>Where this bot lives (29.09.2026, Patrick's order: a town of residence for every bot).</b> The one answer to every
    /// per-bot "home" on the shard — the walk home, where it is born and rises, the flight's last resort, the herb woods,
    /// the hunting darts, the leash — asked of <see cref="BotResidence.Home"/>: its guild's hall or seat, or the town it lives
    /// in, or the population's home. Population-wide questions (the roads' flood, the gates, the city's errands) still ask
    /// <see cref="Where"/>.
    /// </summary>
    public static Point3D HomeOf(Mobile body) => body is BotMobile bot ? BotResidence.Home(bot) : Where;

    /// <summary>Where a bot's leash is measured from: where it lives. See <see cref="HomeOf"/>.</summary>
    public static Point3D Centre(Mobile body) => HomeOf(body);

    /// <summary>As <see cref="Within(Map, Point3D)"/>, and within this bot's own leash. See <see cref="Leash"/>.</summary>
    public static bool Within(Map map, Point3D where, Mobile body)
    {
        if (!Within(map, where))
        {
            return false;
        }

        // A colony's ground is its colonists' ground, two thousand tiles from their seat (07.10.2026, sea20). See BotColonies.Within.
        if (body == null || BotDungeon.Under(where) || BotTowns.Within(where) || Utility.InRange(Centre(body), where, Leash(body))
            || BotColonies.Within(map, where))
        {
            return true;
        }

        Leashed++;

        return false;
    }

    /// <summary>
    /// How far out the walk looks for the edge of the guarded ground, and how coarsely.
    ///
    /// Britain's wall is a hundred-odd tiles from where the population lives, so four hundred is generous
    /// and eight-tile strides find the edge to within eight — which is the right grain for a place to stand
    /// and wait, and turns a walk of four hundred lookups into fifty.
    /// </summary>
    public static int GateReach { get; set; } = 400;

    public static int GateStride { get; set; } = 8;

    /// <summary>Gates found. A named nought: if this stays at nought nobody is gathering anywhere new.</summary>
    public static long Gates { get; private set; }

    /// <summary>
    /// The edge of the guarded ground, in the direction of somewhere worth going.
    ///
    /// <para>
    /// <b>Patrick's order of 03.09.2026: a company gathers at the edge of the town rather than inside it,
    /// so the march out is shorter.</b> Bots live and bank and buy in the middle of Britain, so that is
    /// where one of them is standing when it decides it needs help — and the company it raises then walks
    /// the whole width of the town before it has gone anywhere at all. The edge in the right direction is
    /// the shortest honest place to meet, and it is also where more of the population passes by than
    /// anywhere inside the walls.
    /// </para>
    ///
    /// <para>
    /// Found by walking rather than by geometry: guarded regions are drawn as whatever shape somebody drew
    /// them, so the only reliable way to find their edge is to ask the engine at points along the line. The
    /// first point outside is the answer, stepped back onto ground a body can stand on.
    /// </para>
    ///
    /// <para>
    /// Returns <see cref="Point3D.Zero"/> when there is no edge to find — the bot is not in a town, or the
    /// line leaves the map — and every caller treats that as "gather where you stand", which is what it did
    /// before this existed.
    /// </para>
    /// </summary>
    /// <param name="counted">
    /// False for a proposer weighing where a company would be raised, so <see cref="Gates"/> still counts the gates
    /// companies actually walked to.
    /// </param>
    public static Point3D Gate(Map map, Point3D from, Point3D toward, bool counted = true)
    {
        if (map == null || map == Map.Internal || Region.Find(from, map)?.IsPartOf<GuardedRegion>() != true)
        {
            return Point3D.Zero;
        }

        var dx = toward.X - from.X;
        var dy = toward.Y - from.Y;
        var span = Math.Max(Math.Abs(dx), Math.Abs(dy));

        if (span <= 0)
        {
            return Point3D.Zero;
        }

        for (var out_ = GateStride; out_ <= GateReach; out_ += GateStride)
        {
            var x = from.X + dx * out_ / span;
            var y = from.Y + dy * out_ / span;

            if (x < 0 || y < 0 || x >= map.Width || y >= map.Height)
            {
                return Point3D.Zero;
            }

            if (!BotStep.Settle(map, x, y, out var z))
            {
                continue;
            }

            var here = new Point3D(x, y, z);

            if (Region.Find(here, map)?.IsPartOf<GuardedRegion>() == true)
            {
                continue;
            }

            if (counted)
            {
                Gates++;
            }

            return here;
        }

        return Point3D.Zero;
    }

    /// <summary>How long a dead bot lies there before it is put back on its feet.</summary>
    public static int ReviveMs { get; set; } = 60000;

    /// <summary>How many placements are tried before falling back to the configured point itself.</summary>
    private const int Attempts = 20;

    /// <summary>
    /// Everybody, including holes.
    ///
    /// <b>Deleted bots leave a null rather than shifting the list</b>, because a bot can be deleted by its own
    /// turn — and a list that shifts underneath the clock's loop skips whoever moved into the gap. Holes are
    /// filled when the population is next raised.
    /// </summary>
    private static readonly List<BotMobile> _bots = [];

    private static int _holes;

    public static IReadOnlyList<BotMobile> Bots => _bots;

    /// <summary>
    /// Bots out of the world, resting. See <see cref="BotRest"/>: their slots in <see cref="Bots"/> are holes while they
    /// are away, so nothing that walks the population can offer them anything.
    /// </summary>
    private static readonly List<BotMobile> _away = [];

    public static IReadOnlyList<BotMobile> Away => _away;

    /// <summary>How many bots are in the world.</summary>
    public static int Count => _bots.Count - _holes;

    /// <summary>
    /// The bot that holds an office (<see cref="BotClass.Office"/>), or null: the first of its class in the world — dead ones
    /// counted, a ghost is still on the map and the reviver raises it — or resting. By class over the whole population and not
    /// through <c>BotCritical</c>'s counts, which keep only the posts with a floor, so they never saw a Sage or an Architect, and
    /// skip a bot on the internal map (07.10.2026, the reviews of build 425).
    /// </summary>
    public static BotMobile Holder(BotClass klass)
    {
        if (klass == null)
        {
            return null;
        }

        for (var i = 0; i < _bots.Count; i++)
        {
            if (_bots[i] is { Deleted: false } bot && bot.Class != null && bot.Class.Name.InsensitiveEquals(klass.Name))
            {
                return bot;
            }
        }

        for (var i = 0; i < _away.Count; i++)
        {
            if (_away[i] is { Deleted: false } bot && bot.Class != null && bot.Class.Name.InsensitiveEquals(klass.Name))
            {
                return bot;
            }
        }

        return null;
    }

    /// <summary>Second holders of an office refused at <see cref="Raise(BotClass, string)"/> this session.</summary>
    public static long SecondRefused { get; private set; }

    /// <summary>Second holders of an office deleted at the last <see cref="Reclaim"/>.</summary>
    public static int SecondDeleted { get; private set; }

    public static int Living
    {
        get
        {
            var living = 0;

            for (var i = 0; i < _bots.Count; i++)
            {
                if (_bots[i] is { Deleted: false, Alive: true })
                {
                    living++;
                }
            }

            return living;
        }
    }

    /// <summary>
    /// Takes back into the population every bot the world save returned that configuration still asks for, and
    /// deletes the rest.
    ///
    /// <para>
    /// <b>Patrick's order of 18.09.2026: bots are to survive a restart.</b> What the engine restores by itself is
    /// most of what matters — where the bot stands, its pack, its bank, its skills, its stats, its clothes, its
    /// guild — and <see cref="BotMobile.Serialize"/> keeps the four things it does not. Reviving is therefore
    /// cheap; the care is all in <b>who</b> is revived.
    /// </para>
    ///
    /// <para>
    /// Configuration stays the authority on who exists, which was the whole point of the purge this replaces. A
    /// bot whose class is no longer in the mix is deleted, and so is the surplus when the mix asks for fewer of a
    /// class than the save returned — first come, first kept. The shortfall is raised afresh by the caller. So
    /// editing <c>bot-population.json</c> still changes the population on the next boot, and it now does so
    /// without costing everybody else their day's work.
    /// </para>
    ///
    /// <para>
    /// <b>The one place in this assembly that walks the whole world, and it is deliberate</b> — the same reason
    /// the purge gave: there is no spatial query for "everywhere", this runs once per world load, and the
    /// alternative is a population that doubles every restart.
    /// </para>
    ///
    /// <para>
    /// <b>An office is kept once, whatever the mix says (07.10.2026, Patrick).</b> The save before the 07.10 wipe held six
    /// Captains and eight Sages, and "first come, first kept" would have kept whichever the world's table listed first. The
    /// holders of an office (<see cref="BotClass.Office"/>) are taken first and in order of standing: the most progressed — the
    /// highest skill total — and, between equals, the oldest body (the lower serial, as <see cref="Unduplicate"/> decides). The
    /// most progressed because an office is what its holder knows: the Captain and the Sage teach up to their own standing and
    /// no further, and the Architect is the island's one seasoned armourer, so keeping the lesser of two would cost the island
    /// the very thing the office is for. The rest are deleted and named in the log, each with the one who keeps the office.
    /// </para>
    /// </summary>
    /// <param name="mix">Who configuration says should exist.</param>
    /// <param name="kept">How many of each class were taken back.</param>
    /// <returns>How many were deleted.</returns>
    public static int Reclaim(IReadOnlyDictionary<string, int> mix, out Dictionary<string, int> kept)
    {
        List<BotMobile> saved = [];
        List<BotMobile> offices = [];
        kept = [];

        foreach (var mobile in World.Mobiles.Values)
        {
            if (mobile is BotMobile bot)
            {
                (BotClasses.Find(bot.Was) is { Office: true } ? offices : saved).Add(bot);
            }
        }

        // The offices first, the best holder of each first; everybody else in the order the world gave them, as before.
        offices.Sort(Senior);
        offices.AddRange(saved);
        saved = offices;

        var deleted = 0;
        Dictionary<string, BotMobile> holders = new(StringComparer.OrdinalIgnoreCase);
        SecondDeleted = 0;

        for (var i = 0; i < saved.Count; i++)
        {
            var bot = saved[i];
            var name = bot.Was;
            var klass = name == null ? null : BotClasses.Find(name);

            // A bot that was resting when the shard stopped and has not finished resting stays out of the world. It is
            // kept all the same — it is one of the mix — and goes back in its slot when BotRest says the rest is over.
            var resting = BotRest.Resting(bot.Name);

            var want = 0;
            var asked = klass != null && mix != null && mix.TryGetValue(klass.Name, out want);

            // An office is one whatever the mix it was handed says (07.10.2026): the configuration and BotGrowth.Mix hold it
            // there already, and this is the door a second holder from the save would otherwise come back through.
            if (klass is { Office: true })
            {
                want = Math.Min(want, 1);
            }

            if (!asked || kept.GetValueOrDefault(klass.Name) >= want || !bot.Revive(klass, resting))
            {
                if (asked && klass.Office && holders.TryGetValue(klass.Name, out var holder))
                {
                    SecondDeleted++;
                    logger.Warning(
                        "{Name} the {Class} ({Skills:F1} skill points, serial {Serial}, {Gold}gp in pack and bank, deleted with it) was a second holder of an office and is deleted; {Holder} ({Held:F1}, serial {HeldSerial}) keeps it",
                        bot.Name,
                        klass.Name,
                        bot.SkillsTotal / 10.0,
                        bot.Serial,
                        BotYield.Wealth(bot),
                        holder.Name,
                        holder.SkillsTotal / 10.0,
                        holder.Serial
                    );
                }

                bot.Delete();
                deleted++;

                continue;
            }

            kept[klass.Name] = kept.GetValueOrDefault(klass.Name) + 1;

            if (klass.Office)
            {
                holders[klass.Name] = bot;
            }

            if (resting)
            {
                _away.Add(bot);
                continue;
            }

            Enlist(bot);
        }

        Unduplicate();

        if (SecondDeleted > 0)
        {
            logger.Warning("{Deleted} second holders of an office came back from the save and were deleted; one of each office is kept", SecondDeleted);
        }

        return deleted;
    }

    /// <summary>
    /// The better holder of an office first: the higher skill total, then the older body. See <see cref="Reclaim"/>.
    /// </summary>
    private static int Senior(BotMobile a, BotMobile b)
    {
        var bySkill = b.SkillsTotal.CompareTo(a.SkillsTotal);

        return bySkill != 0 ? bySkill : a.Serial.CompareTo(b.Serial);
    }

    /// <summary>
    /// Deletes every bot that came back from the world save.
    ///
    /// <para>
    /// <b>The one place in this assembly that walks the whole world, and it is deliberate.</b> The shard's
    /// own rules forbid iterating <c>World.Mobiles</c> in favour of spatial queries, for good reason — but
    /// there is no spatial query for "everywhere", this runs once per world load, and the alternative is a
    /// population that doubles every restart. The first version reached the same conclusion for the same
    /// reason.
    /// </para>
    /// </summary>
    public static int PurgeSaved()
    {
        List<BotMobile> stale = [];

        foreach (var mobile in World.Mobiles.Values)
        {
            if (mobile is BotMobile bot)
            {
                stale.Add(bot);
            }
        }

        for (var i = 0; i < stale.Count; i++)
        {
            stale[i].Delete();
        }

        return stale.Count;
    }

    /// <summary>
    /// Raises the population described by configuration: so many of this class, so many of that. Returns how
    /// many were actually born.
    /// </summary>
    public static int Raise(IReadOnlyDictionary<string, int> mix) => Raise(mix, null);

    /// <summary>The same, less whoever is already standing: the shortfall after <see cref="Reclaim"/>.</summary>
    public static int Raise(IReadOnlyDictionary<string, int> mix, IReadOnlyDictionary<string, int> already)
    {
        if (mix == null || mix.Count == 0)
        {
            logger.Error("No population is configured, so no bots exist. Name classes and counts in Configuration/bot-population.json");

            return 0;
        }

        var born = 0;

        foreach (var (name, count) in mix)
        {
            var klass = BotClasses.Find(name);

            if (klass == null)
            {
                // By name, because this is a typo in a config file and the only useful answer names it.
                logger.Error("No class is called {Name}, so none of the {Count} asked for were raised", name, count);

                continue;
            }

            // An office is one, whatever the mix says (07.10.2026, Patrick). See BotClass.Office.
            var asked = klass.Office ? Math.Min(count, 1) : count;

            var wanted = already != null && already.TryGetValue(klass.Name, out var standing)
                ? asked - standing
                : asked;

            for (var i = 0; i < wanted; i++)
            {
                if (Raise(klass) != null)
                {
                    born++;
                }
            }
        }

        return born;
    }

    /// <summary>One bot of the given class, placed, outfitted and put on the clock.</summary>
    public static BotMobile Raise(BotClass klass) => Raise(klass, null);

    /// <summary>
    /// A newcomer: one bot of the given class under a name nobody on the island has worn — no bot in the world, none
    /// resting, and none whose learning <see cref="BotProgress"/> still keeps, which would hand the newcomer somebody
    /// else's skills. See <see cref="BotGrowth"/>.
    /// </summary>
    public static BotMobile RaiseNewcomer(BotClass klass)
    {
        // A newcomer of a people: the next name of that people neither worn nor remembered. See BotPeoples.
        var people = BotPeoples.Pick(klass);

        if (people != null)
        {
            var female = Utility.Random(2) == 0;
            var fresh = BotPeoples.Christen(people, female, n => InUse(n) || BotProgress.Remembers(n));

            if (fresh != null)
            {
                var born = Raise(klass, fresh);

                if (born != null)
                {
                    born.Female = female;
                }

                return born;
            }
        }

        for (var index = 0; index < Names.Length * (Houses.Length + 1); index++)
        {
            var name = NameAt(index);

            if (!InUse(name) && !BotProgress.Remembers(name))
            {
                return Raise(klass, name);
            }
        }

        return null;
    }

    /// <summary>Bots renamed at the boot because another bot already answered to their name.</summary>
    public static long Renamed { get; private set; }

    /// <summary>
    /// Gives the younger of two bots that answer to one name a name of its own.
    ///
    /// <para>
    /// <b>Two Aldens were found on 24.09.2026 at 02:12</b> — Alden the Crafter of The Hammer and Alden the Gatherer of
    /// The Ash — by the rest clock, which keeps one record per name and so met eighty bots with seventy-nine records.
    /// The second came from <see cref="Christen"/> handing out the pool's first name after a boot, which it no longer
    /// does; this takes the pair that already exists apart. The older body (the lower serial) keeps the name, because
    /// the log, the guild and the learning have been about it the longest.
    /// </para>
    /// </summary>
    private static void Unduplicate()
    {
        List<BotMobile> everybody = [];

        for (var i = 0; i < _bots.Count; i++)
        {
            if (_bots[i] is { Deleted: false } bot)
            {
                everybody.Add(bot);
            }
        }

        everybody.AddRange(_away);
        everybody.Sort((a, b) => a.Serial.CompareTo(b.Serial));

        HashSet<string> seen = new(StringComparer.OrdinalIgnoreCase);

        for (var i = 0; i < everybody.Count; i++)
        {
            var bot = everybody[i];

            if (string.IsNullOrEmpty(bot.Name) || seen.Add(bot.Name))
            {
                continue;
            }

            string fresh = null;

            for (var index = 0; index < Names.Length * (Houses.Length + 1); index++)
            {
                var name = NameAt(index);

                if (!seen.Contains(name) && !InUse(name) && !BotProgress.Remembers(name))
                {
                    fresh = name;
                    break;
                }
            }

            if (fresh == null)
            {
                continue;
            }

            logger.Warning(
                "{Old} the {Class} answered to the same name as an older bot; it is {New} from now on",
                bot.Name,
                bot.Class?.Name,
                fresh
            );

            bot.Name = fresh;
            seen.Add(fresh);
            Renamed++;
        }
    }

    /// <summary>
    /// Takes a bot out of the world's population to rest: its slot becomes a hole, as a deleted bot's does, and it is
    /// kept among the away. The caller puts the body on the internal map. See <see cref="BotRest"/>.
    /// </summary>
    public static void Park(BotMobile bot)
    {
        if (bot == null)
        {
            return;
        }

        for (var i = 0; i < _bots.Count; i++)
        {
            if (!ReferenceEquals(_bots[i], bot))
            {
                continue;
            }

            _bots[i] = null;
            _holes++;

            break;
        }

        bot.Scheduled = false;

        if (!_away.Contains(bot))
        {
            _away.Add(bot);
        }
    }

    /// <summary>
    /// Puts a rested bot back where it left and back on the clock, or at home when that ground no longer takes a body.
    /// Returns whether it came back where it left.
    /// </summary>
    public static bool Unpark(BotMobile bot)
    {
        if (bot == null)
        {
            return false;
        }

        _away.Remove(bot);

        var map = bot.LogoutMap;
        var at = bot.LogoutLocation;
        var own = map != null && map != Map.Internal && map.CanSpawnMobile(at);

        if (own)
        {
            bot.MoveToWorld(at, map);
        }
        else if (!TryPlace(bot) && Home != null)
        {
            bot.MoveToWorld(Where, Home);
        }

        Enlist(bot);

        return own;
    }

    /// <summary>
    /// The same, under a name of the caller's choosing.
    ///
    /// <para>
    /// <b>For bots that are not drawn from the population, and the King's Rangers are the first of them.</b>
    /// The name pool is the population's own roll of townsfolk, dealt out in order — so a company raised from
    /// it comes out as "Kerrin 2" and "Lysa 2", which reads as two ordinary bots with duplicate names rather
    /// than as the crown's. Who a bot is meant to be is the caller's to say when the caller is not the
    /// population.
    /// </para>
    /// </summary>
    public static BotMobile Raise(BotClass klass, string called)
    {
        if (klass == null || Home == null || Home == Map.Internal)
        {
            return null;
        }

        // <b>The door every new bot passes through, so the office is kept here and not only by the callers (07.10.2026).</b>
        // Patrick's rule — one Captain, one Baron, one Architect, one Sage at any population — was a count in a file in build
        // 425, and a newcomer laid over the mix, a stale file or a critical floor could each walk round it. Whoever asks — the
        // boot, the growth, a want, a caller not yet written — a second holder is refused here, and said.
        if (klass.Office && Holder(klass) is { } holder)
        {
            SecondRefused++;
            logger.Warning(
                "No second {Class}: {Holder} holds the office, so none was raised ({Refused} refused this session)",
                klass.Name,
                holder.Name,
                SecondRefused
            );

            return null;
        }

        var bot = new BotMobile();
        var female = Utility.Random(2) == 0;

        // The people for the class, and the name for the people and the sex; the old pool when the file names nobody, or
        // when the caller says who this is. See BotPeoples (Patrick's order of 29.09.2026).
        BotPeoples.People people = null;
        var name = called;

        if (string.IsNullOrWhiteSpace(name))
        {
            people = BotPeoples.Pick(klass);
            name = BotPeoples.Christen(people, female, InUse) ?? Christen();
        }
        else
        {
            people = BotPeoples.Of(name);
        }

        bot.Become(klass, name, female, people);

        // After Become, never before: the class deals out its starting skills in there, and a restore that
        // ran first would be overwritten by them. See BotProgress for why only the learning comes back and
        // the belongings are still built from nothing.
        BotProgress.Restore(bot);

        // Whether its character is drawn fresh rather than read back: read before BotTraits.Born, which marks it announced. A fresh
        // character is a birth for its story (PLAN-life A3, 13.10.2026). See BotStory.Born.
        var fresh = BotLives.Of(bot) is { Fresh: true };

        // Its nature, read back or drawn from its name; said in the log when it is a newcomer after the boot (07.10.2026,
        // traits17). See BotTraits.Born.
        BotTraits.Born(bot, BotCharacterModule.Started);

        // Its town of residence before it is put down, so it is born there (29.09.2026). See BotResidence.Raised.
        BotResidence.Raised(bot);

        // Its story: a birth in its town when the character is fresh, else the book found or opened as it stands.
        BotStory.Born(bot, fresh);

        if (!TryPlace(bot))
        {
            logger.Error(
                "Nowhere to put {Name} the {Class} near {Where} on {Map}; it was not raised",
                bot.Name,
                klass.Name,
                Where,
                Home
            );

            bot.Delete();

            return null;
        }

        Enlist(bot);

        return bot;
    }

    /// <summary>
    /// Puts a fallen bot back on its feet once it has lain there long enough, and returns whether it did.
    ///
    /// <para>
    /// Somebody has to: nothing else in this project resurrects anybody, and a dead bot is a ghost for the
    /// rest of the shard's life. The delay is not decoration — dying has to cost something, and what it costs
    /// a bot is time. The decision layer charges it separately in its own units; this is the same fact in
    /// wall-clock.
    /// </para>
    /// </summary>
    public static bool Revive(BotMobile bot)
    {
        if (bot == null || bot.Deleted || bot.Alive || !bot.Fallen)
        {
            return false;
        }

        // A party underground raises its own, where they fell and on their own clock. Asked before the
        // wait rather than after it, because the wait is the ordinary revival's own and ends in the bot
        // being carried back to Britain — which for a delver is not a revival, it is being sent home.
        if (BotDelveParty.Raise(bot))
        {
            return true;
        }

        // <b>Nor while its company is raising it where it fell (02.10.2026).</b> A member that falls in a company is looked at by the
        // company on its next beat, and while somebody able is at the body — or waiting out the fight round it — the minute's wait
        // and the carrying home are held, up to BotRaise.HoldMs. A company with nobody able holds nothing. See BotSquadRaise.
        if (BotRaise.Holding(bot))
        {
            return false;
        }

        if (Core.TickCount - bot.FellTick < ReviveMs)
        {
            return false;
        }

        // Home first, then up: a ghost resurrected where it died is a bot standing in whatever killed it.
        TryPlace(bot);

        bot.Resurrect();

        if (!bot.Alive)
        {
            // The engine refused. It does that silently — <c>Mobile.Resurrect</c> returns nothing and simply
            // does not raise a mobile whose region or state says no — so without this a ghost lies there for
            // the rest of the shard's life and the only symptom is a bot that never moves again.
            if (!bot.ReviveComplained)
            {
                bot.ReviveComplained = true;

                logger.Error(
                    "{Name} the {Class} would not get up at {Where} in {Region}; it stays a ghost",
                    bot.Name,
                    bot.Class?.Name,
                    bot.Location,
                    bot.Region?.Name ?? "nowhere"
                );
            }

            return false;
        }

        logger.Information("{Name} the {Class} is back on its feet at {Where}", bot.Name, bot.Class?.Name, bot.Location);

        // Its company held the ground it fell on: the walk back to its corpse is the first thing it does (build next). See BotUndertaker.Risen.
        BotUndertaker.Risen(bot);

        return true;
    }

    /// <summary>
    /// How many roads a bot may be refused, one after another, before it is presumed to be somewhere it
    /// cannot get out of.
    ///
    /// A refusal is proof: the search walked every tile the bot can reach and the destination was not among
    /// them. One of those is ordinary — an island across the water, a locked crypt. A dozen in a row, to a
    /// dozen different places, is not a statement about the destinations. It is a statement about where the
    /// bot is standing.
    /// </summary>
    public static int StrandedLimit { get; set; } = 12;

    /// <summary>Bots carried home after getting themselves somewhere with no way out. For the summary.</summary>
    public static long Rescued { get; private set; }

    /// <summary>Rescues declined because the bot was underground with a party. Its own bucket, by the rule.</summary>
    public static long Delving { get; private set; }

    /// <summary>Company members let go of their company instead of being carried home. See <see cref="Rescue"/>.</summary>
    public static long Unbound { get; private set; }

    /// <summary>
    /// Puts a stranded bot back where the population lives.
    ///
    /// <para>
    /// <b>The one thing in this project that moves a bot without it walking, and it earns that.</b> A bot on a
    /// spit of land in the water, or inside a yard whose gate was built over, cannot be argued out of it: every
    /// undertaking it takes is refused on its first beat, it fails, the proposer offers another, and that is
    /// the whole of its life from then on — measured at a hundred and eighty failures in twenty minutes,
    /// from two bots, while everything else on the shard worked perfectly. No amount of choosing better work
    /// helps, because the problem is not the work.
    /// </para>
    ///
    /// <para>
    /// It is deliberately not a teleport a bot can want or plan around: nothing offers it, nothing prices it,
    /// and it fires only on proof — a dozen destinations proved unreachable one after another, without a
    /// single step taken in between. Anything less and it would become a way of travelling.
    /// </para>
    /// </summary>
    public static bool Rescue(BotMobile bot)
    {
        if (bot == null || bot.Deleted || Home == null || Home == Map.Internal)
        {
            return false;
        }

        // <b>Not a bot that is underground with a party, and this one cost four delves in one session.</b>
        // A dozen refused roads in a row is proof of being stranded on the island and is ordinary inside a
        // cave: the formation asks a member to take its place three tiles away, a wall is between them, and
        // the search says no — over and over. Measured 11.09.2026: every one of the four parties ran for
        // thirteen minutes, hit exactly twelve refusals in the last half-minute, and had its leader carried
        // to Britain, which ended the delve one kill short of its quota in one case. The way out of a
        // dungeon belongs to the delve, which has its own clock and its own watchdog; see BotDelveParty.
        // A delver in its company is the delve's to carry; a delver its company has let go is nobody's, and
        // nobody's is exactly who this is for. See BotSquad.Release for how one came to be let go underground.
        if (BotDelveParty.Delving(bot) && bot is IBotSquadMember { Squad: not null })
        {
            Delving++;

            return false;
        }

        // <b>A company member that cannot reach its station leaves the company; it is not carried home.</b>
        // A dozen proven refusals from a member are a dozen stations it could not walk to — the leader on a
        // hill, across a river, in a cave — and the remedy was to put the member down at its hall, which on
        // 14.09.2026 at 03:51 sent Fendrel, Ilsa and Nessa from 1150,2200 to their halls six hundred tiles
        // off in one second, out of a hunt that was going perfectly well for the rest of the company. Let
        // go, the member is a free bot standing where the hunt is, with the auction to hand. The leader is
        // not let go of — the members are stationed on it, not it on them — and a delver is handled above.
        if (bot is IBotSquadMember { Squad: { } company } member && !ReferenceEquals(company.Leader, member))
        {
            BotSquads.Leave(member);
            Unbound++;
            bot.Refusals = 0;

            logger.Information(
                "{Name} the {Class} was let go of company {Squad} at {Where}: it could not reach its place in it {Limit} times running",
                bot.Name,
                bot.Class?.Name,
                company.Id,
                bot.Location,
                StrandedLimit
            );

            return false;
        }

        var from = bot.Location;

        // Already home. Then the bot is not stranded and moving it three tiles proves nothing — whatever is
        // refusing its roads is refusing them here too, and carrying it "home" only wipes the errand it was
        // holding and hides the real fault. Eighteen of these in twenty minutes were the symptom of a starved
        // path-search budget, not of bad ground, and every one of them read in the log as a rescue.
        if (Utility.InRange(from, HomeOf(bot), Spread * 2))
        {
            if (!bot.ReviveComplained)
            {
                bot.ReviveComplained = true;

                logger.Error(
                    "{Name} the {Class} can reach nothing from {Where}, and it is standing at home — this is not bad ground, look at what is refusing the roads",
                    bot.Name,
                    bot.Class?.Name,
                    from
                );
            }

            bot.Refusals = 0;

            return false;
        }

        // Before it is moved: what has just been proved about this ground, written where everybody reads it.
        //
        // A dozen roads refused in a row from one tile is the strongest evidence of a pocket the shard ever
        // produces, and until now the whole of it was spent carrying one bot home. The same ground goes on
        // catching the next bot, and the log for the night says so — (1757, 976) took three, (1623, 1179) took
        // three, (1651, 1112) took two. A look from where the bot is standing costs a couple of milliseconds
        // and files the trap for the life of the shard, so the next bot is refused the road in rather than
        // rescued out of it.
        var trap = BotPath.Enclose(bot.Map, from, BotArrival.Exactly, urgent: true);

        // <b>And what the tile itself says, because "could get nowhere at all" named no cause and there are
        // three quite different ones.</b> A walk mask of nought is a tile the planner agrees is closed. A
        // mask with bits in it, against an engine that refuses every step, is the planner and the engine
        // disagreeing — which is the fault this whole subsystem exists to catch. And a floor that is not the
        // height the bot believes it is standing at is the oldest of them: a Z that came out of arithmetic
        // rather than out of Settle, working on the flat and failing on a hill.
        var footing = (sbyte)Math.Clamp(from.Z, sbyte.MinValue, sbyte.MaxValue);
        var allowed = BotStep.Mask(bot.Map, from.X, from.Y, footing).WalkMask;
        var floor = BotStep.Settle(bot.Map, from.X, from.Y, out var under) ? under.ToString() : "no floor at all";

        if (!TryPlace(bot))
        {
            return false;
        }

        bot.Journey?.Finish();
        bot.Refusals = 0;
        Rescued++;

        logger.Error(
            "{Name} the {Class} could get nowhere at all from {From} and has been carried home to {Where}; the ground it was on is {Trap}, its tile allows {Allowed:X2} of the eight directions and the floor there is {Floor} against the {Z} it thought it stood at",
            bot.Name,
            bot.Class?.Name,
            from,
            bot.Location,
            trap,
            allowed,
            floor,
            from.Z
        );

        return true;
    }

    /// <summary>
    /// This bot is gone. Its slot becomes a hole rather than being removed, so the clock's loop cannot skip
    /// its neighbour. Called from <see cref="BotMobile.OnAfterDelete"/>.
    /// </summary>
    public static void Forget(BotMobile bot)
    {
        BotStall.Forget(bot);
        BotGround.Forget(bot);

        if (bot == null)
        {
            return;
        }

        _away.Remove(bot);

        for (var i = 0; i < _bots.Count; i++)
        {
            if (!ReferenceEquals(_bots[i], bot))
            {
                continue;
            }

            _bots[i] = null;
            _holes++;

            return;
        }
    }

    /// <summary>
    /// The whole population deleted and forgotten. Called before a world is replaced: every bot in the list
    /// belongs to a world that is about to stop existing.
    /// </summary>
    public static void Reset()
    {
        for (var i = 0; i < _bots.Count; i++)
        {
            _bots[i]?.Delete();
        }

        for (var i = _away.Count - 1; i >= 0; i--)
        {
            _away[i]?.Delete();
        }

        _bots.Clear();
        _away.Clear();

        _holes = 0;
        _named = 0;
    }

    public static string Describe()
    {
        var fallen = 0;

        for (var i = 0; i < _bots.Count; i++)
        {
            if (_bots[i] is { Deleted: false, Fallen: true })
            {
                fallen++;
            }
        }

        return $"{Count} bots, {Living} on their feet, {fallen} waiting to be revived, {_away.Count} resting; "
            + $"roam {Roam} tiles, a leash of {(int)(Roam * Math.Clamp(LeashFloor, 0.0, 1.0))} to {Roam} from where the bot lives by trade progress against {GrownProgress:F2}, {Leashed} choices refused by one, {Unjoined} places refused for lying on a land no gate joins, {Unreached} with no open ground on the reach map within {BotReachMap.SampleReach}";
    }

    /// <summary>Fills a hole if there is one, so the list does not grow for ever across a long session.</summary>
    private static void Enlist(BotMobile bot)
    {
        // Staggered across one step's worth of turns, which is what spreads the population's work across the
        // clock's ticks. Seeded from a real tick rather than left at zero, because a due time of zero is
        // already overdue — and on a host whose counter starts enormous, zero is not even in the past.
        var step = Math.Max(1, BotWalk.StepDelayMs(BotMobile.Runs));

        // <b>Across the idle look rather than across one step (01.10.2026, build 382).</b> One step is two ticks, so every bot
        // raised at boot took its first turn — its first auction, every proposer asked cold, the ground round it swept for the
        // first time — on one of the first two ticks: 1907, 2231 and 2075ms on the first tick of the three sessions of the
        // evening of 30.09.2026, all of it "in the bots' turns", and the clock's worst tick 3264–3626ms in each. Spread over
        // BotWill.IdleMs — twenty ticks, six bots a tick — which is the longest a bot with nothing on is left standing by
        // design; a newcomer raised mid-session waits the same two seconds, unseen. The beat after the first turn is unchanged:
        // a turn is due a step after the last, whichever tick the first fell on.
        var spread = Math.Max(step, BotWill.IdleMs);

        bot.Scheduled = true;
        bot.DueTick = Core.TickCount + Count * BotBeat.IntervalMs % spread;

        if (_holes > 0)
        {
            for (var i = 0; i < _bots.Count; i++)
            {
                if (_bots[i] != null)
                {
                    continue;
                }

                _bots[i] = bot;
                _holes--;

                return;
            }
        }

        _bots.Add(bot);
    }

    /// <summary>
    /// Somewhere near home that the engine agrees a body can stand.
    ///
    /// <b>Asked of the engine rather than assumed</b> — <see cref="Map.CanSpawnMobile"/> is the same test the
    /// shard's own spawners use, including the region's opinion and a search for a floor within a few units
    /// of the configured height. A bot placed inside a wall is a bot whose first act is to prove that there
    /// is no way out of it.
    /// </summary>
    /// <summary>Bodies put back on the island because there was no walking back. See <see cref="Carry"/>.</summary>
    public static long Carried { get; private set; }

    /// <summary>
    /// Puts a bot back where the population lives, with no diagnosis and no argument.
    ///
    /// <para>
    /// <b>Not <see cref="Rescue"/>, and the difference is whether anything went wrong.</b> A rescue is a
    /// finding — a dozen roads refused one after another, written down at length because a bot that can
    /// reach nothing is a fault somewhere. This is the way up out of a dungeon: the block the dungeons are
    /// cut into has no walkable road to the island at all, so a party that has finished has to be put back
    /// the same way it was brought in, and that is an ordinary event rather than a symptom.
    /// </para>
    /// </summary>
    public static bool Carry(BotMobile bot)
    {
        if (bot is not { Deleted: false } || !TryPlace(bot))
        {
            return false;
        }

        Carried++;

        return true;
    }

    /// <summary>Puts a bot down where it lives, as a birth does. For the boot's newborns moved into their towns (BotRelocate.MoveIn).</summary>
    public static bool PlaceAtHome(BotMobile bot) => TryPlace(bot);

    private static bool TryPlace(BotMobile bot)
    {
        var map = Home;

        if (bot == null || map == null || map == Map.Internal)
        {
            return false;
        }

        // <b>A tile a body fits on is not the same as a tile a body can leave.</b> CanSpawnMobile counts the
        // other bots, so nobody is put on top of anybody; what it does not ask is whether all eight
        // neighbours are taken too. With forty-nine bots living in one patch that is a common shape, and it
        // is what the rescue kept producing: carried home at 01:29:26, "can reach nothing" at 01:29:58, from
        // ground the shard's own line called good.
        //
        // So the first pass wants a way out as well as a place to stand, and only the second settles for a
        // place to stand. The second is counted, because the day this reads high the patch is full and the
        // number to change is Spread.
        //
        // <b>Beside its guild's hall, or at its guild's seat, before the population's one home.</b>
        // Patrick's order of 13.09.2026: 3,597 revivals in twelve hours all landed on the same patch by the
        // graveyard, and that patch was the whole of where the population lived. See BotSeat.Home. The
        // population's home is still the fallback, and still where every bot without a guild goes.
        //
        // <b>And in the town it lives in, since 29.09.2026</b> (BotResidence): round the town's bank, by its own scatter.
        var at = HomeOf(bot);

        if (at != Where)
        {
            BotResidence.Placed(bot, at);
        }

        // Around Britain rather than on one tile of it, for a bot that has no hall to be born beside. See Scatter.
        var span = at == Where && Scatter > Spread ? Scatter : BotResidence.Span(at, Spread);

        for (var pass = 0; pass < 3; pass++)
        {
            // The last pass is the old behaviour: the home patch, and a place to stand is enough.
            var reach = pass < 2 ? span : Spread;

            for (var attempt = 0; attempt < Attempts; attempt++)
            {
                var x = at.X + Utility.RandomMinMax(-reach, reach);
                var y = at.Y + Utility.RandomMinMax(-reach, reach);

                if (!map.CanSpawnMobile(x, y, at.Z - 8, at.Z + 8, false, false, out var z))
                {
                    continue;
                }

                if (pass == 0 && !Roomy(map, x, y, z))
                {
                    continue;
                }

                if (pass > 0)
                {
                    Boxedin++;
                }

                bot.MoveToWorld(new Point3D(x, y, z), map);

                return true;
            }
        }

        // The configured point itself, whatever is standing on it. Better a crowded tile than no population.
        if (!map.CanSpawnMobile(Where))
        {
            return false;
        }

        bot.MoveToWorld(Where, map);

        return true;
    }

    /// <summary>
    /// Whether a body standing here would have somewhere to put its first step: a neighbouring tile the
    /// planner allows and nobody is standing on.
    ///
    /// <para>
    /// Both halves are needed and they come from different places. <see cref="BotStep.Mask"/> is the
    /// planner's own answer about the ground and knows nothing about who is on it - creatures move, so a
    /// planner that treated them as walls would teach itself walls that are not there. Whether anybody is
    /// standing on the tile is a question about this second, and this is the one second it matters.
    /// </para>
    /// </summary>
    private static bool Roomy(Map map, int x, int y, int z)
    {
        var footing = (sbyte)Math.Clamp(z, sbyte.MinValue, sbyte.MaxValue);
        var mask = BotStep.Mask(map, x, y, footing);

        if (mask.WalkMask == 0)
        {
            return false;
        }

        for (var d = 0; d < 8; d++)
        {
            if ((mask.WalkMask & (1 << d)) == 0)
            {
                continue;
            }

            var nx = x;
            var ny = y;

            Movement.Movement.Offset((Direction)d, ref nx, ref ny);

            if (map.CanSpawnMobile(nx, ny, z - 8, z + 8, false, false, out _))
            {
                return true;
            }
        }

        return false;
    }

    private static int _named;

    /// <summary>
    /// Names somebody else hands out, so that this pool can be told it must not hand out the same ones.
    ///
    /// <para>
    /// <b>Two bots with one name are not a cosmetic fault.</b> <c>BotProgress</c> files what a bot has
    /// learned under its name, so a pair sharing one shares a skill record and overwrites it at every
    /// save; <c>BotRoster</c> and the minds resolve a bot from a name the model said, so an order to put
    /// somebody out of a guild can land on the wrong body; and every line of the log about either of them
    /// reads as a line about the other. That last one is the expensive part, because it makes the shard
    /// look as though it is doing something impossible and there is no way to tell from the log that it
    /// is not.
    /// </para>
    ///
    /// <para>
    /// Said at boot and not fixed silently, deliberately. Skipping a reserved name would shift every name
    /// after it by one, and since a name is what a bot's learning is filed under, that quietly starts half
    /// the population over. The fault is in two lists that have to agree, it is a one-line edit, and what
    /// is wanted is to be told about it — loudly, once, by name.
    /// </para>
    /// </summary>
    public static void Reserve(IReadOnlyList<string> names, string who)
    {
        if (names == null)
        {
            return;
        }

        for (var i = 0; i < names.Count; i++)
        {
            var name = names[i];

            if (string.IsNullOrWhiteSpace(name))
            {
                continue;
            }

            // The bank's names too: a mind's name that is also a people's name is the same collision. See BotPeoples.
            if (BotPeoples.Knows(name))
            {
                logger.Error(
                    "{Name} is both a name the bank of peoples hands out (Configuration/bot-names.json) and a name {Who} gives away, so two bots will answer to it and share one record of what they have learned. Take it out of one of them",
                    name,
                    who
                );
            }

            for (var j = 0; j < Names.Length; j++)
            {
                if (!Names[j].InsensitiveEquals(name))
                {
                    continue;
                }

                logger.Error(
                    "{Name} is both the {Which} name the population hands out and a name {Who} gives away, so two bots will answer to it and share one record of what they have learned. Take it out of BotPopulation.Names",
                    name,
                    (j + 1).ToString(),
                    who
                );
            }
        }
    }

    /// <summary>
    /// A name, and it is not decoration. The population is rebuilt every world load, so a name is the only
    /// thing about a bot that survives a restart — which is why the slow tier files what a bot has learned
    /// under one, and why the log is readable at all.
    /// </summary>
    private static string Christen()
    {
        // <b>Never a name somebody is already wearing.</b> The count starts at nought with every process, and since
        // 18.09.2026 the bots come back from the save instead of being raised, so the first bot raised after a boot —
        // a shortfall, a newcomer — was handed the first name of the pool, which the first bot ever raised is still
        // wearing. On a fresh world nobody wears anything yet, so the order there is exactly what it always was.
        string name;

        do
        {
            name = NameAt(_named++);
        }
        while (InUse(name) && _named < Names.Length * (Houses.Length + 1));

        return name;
    }

    /// <summary>Whether a bot in the world or resting answers to this name.</summary>
    private static bool InUse(string name)
    {
        for (var i = 0; i < _bots.Count; i++)
        {
            if (_bots[i] is { Deleted: false } bot && bot.Name.InsensitiveEquals(name))
            {
                return true;
            }
        }

        for (var i = 0; i < _away.Count; i++)
        {
            if (_away[i] is { Deleted: false } bot && bot.Name.InsensitiveEquals(name))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>The name at this place in the order they are handed out.</summary>
    private static string NameAt(int index)
    {
        var pool = Names;

        if (index < pool.Length)
        {
            return pool[index];
        }

        // <b>Past the end of the pool, a house name and never a number.</b> "Doran 2" is not a name, it is a
        // collision handled in public: it reads as a second copy of somebody rather than as a person, and
        // every line of the log, every stall report and every remark the population makes about itself then
        // carries the seam. A surname costs nothing, keeps the given name — which is what makes a bot
        // recognisable across a session — and gives twenty-two rounds of the pool before anything repeats.
        //
        // The name is the only thing about a bot that survives a restart, and what a bot has learned is
        // filed under it, so widening the pool starts whoever was past the end of the old one over again.
        // That is the price of the change and it is paid once.
        var round = index / pool.Length - 1;

        return $"{pool[index % pool.Length]} {Houses[round % Houses.Length]}";
    }

    /// <summary>
    /// Given names, in the order they are handed out. Wide enough that a population of the size this shard
    /// actually runs never reaches <see cref="Houses"/> at all.
    ///
    /// <para>
    /// <b>No name here may be one <c>BotMinds</c> hands out, and the rule is enforced rather than
    /// remembered.</b> A mind renames the bot it is given to, so a pool holding one of those names puts two
    /// bots answering to it in the same log — and worse, <c>BotProgress</c> files what a bot has learned
    /// under its name, so the two overwrite each other's skills at every save and both come back wearing
    /// whichever record was written last.
    /// </para>
    ///
    /// <para>
    /// It happened. The comment here used to name four absent names — Aldric, Godric, Cedric and Baldric —
    /// which had stopped being the minds' names some time before 11.09.2026, when the five crafter minds
    /// were Roderic, Emeric, Ulric, Wulfric and Alaric and two of those five were sitting in this list. For
    /// an evening the log had two Ulrics in it: one leading a party through the Orc Caves and one staking a
    /// square in Britain for a guild the first one was not in, and reading the two as one bot cost an hour
    /// of chasing a teleport that never happened. A list that has to agree with another list is kept
    /// honest by <see cref="Reserve"/>, which says so at boot rather than leaving it to a comment.
    /// </para>
    /// </summary>
    private static readonly string[] Names =
    [
        "Alden", "Bryn", "Calla", "Doran", "Edda", "Faron", "Gerda", "Hale",
        "Ilsa", "Joss", "Kerrin", "Lysa", "Merrick", "Nessa", "Orin", "Perri",
        "Quill", "Rowan", "Sable", "Torvin", "Ulla", "Vance", "Wynn", "Yarrow",
        "Aric", "Brannoc", "Corwin", "Delwyn", "Emrys", "Fenna", "Garrow", "Hollis",
        "Isolde", "Jarek", "Kelda", "Lorcan", "Maeve", "Neriah", "Oswin", "Pell",
        "Quenna", "Ronan", "Selwyn", "Talia", "Ulwin", "Vesna", "Wystan", "Ysolt",
        "Bertram", "Cassia", "Dain", "Elspeth", "Fendrel", "Gwendra", "Harlan", "Ivo",
        "Jorunn", "Kestrel", "Leofric", "Marek", "Nyla", "Otho", "Piers", "Rhiannon"
    ];

    /// <summary>Houses, for when the given names run out. See <see cref="Christen"/>.</summary>
    private static readonly string[] Houses =
    [
        "Ashdown", "Blackbriar", "Coldwell", "Duskmere", "Eastmarch", "Fairholt",
        "Greywood", "Hartley", "Ironvale", "Larkspur", "Marlow", "Northgate",
        "Oakhurst", "Pinewood", "Quarrytop", "Ravenscar", "Stonebridge", "Thornwood",
        "Umberly", "Vinemoor", "Westford", "Yewdale"
    ];
}
