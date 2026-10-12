using System;
using System.Collections.Generic;
using Server.Logging;

namespace Server.BotAI.V2;

/// <summary>
/// Walking back to where the population lives, when there is nothing else to do and the bot is a long way
/// from it.
///
/// <para>
/// <b>Thirty-four proposers and not one of them ever said "come back".</b> Everything this shard offers is
/// offered relative to where a bot already stands: a seam near it, a shopkeeper near it, something worth
/// fighting near it. So a bot that wanders — after quarry, after a shop, behind a company — ends up
/// somewhere with none of those, is offered nothing, and stands there. Nothing in the world is wrong with
/// it and nothing will ever fetch it.
/// </para>
///
/// <para>
/// <b>Measured 02.09.2026.</b> Six casters — Perri, Quill, Bryn, Edda, Faron and Doran, every one of them
/// newly raised and holding between nought and thirty gold — stood together at (1397, 1822), three hundred
/// and fifty tiles south of the camp at (1440, 1470), holding no work at all. Four of them had been there
/// long enough for the shard's own stall detector to complain, one for sixteen minutes. They were not
/// stuck, not hurt and not out of stamina: they were simply somewhere that offers nothing to a bot with no
/// money, and there was no errand in the world whose answer was "then go home".
/// </para>
///
/// <para>
/// <b>It does not need to beat anything and must not.</b> The worth below is a few coins a minute, so any
/// real work — a dig, a sale, a fight, a lesson — wins the auction against it every time. This is what a
/// bot does when the answer to "what is worth doing here" is nothing, and the cure for that is to be
/// somewhere else.
/// </para>
/// </summary>
public sealed class BotHomeward : BotDeed
{
    public const string Trade = "homeward";

    /// <summary>
    /// How far from the camp a bot has to be before going back is worth offering at all.
    ///
    /// Inside this it is already among the shops, the seams and the counters, and walking to the middle of
    /// them would be an errand that changes nothing.
    /// </summary>
    public static int Away { get; set; } = 120;

    /// <summary>Near enough to be home. The camp is a place, not a tile — see BotArrival.Within.</summary>
    public static int Arrived { get; set; } = 20;

    /// <summary>
    /// How long a bot must have had nothing worth doing before the walk home is offered at all.
    ///
    /// <para>
    /// <b>Half a minute, and it was nought for the first hour — which made this an errand that mostly
    /// interrupted itself.</b> Offered on the very first barren review, the walk was taken the instant a bot
    /// ran out of work, and real work turned up a few seconds later and took the bot straight back off it.
    /// Measured 02.09.2026 over five minutes: 43 walks home begun, 28 of them dropped and 15 finished. Two
    /// thirds were bots that had not actually run out of anything; they had simply not been offered it yet.
    /// </para>
    ///
    /// <para>
    /// The cure is not to make the walk worth more — it must go on losing to everything — but to ask it
    /// later. A bot that has had nothing for thirty seconds has been offered every trade on the shard
    /// twice over and been refused by all of them, and that is a bot with nothing to do rather than a bot
    /// between jobs. The shard already keeps that clock: see BotUrges.BarrenMinutes.
    /// </para>
    /// </summary>
    public static double BarrenFor { get; set; } = 0.5;

    /// <summary>
    /// What this is worth a minute. Deliberately tiny.
    ///
    /// <para>
    /// It is not nought, and that distinction is the whole of the design. At nought the auction would score
    /// it at nought and a bot with nothing to do would go on having nothing to do, which is the state this
    /// exists to end. At a few coins it loses to every real offer on the shard and beats only silence.
    /// </para>
    /// </summary>
    public static double Worth { get; set; } = 5.0;

    private readonly Map _map;

    private readonly Point3D _home;

    /// <summary>The recall that may carry a mage home instead of the walk. See <see cref="BotRecall"/>.</summary>
    private BotRecall _recall;

    public BotHomeward(Map map, Point3D home)
    {
        _map = map;
        _home = home;
    }

    public override string Kind => Trade;

    public override Map Map => _map;

    public override Point3D Where => _home;

    public override double Expects => Worth;

    /// <summary>
    /// Not about money, and it must not be refused for failing to earn any.
    ///
    /// <para>
    /// <b>The second deed on this shard to need this flag, and it was found the same way as the first.</b>
    /// The Baron's rounds pay nothing by design, the ledger measured that honestly, and the arithmetic threw
    /// them away — twelve minutes standing in Britain with 600gp in his pack. This is the same thing at
    /// population scale: at 03:20 on 09.09.2026 Argus was asked who was idle and every one of them gave the
    /// same answer — <i>"39 proposers asked, 2 offered work, and it was refused: homeward is expected to pay
    /// -25.7/min here, against a claim of 1.2"</i> — while standing in Britain, where
    /// <c>BotThreat.Hostile</c> answers false for anybody inside a town region and so there is genuinely
    /// nothing to fight, nothing to hunt, and almost nothing to do.
    /// </para>
    ///
    /// <para>
    /// So the one errand whose whole purpose is to cure "there is nothing to do here" was refused for being
    /// unprofitable, by a shard that had learned — correctly — that walking home earns nothing and costs
    /// upkeep on the way. It is measured right and priced wrong, which is precisely what this flag is for.
    /// The shard's own count of it: 416 times nothing was worth doing, in eighty minutes.
    /// </para>
    /// </summary>
    public override bool Unpaid => true;

    /// <summary>
    /// How long the walk will take, honestly.
    ///
    /// <para>
    /// <b>This number is what keeps the appraisal from vetoing the one errand written for distant bots.</b>
    /// The auction weighs an offer by <c>work / (work + travel)</c> — the share of the time that is spent
    /// working rather than walking — so a deed that claims to take a moment and is four hundred tiles away
    /// scores near nought, every time, by construction. That is correct for a dig and it would be fatal
    /// here: the further a bot has strayed the more it needs this and the less it would be offered it.
    /// Declaring the walk as the work is not a way round the rule, it is the truth: the walk IS the errand,
    /// and told that, the arithmetic gives it a fair hearing without a single exception being carved.
    /// </para>
    /// </summary>
    public override double Minutes =>
        Math.Max(0.2, Tiles() * BotWalk.StepDelayMs(BotMobile.Runs) / 60000.0);

    private double Tiles() =>
        _map == null ? 0.0 : Math.Sqrt(Math.Pow(_home.X - _at.X, 2) + Math.Pow(_home.Y - _at.Y, 2));

    private Point3D _at;

    public override BotDoing Advance(IBotWilful bot)
    {
        var body = bot?.Self;

        if (body == null || _map == null || body.Map != _map)
        {
            return BotDoing.Failed("nowhere to go home to");
        }

        _at = body.Location;

        if (Utility.InRange(body.Location, _home, Arrived))
        {
            return BotDoing.Done("home");
        }

        // Runes (29.09.2026): a mage holding a rune for home recalls instead of walking the rest; a failed cast walks. See BotRecall.
        if (BotRecall.Instead(bot, _map, _home, Arrived, "home", ref _recall) is { } cast)
        {
            return cast;
        }

        return BotDoing.Walk(_map, _home, BotArrival.Within(Arrived), "going home");
    }

    public override string Stage => "going home";

    /// <summary>
    /// The road home was refused. Underground, that is a fact about where the bot stands, and it is written there so the
    /// next bot standing in the same pocket is brought up rather than sent walking. The errand still fails: the walk was
    /// tried and did not exist. See <see cref="BotHomer.Sealed"/>.
    /// </summary>
    public override bool Bend(IBotWilful bot)
    {
        if (bot?.Self is { } body && body.Map == _map && BotDungeon.Under(body.Location) && !BotDelveParty.Delving(body))
        {
            BotHomer.Sealed(body.Map, body.Location);
        }

        return false;
    }
}

/// <summary>
/// Offers the walk home, and only to a bot that is a long way from it.
///
/// <para>
/// On the <c>Free</c> rung, like every other piece of ordinary work: a bot that is bleeding, being hit or
/// marching with a company has better things to do than travel, and all three of those are decided below
/// this rung and never reach here.
/// </para>
/// </summary>
public sealed class BotHomer : IBotProposer
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotHomer));

    /// <summary>Bots asked, and what became of the question. Each case counted apart.</summary>
    public static long Asked { get; private set; }

    public static long Near { get; private set; }

    /// <summary>Far from home, but not yet out of work for long enough to be worth fetching.</summary>
    public static long Busy { get; private set; }

    public static long Sent { get; private set; }

    public string Name => "Homeward";

    public BotStanding Rung => BotStanding.Free;

    public BotDeed Propose(IBotWilful bot)
    {
        var body = bot?.Self;
        var home = BotPopulation.Home;

        if (body == null || home == null || home == Map.Internal || body.Map != home || !body.Alive)
        {
            return null;
        }

        Asked++;

        // Home is the guild's hall or seat when it has one, and the population's one home otherwise. See
        // BotSeat.Home: a bot of a guild seated in the south that walked home to the graveyard would be
        // walking to somebody else's home. And since 29.09.2026 the town the bot lives in (BotPopulation.HomeOf).
        var hearth = BotPopulation.HomeOf(body);

        if (Utility.InRange(body.Location, hearth, BotHomeward.Away))
        {
            Near++;

            return null;
        }

        // Not on the first empty review. See BotHomeward.BarrenFor: offered at once, this errand spent two
        // thirds of its life being dropped for work that was about to be offered anyway.
        //
        // <b>Except underground with no party and nothing hostile in sight, which is brought up at once.</b> A loose
        // delver is never barren: something down there always wants hitting back, and every errand offered to it is one
        // it cannot finish. Isolde of The Crown fought orcs alone in the Orc Caves (5313, 1307) from before 09:54 until
        // after 10:18 on 24.09.2026, taking guild supply runs she could not carry home in between. The quiet moment is
        // the one to bring her up in; a fight still going is left to end, as before.
        var barren = bot.Resolve?.Urges?.BarrenMinutes(Core.TickCount) >= BotHomeward.BarrenFor;

        if (!barren && !(body is BotMobile loose && BotDungeon.Under(body.Location) && !BotDelveParty.Delving(body)
                         && BotThreat.Decide(loose, BotMobile.NoticeRange) == BotStand.Nothing))
        {
            Busy++;

            return null;
        }

        // <b>Underground with no party, there is no walk home to offer.</b> The block the dungeons are cut into has no
        // road to the island (see BotPopulation.Carry), and a party brings its own back up when it ends. But parties are
        // not saved: a bot saved in a cave comes back in the cave with nobody to bring it up, and a member its company let
        // go underground is in the same place for as long as that delve runs. In the session of 21:58 on 21.09.2026
        // (build 176) eight such bots failed this errand 38 times, "no way through to (1440, 1470, 0)", Ysolt first forty
        // seconds after the world loaded and eight times in all — asked again on every empty review, since the walk is
        // unpaid and nothing is learnt from failing it. Brought up the way it was taken down, at exactly the moment it
        // would have been sent walking: far from home and out of work, so a loose delver still fighting beside its old
        // party is left to it.
        // <b>Or, since 29.09.2026, the walk out by the mouth it came in by.</b> Where the gates join the cave to the island
        // the walk home is offered like any other and the journey draws it through the gate (BotGates); only a cave with no
        // way out is still climbed out of by the shard's hand.
        // <b>Or where a walk home from about here has already been refused (06.10.2026, work15).</b> The gates' map of lands
        // said the cave joins the island, and the pathfinder said every time that the pocket the bot stands in does not:
        // on 04.10 homeward failed 556 times "no way through", every one dropped "there is no way from here", 183 of them
        // by bots in the dungeon block walking to (1881, 2705, 20); on 06.10 every one of the walks home refused in the
        // first hour started at (5318-5346, 1357-1379), the Orc Caves. The failure is at the start of the road, and it was
        // filed against the end of it, so each bot standing there failed it again on its next empty review.
        var at = body.Location;

        if (body is BotMobile below && BotDungeon.Under(body.Location) && !BotDelveParty.Delving(body)
            && (!BotGates.Joined(body.Map, body.Location, hearth) || WalledIn(body.Map, body.Location)))
        {
            if (BotPopulation.Carry(below))
            {
                below.Journey?.Finish();
                Surfaced++;

                if (BotGates.Joined(below.Map, at, hearth))
                {
                    SurfacedSealed++;
                }

                logger.Information(
                    "{Name} was underground with no party to bring it up and no work for {Minutes:F1} minutes, and has been put back on the island at {Where}",
                    body.Name,
                    BotHomeward.BarrenFor,
                    body.Location
                );
            }

            return null;
        }

        Sent++;

        return new BotHomeward(home, hearth);
    }

    /// <summary>Bots brought up out of a dungeon instead of being offered a walk that does not exist.</summary>
    public static long Surfaced { get; private set; }

    /// <summary>Of those, the ones brought up because a walk home from about where they stood had been refused. See <see cref="Sealed"/>.</summary>
    public static long SurfacedSealed { get; private set; }

    /// <summary>Places underground where a walk home was refused, and when. See <see cref="Sealed"/>.</summary>
    public static long SealedNoted { get; private set; }

    /// <summary>How near a refused start a bot must stand for the refusal to be read as its own: a pocket, not a tile.</summary>
    public static int SealedReach { get; set; } = 12;

    /// <summary>How long a refused start is remembered: the gates and the reach map are redrawn rarely, so half an hour.</summary>
    public static int SealedMs { get; set; } = 1800000;

    private static readonly List<(Map Map, Point3D At, long Tick)> _sealed = [];

    /// <summary>A walk home from this place underground was refused by the pathfinder. Called by <see cref="BotHomeward.Bend"/>.</summary>
    public static void Sealed(Map map, Point3D at)
    {
        var now = Core.TickCount;

        for (var i = _sealed.Count - 1; i >= 0; i--)
        {
            var (m, p, t) = _sealed[i];

            if (now - t >= SealedMs)
            {
                _sealed.RemoveAt(i);
            }
            else if (m == map && Utility.InRange(p, at, SealedReach))
            {
                _sealed[i] = (m, p, now);

                return;
            }
        }

        _sealed.Add((map, at, now));
        SealedNoted++;
    }

    /// <summary>Whether a walk home from near here underground has been refused lately.</summary>
    private static bool WalledIn(Map map, Point3D at)
    {
        var now = Core.TickCount;

        for (var i = 0; i < _sealed.Count; i++)
        {
            var (m, p, t) = _sealed[i];

            if (m == map && now - t < SealedMs && Utility.InRange(p, at, SealedReach))
            {
                return true;
            }
        }

        return false;
    }

    public static void Forget()
    {
        SurfacedSealed = 0;
        SealedNoted = 0;
        Asked = 0;
        Near = 0;
        Busy = 0;
        Sent = 0;
        Surfaced = 0;
    }

    /// <summary>One line, every case named, no branch called other.</summary>
    public static string Describe() =>
        $"{Asked} asked whether to go home: {Near} were already within {BotHomeward.Away} tiles of it, "
        + $"{Busy} were far but had not been out of work for {BotHomeward.BarrenFor:F1} minutes yet, {Sent} were sent back"
        + $" and {Surfaced} were underground with no party and were brought up instead"
        + $" ({SurfacedSealed} of them from a pocket where a walk home had been refused; {SealedNoted} such pockets noted)";
}
