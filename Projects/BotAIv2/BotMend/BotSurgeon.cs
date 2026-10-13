using System;
using Server.Logging;

namespace Server.BotAI.V2;

/// <summary>
/// Offers whoever can mend the worst-hurt bot within sight — <b>itself included</b> — as ordinary work.
///
/// <para>
/// <b>Counting itself among the patients is what closes the gap in the ladder.</b> A bot drops onto
/// <c>Failing</c> at thirty-five per cent and looks after itself above all else; above that it is on the
/// ordinary rung, and without this it would carry on working at half health with nothing in the world offering
/// to patch it up. Now the same question — who here is worst off — answers both cases, and the <em>ladder</em>
/// keeps the ordering rather than a rule in this file: at thirty per cent nobody asks this proposer anything,
/// because the bot is already somewhere more urgent.
/// </para>
///
/// <para>
/// That ordering is a measured defect, not taste. The first version put the call for help <em>above</em>
/// failing health, so a bot on its last few points announced a company it could not join, found nobody able,
/// and posted the same call again dozens of times over. Looking after somebody else must never outrank looking
/// after yourself, and here it cannot.
/// </para>
///
/// <para>
/// <b>Spell or cloth, and the ability decides rather than the name.</b> A heal is two seconds and mana that
/// comes back on its own; a bandage is nine or ten and a thing that had to be bought — so a caster is simply
/// better at this, which is a fact about the two mechanics and not a rule about classes. A warrior with
/// bandages and sixty Healing patching up a miner is exactly as welcome.
/// </para>
///
/// <para>
/// <b>The patient has to be genuinely hurt.</b> That is the anti-exploit and it has to be here rather than in
/// the undertaking, because "cast heal on a healthy friend for ever" is the training dummy with a friend in
/// it — the exact shape the whole ledger is built to refuse. The engine says as much about cloth by itself; for
/// a spell this is the only place it gets said.
/// </para>
/// </summary>
public sealed class BotSurgeon : IBotProposer
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotSurgeon));

    /// <summary>How far a caster looks for somebody worth healing.</summary>
    public static int Reach { get; set; } = 20;

    /// <summary>
    /// How lately somebody must have taken a step to be reckoned on the move, and passed over as a patient while it is
    /// beyond a heal's reach and in no fight.
    ///
    /// <para>
    /// <b>A patient walking away is a chase, and a healer on foot loses it.</b> The mending follows the patient rather
    /// than the place it stood (<see cref="BotSalve"/>), so a hurt bot walking to a counter, riding, or recalling across
    /// the island was followed until the walk gave up. On build 97's window (22:39–23:24, 16.09.2026) 27 mendings ended
    /// "could not get nearer to" — 2 to 4 a window before — nineteen of them in the quarter of an hour of a hunt revel,
    /// when the hurt walked to and from its ground: Sable, Torvin, Brannoc and Emeric all failed at Wulfric inside seven
    /// seconds as it went 21 tiles north in five; Bryn Ashdown was followed five times by four healers and was 250 tiles off
    /// half a minute later. <see cref="BotMend.Beyond"/> answers only after a healer has failed, and only for ten
    /// seconds. The proposer asks the question the walk fails on (A6). A patient in a fight stays a patient: it stays by
    /// its fight, and that is when a healer is wanted.
    /// </para>
    /// </summary>
    public static int MovingMs { get; set; } = 1500;

    /// <summary>Looks at which the worst-hurt was passed over for being on the move out of reach: a count of looks, not of patients.</summary>
    public static long Moving { get; private set; }

    /// <summary>Height, in the engine's units, past which a patient is on another floor. See <see cref="Worst"/> (build 386).</summary>
    public static int Upstairs { get; set; } = 8;

    /// <summary>Patients passed over for standing on another floor. See <see cref="Upstairs"/>.</summary>
    public static long Floors { get; private set; }

    private static bool _said;

    /// <summary>Whether a bot that has just become a patient has the nearest of ours able to mend it look up at once. A switch.</summary>
    public static bool Calls { get; set; } = true;

    /// <summary>Patients for whom somebody was told to look up, and patients with nobody free and able in reach.</summary>
    public static long Called { get; private set; }

    public static long CalledNobody { get; private set; }

    /// <summary>
    /// <b>A healer by a wounded friend heals now (06.10.2026, react14).</b>
    ///
    /// <para>
    /// This proposer is on the free rung, so a healer was asked whether anybody near wanted mending when its review came
    /// round: every fifteen seconds with work in hand. On the stopwatch (build 401, 00:51) 35 bots fell under 70 % with
    /// somebody able to mend within twenty tiles; 18 had a heal begun, at a median of three seconds and a ninth decile of
    /// twenty-six; 9 never did and 3 of those died.
    /// </para>
    ///
    /// <para>
    /// Called from the blow that makes the patient (<c>BotMobile.Alarm</c>: health crossing <c>BotMend.Hurt</c>, or the
    /// failing line): the nearest of ours within <see cref="Reach"/> that has the means, is in no company, no fight and no
    /// mending already, and is on the patient's floor is told to look up (<c>BotWill.Alert</c>). What it does is still this
    /// proposer's answer at the auction — the worst hurt in its reach, by spell or cloth — and <c>BotSalve.Pressing</c> lets
    /// that answer past the dwell for a patient in a fight when the healer's work in hand can be put down. One query a
    /// patient, at the blow; nothing per beat.
    /// </para>
    /// </summary>
    public static void Call(BotMobile patient, bool failing = false)
    {
        var map = patient?.Map;

        if (!Calls || map == null || map == Map.Internal || !patient.Alive)
        {
            return;
        }

        // <b>A member's healer is its company (06.10.2026, react16).</b> Everybody near a member is its company, bound and so
        // never asked by the auction: of build 403's first 167 patients 148 "had nobody free and able". The company's medics
        // bind and cast from its own beat, once a second; the company is asked to answer now. See BotSquads.Hurry.
        if (patient.Squad != null)
        {
            CalledCompany++;

            BotSquads.Hurry(patient.Squad);
        }

        BotMobile best = null;
        var nearest = int.MaxValue;
        var seen = 0;
        BotSquad nurse = null;
        var nurseOff = int.MaxValue;

        foreach (var m in map.GetMobilesInRange<BotMobile>(patient.Location, Reach))
        {
            if (m == patient || m.Deleted || !m.Alive || m.Resolve is not { } resolve)
            {
                continue;
            }

            seen++;

            // One reason each, the first that holds: a gate without a bucket is invisible.
            if (m.Squad != null)
            {
                // A company's medic with no enemy standing may nurse this patient (react17). The nearest such company is told
                // below when no free healer is nearer. See BotSquad.Nurse.
                if (BotSquad.Nurses && m.Squad != patient.Squad && m.Class?.Role == BotRole.Medic
                    && m.Squad.Focus is not { Deleted: false, Alive: true } && !m.Squad.OnTheMarch
                    && Math.Abs(m.Z - patient.Z) < Upstairs && !BotRegard.Hostile(m, patient)
                    && (BotMend.Spell(m, patient) >= 0 || BotMend.Cloth(m) > 0))
                {
                    var away = Math.Max(Math.Abs(m.X - patient.X), Math.Abs(m.Y - patient.Y));

                    if (away < nurseOff)
                    {
                        nurse = m.Squad;
                        nurseOff = away;
                    }

                    continue;
                }

                WhyCompany++;

                continue;
            }

            if (m.Tired || BotLadder.Failing(m) || BotLadder.Hunted(resolve))
            {
                WhyFighting++;

                continue;
            }

            if (resolve.Deed is BotSalve || resolve.Deed is { Committed: true })
            {
                WhyBusy++;

                continue;
            }

            if (Math.Abs(m.Z - patient.Z) >= Upstairs || BotRegard.Hostile(m, patient))
            {
                WhyApart++;

                continue;
            }

            if (BotMend.Spell(m, patient) < 0 && BotMend.Cloth(m) <= 0)
            {
                WhyNoMeans++;

                continue;
            }

            var off = Math.Max(Math.Abs(m.X - patient.X), Math.Abs(m.Y - patient.Y));

            if (off >= nearest)
            {
                continue;
            }

            best = m;
            nearest = off;
        }

        if (best == null || nurse != null && nurseOff < nearest)
        {
            if (nurse != null)
            {
                nurse.Nurse(patient);
                CalledNurse++;

                return;
            }

            if (patient.Squad != null)
            {
                return;
            }

            CalledNobody++;

            if (seen == 0)
            {
                CalledAlone++;
            }

            return;
        }

        Called++;

        // <b>A failing patient is an interruption, not an offer (12.10.2026, w5).</b> The look-up above hands the healer's auction
        // the mending, which jumps the dwell (BotSalve.Pressing) and passes the hold (Summons) and then has to out-price the work
        // in hand plainly: a healer on a delivery at 300 a minute kept delivering. On the night of 12.10 (23:18–03:48) 1 412 bots
        // fell under 70 % with somebody able to mend within twenty tiles, 414 were never mended and 90 of those died; of 902
        // crossings of the failing line 140 died with nothing decided. For a patient under its failing line the nearest able is
        // pressed to the mending as a dire crier's healer is (BotCry.Summon): its work put down and taken up again, never thrown
        // away — a healer holding work that is not taken up again is told to look up, as before.
        //
        // Whether the patient is failing is the caller's word (BotMobile.Alarm): this is called from OnDamage, which the engine runs
        // before it writes the new health, so the patient's own Hits still read the blow away and BotLadder.Failing(patient) was false at
        // every call — the press was dead code until 13.10.2026 (w5 review B).
        if (Presses && failing && best.Resolve is { } free
            && (free.Deed == null || free.Deed.Resumes && (free.Paused == null || BotCry.StackCalls && free.Under == null)))
        {
            var trains = BotMend.Spell(best, patient) >= 0 ? SkillName.Magery : SkillName.Healing;

            if (BotWill.Press(best, new BotSalve(patient, map, onSelf: false, trains), $"called to {patient.Name} under its failing line as its nearest healer, {nearest} tiles off", BotCry.StackCalls))
            {
                Pressed++;

                return;
            }
        }

        BotWill.Alert(best);
    }

    /// <summary>Whether the nearest of ours able to mend a bot under its failing line is pressed to it rather than told to look up. A switch.</summary>
    public static bool Presses { get; set; } = true;

    /// <summary>Healers pressed to a failing patient's mending as an interruption. See <see cref="Call"/>.</summary>
    public static long Pressed { get; private set; }

    /// <summary>Patients brought to the medics of a company near with no enemy standing. See <c>BotSquad.Nurse</c>.</summary>
    public static long CalledNurse { get; private set; }

    /// <summary>Patients in a company, whose company was asked to answer at once. See <see cref="Call"/>.</summary>
    public static long CalledCompany { get; private set; }

    /// <summary>Of <see cref="CalledNobody"/>, patients with nobody of ours at all within reach.</summary>
    public static long CalledAlone { get; private set; }

    /// <summary>Bots within a patient's reach passed over as its healer, by why. Bots, not patients.</summary>
    public static long WhyCompany { get; private set; }

    public static long WhyFighting { get; private set; }

    public static long WhyBusy { get; private set; }

    public static long WhyApart { get; private set; }

    public static long WhyNoMeans { get; private set; }

    /// <summary>The healer's call, for the <c>Reflexes:</c> line.</summary>
    public static string Describe2() =>
        $"{Called} bots that became patients had the nearest of ours able to mend within {Reach} tiles look up at once ({Pressed} of them under the failing line pressed to it, their work put down{(Presses ? "" : "; off")}), {CalledCompany} were members whose company was asked to answer at once, {CalledNurse} were brought to the medics of a company near with no enemy, {CalledNobody} lone ones had nobody free and able there ({CalledAlone} of them nobody of ours at all; bots near passed over: {WhyCompany} in a company, {WhyFighting} in a fight, failing or leaving, {WhyBusy} mending already or on work that may not be left, {WhyApart} on another floor or hostile, {WhyNoMeans} with neither a heal nor cloth)";

    public string Name => "Surgeon";

    public BotStanding Rung => BotStanding.Free;

    public BotDeed Propose(IBotWilful bot)
    {
        var body = bot?.Self;
        var map = body?.Map;

        if (map == null || map == Map.Internal || !body.Alive)
        {
            return null;
        }

        // <b>A healer may heal anybody, itself included, so long as nothing has picked the healer out.</b>
        // Ordered on 24.08.2026, and it is the sharper half of the same rule that stops bandaging under fire.
        // Mending is standing still: a cast is interrupted outright by a blow, and a bandage merely bleeds
        // its worth away per hit — so a healer with a zombie on it is not slow at healing, it is doing
        // nothing at all while being killed, and every beat spent trying is a beat not spent hitting back.
        // What is asked here is who has chosen the healer, not what happens to be nearby: a fight going on
        // ten tiles away is exactly when a healer is needed and is no reason to refuse.
        var onMe = BotThreat.Hunter(body, BotDefender.Reach);

        if (onMe != null)
        {
            BotCry.Raise(body, onMe);

            return null;
        }

        var patient = Worst(body, map);

        if (patient == null)
        {
            return null;
        }

        // Something to mend with, or there is nothing to offer. Spell first, because that is what the
        // undertaking will reach for first.
        var spell = BotMend.Spell(body, patient);

        if (spell < 0 && BotMend.Cloth(body) <= 0)
        {
            return null;
        }

        var onSelf = patient == body;

        if (!onSelf && !_said)
        {
            _said = true;

            logger.Information("{Name} has started patching up the rest of them", body.Name);
        }

        return new BotSalve(patient, map, onSelf, spell >= 0 ? SkillName.Magery : SkillName.Healing);
    }

    /// <summary>
    /// The worst-hurt of this population within reach, counting the asker, or null.
    ///
    /// Asked of the map rather than of a roster, the same way our side of a fight is counted: a hundred and
    /// fifty registry entries walked per bot per decision is what the first version did, and what is nearby
    /// costs what is nearby.
    /// </summary>
    private static Mobile Worst(Mobile bot, Map map)
    {
        Mobile worst = BotMend.Wants(bot) ? bot : null;
        var lowest = worst == null ? 1.0 : BotMend.Share(bot);

        foreach (var mobile in map.GetMobilesInRange<Mobile>(bot.Location, Reach))
        {
            // Being unreachable does not make anybody less hurt, which is why the worst-hurt rule kept
            // handing back the same patient after every refused road. See BotMend.Beyond — the note lapses in
            // ten seconds, so this is a pause rather than an abandonment.
            if (mobile == bot || mobile is not IBotAlly || !BotMend.Wants(mobile) || BotMend.OutOfReach(mobile))
            {
                continue;
            }

            // <b>A patient on another floor (01.10.2026, build 386).</b> Thirteen of the thirty-three "could not get nearer" mendings
            // of build 386 were to patients at z 10–40 in Britain's and Vesper's upper rooms, which the walk cannot climb to from
            // the street. Read as a height, not a house: a hill is a few feet over many tiles, a floor is twenty at once.
            if (Math.Abs(mobile.Z - bot.Z) >= Upstairs)
            {
                Floors++;

                continue;
            }

            var share = BotMend.Share(mobile);

            if (share >= lowest)
            {
                continue;
            }

            // A robber at its robbery, a red, or anybody else it is a crime to help. See BotMend.Abetting.
            if (BotMend.Abetting(bot, mobile) != null)
            {
                continue;
            }

            if (OnTheMove(bot, mobile))
            {
                Moving++;

                continue;
            }

            worst = mobile;
            lowest = share;
        }

        return worst;
    }

    /// <summary>Whether this patient is walking and would have to be chased: stepped lately, beyond a heal's reach, in no fight.</summary>
    private static bool OnTheMove(Mobile healer, Mobile patient) =>
        MovingMs > 0
        && Core.TickCount - patient.LastMoveTime < MovingMs
        && !healer.InRange(patient.Location, BotMend.Cast)
        && !BotMend.Embattled(patient);

    public static string Describe() => $"{Moving} times the worst-hurt was passed over for walking on out of a heal's reach and {Floors} for standing on another floor";

    /// <summary>Lets the note be made again after a world reload.</summary>
    public static void Forget()
    {
        _said = false;
        Moving = 0;
        Floors = 0;
        Called = 0;
        Pressed = 0;
        CalledNobody = 0;
        CalledCompany = 0;
        CalledNurse = 0;
        CalledAlone = 0;
        WhyCompany = 0;
        WhyFighting = 0;
        WhyBusy = 0;
        WhyApart = 0;
        WhyNoMeans = 0;
    }
}
