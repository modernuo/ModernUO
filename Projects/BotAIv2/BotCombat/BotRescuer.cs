using Server.Logging;
using Server.Mobiles;

namespace Server.BotAI.V2;

/// <summary>
/// Offers a free bot the chance to go to somebody's aid.
///
/// <para>
/// <b>The half of "we are a company" that no amount of formation could supply.</b> A squad already carries
/// its own to trouble: the anchor moves onto whoever was hit and every station is derived from it, so the
/// rest are walking before anything is decided. That works beautifully and only for bots who are already in
/// the same squad — which, on a population that spends its day scattered across three hundred tiles at
/// separate trades, is almost nobody. Fourteen of fifteen bots could watch the fifteenth die four screens
/// away and none of them would ever be asked the question.
/// </para>
///
/// <para>
/// It is offered rather than ordered, on the <c>Free</c> rung like every other kind of work, and it is priced
/// so that it wins against digging and loses to nothing much. What makes it actually interrupt a trip to the
/// forge is <see cref="BotDeed.Pressing"/> — see <see cref="BotRescue"/>.
/// </para>
/// </summary>
public sealed class BotRescuer : IBotProposer
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotRescuer));

    /// <summary>How far a bot will go to help. The cry has to carry at least this far to be heard.</summary>
    public static int Reach { get; set; } = 60;

    /// <summary>
    /// The share of health below which a bot is no use to anybody else.
    ///
    /// Lower than the hunter's own fitness bar. Turning up at three-quarters health to pull something off a
    /// friend is worth doing; turning up at a fifth is adding a second corpse.
    /// </summary>
    public static double FitAt { get; set; } = 0.55;

    private static bool _said;

    /// <summary>Rescues passed over because the crowd round the foe outweighs the friend's side and this bot together.</summary>
    public static long Outmatched { get; private set; }

    /// <summary>Steady criers passed over for the crowd note on the thing (<c>BotQuarry.Crowded</c>); a dire crier is not. See <see cref="Propose"/>.</summary>
    public static long PassedCrowded { get; private set; }

    public string Name => "Rescuer";

    public BotStanding Rung => BotStanding.Free;

    public BotDeed Propose(IBotWilful bot)
    {
        var body = bot?.Self;
        var map = body?.Map;

        if (map == null || map == Map.Internal || !body.Alive)
        {
            return null;
        }

        if (body.HitsMax <= 0 || body.Hits < body.HitsMax * FitAt)
        {
            return null;
        }

        var (friend, foe) = BotCry.Nearest(body, Reach);

        if (friend == null || foe == null)
        {
            return null;
        }

        // <b>Not underground: down there the delve's company is the rescue.</b> A cry carries sixty tiles, so the only
        // bots that hear one from a dungeon are its own party, whose company already turns on whatever hits any of them
        // (BotSquads.Note) — and a rescue walks, and no road runs down there. With delving opened to every member on
        // 25.09.2026 (build 228) the Orc Caves held three parties at a time, and all 35 rescues that failed in the
        // window 16:05–16:47 were down them, "no way through to an orc bomber at (5322, 1311, 0)" and the like.
        if (BotDungeon.Under(foe.Location))
        {
            BotDefender.Below++;

            return null;
        }

        // The same question the hunter asks before setting out, for the same reason: arriving at a fight that
        // is already lost turns one death into two. Being outnumbered where you stand is a different fact
        // from what is standing round the friend, and this is the one that stops the whole population walking
        // into the same pile one at a time.
        if (BotThreat.Decide(body, BotMobile.NoticeRange) == BotStand.Outmatched)
        {
            return null;
        }

        // <b>And the question that one cannot answer, which is why the sentence above it was already
        // hedged.</b> Weighing the odds where the bot stands says nothing whatever about the pile the friend
        // is standing in — the comment above admits as much and then measures the near one anyway, because at
        // the moment of choosing, the far one is not knowable. It becomes knowable the instant somebody walks
        // over and finds out, and that is exactly what the fight writes down when it gives up on the numbers.
        //
        // Without reading it, every bot in earshot repeats the same discovery in turn and then repeats it
        // again two seconds later: one creature drew 103 refusals in a single window while the hunt, which
        // does read the note, refused it 7 times. The note lapses in two minutes, so a genuine emergency is
        // only ever deferred, never abandoned.
        // <b>Except for a crier in real trouble (12.10.2026, w5).</b> The note is written by the crier itself the moment it judges
        // the thing beyond it (BotDefender: 462 such judgements in the night of 12.10, each shutting this door for two minutes on
        // the one cry that needed it open), and the question the note was standing in for — can the friend and this bot take it
        // together — is asked just below, where the bot will be standing. A steady crier's crowd is still left alone. Counted.
        if (BotQuarry.Crowded(foe) && !BotCry.IsDire(friend))
        {
            PassedCrowded++;

            return null;
        }

        // And the other list the fight writes: something no journey could reach is not helped by a second bot
        // walking at it from further away.
        if (BotQuarry.Shunned(foe))
        {
            return null;
        }

        // <b>The odds at the fight, asked before the walk.</b> The fight asks them on its first beat — "too many of
        // them around" — and nine rescues in a quarter of an hour on build 269 ended that way on arrival, each one a
        // walk spent to learn what stood round the foe when the bot chose. Asked round the friend, which is where the
        // rescuer will be standing, plus the rescuer itself; the crowd round the rescuer now is the wrong crowd.
        // The cry stays up: a crowd is what BotMuster calls companies for.
        if (BotThreat.OverrunThere(body, friend, foe, BotMobile.NoticeRange))
        {
            BotQuarry.Crowd(foe);
            Outmatched++;

            return null;
        }

        Once(body, friend);
        BotCry.Noted();
        BotCry.Answering(friend);

        var trains = bot.Bond?.Weapon?.Skill ?? SkillName.Wrestling;

        return new BotRescue(new BotSlay(foe, trains), friend, foe, own: false, rescuer: body);
    }

    private static void Once(Mobile body, Mobile friend)
    {
        if (_said)
        {
            return;
        }

        _said = true;

        // Said once, by name. "Nobody ever goes to help" and "help is offered and always outbid" look
        // identical in a log that says neither.
        logger.Information("{Name} is the first to go to somebody's aid: {Friend} called", body.Name, friend.Name);
    }

    public static void Forget()
    {
        _said = false;
        Outmatched = 0;
        PassedCrowded = 0;
        BotDefender.Forget();
    }
}

/// <summary>
/// Hitting back, for a bot that something is presently hitting.
///
/// <para>
/// <b>On the <c>Hunted</c> rung, which had nothing on it at all.</b> The rung exists to say that a bot has no
/// business shopping for work while something is chewing on it, and <c>BotWill</c> has been complaining into
/// the log that nobody proposes anything for it — so the bot simply held whatever it was doing. For a warrior
/// that hardly shows: the engine's own reflex swings back. For a healer it is the whole problem, because a
/// healer's work is to stand still over somebody, it has no combatant of its own, and it will do that until
/// it dies with a staff in its hands it never once raised.
/// </para>
///
/// <para>
/// It sits below <c>Failing</c> in the ladder, so a bot that is badly hurt still runs rather than turning to
/// fight. Fight back while you can afford to; run when you cannot.
/// </para>
/// </summary>
public sealed class BotDefender : IBotProposer
{
    /// <summary>How far around itself the bot looks for whatever is on it.</summary>
    public static int Reach { get; set; } = 12;

    /// <summary>
    /// <b>The stand, on the rung below as well (06.10.2026, react14).</b> This instance answers <c>Failing</c> and offers one
    /// thing: the fight carried on by a bot that is being killed and has nothing else on offer. See <see cref="Stand"/>.
    /// </summary>
    public BotDefender(bool last = false) => _last = last;

    private readonly bool _last;

    public string Name => _last ? "LastStand" : "Defender";

    public BotStanding Rung => _last ? BotStanding.Failing : BotStanding.Hunted;

    /// <summary>Whether a bot under the fighting floor with what is killing it on top of it is offered the fight to carry on. A switch.</summary>
    public static bool Stands { get; set; } = true;

    /// <summary>How near what is on the bot must stand for a stand: on top of it. A caster or an archer strikes from anywhere.</summary>
    public static int StandReach { get; set; } = 2;

    /// <summary>Fights carried on under the fighting floor by a bot with no flight on offer. Counted by the fight, when it first passes the floor.</summary>
    public static long StoodOn { get; set; }

    /// <summary>
    /// <b>A bot being killed decides: it runs, or it fights (06.10.2026, react14).</b>
    ///
    /// <para>
    /// Under <c>BotSlay.FleeAt</c> this rung offered nothing, on the stated rule "run when you cannot" — and the flight is
    /// <c>BotFugitive</c>'s to offer, which has seven reasons not to: what is on the bot is bearable against what it has
    /// left, its company near can take it, it is a caster, the bot has run its two runs, it is cornered, it has just got
    /// clear, there is nowhere to run. In every one of those the bot was left with neither, and <c>BotWill.Aside</c> then
    /// holds the ordinary auction for it "because a walk to a mine is a walk away": Isolt at 00:22:38 on 06.10, "a troll was
    /// winning", gleaned a bolt and ordered leather arms and was dead in ten seconds; Nerys at 00:24:05 ordered eighteen
    /// bandages between two fights with the ogre that killed it. On the stopwatch (build 401, 00:51) 7 of 23 bots that crossed
    /// the failing line decided nothing, and 5 of the 7 died.
    /// </para>
    ///
    /// <para>
    /// So the fight is offered here under the floor too, as a fight that is not given up for the bot's own health
    /// (<c>BotSlay.ToTheEnd</c>), and it is priced as any hitting back is: a flight on the same rung outbids it whenever one
    /// is offered, so nothing changes for a bot that can run. Only with the thing on top of the bot (<see cref="StandReach"/>)
    /// or striking from range; never in the seconds after it has got clear (<c>BotFugitive.Calm</c>).
    /// </para>
    ///
    /// <para>
    /// <b>The odds no longer refuse it, and the stand is the rung's default (12.10.2026, w5).</b> It refused a bot outmatched
    /// where it stood — "walking on beats standing" — and so did <c>BotSlay</c> on its first beat; but the stand is only ever
    /// asked for once the fight has been refused for the odds and no flight is offered, so a stand refused for the odds was a
    /// bot offered nothing at all. The counters already said what that came to and named the stand as the cure without
    /// taking it: on the night of 12.10 (23:18–03:48) 168 of 373 first blows that landed with the warning unused fell on a lone
    /// bot "on the rung with nothing it could take", 45 lone bots with a caster on them died under the failing line "fought to
    /// the end, by order" having never been offered the fight, 4 567 looks found a losing bot "with nowhere to run", 7 794 found
    /// one that "had run 2 times in 2 minutes and stands instead", 25 852 one whose worst was a caster — and the reflex at the
    /// blow keeps an outmatched bot's hands down (<c>BotBolt.HoldsSwing</c>), so such a bot walked its errand under the blows
    /// without a swing. Walking on beats standing only where walking on gets the bot away, and from a caster, a cornered
    /// place or a thing that has caught it twice it does not. On the rung above the floor the thing need not be on top: it has
    /// the bot for its combatant and is coming (<see cref="Reach"/>). A thing no road reaches is still left alone unless it is
    /// on top of the bot; on guarded ground the hands stay down and the guards are called (<c>BotHaven</c>). Nothing changes
    /// for a member of a company, whose company is its refuge.
    /// </para>
    /// </summary>
    private static BotDeed Stand(IBotWilful bot, Mobile body, BaseCreature foe, bool last)
    {
        if (!Stands || body.HitsMax <= 0 || foe == null)
        {
            return null;
        }

        var deed = bot.Resolve?.Deed;

        // Running already, or already in this fight. And never for a member of a company, whose fight is its company's, nor
        // over work that may not be bid away (BotDeed.Committed): the stand is pressing, and a hauler that stops to fight under
        // the floor has lost the one thing walking on was buying it.
        if (bot.Squad != null || deed is BotBolt || deed is { Committed: true } || ReferenceEquals(deed?.Foe, foe))
        {
            return null;
        }

        var onTop = body.InRange(foe.Location, StandReach);

        if (last && !onTop && foe.AI is not (AIType.AI_Mage or AIType.AI_Archer))
        {
            return null;
        }

        if (BotFugitive.Calm(body, bot.Resolve))
        {
            return null;
        }

        if (!onTop && BotQuarry.Shunned(foe))
        {
            StoodShunned++;

            return null;
        }

        if (BotHaven.Guarded(body.Map, body.Location))
        {
            StoodTown++;

            return null;
        }

        var trains = bot.Bond?.Weapon?.Skill ?? SkillName.Wrestling;

        return new BotRescue(new BotSlay(foe, trains) { ToTheEnd = true }, body, foe, own: true);
    }

    /// <summary>
    /// Stands taken (12.10.2026, w5): the fight carried on by a bot that was refused the fight for the odds and offered no
    /// flight; of them, against a caster, and above the bot's own flight share (the rung above the floor); the ones the bot
    /// died in; and stands not taken on guarded ground or for a thing beyond the roads that was not on top of the bot.
    /// Counted where the deed is taken (<c>BotRescue.Taken</c>), never where it is offered.
    /// </summary>
    public static long StoodTaken { get; private set; }

    public static long StoodCaster { get; private set; }

    public static long StoodHealthy { get; private set; }

    public static long StoodDied { get; set; }

    public static long StoodTown { get; private set; }

    public static long StoodShunned { get; private set; }

    /// <summary>Stands the odds would have ended on the fight's own beat; the muster is urged for them instead. See <c>BotSlay</c>.</summary>
    public static long StoodOnOdds { get; set; }

    /// <summary>A stand has been taken. Called by <c>BotRescue.Taken</c>.</summary>
    public static void Stood(Mobile body, Mobile foe)
    {
        StoodTaken++;

        if (BotSquad.Caster(foe))
        {
            StoodCaster++;
        }

        if (body is { HitsMax: > 0 } && body.Hits >= body.HitsMax * BotTraits.FleeAt(body, false))
        {
            StoodHealthy++;
        }
    }

    /// <summary>The stand, for the <c>Reflexes:</c> line.</summary>
    public static string DescribeStands() =>
        $"{StoodOn} fights carried on under {BotSlay.FleeAt:P0} of the bot's health by a bot with what was killing it on top of it and no flight on offer, "
        + $"{UnderOwnShare} looks at a bot struck between {BotSlay.FleeAt:P0} and its own flight share by its nature answered as under the floor rather than with a fight that gives itself up; "
        + $"{StoodTaken} stands taken for want of a flight by a bot refused the fight for the odds ({StoodCaster} against a caster, {StoodHealthy} above the bot's own flight share, {StoodOnOdds} that the odds would have ended on the fight's first beat, {StoodDied} died in them), {StoodTown} not taken on guarded ground (the guards called instead), {StoodShunned} not taken for a thing no road reaches that was not on top of the bot{(Stands ? "" : " (off)")}";

    /// <summary>
    /// Blows on a bot between the standing flight share and its own, leaned by its nature, answered as under the floor (the stand
    /// or the flight) and not with a fight that gives itself up on its first beat (07.10.2026, loops18). Looks, not bots.
    /// </summary>
    public static long UnderOwnShare { get; private set; }

    /// <summary>Blows not answered with an errand of their own, because the work in hand was already that fight.</summary>
    public static long Already { get; private set; }

    /// <summary>Blows not answered with a fight because the odds round the bot were already past calling one off. See <see cref="Propose"/>.</summary>
    public static long Outnumbered { get; private set; }

    /// <summary>Blows on a company's leader left to the company, which turns on whatever hits any of its members.</summary>
    public static long Leading { get; private set; }

    /// <summary>Times a bot set upon by a red of ours was offered the fight back. See <c>BotOutlaw</c>.</summary>
    public static long Assailed { get; private set; }

    /// <summary>Fights underground left to the delve's company, by the rescuer and here. See <see cref="BotRescuer"/>.</summary>
    public static long Below { get; set; }

    /// <summary>Blows answered with a run for a town, the bot outmatched where it stood. See <c>BotFugitive.ToTown</c>.</summary>
    public static long ToTown { get; private set; }

    public static void Forget()
    {
        UnderOwnShare = 0;
        Already = 0;
        Outnumbered = 0;
        Leading = 0;
        Assailed = 0;
        Below = 0;
        ToTown = 0;
        StoodOn = 0;
        StoodTaken = 0;
        StoodCaster = 0;
        StoodHealthy = 0;
        StoodDied = 0;
        StoodTown = 0;
        StoodShunned = 0;
        StoodOnOdds = 0;
    }

    public BotDeed Propose(IBotWilful bot)
    {
        var body = bot?.Self;
        var map = body?.Map;

        if (map == null || map == Map.Internal || !body.Alive)
        {
            return null;
        }

        // The failing rung's instance offers the stand and nothing else; a company's leader and a member underground leave it
        // to the company, as below, and nothing is counted twice. See Stand.
        if (_last)
        {
            if (body.HitsMax <= 0 || body.Hits >= body.HitsMax * BotTraits.FleeAt(body, false) || BotThreat.Hunter(body, Reach) is not { } on)
            {
                return null;
            }

            if (bot is IBotSquadMember { Squad: { } own } one && (ReferenceEquals(own.Leader, one) || BotDungeon.Under(body.Location)))
            {
                return null;
            }

            return Stand(bot, body, on, last: true);
        }

        // <b>A red of ours on this bot is answered like anything else that is killing it.</b> BotThreat counts
        // creatures only, so until 15.09.2026 a bot set upon by a robber swung back by reflex and went on with its
        // errand. See BotOutlaw: the fight back is a brawl, and it ends when the robber is down or breaks off.
        var assailant = BotOutlaw.Assailant(body, Reach);

        if (assailant != null && bot.Resolve?.Deed is not BotBrawl)
        {
            if (body.HitsMax > 0 && body.Hits < body.HitsMax * BotSlay.FleeAt)
            {
                return null;
            }

            Assailed++;

            var against = bot.Bond?.Weapon?.Skill ?? SkillName.Wrestling;

            return new BotBrawl(
                assailant,
                BotBrawl.Defence,
                against,
                (b, w) => assailant is not { Deleted: false, Alive: true }
                    ? $"{assailant?.Name} is down"
                    : !BotOutlaw.Assailing(assailant, b?.Self) && w.Elapsed > 15000
                        ? $"{assailant.Name} broke off"
                        : null
            );
        }

        var foe = BotThreat.Hunter(body, Reach);

        if (foe == null)
        {
            return null;
        }

        // <b>The leader of a company is not offered a fight of its own: its company is the fight.</b> On 16.09.2026 the
        // Baron, twenty-five minutes into a harrowing on ground the city had paid for, was stung by a scorpion; the
        // Hunted rung sits below Bound, this offer went through, the harrowing was dropped and six bots stood down. A
        // company already turns on whatever hits any member of it — see BotSquads.Note — so the leader taking its own
        // errand off to hit back is the one thing that undoes the company. Members are left as they were: their
        // errand is set aside under the company and comes back.
        if (bot is IBotSquadMember { Squad: { } company } member && ReferenceEquals(company.Leader, member))
        {
            Leading++;

            return null;
        }

        // And a member of a company underground leaves it to the company too: hitting back is a walk, and no road runs
        // down a dungeon. See BotRescuer.Propose.
        if (bot is IBotSquadMember { Squad: not null } && BotDungeon.Under(body.Location))
        {
            Below++;

            return null;
        }

        // <b>"Run when you cannot" was the stated rule and nothing anywhere enforced it.</b> The paragraph
        // above says this sits below Failing so that a badly hurt bot runs instead of turning to fight — but
        // that only decides which rung wins when both offer something, and this rung offered a fight at any
        // health at all. So a bot under the fighting floor was handed a fight, BotSlay looked at the same
        // health on its very next beat and abandoned it, and this proposed it again: Ilsa ran four complete
        // take-and-fail cycles inside two seconds, and the repetition penalty came off in hundredths while
        // the loop turned four times a second.
        //
        // Taken from BotSlay.FleeAt rather than written again, because the number that decides whether to
        // start a fight and the number that decides whether to stay in one are the same number. Written twice
        // they drift, and the gap between them is precisely where the loop lives.
        //
        // The cry is still raised below — being unable to fight back is the loudest reason to call for help.
        //
        // <b>The bot's own share, not the standing one (07.10.2026, loops18).</b> Since traits17 the fight gives itself up at the share
        // leaned by the bot's nature (BotTraits.FleeAt: a cautious bot at 44 %, a reckless one at 36 %), and this still asked the
        // standing 40 %. Two numbers on one shelf, and the loop lived between them exactly as the paragraph above says: Orlando, a
        // cautious WarriorMage at 40-44 % of his health, took "hitting back at a troll" 39 times in eight seconds at 11:47 on build
        // 418, and each failed on its first beat "a troll was winning". Between the two shares the bot is now under its own floor:
        // the stand or the flight, as below.
        var ownShare = BotTraits.FleeAt(body, false);

        if (body.HitsMax > 0 && body.Hits >= body.HitsMax * BotSlay.FleeAt && body.Hits < body.HitsMax * ownShare)
        {
            UnderOwnShare++;
        }

        if (body.HitsMax <= 0 || body.Hits < body.HitsMax * ownShare)
        {
            BotCry.Raise(body, foe, dire: true);

            // Not nothing: the fight carried on, which a flight on this rung outbids whenever one is offered. See Stand.
            return Stand(bot, body, foe, last: false);
        }

        // <b>The same note the rescuer reads, and leaving it out of here left half the loop standing.</b>
        // Teaching the rescuer to respect a crowded quarry silenced the bots who go to somebody else's aid
        // and did nothing whatever for the bot the crowd is actually standing on: this rung offers a healthy
        // bot the fight, the odds check throws it out on arrival, and this offers it again. Lysa turned that
        // circle thirty-seven times on one zombie inside eight minutes, several of them inside the same
        // second.
        //
        // Refusing is not standing idle. The cry goes up either way — a crowd is precisely what
        // <c>BotMuster</c> calls companies for, which is the whole reason a crowded thing is noted apart from
        // an unreachable one — and Failing outranks this rung the moment the bot is hurt enough to run. The
        // note lapses in two minutes, so this is a bot waiting for help rather than a bot giving up.
        if (BotQuarry.Crowded(foe) || BotQuarry.Shunned(foe))
        {
            BotCry.Raise(body, foe, dire: true);

            return null;
        }

        // <b>And the odds the fight itself asks on its first beat, asked before it is offered.</b> The note above is
        // written by the fight that failed, one creature at a time, so every new creature in a crowd costs a fight
        // taken and called off in the same second: on build 49, between 05:14 and 06:00 on 15.09.2026, 49 answers to a
        // blow ended "too many of them around" in the ledger's shortest span - Hollis seven times, Yarrow six, Hale
        // Ashdown five - most of them against plague spawns, which the beast keeps making, and some took a paused
        // errand down with them. The fight stands where the bot already stands, so this is the same question at the
        // same place (BotSlay: BotThreat.Decide at the notice range) and the same note written the same way.
        if (BotThreat.Decide(body, BotMobile.NoticeRange) == BotStand.Outmatched)
        {
            BotQuarry.Crowd(foe);
            Outnumbered++;
            BotCry.Raise(body, foe, dire: true);

            // <b>A bot that cannot win the fight runs for a town, when a town is a realistic run (02.10.2026, Patrick's
            // order).</b> Until now the refusal left the bot on its errand, taking the blows on the move; see BotFugitive.ToTown
            // and BotHaven for the engine's guards and what they will and will not do about what follows it.
            if (BotFugitive.ToTown(bot, body, foe) is { } run)
            {
                ToTown++;

                return run;
            }

            // <b>And where no town is a realistic run, the stand (12.10.2026, w5).</b> The refusal left the bot on its errand with
            // its hands down; the early flight on this rung outbids the stand whenever it is offered (BotBolt.Prior against
            // BotRescue.Steady), so this changes nothing for a bot that can run. See Stand.
            return Stand(bot, body, foe, last: false);
        }

        // Say so first, whatever comes of the fight. A bot that can handle this alone costs the population
        // nothing by having said it was in trouble; a bot that cannot is the reason anybody is listening.
        BotCry.Raise(body, foe);

        // <b>And not offered at all when the work in hand is this very fight.</b> A hunt is hit by its own quarry at
        // the first exchange, and hitting back at it is the hunt: offered here it outbid the hunt on this rung and
        // the hunt was dropped "interrupted by rescue" for the same fight under another trade's name. The cry above
        // still stands, so help still comes. See BotDeed.Foe.
        if (ReferenceEquals(bot.Resolve?.Deed?.Foe, foe))
        {
            Already++;

            return null;
        }

        var trains = bot.Bond?.Weapon?.Skill ?? SkillName.Wrestling;

        return new BotRescue(new BotSlay(foe, trains), body, foe, own: true);
    }
}
