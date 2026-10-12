using System;
using System.Collections.Generic;
using Server.Mobiles;

namespace Server.BotAI.V2;

/// <summary>
/// A fight with one of our own — a duel, a robbery, a manhunt — as a piece of work: close, hit, and stop when told.
///
/// <para>
/// <b>Every fight on this shard until 15.09.2026 was against a creature.</b> <c>BotSlay</c> takes a
/// <c>BaseCreature</c>, <c>BotThreat</c> counts only creatures, and a bot hit by another bot swung back by reflex
/// and otherwise carried on with its errand. Patrick's orders for the night — a championship between the strongest
/// bots, murderers who kill and rob, a Baron's patrol that catches them — all need a bot to go after a bot on
/// purpose, and this is the one undertaking that does it. The fighting itself is still the engine's: <c>Combatant</c>
/// and <c>Warmode</c> start its swing timer, and a caster throws what <c>BotStrike</c> says it can.
/// </para>
///
/// <para>
/// <b>It ends when whoever set it up says so, not when the other side is dead.</b> A duel is over at a share of
/// health; a robbery at a corpse; a manhunt at a catch. So the caller hands in the rule — a function answering with
/// the reason the fight is over, or nothing while it goes on — and the deed asks it every beat before it swings.
/// Nothing here decides whether a bot may fight another; that is the caller's, and the engine's notoriety rules
/// underneath (see <c>BotMobile.IsHarmfulCriminal</c>).
/// </para>
///
/// <para>
/// Committed and summoning: it is taken by being pressed on a bot rather than won at auction, and no better offer
/// takes the bot off it. Unpaid, so the earnings veto lets it through; braves lethal ground, because the duel's ring
/// and the robber's victim are wherever they are.
/// </para>
/// </summary>
public sealed class BotBrawl : BotDeed
{
    /// <summary>What a brawl is reckoned at per minute. High: it is pressed, not chosen, and the number only orders it against other pressed work.</summary>
    public static double Prior { get; set; } = 200.0;

    /// <summary>How long a brawl is expected to take.</summary>
    public static double WorkMinutes { get; set; } = 3.0;

    /// <summary>The longest any brawl may go on before it is called, whatever the rule says. Five minutes.</summary>
    public static int CapMs { get; set; } = 300000;

    /// <summary>How long a foe may be out of sight or out of reach before the brawl is given up as lost.</summary>
    public static int LostMs { get; set; } = 30000;

    /// <summary>
    /// The same for a robbery or a manhunt, which chase a foe that is walking somewhere rather than one summoned to a
    /// ring. Ninety seconds: Fendrel's first robbery on 16.09.2026 was given up at thirty with Ysolt prowling away from
    /// it, never in reach.
    /// </summary>
    public static int ChaseMs { get; set; } = 90000;

    /// <summary>
    /// The same for a manhunt, which may start the width of the island from the red: ten minutes, as both the lost
    /// clock and the cap. At ninety seconds on 16.09.2026 the Baron walked towards a murderer nine hundred tiles off
    /// in ninety-second legs, giving up and taking it again each time, pressing a fresh posse at each.
    /// </summary>
    public static int HuntMs { get; set; } = 600000;

    /// <summary>Brawls taken up, by kind.</summary>
    public static long Duels { get; private set; }

    public static long Robberies { get; private set; }

    public static long Manhunts { get; private set; }

    public static long Defences { get; private set; }

    /// <summary>Brawls that ran to the cap.</summary>
    public static long Capped { get; private set; }

    /// <summary>Brawls given up because the foe could not be reached or seen for <see cref="LostMs"/>.</summary>
    public static long Lost { get; private set; }

    public const string Duel = "duel";

    public const string Robbery = "rob";

    public const string Manhunt = "manhunt";

    public const string Defence = "defend";

    private readonly Mobile _foe;

    private readonly Map _map;

    private readonly string _kind;

    private readonly Func<IBotWilful, BotBrawl, string> _over;

    private readonly SkillName _trains;

    private readonly long _began;

    private long _seenTick;

    private bool _aiming;

    private bool _cast;

    private long _castTick;

    private int _casts;

    /// <summary>Casts this brawl began (<see cref="BotStrike.Begin"/> answered yes), whether or not they came round to a target.</summary>
    private int _begun;

    /// <summary>Casts of this brawl the engine broke off before a target came: see <see cref="Broken"/>.</summary>
    private int _broken;

    /// <summary>Whether the cast in flight has come round to its target, so its ending without an aim is a timeout, not a break.</summary>
    private bool _sequenced;

    /// <summary>The last reason the attack ladder was closed to this bot, in words (<see cref="BotStrike.Why"/>), and its gate.</summary>
    private string _why;

    private string _gate;

    private long _whyTick;

    /// <summary>Whether the bot has been a caster at any beat of this brawl: something to throw, or a class that fights by the book.</summary>
    private bool _caster;

    /// <summary>Whether the foe was ever in reach, so a caster's silence is not blamed on a gate it never stood at.</summary>
    private bool _reached;

    /// <summary>Whether this brawl's casting has been added to <see cref="Spells"/>. Once, when it is quieted.</summary>
    private bool _tallied;

    private Point3D _found;

    /// <summary>Where the walk is aimed: the foe's tile as it was, moved only when the foe has gone <see cref="Restride"/> from it.</summary>
    private Point3D _aim;

    /// <summary>How far the foe may move before the walk is re-aimed at it. Six: a plan's worth of steps, not one.</summary>
    public static int Restride { get; set; } = 6;

    /// <param name="foe">Who to fight.</param>
    /// <param name="kind">The ledger's key: <see cref="Duel"/>, <see cref="Robbery"/>, <see cref="Manhunt"/> or <see cref="Defence"/>.</param>
    /// <param name="trains">The skill the swinging trains, named by the caller from the bot's own weapon.</param>
    /// <param name="over">The rule: the reason the fight is over, or null while it goes on. Asked every beat.</param>
    public BotBrawl(Mobile foe, string kind, SkillName trains, Func<IBotWilful, BotBrawl, string> over)
    {
        _foe = foe;
        _map = foe?.Map;
        _kind = kind ?? Duel;
        _trains = trains;
        _over = over;
        _began = Core.TickCount;
        _seenTick = _began;
        _found = foe?.Location ?? Point3D.Zero;
    }

    /// <summary>Whether this brawl has been counted. Once, on the first beat it is advanced.</summary>
    private bool _counted;

    /// <summary>
    /// Counted on the first beat rather than in the constructor: a proposer builds a brawl to offer it, and an offer
    /// the auction turns down was being reported as a fight. "1 robberies fought" on 16.09.2026 with no robbery.
    /// </summary>
    private void Count()
    {
        if (_counted)
        {
            return;
        }

        _counted = true;

        switch (_kind)
        {
            case Duel:
                Duels++;

                break;

            case Robbery:
                Robberies++;

                break;

            case Manhunt:
                Manhunts++;

                break;

            default:
                Defences++;

                break;
        }
    }

    /// <summary>How long this brawl has stood, in milliseconds.</summary>
    public long Elapsed => Core.TickCount - _began;

    /// <summary>Spells thrown in it.</summary>
    public int Casts => _casts;

    public override string Kind => _kind;

    public override bool Committed => true;

    public override bool Steadfast => true;

    public override bool Summons => true;

    public override bool Braves => true;

    public override bool Unpaid => true;

    public override Mobile Foe => _foe;

    public override Map Map => _map;

    public override Point3D Where => _foe is { Deleted: false } ? _foe.Location : _found;

    public override double Expects => Prior;

    public override double Minutes => WorkMinutes;

    public override SkillName? Trains => _trains;

    public override int Outlay => 0;

    public override double Coin => 0.0;

    public override int Made => 0;

    public override string Stage => $"{_kind}: fighting {_foe?.Name ?? "somebody"} ({_casts} casts, {Elapsed / 1000}s{Spellless})";

    /// <summary>
    /// Why a caster's brawl has thrown nothing, for its stage and so its finished line; empty for anybody who has cast, or who
    /// has nothing to do with spells.
    ///
    /// <para>
    /// <b>The finished line said "0 casts" and nothing else (08.10.2026, cast30).</b> At the arenas on 07.10.2026 the casters
    /// cast until they had nothing left and then wrestled, and no line said so: of the Mage, Warrior-Mage and Healer runs of bouts
    /// in the two championships not one went from nought casts back to some, and the run before the noughts was a supply being
    /// spent — Jasper 11, 7, 8, 4 and then 0; Vaeril 10, 3, then 0 four times; Meliora 4, 4, 4, then 0 against Morgrim; Ambrose
    /// 3, 4, 4, then 0 — with the referee filling every pool before every bout. The save of 01:35 on 08.10.2026 held a spellbook
    /// with an attack in it in all 93 casters' packs and herbs for none of their attacks in 61: 27 of 45 Mages, 15 of 26
    /// Warrior-Mages, 19 of 21 Healers (whose one attack, magic arrow, burns sulphurous ash; 2 of the 21 carried any).
    /// </para>
    ///
    /// <para>
    /// <b>Explained, not cured, in a championship (cast30 reviews A4/B F4).</b> A bout burns 4 to 11 casts and the referee fills the
    /// pool, never the pack, so a caster held at the arenas through several bouts still runs dry; the line now says so. A restock
    /// before a bout, out of the bot's own purse, is left for later.
    /// </para>
    /// </summary>
    private string Spellless =>
        _casts > 0 ? ""
        : _begun > 0 ? $", {_begun} begun, none landed"
        : _why != null ? $", no spell: {_why}{(_kind == Duel && _gate == BotStrike.NoHerbs ? " — the arena fills the pool, not the pack" : "")}"
        : "";

    public override BotDoing Advance(IBotWilful bot)
    {
        var body = bot?.Self;

        if (body == null || _map == null || _map == Map.Internal)
        {
            return BotDoing.Failed("no body");
        }

        Count();

        // A cast of ours past its delay with its target up, seen on any beat — a walking one too, where the aim below is never
        // reached: it came round, so if it ends unaimed it timed out and was not broken off (cast30 review A5).
        if (_aiming && body.Spell is Server.Spells.Spell { State: Server.Spells.SpellState.Sequencing })
        {
            _sequenced = true;
        }

        if (_foe is not { Deleted: false } || _foe.Map != _map)
        {
            Quiet(body);

            return BotDoing.Done($"{_foe?.Name ?? "the foe"} is gone");
        }

        // The caller's rule first, before anything is swung: a duel called at a share of health must not land one
        // more blow on the way out.
        var verdict = _over?.Invoke(bot, this);

        if (verdict != null)
        {
            Quiet(body);

            return BotDoing.Done(verdict);
        }

        if (!_foe.Alive)
        {
            Quiet(body);

            return BotDoing.Done($"{_foe.Name} is down");
        }

        var cap = _kind == Manhunt ? HuntMs : CapMs;

        if (Core.TickCount - _began >= cap)
        {
            Capped++;
            Quiet(body);

            return BotDoing.Done($"the {_kind} ran to its cap");
        }

        _found = _foe.Location;

        var casting = BotStrike.Can(body);
        var armed = casting && BotStrike.Best(body) >= 0;
        var reach = Math.Max(body.Weapon?.MaxRange ?? 1, armed ? BotStrike.Range : 0);

        // <b>A caster with nothing it can throw fights with what it holds, and says which gate closed (08.10.2026, cast30).</b>
        // Reach is the weapon's then — the staff's one tile — which is what Patrick saw: mages and healers "standing and not
        // fighting", wrestling at arm's length with a full pool and a book. That part is right, there being nothing else to do with
        // a book and no herbs; what was wrong is that nothing said so. Asked at most once a second, because Why builds the spells to
        // read their herbs.
        if (casting || body is BotMobile { Class.Role: BotRole.Caster or BotRole.Medic })
        {
            _caster = true;

            if (!armed && Core.TickCount - _whyTick >= 1000)
            {
                _whyTick = Core.TickCount;
                _why = BotStrike.Why(body, out _gate);
            }
        }

        if (!body.InRange(_foe.Location, reach) || !body.InLOS(_foe))
        {
            var lost = _kind == Manhunt ? HuntMs : _kind == Robbery ? ChaseMs : LostMs;

            if (Core.TickCount - _seenTick >= lost)
            {
                Lost++;
                Quiet(body);

                return BotDoing.Done($"{_foe.Name} could not be reached for {lost / 1000}s");
            }

            // <b>To where the foe is, not after the foe.</b> A walk after a moving bot is an order that changes with
            // every step it takes, and the walker reads a changed order as a new route: on 16.09.2026 three robberies
            // in three ended "could not get nearer to Joss" with Joss walking in the open. The aim is the foe's tile,
            // and it moves only when the foe has gone Restride tiles from it.
            //
            // <b>And at once when the aim is reached and the foe is not (07.10.2026, reach28).</b> The aim is where the foe stood,
            // and a foe walking at the same moment is no longer there: two duellists set a few tiles apart in a ring each walked to
            // the other's first tile, passed through each other and stood three to five tiles apart, each "arrived", the foe
            // under Restride from the aim, so never re-aimed — 5 of 8 bouts of the 23:01 batch at the arenas called at 30s.
            var arrival = BotArrival.Within(Math.Max(1, reach - 1));

            if (_aim == Point3D.Zero || !Utility.InRange(_foe.Location, _aim, Restride) || arrival.Reached(body.Location, _aim))
            {
                _aim = _foe.Location;
            }

            _walking = true;

            return BotDoing.Walk(_map, _aim, arrival, $"after {_foe.Name}");
        }

        _seenTick = Core.TickCount;
        _reached = true;

        // <b>In reach is where the walk ends (07.10.2026, reach28).</b> Fighting is answered as Work, and Work leaves the journey as
        // it was: the walk to the foe's first tile went on under the fight, step after step, carried the bot past a foe walking the
        // other way and out of its reach again. Measured live at 23:01 on 07.10.2026: Valindra and Ingrun, Berengar and Katrin,
        // Belthorn and Gareth stood two to five tiles apart in an open ring with each one's journey "after" the other aimed one tile
        // behind itself — the other's first tile — for thirty seconds, until the brawl gave the foe up as unreachable.
        if (body.InRange(_foe.Location, Math.Max(1, reach - 1)))
        {
            Halt(bot);
        }

        Leftover(bot);

        body.Warmode = true;
        body.Combatant = _foe;

        if (!casting)
        {
            return BotDoing.Work($"fighting {_foe.Name}");
        }

        if (_aiming && body.Target != null)
        {
            _aiming = false;

            if (BotStrike.Aim(body, _foe))
            {
                _casts++;
            }

            return BotDoing.Work($"casting at {_foe.Name}");
        }

        if (body.Spell != null)
        {
            return BotDoing.Work("casting");
        }

        // A cast of ours that ended with no target to fill: the engine broke it off — a blow on a spell above the first circle
        // (Spell.OnCasterHurt), something put in a hand — and spent nothing for it. Counted, so a caster whose spells never land is
        // told apart from one that never began any.
        //
        // Only a spell that never came round to a target is broken: a target that timed out on a walk is not (cast30 review A5), and
        // that one is told by the spell having reached its target once — see _sequenced.
        if (_aiming)
        {
            _aiming = false;

            if (!_sequenced)
            {
                Broken++;
                _broken++;
            }
        }

        _sequenced = false;

        if (_cast && Core.TickCount - _castTick < BotStrike.CastMs)
        {
            return BotDoing.Work($"between spells at {_foe.Name}");
        }

        var spell = BotStrike.Best(body);

        if (spell >= 0 && BotStrike.Begin(body, spell))
        {
            _aiming = true;
            _cast = true;
            _castTick = Core.TickCount;
            _begun++;
            _sequenced = false;

            return BotDoing.Work($"casting at {_foe.Name}");
        }

        // Refused with a spell in hand — the engine's recovery, mostly, which passes on its own: the gate is named for the finished
        // line, and the next beat asks again.
        if (spell >= 0 && Core.TickCount - _whyTick >= 1000)
        {
            _whyTick = Core.TickCount;
            _why = BotStrike.Why(body, out _gate);
        }

        return BotDoing.Work($"fighting {_foe.Name}");
    }

    /// <summary>Whether the last answer was a walk after the foe, so the walk is ended once the foe is in reach. See <see cref="Halt"/>.</summary>
    private bool _walking;

    /// <summary>Walks ended because the foe came into reach with the walk still on the road. See <see cref="Halt"/>.</summary>
    public static long Halted { get; private set; }

    /// <summary>
    /// Ends this brawl's own walk, and only that: the one errand on the road, aimed where this brawl aimed it. The aim is
    /// cleared too, so a foe that steps out of reach again is aimed at where it stands then.
    /// </summary>
    private void Halt(IBotWilful bot)
    {
        if (!_walking)
        {
            return;
        }

        _walking = false;

        var journey = bot?.Journey;

        // Only when it is the whole road: an interruption on top of it is somebody else's walk, left to end by itself.
        if (journey is { Queued: 1, Bottom: { Interruption: false, Follow: null } errand } && errand.Where == _aim && errand.Map == _map)
        {
            journey.Complete();
            Halted++;
        }

        _aim = Point3D.Zero;
    }

    /// <summary>Whether the road under this brawl has been looked at for another deed's walk. Once, on the first beat in reach.</summary>
    private bool _cleared;

    /// <summary>Walks of a deed put down under the brawl, taken off the road the first time the foe was in reach. See <see cref="Leftover"/>.</summary>
    public static long Leftovers { get; private set; }

    /// <summary>
    /// <b>Nor another deed's road (07.10.2026, reach28 review B).</b> Pressing puts the deed in hand down but leaves its walk on the
    /// journey, and a brawl in reach on its first beat has asked for no walk of its own: an archer pressed by a lawman, a defence taken
    /// up on the way to market, a robber's stalk went on walking the old road while it fought. On the first beat in reach, with no walk
    /// asked for by this brawl yet, the one errand left at the bottom of the road is ended — never an interruption nor a company's place.
    /// </summary>
    private void Leftover(IBotWilful bot)
    {
        if (_cleared)
        {
            return;
        }

        _cleared = true;

        if (bot?.Resolve is not { } resolve || resolve.Sent.Kind != BotDoingKind.None)
        {
            return;
        }

        if (bot.Journey is { Queued: 1, Bottom: { Interruption: false } errand } journey && errand.Reason is not ("station" or "sweep" or "bind" or "raise"))
        {
            journey.Complete();
            Leftovers++;
        }
    }

    /// <summary>Lowers the sword. The engine's combat timer runs on the combatant, so clearing it is what stops the swinging.</summary>
    private void Quiet(Mobile body)
    {
        Tally();

        if (body != null && ReferenceEquals(body.Combatant, _foe))
        {
            body.Combatant = null;
            body.Warmode = false;
        }
    }

    public override void Drop(IBotWilful bot) => Quiet(bot?.Self);

    /// <summary>
    /// Casters' brawls, by kind (08.10.2026, cast30): how many there were, how many threw anything, the spells thrown and begun, and
    /// why the rest threw nothing. Indexed by <see cref="SpellSlot"/>.
    /// </summary>
    private static readonly Dictionary<string, long[]> Spells = [];

    /// <summary>Casts begun in a brawl that ended with no target to aim: broken off by the engine.</summary>
    public static long Broken { get; private set; }

    private enum SpellSlot
    {
        Casters,
        Cast,
        Thrown,
        Begun,
        NoSpell,
        NoMana,
        NoHerbs,
        Refused,
        AllBroken,
        Unreached,
        Unknown,
        Count
    }

    /// <summary>Adds this brawl's casting to <see cref="Spells"/>, once, when it is quieted.</summary>
    private void Tally()
    {
        if (_tallied || !_counted || !_caster)
        {
            return;
        }

        _tallied = true;

        if (!Spells.TryGetValue(_kind, out var row))
        {
            Spells[_kind] = row = new long[(int)SpellSlot.Count];
        }

        row[(int)SpellSlot.Casters]++;
        row[(int)SpellSlot.Thrown] += _casts;
        row[(int)SpellSlot.Begun] += _begun;

        if (_casts > 0)
        {
            row[(int)SpellSlot.Cast]++;

            return;
        }

        // Begun and none landed: broken off when the engine broke any, otherwise unknown (a spell still in flight at the end, a
        // timeout); a null gate is unknown too, not "no attack spell" (cast30 review A5).
        var slot = _begun > 0 ? _broken > 0 ? SpellSlot.AllBroken : SpellSlot.Unknown
            : !_reached ? SpellSlot.Unreached
            : _gate switch
            {
                BotStrike.NoMana => SpellSlot.NoMana,
                BotStrike.NoHerbs => SpellSlot.NoHerbs,
                BotStrike.Refused => SpellSlot.Refused,
                BotStrike.NoSpell => SpellSlot.NoSpell,
                _ => SpellSlot.Unknown
            };

        row[(int)slot]++;
    }

    /// <summary>The casters' part of <see cref="Describe"/>: per kind of brawl, casters that fought, how many cast, and why the rest did not.</summary>
    private static string DescribeSpells()
    {
        if (Spells.Count == 0)
        {
            return "no caster has fought a brawl";
        }

        var parts = new List<string>(Spells.Count);

        foreach (var (kind, r) in Spells)
        {
            parts.Add(
                $"{kind} {r[(int)SpellSlot.Cast]} of {r[(int)SpellSlot.Casters]} cast ({r[(int)SpellSlot.Thrown]} spells of {r[(int)SpellSlot.Begun]} begun; "
                + $"none for: {r[(int)SpellSlot.NoHerbs]} no reagents, {r[(int)SpellSlot.NoMana]} no mana, {r[(int)SpellSlot.NoSpell]} no attack spell, "
                + $"{r[(int)SpellSlot.Refused]} refused by the engine, {r[(int)SpellSlot.AllBroken]} begun and broken off, {r[(int)SpellSlot.Unreached]} never in reach, "
                + $"{r[(int)SpellSlot.Unknown]} begun or barred with no gate named)"
            );
        }

        return $"casters in brawls: {string.Join(", ", parts)}; {Broken} casts broken off in all";
    }

    public static string Describe() =>
        Duels + Robberies + Manhunts + Defences == 0
            ? "no bot has fought another on purpose"
            : $"{Duels} duels, {Robberies} robberies, {Manhunts} manhunts and {Defences} defences fought bot against bot, {Capped} ran to the cap and {Lost} lost their foe, {Halted} walks ended with the foe in reach, {Leftovers} walks of a deed put down taken off the road; {DescribeSpells()}";

    public static void Forget()
    {
        Duels = 0;
        Robberies = 0;
        Manhunts = 0;
        Defences = 0;
        Capped = 0;
        Lost = 0;
        Halted = 0;
        Leftovers = 0;
        Broken = 0;
        Spells.Clear();
    }
}
