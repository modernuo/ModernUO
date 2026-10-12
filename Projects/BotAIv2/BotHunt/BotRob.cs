using System;
using System.Collections.Generic;
using Server.Items;
using Server.Logging;
using Server.Mobiles;
using Server.Regions;

namespace Server.BotAI.V2;

/// <summary>
/// Setting on one of our own for what is in its pack: kill it, go through the corpse, and go red for it.
///
/// <para>
/// <b>Patrick's order of 15.09.2026, and the rarest thought on the shard.</b> The fight is <see cref="BotBrawl"/>'s;
/// what is here is the two ends of it — the victim chosen for its purse and its solitude, and the corpse gone through
/// for coin — and the one consequence that makes it a crime rather than a hunt: the robber is red for an hour, by
/// <see cref="BotOutlaw"/>, from the moment the victim falls, whether or not it ever reaches the corpse.
/// </para>
///
/// <para>
/// A robber that finds itself losing gives up rather than dies for a purse; a victim that runs is let go. The trade
/// pays what it took, so a ledger will learn what robbery is worth — and a population in which that number is high
/// is a population carrying too much coin about, which is a fact worth having.
/// </para>
/// </summary>
public sealed class BotRob : BotDeed
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotRob));

    public const string Trade = "rob";

    /// <summary>The ledger's key for blackmail: a robbery that asks first. See <see cref="Demand"/>.</summary>
    public const string ExtortTrade = "extort";

    /// <summary>The share of the victim's purse a demand asks for.</summary>
    public static double DemandShare { get; set; } = 0.3;

    /// <summary>The least a demand asks for, however light the purse.</summary>
    public static int LeastDemand { get; set; } = 20;

    /// <summary>How near the victim a demand is made from.</summary>
    public static int DemandWithin { get; set; } = 2;

    /// <summary>Demands made.</summary>
    public static long Demands { get; private set; }

    /// <summary>Demands paid.</summary>
    public static long Paid { get; private set; }

    /// <summary>Demands refused.</summary>
    public static long Refused { get; private set; }

    /// <summary>Refusals answered with a blow: "or die".</summary>
    public static long Punished { get; private set; }

    /// <summary>Gold taken by demands.</summary>
    public static long Extorted { get; private set; }

    /// <summary>What a robbery is reckoned at per minute before experience corrects it. High: it is a rare thought, and when it comes it is meant to be acted on.</summary>
    public static double Prior { get; set; } = 400.0;

    public static double WorkMinutes { get; set; } = 3.0;

    /// <summary>How near the corpse the robber has to be to go through it.</summary>
    public static int LootReach { get; set; } = 2;

    /// <summary>Robberies that ended with a corpse and coin.</summary>
    public static long Done { get; private set; }

    /// <summary>Robberies given up: the robber losing, or the victim gone.</summary>
    public static long GivenUp { get; private set; }

    /// <summary>Robberies that ended with the mark buying its life rather than losing it. See <c>BotQuarter</c>.</summary>
    public static long Bought { get; private set; }

    /// <summary>Robberies that ended with the robber's own death: the victim's, or the posse's, answer.</summary>
    public static long Slain { get; private set; }

    /// <summary>How far from the victim a stalker hides before it moves quietly in. Fourteen: beyond a screen's middle.</summary>
    public static int HideAt { get; set; } = 14;

    /// <summary>How near the victim a robber that cannot move hidden walks before it hides and lies in wait.</summary>
    public static int AmbushWithin { get; set; } = 2;

    /// <summary>How near the victim a stalker has come when it strikes.</summary>
    public static int StrikeWithin { get; set; } = 2;

    /// <summary>How long a robber lies in wait hidden beside its victim before it strikes. Twelve seconds.</summary>
    public static int AmbushMs { get; set; } = 12000;

    /// <summary>
    /// How many failed tries at hiding before a robbery is given up.
    ///
    /// <para>
    /// <b>Three was a robbery given up before the skill had a chance to work.</b> The engine rolls Hiding against the
    /// bot's own number, so a thief at eleven hides about one try in nine, and on the night of 17.09.2026 robbery after
    /// robbery ended "could not hide near X" after thirty seconds of trying. Six is a minute of patience, which is what
    /// lying in wait is, and it does not touch the engine's odds — only how long the thief is willing to crouch there.
    /// </para>
    /// </summary>
    public static int HideTries { get; set; } = 6;

    /// <summary>Robberies struck after stalking the victim hidden.</summary>
    public static long Stalked { get; private set; }

    /// <summary>Robberies struck after lying in wait hidden beside the victim.</summary>
    public static long Ambushed { get; private set; }

    /// <summary>Robberies given up because the robber could not hide.</summary>
    public static long Unhidden { get; private set; }

    /// <summary>Robberies struck in the open because the robber was brought out of hiding on the way.</summary>
    public static long Spotted { get; private set; }

    /// <summary>
    /// How long a robber keeps trying for its blow from hiding before it settles for an honest one,
    /// counted from the last time it got nearer.
    ///
    /// <para>
    /// <b>A robber that cannot move hidden has to reach two tiles of its mark and then stand still long enough to
    /// hide</b>, and a mark walking about the island never gives it either. Cassia set on Pell at 18:22:13 on
    /// 18.09.2026 and gave up at 18:29:27 — seven minutes and twelve seconds of "lying in wait for Pell", not one
    /// blow struck, the mark four hundred tiles away by the end. Nine robberies before it went the same way. Patience
    /// is the missing number: past it the robber stops trying to be clever and sets on the mark where it stands.
    /// </para>
    ///
    /// <para>
    /// <b>Counted from the last gain, not from the press</b>, because the first form of this number cut off a robbery
    /// that was working. Pell set on Lorcan at 19:12:42 on 18.09.2026 from the far side of a waylay and was told to
    /// give up at 19:13:27 with the mark "31 tiles off" — having closed most of the way there. A clock from the press
    /// punishes distance; a clock from the last gain asks the only question worth asking, which is whether the chase
    /// is getting anywhere. A robber walking behind a mark of equal speed never gains a tile and stops in the same
    /// forty-five seconds it always did.
    /// </para>
    /// </summary>
    public static int PatienceMs { get; set; } = 45000;

    /// <summary>
    /// How near the mark a robber out of patience will still set on it openly.
    ///
    /// <para>
    /// <b>Eight was a melee number in an archer's hand.</b> Kerrin Ashdown, a WarriorArcher, set on Pell at 19:03:30
    /// on 18.09.2026 and gave up at 19:04:15 "could not get near Pell": forty-five seconds of walking after a bot
    /// walking away at the same speed, which never closes, while the bow on its back reaches three times eight. Past
    /// patience the robbery is handed to <c>BotBrawl</c>, which is the machinery every hunt and every war on this
    /// shard uses to close and to shoot, so this number only has to be near enough for that to be worth starting.
    /// </para>
    /// </summary>
    public static int OpenWithin { get; set; } = 16;

    /// <summary>How much nearer the robber has to get for the approach to count as working. One tile is noise.</summary>
    public static int Gaining { get; set; } = 2;

    /// <summary>Robberies where patience ran out and the robber struck in the open rather than from hiding.</summary>
    public static long Impatient { get; private set; }

    /// <summary>Robberies given up because patience ran out with the mark still out of reach.</summary>
    public static long Outwalked { get; private set; }

    /// <summary>Springs from a wait called off because somebody else stood by the mark: a blow that would be seen.</summary>
    public static long Watched { get; private set; }

    /// <summary>Springs from a wait struck at once, without a step taken.</summary>
    public static long Pounced { get; private set; }

    /// <summary>Of those, the ones whose thief had lost its cover before the blow: a spring with no ambush in it.</summary>
    public static long Bared { get; private set; }

    /// <summary>Of those, the ones where the mark was mounted and the robber afoot: a chase that cannot be won.</summary>
    public static long Outpaced { get; private set; }

    private enum Leg
    {
        Stalk,
        Demand,
        Fight,
        Loot
    }

    /// <summary>Whether this robbery asks for the purse before it takes the life: The Shadow's blackmail.</summary>
    private readonly bool _demand;

    private readonly BotMobile _victim;

    private readonly SkillName _trains;

    private BotBrawl _fight;

    private Leg _leg = Leg.Stalk;

    private int _hideFailures;

    private bool _hidOnce;

    private long _hidTick;

    private bool _stalking;

    /// <summary>Whether the blow was struck from hiding, for the record. See <see cref="BotUnderworld"/>.</summary>
    private bool _unseen;

    /// <summary>Whether the opening blow is a backstab: the striker can move while hidden.</summary>
    private bool _backstab;

    private Point3D _creep = Point3D.Zero;

    private Point3D _fell;

    private int _coins;

    private bool _murdered;

    private bool _quartered;

    /// <summary>Whether this robbery came out of a wait by the road: the thief is already hidden and already near.</summary>
    private readonly bool _sprang;

    /// <summary>When the ambush's hold on the mark runs out, or 0 if it was never held. See <c>BotQuarter</c>.</summary>
    private long _held;

    /// <summary>When this robbery last got nearer its mark, for <see cref="PatienceMs"/>.</summary>
    private long _gained;

    /// <summary>The nearest this robbery has been to its mark, and how far off it opened.</summary>
    private double _closest = double.MaxValue;

    private int _opened = -1;

    /// <param name="victim">Who.</param>
    /// <param name="trains">The skill the fighting trains.</param>
    /// <param name="demand">Blackmail: the purse is asked for first, and the fight comes only if it is refused.</param>
    public BotRob(BotMobile victim, SkillName trains, bool demand) : this(victim, trains)
    {
        _demand = demand;
    }

    /// <param name="sprang">Whether the thief was lying in wait and the mark walked into it.</param>
    public BotRob(BotMobile victim, SkillName trains, bool demand, bool sprang) : this(victim, trains, demand)
    {
        _sprang = sprang;
    }

    public BotRob(BotMobile victim, SkillName trains)
    {
        _victim = victim;
        _trains = trains;
        _fight = new BotBrawl(victim, BotBrawl.Robbery, trains, Over);
        _fell = victim?.Location ?? Point3D.Zero;
        _gained = Core.TickCount;
    }

    /// <summary>
    /// The tile the stalk walks to.
    ///
    /// <para>
    /// <b>To where the victim is, not after the victim.</b> The same cure <c>BotBrawl</c> has kept since 16.09.2026 and
    /// the same defect without it: a walk aimed at a moving bot is an order that changes with every step it takes, the
    /// walker reads a changed order as a new route, and the route never gets a tile closer. The first two robberies of
    /// the night of 17.09.2026 died exactly there — Perri Ashdown "could not get one tile closer to (1501, 962) in 35
    /// plans (creeping up on Neriah)" at 23:29:36, Cassia in 19 plans at 23:28:59 — with both victims walking in the
    /// open and neither robber ever drawing a weapon. The aim moves only once the victim has gone
    /// <c>BotBrawl.Restride</c> tiles from it.
    /// </para>
    /// </summary>
    private Point3D Creep()
    {
        if (_victim == null)
        {
            return _creep;
        }

        if (_creep == Point3D.Zero || !Utility.InRange(_victim.Location, _creep, BotBrawl.Restride))
        {
            _creep = _victim.Location;
        }

        return _creep;
    }

    /// <summary>
    /// Whether the mark stands alone: nobody but it and the robber within <see cref="BotRobber.Alone"/> tiles of it.
    ///
    /// <para>
    /// Asked again at the moment of the blow rather than trusted from the choosing. A mark is picked alone and walks
    /// on; a second later somebody's company crosses the same road, and the blow that was to be unseen is a blow with
    /// a witness standing beside it. Patrick's own words for why: "otherwise he'd be found out at once".
    /// </para>
    /// </summary>
    private bool Lonely(Map map, Mobile robber)
    {
        if (_victim == null || map == null)
        {
            return false;
        }

        foreach (var third in map.GetMobilesInRange<BotMobile>(_victim.Location, Math.Max(1, BotRobber.Alone)))
        {
            if (third != _victim && third != robber && third.Alive)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>The fight's rule: over when the victim is down, when the robber is losing, or when the victim is somebody's duel or catch.</summary>
    private static string Over(IBotWilful bot, BotBrawl brawl)
    {
        var body = bot?.Self;
        var victim = brawl?.Foe;

        if (victim is not { Deleted: false, Alive: true })
        {
            return "the victim is down";
        }

        if (body != null && body.HitsMax > 0 && body.Hits < body.HitsMax * BotSlay.FleeAt)
        {
            return "the robber lost its nerve";
        }

        if (BotDuel.Duelling(victim) || BotOutlaw.Jailed(victim))
        {
            return "the victim is spoken for";
        }

        // The victim reached a town: the guards are the end of any robbery, and a robber that follows it in is a robber
        // that fails its walk at the wall ("could not get nearer to Aric at (1652, 1580) in Britain", 11:40 on 16.09.2026).
        if (victim.Region?.GetRegion<GuardedRegion>() is { } guarded && !guarded.IsDisabled())
        {
            return "the victim reached the town";
        }

        return null;
    }

    public override string Kind => _demand ? ExtortTrade : Trade;

    public override bool Committed => true;

    public override bool Braves => true;

    /// <summary>
    /// A robber in the middle of a robbery is not tempted afresh.
    ///
    /// <para>
    /// <b>Delwyn struck Yarrow out of hiding for 48 at 23:49:08 on 17.09.2026 and dropped it one second later</b>,
    /// "pressed to rob: tempted by Corwin's 226gp" — a stalk, a hiding and an ambush thrown away for a fatter purse,
    /// and a victim left standing at 27 of 75 with nobody on it. The temptation is a press, and a press replaces what
    /// is in hand unless the work says it is already this work.
    /// </para>
    /// </summary>
    public override bool Repeats(BotDeed other) => other is BotRob;

    public override Mobile Foe => _victim;

    public override Map Map => _fight.Map;

    public override Point3D Where => _leg switch
    {
        Leg.Loot  => _fell,
        Leg.Stalk or Leg.Demand => _victim?.Location ?? _fell,
        _         => _fight.Where
    };

    /// <summary>A hidden bot that runs is revealed, so a stalk is walked.</summary>
    public override bool Hurries => _leg != Leg.Stalk;

    /// <summary>A hidden bot that rides is revealed, so a stalker goes on foot.</summary>
    public override bool Afoot => _leg == Leg.Stalk && _stalking;

    public override double Expects => Prior;

    public override double Minutes => WorkMinutes;

    public override SkillName? Trains => _fight.Trains;

    public override int Outlay => 0;

    public override double Coin => 1.0;

    public override int Made => 0;

    public override string Stage => _leg switch
    {
        Leg.Stalk  => _stalking ? $"stalking {_victim?.Name}" : $"lying in wait for {_victim?.Name}",
        Leg.Demand => $"demanding {_victim?.Name}'s purse",
        Leg.Fight  => $"setting on {_victim?.Name}",
        _         => $"going through {_victim?.Name}'s corpse"
    };

    public override BotDoing Advance(IBotWilful bot)
    {
        var body = bot?.Self;
        var map = _fight.Map;

        if (body == null || map == null || map == Map.Internal)
        {
            return BotDoing.Failed("no body");
        }

        if (_leg == Leg.Stalk)
        {
            if (Stalk(bot, body, map, out var stalking))
            {
                return stalking;
            }

            if (_demand)
            {
                _leg = Leg.Demand;
            }
            else
            {
                // <b>The blow out of hiding is worth three, and holds the victim where it stands.</b> Patrick's order of
                // 17.09.2026, night. Only a blow that was truly unseen: a robber brought out of hiding on the way in
                // opens with an ordinary swing, as it always did.
                if (_unseen)
                {
                    var stun = BotAmbush.Strike(body, _victim, _backstab);

                    _held = stun > 0 ? Core.TickCount + stun : 0;
                }

                // The blow is struck now, on a fight whose clocks start now rather than when the stalk began.
                _fight = new BotBrawl(_victim, BotBrawl.Robbery, _trains, Over);
                _leg = Leg.Fight;
            }
        }

        if (_leg == Leg.Demand && Demand(bot, body, map, out var demanding))
        {
            return demanding;
        }

        if (_leg == Leg.Fight)
        {
            // <b>Patrick's rule of the evening of 18.09.2026.</b> Beaten to a quarter of its health, the mark is given
            // the choice: run, throw down part of its pack, or fight on and maybe die because help may come. Asked
            // once, before the next blow, so that the answer is worth something. See BotQuarter.
            if (!_quartered && (BotQuarter.Beaten(_victim) || _held > 0 && Core.TickCount >= _held))
            {
                _quartered = true;

                if (BotQuarter.Choose(body as BotMobile, _victim) == BotQuarter.Answer.Pay)
                {
                    Bought++;

                    return BotDoing.Done($"{_victim.Name} bought its life");
                }
            }

            var doing = _fight.Advance(bot);

            if (doing.Kind != BotDoingKind.Done)
            {
                return doing;
            }

            if (_victim is { Deleted: false, Alive: true } || _victim == null)
            {
                GivenUp++;

                return BotDoing.Failed(doing.Note ?? "the victim got away");
            }

            // The victim fell. Red from this moment, whoever reaches the corpse first.
            if (!_murdered && ReferenceEquals(_victim.LastKiller, body))
            {
                _murdered = true;
                BotOutlaw.Murdered(body as BotMobile, _victim);
                BotUnderworld.Murder(body, _unseen);
            }

            _fell = _victim.Corpse?.Location ?? _victim.Location;
            _leg = Leg.Loot;
        }

        if (!body.InRange(_fell, LootReach))
        {
            return BotDoing.Walk(map, _fell, BotArrival.Within(LootReach), "to the corpse");
        }

        var corpse = _victim?.Corpse as Corpse ?? BotQuarry.Remains(map, _fell, _victim);
        var pack = body.Backpack;

        if (corpse == null || corpse.Deleted || pack == null)
        {
            Done++;
            BotUnderworld.Robbed(body);

            return BotDoing.Done($"{_victim?.Name} is dead and there was nothing left to take");
        }

        List<Item> lying = [.. corpse.Items];

        for (var i = 0; i < lying.Count; i++)
        {
            if (lying[i] is Gold coin && !coin.Deleted && coin.Movable)
            {
                _coins += coin.Amount;
                pack.DropItem(coin);
            }
        }

        BotOutlaw.Took(_coins);
        Done++;
        BotUnderworld.Robbed(body);

        logger.Information("{Name} went through {Victim}'s corpse and took {Coins}gp", body.Name, _victim?.Name, _coins);

        // And away from the body, to hide. See BotLieLow.
        if (_murdered && BotShadow.Running && BotShadow.CanHide(body))
        {
            BotInquest.LieLow(body as BotMobile, map, _fell);
        }

        return BotDoing.Done($"killed {_victim?.Name} and took {_coins}gp");
    }

    /// <summary>
    /// Blackmail, once the thief is beside its victim: out of hiding, "your purse or your life". The victim weighs the thief — and
    /// any of The Shadow within twelve tiles, hidden or not — against itself: at half its strength it never pays, at one and a
    /// half times it always does, and between the two by degrees. Paid, a share of the purse changes hands and the victim tells
    /// the Baron (<see cref="BotOutlaw.Want"/>), and the thief goes to lie low. Refused, the victim tells all the same; a thief
    /// that is the stronger sets on it — "or die" — and one that is not goes away. True with what to do; false to fight.
    ///
    /// <para>Patrick's order and choice of 17.09.2026: pay or die, and whoever paid informs the Baron.</para>
    /// </summary>
    private bool Demand(IBotWilful bot, Mobile body, Map map, out BotDoing doing)
    {
        doing = default;

        if (Over(bot, _fight) is { } over)
        {
            GivenUp++;
            doing = BotDoing.Failed(over);

            return true;
        }

        if (!body.InRange(_victim, DemandWithin))
        {
            doing = BotDoing.Walk(map, Creep(), BotArrival.Within(DemandWithin), $"up to {_victim.Name}");

            return true;
        }

        if (body.Hidden)
        {
            body.RevealingAction();
        }

        Demands++;

        var coins = _victim.Backpack?.GetAmount(typeof(Gold)) ?? 0;
        var ours = BotThreat.Power(body) + Brothers(body);
        var theirs = Math.Max(1.0, BotThreat.Power(_victim));
        var ratio = ours / theirs;

        BotVoice.Aloud(body, $"Your purse, {_victim.Name}, or your life.");

        if (coins > 0 && Utility.RandomDouble() < Math.Clamp(ratio - 0.5, 0.0, 1.0))
        {
            var amount = Math.Min(coins, Math.Max(LeastDemand, (int)(coins * DemandShare)));

            _victim.Backpack.ConsumeTotal(typeof(Gold), amount);
            body.AddToBackpack(new Gold(amount));
            BotVoice.Aloud(_victim, "Take it, and go.");

            Paid++;
            Extorted += amount;
            BotUnderworld.Extorted(body);

            // The victim's grudge and its tale (07.10.2026, traits17). See BotBiography.Extorted.
            BotBiography.Extorted(_victim, body, amount);
            BotOutlaw.Want(body as BotMobile, _victim.Name, $"taking {amount}gp from {_victim.Name} under threat");

            if (BotShadow.Running && BotShadow.CanHide(body))
            {
                BotInquest.LieLow(body as BotMobile, map, _victim.Location);
            }

            logger.Information(
                "{Name} made {Victim} pay {Amount}gp of {Coins} under threat, {Ours:F0} of strength against {Theirs:F0}",
                body.Name,
                _victim.Name,
                amount,
                coins,
                ours,
                theirs
            );

            doing = BotDoing.Done($"{_victim.Name} paid {amount}gp");

            return true;
        }

        BotVoice.Aloud(_victim, "Not one coin.");
        Refused++;
        BotOutlaw.Want(body as BotMobile, _victim.Name, $"threatening {_victim.Name}");

        if (ratio >= 1.0)
        {
            Punished++;

            logger.Information(
                "{Victim} would not pay {Name}, who sets on it, {Ours:F0} of strength against {Theirs:F0}",
                _victim.Name,
                body.Name,
                ours,
                theirs
            );

            _fight = new BotBrawl(_victim, BotBrawl.Robbery, _trains, Over);
            _leg = Leg.Fight;

            return false;
        }

        GivenUp++;

        logger.Information(
            "{Victim} would not pay {Name}, who goes away, {Ours:F0} of strength against {Theirs:F0}",
            _victim.Name,
            body.Name,
            ours,
            theirs
        );

        doing = BotDoing.Failed($"{_victim.Name} would not pay");

        return true;
    }

    /// <summary>The strength of The Shadow's other members within twelve tiles of this thief, hidden or not.</summary>
    private static double Brothers(Mobile body)
    {
        if (!BotUnderworld.Member(body) || body.Map is not { } map)
        {
            return 0.0;
        }

        var sum = 0.0;

        foreach (var m in map.GetMobilesInRange<BotMobile>(body.Location, 12))
        {
            if (m != body && m.Alive && BotUnderworld.Member(m))
            {
                sum += BotThreat.Power(m);
            }
        }

        return sum;
    }

    /// <summary>
    /// The robber's way in, unseen: walk up, hide, and either move quietly in (<see cref="BotShadow.CanStalk"/>) or lie in
    /// wait beside the victim for <see cref="AmbushMs"/>. True with what to do while it goes on; false when it is time to
    /// strike. See <see cref="BotShadow"/>: Patrick's order of 17.09.2026.
    /// </summary>
    private bool Stalk(IBotWilful bot, Mobile body, Map map, out BotDoing doing)
    {
        doing = default;

        // A robbery begun without the skill — pressed before build 112, or a bot whose hiding was taken back — goes in openly.
        if (!BotShadow.Running || !BotShadow.CanHide(body))
        {
            return false;
        }

        if (Over(bot, _fight) is { } over)
        {
            GivenUp++;

            if (body.Hidden)
            {
                body.RevealingAction();
            }

            doing = BotDoing.Failed(over);

            return true;
        }

        var now = Core.TickCount;
        var away = body.GetDistanceToSqrt(_victim);

        if (_opened < 0)
        {
            _opened = (int)away;
        }

        // Getting nearer is the whole of the answer to "is this working": it buys the robbery another PatienceMs.
        if (away < _closest - Gaining)
        {
            _closest = away;
            _gained = now;
        }

        // <b>Patience.</b> Every trick in this method needs the mark to hold still for a moment, and a mark going
        // about its day never does. Past PatienceMs without a single tile gained the robber takes what it can get: a
        // blow in the open if the mark is within OpenWithin, and otherwise an honest ending rather than a walk that
        // was never going to arrive.
        if (PatienceMs > 0 && now - _gained > PatienceMs)
        {
            if (away > OpenWithin)
            {
                Outwalked++;
                GivenUp++;
                Outpaced += _victim.Mounted && !body.Mounted ? 1 : 0;

                if (body.Hidden)
                {
                    body.RevealingAction();
                }

                // The distance and the mounts are in the ending on purpose: what OpenWithin should be is a question
                // about how far a robber actually ends up, and nothing else on the shard can answer it.
                doing = BotDoing.Failed(
                    $"could not get near {_victim.Name}, {(int)away} tiles off and gaining nothing, set on from {_opened}"
                    + (_victim.Mounted ? body.Mounted ? ", both mounted" : ", and it is mounted" : "")
                );

                return true;
            }

            Impatient++;

            if (body.Hidden)
            {
                body.RevealingAction();
            }

            logger.Information(
                "{Name} runs out of patience {Away} tiles from {Victim} and sets on it in the open",
                body.Name,
                (int)away,
                _victim.Name
            );

            return false;
        }

        _stalking = BotShadow.CanStalk(body) || _stalking && body.Hidden;

        // <b>Patrick's rule of the evening of 18.09.2026, the whole of it.</b> A thief lying hidden by the road, when
        // somebody walks within Pounce of it, strikes **at once** — no step taken, no second hiding, no twelve-second
        // count. One that can move hidden strikes from behind, which holds the mark a second longer. And it strikes
        // only if the mark is alone: "otherwise he'd be found out at once" — a blow with a third bot standing by is a blow that
        // is seen, so the thief lets that one pass and goes back to its wait.
        if (_sprang && _leg == Leg.Stalk && away <= BotRobber.Pounce)
        {
            if (!Lonely(map, body))
            {
                Watched++;
                GivenUp++;

                doing = BotDoing.Failed($"somebody else stood by {_victim.Name}");

                return true;
            }

            Pounced++;

            // Hidden, this is the ambush Patrick described, worth three and holding the mark. Revealed on the way —
            // the wait holds still for exactly this reason, but a search or a stray blow can still do it — the thief
            // sets on the mark all the same rather than beginning the two-tile dance that never ends.
            if (body.Hidden)
            {
                Ambushed++;
                _unseen = true;
                _backstab = _stalking;
            }
            else
            {
                Bared++;
            }

            return false;
        }

        // Off the horse to move hidden: the engine reveals a rider at its first step. Afoot keeps the horse away meanwhile.
        if (_stalking && body.Mounted)
        {
            BotStable.Alight(body);
        }

        if (!body.Hidden)
        {
            // Brought out of hiding on the way in — a failed roll at moving quietly, or somebody's search — and too near,
            // or too clumsy, to hide again: the blow is struck in the open.
            if (_hidOnce && (away <= StrikeWithin || _hideFailures >= HideTries))
            {
                Spotted++;

                return false;
            }

            var hideAt = _stalking ? HideAt : AmbushWithin;

            if (away > hideAt)
            {
                doing = BotDoing.Walk(map, Creep(), BotArrival.Within(hideAt), $"creeping up on {_victim.Name}");

                return true;
            }

            if (!BotShadow.Ready(body))
            {
                doing = BotDoing.Work($"waiting to hide near {_victim.Name}");

                return true;
            }

            if (!BotShadow.Hide(body))
            {
                if (++_hideFailures >= HideTries)
                {
                    Unhidden++;
                    GivenUp++;

                    doing = BotDoing.Failed($"could not hide near {_victim.Name}");

                    return true;
                }

                doing = BotDoing.Work($"trying to hide near {_victim.Name}");

                return true;
            }

            _hidOnce = true;
            _hidTick = now;
            doing = BotDoing.Work(_stalking ? $"hidden, {(int)away} tiles from {_victim.Name}" : $"lying in wait for {_victim.Name}");

            return true;
        }

        if (!_stalking)
        {
            if (now - _hidTick < AmbushMs)
            {
                doing = BotDoing.Work($"lying in wait for {_victim.Name}");

                return true;
            }

            Ambushed++;
            _unseen = true;

            return false;
        }

        if (away <= StrikeWithin)
        {
            Stalked++;
            _unseen = true;

            return false;
        }

        if (body.AllowedStealthSteps <= 0)
        {
            // A failed roll reveals the robber; the next beat reads that as seen, or hides again.
            BotShadow.Quiet(body);
            doing = BotDoing.Work($"moving quietly towards {_victim.Name}");

            return true;
        }

        doing = BotDoing.Walk(map, Creep(), BotArrival.Within(StrikeWithin), $"stalking {_victim.Name}");

        return true;
    }

    public override bool Bend(IBotWilful bot) => _leg == Leg.Fight && _fight.Bend(bot);

    public override void Drop(IBotWilful bot)
    {
        _fight.Drop(bot);

        if (_leg == Leg.Stalk && bot?.Self is { Hidden: true } hidden)
        {
            hidden.RevealingAction();
        }

        // <b>A robber that died in the attempt was nowhere in the log.</b> The deed was let go for the death, the death
        // line named no robbery, and Selwyn's twenty-four seconds against Lorcan beside the Baron's company on
        // 16.09.2026 read as nothing at all. The posse's answer is the commonest ending a robbery has, and is counted.
        if (_leg == Leg.Fight && bot?.Self is { Alive: false } body)
        {
            Slain++;

            logger.Information("{Name}'s robbery of {Victim} ended with its own death at ({X}, {Y})", body.Name, _victim?.Name, body.X, body.Y);
        }
    }

    public static string Describe() =>
        $"{Done} robberies carried through, {GivenUp} given up and {Slain} ended with the robber's death; {Demands} demands made ({Paid} paid, {Extorted}gp in all; {Refused} refused, {Punished} of those answered with a blow), {Bought} marks bought their lives, {Pounced} sprung on a mark that walked into the wait ({Bared} of them already in plain sight) and {Watched} let pass for somebody standing by, {Impatient} set on the mark openly when patience ran out and {Outwalked} were outwalked ({Outpaced} of them by a mounted mark), {Stalked} struck after stalking hidden, {Ambushed} from lying in wait beside the victim, {Spotted} in the open after being brought out of hiding, {Unhidden} given up for want of hiding; {BotAmbush.Describe()}; {BotQuarter.Describe()}; {BotShadow.Describe()}; {BotSkulk.Describe()}";

    public static void Forget()
    {
        Done = 0;
        GivenUp = 0;
        Slain = 0;
        Stalked = 0;
        Ambushed = 0;
        Demands = 0;
        Paid = 0;
        Refused = 0;
        Punished = 0;
        Extorted = 0;
        Unhidden = 0;
        Spotted = 0;
        Impatient = 0;
        Outwalked = 0;
        Watched = 0;
        Pounced = 0;
        Bared = 0;
        Outpaced = 0;
        Bought = 0;
        BotAmbush.Forget();
        BotQuarter.Forget();
        BotFence.Forget();
        BotSilence.Forget();
        BotLair.Forget();
        BotStash.Forget();
        BotFetch.Forget();
        BotShadow.Forget();
        BotSkulk.Forget();
    }
}

/// <summary>
/// Puts, very rarely, the thought of robbing somebody into a fighter's head: once every <see cref="EveryMs"/> a fighter
/// rolls, and with <see cref="Chance"/> it looks for a victim — alone, outside any town, with coin in its pack and
/// weaker than itself — and is pressed into the robbery on the spot. Nearly every answer is nothing, and every gate is
/// counted.
///
/// <para>
/// <b>Pressed, not offered, and the first hour of build 70 is why.</b> As a proposer this marked two victims and won
/// nothing: the thought came up once, went into the auction at 400 over three minutes against a hunt at 459, lost,
/// and was gone — a mark is not kept, and the next roll was ten minutes off. A temptation is not an opportunity to be
/// weighed against a seam; it is what the bot does next, which is what <see cref="BotWill.Press"/> is for. The rarity
/// stays in <see cref="Chance"/>, where Patrick put it.
/// </para>
/// </summary>
public static class BotRobber
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotRobber));

    /// <summary>Whether the thought is ever offered.</summary>
    public static bool Running { get; set; } = true;

    /// <summary>How often one fighter rolls for the thought. Ten minutes.</summary>
    public static int EveryMs { get; set; } = 600000;

    /// <summary>
    /// The chance the roll comes up. One in fifty since 12:00 on 16.09.2026: one in a hundred gave a robbery a day
    /// (forty-four of forty-six temptations finding nobody alone with coin outside a town); one in twenty, once the
    /// victim's gate was loosened to four tiles, gave four temptations in thirty-five minutes — not "a very rare
    /// thought", which is Patrick's word for it. The rarity lives here; the gate is only whether there is anybody to rob.
    /// </summary>
    public static double Chance { get; set; } = 0.02;

    /// <summary>How far a tempted robber looks for a victim. Sixty since 16.09.2026: at thirty, with the victim also asked to be alone, ten temptations in ten found nobody.</summary>
    public static int Reach { get; set; } = 60;

    /// <summary>
    /// How far from any other bot the victim has to be. Four since 16.09.2026, and it was twelve: at twelve, on an island
    /// where eighty bots hunt in companies and trade in one town, every temptation of the morning "found nobody alone" —
    /// 54 of 56. A victim with a friend six tiles off is a robbery somebody sees begin, which is what the defender and
    /// the red hour are for.
    /// </summary>
    public static int Alone { get; set; } = 4;

    /// <summary>The least coin in the victim's pack worth killing for.</summary>
    public static int Purse { get; set; } = 50;

    /// <summary>
    /// How far from the nearest town wall a victim must stand to be worth robbing at all.
    ///
    /// <para>
    /// <b>Seven of the eight robberies given up in the first crime window ended "the victim reached the town".</b> On
    /// the night of 17.09.2026, between 23:33 and 23:42, fourteen robberies were pressed, one was carried through and
    /// seven died because the mark walked through a ward while it was being stalked — which is right, since guards are
    /// the end of any robbery, and is also a minute of a bot's life thrown away every time. Nearly every bot on this
    /// island works in or beside a town, so a robber that does not ask this question is a robber whose work is mostly
    /// walking after somebody who is about to be safe. Eight tiles are sampled at this range round the mark; any of
    /// them inside a ward and the mark is left alone.
    /// </para>
    /// </summary>
    public static int FromTown { get; set; } = 24;

    /// <summary>
    /// Whether a mark whose work in hand ends inside a town is passed over. See the note in <c>Prey</c>.
    /// </summary>
    public static bool Heading { get; set; } = true;

    /// <summary>
    /// How near its townward errand a mark has to be before that errand disqualifies it.
    ///
    /// <para>
    /// <b>A bot walking to town from three hundred tiles out is the highwayman's mark, not a bot to pass over.</b>
    /// The first form of this gate asked only where the work ended, and turned away 29 marks in nineteen minutes
    /// against two robberies in the twenty before it: half the island is on its way to a counter at any moment.
    /// What makes a mark hopeless is being nearly there.
    /// </para>
    /// </summary>
    public static int HomeWithin { get; set; } = 80;


    /// <summary>Marks passed over for standing too near a town to be robbed before they reach it.</summary>
    public static long Townbound { get; private set; }

    /// <summary>Marks passed over for being on their way into a town, whatever they are worth where they stand.</summary>
    public static long Homing { get; private set; }

    /// <summary>Marks passed over for standing inside a house, which the road search cannot reach.</summary>
    public static long Roofed { get; private set; }

    /// <summary>How much stronger than the victim the robber has to be, as a ratio of powers.</summary>
    public static double Edge { get; set; } = 1.0;

    public static long Rolled { get; private set; }

    /// <summary>Thoughts of robbery that went to a guild's hall. See BotBurgle.</summary>
    public static long Burgling { get; private set; }

    public static long Tempted { get; private set; }

    public static long NoVictim { get; private set; }

    /// <summary>Fighters pressed into a robbery.</summary>
    public static long Pressed { get; private set; }

    /// <summary>Of those, how many were taken off work they had in hand.</summary>
    public static long Interrupted { get; private set; }

    /// <summary>Temptations that sent a bot to practise hiding instead, because it could not hide well enough to rob.</summary>
    public static long Practising { get; private set; }

    /// <summary>Temptations that came up on a bot in a company, in a fight, failing or otherwise not its own master.</summary>
    public static long NotFree { get; private set; }

    /// <summary>Thoughts of robbery that came to a bot holding steadfast work — a paid lesson, a stake — and were kept.</summary>
    public static long Holding { get; private set; }

    /// <summary>How often the roster is swept for fighters due a roll. Ten seconds; each fighter still rolls once per <see cref="EveryMs"/>.</summary>
    public static int SweepMs { get; set; } = 10000;

    /// <summary>
    /// How long after a boot robbery is not thought of at all.
    ///
    /// <para>
    /// <b>Every restart of the evening of 18.09.2026 produced a doomed robbery inside a minute of the boot</b> —
    /// 18:00:43, 19:03:30, 19:17:42, 19:36:09, each of them ending "the victim reached the town". Two reasons, and
    /// both are about the boot rather than about robbery: the world puts its bots back where they were saved, which
    /// is mostly in and beside the towns, and at that moment no bot has any work in hand, so the gate that asks a
    /// mark where it is going (<see cref="Heading"/>) is blind. `BotFence.SettleMs` cured exactly this for the
    /// keeper in build 152; this is the same cure for the same minute.
    /// </para>
    /// </summary>
    public static int SettleMs { get; set; } = 120000;

    /// <summary>Sweeps passed over because the island had not settled after a boot.</summary>
    public static long Settling { get; private set; }

    private static readonly Dictionary<Serial, long> _rolledTick = [];

    private static long _sweptTick;

    private static long? _woke;

    /// <summary>One sweep of the roster, from <c>BotOutlaw</c>'s clock.</summary>
    public static void Beat(long now)
    {
        if (!Running || now - _sweptTick < SweepMs)
        {
            return;
        }

        _sweptTick = now;

        // Kept as the first tick seen rather than against nought, because a tick count is only safe to compare by
        // subtraction. See SettleMs.
        _woke ??= now;

        if (now - _woke.Value < SettleMs)
        {
            Settling++;

            return;
        }

        var bots = BotPopulation.Bots;

        BotSkulk.Rank(bots);

        for (var i = 0; i < bots.Count; i++)
        {
            Roll(bots[i], now);
            BotSkulk.Urge(bots[i], now);
        }
    }

    private static void Roll(BotMobile body, long now)
    {
        if (body is not { Deleted: false, Alive: true, Fallen: false } || body.Class is not { } klass)
        {
            return;
        }

        var map = body.Map;

        if (map == null || map == Map.Internal || body.Squad != null || BotDuel.Duelling(body) || BotOutlaw.Jailed(body))
        {
            return;
        }

        if (klass.Role is BotRole.Producer or BotRole.Medic || klass.Unpaid || klass.Leads)
        {
            return;
        }

        // The band's fence is shy of every unlawful act — Patrick's own words for it. It keeps the goods; it does not take them.
        if (BotFence.Is(body))
        {
            return;
        }

        // <b>A thief that has been lying in wait and seen somebody has already had the thought.</b> It skips the
        // clock and the odds both: the rarity is about how often robbery occurs to a bot, not about how often a bot
        // already waiting by the road notices the traveller it is waiting for. See BotWaylay.
        var sprung = _sprung.Remove(body.Serial, out var seen);

        var bandit = BotUnderworld.Member(body) || BotUnderworld.Outlawed(body);

        if (!sprung && _rolledTick.TryGetValue(body.Serial, out var last) && now - last < (bandit ? BotUnderworld.BanditEveryMs : EveryMs))
        {
            return;
        }

        _rolledTick[body.Serial] = now;
        Rolled++;

        // Robbery is The Shadow's trade, and its members think of it five times as often. See BotUnderworld.
        if (!sprung && Utility.RandomDouble() >= (bandit ? BotUnderworld.MemberRobChance : Chance))
        {
            return;
        }

        Tempted++;

        // Only a bot that is its own master: free, or busy at its own work. Bound is a company's, and a bot being
        // hunted or failing has enough on its hands. Read after the roll so that the gate is counted and not hidden
        // inside the odds.
        if (body is not IBotWilful bot || BotLadder.Standing(bot) is not (BotStanding.Free or BotStanding.Busy))
        {
            NotFree++;

            return;
        }

        // <b>Not off work it is holding.</b> A press does not wait, and the itch to practise hiding pressed Brannoc and
        // Jarek out of a paid lesson at 15:43 on 22.09.2026 with 5.5 and 5.3 points of ten in hand — the one class of the
        // afternoon the captain held to the end, emptied by the underworld. Work that is Steadfast is being held for a
        // reason; the thought keeps until the next roll.
        // <b>Except the wait by the road, which is held for exactly this.</b> BotWaylay is Steadfast so that nothing pulls
        // a thief off its wait — and this gate, from the evening of 22.09.2026, then refused the robbery the wait had
        // sprung: from then on every mark a wait saw walked past ("10 saw a mark, 10 of them never set on" in the six
        // hours to 15:14 on 25.09.2026), no robbery was carried through, nobody became a murderer, and The Shadow — two
        // murderers found it — was never founded again after the reset of 21.09. Patrick noticed the band was missing.
        if (!sprung && bot.Resolve?.Deed is { Steadfast: true })
        {
            Holding++;

            return;
        }

        // <b>A crook acts unseen, or practises until it can.</b> Patrick's order of 17.09.2026, and his choice over robbing
        // in the open meanwhile: a bot that hides worse than BotShadow.RobHiding goes off to practise hiding instead.
        if (BotShadow.Running && !BotShadow.CanHide(body))
        {
            if (!BotWill.Press(bot, new BotSkulk(map, body.Location), $"tempted, but it hides at {body.Skills.Hiding.Base:F1} of the {BotShadow.RobHiding:F0} a robbery wants"))
            {
                NotFree++;

                return;
            }

            Practising++;

            logger.Information(
                "{Name} the {Class} is tempted to rob, but hides at only {Hiding:F1} of the {Wanted:F0} it would need, and goes off to practise hiding instead",
                body.Name,
                klass.Name,
                body.Skills.Hiding.Base,
                BotShadow.RobHiding
            );

            return;
        }

        // <b>Or a guild's hall.</b> Patrick, 26.09.2026: "since The Shadow is the enemy of every guild, bandits may rob guild
        // houses, by stealth and lockpicking." A bandit's thought is of a hall a share of the time, when some guild within
        // reach has something put by and has not been tried lately. See BotBurgle.
        if (bandit && !sprung && Utility.RandomDouble() < BotBurgle.Share && BotBurgle.Target(body, map, out var whose) is { } hall)
        {
            if (BotWill.Press(bot, new BotBurgle(hall, whose), $"tempted by what {whose} has put by"))
            {
                Burgling++;

                logger.Information(
                    "{Name} the {Class} is tempted by the chest of {Guild} ({Gold}gp put by) and creeps off to break into its hall at {X},{Y}",
                    body.Name,
                    klass.Name,
                    whose,
                    BotChest.Holds(whose),
                    hall.X,
                    hall.Y
                );

                return;
            }
        }

        if (body.Region?.GetRegion<GuardedRegion>() is { } guarded && !guarded.IsDisabled())
        {
            NoVictim++;
            Wait(thief: body, klass, map, bot);

            return;
        }

        // A member of The Shadow asks first as often as not. See BotRob.Demand — but never out of a wait: a thief
        // that has been lying still while somebody walked up to it strikes, and asking gives the mark the two seconds
        // it needs to reach a ward.
        var demand = !sprung && BotUnderworld.Member(body) && Utility.RandomDouble() < BotUnderworld.ExtortShare;

        // <b>The mark the wait actually saw, not whoever is there a second later.</b> At 15:13:19 on 18.09.2026 Joss
        // Ashdown's wait ended "a mark came by" and the setting-on a second afterwards read "tempted with nobody
        // about": at a place bots walk through, one second is the difference between a mark in the open and a mark
        // inside the ward. What the wait saw is what it sets on.
        var victim = sprung && seen is { Deleted: false, Alive: true } && !BotOutlaw.Jailed(seen)
            ? seen
            : Prey(body, map, demand ? BotUnderworld.ExtortEdge : Edge);

        if (victim == null)
        {
            NoVictim++;
            Wait(thief: body, klass, map, bot);

            return;
        }

        var coins = victim.Backpack?.GetAmount(typeof(Gold)) ?? 0;
        var held = bot.Resolve?.Deed?.Kind;
        var deed = new BotRob(victim, bot.Bond?.Weapon?.Skill ?? SkillName.Wrestling, demand, sprung);

        if (!BotWill.Press(bot, deed, $"tempted by {victim.Name}'s {coins}gp"))
        {
            NotFree++;

            return;
        }

        Pressed++;

        if (held != null)
        {
            Interrupted++;
        }

        logger.Information(
            "{Name} the {Class} is tempted and sets on {Victim} the {VictimClass}, alone with {Coins}gp in its pack, for {Crime}{Dropping}",
            body.Name,
            klass.Name,
            victim.Name,
            victim.Class?.Name,
            coins,
            demand ? "blackmail" : "robbery",
            held == null ? "" : $", dropping {held}"
        );
    }

    /// <summary>The richest victim within reach that is alone, outside a town, not red, not of the robber's own band, and weaker.</summary>
    /// <summary>Whether a ward stands within <see cref="FromTown"/> of this tile, sampled at the eight bearings. See that dial.</summary>
    private static bool Nearing(Map map, Point3D at) => Nearing(map, at, FromTown);

    private static bool Nearing(Map map, Point3D at, int span)
    {
        for (var i = 0; i < 8; i++)
        {
            var x = at.X + (i is 0 or 1 or 7 ? span : i is 3 or 4 or 5 ? -span : 0);
            var y = at.Y + (i is 1 or 2 or 3 ? span : i is 5 or 6 or 7 ? -span : 0);

            if (BotOutlaw.Guarded(map, x, y, at.Z))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// How much further from a ward a waiting place must stand than a mark must, as a multiple of <see cref="FromTown"/>.
    ///
    /// <para>
    /// <b>A mark that can run inside is no mark.</b> The first robbery the waiting ever produced, at 13:25:58 on
    /// 18.09.2026, ended "failed at rob: the victim reached the town": the place <c>BotCommons</c> said paid best was
    /// (1440, 1440), which clears <see cref="FromTown"/> honestly and is still a few seconds' run from Britain's ward.
    /// Twenty-four tiles is the right distance for judging whether somebody <em>counts</em> as a mark; it is the wrong
    /// distance for choosing where to spend five minutes waiting for one.
    /// </para>
    /// </summary>
    public static int WaitRoom { get; set; } = 1;

    /// <summary>Bots that were tempted, found nobody, and went to lie in wait instead. See <c>BotWaylay</c>.</summary>
    public static long Waiting { get; private set; }

    /// <summary>Temptations that found nowhere worth waiting at either.</summary>
    public static long Nowhere { get; private set; }

    private static readonly Dictionary<Serial, BotMobile> _sprung = [];

    /// <summary>
    /// How near a mark must come before a thief lying in wait springs on it.
    ///
    /// <para>
    /// <b>Fifteen, by Patrick's order of the evening of 18.09.2026, and the ordinary search is sixty — that
    /// difference is the whole of whether a robbery can succeed.</b> A thief springing on somebody sixty tiles off has
    /// to walk there, and a thief with no Stealth is revealed by its first step, so the mark sees it coming and runs,
    /// and a mark that runs reaches a town: "failed at extort: the victim reached the town" at 14:17:48 on 18.09.2026,
    /// and the same the robbery before it. Waiting is for what comes to you. Within fifteen the blow is struck where
    /// the thief lies, with no step taken at all — see the spring in <c>Stalk</c>.
    /// </para>
    /// </summary>
    public static int Pounce { get; set; } = 15;

    /// <summary>Whoever worth robbing stands within <see cref="Pounce"/> of this bot now, or null. Asked by a thief lying in wait.</summary>
    public static BotMobile Mark(BotMobile robber, Map map) =>
        Running && robber is { Deleted: false, Alive: true } && map != null && map != Map.Internal
            ? Prey(robber, map, Edge, Math.Max(1, Pounce))
            : null;

    /// <summary>
    /// A thief lying in wait has seen somebody: set on it at the next beat.
    ///
    /// <para>
    /// Queued rather than done on the spot, because the seeing happens inside the wait's own <c>Advance</c>, and
    /// pressing a deed there would replace the very work whose answer is about to be applied. The outlaws' clock runs
    /// every second, so the mark has a second's grace and no more.
    /// </para>
    /// </summary>
    public static void Spring(BotMobile robber, BotMobile mark)
    {
        if (robber is { Deleted: false })
        {
            _sprung[robber.Serial] = mark;
        }
    }

    /// <summary>
    /// Sends a thief that has thought of robbery and found nobody to where the island's work is, to wait there.
    ///
    /// <para>
    /// <b>Where the work is, is where the marks are.</b> <c>BotCommons</c> keeps what pays where, measured by the
    /// population itself over thousands of outcomes; those are the places bots walk to alone with coin in their packs
    /// all day. The nearest one outside a town and beyond <see cref="FromTown"/> of a ward is the place to wait.
    /// </para>
    /// </summary>
    private static void Wait(BotMobile thief, BotClass klass, Map map, IBotWilful bot)
    {
        if (!BotWaylay.Running || bot.Resolve?.Deed is BotWaylay or BotRob)
        {
            return;
        }

        var at = Ambush(thief, map);

        if (at == Point3D.Zero)
        {
            Nowhere++;

            return;
        }

        if (!BotWill.Press(bot, new BotWaylay(map, at), "tempted, with nobody about to rob"))
        {
            NotFree++;

            return;
        }

        Waiting++;

        logger.Information(
            "{Name} the {Class} is tempted with nobody about, and goes to lie in wait at ({X}, {Y})",
            thief.Name,
            klass?.Name,
            at.X,
            at.Y
        );
    }

    /// <summary>The nearest paying place fit to wait at: on this map, outside a town, and not too near a ward.</summary>
    private static Point3D Ambush(BotMobile thief, Map map)
    {
        var places = BotCommons.Best(32);

        // Well clear of the wards first; anywhere merely legal second. The fallback matters: this island's paying
        // places cluster round its one town, and a wait with nowhere to go is a temptation wasted.
        return Ambush(thief, map, places, Math.Max(1, WaitRoom)) is { } room && room != Point3D.Zero
            ? room
            : Ambush(thief, map, places, 1);
    }

    /// <summary>How many of the nearest fit places are asked whether there is a road to them at all.</summary>
    public static int Tried { get; set; } = 4;

    /// <summary>Waits called off because the shard has no road to the place chosen.</summary>
    public static long Sealed { get; private set; }

    private static readonly List<(Point3D At, double Span)> _fit = [];

    private static Point3D Ambush(
        BotMobile thief,
        Map map,
        List<(string Kind, Map Map, Point3D Where, double PerMinute, int Settled, int Minded)> places,
        int room
    )
    {
        _fit.Clear();

        for (var i = 0; i < places.Count; i++)
        {
            var (_, where, at, _, _, _) = places[i];

            if (where != map || at == Point3D.Zero || Nearing(map, at, FromTown * room))
            {
                continue;
            }

            if (Region.Find(at, map)?.GetRegion<GuardedRegion>() is { } guarded && !guarded.IsDisabled())
            {
                continue;
            }

            _fit.Add((at, thief.GetDistanceToSqrt(at)));
        }

        _fit.Sort((x, y) => x.Span.CompareTo(y.Span));

        // <b>A place with no road to it is not a place.</b> "failed at waylay: no way through to (1440, 1248, 0)" at
        // 13:54:57 on 18.09.2026: BotCommons keeps where work paid, and the middle of a patch is not always a tile a
        // bot can walk to. The nearest few are asked before one is chosen, rather than the walk finding out.
        for (var i = 0; i < _fit.Count && i < Math.Max(1, Tried); i++)
        {
            if (BotReach.Ask(map, thief.Location, _fit[i].At, BotArrival.Within(3)) == BotReachVerdict.Sealed)
            {
                Sealed++;

                continue;
            }

            return _fit[i].At;
        }

        return Point3D.Zero;
    }

    private static BotMobile Prey(BotMobile robber, Map map, double edge) => Prey(robber, map, edge, Reach);

    private static BotMobile Prey(BotMobile robber, Map map, double edge, int reach)
    {
        BotMobile best = null;
        var richest = 0;
        var mine = BotThreat.Power(robber);

        foreach (var other in map.GetMobilesInRange<BotMobile>(robber.Location, reach))
        {
            if (other == robber || !other.Alive || other.Squad != null || BotOutlaw.IsRed(other) || BotDuel.Duelling(other)
                || BotOutlaw.Jailed(other))
            {
                continue;
            }

            if (robber.Guild != null && ReferenceEquals(other.Guild, robber.Guild))
            {
                continue;
            }

            var coins = other.Backpack?.GetAmount(typeof(Gold)) ?? 0;

            if (coins < Purse || coins <= richest)
            {
                continue;
            }

            if (other.Region?.GetRegion<GuardedRegion>() is { } guarded && !guarded.IsDisabled())
            {
                continue;
            }

            if (FromTown > 0 && Nearing(map, other.Location))
            {
                Townbound++;

                continue;
            }

            // <b>A mark under a roof is a mark nobody can walk to.</b> Six robberies in the window of 23:46 on
            // 17.09.2026 ended "no way through to (1377, 1464, 10)" one after another, every one of them a bot standing
            // inside a guild hall: a house is entered by its door, the road search does not find one, and the robbery
            // spends its minute failing. Robbery is a thing done on a road.
            if (other.Region?.IsPartOf<HouseRegion>() == true)
            {
                Roofed++;

                continue;
            }

            // <b>A mark on its way to town is not a mark.</b> Nine robberies in a row on 18.09.2026 ended "the victim
            // reached the town", six of them inside half a minute and before a single blow was struck; the last, Joss
            // Ashdown lying in wait for Oswin at 17:48, spent 1.7 minutes on a peddler carrying ribs to a stall in
            // Britain. <see cref="FromTown"/> is measured from where the mark is standing, which says nothing at all
            // about where it is walking: a bot whose work in hand ends inside a ward is inside it within the minute,
            // however rich and however alone it looks now. Robbery needs a mark with somewhere else to be — but only
            // one that is nearly home counts as lost, by HomeWithin: a bot walking to a counter from the far side of
            // the island is the highwayman's whole trade.
            if (Heading && other.Resolve?.Deed is { } work && work.Map == map && work.Where != Point3D.Zero
                && other.GetDistanceToSqrt(work.Where) <= HomeWithin
                && (BotOutlaw.Guarded(map, work.Where.X, work.Where.Y, work.Where.Z) || Nearing(map, work.Where)))
            {
                Homing++;

                continue;
            }

            if (mine < BotThreat.Power(other) * edge)
            {
                continue;
            }

            var alone = true;

            foreach (var third in map.GetMobilesInRange<BotMobile>(other.Location, Alone))
            {
                if (third != other && third != robber && third.Alive)
                {
                    alone = false;

                    break;
                }
            }

            if (!alone)
            {
                continue;
            }

            best = other;
            richest = coins;
        }

        return best;
    }

    public static string Describe() =>
        !Running
            ? "the thought of robbery is never offered"
            : $"{Settling} sweeps passed over while the island settled after a boot, {Rolled} rolls for the thought of robbery at {Chance:P1} every {EveryMs / 60000} min, {Tempted} came up, {NotFree} on a bot not its own master, {Holding} on a bot holding steadfast work, {NoVictim} found nobody alone with {Purse}gp outside a town, {Townbound} stood too near a town to be worth it, {Homing} were walking into one, {Roofed} stood under a roof, {Practising} sent to practise hiding first, {Waiting} sent to lie in wait where the work is ({Nowhere} found nowhere worth waiting at, {Sealed} places passed over for having no road to them), {Pressed} pressed into a robbery ({Interrupted} off work in hand); {BotWaylay.Describe()}; {BotRob.Describe()}";

    public static void Forget()
    {
        _rolledTick.Clear();
        Rolled = 0;
        Tempted = 0;
        NoVictim = 0;
        NotFree = 0;
        Holding = 0;
        Pressed = 0;
        Interrupted = 0;
        Practising = 0;
        Settling = 0;
        Townbound = 0;
        Homing = 0;
        Roofed = 0;
        _sweptTick = 0;
        BotRob.Forget();
    }
}
