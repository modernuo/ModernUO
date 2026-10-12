using System;
using Server.Logging;

namespace Server.BotAI.V2;

/// <summary>
/// A thief that has thought of robbery and found nobody to rob, going to where the work is and waiting there.
///
/// <para>
/// <b>The thought had nowhere to go, and that is the whole of why this island has no robberies.</b> On 18.09.2026 the
/// roll read "193 rolls for the thought of robbery at 2.0% every 10 min, 7 came up, 4 sent to practise hiding first,
/// <b>3 found nobody alone with 50gp outside a town</b>, 0 pressed into a robbery". Every one of the three was a
/// thief able to rob: one of them, Pell, was standing in the middle of Britain when the thought came, and the other
/// two were alone in the wilds where they hunt. A temptation that comes once in ten minutes and dies on the spot
/// because of where the bot happened to be standing is a temptation that never happens at all — and the rarity is
/// Patrick's number, deliberately set (see <c>BotRob.Chance</c>), so the answer cannot be to make it commoner.
/// </para>
///
/// <para>
/// <b>So the thief does what a robber does: it goes to the road and waits.</b> The island already knows where its
/// people are — <c>BotCommons</c> keeps what pays where, measured by the population itself, and those places are
/// exactly where lone bots with coin in their packs go all day. The nearest one outside a town and beyond
/// <c>BotRob.FromTown</c> of a ward is where it lies in wait, hidden, for <see cref="WaitMs"/>; the first mark that
/// comes within reach is set on through the ordinary machinery, and if none comes the thought lapses as it would have
/// anyway.
/// </para>
/// </summary>
public sealed class BotWaylay : BotDeed
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotWaylay));

    public const string Trade = "waylay";

    /// <summary>Whether a thief that finds nobody goes to wait at all.</summary>
    public static bool Running { get; set; } = true;

    /// <summary>What lying in wait is claimed at, a minute: just under the robbery's own, because it is the robbery's first half.</summary>
    public static double Prior { get; set; } = 380.0;

    /// <summary>How long it waits before the thought lapses. Five minutes.</summary>
    public static int WaitMs { get; set; } = 300000;

    /// <summary>How many failed tries at hiding before it waits in the open instead.</summary>
    public static int HideTries { get; set; } = 4;

    /// <summary>Waits begun.</summary>
    public static long Begun { get; private set; }

    /// <summary>Waits that saw a mark and set on it.</summary>
    public static long Sprung { get; private set; }

    /// <summary>Waits that ran out with nobody coming.</summary>
    public static long Barren { get; private set; }

    /// <summary>
    /// How long a thief that has seen its mark holds absolutely still, waiting for the setting-on.
    ///
    /// <para>
    /// <b>This is the second between the wait and the blow, and until build 165 the whole ambush died in it.</b> The
    /// wait ended <c>Done</c> the moment a mark came by, so the bot took up whatever it had put down — "took up
    /// acquire again after waylay, 6s after putting it down" — walked a step towards a shop and was revealed, and the
    /// robbery that arrived on the outlaws' next beat found a thief standing in plain sight. `0 sprung on a mark that
    /// walked into the wait` on build 163, out of a wait that had seen one. So the wait does not end: it holds,
    /// hidden and motionless, and lets the press replace it.
    /// </para>
    /// </summary>
    public static int HoldMs { get; set; } = 5000;

    /// <summary>Waits that saw a mark, held still, and were never set on. A press that did not come.</summary>
    public static long Lapsed { get; private set; }

    /// <summary>Waits that could not hide and lay in the open.</summary>
    public static long Open { get; private set; }

    private readonly Map _map;

    private readonly Point3D _at;

    private bool _counted;

    private bool _there;

    private bool _hid;

    private int _tries;

    private long _since;

    /// <summary>When this wait saw its mark, or 0 while it is still waiting. See <see cref="HoldMs"/>.</summary>
    private long _saw;

    public BotWaylay(Map map, Point3D at)
    {
        _map = map;
        _at = at;
    }

    public override string Kind => Trade;

    public override bool Committed => true;

    public override bool Unpaid => true;

    public override bool Steadfast => true;

    /// <summary>Hidden and still is what waiting looks like, and it ends on its own clock.</summary>
    public override bool Still => _there;

    public override bool Repeats(BotDeed other) => other is BotWaylay;

    public override Map Map => _map;

    public override Point3D Where => _at;

    public override double Expects => Prior;

    public override double Minutes => WaitMs / 60000.0 + 2.0;

    public override int Outlay => 0;

    public override double Coin => 0.0;

    public override int Made => 0;

    public override string Stage => _there ? "lying in wait for a mark" : "off to where the work is, to lie in wait";

    public override BotDoing Advance(IBotWilful bot)
    {
        var body = bot?.Self;

        if (body is not BotMobile thief || _map == null || body.Map != _map)
        {
            return BotDoing.Failed("no body");
        }

        if (!_counted)
        {
            _counted = true;
            Begun++;
        }

        if (BotOutlaw.Jailed(thief))
        {
            return BotDoing.Done("taken before it could rob");
        }

        var now = Core.TickCount;

        if (!_there)
        {
            if (!body.InRange(_at, 3))
            {
                return BotDoing.Walk(_map, _at, BotArrival.Within(3), "to where the work is");
            }

            _there = true;
            _since = now;
        }

        // Hidden if it can be: a robber that stands on the road in plain sight is a robber everybody walks round.
        if (!_hid && BotShadow.Running)
        {
            if (!BotShadow.Ready(body))
            {
                return BotDoing.Work("catching its breath");
            }

            if (BotShadow.Hide(body))
            {
                _hid = true;
            }
            else if (++_tries >= HideTries)
            {
                Open++;
                _hid = true;
            }
            else
            {
                return BotDoing.Work("trying to hide");
            }
        }

        // Seen one already, and now holding still for it. Nothing is searched for again: the mark this wait saw is
        // the mark the robbery sets on, and moving or looking about is how the thief stops being hidden.
        if (_saw != 0)
        {
            if (now - _saw < HoldMs)
            {
                return BotDoing.Work("a mark has come, and the blow is a beat away");
            }

            Lapsed++;

            return BotDoing.Done("a mark came by and nothing was made of it");
        }

        // The same search the temptation makes, from where it now stands. The setting-on itself is left to the
        // outlaws' own clock rather than done from inside this: pressing a deed from within a deed's own Advance
        // would replace the work this answer is about to be applied to.
        if (BotRobber.Mark(thief, _map) is { } mark)
        {
            Sprung++;
            _saw = now;
            BotRobber.Spring(thief, mark);

            // <b>Where the thief is standing, not where it was sent to wait.</b> The old line printed the waiting
            // place, and on 18.09.2026 that cost an hour of reasoning from a number that was never about the thief:
            // a mark "in reach of its wait" looked 180 tiles away because the thief had drifted from the tile named.
            logger.Information(
                "{Name} sees {Mark} come by, {Away} tiles off, and holds still at ({X}, {Y})",
                body.Name,
                mark.Name,
                (int)body.GetDistanceToSqrt(mark),
                body.X,
                body.Y
            );

            return BotDoing.Work("a mark has come, and the blow is a beat away");
        }

        if (now - _since >= WaitMs)
        {
            Barren++;

            if (body.Hidden)
            {
                body.RevealingAction();
            }

            return BotDoing.Done($"waited {WaitMs / 60000} minutes and nobody came");
        }

        return BotDoing.Work("lying in wait");
    }

    public static string Describe() =>
        $"{Begun} waits by the road: {Sprung} saw a mark ({Lapsed} of them never set on), {Barren} ran out with nobody coming, {Open} could not hide and waited in the open";

    public static void Forget()
    {
        Begun = 0;
        Sprung = 0;
        Barren = 0;
        Lapsed = 0;
        Open = 0;
    }
}
