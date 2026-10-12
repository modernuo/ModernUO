using System;
using System.Collections.Generic;
using Server.Guilds;
using Server.Items;
using Server.Logging;
using Server.Spells;
using Server.Spells.Sixth;

namespace Server.BotAI.V2;

/// <summary>
/// A rune-mage marking a rune for a place its guild holds none for: walk to a tile the engine will let a rune be marked
/// on, cast Mark, aim it at a blank, and keep the rune.
///
/// <para>
/// <b>The pathfinder is simply the first mage of its guild to stand there.</b> Nothing sends a mage to mark anything: a
/// mage that arrives in a town (a journey, a boat, a gate), comes home to its seat, or walks out of a cave mouth with its
/// party is offered the mark by <see cref="BotMarker"/> when its guild holds no rune there and it carries a blank. The
/// rune goes into the mage's own book (<see cref="BotRunes.Satchel"/>) and the place into the guild's knowledge
/// (<see cref="BotRuneShelf"/>), from which the library is written and the other mages are lent copies.
/// </para>
///
/// <para>
/// <b>A fizzle is the engine's, and so is the cost of one.</b> <c>Spell.CheckSequence</c> takes the reagents before the
/// skill check, so every failed try spends a set of herbs; <see cref="BotRunes.Tries"/> bounds how many, and a mark given
/// up rests its place for this mage for <see cref="BotMarker.RestMs"/>.
/// </para>
/// </summary>
public sealed class BotMark : BotDeed
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotMark));

    public const string Trade = "mark";

    /// <summary>What a mark is reckoned at a minute. Thirty: above a walk home and a journey, below any real work nearby.</summary>
    public static double Prior { get; set; } = 30.0;

    /// <summary>How long a mage may take to walk to the spot and mark it. Three minutes: the place is within a town's box or forty tiles.</summary>
    public static int CapMs { get; set; } = 180000;

    public static long Undertaken { get; private set; }

    public static long Succeeded { get; private set; }

    public static long Fizzles { get; private set; }

    public static long Beaten { get; private set; }

    public static long GaveUp { get; private set; }

    private readonly Map _map;

    private readonly string _guild;

    private readonly string _name;

    private readonly string _kind;

    private readonly Point3D _anchor;

    private readonly Point3D _spot;

    private RecallRune _blank;

    private MarkSpell _spell;

    private int _tries;

    private long _began;

    private long _nextTick;

    public BotMark(Map map, string guild, string name, string kind, Point3D anchor, Point3D spot)
    {
        _map = map;
        _guild = guild;
        _name = name;
        _kind = kind;
        _anchor = anchor;
        _spot = spot;
        _began = Core.TickCount;
        _nextTick = _began;
    }

    public override string Kind => Trade;

    public override Map Map => _map;

    public override Point3D Where => _spot;

    public override double Expects => Prior;

    public override double Minutes => 1.0;

    /// <summary>It pays nothing and is not for money: the guild's knowledge of a place. See <see cref="BotDeed.Unpaid"/>.</summary>
    public override bool Unpaid => true;

    public override double Coin => 0.0;

    /// <summary>A minute's work with reagents spent in the middle of it: not bid away by the first better offer.</summary>
    public override bool Steadfast => true;

    public override string Stage => _spell != null ? $"casting Mark for {_name}" : $"to mark a rune for {_name}";

    public override void Taken(IBotWilful bot)
    {
        _began = Core.TickCount;
        _nextTick = _began;
        Undertaken++;
    }

    public override void Resumed(IBotWilful bot)
    {
        _began = Core.TickCount;
        _nextTick = _began;
    }

    public override BotDoing Advance(IBotWilful bot)
    {
        var body = bot?.Self as BotMobile;

        if (body is not { Deleted: false, Alive: true } || _map == null || body.Map != _map)
        {
            return BotDoing.Failed("no body, or not on the map of the place");
        }

        var now = Core.TickCount;

        if (_spell != null)
        {
            return Casting(body, now);
        }

        if (now - _began >= CapMs)
        {
            return GiveUp(body, $"could not mark a rune for {_name} in {CapMs / 60000} minutes");
        }

        // Beaten to it: another of the guild's mages marked this place while this one walked.
        if (BotRuneShelf.Held(_guild, _map, _anchor, BotRunes.Near * 2) != null)
        {
            Beaten++;

            return BotDoing.Done($"{_guild} already holds a rune for {_name}");
        }

        // On the spot, or on any tile beside it the engine would let a rune be marked on.
        if (!body.InRange(_spot, 1) || !BotRunes.Landable(_map, body.Location) && body.Location != _spot)
        {
            return BotDoing.Walk(_map, _spot, body.InRange(_spot, 1) ? BotArrival.Exactly : BotArrival.Within(1), $"to mark a rune for {_name}");
        }

        if (now - _nextTick < 0)
        {
            return BotDoing.Work($"gathering itself to try Mark again at {_name}");
        }

        if (!BotRunes.Ready(body, BotRunes.Mark, out var scroll, out _, out var why))
        {
            // Mana and the engine's recovery come back by themselves; anything else will not while the mage stands here.
            if (why is "too little mana" or "not recovered" or "already casting")
            {
                _nextTick = now + BotRunes.RetryMs;

                return BotDoing.Work($"waiting for its mana to mark {_name}");
            }

            return GiveUp(body, $"cannot cast Mark: {why}");
        }

        _blank = BotRunes.Blank(body);

        if (_blank == null)
        {
            return BotDoing.Failed("it has no blank rune left");
        }

        body.Journey?.Finish();
        _spell = new MarkSpell(body, scroll);

        if (!_spell.Cast())
        {
            _spell = null;

            return Fizzled(body, now, "the engine would not begin the cast");
        }

        return BotDoing.Work($"casting Mark for {_name}");
    }

    /// <summary>The cast in the air, its target, and what came of it.</summary>
    private BotDoing Casting(BotMobile body, long now)
    {
        if (_spell.IsCasting)
        {
            return BotDoing.Work($"casting Mark for {_name}");
        }

        // The engine has put a target on the mage: the click a bot has no client for. See BotMend.Aim.
        if (body.Target is SpellTarget<Item> aim && ReferenceEquals(aim.Spell, _spell))
        {
            if (_blank is not { Deleted: false } || !_blank.IsChildOf(body.Backpack) || _blank.Marked)
            {
                _blank = BotRunes.Blank(body);
            }

            if (_blank == null)
            {
                body.Target.Cancel(body, Server.Targeting.TargetCancelType.Canceled);
                _spell = null;

                return BotDoing.Failed("its blank rune went while it cast");
            }

            body.Target.Invoke(body, _blank);
        }

        if (_blank is { Deleted: false, Marked: true })
        {
            _spell = null;

            return Filed(body);
        }

        if (ReferenceEquals(body.Spell, _spell) && _spell.State != SpellState.None)
        {
            return BotDoing.Work($"casting Mark for {_name}");
        }

        _spell = null;

        return Fizzled(body, now, "the spell fizzled");
    }

    private BotDoing Fizzled(BotMobile body, long now, string how)
    {
        _tries++;
        Fizzles++;

        if (_tries >= BotRunes.Tries)
        {
            return GiveUp(body, $"Mark failed {_tries} times at {_name}: {how}");
        }

        _nextTick = now + BotRunes.RetryMs;

        return BotDoing.Work($"{how}; trying Mark again at {_name}");
    }

    private BotDoing GiveUp(BotMobile body, string why)
    {
        GaveUp++;
        BotMarker.Rest(body, _name);

        return BotDoing.Failed(why);
    }

    /// <summary>Marked: the place into the guild's knowledge, the rune into the mage's own book.</summary>
    private BotDoing Filed(BotMobile body)
    {
        var rune = _blank;
        var at = rune.Target;

        rune.Description = _name;

        BotRuneShelf.File(_guild, _name, _kind, _map, at, body.Name);
        BotRunes.Counted(_name);
        Succeeded++;

        var own = BotRunes.Satchel(body, true);

        // The engine's own way into a book: an entry is written and the rune is gone.
        if (own == null || own.Entries.Count >= 16 || !own.OnDragDrop(body, rune))
        {
            BotRunes.Bind(body, rune);
        }

        logger.Information(
            "{Name} the {Class} marked a rune for {Place} at ({X}, {Y}) with {Magery:F1} Magery; {Guild} holds runes for {Count} places",
            body.Name,
            body.Class?.Name,
            _name,
            at.X,
            at.Y,
            body.Skills[SkillName.Magery].Value,
            _guild,
            BotRuneShelf.Of(_guild).Count
        );

        return BotDoing.Done($"marked a rune for {_name}");
    }

    public override void Drop(IBotWilful bot)
    {
        // A cast still in the air when the work is taken off the mage: its target is let go, or it hangs on the body.
        if (_spell != null && bot?.Self is { Target: SpellTarget<Item> aim } body && ReferenceEquals(aim.Spell, _spell))
        {
            body.Target.Cancel(body, Server.Targeting.TargetCancelType.Canceled);
        }

        _spell = null;
    }

    public static string Describe() =>
        $"{Undertaken} marks taken on, {Succeeded} marked, {Fizzles} casts fizzled, {Beaten} found another mage of the guild had been first, {GaveUp} given up";

    public static void Forget()
    {
        Undertaken = 0;
        Succeeded = 0;
        Fizzles = 0;
        Beaten = 0;
        GaveUp = 0;
    }
}

/// <summary>
/// Offers a rune-mage with a blank the mark of a place its guild holds no rune for, where it stands: its seat, a cave
/// mouth, or the town it is in. See <see cref="BotMark"/>.
/// </summary>
public sealed class BotMarker : IBotProposer
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotMarker));

    /// <summary>How near its seat a mage stands to be offered the seat's rune. Forty.</summary>
    public static int SeatReach { get; set; } = 40;

    /// <summary>How near a cave mouth a mage stands to be offered the mouth's rune. Forty: a party walking out stands within it.</summary>
    public static int MouthReach { get; set; } = 40;

    /// <summary>How long a mark given up rests its place for that mage. Twenty minutes.</summary>
    public static int RestMs { get; set; } = 1200000;

    public static long Asked { get; private set; }

    public static long Busy { get; private set; }

    public static long NoSpell { get; private set; }

    public static long NoBlank { get; private set; }

    public static long Nothing { get; private set; }

    public static long Rested { get; private set; }

    public static long Forbidden { get; private set; }

    /// <summary>Mages at an unmarked place with a blank that the engine would not let cast Mark; the reasons are in the recall's list, prefixed "mark:".</summary>
    public static long Unable { get; private set; }

    public static long Offered { get; private set; }

    private static readonly Dictionary<(Serial, string), long> _rest = [];

    private static readonly Dictionary<string, (Point3D Anchor, Point3D Spot)> _spots = new(StringComparer.Ordinal);

    private static bool _said;

    public string Name => "Marker";

    public BotStanding Rung => BotStanding.Free;

    public static void Rest(Mobile body, string place)
    {
        if (body != null && place != null)
        {
            _rest[(body.Serial, place)] = Core.TickCount;
        }
    }

    private static bool Resting(Mobile body, string place) =>
        _rest.TryGetValue((body.Serial, place), out var when) && Core.TickCount - (when + RestMs) < 0;

    public BotDeed Propose(IBotWilful bot)
    {
        var body = bot?.Self;

        if (!BotRunes.Mage(body) || body is not BotMobile mage || mage.Guild is not Guild guild)
        {
            return null;
        }

        var map = body.Map;

        if (map == null || map == Map.Internal)
        {
            return null;
        }

        Asked++;

        if (bot is IBotSquadMember { Squad: not null } || BotDungeon.Under(body.Location))
        {
            Busy++;

            return null;
        }

        if (!BotRunes.Knows(body, BotRunes.Mark))
        {
            NoSpell++;

            return null;
        }

        if (BotRunes.Blank(body) == null)
        {
            NoBlank++;

            return null;
        }

        if (!Candidate(mage, guild, map, out var name, out var kind, out var anchor))
        {
            Nothing++;

            return null;
        }

        if (Resting(mage, name))
        {
            Rested++;

            return null;
        }

        if (!SpotFor(map, name, anchor, out var spot))
        {
            Forbidden++;
            Rest(mage, name);

            return null;
        }

        // Asked of the engine before the offer, not after the walk: a book Mark below 50 Magery never lands in this era,
        // and a mark offered to fail would teach the ledger that marking fails. Mana comes back and is not a refusal.
        if (!BotRunes.Ready(body, BotRunes.Mark, out _, out _, out var why) && why is not ("too little mana" or "not recovered" or "already casting"))
        {
            // Rested like a failed mark: Magery grows by tenths, and the engine's answer will not change by the next beat.
            Unable++;
            BotRunes.Unready($"mark: {why}");
            Rest(mage, name);

            return null;
        }

        Offered++;

        if (!_said)
        {
            _said = true;

            logger.Information("{Name} the {Class} is the first rune-mage offered a mark: {Place} for {Guild}", body.Name, mage.Class?.Name, name, guild.Name);
        }

        return new BotMark(map, guild.Name, name, kind, anchor, spot);
    }

    /// <summary>
    /// The place here the guild holds no rune for: its seat first — every recall home is made on it — then a cave mouth,
    /// which a company is gated to, then the town the mage stands in.
    /// </summary>
    private static bool Candidate(BotMobile mage, Guild guild, Map map, out string name, out string kind, out Point3D anchor)
    {
        var at = mage.Location;
        var near = BotRunes.Near * 2;

        var home = BotSeat.Home(mage);

        if (home != Point3D.Zero && Utility.InRange(at, home, SeatReach) && BotRuneShelf.Held(guild.Name, map, home, near) == null)
        {
            name = $"the seat of {guild.Name}";
            kind = "seat";
            anchor = home;

            return true;
        }

        if (BotGates.Ready)
        {
            var gates = BotGates.All;
            BotGates.Gate best = null;
            BotDungeon.Deep into = null;
            var nearest = int.MaxValue;

            for (var i = 0; i < gates.Count; i++)
            {
                var gate = gates[i];
                var away = BotRunes.Apart(gate.From, at);

                if (away > MouthReach || away >= nearest || BotDungeon.Under(gate.From) || BotDungeon.Holding(gate.To) is not { } deep)
                {
                    continue;
                }

                if (BotRuneShelf.Held(guild.Name, map, gate.From, near) != null)
                {
                    continue;
                }

                nearest = away;
                best = gate;
                into = deep;
            }

            if (best != null)
            {
                name = $"the mouth of {into.Name}";

                // A second mouth into the same dungeon is its own place, not the first one moved.
                if (BotRuneShelf.Named(guild.Name, name) is { } same && BotRunes.Apart(same.Spot, best.From) > near)
                {
                    name = $"the mouth of {into.Name} at ({best.From.X}, {best.From.Y})";
                }

                kind = "mouth";
                anchor = best.From;

                return true;
            }
        }

        if (BotTowns.Ensure(map) && BotTowns.Of(at) is { } town && BotRuneShelf.Held(guild.Name, map, town.Square, near) == null)
        {
            name = town.Name;
            kind = "town";
            anchor = town.Square;

            return true;
        }

        name = null;
        kind = null;
        anchor = Point3D.Zero;

        return false;
    }

    /// <summary>The tile a place's rune is marked on, found once per place and kept: the anchor, or the nearest tile the engine allows.</summary>
    private static bool SpotFor(Map map, string name, Point3D anchor, out Point3D spot)
    {
        if (_spots.TryGetValue(name, out var kept) && kept.Anchor == anchor)
        {
            spot = kept.Spot;

            return spot != Point3D.Zero;
        }

        if (!BotRunes.Spot(map, anchor, BotRunes.Near, out spot))
        {
            spot = Point3D.Zero;
        }

        _spots[name] = (anchor, spot);

        return spot != Point3D.Zero;
    }

    public static string Describe() =>
        Asked == 0
            ? "no rune-mage has been asked to mark"
            : $"{Asked} rune-mages asked to mark: {Offered} offered a place, {Nothing} stood nowhere new, {NoSpell} had neither Mark in the book nor a scroll of it, {NoBlank} had no blank, {Unable} could not cast it where they stood, {Busy} were in a company or underground, {Rested} resting after a mark failed or could not be cast there, {Forbidden} stood where the engine lets no rune be marked; {BotMark.Describe()}";

    public static void Forget()
    {
        Asked = 0;
        Busy = 0;
        NoSpell = 0;
        NoBlank = 0;
        Nothing = 0;
        Rested = 0;
        Forbidden = 0;
        Unable = 0;
        Offered = 0;
        _rest.Clear();
        _spots.Clear();
        _said = false;
        BotMark.Forget();
    }
}
