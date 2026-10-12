using System;
using Server.Logging;

namespace Server.BotAI.V2;

/// <summary>
/// Offers a thief a relic in a carrier's pack (07.10.2026, relics20; Patrick: "a relic can be stolen — thieves get a target"). The
/// thieves are <c>BotCrime</c>'s: a member of The Shadow or a recruit on its roll (<see cref="BotCrime.IsThief"/>), and every try
/// is one of the island's hourly tries at a pack (<see cref="BotCutpurse.Room"/>, <see cref="BotCutpurse.Spent"/>), so relic
/// thefts are bounded by the same number the crowds by the banks are.
///
/// <para>
/// <b>The relic bar.</b> The engine's Stealing will not lift an item that cannot be moved, and a relic cannot, so the try is the
/// shard's own, on the engine's skill: only a thief at <see cref="Bar"/> Stealing or better is offered a relic at all, and it takes
/// it with a chance of its skill over a hundred times <see cref="Odds"/> (at most <see cref="Most"/>). A miss is always seen: the
/// carrier cries thief and reports it to the town. A lift is seen <see cref="SeenChance"/> of the time, and then reported with the
/// weight of two (a relic's theft posts the thief at once). Only a relic on the road is a target — one in a hall is taken by a war
/// alone — and never one carried by the thief's own guild. At most one relic theft is tried on the island every
/// <see cref="EveryMs"/>.
/// </para>
/// </summary>
public sealed class BotRelicSnatch : IBotProposer
{
    public string Name => "RelicThief";

    public BotStanding Rung => BotStanding.Free;

    public static bool Running { get; set; } = true;

    /// <summary>The least Stealing a thief brings to be offered a relic.</summary>
    public static double Bar { get; set; } = 30.0;

    /// <summary>The chance of a lift is Stealing / 100 × this, at most <see cref="Most"/>.</summary>
    public static double Odds { get; set; } = 0.6;

    public static double Most { get; set; } = 0.6;

    /// <summary>The chance a lift is seen anyway.</summary>
    public static double SeenChance { get; set; } = 0.5;

    /// <summary>How far a thief goes after a carrier.</summary>
    public static int Reach { get; set; } = 150;

    /// <summary>The least time between two relic thefts tried on the island.</summary>
    public static int EveryMs { get; set; } = 1800000;

    /// <summary>How long a thief closes on a carrier before it gives up.</summary>
    public static int CloseMs { get; set; } = 120000;

    /// <summary>What a relic's theft is reported as having taken: it raises the bounty on the thief's head.</summary>
    public static int Worth { get; set; } = 300;

    /// <summary>What the try is reckoned at per minute before the ledger has measured one.</summary>
    public static double Prior { get; set; } = 60.0;

    public static long Offered { get; private set; }

    public static long Tried { get; private set; }

    public static long Stolen { get; private set; }

    public static long Missed { get; private set; }

    public static long Seen { get; private set; }

    public static long GaveUp { get; private set; }

    private static long _lastTick;

    private static bool _tried;

    internal static bool Spacing(long now) => _tried && now - _lastTick < EveryMs;

    internal static void Spend(Mobile thief)
    {
        _tried = true;
        _lastTick = Core.TickCount;
        Tried++;
        BotCutpurse.Spent(thief);
    }

    internal static void Lifted() => Stolen++;

    internal static void Miss() => Missed++;

    internal static void Saw() => Seen++;

    internal static void Lose() => GaveUp++;

    public BotDeed Propose(IBotWilful bot)
    {
        if (!Running || !BotRelics.Running || !BotCrime.Running || BotRelics.Carrying == 0)
        {
            return null;
        }

        if (bot?.Self is not BotMobile { Deleted: false, Alive: true } body || !BotCrime.IsThief(body) || body.Skills.Stealing.Value < Bar)
        {
            return null;
        }

        var map = body.Map;

        if (map == null || map == Map.Internal || body.Tired || body.Squad != null || body.Criminal || BotOutlaw.Outlaw(body)
            || BotOutlaw.Jailed(body) || BotCrime.Posted(body))
        {
            return null;
        }

        var now = Core.TickCount;

        if (Spacing(now) || !BotCutpurse.Room())
        {
            return null;
        }

        var all = BotRelics.All;

        for (var i = 0; i < all.Count; i++)
        {
            var r = all[i];

            if (r.Place != BotRelicPlace.Carried || r.Carrier is not BotMobile { Deleted: false, Alive: true } carrier || carrier == body
                || carrier.Map != map || !body.InRange(carrier.Location, Reach) || BotGuilds.Same(body, carrier) || !body.CanBeHarmful(carrier, false)
                || BotOutlaw.Jailed(carrier))
            {
                continue;
            }

            Offered++;

            return new BotRelicTheft(r, carrier);
        }

        return null;
    }

    public static string Describe() =>
        !Running
            ? "off"
            : $"{Offered} relics offered to thieves at {Bar:F0} Stealing or better, {Tried} tries, {Stolen} stolen, {Missed} missed, {Seen} seen and reported, {GaveUp} carriers lost on the way";

    public static void Forget()
    {
        _tried = false;
        Offered = Tried = Stolen = Missed = Seen = GaveUp = 0;
    }
}

/// <summary>A thief after a relic in a carrier's pack: close on it, reach for it, and slip away.</summary>
public sealed class BotRelicTheft : BotDeed
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotRelicTheft));

    public const string Trade = "relic-theft";

    private readonly BotRelic _relic;

    private readonly BotMobile _carrier;

    private long _began;

    private bool _done;

    private bool _lifted;

    private string _outcome;

    private Point3D _away;

    private long _leaving;

    public BotRelicTheft(BotRelic relic, BotMobile carrier)
    {
        _relic = relic;
        _carrier = carrier;
    }

    public override string Kind => Trade;

    public override Map Map => _carrier.Map;

    public override Point3D Where => _carrier.Location;

    public override double Expects => BotRelicSnatch.Prior;

    public override double Minutes => 4.0;

    public override SkillName? Trains => SkillName.Stealing;

    /// <summary>
    /// Half what the fence asks for it: takings in their own right, as a lifted purse's goods are at the fence's share — and half,
    /// because a relic is sold only if some guild's chest can pay before the thief is caught.
    /// </summary>
    public override int Made => _lifted ? BotRelics.RelicRansom / 2 : 0;

    public override bool Committed => _done;

    public override string Stage =>
        _done ? $"slipping away with {_relic.Name}" : $"stalking {_carrier.Name} for {_relic.Name}";

    public override void Taken(IBotWilful bot) => _began = Core.TickCount;

    public override BotDoing Advance(IBotWilful bot)
    {
        if (bot?.Self is not BotMobile { Deleted: false, Alive: true } thief || thief.Map != _carrier.Map)
        {
            return BotDoing.Failed("no body");
        }

        var now = Core.TickCount;

        if (_done)
        {
            if (thief.InRange(_away, 2) || now - _leaving >= 20000)
            {
                return _lifted ? BotDoing.Done(_outcome) : BotDoing.Failed(_outcome);
            }

            return BotDoing.Walk(thief.Map, _away, BotArrival.Within(2), "slipping away");
        }

        if (_relic.Place != BotRelicPlace.Carried || _relic.Bot != _carrier.Serial || _carrier is not { Deleted: false, Alive: true })
        {
            return BotDoing.Failed($"{_relic.Name} is not in {_carrier.Name}'s pack any more");
        }

        if (!thief.InRange(_carrier.Location, 1))
        {
            if (now - _began >= BotRelicSnatch.CloseMs || !thief.InRange(_carrier.Location, BotRelicSnatch.Reach * 2))
            {
                BotRelicSnatch.Lose();

                return BotDoing.Failed($"{_carrier.Name} kept moving");
            }

            return BotDoing.Walk(thief.Map, _carrier, BotArrival.Beside, $"stalking {_carrier.Name} for {_relic.Name}");
        }

        if (!BotCutpurse.Room())
        {
            return BotDoing.Failed("the island is watchful tonight");
        }

        return Try(thief, now);
    }

    private BotDoing Try(BotMobile thief, long now)
    {
        BotRelicSnatch.Spend(thief);

        var skill = thief.Skills.Stealing.Value;
        var chance = Math.Clamp(skill / 100.0 * BotRelicSnatch.Odds, 0.0, Math.Clamp(BotRelicSnatch.Most, 0.0, 1.0));
        var lifted = Utility.RandomDouble() < chance;
        var seen = !lifted || Utility.RandomDouble() < BotRelicSnatch.SeenChance;
        var town = BotCrime.TownAt(_carrier.Location);
        var from = _relic.Guild;

        thief.CheckSkill(SkillName.Stealing, 0.0, 100.0);
        BotCrime.Crime(thief);

        if (lifted)
        {
            BotRelics.Steal(_relic, thief);
            BotRelicSnatch.Lifted();
            _lifted = true;

            BotRelics.Tell(
                $"{thief.Name} stole {_relic.Name} from {_carrier.Name}{(from != null ? $" of {from}" : "")}",
                "{Thief} the thief stole {Relic} from {Carrier}'s pack{From} near {Town} at Stealing {Skill:F1} (a {Chance:P0} chance){Seen}",
                thief.Name,
                _relic.Name,
                _carrier.Name,
                from != null ? $", carried for {from}" : "",
                town,
                skill,
                chance,
                seen ? ", and was seen" : ", unseen"
            );

            BotEvents.Post("crime", thief, $"stole {_relic.Name} from {_carrier.Name}{(seen ? "" : ", unseen")}");
            BotRelics.Remember(thief, "relic-theft", _carrier.Name, $"I lifted {_relic.Name} out of {_carrier.Name}'s own pack.", 50.0);
            BotRelics.Remember(_carrier, "relic-robbed", thief.Name, $"{thief.Name} stole {_relic.Name} out of my pack on the road home.", 48.0);
        }
        else
        {
            BotRelicSnatch.Miss();

            logger.Information("{Thief} the thief reached for {Relic} in {Carrier}'s pack near {Town} at Stealing {Skill:F1} and missed", thief.Name, _relic.Name, _carrier.Name, town, skill);
        }

        if (seen)
        {
            BotRelicSnatch.Saw();
            BotVoice.Say(_carrier, "cry", lifted ? $"Thief! {thief.Name} has taken {_relic.Name}!" : $"Thief! {thief.Name} reached for {_relic.Name}!", true);
            BotCrime.Report(thief, _carrier, town, lifted ? $"stealing {_relic.Name}" : $"reaching for {_relic.Name}", lifted ? BotRelicSnatch.Worth : 0, lifted ? 2 : 1);
        }

        _done = true;
        _leaving = now;
        _outcome = lifted ? $"stole {_relic.Name} from {_carrier.Name}" : $"reached for {_relic.Name} in {_carrier.Name}'s pack and missed";

        var dx = thief.X - _carrier.X;
        var dy = thief.Y - _carrier.Y;

        if (dx == 0 && dy == 0)
        {
            dx = Utility.RandomBool() ? 1 : -1;
            dy = Utility.RandomBool() ? 1 : -1;
        }

        var len = Math.Max(1.0, Math.Sqrt(dx * dx + dy * dy));
        var x = thief.X + (int)Math.Round(dx / len * 14);
        var y = thief.Y + (int)Math.Round(dy / len * 14);

        _away = new Point3D(x, y, thief.Map.GetAverageZ(x, y));

        return BotDoing.Walk(thief.Map, _away, BotArrival.Within(2), seen ? "running from the cry of thief" : "slipping away");
    }
}
