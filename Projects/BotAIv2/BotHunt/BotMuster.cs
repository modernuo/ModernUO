using System.Collections.Generic;
using Server.Logging;
using Server.Mobiles;

namespace Server.BotAI.V2;

/// <summary>
/// Offers a bot the chance to call a company against something it must otherwise walk past.
///
/// <para>
/// <b>It answers the question the hunter is not allowed to ask.</b> <see cref="BotHunter"/> judges a quarry
/// against what one bot can do alone, because a claim sends exactly one bot — and that is right, and it was
/// paid for in five deaths in nine minutes when the sum counted company that never came. The cost of being
/// right about it is a population that spends its evenings on rats while everything worth killing stands in
/// the next field. This is the other half: not "can I take that", but "could we".
/// </para>
///
/// <para>
/// <b>Nothing is announced and nobody is asked.</b> The bots who come are simply put in the company by the
/// one that called it, in the same beat, which is what the squad's own note means by the collective mind
/// being arithmetic rather than messages. The first version's alternative is on record — a bot posted a
/// call for help, the call found nobody able, it disbanded in the same tick, and the bot posted it again,
/// dozens of times over.
/// </para>
/// </summary>
public sealed class BotMuster : IBotProposer
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotMuster));

    /// <summary>
    /// How far around itself a bot looks, both for the thing worth calling a company against and for the
    /// bots who would make it up.
    ///
    /// <para>
    /// One number for both, and it has to be one: the company is assembled in a single beat out of whoever
    /// is standing there, so a creature inside the reach and helpers outside it is a company of one.
    /// </para>
    ///
    /// <para>
    /// <b>Twelve tiles — about a screen — turned out to be the reason ogres were walked past.</b> A company
    /// is refused outright when no ally is inside this range, and that check happens before a single creature
    /// is examined, so a bot alone in a field never even asks what is standing in it. Once prowling started
    /// working properly the population spread out across three hundred tiles, and being alone became the
    /// normal state: nine hundred and eighty-one refusals in twenty minutes, every one of them silent about
    /// which gate closed. Widening this costs a longer walk to the fight and buys the fights themselves,
    /// which is the trade the whole subsystem exists to make.
    /// </para>
    /// </summary>
    /// <para>
    /// <b>And twenty-eight became too narrow the same way twelve did, for the same reason, on 08.09.2026.</b>
    /// Widening the population's roaming that afternoon — the dart limit went from 240 tiles to 500 — spread
    /// it further again, and the refusals came back larger than before: 1306 in fifteen minutes, against the
    /// 981 in twenty that bought the last widening. Patrick watched five bots stand beside four orcs, each
    /// orc at 111 health against their 62, and nobody call anybody.
    /// </para>
    ///
    /// <para>
    /// <b>Tying it to <see cref="BotCry.Carries"/> was tried first and measured as not enough:</b> refusals
    /// went from 82% of real asks to 78%, and companies called from 2.7 a minute to 2.5. The two numbers
    /// look alike and are not. A cry is an emergency — forty tiles is twenty seconds of running, and help
    /// that arrives later arrives at a corpse. A muster is a plan: the fight has not started, nobody is
    /// dying, and a march is affordable.
    /// </para>
    ///
    /// <para>
    /// <b>So it is sized from the population's own spread instead of guessed.</b> Forty-nine bots roaming a
    /// circle of five hundred tiles is about sixteen thousand tiles each, which puts the nearest neighbour
    /// something like seventy tiles away; finding two of them needs roughly twice that. A hundred and twenty
    /// is a march of about a minute at walking pace, against fights that run for several.
    /// </para>
    /// </summary>
    public static int Reach { get; set; } = 120;

    /// <summary>
    /// How many others have to be free and able before it is worth calling at all.
    ///
    /// Two. One helper is not a company — it is two bots taking on something the arithmetic already said one
    /// bot must refuse, which is the same mistake with a witness.
    /// </summary>
    public static int Least { get; set; } = 2;

    /// <summary>
    /// The largest share of the population that may be standing in companies before no more are called.
    ///
    /// <para>
    /// <b>A company is bots not working, and until 08.09.2026 nothing capped how many.</b> Widening
    /// <see cref="Reach"/> to 120 that evening fixed the calling — refusals for want of a neighbour fell
    /// from 82% of real asks to 6% and companies called went from 40 to 96 — and immediately overshot: eight
    /// companies standing, 36 of 49 bots on the Bound rung, twelve holding nothing at all, and the shard's
    /// finishing rate down to 68%. A bot on that rung is not offered work of its own, so three quarters of
    /// the population had quietly stopped mining, sewing and trading.
    /// </para>
    ///
    /// <para>
    /// A third. Enough that a real threat always gets a company, few enough that the island goes on being
    /// worked while it does. This is the number to move if companies are wanted more or less often — moving
    /// Reach instead only changes how far away they find each other.
    /// </para>
    ///
    /// <para>
    /// <b>A half, from a third (01.10.2026, Patrick's night order: «more hunts, more loot, more levelling»).</b> On 30.09
    /// this gate refused 1009, 1082, 1264 and 2171 musters in the four evening sessions (17:58–23:06) — 28 % of every ask on 378
    /// ("933 refused because 32 of us are already in one" of 3301 at ten minutes) — the largest single reason a band was not
    /// called, while the fights a band takes on are the ones worth most loot and most skill. What the third protected is still
    /// protected differently now: a call takes only bots whose own work it would not throw away (BotSquads.Enrol).
    /// </para>
    /// </summary>
    public static double MostBound { get; set; } = 0.5;

    /// <summary>Calls not made because too much of the population is already in a company.</summary>
    public static long Enough { get; private set; }

    /// <summary>Companies a veteran was let call past the island's ceiling (07.10.2026, traits17).</summary>
    public static long VeteransPastCeiling { get; private set; }

    private static bool _said;

    public string Name => "Muster";

    public BotStanding Rung => BotStanding.Free;

    /// <summary>
    /// Why a company was not called, counted case by case with no bucket called "other".
    ///
    /// <para>
    /// <b>Nought companies in an evening and nothing anywhere saying why.</b> Squads went from fifteen in
    /// twenty minutes to none, twice, and every explanation on offer was a guess: the population might be too
    /// scattered to gather two helpers, or there might be nothing big enough to need a company, or everybody
    /// might be too hurt. Those want opposite fixes and the log could not tell them apart, because a proposer
    /// that returns null is silent about which of its four gates closed. The counters below are that
    /// distinction, and the denominator — how many bots got as far as being asked — is what makes the rest
    /// of them mean anything.
    /// </para>
    /// </summary>
    public static long Asked { get; private set; }

    public static long Held { get; private set; }

    public static long Unfit { get; private set; }

    /// <summary>Nobody of ours was near enough to make a company at all.</summary>
    public static long Alone { get; private set; }

    /// <summary>Everything standing here is already one bot's work.</summary>
    public static long AllSmall { get; private set; }

    /// <summary>Something is here and it is beyond what everybody standing here could take together.</summary>
    public static long AllTooBig { get; private set; }

    public static long NothingBig { get; private set; }

    public static long TooFewNear { get; private set; }

    public static long Called { get; private set; }

    /// <summary>How long an urged foe is held for the next look. Fifteen seconds. See <see cref="Urge"/>.</summary>
    public static int UrgeMs { get; set; } = 15000;

    /// <summary>Bands raised on an urged foe, and urges that found too few free bots near. See <see cref="Urge"/>.</summary>
    public static long UrgedBands { get; private set; }

    public static long UrgedAlone { get; private set; }

    private static readonly Dictionary<Serial, (BaseCreature Quarry, long Tick)> _urged = [];

    /// <summary>The slayer, failing on a crowd, names the foe for this bot's next look. See the note in <c>Propose</c>.</summary>
    public static void Urge(Mobile body, BaseCreature quarry)
    {
        if (body == null || quarry == null)
        {
            return;
        }

        _urged[body.Serial] = (quarry, Core.TickCount);
    }

    public BotDeed Propose(IBotWilful bot)
    {
        var body = bot?.Self;
        var map = body?.Map;

        if (map == null || map == Map.Internal || !body.Alive)
        {
            return null;
        }

        // Squads have to actually be running. The module can be switched off, and a proposer that offered
        // companies into a subsystem that is not there would hand every bot an undertaking that fails on its
        // first beat.
        if (!BotSquads.Running)
        {
            return null;
        }

        Asked++;

        if (bot is not IBotSquadMember { Squad: null })
        {
            Held++;

            return null;
        }

        // A trader calls no company (01.10.2026, build 385): BotSquads.Form would refuse it, and the band would fail on its first
        // beat. It fights what falls on it alone. See BotTrading.Neutral.
        if (BotTrading.Passes(body))
        {
            return null;
        }

        // <b>Enough of the island is already under arms.</b> Every bot in a company is a bot the auction no
        // longer offers work to, so calling without a ceiling turns a population into an army that owns
        // nothing. See MostBound for the evening this was measured on.
        var alive = BotPopulation.Count;

        if (alive > 0 && BotSquads.Bound >= alive * MostBound)
        {
            // A veteran may call a company past the ceiling: the right to lead (07.10.2026, traits17). See BotTraits.Veteran.
            if (!BotTraits.Veteran(body))
            {
                Enough++;

                return null;
            }

            VeteransPastCeiling++;
        }

        // The same fitness a lone hunt asks for. Being in company does not make a bot at half health any
        // more able to be in a fight, and the member that falls over first is the one that takes the squad
        // below the two it needs to exist.
        if (body.HitsMax <= 0 || body.Hits < body.HitsMax * BotHunter.FitAt)
        {
            Unfit++;

            return null;
        }

        // <b>The foe a bot has just found too many round is the company's foe (29.09.2026, Patrick's word: full mutual
        // aid).</b> Thirty-seven hunts and rescues since 10:59 ended "too many of them around X" or "X was winning", and
        // what the bot did next was flee (11) or wander off (26); not one called the company. The slayer urges the foe
        // here as it fails (BotSlay), and the next look raises the band on it without asking whether "we could" — the
        // company decides that at the foe — so long as Least are free within reach.
        if (_urged.TryGetValue(body.Serial, out var urge) && Core.TickCount - urge.Tick < UrgeMs)
        {
            _urged.Remove(body.Serial);

            // Not one shunned meanwhile (build 388): a band's judgement after the levy shuns the quarry for ShunMs, and the urge
            // read none of it — Freya called companies 34, 35 and 36 against one reaper at 19:58:13, :16 and :18 on 387.
            if (urge.Quarry is { Deleted: false, Alive: true } foe && foe.Map == map && body.InRange(foe, Reach * 2) && !BotQuarry.Shunned(foe))
            {
                if (Free(body, Reach, out var levy) >= Least)
                {
                    if (Judged && (levy + BotThreat.Power(body)) * BotThreat.Tolerance < BotBand.Asked(map, foe, body))
                    {
                        Short++;

                        return null;
                    }

                    if (Judged && BotSquad.BeyondFor(body, foe, levy + BotThreat.Power(body)))
                    {
                        Priced++;

                        return null;
                    }

                    UrgedBands++;
                    Called++;
                    Once(body, foe);

                    return new BotBand(foe, BotThreat.OurPower(body, Reach));
                }

                UrgedAlone++;
            }
        }

        // At the company's choice gate (06.10.2026, company16): the finder passes over what BeyondFor below would refuse, and
        // offers the next creature down rather than nothing.
        var quarry = BotQuarry.Company(body, Reach, BotSquad.ChoiceOdds, out var why);

        if (quarry == null)
        {
            switch (why)
            {
                case BotQuarry.CompanyRefusal.Alone:
                    Alone++;

                    break;

                case BotQuarry.CompanyRefusal.AllSmall:
                    AllSmall++;

                    break;

                case BotQuarry.CompanyRefusal.AllTooBig:
                    AllTooBig++;

                    break;

                default:
                    NothingBig++;

                    break;
            }

            return null;
        }

        if (Free(body, Reach, out var brought) < Least)
        {
            TooFewNear++;

            return null;
        }

        // <b>The company the call could raise is judged before the band is offered (01.10.2026, build 388).</b> The quarry was
        // weighed against everybody able within reach (BotQuarry.Company, BotThreat.OurPower) — bots already in other companies
        // and bots at work the levy would not take included — and the band's call then judged the company that came
        // (BotBand.Rejudged, build 387) and failed on its first beat when it fell short: 16 such failures in 80 minutes of 387,
        // Freya calling the same three against the same reaper three times, each a failed band and a disbanded company. The
        // free and pressable bots the call would take, with the caller, are weighed here against what the fight asks, by the
        // same bar; a band that could not pass it is not offered. Patrick: work that cannot start is not offered.
        if (Judged && (brought + BotThreat.Power(body)) * BotThreat.Tolerance < BotBand.Asked(map, quarry, body))
        {
            Short++;

            return null;
        }

        // <b>And by the company's own price, which the band's call asks next (06.10.2026, work15).</b> The call judges the
        // company that came twice: against BotBand.Asked, the bar above, and against BotSquad.Beyond, which prices the quarry by
        // the company record (reaper13, BotCompanyRecord) and withdraws past WithdrawOdds. The offer asked only the first, so a
        // band whose crowd cleared Asked was called, levied and disbanded in one beat for the second: "only 4 came against an air
        // elemental: 10377 of strength to the 5811 it asks" on 06.10, a failure that printed a passing sum. Asked here by the same
        // test; like the Short gate above it shuns nothing, since a bigger crowd may yet come by.
        if (Judged && BotSquad.BeyondFor(body, quarry, brought + BotThreat.Power(body)))
        {
            Priced++;

            return null;
        }

        Called++;

        Once(body, quarry);

        return new BotBand(quarry, BotThreat.OurPower(body, Reach));
    }

    /// <summary>Whether the free bots within reach are weighed against the fight before a band is offered. A switch. See <see cref="Propose"/>.</summary>
    public static bool Judged { get; set; } = true;

    /// <summary>Bands not offered because the free bots within reach, with the caller, fell short of the fight. See <see cref="Judged"/>.</summary>
    public static long Short { get; private set; }

    /// <summary>Bands not offered because the company's own price of the quarry put it past what the free near could beat. See <c>BotSquad.BeyondFor</c>.</summary>
    public static long Priced { get; private set; }

    /// <summary>Bots near a caller passed over in the count because the company's door would refuse them as hostile. See <see cref="Free"/>.</summary>
    public static long HostileNear { get; private set; }

    /// <summary>
    /// How many of ours are standing here, able to fight, and not already in a company — and what they bring between them
    /// (<c>BotThreat.Power</c>), the sum the band's own judgement reads (<c>BotBand.Brought</c>).
    ///
    /// Counted from what is nearby rather than from a roster — the same rule the rest of this project
    /// follows, and for the same reason: asking the map costs what is nearby, while walking the population
    /// costs the population.
    /// </summary>
    private static int Free(Mobile body, int range, out double strength)
    {
        var map = body.Map;
        var free = 0;

        strength = 0.0;

        foreach (var mobile in map.GetMobilesInRange<Mobile>(body.Location, range))
        {
            if (mobile == body || mobile is not IBotSquadMember { Squad: null })
            {
                continue;
            }

            // Only a bot the call would take: one whose work the enlistment would not throw away (01.10.2026, BotSquads.Enrol) —
            // the offer and the call ask the same question, or the offer is a band that calls nobody. Nor a neutral class, which
            // the company's door refuses (BotTrading.Refuses).
            if (mobile is IBotAlly { AbleToFight: true } && (mobile is not IBotWilful wilful || BotSquads.Pressable(wilful))
                && !BotTrading.Neutral(mobile))
            {
                // Nor one of a guild hostile to the caller's, which the company's one door turns away (BotSquads.Join, 01.10.2026):
                // counted here, it was strength the offer promised and the call could never take (06.10.2026, work15).
                if (BotRegard.Hostile(body, mobile))
                {
                    HostileNear++;

                    continue;
                }

                free++;
                strength += BotThreat.Power(mobile);
            }
        }

        return free;
    }

    private static void Once(Mobile body, Mobile quarry)
    {
        if (_said)
        {
            return;
        }

        _said = true;

        // Said once, by name, because this is the first time on this shard that anything has proposed a
        // company at all — and "companies are never formed" and "companies are formed and do nothing" look
        // identical in a log that says neither.
        logger.Information(
            "{Name} is the first to call a company: {What} is beyond one bot and within reach of several",
            body.Name,
            quarry.Name
        );
    }

    /// <summary>Every gate, counted apart, against the number of bots that reached them.</summary>
    public static string Describe() =>
        Asked == 0
            ? "nobody has been offered a company yet"
            : $"{Asked} asked: {Called} called ({UrgedBands} of them on a foe a bot had just found too many round, {UrgedAlone} such urges with too few free near), {Enough} refused because {BotSquads.Bound} of us are already in one, {Alone} had nobody of ours near enough, {AllSmall} found only one-bot work, "
              + $"{AllTooBig} found something beyond all of us together, {NothingBig} found nothing hostile at all, "
              + $"{TooFewNear} found one but fewer than {Least} free, {Short} found one the free near fell short of (judged before the call{(Judged ? "" : ", off")}), {Priced} found one the company's own price put past them, {HostileNear} hostile bots near a caller left out of the count, {Unfit} too hurt, {Held} already in one";

    /// <summary>Lets the line be said again after a world reload.</summary>
    public static void Forget()
    {
        _said = false;
        Asked = 0;
        Held = 0;
        Unfit = 0;
        Alone = 0;
        Enough = 0;
        AllSmall = 0;
        AllTooBig = 0;
        NothingBig = 0;
        TooFewNear = 0;
        Short = 0;
        Priced = 0;
        HostileNear = 0;
        Called = 0;
    }
}
