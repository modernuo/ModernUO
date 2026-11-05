using System;
using System.Collections.Generic;
using Server.Items;
using Server.Logging;
using Server.Mobiles;
using Server.Regions;

namespace Server.BotAI.V2;

/// <summary>
/// Fires in the woods: who has lit one, who sits round it, and what an hour beside one is worth to a tired bot.
///
/// <para>
/// <b>Patrick's point 6 of 29.09.2026: "More wandering, more chance meetings, more talk. Add socialisation: some bots
/// may light a campfire in the woods and others join them; this counts as rest. Let them stand and chat with each
/// other."</b> Until now a bot met another only in a company, a war or a queue at a counter, and said something only
/// when work began or ended. A fire is the one place on the island a bot goes to be with others rather than to get
/// something, and it is the engine's own: the kindling is the camping skill's <c>Kindling</c>, lit by its own
/// double-click and roll, and what burns is the engine's <c>Campfire</c>, which puts itself out after a hundred seconds
/// unless somebody feeds it (<see cref="TryLight"/>).
/// </para>
///
/// <para>
/// <b>Rest by a fire is rest at the inn's rate.</b> <see cref="BotRest"/> counts play only while a bot is in the world,
/// and gives a whole session back for a rest of five to eight hours; a bot sitting at a lit fire has its play counted
/// backwards at that same rate (<see cref="Played"/>), so ten minutes at a fire give back about eight of the evening
/// (<see cref="RestShare"/>). And a tired bot that has sat at a fire for the thirty seconds the engine takes to call a
/// camp secure (<see cref="SecureMs"/>, <c>Campfire.OnTick</c>) goes offline there, as it would at an inn: unpaid, so
/// no cheer, and a regeneration buff for <see cref="FireBuffShare"/> of its rest, half the paid bed's (<see cref="Rested"/>).
/// Patrick's rule of the morning — "offline only at an inn or one's own house" — gains this third place and no fourth.
/// </para>
///
/// <para>
/// <b>The fire list is not saved.</b> The engine's campfire skips serialization and burns out in a hundred seconds, so a
/// restart finds no fire to go back to; a bot that went offline by one has the fire's name in its rest record
/// (<c>BotRestStore</c>), which is all that has to survive.
/// </para>
/// </summary>
public static class BotCamp
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotCamp));

    /// <summary>What the rest record calls a night by a fire. <see cref="Rested"/> knows a fire by it.</summary>
    public const string FirePrefix = "a campfire";

    /// <summary>Whether bots light fires, join them and meet on the road at all. Off, nobody is offered any of it.</summary>
    public static bool Running { get; set; } = true;

    /// <summary>
    /// The share of the population that lights fires: "some bots may light a campfire". A half, dealt by name so a bot
    /// that lights fires lights them after a restart too. Everybody may join one.
    /// </summary>
    public static double LighterShare { get; set; } = 0.5;

    /// <summary>
    /// How far a free bot may be from a lit fire and still be offered a seat at it. Forty tiles, Patrick's order: about
    /// the distance at which a fire's smoke is seen through the trees, and a minute's walk.
    /// </summary>
    public static int JoinReach { get; set; } = 40;

    /// <summary>The least of a fire's sitting that must be left for a guest to be asked to it. See <see cref="Nearest"/> (build 386).</summary>
    public static int JoinLeastMs { get; set; } = 120000;

    /// <summary>Fires passed over as about to go out. See <see cref="JoinLeastMs"/>.</summary>
    public static long Ending { get; private set; }

    /// <summary>The most fires burning at once on the island. Six: one for every guild's worth of the evening.</summary>
    public static int MostFires { get; set; } = 6;

    /// <summary>The most seats round one fire, the keeper's among them. Six; the ring round a fire holds eight at most.</summary>
    public static int MostAtFire { get; set; } = 6;

    /// <summary>How close to a fire another may be lit. Thirty tiles: nearer than that, the second bot should sit at the first.</summary>
    public static int FireSpacing { get; set; } = 30;

    /// <summary>
    /// How long a keeper keeps its fire fed, in minutes, a quarter either way. Six: long enough for a guest forty tiles
    /// off to walk over and a few lines each, short enough that a fire is an evening's stop rather than a home.
    /// </summary>
    public static double SitMinutes { get; set; } = 6.0;

    /// <summary>How long a fire with nobody come to it is kept going. Three minutes; the keeper has rested either way.</summary>
    public static int LonelyMs { get; set; } = 180000;

    /// <summary>How long a keeper may gather and strike before a fire that has not caught is given up. Four minutes.</summary>
    public static int PrepareMs { get; set; } = 240000;

    /// <summary>
    /// How long a bot sits at a lit fire before its camp is secure: thirty seconds, the engine's own number
    /// (<c>Campfire.OnTick</c>: "The camp is now secure"). The engine keeps that clock only for a client; this is the
    /// same clock for a bot.
    /// </summary>
    public static int SecureMs { get; set; } = 30000;

    /// <summary>How long after its last beat at a fire a tired bot may still leave the world from beside it. A minute.</summary>
    public static int BedGraceMs { get; set; } = 60000;

    /// <summary>How long a fire counts as lit between one flame and the next. Thirty seconds: a tender fumbling the kindling.</summary>
    public static int FireGraceMs { get; set; } = 30000;

    /// <summary>
    /// What share of the inn's rate an hour by a fire gives back. One: the order is "at the inn's rate". The inn's rate is
    /// a session of play for a rest of <c>BotRest.LeastRestHours</c> to <c>MostRestHours</c>, so a bot that plays five
    /// hours gets about 0.77 of a minute back for every minute it sits.
    /// </summary>
    public static double RestShare { get; set; } = 1.0;

    /// <summary>
    /// What share of a rest by a fire comes back as regeneration afterwards. A sixth — half the paid bed's third
    /// (<c>BotInns.BuffShare</c>). The bed is bought (five gold an hour, some thirty a night) and has a roof; the fire cost
    /// a few sticks of kindling at two gold, or none if they were cut, and the ground is hard. More than the unpaid bed,
    /// which gives nothing, because a fire is work: the kindling is gathered and the camp kept secure, which is the
    /// engine's own condition for a camper's clean logout (<c>Bedroll</c>, <c>CampfireEntry.Safe</c>).
    /// </summary>
    public static double FireBuffShare { get; set; } = 1.0 / 6.0;

    /// <summary>Whether a tired bot may leave the world by a fire, and be sent to one before it would leave on the open ground.</summary>
    public static bool SleepByFire { get; set; } = true;

    /// <summary>How long after a tired bot was sent to a fire it may be sent again. Ten minutes, as the inn's retry is.</summary>
    public static int ToFireRetryMs { get; set; } = 600000;

    /// <summary>How near a hostile creature may come before the fire is broken up. Twelve tiles, the reach of a look round.</summary>
    public static int HostileReach { get; set; } = 12;

    /// <summary>How often a fire looks round for anything hostile: one spatial query per fire, not per sitter. Four seconds.</summary>
    public static int ThreatMs { get; set; } = 4000;

    /// <summary>
    /// The least the quadrant map may read where a fire is lit. Nought: fresh and quiet ground is fine, and anything the map
    /// calls unsafe (below −0.01) is ground bots have been hurt on lately.
    /// </summary>
    public static double LeastSafety { get; set; } = 0.0;

    /// <summary>How often a fire whose flame is dimming is fed another stick. A second and a half, as a striking hand goes.</summary>
    public static int TendTryMs { get; set; } = 1500;

    /// <summary>
    /// Whether the fire is fed when it dims (sixty seconds) rather than when it goes out (ninety). True: a tender new to the
    /// skill fumbles for twenty seconds or more, and fed at ninety the fire would be dark half the time. Costs about a
    /// stick a minute.
    /// </summary>
    public static bool RelightWhenDim { get; set; } = true;

    /// <summary>
    /// Where a bot starts to count as tired enough for the fire to be worth more to it: this share of its session played.
    /// Six-tenths — an evening player's last hour and a half. See <see cref="Claim"/>.
    /// </summary>
    public static double WearyFrom { get; set; } = 0.6;

    /// <summary>What a fire is worth a minute more to a bot at the very end of its session. Forty: above digging's forty-five prior only with boredom on top.</summary>
    public static double WearyWorth { get; set; } = 40.0;

    /// <summary>What a fire is worth a minute more to a bot bored through. Thirty: company is the cure for boredom.</summary>
    public static double BoredWorth { get; set; } = 30.0;

    /// <summary>Fires lit this session.</summary>
    public static long Lit { get; private set; }

    /// <summary>Fires fed another stick as they dimmed.</summary>
    public static long Relit { get; private set; }

    /// <summary>Strikes that did not catch: the engine's camping roll said no.</summary>
    public static long Fumbles { get; private set; }

    /// <summary>Bots that sat down at somebody else's fire.</summary>
    public static long Joined { get; private set; }

    /// <summary>Bots that left the world from beside a fire.</summary>
    public static long Slept { get; private set; }

    /// <summary>Rested buffs given for a night by a fire.</summary>
    public static long FireBuffs { get; private set; }

    /// <summary>Tired bots sent to a fire, or to make one, rather than leave on the open ground.</summary>
    public static long SentToFire { get; private set; }

    /// <summary>Tired bots with no fire near and none they could make, left to the open ground.</summary>
    public static long NoFireToSleepBy { get; private set; }

    /// <summary>Fires put out with nobody come to them after <see cref="LonelyMs"/>.</summary>
    public static long Lonely { get; private set; }

    /// <summary>Fires broken up by something hostile coming near.</summary>
    public static long Scattered { get; private set; }

    /// <summary>Fires that burned down at the end of their evening, or when nobody could feed them.</summary>
    public static long BurnedDown { get; private set; }

    /// <summary>Fires given up before they were ever lit.</summary>
    public static long NeverLit { get; private set; }

    /// <summary>Fires handed to another sitter when the keeper left.</summary>
    public static long Handed { get; private set; }

    /// <summary>Minutes bots sat at a lit fire, as the rest clock counted them.</summary>
    public static double RestedMinutes { get; private set; }

    /// <summary>Minutes of play those gave back. See <see cref="Played"/>.</summary>
    public static double GivenBack { get; private set; }

    /// <summary>Where a place lies, for the question "may a fire be lit here".</summary>
    public enum Land
    {
        Wild,
        Town,
        Roof,
        Under,
        Barred,
        Bloodied
    }

    /// <summary>One seat round a fire: where it is, who holds it, and what they have done there.</summary>
    public sealed class Place
    {
        public Point3D Spot;

        /// <summary>Who holds the seat: walking to it or sitting in it. Null while it is free.</summary>
        public BotMobile Bot;

        public bool Seated;

        public bool Greeted;

        public int Lines;

        public long SatTick;
    }

    /// <summary>One fire: where, whose, the seats round it, and the engine's flame burning on it now.</summary>
    public sealed class Fire
    {
        public Fire(BotMobile keeper, Map map, Point3D centre, List<Point3D> seats, long now)
        {
            Keeper = keeper;
            KeeperName = keeper?.Name ?? "somebody";
            Map = map;
            Centre = centre;
            Places = new Place[seats.Count];

            for (var i = 0; i < seats.Count; i++)
            {
                Places[i] = new Place { Spot = seats[i] };
            }

            Places[0].Bot = keeper;

            // Every clock seeded from a real tick: a tick may start anywhere and wrap, so nought is not "never" (CLAUDE.md, rule 20).
            CreatedTick = now;
            LitTick = now;
            BurnedTick = now;
            TendTick = now;
            ThreatTick = now;
            NextLineTick = now;
            SitMs = (long)(Math.Max(1.0, SitMinutes) * 60000 * (0.75 + 0.5 * Utility.RandomDouble()));
        }

        public Map Map { get; }

        /// <summary>The hearth: the tile the kindling is laid on and the flame burns on.</summary>
        public Point3D Centre { get; }

        public BotMobile Keeper { get; internal set; }

        public string KeeperName { get; }

        public Place[] Places { get; }

        /// <summary>What the rest record and the log call this fire.</summary>
        public string Name => $"{FirePrefix} lit by {KeeperName}";

        /// <summary>The engine's flame burning on the hearth now, or the last one.</summary>
        public Campfire Flame { get; internal set; }

        /// <summary>A stick laid on the hearth that has not caught yet; picked up again if the fire is given up.</summary>
        public Kindling Stick { get; internal set; }

        public bool Lit { get; internal set; }

        public bool Banked { get; internal set; }

        public string BankedWhy { get; internal set; }

        public bool Closed { get; internal set; }

        public string Ended { get; internal set; }

        public long CreatedTick { get; }

        public long LitTick { get; internal set; }

        public long BurnedTick { get; internal set; }

        public long TendTick { get; internal set; }

        public long ThreatTick { get; internal set; }

        public long NextLineTick { get; internal set; }

        /// <summary>How long this fire's keeper means to keep it; see <see cref="SitMinutes"/>.</summary>
        public long SitMs { get; }

        /// <summary>Everybody other than the keeper who has sat at it.</summary>
        public int Guests { get; internal set; }

        public int Lines { get; internal set; }

        public int SleptHere { get; internal set; }

        /// <summary>Whoever spoke last, so the next line is somebody else's.</summary>
        public Mobile LastSpeaker { get; internal set; }

        /// <summary>An answer owed: who says it, to whom, on what occasion, and the name it is about when one is carried.</summary>
        public (Mobile Who, Mobile To, string Occasion, string About) Owed { get; internal set; }

        /// <summary>Whether the engine's flame is burning at all (lit or dimming).</summary>
        public bool Burning => Flame is { Deleted: false } && Flame.Status != CampfireStatus.Off;

        /// <summary>Whether the flame is burning bright, so nobody need feed it.</summary>
        public bool Bright => Flame is { Deleted: false } && Flame.Status == CampfireStatus.Burning;

        /// <summary>Lit, and burning or between flames.</summary>
        public bool Warm(long now) => Lit && !Closed && (Burning || now - BurnedTick < FireGraceMs);

        /// <summary>Whether a newcomer may still be offered a seat here.</summary>
        public bool Open(long now) => Warm(now) && !Banked && Keeper != null;

        public bool Has(Mobile bot)
        {
            if (bot == null)
            {
                return false;
            }

            for (var i = 0; i < Places.Length; i++)
            {
                if (ReferenceEquals(Places[i].Bot, bot))
                {
                    return true;
                }
            }

            return false;
        }

        public Place PlaceOf(Mobile bot)
        {
            if (bot == null)
            {
                return null;
            }

            for (var i = 0; i < Places.Length; i++)
            {
                if (ReferenceEquals(Places[i].Bot, bot))
                {
                    return Places[i];
                }
            }

            return null;
        }

        /// <summary>The free seat nearest a place, or −1.</summary>
        public int Free(Point3D from)
        {
            var best = -1;
            var nearest = int.MaxValue;

            for (var i = 0; i < Places.Length; i++)
            {
                if (Places[i].Bot != null)
                {
                    continue;
                }

                var away = Tiles(Places[i].Spot, from);

                if (away < nearest)
                {
                    nearest = away;
                    best = i;
                }
            }

            return best;
        }

        public int SeatedCount()
        {
            var n = 0;

            for (var i = 0; i < Places.Length; i++)
            {
                if (Places[i] is { Seated: true, Bot: not null })
                {
                    n++;
                }
            }

            return n;
        }

        public override string ToString() => $"{Name} at ({Centre.X}, {Centre.Y})";
    }

    /// <summary>Where a bot last sat at a fire, and for how long: what the rest clock and the leaving ask.</summary>
    private sealed class Sitting
    {
        public Fire Fire;

        public long Since;

        public long Seen;
    }

    private static readonly List<Fire> _fires = [];

    private static readonly Dictionary<Serial, Sitting> _sitting = [];

    private static readonly Dictionary<Serial, long> _sentToFire = [];

    private static readonly List<Serial> _stale = [];

    private static long _sweptTick;

    private static bool _swept;

    /// <summary>The fires on the island now, lit or being made.</summary>
    public static IReadOnlyList<Fire> Fires => _fires;

    /// <summary>
    /// Whether this name is one of the bots that light fires. Dealt by a hash of the name rather than a roll, so the
    /// same bots are the fire-lighters after a restart — string hashing in .NET is seeded per process, so it is FNV here.
    /// </summary>
    public static bool Lighter(Mobile bot)
    {
        if (bot?.Name == null)
        {
            return false;
        }

        var hash = 2166136261u;
        var name = bot.Name;

        for (var i = 0; i < name.Length; i++)
        {
            hash ^= char.ToLowerInvariant(name[i]);
            hash *= 16777619u;
        }

        return hash % 1000 < Math.Clamp(LighterShare, 0.0, 1.0) * 1000;
    }

    /// <summary>Sticks of kindling in the pack.</summary>
    public static int Sticks(Mobile bot) => bot?.Backpack?.GetAmount(typeof(Kindling)) ?? 0;

    /// <summary>A blade that cuts kindling off a tree — the engine's own use of a knife or a sword (<c>BladedItemTarget</c>) — held or carried.</summary>
    public static Item Blade(Mobile bot)
    {
        if (bot == null)
        {
            return null;
        }

        if (bot.FindItemOnLayer(Layer.OneHanded) is BaseKnife or BaseSword)
        {
            return bot.FindItemOnLayer(Layer.OneHanded);
        }

        if (bot.FindItemOnLayer(Layer.TwoHanded) is BaseKnife or BaseSword)
        {
            return bot.FindItemOnLayer(Layer.TwoHanded);
        }

        var pack = bot.Backpack;

        return pack == null ? null : (Item)pack.FindItemByType<BaseKnife>() ?? pack.FindItemByType<BaseSword>();
    }

    /// <summary>
    /// Whether the engine's roll could ever light a fire for this bot. <c>Kindling</c> rolls Camping between nought and a
    /// hundred, so at nought it never catches — but every try under ten points gains, unless the bot is at its skill cap,
    /// where a player gains nothing (<c>SkillCheck.Gain</c>). Such a bot can sit at a fire and never make one.
    /// </summary>
    public static bool Lights(Mobile bot)
    {
        var skill = bot?.Skills[SkillName.Camping];

        return skill != null && (skill.Value > 0.0 || skill.Lock == SkillLock.Up && bot.Skills.Total < bot.Skills.Cap);
    }

    /// <summary>How far through its session this bot is, from nought to one. Read off the rest's own record.</summary>
    public static double Weariness(Mobile bot)
    {
        if (bot?.Name == null || !BotRest.Records.TryGetValue(bot.Name, out var record) || record.Hours <= 0.0)
        {
            return 0.0;
        }

        return Math.Clamp(record.PlayedMinutes / (record.Hours * 60.0), 0.0, 1.0);
    }

    /// <summary>
    /// What a fire is worth a minute to this bot: the typed claim, and more as the bot tires and as it is bored. Unpaid
    /// work is priced at its claim (<c>BotAppraisal</c>), so this is the whole of what the auction weighs it by.
    /// </summary>
    public static double Claim(BotMobile bot, double prior)
    {
        var weary = Math.Clamp((Weariness(bot) - WearyFrom) / Math.Max(0.01, 1.0 - WearyFrom), 0.0, 1.0);
        var bored = bot?.Resolve?.Urges?.Boredom ?? 0.0;

        return prior + weary * WearyWorth + bored * BoredWorth;
    }

    /// <summary>Whether a fire may be lit here, and if not, why not.</summary>
    public static Land LandOf(Map map, Point3D where)
    {
        var region = Region.Find(where, map);

        if (region.IsPartOf<GuardedRegion>())
        {
            return Land.Town;
        }

        if (region.IsPartOf<HouseRegion>())
        {
            return Land.Roof;
        }

        if (region.IsPartOf<DungeonRegion>() || BotDungeon.Under(where))
        {
            return Land.Under;
        }

        if (BotBarred.Holds(map, where))
        {
            return Land.Barred;
        }

        return BotQuad.Safety(map, where) < LeastSafety ? Land.Bloodied : Land.Wild;
    }

    /// <summary>Whether another fire, lit or being made, lies within <see cref="FireSpacing"/>.</summary>
    public static bool Crowded(Map map, Point3D where)
    {
        for (var i = 0; i < _fires.Count; i++)
        {
            var fire = _fires[i];

            if (!fire.Closed && fire.Map == map && Tiles(fire.Centre, where) < FireSpacing)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Fires not yet out.</summary>
    public static int Count()
    {
        var n = 0;

        for (var i = 0; i < _fires.Count; i++)
        {
            if (!_fires[i].Closed)
            {
                n++;
            }
        }

        return n;
    }

    /// <summary>Why no seat was found, for the proposer's buckets.</summary>
    public enum Refusal
    {
        None,
        Far,
        Full,
        Unwelcome
    }

    /// <summary>
    /// The nearest fire open to this bot within <see cref="JoinReach"/>, with the seat it would take. A walk of a list of
    /// at most <see cref="MostFires"/>; no spatial query.
    /// </summary>
    public static Fire Nearest(BotMobile bot, long now, out int seat, out Refusal refusal)
    {
        seat = -1;
        refusal = Refusal.Far;

        Fire best = null;
        var nearest = int.MaxValue;

        for (var i = 0; i < _fires.Count; i++)
        {
            var fire = _fires[i];

            if (!fire.Open(now) || fire.Map != bot.Map || fire.Has(bot))
            {
                continue;
            }

            // <b>Not a fire about to go out (01.10.2026, build 386).</b> A guest asked in the last minute of a fire's evening walked
            // up to embers: "was out before it got there: it burned down: the evening is over". A fire with less than
            // JoinLeastMs of its sitting left is nobody's invitation.
            if (fire.SitMs - (now - fire.LitTick) < JoinLeastMs)
            {
                Ending++;

                continue;
            }

            var away = Tiles(fire.Centre, bot.Location);

            if (away > JoinReach || away >= nearest)
            {
                continue;
            }

            if (!Welcome(bot, fire.Keeper))
            {
                refusal = Refusal.Unwelcome;

                continue;
            }

            var free = fire.Free(bot.Location);

            if (free < 0)
            {
                refusal = Refusal.Full;

                continue;
            }

            best = fire;
            nearest = away;
            seat = free;
        }

        if (best != null)
        {
            refusal = Refusal.None;
        }

        return best;
    }

    /// <summary>
    /// Whether two bots would sit together: each innocent or an ally in the other's eyes, the engine's own word
    /// (<c>Notoriety.Compute</c>) — so a guild at war, a murderer and a criminal are not asked to the fire.
    /// </summary>
    public static bool Welcome(Mobile one, Mobile other)
    {
        if (one == null || other == null || ReferenceEquals(one, other))
        {
            return true;
        }

        var there = Notoriety.Compute(one, other);
        var back = Notoriety.Compute(other, one);

        return there is Notoriety.Innocent or Notoriety.Ally && back is Notoriety.Innocent or Notoriety.Ally;
    }

    /// <summary>
    /// Where to lay a fire near this bot: its own tile first, then a few round it. The hearth must be wild ground with no
    /// fire within <see cref="FireSpacing"/>, and have at least <c>BotKindle.LeastSeats</c> tiles round it at two tiles'
    /// remove a body can stand on — every seat inside the two tiles a tender must be within to feed it (<c>Kindling</c>).
    /// </summary>
    public static bool Hearth(Mobile body, Map map, int reach, int tries, out Point3D centre, out List<Point3D> seats, out string why)
    {
        centre = Point3D.Zero;
        seats = null;
        why = "no ground was looked at";

        for (var t = 0; t <= tries; t++)
        {
            var x = t == 0 ? body.X : body.X + Utility.RandomMinMax(-reach, reach);
            var y = t == 0 ? body.Y : body.Y + Utility.RandomMinMax(-reach, reach);

            if (!BotStep.Settle(map, x, y, out var z))
            {
                why = "no footing";

                continue;
            }

            var here = new Point3D(x, y, z);
            var land = LandOf(map, here);

            if (land != Land.Wild)
            {
                why = $"the ground is {Words(land)}";

                continue;
            }

            if (Crowded(map, here))
            {
                why = $"another fire burns within {FireSpacing} tiles";

                continue;
            }

            if (!map.CanFit(x, y, z, 16, false, false))
            {
                why = "nothing can be laid there";

                continue;
            }

            var ring = Ring(map, here, body.Location);

            if (ring.Count < BotKindle.LeastSeats)
            {
                why = $"only {ring.Count} places to sit round it";

                continue;
            }

            centre = here;
            seats = ring;

            return true;
        }

        return false;
    }

    // The ring two tiles out: the eight principal points first, then the eight between them.
    private static readonly (int X, int Y)[] RingOffsets =
    [
        (0, -2), (2, -2), (2, 0), (2, 2), (0, 2), (-2, 2), (-2, 0), (-2, -2),
        (1, -2), (2, -1), (2, 1), (1, 2), (-1, 2), (-2, 1), (-2, -1), (-1, -2)
    ];

    /// <summary>
    /// Seats round a hearth: standable tiles two out, no two seats side by side, the first the one nearest the keeper.
    /// Every one is settled against the map (<c>BotStep.Settle</c>): an invented height is a place nobody can stand.
    /// </summary>
    private static List<Point3D> Ring(Map map, Point3D centre, Point3D keeper)
    {
        List<Point3D> seats = [];
        var most = Math.Clamp(MostAtFire, 2, RingOffsets.Length);

        for (var i = 0; i < RingOffsets.Length && seats.Count < most; i++)
        {
            var x = centre.X + RingOffsets[i].X;
            var y = centre.Y + RingOffsets[i].Y;

            if (!BotStep.Settle(map, x, y, out var z) || Math.Abs(z - centre.Z) > 8 || !map.CanFit(x, y, z, 16, false, false))
            {
                continue;
            }

            var spot = new Point3D(x, y, z);
            var apart = true;

            for (var k = 0; k < seats.Count && apart; k++)
            {
                apart = Tiles(seats[k], spot) >= 2;
            }

            if (apart)
            {
                seats.Add(spot);
            }
        }

        // The keeper's seat first: the one nearest where it stands.
        var first = 0;

        for (var i = 1; i < seats.Count; i++)
        {
            if (Tiles(seats[i], keeper) < Tiles(seats[first], keeper))
            {
                first = i;
            }
        }

        if (first > 0)
        {
            (seats[0], seats[first]) = (seats[first], seats[0]);
        }

        return seats;
    }

    /// <summary>A fire laid, not yet lit: on the list from now, so nobody lays another beside it.</summary>
    public static Fire Lay(BotMobile keeper, Map map, Point3D centre, List<Point3D> seats)
    {
        var fire = new Fire(keeper, map, centre, seats, Core.TickCount);

        _fires.Add(fire);

        return fire;
    }

    /// <summary>
    /// One strike at the hearth, the engine's way: a stick out of the pack is laid on the hearth and double-clicked
    /// (<c>Kindling.OnDoubleClick</c>), which rolls Camping and, when it catches, puts the engine's <c>Campfire</c> where
    /// the stick lay. A stick laid on the ground rather than lit from the pack, because from the pack the engine puts the
    /// flame on a random tile beside the striker, and the seats are counted round one hearth. Returns false when there
    /// was nothing to strike with; <paramref name="caught"/> says whether it caught.
    /// </summary>
    internal static bool TryLight(Fire fire, Mobile tender, out bool caught)
    {
        caught = false;

        if (fire?.Map == null || tender?.Backpack == null)
        {
            return false;
        }

        var map = fire.Map;
        var stick = fire.Stick;

        if (stick is not { Deleted: false } || stick.Parent != null || stick.Map != map || stick.X != fire.Centre.X || stick.Y != fire.Centre.Y)
        {
            var carried = tender.Backpack.FindItemByType<Kindling>();

            if (carried == null)
            {
                return false;
            }

            if (carried.Amount > 1)
            {
                carried.Consume(1);
                stick = new Kindling();
            }
            else
            {
                stick = carried;
            }

            stick.MoveToWorld(fire.Centre, map);
            fire.Stick = stick;
        }

        var old = fire.Flame;

        stick.OnDoubleClick(tender);

        if (!stick.Deleted)
        {
            // The roll said no, and the stick is still lying on the hearth for the next strike.
            Fumbles++;

            return true;
        }

        fire.Stick = null;

        Campfire flame = null;

        foreach (var item in map.GetItemsAt<Campfire>(fire.Centre))
        {
            if (item is { Deleted: false } && !ReferenceEquals(item, old) && (flame == null || item.Created > flame.Created))
            {
                flame = item;
            }
        }

        if (flame == null)
        {
            return true;
        }

        // The new stick catches from the old embers: one flame on the hearth, not two.
        if (old is { Deleted: false })
        {
            old.Delete();
        }

        fire.Flame = flame;
        fire.BurnedTick = Core.TickCount;
        caught = true;

        return true;
    }

    /// <summary>The fire has caught for the first time: it is on offer from now.</summary>
    internal static void Kindled(Fire fire, Mobile keeper)
    {
        var now = Core.TickCount;

        fire.Lit = true;
        fire.LitTick = now;
        fire.BurnedTick = now;
        fire.NextLineTick = now;
        Lit++;

        logger.Information(
            "{Name} the {Class} has lit a campfire at ({X}, {Y}) with {Seats} seats round it (Camping {Skill:F1}); it means to keep it {Minutes:F0} minutes",
            keeper?.Name,
            (keeper as BotMobile)?.Class?.Name,
            fire.Centre.X,
            fire.Centre.Y,
            fire.Places.Length,
            keeper?.Skills[SkillName.Camping].Value ?? 0.0,
            fire.SitMs / 60000.0
        );

        BotEvents.Post("camp", keeper, $"lit a campfire at ({fire.Centre.X}, {fire.Centre.Y})");
    }

    /// <summary>A bot takes a seat: held until it walks away or the fire is out.</summary>
    internal static bool Hold(Fire fire, BotMobile bot, ref int seat)
    {
        if (fire == null || bot == null || fire.Closed)
        {
            return false;
        }

        if (fire.PlaceOf(bot) is { } held)
        {
            seat = Array.IndexOf(fire.Places, held);

            return true;
        }

        if (seat < 0 || seat >= fire.Places.Length || fire.Places[seat].Bot != null)
        {
            seat = fire.Free(bot.Location);
        }

        if (seat < 0)
        {
            return false;
        }

        fire.Places[seat].Bot = bot;
        fire.Places[seat].Seated = false;
        fire.Places[seat].Greeted = false;
        fire.Places[seat].Lines = 0;

        return true;
    }

    /// <summary>A bot has sat down in its seat.</summary>
    internal static void Sat(Fire fire, BotMobile bot)
    {
        if (fire?.PlaceOf(bot) is not { } place || place.Seated)
        {
            return;
        }

        place.Seated = true;
        place.SatTick = Core.TickCount;

        if (!ReferenceEquals(bot, fire.Keeper))
        {
            fire.Guests++;
            Joined++;

            BotEvents.Post("camp", bot, $"sat down at {fire.Name}");
        }
    }

    /// <summary>
    /// A seated bot's beat at a lit fire, for the rest clock: the first beat starts the sitting, every beat after keeps
    /// it. Not a count of beats — the clock asks only when the sitting began and when it was last seen.
    /// </summary>
    internal static void Seated(Fire fire, BotMobile bot, long now)
    {
        if (fire == null || bot == null || !fire.Warm(now))
        {
            return;
        }

        if (!_sitting.TryGetValue(bot.Serial, out var sitting) || !ReferenceEquals(sitting.Fire, fire) || now - sitting.Seen > BedGraceMs)
        {
            _sitting[bot.Serial] = new Sitting { Fire = fire, Since = now, Seen = now };

            return;
        }

        sitting.Seen = now;
    }

    /// <summary>Whether this bot has sat at a lit fire long enough for its camp to be secure. See <see cref="SecureMs"/>.</summary>
    public static bool Secure(Mobile bot) =>
        bot != null && _sitting.TryGetValue(bot.Serial, out var sitting) && sitting.Seen - sitting.Since >= SecureMs
        && Core.TickCount - sitting.Seen <= BedGraceMs;

    /// <summary>
    /// A bot leaves its seat. When it was the keeper of a lit fire, the fire goes to whoever else sits there with kindling
    /// and the skill to feed it; with nobody, it is banked and burns down. A keeper leaving a fire never lit takes it away.
    /// </summary>
    internal static void Let(Fire fire, BotMobile bot, string why)
    {
        if (fire == null || bot == null)
        {
            return;
        }

        var place = fire.PlaceOf(bot);

        if (place != null)
        {
            place.Bot = null;
            place.Seated = false;
        }

        if (!ReferenceEquals(fire.Keeper, bot) || fire.Closed)
        {
            return;
        }

        // Closed while the keeper is still named, so the stick it laid goes back into its pack.
        if (!fire.Lit)
        {
            Close(fire, $"its keeper went before it was lit: {why}");
            fire.Keeper = null;

            return;
        }

        fire.Keeper = null;

        for (var i = 0; i < fire.Places.Length; i++)
        {
            var other = fire.Places[i];

            if (other is { Seated: true, Bot: { Deleted: false, Alive: true } heir } && Sticks(heir) > 0 && Lights(heir))
            {
                fire.Keeper = heir;
                Handed++;

                logger.Information("{Keeper} leaves {Fire}, and {Heir} keeps it going", bot.Name, fire.Name, heir.Name);

                return;
            }
        }

        Bank(fire, $"{bot.Name} left it ({why}) and nobody had kindling to keep it");
    }

    /// <summary>No more sticks: the fire burns down and closes when the flame is out.</summary>
    internal static void Bank(Fire fire, string why)
    {
        if (fire == null || fire.Banked || fire.Closed)
        {
            return;
        }

        fire.Banked = true;
        fire.BankedWhy = why;
    }

    /// <summary>The fire is over: off the list, and every deed at it ends on its next beat.</summary>
    internal static void Close(Fire fire, string why)
    {
        if (fire == null || fire.Closed)
        {
            return;
        }

        fire.Closed = true;
        fire.Ended = why;

        // A stick that never caught goes back to whoever laid it, or stays on the ground for the engine to decay.
        if (fire.Stick is { Deleted: false, Parent: null } stick)
        {
            var keeper = fire.Keeper;

            if (keeper is { Deleted: false, Alive: true } && keeper.Map == stick.Map && keeper.InRange(stick.Location, 2))
            {
                keeper.Backpack?.DropItem(stick);
            }

            fire.Stick = null;
        }

        if (fire.Lit)
        {
            logger.Information(
                "{Fire} at ({X}, {Y}) is out after {Minutes:F1} minutes — {Why}; {Guests} came to it, {Lines} lines were said and {Slept} slept by it",
                fire.Name,
                fire.Centre.X,
                fire.Centre.Y,
                (Core.TickCount - fire.LitTick) / 60000.0,
                why,
                fire.Guests,
                fire.Lines,
                fire.SleptHere
            );
        }
    }

    /// <summary>
    /// The camp's clock: every fire fed, looked round, talked at and closed when it is out. From the module's timer, once a
    /// second; one spatial query per fire every <see cref="ThreatMs"/>, and none for anything else.
    /// </summary>
    public static void Tick(long now)
    {
        for (var i = _fires.Count - 1; i >= 0; i--)
        {
            var fire = _fires[i];

            try
            {
                Keep(fire, now);
            }
            catch (Exception e)
            {
                logger.Error(e, "{Fire} threw on its beat and is put out", fire.Name);
                Close(fire, "a fault on its beat");
            }

            if (fire.Closed)
            {
                _fires.RemoveAt(i);
            }
        }

        // Sittings long over are forgotten once a minute; the leaving only asks about the last minute.
        if (!_swept || now - _sweptTick >= 60000)
        {
            _swept = true;
            _sweptTick = now;
            _stale.Clear();

            foreach (var (serial, sitting) in _sitting)
            {
                if (now - sitting.Seen > 5 * BedGraceMs)
                {
                    _stale.Add(serial);
                }
            }

            for (var k = 0; k < _stale.Count; k++)
            {
                _sitting.Remove(_stale[k]);
            }
        }
    }

    private static void Keep(Fire fire, long now)
    {
        // Seats held by bots that are no longer at this fire's work are freed. A bot whose work here is only put down —
        // for a rescue, a flight — keeps its seat for when it takes it up again.
        for (var i = 0; i < fire.Places.Length; i++)
        {
            var place = fire.Places[i];

            if (place.Bot is { } bot && !AtFire(bot, fire))
            {
                Let(fire, bot, "it went to other work");

                if (fire.Closed)
                {
                    return;
                }
            }
        }

        if (!fire.Lit)
        {
            if (now - fire.CreatedTick >= PrepareMs)
            {
                NeverLit++;
                Close(fire, $"it had not caught in {PrepareMs / 60000} minutes");
            }

            return;
        }

        if (fire.Burning)
        {
            fire.BurnedTick = now;
        }

        if (now - fire.ThreatTick >= ThreatMs)
        {
            fire.ThreatTick = now;

            var anchor = Anchor(fire);

            if (anchor != null && BotThreat.Strongest(anchor, HostileReach) is { } foe)
            {
                Scattered++;
                Close(fire, $"scattered by {foe.Name} coming within {HostileReach} tiles");

                return;
            }
        }

        if (!fire.Banked)
        {
            if (now - fire.LitTick >= fire.SitMs)
            {
                Bank(fire, "the evening is over");
            }
            else if (fire.Guests == 0 && now - fire.LitTick >= LonelyMs)
            {
                Lonely++;
                Bank(fire, $"nobody came in {LonelyMs / 60000} minutes");
            }
        }

        var dim = RelightWhenDim ? !fire.Bright : !fire.Burning;

        if (!fire.Banked && dim && now - fire.TendTick >= TendTryMs)
        {
            fire.TendTick = now;

            var tender = Tender(fire);

            if (tender != null && TryLight(fire, tender, out var caught) && caught)
            {
                Relit++;
            }
        }

        if (!fire.Warm(now))
        {
            BurnedDown++;
            Close(fire, fire.Banked ? $"it burned down: {fire.BankedWhy}" : "it went out and nobody could feed it");

            return;
        }

        BotCampTalk.Speak(fire, now);
    }

    /// <summary>Whether this bot's work, in hand or put down, is at this fire.</summary>
    private static bool AtFire(BotMobile bot, Fire fire)
    {
        if (bot.Deleted || !bot.Alive || bot.Map != fire.Map)
        {
            return false;
        }

        return Of(bot.Resolve?.Deed) == fire || Of(bot.Resolve?.Paused?.Deed) == fire;
    }

    private static Fire Of(BotDeed deed) =>
        deed switch
        {
            BotKindle kindle => kindle.Fire,
            BotFireside fireside => fireside.Fire,
            _ => null
        };

    /// <summary>Somebody seated to look round from: the keeper first.</summary>
    private static Mobile Anchor(Fire fire)
    {
        if (fire.PlaceOf(fire.Keeper) is { Seated: true })
        {
            return fire.Keeper;
        }

        for (var i = 0; i < fire.Places.Length; i++)
        {
            if (fire.Places[i] is { Seated: true, Bot: { Deleted: false, Alive: true } bot })
            {
                return bot;
            }
        }

        return null;
    }

    /// <summary>Who feeds the fire: the keeper if it can, else anybody seated within reach of the hearth with a stick.</summary>
    private static Mobile Tender(Fire fire)
    {
        Mobile best = null;

        for (var i = 0; i < fire.Places.Length; i++)
        {
            if (fire.Places[i] is not { Seated: true, Bot: { Deleted: false, Alive: true } bot })
            {
                continue;
            }

            var away = Tiles(bot.Location, fire.Centre);

            if (away is < 1 or > 2 || Sticks(bot) <= 0 && fire.Stick == null || !Lights(bot))
            {
                continue;
            }

            if (ReferenceEquals(bot, fire.Keeper))
            {
                return bot;
            }

            best ??= bot;
        }

        return best;
    }

    // ---- The rest's hooks. Each is called from one line in BotRest, and each is a dictionary lookup for a bot at no fire.

    /// <summary>
    /// The play to add to a bot's session for the minutes that just passed: the minutes themselves, or — for a bot seated
    /// at a lit fire and not yet tired — the rest those minutes buy, as a negative number, at the inn's rate. From
    /// <c>BotRest.Look</c>.
    /// </summary>
    public static double Played(BotMobile bot, double minutes, double hours)
    {
        if (!Running || bot == null || bot.Tired || minutes <= 0.0 || !_sitting.TryGetValue(bot.Serial, out var sitting))
        {
            return minutes;
        }

        var now = Core.TickCount;

        if (now - sitting.Seen > 5000 || !sitting.Fire.Warm(now))
        {
            return minutes;
        }

        var rest = Math.Max(0.1, (BotRest.LeastRestHours + BotRest.MostRestHours) / 2.0);
        var back = minutes * Math.Max(0.0, hours) / rest * Math.Max(0.0, RestShare);

        RestedMinutes += minutes;
        GivenBack += back;

        return -back;
    }

    /// <summary>
    /// Whether a tired bot is secure at a fire and may leave the world from there. From <c>BotRest.Look</c>, before the
    /// walk home and the inn: a bot already sitting by a fire needs neither.
    /// </summary>
    public static bool Beds(BotMobile bot) => Running && SleepByFire && Asleep(bot, out _);

    private static bool Asleep(BotMobile bot, out Sitting sitting)
    {
        sitting = null;

        if (bot?.Map == null || !_sitting.TryGetValue(bot.Serial, out sitting))
        {
            return false;
        }

        return Core.TickCount - sitting.Seen <= BedGraceMs && sitting.Seen - sitting.Since >= SecureMs
               && sitting.Fire.Map == bot.Map && bot.InRange(sitting.Fire.Centre, Campfire.SecureRange);
    }

    /// <summary>
    /// A tired bot with no roof and no inn to go to is sent to the nearest open fire, or makes one where it stands, before
    /// it would leave on the open ground. From <c>BotRest.Look</c>, after the house and the inn. Returns whether it was sent.
    /// With <paramref name="fieldFirst"/>, asked before the inn for a bot whose timetable prefers the field (<c>BotDay</c>), and a
    /// miss then is that gate's own count (<c>BotDay.FieldFirstMissed</c>), not <see cref="NoFireToSleepBy"/>, whose meaning stays
    /// "no inn, no fire, the open ground" (review A of 13.10.2026, §3.3).
    /// </summary>
    public static bool ToFire(BotMobile bot, bool fieldFirst = false)
    {
        if (!Running || !SleepByFire || bot is not { Deleted: false, Alive: true } || bot.Map == null || bot.Map == Map.Internal)
        {
            return false;
        }

        // Standing in an inn already: the inn is the better bed.
        if (BotInns.At(bot) != null)
        {
            return false;
        }

        var now = Core.TickCount;

        if (_sentToFire.TryGetValue(bot.Serial, out var sent) && now - sent < ToFireRetryMs)
        {
            return false;
        }

        var fire = Nearest(bot, now, out var seat, out _);
        BotDeed deed = null;
        string why = null;

        if (fire != null)
        {
            deed = new BotFireside(fire, seat, BotFireside.Prior, true);
            why = $"tired, and going to sleep by {fire.Name}";
        }
        else if (Count() < MostFires && LandOf(bot.Map, bot.Location) == Land.Wild && !Crowded(bot.Map, bot.Location)
                 && BotKindle.Means(bot) && Lights(bot) && !BotThreat.Anything(bot, HostileReach))
        {
            deed = new BotKindle(bot.Map, bot.Location, BotKindle.Prior, true);
            why = "tired, and making camp to sleep by a fire";
        }

        if (deed == null)
        {
            if (fieldFirst)
            {
                BotDay.FieldFirstMissed++;
            }
            else
            {
                NoFireToSleepBy++;
            }

            return false;
        }

        if (!BotWill.Press(bot, deed, why))
        {
            return false;
        }

        _sentToFire[bot.Serial] = now;
        SentToFire++;

        return true;
    }

    /// <summary>
    /// The bot is leaving the world: from beside a fire, or not. From <c>BotRest.Leave</c> when no inn and no house took it;
    /// the name returned is written into the rest's record, so a restart still knows where it slept.
    /// </summary>
    public static string Settle(BotMobile bot)
    {
        if (!Running || !Asleep(bot, out var sitting))
        {
            return null;
        }

        _sitting.Remove(bot.Serial);
        _sentToFire.Remove(bot.Serial);
        Slept++;
        sitting.Fire.SleptHere++;

        BotEvents.Post("camp", bot, $"went to sleep by {sitting.Fire.Name}");

        return sitting.Fire.Name;
    }

    /// <summary>
    /// A rest by a fire is over: the smaller buff. From <c>BotRest.Return</c>; false for anything that was not a fire, and
    /// the rest's record keeps the buff's end so a restart neither loses it nor grants it twice. See <see cref="FireBuffShare"/>.
    /// </summary>
    public static bool Rested(BotMobile bot, string inn, double hours, out DateTime until)
    {
        until = default;

        if (bot == null || hours <= 0.0 || FireBuffShare <= 0.0 || inn == null || !inn.StartsWith(FirePrefix, StringComparison.Ordinal))
        {
            return false;
        }

        until = Core.Now + TimeSpan.FromHours(hours * FireBuffShare);
        bot.WellRestedUntil = until;
        FireBuffs++;

        return true;
    }

    // ---- Words and numbers.

    internal static int Tiles(Point3D a, Point3D b) => Math.Max(Math.Abs(a.X - b.X), Math.Abs(a.Y - b.Y));

    public static string Words(Land land) =>
        land switch
        {
            Land.Town => "a town's",
            Land.Roof => "under a roof",
            Land.Under => "underground",
            Land.Barred => "barred",
            Land.Bloodied => "bloodied",
            _ => "wild"
        };

    public static string Describe()
    {
        var now = Core.TickCount;
        var burning = 0;
        var sitting = 0;

        for (var i = 0; i < _fires.Count; i++)
        {
            var fire = _fires[i];

            if (fire.Warm(now))
            {
                burning++;
                sitting += fire.SeatedCount();
            }
        }

        return !Running
            ? "fires are off"
            : $"{Lit} fires lit, {Joined} joined one, {Slept} went offline at a fire, {BotMeetings.Met} chats ({burning} burning now, {sitting} sitting at them); "
              + $"{Relit} fed as they dimmed and {Fumbles} strikes that did not catch, {Handed} handed on by a keeper who left; "
              + $"ended: {BurnedDown} burned down ({Lonely} of them put out with nobody come in {LonelyMs / 60000} minutes), {Scattered} scattered by something hostile, {NeverLit} never lit; "
              + $"{RestedMinutes:F0} minutes sat at a fire gave back {GivenBack:F0} minutes of play; {SentToFire} tired bots sent to a fire to sleep, {NoFireToSleepBy} had none near and could make none, "
              + $"{FireBuffs} rested buffs from a night by one ({FireBuffShare:P0} of the rest); "
              + $"lighting: {BotCamper.Describe()}; {BotKindle.Describe()}; joining: {BotBeckon.Describe()}; {BotFireside.Describe()}; talk: {BotCampTalk.Describe()}; meetings: {BotMeetings.Describe()}";
    }

    /// <summary>A new world: every fire and sitting forgotten. Flames belong to the world being replaced.</summary>
    public static void Forget()
    {
        _fires.Clear();
        _sitting.Clear();
        _sentToFire.Clear();
        Lit = 0;
        Relit = 0;
        Ending = 0;
        Fumbles = 0;
        Joined = 0;
        Slept = 0;
        FireBuffs = 0;
        SentToFire = 0;
        NoFireToSleepBy = 0;
        Lonely = 0;
        Scattered = 0;
        BurnedDown = 0;
        NeverLit = 0;
        Handed = 0;
        RestedMinutes = 0;
        GivenBack = 0;
        BotCamper.Forget();
        BotBeckon.Forget();
        BotCampTalk.Forget();
        BotMeetings.Forget();
    }
}
