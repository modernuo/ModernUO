using System;
using Server.Logging;
using Server.Regions;

namespace Server.BotAI.V2;

/// <summary>
/// The keeper going to ground at the hideout while a price stands on its head.
///
/// <para>
/// <b>Patrick's rule of 17.09.2026 made the keeper worth 5000gp, and the island took him up on it.</b> A fence seen in
/// the company of thieves becomes a criminal and taking it is worth five thousand — the largest price on this shard by
/// a factor of five — so every hunter on the island goes for it the moment the Baron hears. On 18.09.2026 that came to
/// three catches in a four-hour session, all of them the fence, which is seventy-eight per cent of that session spent
/// in a cell by the one member of the band whose whole purpose is to mind its business. A keeper that is never at
/// large keeps nothing.
/// </para>
///
/// <para>
/// <b>So it does what a criminal with a price on its head does: it disappears until the price lapses.</b> Not hidden —
/// a Sage or an Architect has no Hiding worth the name, which is why <c>BotLieLow</c> is the thieves' answer and not
/// this one — but <em>away</em>: the hideout is chosen at two hundred and fifty to five hundred road steps from
/// anywhere anybody lives, patrols are raised within <c>BotOutlaw.Reach</c> of a murder, and nothing walks that far
/// looking for one bot. The price still stands and is still worth taking; a raid on the hideout still finds it there.
/// The keeper simply stops walking past the people who would collect.
/// </para>
///
/// <para>
/// The wait ends by itself when <c>BotOutlaw.WantedMs</c> runs out, which is what makes this work rather than a bot
/// parked for ever: half an hour of keeping the band's chest instead of an hour in a cell.
/// </para>
/// </summary>
public sealed class BotHoleUp : BotDeed
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotHoleUp));

    public const string Trade = "holeup";

    /// <summary>What going to ground is held at against other work, a minute: above the band's other errands, because nothing else the keeper could be doing survives a catch.</summary>
    public static double Prior { get; set; } = 500.0;

    /// <summary>Times the keeper went to ground.</summary>
    public static long Begun { get; private set; }

    /// <summary>Waits that ran to the end of the price.</summary>
    public static long Waited { get; private set; }

    /// <summary>Waits that ended in a cell anyway.</summary>
    public static long Taken { get; private set; }

    /// <summary>Waits that found no hideout to go to.</summary>
    public static long Homeless { get; private set; }

    /// <summary>Waits that went somewhere emptier than the hideout.</summary>
    public static long Elsewhere { get; private set; }

    /// <summary>How far from the hideout to look for empty ground.</summary>
    public static int Away { get; set; } = 120;

    /// <summary>
    /// The emptiest ground within reach: the hideout itself, or one of eight points a walk out from it.
    ///
    /// <para>
    /// <b>A patrol is raised out of whoever is standing near the wanted bot</b> — <c>BotManhunt.Reach</c>, sixty tiles — so
    /// going to ground means going where nobody is, and the hideout is not reliably that. On 18.09.2026 Hale was let
    /// out of its cell, made wanted the same second by a raid standing in the camp, sent here by build 139, and killed
    /// at the new hideout four minutes later by a posse of three: "Gerda finished manhunt: patrol against Hale — Hale
    /// is down", and it was caught the moment it rose. The hideout is where the band is, which is exactly why the law
    /// goes there.
    /// </para>
    ///
    /// <para>
    /// The hideout stays in the running and wins whenever it is quiet, which keeps the keeper by its own chest most of
    /// the time; it only loses when somebody else is standing there.
    /// </para>
    /// </summary>
    private static Point3D Quiet(Map map, Point3D lair)
    {
        var best = lair;
        var fewest = int.MaxValue;

        for (var i = 0; i <= 8; i++)
        {
            Point3D at;

            if (i == 0)
            {
                at = lair;
            }
            else
            {
                var angle = (i - 1) * Math.PI / 4.0;
                var x = lair.X + (int)Math.Round(Math.Cos(angle) * Math.Max(20, Away));
                var y = lair.Y + (int)Math.Round(Math.Sin(angle) * Math.Max(20, Away));

                if (!BotStep.Settle(map, x, y, out var z))
                {
                    continue;
                }

                at = new Point3D(x, y, z);

                if (Region.Find(at, map)?.GetRegion<GuardedRegion>() is { } guarded && !guarded.IsDisabled())
                {
                    continue;
                }
            }

            var many = 0;

            foreach (var other in map.GetMobilesInRange<BotMobile>(at, BotManhunt.Reach))
            {
                if (other is { Deleted: false, Alive: true } && !BotUnderworld.Member(other))
                {
                    many++;
                }
            }

            if (many >= fewest)
            {
                continue;
            }

            fewest = many;
            best = at;

            if (i > 0)
            {
                Elsewhere++;
            }
        }

        return best;
    }

    private readonly Map _map;

    private Point3D _at;

    private bool _counted;

    private bool _there;

    private bool _picked;

    public BotHoleUp(Map map, Point3D at)
    {
        _map = map;
        _at = at;
    }

    public override string Kind => Trade;

    public override bool Committed => true;

    public override bool Unpaid => true;

    public override bool Steadfast => true;

    /// <summary>Sitting still at the hideout is the whole of it, and it ends on the price's own clock.</summary>
    public override bool Still => _there;

    public override bool Repeats(BotDeed other) => other is BotHoleUp;

    public override Map Map => _map;

    public override Point3D Where => _at;

    public override double Expects => Prior;

    public override double Minutes => BotOutlaw.WantedMs / 60000.0 + 2.0;

    public override int Outlay => 0;

    public override double Coin => 0.0;

    public override int Made => 0;

    public override string Stage => _there ? "sitting out the price on its head" : "off to the hideout, wanted";

    public override BotDoing Advance(IBotWilful bot)
    {
        var body = bot?.Self;

        if (body == null || _map == null || body.Map != _map)
        {
            return BotDoing.Failed("no body");
        }

        if (!_counted)
        {
            _counted = true;
            Begun++;
        }

        if (BotOutlaw.Jailed(body))
        {
            Taken++;

            return BotDoing.Done("taken anyway");
        }

        // The price lapsed on its own clock, or the band lost its fence: either way there is nothing to sit out.
        if (!BotOutlaw.IsWanted(body) && !BotOutlaw.IsRed(body))
        {
            if (_there)
            {
                Waited++;
            }

            return BotDoing.Done("the price has lapsed");
        }

        if (BotUnderworld.Hideout == Point3D.Zero)
        {
            Homeless++;

            return BotDoing.Failed("the band has no hideout");
        }

        if (!_picked)
        {
            _picked = true;
            _at = Quiet(_map, BotUnderworld.Hideout);
        }

        if (!body.InRange(_at, 3))
        {
            _there = false;

            // Beside the chest rather than onto it, as every other walk to this place is. See BotLair.Doorstep.
            var step = BotLair.Doorstep();

            return BotDoing.Walk(_map, step == Point3D.Zero ? _at : step, BotArrival.Within(2), "to ground at the hideout");
        }

        if (!_there)
        {
            _there = true;

            logger.Information(
                "{Name} the fence has gone to ground at the hideout with {Price}gp on its head",
                body.Name,
                BotFence.HeadPrice
            );
        }

        return BotDoing.Work("sitting out the price on its head");
    }

    public static string Describe() =>
        $"{Begun} times the keeper went to ground: {Waited} sat the price out, {Taken} were taken anyway, {Homeless} had nowhere to go, {Elsewhere} went somewhere emptier than the hideout";

    public static void Forget()
    {
        Begun = 0;
        Waited = 0;
        Taken = 0;
        Homeless = 0;
        Elsewhere = 0;
    }
}
