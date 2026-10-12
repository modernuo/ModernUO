using System;
using System.Collections.Generic;
using Server.Logging;
using Server.Mobiles;

namespace Server.BotAI.V2;

/// <summary>
/// Offers a bot whose health is going the one thing that was missing from that rung: leaving.
///
/// <para>
/// <b>It shares <c>Failing</c> with <see cref="BotMedic"/> and the two do not overlap.</b> This one offers
/// nothing at all unless something hostile is standing there, so an ordinarily hurt bot is offered a bandage
/// and nothing else, exactly as before. When something <em>is</em> standing there the two compete on the
/// same arithmetic as everything else on this shard, and flight is priced to win — see
/// <see cref="BotBolt.Prior"/> for why that price is honest rather than a thumb on the scale.
/// </para>
///
/// <para>
/// <b>Not every hostile is worth running from</b>, and the test is what is left rather than what the bot
/// started with. A bot at a third of its health has a third of its fighting power, and the question is
/// whether what is here can finish that third — a rat cannot and an ogre plainly can. Judging by full
/// strength would have said "you can take these three" of a bot that had already lost the ability to.
/// </para>
/// </summary>
public sealed class BotFugitive : IBotProposer
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotFugitive));

    /// <summary>
    /// How much of what a bot has left the opposition may come to before running beats staying.
    ///
    /// Half. Not one — a bot on this rung is by definition already losing, and something worth half of what
    /// remains is something that finishes the job in the time a bandage takes to wind. Not a tenth either:
    /// a bot that bolts from a rat while bleeding never gets patched up at all.
    /// </summary>
    public static double Bearable { get; set; } = 0.5;

    /// <summary>
    /// How long between saying that somebody had nowhere to run.
    ///
    /// It is worth saying and it is not worth saying eighty-one times in half a minute, which is what the
    /// undertaking's own failure line did before this was asked here instead. One a minute for the whole
    /// population is enough to see it happening and cheap enough to leave on.
    /// </summary>
    public static int CorneredSayMs { get; set; } = 60000;

    /// <summary>
    /// Bots that were losing, wanted to run, and had nowhere to go. A named nought: without it, the whole
    /// case simply stops appearing anywhere.
    /// </summary>
    public static long Cornered { get; private set; }

    /// <summary>
    /// How long a bot that has just got clear of something is not offered flight again, unless it is hurt in the meantime.
    ///
    /// <para>
    /// <b>Rhiannon ran and mended thirty-two times in fifteen minutes on build 46, once a second.</b> A hurt healer in a
    /// company whose fight kept moving at the edge of <see cref="BotBolt.Watch"/>: the flight took one leg, found nothing
    /// hostile within fourteen tiles and ended "clear of it after 1 legs", the medic's bandage was taken, and on the next
    /// beat the fight was back inside fourteen and flight outbid the bandage — "getting away to (1952, 1522)" five times in
    /// six seconds, "(1966, 1500)" seventeen times in the session. Harlan, Joss and Rhiannon had done it on the builds
    /// before, 23 and 24 drops a build. The blow is the fact that matters: a bot nothing has hit since it got clear is left
    /// to wind its bandage for these seconds, and a bot that is hit is offered flight at once.
    /// </para>
    /// </summary>
    public static int CalmMs { get; set; } = 8000;

    /// <summary>Flights not offered because the bot had just got clear and nothing had hit it since. See <see cref="CalmMs"/>.</summary>
    public static long Calmed { get; private set; }

    /// <summary>
    /// How long a bot whose flight ended cornered is not offered another (07.10.2026, work18). Twenty seconds: on build 416 (01:16-10:52)
    /// 40 flights failed "cornered" a median 12 s after they began, and 5 of the 40 were followed by that bot's next flight failing
    /// too. A wall behind the bot is still there a few seconds later; the fight is what is left. Nought: off.
    /// </summary>
    public static int CorneredRestMs { get; set; } = 20000;

    /// <summary>Flights not offered within <see cref="CorneredRestMs"/> of the bot's last cornered flight.</summary>
    public static long CorneredRested { get; private set; }

    private static readonly Dictionary<Serial, long> _corneredAt = [];

    /// <summary>This bot's flight just ended cornered. Called by <see cref="BotBolt"/>.</summary>
    public static void CorneredRun(Mobile body)
    {
        if (body == null)
        {
            return;
        }

        if (_corneredAt.Count > 1024)
        {
            _corneredAt.Clear();
        }

        _corneredAt[body.Serial] = Core.TickCount;
    }

    /// <summary>
    /// How many flights a bot may take inside <see cref="RepeatMs"/> before it stands and fights instead. Two.
    ///
    /// <para>
    /// <b>Forty-seven of the ninety-four deaths in twenty minutes of build 281 were bots in flight, and every one of them had
    /// been running for more than a minute.</b> Faelar the Archer, 6,100 of strength in a company of four, ran from a plague
    /// spawn of 3,500 three times in ninety seconds — "clear of it after 7 legs", then 4, then 3 — and died on the fourth. A
    /// thing that keeps up with a runner cannot be run from; the third time it is on you, it is a fight.
    /// </para>
    /// </summary>
    public static int MostRepeats { get; set; } = 2;

    public static int RepeatMs { get; set; } = 120000;

    /// <summary>Flights not offered because the bot had already run that often lately, and stands instead.</summary>
    public static long Winded { get; private set; }

    /// <summary>Flights not offered because the company round the bot could take what was after it.</summary>
    public static long Companied { get; private set; }

    private static readonly Dictionary<Serial, (long First, int Count)> _runs = [];

    /// <summary>When each bot last got clear of what it was running from.</summary>
    private static readonly Dictionary<Serial, long> _cleared = [];

    /// <summary>This bot has just got clear. Called by <see cref="BotBolt"/> when a flight ends with nothing following.</summary>
    public static void Cleared(Mobile body)
    {
        if (body != null)
        {
            _cleared[body.Serial] = Core.TickCount;
        }
    }

    private static bool _saidCornered;

    private static long _saidCorneredTick;

    /// <summary>
    /// <b>The early flight, on the rung above (29.09.2026, build 323).</b> A flight was offered only once health had run down
    /// to the failing share (a third, a half for a novice), and a third of all flights ended in death a median seven seconds
    /// after they began (19 of 58 on 313–315; ten of the 35 deaths of 322's first ten minutes were "died doing flee"): the
    /// bot ran when the exchange was already lost. This instance answers the Hunted rung — a bot being hit, not yet failing —
    /// and offers the same flight only when it is plainly outmatched: below <see cref="EarlyHealth"/> of its health, and the
    /// opposition worth more than <see cref="EarlyBar"/> times what it has left, the company counted as on the rung below.
    /// </summary>
    public BotFugitive(bool early = false) => _early = early;

    private readonly bool _early;

    /// <summary>The share of health below which a bot being hit may run early. See the constructor.</summary>
    public static double EarlyHealth { get; set; } = 0.7;

    /// <summary>How many times what the bot has left the opposition must come to for an early flight.</summary>
    public static double EarlyBar { get; set; } = 1.5;

    /// <summary>The share of health below which a poisoned bot being hit may run early. Nine tenths. See <see cref="Propose"/>.</summary>
    public static double PoisonedHealth { get; set; } = 0.9;

    /// <summary>Early flights weighed for a poisoned bot above the ordinary gate. Looks, not flights.</summary>
    public static long PoisonedEarly { get; private set; }

    /// <summary>Flights not offered because the worst threat was a caster. See <see cref="Propose"/>.</summary>
    public static long StoodToCasters { get; private set; }

    /// <summary>Early flights offered. See the constructor.</summary>
    public static long Early { get; private set; }

    public string Name => _early ? "Outmatched" : "Fugitive";

    /// <summary>The rung this was written for, shared with the medic; the early flight answers the Hunted rung. See the class note.</summary>
    public BotStanding Rung => _early ? BotStanding.Hunted : BotStanding.Failing;

    public BotDeed Propose(IBotWilful bot)
    {
        var body = bot?.Self;
        var map = body?.Map;

        if (map == null || map == Map.Internal || !body.Alive || body.HitsMax <= 0)
        {
            return null;
        }

        // <b>An offer is not a run (06.10.2026, react14).</b> The flight was counted against <see cref="MostRepeats"/>, and said in
        // the log, where it was offered - and it is offered again on the very next beat while the first is in hand (the auction
        // lets that offer alone: BotBolt.Repeats). So every flight was two: of 695 flights on build 400 (04.10, 04:20-15:37) 654
        // wrote "runs" twice inside two seconds, the bot was at its two runs after its first, and 60038 looks then refused it
        // another "to a bot that had run 2 times in 2 minutes". A bot that had run once and been caught was left with neither a
        // flight nor - under BotSlay.FleeAt - a fight: Isolt at 00:22:38 on 06.10, "a troll was winning", gleaned a bolt and
        // ordered leather arms in front of what killed it ten seconds later. The run is now counted, and said, when the flight
        // is taken (BotBolt.Began); the offers are made as before, so the auction weighs exactly what it weighed.
        var threat = BotThreat.ThreatPower(body, BotBolt.Watch);

        if (threat <= 0.0)
        {
            // Hurt, and nothing is doing it. That is the medic's case and always was.
            return null;
        }

        // What this bot can still bring, not what it could bring at full health. See the class note.
        var left = BotThreat.Power(body) * ((double)body.Hits / body.HitsMax);

        // <b>A poisoned bot runs earlier (01.10.2026, build 388).</b> The health it has is not the health it will have in ten
        // seconds: the poison keeps taking it while the serpent, the spider or the vine — 67 % of 386's flight deaths — keeps
        // biting. The early flight's gate opens at PoisonedHealth for a poisoned bot rather than EarlyHealth.
        var poisoned = body.Poisoned;

        // <b>At the first blows from what it plainly cannot beat, whatever its health (03.10.2026).</b> See ContactOdds. And at
        // once for a bot whose company has just withdrawn from what is on it: the company already judged the thing beyond it
        // (BotSquad.Withdrawing), so neither the odds round the bot nor its company near are asked again.
        var withdrawing = BotSquad.Withdrawing(body);
        var contact = false;

        // <b>Seen coming, and not yet struck (06.10.2026, react14).</b> A bot alone, in no fight of its own choosing, with a
        // creature set on it inside BotMobile.MenaceRange and no blow landed: it has all its health and a start, which is the
        // one moment a flight is cheap. The gates below still decide whether it runs - the opposition past EarlyBar of what it
        // has, past BotThreat.Tolerance of what it and its own near bring, never from a caster - so this only stops the flight
        // waiting for a third of the bot's health to go first. The same odds sight13 walks round a creature at.
        var forewarned = _early && bot.Squad == null && bot.Resolve?.Deed?.Foe == null && BotLadder.Forewarned(bot.Resolve);

        // <b>Its own fight has just judged itself lost (07.10.2026, loops18).</b> A fight that ends "X was winning" is the bot under its
        // own flight share with X on it, and that fight is rested against X for a few seconds (BotTreadmill): fight what you beat, leave
        // what you do not. For those seconds the early flight is weighed as the failing rung weighs it - past Bearable of what the bot
        // has left, whatever its health - rather than waiting for 1.5 times or for the failing line. Every other gate below holds.
        var losing = _early && BotTreadmill.Losing(body);

        if (_early && !losing && body.Hits >= body.HitsMax * (poisoned ? PoisonedHealth : EarlyHealth))
        {
            if (!withdrawing && !forewarned && !AtContact(bot, body, threat))
            {
                return null;
            }

            contact = !forewarned;
        }

        if (_early && poisoned && body.Hits >= body.HitsMax * EarlyHealth)
        {
            PoisonedEarly++;
        }

        if (!withdrawing && threat <= left * (_early && !losing ? EarlyBar : Bearable))
        {
            return null;
        }

        // <b>The company counts (29.09.2026).</b> Decide (BotThreat) weighs the threat against everybody of ours within reach;
        // this weighed it against the bot alone, so a member of a company of four ran from what the four could have taken.
        var ours = BotThreat.OurPower(body, BotBolt.Watch) - BotThreat.Power(body) + left;

        if (!withdrawing && ours > left && threat <= ours * BotThreat.Tolerance)
        {
            Companied++;

            return null;
        }

        // <b>From a caster a bot does not run: it fights to the end (30.09.2026, build 355; Patrick: "fight to the end").</b>
        // A mage follows and casts at the back of a runner; flights from casters died two times in three on 337–342 against
        // a third or less hand to hand (`scratchpad/flightx.py`), a gazer killing Dunstan 69 tiles into his run. With the
        // worst of what is round the bot a caster, no flight is offered — the fight and the medic on this rung are what is left.
        if (BotThreat.Strongest(body, BotBolt.Watch) is Server.Mobiles.BaseCreature { AI: Server.Mobiles.AIType.AI_Mage })
        {
            StoodToCasters++;

            return null;
        }

        // <b>And the third run is a stand.</b> See MostRepeats.
        var now = Core.TickCount;

        if (_runs.TryGetValue(body.Serial, out var runs) && now - runs.First < RepeatMs && runs.Count >= MostRepeats)
        {
            Winded++;

            return null;
        }

        // Cornered a moment ago: no new run into the same wall. See CorneredRestMs.
        if (CorneredRestMs > 0 && _corneredAt.TryGetValue(body.Serial, out var corneredTick) && now - corneredTick < CorneredRestMs)
        {
            CorneredRested++;

            return null;
        }

        // Just got clear, and nothing has hit it since: the bandage gets its seconds. Compared by subtraction, as every
        // tick on this shard is. See CalmMs.
        if (_cleared.TryGetValue(body.Serial, out var clearedTick) && Core.TickCount - clearedTick < CalmMs
            && (bot.Resolve == null || bot.Resolve.HurtTick - clearedTick <= 0))
        {
            Calmed++;

            return null;
        }

        var strongest = BotThreat.Strongest(body, BotBolt.Watch);

        // <b>A town first, when running for one is realistic (02.10.2026, Patrick's order).</b> A bot alone — a company is its
        // own refuge — with a guarded town within reach at its own pace, the pursuer no quicker, and no deadlier ground on the
        // way runs for the town rather than eighteen tiles away from the thing. See BotHaven for what the guards there do.
        if (bot.Squad == null && BotHaven.Choose(body, strongest, out var haven, out var town, out var tiles))
        {
            return new BotBolt(map, body.Location, haven, town, tiles)
            {
                Began = () =>
                {
                    Running(body, threat, left, strongest, _early, town, contact, withdrawing, forewarned);
                    Offered(contact, withdrawing, forewarned);
                    Ran(body, Core.TickCount);

                    if (losing)
                    {
                        BotTreadmill.FlightsAfterLosing++;
                    }
                }
            };
        }

        // <b>Asked before the offer is made, not discovered after it is taken.</b> See BotBolt.Retreat: an
        // undertaking that fails the instant it begins is offered again in the same beat, for ever. Two tile
        // probes, and only for a bot that is already losing a fight to something standing next to it.
        if (BotBolt.Retreat(map, body, strongest) == Point3D.Zero)
        {
            Cornered++;

            Trapped(body);

            // Nothing offered, so the medic and the fighting on this rung are what is left — which is the
            // honest answer for a bot with its back to the water.
            return null;
        }

        return new BotBolt(map, body.Location)
        {
            Began = () =>
            {
                Running(body, threat, left, strongest, _early, null, contact, withdrawing, forewarned);
                Offered(contact, withdrawing, forewarned);
                Ran(body, Core.TickCount);

                if (losing)
                {
                    BotTreadmill.FlightsAfterLosing++;
                }
            }
        };
    }

    /// <summary>Flights taken before the first blow, from something seen coming. See <see cref="Propose"/>.</summary>
    public static long Forewarned { get; private set; }

    /// <summary>
    /// Whether this bot has just got clear of something and nothing has hit it since: the seconds its bandage is given. Read by
    /// <c>BotDefender</c>, which does not send it back into the fight it has just left. See <see cref="CalmMs"/>.
    /// </summary>
    public static bool Calm(Mobile body, BotResolve resolve) =>
        body != null && _cleared.TryGetValue(body.Serial, out var cleared) && Core.TickCount - cleared < CalmMs
        && (resolve == null || resolve.HurtTick - cleared <= 0);

    /// <summary>
    /// How many times what the bot and its own near it bring (<c>BotThreat.OurPower</c>, at full health) the opposition round it
    /// must come to for a bot being hit to run at once, whatever its health. Three: the "several times theirs" Patrick drew the
    /// line at on 26.09.2026, the figure <c>BotPeril.Overwhelm</c> closes work by and <c>BotDanger.DeadlyVsWalker</c> prices a
    /// road by. Nought turns it off.
    ///
    /// <para>
    /// <b>The early flight waited for a third of the bot's health (03.10.2026).</b> A bot set upon by something past
    /// <c>BotThreat.Tolerance</c> of its side neither hits back — <c>BotDefender</c> refuses the fight and the swing is held (1213
    /// swings held on build 395 by 04:19) — nor runs, unless a town is a realistic run, until it is under
    /// <see cref="EarlyHealth"/> of its health. Measured on build 395 (03:20–04:19): 190 flights, 89 of them dead inside a
    /// minute; 35 began against ten times or more what the bot had left, and 23 of those died. Leofwin ran from a plague beast
    /// of 20262 at 46 of 80 health "with 1607 of ours near" and died 21 seconds later; Marcus at 44 of 75 and died 28 seconds
    /// later. Against something it cannot hope to beat, the health a bot spends waiting is the health it would have run on.
    /// </para>
    /// </summary>
    public static double ContactOdds { get; set; } = 3.0;

    /// <summary>Flights offered at the first blows, the bot's health above the early gate. See <see cref="ContactOdds"/>.</summary>
    public static long Contacted { get; private set; }

    /// <summary>Flights offered to a bot whose company had just withdrawn from what was on it. See <c>BotSquad.Withdrawing</c>.</summary>
    public static long Withdrawn { get; private set; }

    /// <summary>One flight offered, counted once: for a withdrawn company's member, at the first blows, or early.</summary>
    private void Offered(bool contact, bool withdrawing, bool forewarned = false)
    {
        if (forewarned)
        {
            Forewarned++;
        }
        else if (withdrawing)
        {
            Withdrawn++;
        }
        else if (contact)
        {
            Contacted++;
        }
        else if (_early)
        {
            Early++;
        }
    }

    /// <summary>
    /// Whether a bot being hit, above the early flight's health gate, is plainly overmatched: the opposition round it at least
    /// <see cref="ContactOdds"/> times what it and its own near bring. Never for a bot in a fight it chose (its work has a foe:
    /// the hunt weighs its own odds and gives up on the numbers itself), so the hunting is left as it was.
    /// </summary>
    private static bool AtContact(IBotWilful bot, Mobile body, double threat) =>
        ContactOdds > 0.0 && bot.Resolve?.Deed?.Foe == null && threat >= ContactOdds * Math.Max(1.0, BotThreat.OurPower(body, BotBolt.Watch));

    /// <summary>One more flight on this bot's recent count. See <see cref="MostRepeats"/>.</summary>
    private static void Ran(Mobile body, long now)
    {
        _runs[body.Serial] = _runs.TryGetValue(body.Serial, out var ran) && now - ran.First < RepeatMs
            ? (ran.First, ran.Count + 1)
            : (now, 1);
    }

    /// <summary>Runs for a town offered to a bot that could not win where it stood, by the defender. See <see cref="ToTown"/>.</summary>
    public static long Outrun { get; private set; }

    /// <summary>
    /// <b>The run for a town offered where the fight is refused (02.10.2026).</b> <see cref="BotDefender"/> refuses to hit back
    /// when the odds where the bot stands are past <c>BotThreat.Tolerance</c> — the bot "cannot win the fight" in Patrick's
    /// words — and until now raised a cry and kept its errand, taking the blows on the move. With a guarded town a realistic
    /// run away (<see cref="BotHaven.Choose"/>) it runs there instead; nothing changes where no town is. Not for a bot in a
    /// company, which is its own refuge; not for a bot that has run <see cref="MostRepeats"/> times lately (it stands, as
    /// before); not from a caster (Patrick: fight to the end). Null when no run is offered.
    /// </summary>
    public static BotDeed ToTown(IBotWilful bot, Mobile body, BaseCreature foe)
    {
        var map = body?.Map;

        if (bot == null || map == null || map == Map.Internal || !body.Alive || foe == null || bot.Squad != null)
        {
            return null;
        }

        if (bot.Resolve?.Deed is BotBolt)
        {
            return null;
        }

        if (foe is { AI: AIType.AI_Mage })
        {
            StoodToCasters++;

            return null;
        }

        var now = Core.TickCount;

        if (_runs.TryGetValue(body.Serial, out var runs) && now - runs.First < RepeatMs && runs.Count >= MostRepeats)
        {
            Winded++;

            return null;
        }

        if (!BotHaven.Choose(body, foe, out var haven, out var town, out var tiles))
        {
            return null;
        }

        var left = BotThreat.Power(body) * (body.HitsMax > 0 ? (double)body.Hits / body.HitsMax : 1.0);

        // Said and counted when the run is taken, as every flight is. See Propose.
        return new BotBolt(map, body.Location, haven, town, tiles)
        {
            Began = () =>
            {
                Running(body, BotThreat.ThreatPower(body, BotBolt.Watch), left, foe, false, town);
                Ran(body, Core.TickCount);
                Outrun++;
            }
        };
    }

    /// <summary>Says that somebody is cornered, at most once a minute for the whole population.</summary>
    private static void Trapped(Mobile body)
    {
        var now = Core.TickCount;

        if (_saidCornered && now - _saidCorneredTick < CorneredSayMs)
        {
            return;
        }

        _saidCornered = true;
        _saidCorneredTick = now;

        logger.Information(
            "{Name} is losing and has nowhere to run to, so it is not being offered flight; {Count} times so far",
            body.Name,
            Cornered
        );
    }

    /// <summary>Flights by what they ran from: a caster, an archer, or something that has to close to hurt. See Running.</summary>
    public static long FromCasters { get; private set; }

    public static long FromArchers { get; private set; }

    public static long FromMelee { get; private set; }

    /// <summary>
    /// <b>Every flight, with its numbers (29.09.2026, build 333).</b> The line was said once a session, so "did it run too
    /// late" could not be asked of the log: the night's analyst matched 76 flights to their deaths (median 14 s from taking
    /// flight to dying, 42–52 % of all deaths) and found the one number that would answer it printed once. Now each flight
    /// says its health, what it ran from and how far off that stood, whether it was the early flight, and who of ours was near.
    /// An instrument before a fix.
    /// </summary>
    private static void Running(
        Mobile body, double threat, double left, Mobile foe, bool early, string town, bool contact = false, bool withdrawing = false,
        bool forewarned = false
    )
    {
        var kind = foe is BaseCreature { AI: AIType.AI_Mage } ? "a caster"
            : foe is BaseCreature { AI: AIType.AI_Archer } ? "an archer"
            : foe == null ? "nothing seen" : "hand to hand";

        switch (kind)
        {
            case "a caster":
                FromCasters++;
                break;
            case "an archer":
                FromArchers++;
                break;
            case "hand to hand":
                FromMelee++;
                break;
        }

        var off = foe == null ? -1 : Math.Max(Math.Abs(foe.X - body.X), Math.Abs(foe.Y - body.Y));
        var ours = BotThreat.OurPower(body, BotBolt.Watch) - BotThreat.Power(body);

        logger.Information(
            "{Name} runs{Early}{Town} from ({X}, {Y}) at {Hits} of {Pool} health: {Threat:F0} of hostile against the {Left:F0} it has left ({Ours:F0} of ours near), from {Foe} ({Kind}, {Off} tiles off); stamina {Stam} of {StamMax}{Mounted}{Contact}",
            body.Name,
            early ? " early" : "",
            town == null ? "" : $" for {town}",
            body.X,
            body.Y,
            body.Hits,
            body.HitsMax,
            threat,
            left,
            Math.Max(0.0, ours),
            foe?.Name ?? "nothing it could see",
            kind,
            off,
            body.Stam,
            body.StamMax,
            body.Mounted ? ", mounted" : "",
            forewarned ? "; seen coming, before its first blow"
            : withdrawing ? "; its company withdrew from it"
            : contact ? $"; at the first blows, the opposition past {ContactOdds:F1} times what it and ours near bring" : ""
        );
    }

    /// <summary>Lets the line be said again after a world reload.</summary>
    public static void Forget()
    {
        _saidCornered = false;
        Cornered = 0;
        Calmed = 0;
        _cleared.Clear();
    }

    /// <summary>For the mending line: flights with nowhere to go, and flights held back while a bandage was wound.</summary>
    public static string Describe() =>
        $"flights from {FromCasters} casters, {FromArchers} archers and {FromMelee} hand to hand, {StoodToCasters} not offered because the worst of it was a caster (fought to the end); {Early} flights offered early to a bot plainly outmatched (under {EarlyHealth:P0} of its health, the opposition past {EarlyBar:F1} times what it had left; {PoisonedEarly} looks taken for a poisoned bot above that gate, under {PoisonedHealth:P0}), {Contacted} at the first blows above that gate, the opposition past {ContactOdds:F1} times what the bot and ours near bring and the bot in no fight of its own choosing, {Withdrawn} to a bot whose company had just withdrawn from what was on it, {Outrun} runs for a town offered by the defender to a bot that could not win where it stood, {Cornered} bots losing with nowhere to run, {Calmed} flights not offered to a bot that had just got clear and had not been hit since, {Companied} not offered because the company round the bot could take it, {Winded} not offered to a bot that had run {MostRepeats} times in {RepeatMs / 60000} minutes and stands instead, every run counted when its flight was taken and not when it was offered, {Forewarned} flights taken before the first blow from something seen coming, "
        + $"{BotBolt.Tended} bandages and bottles taken on the run, {BotBolt.Bent} flights turned another way for a refused road, {BotBolt.Vetoed} retreats refused by the look ahead before anybody walked";
}
