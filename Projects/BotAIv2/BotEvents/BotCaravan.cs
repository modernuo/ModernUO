using System;
using System.Collections.Generic;
using Server.Mobiles;

namespace Server.BotAI.V2;

/// <summary>
/// Offers a trader near a posted caravan's source the lead (07.10.2026): a trader of the neutral class, fit, in the world, within
/// <see cref="BotCaravans.Reach"/> of the source's counter, able to pay for the goods out of what it may spend, with a way there.
/// </summary>
public sealed class BotCaravanCall : IBotProposer
{
    public string Name => "Caravan";

    public BotStanding Rung => BotStanding.Free;

    public BotDeed Propose(IBotWilful bot)
    {
        if (!BotSeasons.Running || !BotCaravans.Running || BotCaravans.Now is not { Phase: BotCaravans.Phase.Posted } run)
        {
            return null;
        }

        var body = bot?.Self;

        if (body is not BotMobile { Alive: true } || !BotTrading.Trader(body) || body.Map != run.Source.Map
            || body.HitsMax <= 0 || body.Hits < body.HitsMax * BotHunter.FitAt || !body.InRange(run.Source.Location, BotCaravans.Reach))
        {
            return null;
        }

        if (BotYield.Spendable(body) < run.Units * run.Price)
        {
            return null;
        }

        var map = body.Map;

        if (!BotGates.Joined(map, body.Location, run.Source.Location)
            || BotReach.Ask(map, body.Location, run.Source.Location, BotArrival.Within(BotShops.CounterReach)) == BotReachVerdict.Sealed)
        {
            return null;
        }

        BotCaravans.OfferedLead();

        return new BotCaravan(run);
    }
}

/// <summary>
/// Leading a caravan: to the source's counter, the goods bought with the trader's own coin, a wait for the guards, the road to the
/// town that posted it, and the town's pay on arrival (<see cref="BotCaravans.Deliver"/>). Held through ordinary offers like any
/// work a bot sees through.
/// </summary>
public sealed class BotCaravan : BotDeed
{
    public const string Trade = "caravan";

    private readonly BotCaravans.Run _run;

    private int _paid;

    public BotCaravan(BotCaravans.Run run) => _run = run;

    public override string Kind => Trade;

    /// <summary>What the caravan carries, for the road's bandits (<c>BotHighway</c>, life18a): null until the goods are bought.</summary>
    public Type Goods => _run.Phase is BotCaravans.Phase.Gathering or BotCaravans.Phase.Road ? _run.Kind : null;

    public override Map Map => _run.Source.Map;

    public override Point3D Where => _run.Phase == BotCaravans.Phase.Road ? _run.To.Square : _run.Source.Location;

    public override double Expects => Math.Max(BotCaravans.Prior, _run.Premium / Math.Max(1.0, BotCaravans.WorkMinutes));

    public override double Minutes => BotCaravans.WorkMinutes;

    public override int Outlay => _run.Units * _run.Price;

    public override bool AtCounter => true;

    public override bool Steadfast => true;

    public override string Stage =>
        _run.Phase switch
        {
            BotCaravans.Phase.Buying => $"buying {_run.Units} {_run.Kind.Name} at {_run.Source.Name}'s counter for {_run.To.Name}'s caravan",
            BotCaravans.Phase.Gathering => $"waiting for guards at {_run.Source.Name}'s counter ({_run.Guards.Count} of {BotCaravans.MostGuards})",
            BotCaravans.Phase.Road => $"leading the caravan to {_run.To.Name} with {_run.Guards.Count} guards",
            _ => $"the caravan to {_run.To.Name}"
        };

    public override void Taken(IBotWilful bot) => BotCaravans.Lead(_run, bot?.Self);

    public override BotDoing Advance(IBotWilful bot)
    {
        var body = bot?.Self;

        if (body == null)
        {
            return BotDoing.Failed("no body");
        }

        if (_run.Over)
        {
            return _paid > 0
                ? BotDoing.Done($"delivered the caravan to {_run.To.Name} for {_paid}gp")
                : BotDoing.Failed($"the caravan to {_run.To.Name} is over: {_run.Ending ?? "withdrawn"}");
        }

        if (!ReferenceEquals(_run.Leader, body))
        {
            return BotDoing.Failed("another trader leads this caravan");
        }

        switch (_run.Phase)
        {
            case BotCaravans.Phase.Buying:
                {
                    if (!body.InRange(_run.Source.Location, BotShops.CounterReach))
                    {
                        return BotDoing.Walk(_run.Source.Map, _run.Source.Location, BotArrival.Within(BotShops.CounterReach), $"to {_run.Source.Name}'s counter for the caravan's goods");
                    }

                    var bought = BotShops.Buy(bot, _run.Source, _run.Kind, _run.Units, out var refused);

                    if (bought <= 0)
                    {
                        BotCaravans.Unbuyable(_run, refused);

                        return BotDoing.Failed($"could not buy the caravan's {_run.Kind.Name}: {refused}");
                    }

                    BotCaravans.Bought(_run, bought);

                    return BotDoing.Work($"bought {bought} {_run.Kind.Name}; waiting for guards");
                }
            case BotCaravans.Phase.Gathering:
                {
                    if (_run.Guards.Count < BotCaravans.MostGuards && Core.TickCount - _run.GatherTick < BotCaravans.GatherMs)
                    {
                        return BotDoing.Work($"waiting for guards at {_run.Source.Name}'s counter ({_run.Guards.Count} of {BotCaravans.MostGuards})");
                    }

                    BotCaravans.Setting(_run);

                    return Road(body);
                }
            case BotCaravans.Phase.Road:
                {
                    return Road(body);
                }
        }

        return BotDoing.Failed("the caravan is in no state to lead");
    }

    private BotDoing Road(Mobile body)
    {
        if (body.InRange(_run.To.Square, BotCaravans.ArriveReach))
        {
            _paid = BotCaravans.Deliver(_run, body);

            return BotDoing.Done($"delivered the caravan to {_run.To.Name} for {_paid}gp");
        }

        // A caravan goes at its guards' pace: the walk is held where it stands while a guard has dropped back (12.10.2026, w7).
        // The order is the same either way, so the Will keeps the road it has. See BotCaravans.GuardSpread.
        BotCaravans.WaitForGuards(_run, body);

        return BotDoing.Walk(_run.Source.Map, _run.To.Square, BotArrival.Within(BotCaravans.ArriveReach), $"leading the caravan to {_run.To.Name}");
    }

    public override void Drop(IBotWilful bot)
    {
        if (!_run.Over && ReferenceEquals(_run.Leader, bot?.Self))
        {
            BotCaravans.Dropped(_run, bot?.Self, "the work was put down");
        }
    }
}

/// <summary>
/// Offers a fighter near a caravan's trader a guard's place (07.10.2026): while the trader buys or waits, until
/// <see cref="BotCaravans.MostGuards"/> are hired; a fighter in no company, within <see cref="BotCaravans.GuardReach"/> of the trader.
/// </summary>
public sealed class BotGuardCall : IBotProposer
{
    public string Name => "Guard";

    public BotStanding Rung => BotStanding.Free;

    public BotDeed Propose(IBotWilful bot)
    {
        if (!BotSeasons.Running || !BotCaravans.Running
            || BotCaravans.Now is not { Phase: BotCaravans.Phase.Buying or BotCaravans.Phase.Gathering, Leader: { } leader } run
            || run.Guards.Count >= BotCaravans.MostGuards)
        {
            return null;
        }

        var body = bot?.Self;

        if (!BotSeasons.Fighter(body) || bot is not IBotSquadMember { Squad: null } || body.Map != leader.Map
            || !body.InRange(leader.Location, BotCaravans.GuardReach) || run.Guards.Contains(body))
        {
            return null;
        }

        BotCaravans.OfferedGuard();

        return new BotGuard(run, bot.Bond?.Weapon?.Skill ?? SkillName.Wrestling);
    }
}

/// <summary>
/// Guarding a caravan: walking with its trader and setting on whatever attacks it — one creature at a time, the shard's own fight
/// (<see cref="BotSlay"/>) — when the guards between them bring <see cref="BotCaravans.GuardOdds"/> times what it is reckoned at.
/// Paid by the town on arrival, if still near the trader then; a guard that falls <see cref="BotCaravans.Behind"/> tiles back for
/// <see cref="BotCaravans.BehindMs"/> has left the caravan.
/// </summary>
public sealed class BotGuard : BotDeed
{
    public const string Trade = "guard";

    private readonly BotCaravans.Run _run;

    private readonly SkillName _trains;

    private BotSlay _slay;

    private Mobile _target;

    private long _behindTick;

    public BotGuard(BotCaravans.Run run, SkillName trains)
    {
        _run = run;
        _trains = trains;
    }

    public override string Kind => Trade;

    public override Map Map => _run.Leader?.Map ?? _run.Source.Map;

    public override Point3D Where => _run.Leader?.Location ?? _run.Source.Location;

    /// <summary>life18a: the caravan's own wage over the measured trip and the wait (was the 100gp floor over fifteen minutes).</summary>
    public override double Expects =>
        Math.Max(BotCaravans.GuardPrior, Math.Max(BotCaravans.Wage, _run.GuardWage) * BotNight.Pay / Math.Max(1.0, Minutes));

    public override double Minutes => BotCaravans.Trip + BotCaravans.GatherMs / 60000.0 * 0.5;

    public override bool Braves => true;

    public override bool Alongside => true;

    public override bool Steadfast => true;

    public override Mobile Foe => _slay?.Foe;

    public override string Stage => $"guarding {_run.Leader?.Name}'s caravan to {_run.To.Name}";

    public override void Taken(IBotWilful bot) => BotCaravans.Hired(_run, bot?.Self);

    public override BotDoing Advance(IBotWilful bot)
    {
        var body = bot?.Self;

        if (body == null)
        {
            return BotDoing.Failed("no body");
        }

        if (_run.Over)
        {
            Release(bot);

            // life18a: the trader arrived while this guard was still on the road; its wage is kept until it reaches the square.
            if (_run.Ending == null && BotCaravans.Owes(_run, body))
            {
                if (body.Map == _run.Source.Map && body.InRange(_run.To.Square, BotCaravans.PayReach))
                {
                    var paid = BotCaravans.PayLate(_run, body);

                    return BotDoing.Done($"guarded the caravan to {_run.To.Name}, paid {paid}gp on reaching it");
                }

                return BotDoing.Walk(_run.Source.Map, _run.To.Square, BotArrival.Within(BotCaravans.PayReach - 4), $"after {_run.Leader?.Name}'s caravan to {_run.To.Name}, paid on arrival");
            }

            return _run.Ending == null
                ? BotDoing.Done($"guarded the caravan to {_run.To.Name}")
                : BotDoing.Failed($"the caravan to {_run.To.Name} was lost: {_run.Ending}");
        }

        var leader = _run.Leader;

        if (leader is not { Deleted: false, Alive: true })
        {
            Release(bot);

            return BotDoing.Failed("the caravan's trader fell");
        }

        if (_slay != null)
        {
            var fighting = _slay.Advance(bot);

            if (fighting.Kind is not (BotDoingKind.Done or BotDoingKind.Failed))
            {
                return fighting;
            }

            _slay.Drop(bot);
            _slay = null;
            _target = null;
        }

        if (Attacker(body, leader) is { } foe)
        {
            BotCaravans.Defend();
            _target = foe;
            _slay = new BotSlay(foe, _trains);
            _slay.Taken(bot);

            return _slay.Advance(bot);
        }

        var now = Core.TickCount;

        if (_run.Phase == BotCaravans.Phase.Road && (body.Map != leader.Map || !body.InRange(leader.Location, BotCaravans.Behind)))
        {
            if (_behindTick == 0)
            {
                _behindTick = now;
            }
            else if (now - _behindTick >= BotCaravans.BehindMs)
            {
                BotCaravans.Left(_run, body, true);

                return BotDoing.Failed($"fell behind {leader.Name}'s caravan");
            }
        }
        else
        {
            _behindTick = 0;
        }

        // life18a: at the counter the trader stands still inside a shop; a guard near enough waits where it is rather than asking the
        // walker for a tile beside it (one of the night's two guards dropped the post on "could not get one tile closer").
        var slack = _run.Phase == BotCaravans.Phase.Road ? 3 : 8;

        if (!body.InRange(leader.Location, slack))
        {
            // <b>Kept up with (12.10.2026, w7).</b> 100 of the 104 guards that failed on 11–12.10 ended "could not get nearer to" their
            // trader, four to fifteen tiles behind it on the road north through Britain (x 1466–1490, z 20–30), every plan but one
            // reaching it; Ingram was hired and lost five times in nineteen seconds behind Ilphrin. A plan that reaches the trader is a way
            // (BotDoing.WalkAfter); Behind and BehindMs end a guard that cannot keep up, and the trader waits for its guards (BotCaravan).
            return BotDoing.WalkAfter(leader.Map, leader, BotArrival.Within(slack), $"guarding {leader.Name}'s caravan to {_run.To.Name}");
        }

        return BotDoing.Work($"guarding {leader.Name}'s caravan to {_run.To.Name}");
    }

    /// <summary>
    /// The strongest creature attacking the trader or this guard, near, lawful, and within the guards' odds; or null. Read off the
    /// two bodies' own lists of who has struck them, not off the map: no search round anybody on a guard's beat.
    /// </summary>
    private BaseCreature Attacker(Mobile body, Mobile leader)
    {
        BaseCreature best = null;
        var most = 0.0;
        var bring = BotCaravans.Guarding(_run) / Math.Max(1.0, BotCaravans.GuardOdds);

        Weigh(body, leader, leader.Aggressors, bring, ref best, ref most);
        Weigh(body, leader, body.Aggressors, bring, ref best, ref most);

        return best;
    }

    private static void Weigh(Mobile body, Mobile leader, List<AggressorInfo> struck, double bring, ref BaseCreature best, ref double most)
    {
        if (struck == null)
        {
            return;
        }

        for (var i = 0; i < struck.Count; i++)
        {
            if (struck[i]?.Attacker is not BaseCreature { Deleted: false, Alive: true, Controlled: false } m || m.Map != leader.Map
                || !m.InRange(leader.Location, BotCaravans.DefendReach))
            {
                continue;
            }

            var reckon = BotSquad.Reckon(m);

            if (reckon <= most || reckon > bring || !body.CanBeHarmful(m, false) || body.IsHarmfulCriminal(m))
            {
                continue;
            }

            most = reckon;
            best = m;
        }
    }

    private void Release(IBotWilful bot)
    {
        _slay?.Drop(bot);
        _slay = null;
        _target = null;
    }

    public override void Drop(IBotWilful bot)
    {
        Release(bot);

        if (!_run.Over)
        {
            BotCaravans.Left(_run, bot?.Self, false);
        }
        else
        {
            BotCaravans.Forfeit(_run, bot?.Self);
        }
    }
}
