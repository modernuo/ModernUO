using System;
using System.Collections.Generic;
using Server.Engines.Spawners;
using Server.Logging;
using Server.Mobiles;

namespace Server.BotAI.V2;

/// <summary>
/// The dungeons of this era: where each one is, what rooms it has, and how hard it is.
///
/// <para>
/// <b>Patrick's order of 11.09.2026: the places at the bottom of the map are meant to be dungeons.</b>
/// They have been standing in the world since it was generated — twelve of them, six hundred-odd spawners
/// between them — and nothing on this shard had ever been inside one. They are cut into their own block of
/// the map, from x 5120 eastward, with no walkable road from the island at all: the engine's own way in is
/// a teleporter at an entrance, and a bot that plans a walk to one gets "no way through" as many times as
/// it is asked.
/// </para>
///
/// <para>
/// <b>The rooms are read out of the world rather than written down here, and that is the difference
/// between a table and a survey.</b> Every dungeon in the save carries spawners, and a spawner's own
/// location is a tile the engine itself has decided a creature can stand on. So the rooms of a dungeon are
/// its spawners — guaranteed to be real floor, guaranteed to be where the fighting is, and guaranteed to
/// still be right if somebody re-imports the spawn files tomorrow. A hand-typed list of coordinates would
/// be a promise about a map, and this project has paid for that kind of promise before.
/// </para>
///
/// <para>
/// <b>And the difficulty is measured, not asserted.</b> Patrick's order names the reason — a demon is
/// plainly not a harpy — and the shard already has the arithmetic for it: <see cref="BotThreat.Power"/>,
/// health times what a thing hits for, which is the same number the hunt and the muster weigh a fight by.
/// One of each creature a dungeon's spawners name is built, measured and destroyed at once, exactly as
/// <c>BotHarness</c> measures armour. Nothing here decides what those numbers mean; that is
/// <see cref="BotDelver"/>'s business.
/// </para>
///
/// <para>
/// <b>The boxes are the one hand-written thing, and they are drawn tight on purpose.</b> Each is the
/// cluster its own spawn file actually occupies, with the neighbours checked — Shame ends before Ice
/// begins, the Orc Caves before Terathan Keep — because two boxes that overlap would hand one dungeon the
/// other's rooms and quietly report a lizard den as a drake pit.
/// </para>
/// </summary>
public static class BotDungeon
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotDungeon));

    /// <summary>One dungeon: a name, a box, the rooms found in it and what lives in them.</summary>
    /// <summary>
    /// How far past its box a body may be and still count as being down the dungeon. See <c>Deep.Holds</c>.
    /// </summary>
    public static int Margin { get; set; } = 32;

    public sealed class Deep
    {
        public Deep(string name, int x1, int y1, int x2, int y2)
        {
            Name = name;
            Bounds = new Rectangle2D(x1, y1, x2 - x1 + 1, y2 - y1 + 1);
        }

        public string Name { get; }

        public Rectangle2D Bounds { get; }

        /// <summary>Where a party can stand, taken from the spawners inside the box.</summary>
        public List<Point3D> Rooms { get; } = [];

        /// <summary>What the spawners inside it name, once each.</summary>
        public List<string> Kinds { get; } = [];

        /// <summary>The average of what one of its creatures is worth in <see cref="BotThreat.Power"/>.</summary>
        public double Power { get; set; }

        /// <summary>And the worst of them, which is what actually kills a party.</summary>
        public double Worst { get; set; }

        /// <summary>How many creatures its spawners are set to keep alive.</summary>
        public int Heads { get; set; }

        /// <summary>Delves that ended in this one, for the summary.</summary>
        public long Runs;

        /// <summary>The middle of the box, for a line of log.</summary>
        public Point2D Middle => new(Bounds.X + Bounds.Width / 2, Bounds.Y + Bounds.Height / 2);

        public bool Ready => Rooms.Count > 0 && Power > 0.0;

        /// <summary>
        /// Whether a body is still down this dungeon, which is a wider question than whether it is inside
        /// the box.
        ///
        /// <para>
        /// <b>The box is drawn round the spawners, and a dungeon is bigger than its spawners.</b> Its job
        /// is to say which spawners belong to which name, and for that it wants to be tight. The question
        /// here is a different one — "is this bot with its party, or is it back on the island" — and for
        /// that the box is the wrong shape by a corridor's width.
        /// </para>
        ///
        /// <para>
        /// It cost five delves to find out. On the evening of 11.09.2026 every party's leader was ruled
        /// out of the Orc Caves at least once, and every single one of them was standing at y 1294 — one
        /// tile north of a box whose top edge is 1295. <c>Rectangle2D.Contains</c> is start-inclusive, so
        /// the tile the orcs were on was outside the dungeon by arithmetic. Four delves the evening before
        /// ended on it, and the reading it invited was that something on the island was pulling bodies out
        /// of the ground, which nothing was.
        /// </para>
        ///
        /// <para>
        /// <see cref="Margin"/> is generous on purpose. There is no wrong answer between "a few tiles past
        /// the spawners" and "the next dungeon along", because what this is really distinguishing is a bot
        /// underground from a bot a few thousand tiles away in Britain, and nothing at all lives in
        /// between.
        /// </para>
        /// </summary>
        public bool Holds(Point3D where) =>
            where.X >= Bounds.Start.X - Margin
            && where.X < Bounds.End.X + Margin
            && where.Y >= Bounds.Start.Y - Margin
            && where.Y < Bounds.End.Y + Margin;

        public override string ToString() => $"{Name} ({Rooms.Count} rooms, {Power:F0} each, worst {Worst:F0})";
    }

    /// <summary>
    /// The twelve, with the boxes each one's spawners actually occupy.
    ///
    /// Felucca only, by the same rule the rest of the shard follows: the population lives there, and a
    /// dungeon on a map nobody is on is a place nobody can be brought back from.
    /// </summary>
    private static readonly Deep[] _deeps =
    [
        new("the Orc Caves", 5280, 1295, 5365, 1385),
        new("Shame", 5380, 5, 5630, 240),
        new("Deceit", 5130, 520, 5350, 760),
        new("Despise", 5380, 515, 5615, 1005),
        new("Wrong", 5650, 520, 5865, 600),
        new("Covetous", 5380, 1790, 5625, 2045),
        new("Ice", 5660, 130, 5875, 370),
        new("Khaldun", 5385, 1285, 5615, 1495),
        new("Terathan Keep", 5125, 1545, 5370, 1765),
        new("Fire", 5630, 1285, 5875, 1480),
        new("Destard", 5130, 770, 5360, 1015),
        new("Hythloth", 5900, 15, 6135, 245)
    ];

    public static IReadOnlyList<Deep> All => _deeps;

    /// <summary>The dungeon whose box, margin and all, holds this place; null on the island.</summary>
    public static Deep Holding(Point3D where)
    {
        for (var i = 0; i < _deeps.Length; i++)
        {
            if (_deeps[i].Holds(where))
            {
                return _deeps[i];
            }
        }

        return null;
    }

    /// <summary>Whether this place lies inside any dungeon's box, margin and all. Twelve comparisons; asked per appraisal.</summary>
    public static bool Under(Point3D where)
    {
        for (var i = 0; i < _deeps.Length; i++)
        {
            if (_deeps[i].Holds(where))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Whether the survey has been done since the world was loaded.</summary>
    public static bool Surveyed { get; private set; }

    /// <summary>Rooms found, across every dungeon.</summary>
    public static int Rooms { get; private set; }

    /// <summary>Creature kinds measured. See <see cref="Might"/>.</summary>
    public static int Measured { get; private set; }

    /// <summary>Kinds named by a spawner that the engine could not build. Each is a room nobody can price.</summary>
    public static int Unknown { get; private set; }

    /// <summary>What one creature of each kind is worth, measured once.</summary>
    private static readonly Dictionary<string, double> _might = [];

    /// <summary>
    /// What a kind is besides its strength, read off the same specimen <see cref="Might"/> builds: whether it attacks on sight,
    /// what mind it has, how far it sees, whether it can leave the water, whether it is the towns' rather than the wild's. For
    /// the danger zones (<see cref="BotHaunts"/>), which must classify a spawner whose creatures are all dead; one specimen a
    /// kind, never two.
    /// </summary>
    public readonly record struct KindTraits(FightMode Mode, AIType AI, int Perception, bool CantWalk, bool CanSwim, bool Townsfolk);

    private static readonly Dictionary<string, KindTraits> _traits = [];

    /// <summary>The traits of a kind by the name a spawner uses; false when it could not be built. Builds it on first ask.</summary>
    public static bool Traits(string named, out KindTraits traits)
    {
        traits = default;

        if (string.IsNullOrWhiteSpace(named))
        {
            return false;
        }

        Might(named);

        return _traits.TryGetValue(named, out traits);
    }

    /// <summary>
    /// Walks every box and reads what is in it. Costs one pass over the spawners of twelve rectangles and
    /// one construction per distinct creature kind, once per world load.
    /// </summary>
    public static void Survey(Map map)
    {
        if (Surveyed || map == null || map == Map.Internal)
        {
            return;
        }

        Surveyed = true;

        for (var i = 0; i < _deeps.Length; i++)
        {
            Read(map, _deeps[i]);
        }

        for (var i = 0; i < _deeps.Length; i++)
        {
            var deep = _deeps[i];

            logger.Information(
                "{Deep} surveyed: {Rooms} rooms around ({X}, {Y}), {Heads} creatures of {Kinds} kinds, {Power} of strength each and {Worst} at worst",
                deep.Name,
                deep.Rooms.Count,
                deep.Middle.X,
                deep.Middle.Y,
                deep.Heads,
                deep.Kinds.Count,
                deep.Power.ToString("F0"),
                deep.Worst.ToString("F0")
            );
        }
    }

    private static void Read(Map map, Deep deep)
    {
        var total = 0.0;
        var counted = 0;

        foreach (var spawner in map.GetItemsInBounds<BaseSpawner>(deep.Bounds))
        {
            if (spawner is not { Deleted: false } || spawner.Entries == null)
            {
                continue;
            }

            _spawners.Add(spawner);
            deep.Rooms.Add(spawner.Location);
            Rooms++;

            for (var i = 0; i < spawner.Entries.Count; i++)
            {
                var named = spawner.Entries[i]?.SpawnedName;

                if (string.IsNullOrWhiteSpace(named))
                {
                    continue;
                }

                var might = Might(named);

                if (might <= 0.0)
                {
                    continue;
                }

                if (!deep.Kinds.Contains(named))
                {
                    deep.Kinds.Add(named);
                }

                deep.Heads += Math.Max(1, spawner.Entries[i].SpawnedMaxCount);
                total += might;
                counted++;

                if (might > deep.Worst)
                {
                    deep.Worst = might;
                }
            }
        }

        deep.Power = counted == 0 ? 0.0 : total / counted;
    }

    /// <summary>
    /// What one of these is worth in a fight, built once to be asked and destroyed in the same breath.
    ///
    /// <para>
    /// <b>It must not survive the question.</b> Constructing a creature puts a real mobile into the world;
    /// leaving it there would be one monster in limbo per kind, for ever, and a world save full of them.
    /// The same rule and the same shape as <c>BotHarness.Sample</c>, which measures armour.
    /// </para>
    ///
    /// <para>
    /// Anything that is not a creature — a treasure chest, a piece of scenery a spawner happens to place —
    /// is worth nothing and is not counted, which is right: a party is not made safer by the barrels.
    /// </para>
    /// </summary>
    public static double Might(string named)
    {
        if (string.IsNullOrWhiteSpace(named))
        {
            return 0.0;
        }

        if (_might.TryGetValue(named, out var known))
        {
            return known;
        }

        var might = 0.0;
        var kind = AssemblyHandler.FindTypeByName(named);

        if (kind == null || !typeof(BaseCreature).IsAssignableFrom(kind))
        {
            Unknown++;
            _might[named] = 0.0;

            return 0.0;
        }

        BaseCreature made = null;

        try
        {
            made = Activator.CreateInstance(kind) as BaseCreature;

            if (made != null)
            {
                might = BotThreat.Power(made);
                Measured++;

                _traits[named] = new KindTraits(
                    made.FightMode,
                    made.AI,
                    made.RangePerception,
                    made.CantWalk,
                    made.CanSwim,
                    BotHaunts.IsTownsfolk(made)
                );
            }
        }
        catch (Exception)
        {
            // A creature whose constructor wants arguments is a creature this cannot price, and that is a
            // fact worth counting rather than an error worth throwing: the spawner will still make it and
            // the party will still meet it, so the dungeon is simply reckoned off the kinds that did build.
            Unknown++;
        }
        finally
        {
            made?.Delete();
        }

        _might[named] = might;

        return might;
    }

    /// <summary>One dungeon by name, or nothing.</summary>
    public static Deep Find(string name)
    {
        for (var i = 0; i < _deeps.Length; i++)
        {
            if (string.Equals(_deeps[i].Name, name, StringComparison.OrdinalIgnoreCase))
            {
                return _deeps[i];
            }
        }

        return null;
    }

    /// <summary>
    /// Somewhere inside it to be put down, and somewhere to walk to next.
    ///
    /// <para>
    /// Settled against the map rather than taken as the spawner wrote it. A spawner's own Z is where the
    /// spawner is, which in a dungeon is usually the floor and occasionally a shelf on the wall; a party
    /// put down at a height nothing can stand at is a party that cannot take a step. See the rule this
    /// project keeps relearning: a point whose Z came out of anything but the map is a place nothing can
    /// stand on.
    /// </para>
    /// </summary>
    public static Point3D Room(Map map, Deep deep, int which)
    {
        if (map == null || deep == null || deep.Rooms.Count == 0)
        {
            return Point3D.Zero;
        }

        var room = deep.Rooms[Math.Abs(which) % deep.Rooms.Count];

        return BotStep.Settle(map, room.X, room.Y, out var z)
            ? new Point3D(room.X, room.Y, z)
            : room;
    }

    /// <summary>
    /// How often every dungeon spawner short of its count is made to put one more creature down. Nought turns it off.
    ///
    /// <para>
    /// <b>Patrick's order of 25.09.2026: delves for everybody and more often — "and above all the monsters there must
    /// come back, or there will be no fight".</b> A spawner puts one creature down per tick of its own clock, five to ten
    /// minutes on every one of the Orc Caves' nineteen (Data/Spawns/shared/felucca/OrcCaves.json), so a room a party
    /// emptied took twenty to forty minutes to fill again — about the time a band waited between delves, and a party
    /// that now comes every ten minutes would find the cave empty. Once a minute each of them adds one more of what it
    /// keeps, up to its own count and never past it; nothing about the spawners themselves is changed or saved.
    /// </para>
    /// </summary>
    public static int RestockMs { get; set; } = 60000;

    /// <summary>Creatures put back by the restock. See <see cref="RestockMs"/>.</summary>
    public static long Restocked { get; private set; }

    /// <summary>Every spawner inside a dungeon's box, from the survey.</summary>
    private static readonly List<BaseSpawner> _spawners = [];

    private static long _restockedTick;

    /// <summary>One creature more from every dungeon spawner that is running and short, at most once a <see cref="RestockMs"/>.</summary>
    public static void Restock()
    {
        if (RestockMs <= 0 || !Surveyed || Core.TickCount - _restockedTick < RestockMs)
        {
            return;
        }

        _restockedTick = Core.TickCount;

        for (var i = _spawners.Count - 1; i >= 0; i--)
        {
            var spawner = _spawners[i];

            if (spawner is not { Deleted: false })
            {
                _spawners.RemoveAt(i);

                continue;
            }

            // A group spawner fills all at once when its last is gone; its own clock does that, and it is left alone.
            if (!spawner.Running || spawner.Group || spawner.IsFull)
            {
                continue;
            }

            var before = spawner.Spawned?.Count ?? 0;

            spawner.Spawn();

            if ((spawner.Spawned?.Count ?? 0) > before)
            {
                Restocked++;
            }
        }
    }

    public static string Describe()
    {
        if (!Surveyed)
        {
            return "the dungeons have not been surveyed";
        }

        var ready = 0;

        for (var i = 0; i < _deeps.Length; i++)
        {
            if (_deeps[i].Ready)
            {
                ready++;
            }
        }

        return $"{ready} of {_deeps.Length} dungeons surveyed, {Rooms} rooms between them, "
            + $"{Measured} kinds of creature measured and {Unknown} the engine would not build; "
            + $"{Restocked} creatures put back by the restock, one a spawner every {RestockMs / 1000}s";
    }

    /// <summary>A world reload is a different world, and the rooms are items in it.</summary>
    public static void Forget()
    {
        for (var i = 0; i < _deeps.Length; i++)
        {
            _deeps[i].Rooms.Clear();
            _deeps[i].Kinds.Clear();
            _deeps[i].Power = 0.0;
            _deeps[i].Worst = 0.0;
            _deeps[i].Heads = 0;
            _deeps[i].Runs = 0;
        }

        // The measurements are about types rather than about the world, but a reload is the one moment this
        // shard has for putting everything back, and re-measuring twelve dozen creatures costs milliseconds.
        _might.Clear();
        _traits.Clear();
        _spawners.Clear();
        Surveyed = false;
        Rooms = 0;
        Measured = 0;
        Unknown = 0;
    }
}
