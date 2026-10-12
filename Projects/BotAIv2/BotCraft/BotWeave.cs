using System;
using System.Collections.Generic;
using Server.Engines.Spawners;
using Server.Items;
using Server.Logging;
using Server.Mobiles;

namespace Server.BotAI.V2;

/// <summary>
/// The spinning wheels and looms of the towns, read from the world once.
///
/// <para>
/// A wheel and a loom are addons — the tailor's shop has both — and an addon is a set of components on the map. One pass
/// over the map's components finds every <c>ISpinningWheel</c> and every <c>ILoom</c>; where each stands is what a weaver
/// walks to. Nothing is written down, by the rule the forges and the dungeons' rooms are found by.
/// </para>
/// </summary>
public static class BotLooms
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotLooms));

    private static readonly List<(Point3D Where, ISpinningWheel Wheel)> _wheels = [];

    private static readonly List<(Point3D Where, ILoom Loom)> _looms = [];

    private static Map _map;

    public static bool Surveyed { get; private set; }

    public static int Wheels => _wheels.Count;

    public static int Looms => _looms.Count;

    public static void Survey(Map map)
    {
        if (Surveyed || map == null || map == Map.Internal)
        {
            return;
        }

        Surveyed = true;
        _map = map;

        foreach (var component in map.GetItemsInBounds<AddonComponent>(new Rectangle2D(0, 0, map.Width, map.Height)))
        {
            if (component is not { Deleted: false } || component.Addon is not { Deleted: false } addon)
            {
                continue;
            }

            var where = component.GetWorldLocation();

            switch (addon)
            {
                case ISpinningWheel wheel:
                    {
                        if (!Known(_wheels, where))
                        {
                            _wheels.Add((where, wheel));
                        }

                        break;
                    }
                case ILoom loom:
                    {
                        if (!Known(_looms, where))
                        {
                            _looms.Add((where, loom));
                        }

                        break;
                    }
            }
        }

        logger.Information("Looms: {Wheels} spinning wheels and {Looms} looms on {Map}", _wheels.Count, _looms.Count, map.Name);
    }

    private static bool Known<T>(List<(Point3D Where, T Thing)> list, Point3D where)
    {
        for (var i = 0; i < list.Count; i++)
        {
            if (Math.Max(Math.Abs(list[i].Where.X - where.X), Math.Abs(list[i].Where.Y - where.Y)) <= 2)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Whether a weaver spins and weaves only at Britain's wheels and looms, where the island's workshops are (Patrick's order of
    /// 30.09.2026: "all the workshops live in Britain"). The shearing stays in the fields; the pasture chooser already weighs the
    /// road from the flock to the nearest wheel and loom, so with this on it chooses flocks within <c>BotWeave.LoomReach</c> of
    /// Britain. Off, the nearest station anywhere, as before.
    /// </summary>
    public static bool WorkshopsInBritain { get; set; } = true;

    /// <summary>Whether a station counts: in Britain when <see cref="WorkshopsInBritain"/>, anywhere otherwise.</summary>
    private static bool Allowed(Point3D where) => !WorkshopsInBritain || BotMarket.InBritain(where);

    public static (Point3D Where, ISpinningWheel Wheel) NearestWheel(Point3D at, int reach)
    {
        var best = (Point3D.Zero, (ISpinningWheel)null);
        var nearest = reach + 1;

        for (var i = 0; i < _wheels.Count; i++)
        {
            var away = Math.Max(Math.Abs(_wheels[i].Where.X - at.X), Math.Abs(_wheels[i].Where.Y - at.Y));

            if (away < nearest && _wheels[i].Wheel is Item { Deleted: false } && Allowed(_wheels[i].Where))
            {
                nearest = away;
                best = _wheels[i];
            }
        }

        return best;
    }

    public static (Point3D Where, ILoom Loom) NearestLoom(Point3D at, int reach)
    {
        var best = (Point3D.Zero, (ILoom)null);
        var nearest = reach + 1;

        for (var i = 0; i < _looms.Count; i++)
        {
            var away = Math.Max(Math.Abs(_looms[i].Where.X - at.X), Math.Abs(_looms[i].Where.Y - at.Y));

            if (away < nearest && _looms[i].Loom is Item { Deleted: false } && Allowed(_looms[i].Where))
            {
                nearest = away;
                best = _looms[i];
            }
        }

        return best;
    }

    public static void Forget()
    {
        _wheels.Clear();
        _looms.Clear();
        _map = null;
        Surveyed = false;
    }
}

/// <summary>
/// The flocks of the map, read off the spawners that keep them: where each pasture is, and which of its sheep have their wool
/// grown.
///
/// <para>
/// <b>Patrick's word of 29.09.2026: the weavers walk to the sheep; the world is not bent to the bots.</b> Build 336 asked 1331
/// times to weave in its first 105 minutes and 872 of the answers were "no shorn sheep near": the only question asked was whether a
/// sheep with its wool stood within <see cref="BotWeave.SheepReach"/> of wherever the weaver happened to be. The needle's nine live in
/// Britain (five), Minoc (one) and Yew (three). Britain's bank has no sheep that close — the nearest are single wild ones in the
/// WildLife spawners two and three hundred tiles out — and the flocks are on the farms by Yew: three spawners of ten and three of
/// fifteen round (571–676, 939–1179), with smaller ones by Skara Brae and Jhelom. In the same window the population bought 1871
/// cloth from shopkeepers for 3773gp, the largest single thing it bought.
/// </para>
///
/// <para>
/// <b>Read off the spawners rather than swept, so the question costs a list and not a map.</b> A spawner keeps its own creatures
/// (<c>SpawnerEntry.Spawned</c>) and stands where it keeps them. One pass over the map's spawners when the first weaver asks — the
/// rule the looms above are found by — keeps those with an entry that raises sheep, and from then on "how many sheep with their wool
/// grown has that flock" is a walk over a handful of references, where the old question was a sweep of every sector within a
/// hundred and fifty tiles on every ask. Nothing is written down: a spawner that stops raising sheep answers nought.
/// </para>
///
/// <para>
/// <b>Grown by the engine's clock, not by the body.</b> <c>Sheep.Carve</c> refuses by <c>NextWoolTime</c>, and the shorn body is put
/// back only in <c>OnThink</c>, which only a running AI calls. A sheep in a sector nobody has woken (<see cref="BotWake"/>) has none,
/// so a flock whose wool grew back while nobody was near read as shorn by its body for as long as it slept.
/// </para>
/// </summary>
public static class BotPastures
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotPastures));

    /// <summary>One spawner that raises sheep, and the entries on it that do.</summary>
    public sealed class Pasture
    {
        public Pasture(BaseSpawner spawner, List<SpawnerEntry> flocks)
        {
            Spawner = spawner;
            Where = spawner.Location;
            Flocks = flocks;
        }

        public BaseSpawner Spawner { get; }

        /// <summary>Where the spawner stands, which is where it puts its sheep and where they wander round.</summary>
        public Point3D Where { get; }

        public List<SpawnerEntry> Flocks { get; }

        /// <summary>When a weaver last found it shorn, and for how long it is let be. See <see cref="Rest"/>.</summary>
        public long RestedAt { get; set; }

        public long RestFor { get; set; }

        public bool Rested { get; set; }
    }

    private static readonly List<Pasture> _pastures = [];

    private static Map _map;

    public static bool Surveyed { get; private set; }

    public static IReadOnlyList<Pasture> All => _pastures;

    public static int Count => _pastures.Count;

    /// <summary>How long a pasture found shorn is let be, at least. A spawner puts a killed sheep back in five to ten minutes.</summary>
    public static int RestLeastMs { get; set; } = 600000;

    /// <summary>And at most: the engine's own regrowth is three hours (<c>Sheep.Carve</c>).</summary>
    public static int RestMostMs { get; set; } = 10800000;

    /// <summary>Pastures a weaver reached and found with no sheep ready. For the summary.</summary>
    public static long FoundShorn { get; private set; }

    public static void Survey(Map map)
    {
        if (Surveyed || map == null || map == Map.Internal)
        {
            return;
        }

        Surveyed = true;
        _map = map;

        foreach (var spawner in map.GetItemsInBounds<BaseSpawner>(new Rectangle2D(0, 0, map.Width, map.Height)))
        {
            if (spawner is not { Deleted: false } || spawner.Entries == null)
            {
                continue;
            }

            List<SpawnerEntry> flocks = null;

            for (var i = 0; i < spawner.Entries.Count; i++)
            {
                var entry = spawner.Entries[i];

                if (string.Equals(entry?.SpawnedName?.Trim(), nameof(Server.Mobiles.Sheep), StringComparison.OrdinalIgnoreCase))
                {
                    (flocks ??= []).Add(entry);
                }
            }

            if (flocks != null)
            {
                _pastures.Add(new Pasture(spawner, flocks));
            }
        }

        var sheep = 0;
        var grown = 0;

        for (var i = 0; i < _pastures.Count; i++)
        {
            sheep += Kept(_pastures[i]);
            grown += Ready(_pastures[i]);
        }

        logger.Information(
            "Pastures: {Count} spawners on {Map} raise sheep, keeping {Sheep} now, {Grown} of them with their wool grown",
            _pastures.Count,
            map.Name,
            sheep,
            grown
        );
    }

    /// <summary>Whether this sheep can be shorn now, by the engine's own test in <c>Sheep.Carve</c>.</summary>
    public static bool Grown(Sheep sheep) =>
        sheep is { Deleted: false, Alive: true, Controlled: false } && Core.Now >= sheep.NextWoolTime;

    /// <summary>Every living, untamed sheep the pasture keeps.</summary>
    public static int Kept(Pasture pasture)
    {
        var count = 0;
        var flocks = pasture?.Flocks;

        for (var i = 0; flocks != null && i < flocks.Count; i++)
        {
            var spawned = flocks[i]?.Spawned;

            for (var j = 0; spawned != null && j < spawned.Count; j++)
            {
                if (spawned[j] is Sheep { Deleted: false, Alive: true, Controlled: false } sheep && sheep.Map == _map)
                {
                    count++;
                }
            }
        }

        return count;
    }

    /// <summary>How many of the pasture's sheep have their wool grown now.</summary>
    public static int Ready(Pasture pasture)
    {
        var count = 0;
        var flocks = pasture?.Flocks;

        for (var i = 0; flocks != null && i < flocks.Count; i++)
        {
            var spawned = flocks[i]?.Spawned;

            for (var j = 0; spawned != null && j < spawned.Count; j++)
            {
                if (spawned[j] is Sheep sheep && sheep.Map == _map && Grown(sheep))
                {
                    count++;
                }
            }
        }

        return count;
    }

    /// <summary>
    /// The pasture's sheep with its wool grown that is nearest to <paramref name="from"/>, leaving out <paramref name="shunned"/>.
    /// </summary>
    public static Sheep Nearest(Pasture pasture, Point3D from, HashSet<Serial> shunned = null)
    {
        Sheep best = null;
        var nearest = int.MaxValue;
        var flocks = pasture?.Flocks;

        for (var i = 0; flocks != null && i < flocks.Count; i++)
        {
            var spawned = flocks[i]?.Spawned;

            for (var j = 0; spawned != null && j < spawned.Count; j++)
            {
                if (spawned[j] is not Sheep sheep || sheep.Map != _map || !Grown(sheep) || shunned?.Contains(sheep.Serial) == true)
                {
                    continue;
                }

                var away = Math.Max(Math.Abs(sheep.X - from.X), Math.Abs(sheep.Y - from.Y));

                if (away < nearest)
                {
                    nearest = away;
                    best = sheep;
                }
            }
        }

        return best;
    }

    /// <summary>
    /// The nearest pasture within <paramref name="reach"/> of <paramref name="from"/> with a sheep ready, not resting, other than
    /// <paramref name="but"/>. For a weaver already out among the flocks: the next one along, as the old radius scan found the
    /// next sheep. The choice of where to walk to from home is <c>BotWeaver</c>'s, and asks a good deal more.
    /// </summary>
    public static Pasture Next(Point3D from, int reach, Pasture but)
    {
        Pasture best = null;
        var nearest = reach + 1;

        for (var i = 0; i < _pastures.Count; i++)
        {
            var pasture = _pastures[i];
            var away = Math.Max(Math.Abs(pasture.Where.X - from.X), Math.Abs(pasture.Where.Y - from.Y));

            if (away >= nearest || ReferenceEquals(pasture, but) || Resting(pasture) || Ready(pasture) <= 0)
            {
                continue;
            }

            nearest = away;
            best = pasture;
        }

        return best;
    }

    /// <summary>Whether a weaver found this pasture shorn lately enough that nobody should walk there yet.</summary>
    public static bool Resting(Pasture pasture) =>
        pasture is { Rested: true } && Core.TickCount - pasture.RestedAt < pasture.RestFor;

    /// <summary>
    /// A weaver stood at this pasture and found nothing to shear: let it be until the first of its sheep grows its wool back, or a
    /// spawner's respawn if it keeps none, and never longer than the engine's three hours.
    ///
    /// <para>
    /// <b>The one fact a walk there buys, written down so the next weaver does not pay for it again.</b> Two weavers reading the
    /// same count in the same minute both walk, and the second finds the first has shorn the lot.
    /// </para>
    /// </summary>
    public static void Rest(Pasture pasture)
    {
        if (pasture == null)
        {
            return;
        }

        var soonest = long.MaxValue;
        var flocks = pasture.Flocks;

        for (var i = 0; flocks != null && i < flocks.Count; i++)
        {
            var spawned = flocks[i]?.Spawned;

            for (var j = 0; spawned != null && j < spawned.Count; j++)
            {
                if (spawned[j] is Sheep { Deleted: false, Alive: true } sheep)
                {
                    soonest = Math.Min(soonest, (long)(sheep.NextWoolTime - Core.Now).TotalMilliseconds);
                }
            }
        }

        FoundShorn++;
        pasture.Rested = true;
        pasture.RestedAt = Core.TickCount;
        pasture.RestFor = Math.Clamp(soonest == long.MaxValue ? RestLeastMs : soonest, RestLeastMs, Math.Max(RestLeastMs, RestMostMs));
    }

    public static string Describe() =>
        !Surveyed
            ? "no pasture surveyed yet"
            : $"{_pastures.Count} pastures on the map, {FoundShorn} times a flock was found or left shorn and let be";

    public static void Forget()
    {
        _pastures.Clear();
        _map = null;
        Surveyed = false;
        FoundShorn = 0;
    }
}

/// <summary>
/// From a sheep to a bolt of cloth: shear, spin, weave, cut — and cut some of the cloth into bandages.
///
/// <para>
/// <b>Patrick's sixth point of the night of 29.09.2026: make the economy self-sufficient.</b> In twenty-five minutes of build
/// 282 the population bought 435 things over counters, and 334 of them were cloth (180) and bandages (154): the tailors'
/// cloth and everybody's bandages, and neither had a source on the island. The engine has had the whole chain since the
/// first day: a sheep with its wool grown back gives two wool to scissors (<c>Sheep.Carve</c>), a spinning wheel turns one
/// wool into three yarn (<c>Wool.OnSpun</c>), a loom takes five yarn for a bolt (<c>BaseClothMaterial</c>, phases 0–4),
/// scissors cut a bolt into fifty cloth (<c>BoltOfCloth.Scissor</c>) and a cloth into a bandage (<c>Cloth.Scissor</c>). So
/// two wool are a bolt, and a bolt is fifty cloth or fifty bandages. Britain's sheep graze by its farms; the tailor's shop
/// has the wheel and the loom.
/// </para>
///
/// <para>
/// <b>Everything here is the engine's own verb, said by the bot.</b> The shearing is the sheep's method with the bot's
/// scissors; the spinning is the wheel's timer with the wool's callback; the loom's phases are stepped exactly as the
/// yarn's target steps them; the cutting is the bolt's and the cloth's own <c>Scissor</c>. Nothing is made out of nothing.
/// </para>
/// </summary>
public sealed class BotWeave : BotDeed
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotWeave));

    public const string Trade = "weave";

    /// <summary>What weaving is worth a minute. A tailor's stint, about: the cloth it makes is what the tailor was buying.</summary>
    public static double Prior { get; set; } = 140.0;

    public static double WorkMinutes { get; set; } = 6.0;

    /// <summary>Wool to gather before spinning: two, which is one bolt.</summary>
    public static int WoolWanted { get; set; } = 2;

    /// <summary>
    /// How far round the bot a sheep is looked for, and how far a wheel or a loom may be. Sixty until 29.09.2026, a hundred and
    /// fifty since: 693 of 782 asks read "no shorn sheep near" at sixty, and Patrick's word is that the bots go to the sheep.
    /// </summary>
    public static int SheepReach { get; set; } = 150;

    /// <summary>
    /// How far a wheel and a loom may be from where the wool is: eight hundred since 30.09.2026, four hundred before.
    ///
    /// <para>
    /// <b>Yew has flocks and no loom.</b> The island's wheels and looms stand in the tailors' shops of Britain, Vesper, Skara Brae,
    /// Trinsic, Jhelom, Serpent's Hold, Moonglow, Nujel'm, Magincia, Ocllo, Delucia and Papua (the town decorations) — none in
    /// Yew and none in Minoc. The seventy-odd sheep of Yew's six farm spawners, the largest flocks on Felucca, lie 717 tiles and
    /// more from Britain's loom and a thousand from Skara Brae's, so at four hundred no flock of Yew's could ever be woven: build 336
    /// read "85 with no wheel or loom within reach", and at that reach Yew's three weavers had two lone wild sheep to go to. At
    /// eight hundred a Yew weaver shears at the farm and carries the wool to Britain; the road to the loom is counted in the
    /// flock's price (<c>BotWeaver.Choose</c>).
    /// </para>
    /// </summary>
    public static int LoomReach { get; set; } = 800;

    /// <summary>Seconds between the loom's steps, and between two spins.</summary>
    public static int StepMs { get; set; } = 1500;

    /// <summary>How much of the cloth is cut into bandages when the population wants them: a fifth.</summary>
    public static double BandageShare { get; set; } = 0.2;

    public static int CapMs { get; set; } = 900000;

    /// <summary>
    /// Wool a stint gathers at most when the flock has it: ten, which is thirty yarn, six bolts and three hundred cloth.
    ///
    /// <para>
    /// <b>Two wool was one sheep, and one sheep is not worth a walk.</b> <see cref="WoolWanted"/> was set for a sheep standing
    /// in the next field: two wool, one bolt, fifty cloth. Sent across the map to a flock of fifteen, a weaver that came back
    /// with the wool of one sheep would have walked half an hour for a hundred gold of cloth. The stint takes up to this much
    /// where the flock has it grown (<c>BotWeaver</c> sets the target from the count it chose the pasture by), and at least
    /// <see cref="WoolWanted"/>. Ten wool weigh forty stones; three hundred cloth weigh thirty.
    /// </para>
    /// </summary>
    public static int WoolAfield { get; set; } = 10;

    /// <summary>How long a stint to a pasture past <see cref="SheepReach"/> may run: the walk there is inside it.</summary>
    public static int AfieldCapMs { get; set; } = 1800000;

    /// <summary>Sheep a stint may fail to walk up to before it gives the pasture up. See <see cref="Bend"/>.</summary>
    public static int MostBends { get; set; } = 3;

    /// <summary>What cloth is worth when nothing within the weaver's reach sells it: the tailor's own two.</summary>
    public static int ClothGuess { get; set; } = 2;

    public static long Stints { get; private set; }

    public static long Sheared { get; private set; }

    public static long Bolts { get; private set; }

    public static long ClothMade { get; private set; }

    public static long BandagesMade { get; private set; }

    public static long NoSheep { get; private set; }

    public static long NoLoom { get; private set; }

    /// <summary>Stints ended that were sent a long way: to a flock, or from it to a loom, past <see cref="SheepReach"/>.</summary>
    public static long Afield { get; private set; }

    /// <summary>Spare cloth a stint kept to sell, over the <c>BotSew.Bolt</c> × 2 the needle keeps, and of it what a want waits for.</summary>
    public static long ClothListed { get; private set; }

    /// <summary>Of that, cloth that went straight into somebody's order.</summary>
    public static long ClothOrdered { get; private set; }


    /// <summary>Sheep a weaver could not walk up to, and pastures given up for it.</summary>
    public static long Unreached { get; private set; }

    private enum Leg
    {
        Shear,
        Spin,
        Weave,
        Cut,
        Done
    }

    private readonly Map _map;

    private readonly Point3D _where;

    private readonly bool _bandages;

    private readonly long _began;

    private Leg _leg = Leg.Shear;

    private long _stepTick;

    private int _sheared;

    private int _bolts;

    private int _cloth;

    private int _bandaged;

    private Sheep _sheep;

    /// <summary>The flock this stint was sent to, or null for a stint that looks round where it stands.</summary>
    private BotPastures.Pasture _pasture;

    private readonly int _woolWanted;

    private readonly int _cap;

    /// <summary>What one cloth and one bandage are worth, read once when the stint is offered. See <see cref="Made"/>.</summary>
    private readonly int _clothWorth;

    private readonly int _bandageWorth;

    /// <summary>Sheep this stint could not walk up to. See <see cref="Bend"/>.</summary>
    private HashSet<Serial> _shunned;

    private int _bends;

    public BotWeave(Map map, Point3D where, bool bandages) : this(map, where, bandages, null, 0, false)
    {
    }

    /// <summary>
    /// A stint sent to a flock: <paramref name="where"/> is the pasture, so the walk there is priced like any other work's walk,
    /// and <paramref name="wool"/> is how much wool the flock had grown when it was chosen.
    /// </summary>
    public BotWeave(Map map, Point3D where, bool bandages, BotPastures.Pasture pasture, int wool, bool far)
    {
        _map = map;
        _where = where;
        _bandages = bandages;
        _began = Core.TickCount;
        _pasture = pasture;
        _woolWanted = Math.Clamp(wool, WoolWanted, Math.Max(WoolWanted, WoolAfield));
        _far = far;
        _cap = far ? Math.Max(CapMs, AfieldCapMs) : CapMs;
        _clothWorth = Math.Max(1, BotMarket.Worth(typeof(Cloth), ClothGuess));
        _bandageWorth = Math.Max(1, BotMarket.Worth(typeof(Bandage), 5));
    }

    /// <summary>Whether the walk to the flock or on from it to the loom is a long one: the stint's clock is longer for it.</summary>
    private readonly bool _far;

    public override string Kind => Trade;

    public override Map Map => _map;

    public override Point3D Where => _where;

    public override double Expects => Prior;

    /// <summary>The needle's six minutes for a bolt, and that again for every further bolt the wool will make.</summary>
    public override double Minutes => WorkMinutes * Math.Max(1.0, _woolWanted / (double)Math.Max(1, WoolWanted));

    public override SkillName? Trains => SkillName.Tailoring;

    public override double Coin => 0.6;

    /// <summary>
    /// Gold-equivalent, as <see cref="BotDeed.Made"/> promises, and it was a count.
    ///
    /// <para>
    /// <b>Fifty cloth were fifty gold and a bandage one.</b> Every other maker multiplies its pieces by their worth — the forge by
    /// <c>BotMarket.Worth</c>, the needle by its own, the mortar and the fletcher by theirs — and this added the pieces up: a
    /// bolt the counter sells for a hundred gold was measured at fifty, and a bandage the healer sells for five at one. The ledger
    /// reckoned weaving at 23 to 63 a minute on build 336 from exactly that, where mining was taken at a median of 100.
    /// </para>
    /// </summary>
    /// <summary>And since 01.10.2026 at the island's price only for what somebody asks for; the rest at the surplus price. See <see cref="BotGlut.Yield"/>.</summary>
    public override int Made => BotGlut.Yield(typeof(Cloth), _cloth, _clothWorth) + BotGlut.Yield(typeof(Bandage), _bandaged, _bandageWorth);

    /// <summary>
    /// The walk up to a sheep turned out impossible: another sheep of the flock, a few times, and then the pasture is let be.
    ///
    /// <para>
    /// A flock wanders over fifty tiles of its spawner's ground, and a sheep across a river or behind a fence is not the flock.
    /// Anywhere but the shearing the stint gives up as it always has: a wheel or a loom it cannot reach is not bent round.
    /// </para>
    /// </summary>
    public override bool Bend(IBotWilful bot)
    {
        if (_leg != Leg.Shear)
        {
            return false;
        }

        if (_sheep != null)
        {
            (_shunned ??= []).Add(_sheep.Serial);
            _sheep = null;
        }

        Unreached++;

        if (++_bends <= MostBends)
        {
            return true;
        }

        if (_pasture != null)
        {
            bot?.Resolve?.Ledger?.Beware(Trade, _map, _pasture.Where);
        }

        return false;
    }

    public override bool Steadfast => true;

    /// <summary>
    /// Held for its own clock (<see cref="CapMs"/>, <see cref="AfieldCapMs"/>), not the will's eight-minute cap.
    ///
    /// <para>
    /// <b>Outbid at eight minutes, every time (01.10.2026, build 386).</b> A stint to a flock past the sheep reach is reckoned at
    /// ten to thirty-four minutes and the will's hold is capped at eight (<c>BotWill.CommitCapMs</c>), so twenty of the
    /// twenty-one outbid weaving stints of build 386 were dropped at 8.0–9.1 minutes — walking back from the flock with the
    /// wool, or at the wheel — 165 bot-minutes for nothing. The stint has a clock of its own that ends it, which is the case
    /// <see cref="BotDeed.HoldsFor"/> is written for; the trek and stall watchdogs still run under it.
    /// </para>
    /// </summary>
    public override double HoldsFor => _cap / 60000.0;

    public override string Stage => _leg switch
    {
        Leg.Shear => $"shearing sheep ({_sheared} wool)",
        Leg.Spin => "spinning wool at the wheel",
        Leg.Weave => $"weaving at the loom ({_bolts} bolts)",
        Leg.Cut => "cutting the bolt into cloth",
        _ => "putting the cloth away"
    };

    private static Scissors ScissorsOf(Mobile body) => BotOutfit.Oldest<Scissors>(body?.Backpack);

    private static int Wool(Mobile body) => body?.Backpack?.GetAmount(typeof(Wool)) ?? 0;

    private static int Yarn(Mobile body) => body?.Backpack?.GetAmount(typeof(BaseClothMaterial), true) ?? 0;

    public override BotDoing Advance(IBotWilful bot)
    {
        var body = bot?.Self;

        if (body == null || _map == null || _map == Map.Internal)
        {
            return BotDoing.Failed("no body");
        }

        if (Core.TickCount - _began >= _cap)
        {
            return Finish(bot, body, "the stint ran out of time");
        }

        var scissors = ScissorsOf(body);

        if (scissors == null)
        {
            return _cloth > 0 ? Finish(bot, body, "the scissors wore out") : BotDoing.Failed("no scissors");
        }

        for (var guard = 0; guard < 4; guard++)
        {
            var doing = _leg switch
            {
                Leg.Shear => Shearing(body, scissors),
                Leg.Spin => Spinning(body),
                Leg.Weave => Weaving(body),
                Leg.Cut => Cutting(body, scissors),
                _ => Finish(bot, body, null)
            };

            if (doing.Kind != BotDoingKind.Work || doing.Note != "next")
            {
                return doing;
            }
        }

        return BotDoing.Work(Stage);
    }

    private BotDoing Shearing(Mobile body, Scissors scissors)
    {
        if (Wool(body) >= _woolWanted || Yarn(body) >= 5 * Math.Max(1, _woolWanted / Math.Max(1, WoolWanted)))
        {
            _leg = Leg.Spin;

            return BotDoing.Work("next");
        }

        // Grown by the engine's clock (BotPastures.Grown), not by the body: the body is put back only by a running AI.
        if (_sheep is not { Deleted: false, Alive: true } || _sheep.Map != _map || !BotPastures.Grown(_sheep))
        {
            _sheep = _pasture != null ? Flock(body) : Nearest(body);

            if (_sheep == null)
            {
                NoSheep++;

                if (Wool(body) > 0 || Yarn(body) > 0)
                {
                    return Next(Leg.Spin);
                }

                return _pasture != null
                    ? BotDoing.Failed($"the flock by ({_pasture.Where.X}, {_pasture.Where.Y}) had no sheep with its wool grown")
                    : BotDoing.Failed($"no sheep with its wool grown within {SheepReach} tiles");
            }
        }

        if (!body.InRange(_sheep.Location, 1))
        {
            return BotDoing.Walk(_map, _sheep, BotArrival.Within(1), "up to a sheep");
        }

        var before = Wool(body);

        _sheep.Carve(body, scissors);

        var got = Wool(body) - before;

        if (got > 0)
        {
            _sheared += got;
            Sheared += got;
        }

        _sheep = null;

        return BotDoing.Work($"shearing sheep ({_sheared} wool)");
    }

    /// <summary>
    /// The next sheep of the flock this stint was sent to, and when it has none left, of the nearest other flock round where the
    /// weaver stands now — which is what the old look round the bot did, by the spawners' lists instead of a sweep. A flock
    /// found with nothing to shear is let be for everybody (<see cref="BotPastures.Rest"/>).
    /// </summary>
    private Sheep Flock(Mobile body)
    {
        for (var tries = 0; tries < 3 && _pasture != null; tries++)
        {
            var sheep = BotPastures.Nearest(_pasture, body.Location, _shunned);

            if (sheep != null)
            {
                return sheep;
            }

            BotPastures.Rest(_pasture);
            _pasture = BotPastures.Next(body.Location, SheepReach, _pasture);
        }

        return null;
    }

    /// <summary>The nearest sheep with its wool grown round the bot, for a map no spawner keeps sheep on.</summary>
    private Sheep Nearest(Mobile body)
    {
        Sheep best = null;
        var nearest = int.MaxValue;

        foreach (var sheep in _map.GetMobilesInRange<Sheep>(body.Location, SheepReach))
        {
            if (!BotPastures.Grown(sheep) || _shunned?.Contains(sheep.Serial) == true)
            {
                continue;
            }

            var away = Math.Max(Math.Abs(sheep.X - body.X), Math.Abs(sheep.Y - body.Y));

            if (away < nearest)
            {
                nearest = away;
                best = sheep;
            }
        }

        return best;
    }

    private BotDoing Next(Leg leg)
    {
        _leg = leg;

        return BotDoing.Work("next");
    }

    private BotDoing Spinning(Mobile body)
    {
        if (Wool(body) <= 0)
        {
            return Next(Yarn(body) > 0 ? Leg.Weave : Leg.Done);
        }

        var (where, wheel) = BotLooms.NearestWheel(body.Location, LoomReach);

        if (wheel == null)
        {
            NoLoom++;

            return BotDoing.Failed($"no spinning wheel within {LoomReach} tiles");
        }

        if (!body.InRange(where, 2))
        {
            return BotDoing.Walk(_map, where, BotArrival.Within(2), "to the spinning wheel");
        }

        if (wheel.Spinning || Core.TickCount - _stepTick < StepMs)
        {
            return BotDoing.Work("spinning wool at the wheel");
        }

        if (body.Backpack?.FindItemByType<Wool>() is not { Deleted: false } wool)
        {
            return Next(Leg.Weave);
        }

        _stepTick = Core.TickCount;

        // Exactly as the wool's own target does it: the wool consumed, the wheel's timer started with the wool's callback,
        // which puts three yarn in the pack when it ends.
        var hue = wool.Hue;

        wool.Consume();
        wheel.BeginSpin(wool.OnSpun, body, hue);

        return BotDoing.Work("spinning wool at the wheel");
    }

    private BotDoing Weaving(Mobile body)
    {
        if (Yarn(body) <= 0)
        {
            return Next(body.Backpack?.GetAmount(typeof(BoltOfCloth)) > 0 ? Leg.Cut : Wool(body) > 0 ? Leg.Spin : Leg.Done);
        }

        var (where, loom) = BotLooms.NearestLoom(body.Location, LoomReach);

        if (loom == null)
        {
            NoLoom++;

            return BotDoing.Failed($"no loom within {LoomReach} tiles");
        }

        if (!body.InRange(where, 2))
        {
            return BotDoing.Walk(_map, where, BotArrival.Within(2), "to the loom");
        }

        if (Core.TickCount - _stepTick < StepMs)
        {
            return BotDoing.Work($"weaving at the loom ({_bolts} bolts)");
        }

        if (body.Backpack?.FindItemByType<BaseClothMaterial>() is not { Deleted: false } yarn)
        {
            return Next(Leg.Cut);
        }

        _stepTick = Core.TickCount;

        // Exactly as the yarn's own target steps the loom: four yarn raise the phase, the fifth makes the bolt.
        if (loom.Phase < 4)
        {
            yarn.Consume();
            loom.Phase++;
        }
        else
        {
            var bolt = new BoltOfCloth { Hue = yarn.Hue };

            yarn.Consume();
            loom.Phase = 0;
            body.AddToBackpack(bolt);
            _bolts++;
            Bolts++;
        }

        return BotDoing.Work($"weaving at the loom ({_bolts} bolts)");
    }

    private BotDoing Cutting(Mobile body, Scissors scissors)
    {
        var pack = body.Backpack;
        var bolt = pack?.FindItemByType<BoltOfCloth>();

        if (bolt is { Deleted: false })
        {
            var before = pack.GetAmount(typeof(Cloth));

            if (!bolt.Scissor(body, scissors))
            {
                return BotDoing.Failed("the bolt would not cut");
            }

            var cut = pack.GetAmount(typeof(Cloth)) - before;

            _cloth += Math.Max(0, cut);
            ClothMade += Math.Max(0, cut);
            BotCraftwork.Produced(typeof(Cloth), Math.Max(0, cut));

            return BotDoing.Work("cutting the bolt into cloth");
        }

        if (_bandages && _cloth > 0)
        {
            _bandaged += Bandages(body, (int)Math.Ceiling(_cloth * BandageShare));
        }

        return Next(Leg.Done);
    }

    /// <summary>
    /// Cuts up to <paramref name="many"/> cloth in the pack into bandages, one for one, with the bot's own scissors. Returns
    /// how many were made. Used by the weaver at the end of a stint and by the provisioner instead of a purchase.
    /// </summary>
    public static int Bandages(Mobile body, int many)
    {
        var pack = body?.Backpack;
        var scissors = ScissorsOf(body);

        if (pack == null || scissors == null || many <= 0)
        {
            return 0;
        }

        var cloth = pack.FindItemByType<Cloth>();

        if (cloth is not { Deleted: false })
        {
            return 0;
        }

        var take = Math.Min(many, cloth.Amount);

        if (take <= 0)
        {
            return 0;
        }

        // A stack of exactly what is to be cut, so the cloth's own Scissor turns only that much into bandages.
        //
        // <b>Split with the engine's own divider, and never with a new stack dropped into the same pack (30.09.2026).</b> A new
        // stack of the same cloth dropped into the pack stacks straight back onto the one it was cut from
        // (Container.TryDropItem asks every item there to StackWith it first) and is deleted doing so, so the cloth's Scissor
        // was asked of a deleted item and cut nothing: every bot carrying a few cloth of its own before a purchase cut none of the
        // purchase. LiftItemDupe leaves the original holding what it is given and puts the rest beside it unstacked — the rule the
        // brewer, the cook and the woodcutter already keep.
        if (take < cloth.Amount && Mobile.LiftItemDupe(cloth, take) == null)
        {
            return 0;
        }

        var piece = cloth;
        var before = pack.GetAmount(typeof(Bandage));

        if (!piece.Scissor(body, scissors))
        {
            return 0;
        }

        var made = Math.Max(0, pack.GetAmount(typeof(Bandage)) - before);

        BandagesMade += made;
        BotCraftwork.Produced(typeof(Bandage), made);

        return made;
    }

    private BotDoing Finish(IBotWilful bot, Mobile body, string because)
    {
        Stints++;

        if (_far)
        {
            Afield++;
        }

        var listed = Spare(bot, body);

        logger.Information(
            "{Name} sheared {Wool} wool, wove {Bolts} bolts and cut {Cloth} cloth and {Bandages} bandages, {Listed} cloth spare to sell by hand at {Price}gp{Because}",
            body.Name,
            _sheared,
            _bolts,
            _cloth,
            _bandaged,
            listed,
            Ask(bot),
            because == null ? "" : $"; stopped because {because}"
        );

        return _cloth > 0 || _bandaged > 0
            ? BotDoing.Done($"{_cloth} cloth and {_bandaged} bandages from {_sheared} wool")
            : BotDoing.Failed($"nothing came of it{(because == null ? "" : $": {because}")}");
    }

    /// <summary>
    /// What cloth is worth: the market's own price once one is known, never above what the nearest tailor asks, never under the
    /// floor. An ask above the counter is cloth nobody on the island can rationally buy.
    /// </summary>
    public static int Ask(IBotWilful bot)
    {
        var worth = BotMarket.Worth(typeof(Cloth), ClothGuess);
        var shelf = BotShops.Shelf(bot, typeof(Cloth), worth);

        return Math.Max(BotMarket.Floor, Math.Min(worth, shelf));
    }

    /// <summary>
    /// Everything over what the needle keeps (<c>BotSew.Bolt</c> × 2), counted: what somebody's want waits for, carried to them by
    /// the walk that comes next (<see cref="BotCourier"/>), and the rest kept for the weaver's shift by the bank or the porter.
    /// Returns how many cloth are spare. Nothing is put out from the loom any more (01.10.2026, the auction removed); the stint of
    /// build 336 that split its spare off onto a stall and made ten cloth out of nothing on the refusal has no stall to meet.
    /// </summary>
    private static int Spare(IBotWilful bot, Mobile body)
    {
        if (body?.Backpack == null)
        {
            return 0;
        }

        var spare = Math.Max(0, BotThread.Amount(body, typeof(Cloth)) - BotSew.Bolt * 2);
        var want = BotMarket.Demand(bot, typeof(Cloth));
        var ordered = want == null ? 0 : Math.Min(spare, want.Payable);

        ClothOrdered += ordered;
        ClothListed += spare - ordered;

        return spare;
    }

    public static string Describe() =>
        Stints == 0
            ? $"no weaving has ended yet; {BotPastures.Describe()}"
            : $"{Stints} weaving stints ended ({Afield} of them sent past {SheepReach} tiles to the flock or on to the loom): {Sheared} wool sheared, {Bolts} bolts woven, {ClothMade} cloth and {BandagesMade} bandages cut, {ClothListed} cloth spare to sell by hand ({ClothOrdered} of it for somebody's want); {NoSheep} looks found no sheep, {Unreached} sheep could not be walked up to and {NoLoom} looks found no wheel or loom{(BotLooms.WorkshopsInBritain ? " in Britain" : "")}; {BotPastures.Describe()}";

    public static void Forget()
    {
        Stints = 0;
        Sheared = 0;
        Bolts = 0;
        ClothMade = 0;
        BandagesMade = 0;
        NoSheep = 0;
        NoLoom = 0;
        Afield = 0;
        ClothListed = 0;
        ClothOrdered = 0;
        Unreached = 0;
    }
}

/// <summary>
/// Offers a bot with scissors and the tailor's trade a weaving stint when its cloth is short: at the flock that gives the most wool
/// for the walk, as far off as the bot's leash allows.
///
/// <para>
/// <b>Asked of the flocks, not of the bot's surroundings (30.09.2026).</b> Until build 336 this asked whether a sheep with its wool
/// stood within <see cref="BotWeave.SheepReach"/> of the bot and refused the stint otherwise — 872 of 1331 asks — so a weaver in
/// Britain never wove at all and the island bought its cloth. Now every pasture the spawners keep (<see cref="BotPastures"/>) is
/// weighed as the woods and the seams are: within the bot's leash and <see cref="PastureReach"/>, on the bot's own land, not refused
/// to the population or lately to this bot, not on ground that asks more than the weaver brings, not sealed, and with a wheel and a
/// loom within <see cref="BotWeave.LoomReach"/> of it — in Britain since 01.10.2026 (<see cref="BotLooms.WorkshopsInBritain"/>). Of
/// those, the one with the least road per sheep ready to shear. The walk there is priced by the auction like every other walk: a
/// flock three hundred tiles off wins only when its wool is worth the road.
/// </para>
/// </summary>
public sealed class BotWeaver : IBotProposer
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotWeaver));

    /// <summary>Fewest cloth on show at the shops by the bank before weaving for the island is offered.</summary>
    public static int LeastOnMarket { get; set; } = 20;

    /// <summary>Farthest a flock is looked for, from where the weaver stands; the bot's own leash bounds it too.</summary>
    public static int PastureReach { get; set; } = 600;

    /// <summary>How near the flock's spawner a walk has to be able to come for the pasture to count as reachable.</summary>
    public static int ArriveWithin { get; set; } = 8;

    public static long Asked { get; private set; }

    public static long NoScissors { get; private set; }

    public static long Stocked { get; private set; }

    public static long NoSheep { get; private set; }

    public static long NoLoom { get; private set; }

    public static long Offered { get; private set; }

    /// <summary>Offers to a flock past <see cref="BotWeave.SheepReach"/>, and the tiles to them, for an average.</summary>
    public static long Afield { get; private set; }

    public static long AfieldTiles { get; private set; }

    // ---- The pastures within reach that an ask passed over, each counted apart. Their sum is Looked. ----

    public static long Looked { get; private set; }

    public static long Shorn { get; private set; }

    public static long Rested { get; private set; }

    public static long Leashed { get; private set; }

    public static long Unjoined { get; private set; }

    public static long Refused { get; private set; }

    public static long Perilous { get; private set; }

    public static long Sealed { get; private set; }

    public static long LoomFar { get; private set; }

    private static bool _said;

    public string Name => "Weaver";

    public BotStanding Rung => BotStanding.Free;

    public BotDeed Propose(IBotWilful bot)
    {
        var body = bot?.Self;
        var map = body?.Map;

        if (map == null || map == Map.Internal || !body.Alive || body is not BotMobile who)
        {
            return null;
        }

        if (who.Class?.Skills == null || !Wants(who.Class))
        {
            return null;
        }

        Asked++;

        if (BotOutfit.Oldest<Scissors>(body.Backpack) == null)
        {
            NoScissors++;

            return null;
        }

        BotLooms.Survey(map);

        if (BotLooms.Wheels == 0 || BotLooms.Looms == 0)
        {
            NoLoom++;

            return null;
        }

        var bandages = Demanded(typeof(Bandage));

        if (BotThread.Amount(body, typeof(Cloth)) >= BotSew.Bolt && OnMarket(typeof(Cloth)) >= LeastOnMarket && !bandages)
        {
            Stocked++;

            return null;
        }

        var wool = body.Backpack?.GetAmount(typeof(Wool)) ?? 0;

        // Wool in the pack already: spun and woven where the bot is, as it always was. The wheel is asked as well as the loom,
        // which it was not — "no spinning wheel within 400 tiles" twice for Morgrim at 00:28 on 30.09.2026, offered on the loom alone.
        if (wool >= BotWeave.WoolWanted)
        {
            if (BotLooms.NearestWheel(body.Location, BotWeave.LoomReach).Wheel == null
                || BotLooms.NearestLoom(body.Location, BotWeave.LoomReach).Loom == null)
            {
                NoLoom++;

                return null;
            }

            return Offer(body, who, new BotWeave(map, body.Location, bandages));
        }

        BotPastures.Survey(map);

        // A map no spawner keeps sheep on: the old look round the bot, the only thing there is to go on.
        if (BotPastures.Count == 0)
        {
            if (BotLooms.NearestLoom(body.Location, BotWeave.LoomReach).Loom == null)
            {
                NoLoom++;

                return null;
            }

            if (!SheepNear(map, body))
            {
                NoSheep++;

                return null;
            }

            return Offer(body, who, new BotWeave(map, body.Location, bandages));
        }

        var pasture = Choose(bot, body, map, out var ready, out var loomAway);

        if (pasture == null)
        {
            NoSheep++;

            return null;
        }

        var away = Math.Max(Math.Abs(pasture.Where.X - body.X), Math.Abs(pasture.Where.Y - body.Y));

        if (away > BotWeave.SheepReach)
        {
            Afield++;
            AfieldTiles += away;
        }

        var far = away > BotWeave.SheepReach || loomAway > BotWeave.SheepReach;

        return Offer(body, who, new BotWeave(map, pasture.Where, bandages, pasture, ready * WoolPerSheep(map), far));
    }

    private static BotWeave Offer(Mobile body, BotMobile who, BotWeave stint)
    {
        Offered++;

        if (!_said)
        {
            _said = true;

            logger.Information("{Name} the {Class} is the first to go weaving: {Wheels} wheels and {Looms} looms are known", body.Name, who.Class?.Name, BotLooms.Wheels, BotLooms.Looms);
        }

        return stint;
    }

    /// <summary>What one shearing gives: two wool on Felucca and one elsewhere, as <c>Sheep.Carve</c> says.</summary>
    private static int WoolPerSheep(Map map) => map == Map.Felucca ? 2 : 1;

    /// <summary>
    /// The flock this weaver should walk to, or null, and how many of its sheep have their wool grown (at most a full stint's).
    ///
    /// <para>
    /// Every gate is one the shard already asks of a place it sends a bot to, in the order of what each costs, and every pasture
    /// within reach that fails one is counted under it — the woods' own rule (<c>BotHerbalist</c>'s choice of a wood). The one
    /// question the woods do not have is the wheel and the loom: a flock with no wheel within reach of it is wool nobody can spin.
    /// </para>
    /// </summary>
    private static BotPastures.Pasture Choose(IBotWilful bot, Mobile body, Map map, out int ready, out int loomAway)
    {
        ready = 0;
        loomAway = 0;

        var ledger = bot?.Resolve?.Ledger;
        var power = BotThreat.Power(body);
        var full = Math.Max(1, BotWeave.WoolAfield / WoolPerSheep(map));
        var pastures = BotPastures.All;

        BotPastures.Pasture best = null;
        var bestCost = double.MaxValue;

        for (var i = 0; i < pastures.Count; i++)
        {
            var pasture = pastures[i];
            var where = pasture.Where;

            if (Math.Max(Math.Abs(where.X - body.X), Math.Abs(where.Y - body.Y)) > PastureReach)
            {
                continue;
            }

            Looked++;

            if (BotPastures.Resting(pasture))
            {
                Rested++;

                continue;
            }

            var grown = BotPastures.Ready(pasture);

            if (grown <= 0)
            {
                Shorn++;

                continue;
            }

            if (!BotPopulation.Within(map, where, body))
            {
                Leashed++;

                continue;
            }

            // See BotPopulation.Reachable.
            if (!BotPopulation.Reachable(map, body.Location, where))
            {
                Unjoined++;

                continue;
            }

            if (BotRefused.Refusing(map, where) || ledger?.Cautious(BotWeave.Trade, map, where) == true)
            {
                Refused++;

                continue;
            }

            // Nor a flock the weaver cannot live among: the herb woods' and the seams' own test (BotGround.MineOdds). A WildLife
            // spawner keeps its one sheep beside bears, panthers and wolves.
            if (BotGround.MineOdds > 0.0 && BotQuad.Muscle(map, where) > power * BotGround.MineOdds)
            {
                Perilous++;

                continue;
            }

            // Nor a flock inside a deadly zone (build 386): Thorgar died five times at y 3072–3156 for wool. The hunt's own read.
            if (BotZones.LevelAt(map, where.X, where.Y) == BotZoneLevel.Deadly)
            {
                Perilous++;

                continue;
            }

            if (BotReach.Ask(map, body.Location, where, BotArrival.Within(ArriveWithin)) == BotReachVerdict.Sealed)
            {
                Sealed++;

                continue;
            }

            var wheel = BotLooms.NearestWheel(where, BotWeave.LoomReach);
            var loom = BotLooms.NearestLoom(where, BotWeave.LoomReach);

            if (wheel.Wheel == null || loom.Loom == null)
            {
                LoomFar++;

                continue;
            }

            // The road per sheep, the walk on to the loom included: a flock of ten at four hundred tiles beats a lone sheep at
            // a hundred and fifty, and a flock by its loom beats one across the map from it.
            var toLoom = Math.Max(
                Math.Max(Math.Abs(wheel.Where.X - where.X), Math.Abs(wheel.Where.Y - where.Y)),
                Math.Max(Math.Abs(loom.Where.X - where.X), Math.Abs(loom.Where.Y - where.Y))
            );

            var sheep = Math.Min(grown, full);
            var road = body.GetDistanceToSqrt(where) + Math.Max(0, BotRoads.Behind(map, body.Location, where)) + toLoom;
            var cost = road / sheep;

            if (cost < bestCost)
            {
                best = pasture;
                bestCost = cost;
                ready = sheep;
                loomAway = toLoom;
            }
        }

        return best;
    }

    private static bool SheepNear(Map map, Mobile body)
    {
        foreach (var sheep in map.GetMobilesInRange<Sheep>(body.Location, BotWeave.SheepReach))
        {
            if (BotPastures.Grown(sheep))
            {
                return true;
            }
        }

        return false;
    }

    private static bool Demanded(Type kind)
    {
        var wants = BotMarket.Wants;

        for (var i = 0; i < wants.Count; i++)
        {
            if (wants[i].IsOpen && wants[i].Kind == kind)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>What the shops by the bank show of a kind: the island's stock on sale since the stalls went (01.10.2026).</summary>
    private static int OnMarket(Type kind) => BotShopkeep.Showing(kind);

    private static bool Wants(BotClass klass)
    {
        var skills = klass.Skills;

        for (var i = 0; i < skills.Count; i++)
        {
            if (skills[i].Skill == SkillName.Tailoring)
            {
                return true;
            }
        }

        return false;
    }

    public static string Describe() =>
        Asked == 0
            ? "nobody with the needle has been asked to weave"
            : $"{Asked} asked to weave: {Offered} offered a stint ({Afield} to a flock past {BotWeave.SheepReach} tiles, {(Afield == 0 ? 0 : AfieldTiles / Afield)} tiles off on average), {Stocked} with cloth enough and the shops stocked, {NoSheep} with no flock they could get to with wool grown, {NoLoom} with no wheel or loom within reach, {NoScissors} without scissors; "
              + $"{Looked} pastures within {PastureReach} tiles looked at: {Shorn} shorn, {Rested} let be after a weaver found them shorn, {Leashed} past the weaver's leash, {Unjoined} on another land, {Refused} refused, {Perilous} on ground asking more than the weaver brings, {Sealed} with no way there and {LoomFar} with no wheel and loom within {BotWeave.LoomReach}; {BotWeave.Describe()}";

    public static void Forget()
    {
        Asked = 0;
        NoScissors = 0;
        Stocked = 0;
        NoSheep = 0;
        NoLoom = 0;
        Offered = 0;
        Afield = 0;
        AfieldTiles = 0;
        Looked = 0;
        Shorn = 0;
        Rested = 0;
        Leashed = 0;
        Unjoined = 0;
        Refused = 0;
        Perilous = 0;
        Sealed = 0;
        LoomFar = 0;
        _said = false;
        BotWeave.Forget();
    }
}
