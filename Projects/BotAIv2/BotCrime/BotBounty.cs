using System;
using System.Collections.Generic;
using Server.Logging;

namespace Server.BotAI.V2;

/// <summary>
/// Offers a free fighter of any guild a head on a town's wanted list, priced at the bounty.
///
/// <para>
/// <b>Patrick, 07.10.2026: "a wanted list: the town sets a bounty on a head, bounty hunters from any guild, trial and jail when
/// caught".</b> The hunt is the manhunt the shard already has (<see cref="BotBrawl.Manhunt"/>): close, fight, and stop when the
/// quarry is down, caught or no longer wanted. A wanted thief beaten to a quarter yields and is taken alive
/// (<see cref="BotCrime.Yield"/>); a kill is a catch all the same (<c>BotOutlaw.Fell</c> reads <see cref="BotCrime.Hunts"/>), and
/// either way the bounty is paid out of the town's purse to whoever took it (<see cref="BotCrime.Caught"/>). A hunter must be
/// <see cref="Odds"/> times as strong as its quarry, as fit as a company's choosing rule wants, and no more than
/// <see cref="HuntersPer"/> hunt one head at once. Traders are neutral and take no part; neither does anybody of The Shadow.
/// </para>
/// </summary>
public sealed class BotBountyBoard : IBotProposer
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotBountyBoard));

    /// <summary>Whether bounties are hunted at all. The list and the payment stand either way: the law-abiding still take what they see.</summary>
    public static bool Running { get; set; } = true;

    /// <summary>How far a hunter will go after a head.</summary>
    public static int Reach { get; set; } = 400;

    /// <summary>How much stronger than its quarry a hunter must be.</summary>
    public static double Odds { get; set; } = 1.2;

    /// <summary>The share of its health a hunter must have to take a bounty.</summary>
    public static double Healthy { get; set; } = 0.8;

    /// <summary>Hunters on one head at once.</summary>
    public static int HuntersPer { get; set; } = 2;

    /// <summary>The minutes a hunt is reckoned at, for the price a minute: a bounty over the walk and the fight.</summary>
    public static double Minutes { get; set; } = 6.0;

    /// <summary>How long the board's list of heads is kept between reads.</summary>
    public static int ListMs { get; set; } = 10000;

    public static long Looks { get; private set; }

    public static long Offered { get; private set; }

    public static long Taken { get; private set; }

    public static long Crowded { get; private set; }

    public static long Weak { get; private set; }

    public static long Ended { get; private set; }

    private static readonly List<(BotMobile Bot, BotCrime.Sheet Sheet)> _heads = [];

    private static long _listed;

    private static readonly Dictionary<Serial, HashSet<Serial>> _hunters = [];

    public string Name => "Bounty";

    public BotStanding Rung => BotStanding.Free;

    public BotDeed Propose(IBotWilful bot)
    {
        if (!Running || !BotCrime.Running || bot?.Self is not BotMobile { Deleted: false, Alive: true } body)
        {
            return null;
        }

        var now = Core.TickCount;

        if (now - _listed >= ListMs)
        {
            _listed = now;
            BotCrime.Wanted(_heads);
        }

        if (_heads.Count == 0)
        {
            return null;
        }

        var map = body.Map;

        if (map == null || map == Map.Internal || body.Tired || body.Squad != null || body.Class is not { } klass || klass.Neutral
            || klass.Role is BotRole.Producer || BotUnderworld.Member(body) || BotCrime.IsThief(body) || BotOutlaw.Outlaw(body)
            || BotOutlaw.Jailed(body) || BotCrime.Posted(body) || body.HitsMax <= 0 || body.Hits < body.HitsMax * Healthy
            || BotDungeon.Under(body.Location))
        {
            return null;
        }

        Looks++;

        var power = BotThreat.Now(body);

        for (var i = 0; i < _heads.Count; i++)
        {
            var (head, sheet) = _heads[i];

            if (head == body || head is not { Deleted: false, Alive: true } || head.Map != map || !sheet.Posted
                || !Utility.InRange(body.Location, head.Location, Reach) || BotDungeon.Under(head.Location)
                || BotGuilds.Same(body, head))
            {
                continue;
            }

            if (power < BotThreat.Now(head) * Odds)
            {
                Weak++;

                continue;
            }

            if (Hunters(head) >= Math.Max(1, HuntersPer) && !Hunting(head, body))
            {
                Crowded++;

                continue;
            }

            Offered++;

            var trains = body.Bond?.Weapon?.Skill ?? SkillName.Wrestling;

            return new BotBountyHunt(head, sheet.Bounty, sheet.Town, trains);
        }

        return null;
    }

    private static int Hunters(Mobile head) => _hunters.TryGetValue(head.Serial, out var set) ? set.Count : 0;

    private static bool Hunting(Mobile head, Mobile hunter) =>
        _hunters.TryGetValue(head.Serial, out var set) && set.Contains(hunter.Serial);

    public static void On(Mobile head, Mobile hunter, int bounty, string town)
    {
        if (!_hunters.TryGetValue(head.Serial, out var set))
        {
            _hunters[head.Serial] = set = [];
        }

        if (set.Add(hunter.Serial))
        {
            Taken++;

            logger.Information("{Hunter} the {Class} takes the bounty of {Bounty}gp on {Head}, wanted in {Town}", hunter.Name, (hunter as BotMobile)?.Class?.Name, bounty, head.Name, town);

            BotVoice.Say(hunter, "local", $"{head.Name} is worth {bounty} gold to {town}. I am going after it.", true);
            BotEvents.Post("crime", hunter, $"takes the bounty of {bounty}gp on {head.Name}");
        }
    }

    public static void Off(Mobile head, Mobile hunter)
    {
        if (head != null && hunter != null && _hunters.TryGetValue(head.Serial, out var set) && set.Remove(hunter.Serial))
        {
            Ended++;

            if (set.Count == 0)
            {
                _hunters.Remove(head.Serial);
            }
        }
    }

    /// <summary>The rule the bounty's fight follows: over when the head is down, taken, yields, or comes off the list.</summary>
    public static string Over(IBotWilful bot, BotBrawl brawl)
    {
        var head = brawl?.Foe as BotMobile;

        if (head is not { Deleted: false, Alive: true })
        {
            return $"{head?.Name ?? "the wanted"} is down";
        }

        if (BotOutlaw.Jailed(head))
        {
            return $"{head.Name} is in a cell";
        }

        if (BotCrime.Yield(bot, head))
        {
            return $"{head.Name} yielded";
        }

        return BotCrime.Posted(head) ? null : $"{head.Name} is no longer wanted";
    }

    public static string Describe() =>
        !Running
            ? "bounties: off"
            : $"bounties: {Looks} looks at the list by free fighters, {Offered} hunts offered and {Taken} taken ({Weak} heads passed over as too strong, {Crowded} with {HuntersPer} hunters on them already), {Ended} hunts ended";

    public static void Forget()
    {
        _heads.Clear();
        _hunters.Clear();
        _listed = 0;
        Looks = 0;
        Offered = 0;
        Taken = 0;
        Crowded = 0;
        Weak = 0;
        Ended = 0;
    }
}

/// <summary>A bounty hunter after one head on a town's wanted list. See <see cref="BotBountyBoard"/>.</summary>
public sealed class BotBountyHunt : BotDeed
{
    public const string Trade = "bounty";

    private readonly BotMobile _head;

    private readonly BotBrawl _fight;

    private readonly int _bounty;

    private readonly string _town;

    private Mobile _body;

    public BotBountyHunt(BotMobile head, int bounty, string town, SkillName trains)
    {
        _head = head;
        _bounty = bounty;
        _town = town;
        _fight = new BotBrawl(head, BotBrawl.Manhunt, trains, BotBountyBoard.Over);
    }

    public override string Kind => Trade;

    public override Map Map => _fight.Map;

    public override Point3D Where => _fight.Where;

    /// <summary>The bounty over the hunt's minutes: the town's price, reckoned as gold a minute.</summary>
    public override double Expects => Math.Max(1.0, _bounty / Math.Max(1.0, BotBountyBoard.Minutes));

    public override double Minutes => BotBountyBoard.Minutes;

    /// <summary>A price the town posted and holds, corrected by how much of such prices is earned rather than by the trade's average.</summary>
    public override bool Posted => true;

    public override bool Committed => true;

    public override bool Steadfast => true;

    public override bool Braves => true;

    public override Mobile Foe => _head;

    public override SkillName? Trains => _fight.Trains;

    public override string Stage => $"bounty on {_head?.Name} ({_bounty}gp from {_town}): {_fight.Stage}";

    public override void Taken(IBotWilful bot)
    {
        _body = bot?.Self;

        if (_body != null && _head != null)
        {
            BotBountyBoard.On(_head, _body, _bounty, _town);
        }
    }

    public override void Drop(IBotWilful bot)
    {
        _fight.Drop(bot);
        BotBountyBoard.Off(_head, bot?.Self ?? _body);
    }

    public override BotDoing Advance(IBotWilful bot)
    {
        var doing = _fight.Advance(bot);

        if (doing.Kind is BotDoingKind.Done or BotDoingKind.Failed)
        {
            BotBountyBoard.Off(_head, bot?.Self ?? _body);
        }

        return doing;
    }
}
