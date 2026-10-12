using System;
using System.Collections.Generic;
using Server.Guilds;
using Server.Logging;
using Server.Mobiles;

namespace Server.BotAI.V2;

/// <summary>
/// What one guild thinks of another, and the two thresholds that turn an opinion into a war.
///
/// <para>
/// <b>Stage four of Patrick's order of 08.09.2026, and the one he deliberately left unbuilt</b> — his own
/// words: the only part that can make the population smaller. So it is built with the fighting behind a
/// switch that is off, and the opinions running and visible from the first minute. An evening of watching
/// guilds come to dislike each other, with nobody dying of it, is what says whether the numbers are right;
/// the switch is then one dial.
/// </para>
///
/// <para>
/// <b>A number per ordered pair, and it is only ever moved by something that happened.</b> Nothing here
/// decays towards a designed outcome and nothing drifts on a timer except the border, which is itself a
/// fact about the ground. Every mover is listed in <see cref="Describe"/> with its own count, so a war can
/// always be explained by what caused it rather than guessed at.
/// </para>
///
/// <para>
/// <b>Two thresholds and not one.</b> War is declared below <see cref="Enmity"/> and ended above
/// <see cref="Amity"/>, and the gap between them is the whole reason a war does not flicker on and off every
/// minute as one trade lands. That is the same shape the shard's other hysteresis has — see
/// <c>BotUnload</c>'s pair — and it is here for the same reason.
/// </para>
///
/// <para>
/// <b>The engine does the enforcing.</b> <c>Guild.AddEnemy</c> makes the other side
/// <c>Notoriety.Enemy</c>, which makes striking them lawful with no criminal flag and no guard reaction,
/// and makes their corpses lawful to loot. Nothing here touches karma, the criminal flag or the watch, and
/// no looting code is written: <c>BotPickings</c> already loots what a bot killed itself.
/// </para>
/// </summary>
public static class BotRegard
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotRegard));

    /// <summary>Whether guilds form opinions of each other at all.</summary>
    public static bool Running { get; set; } = true;

    /// <summary>
    /// Whether a bad enough opinion is allowed to become a war.
    ///
    /// <para>
    /// <b>On, by Patrick's order of 09.09.2026: "they should feel the consequences."</b> It was built off,
    /// because he had named this the one part of the plan that can shrink the population; it ran for an
    /// evening with the opinions moving and nothing dying of them, and the numbers behaved — a quarrel driven
    /// by trespass and by bots refusing to move along, mended by trade, reaching -94 of the -100 it takes.
    /// </para>
    ///
    /// <para>
    /// What to read now that it bites, and none of it can be read before: deaths an hour, skill lost per
    /// death, and how much of what was looted was ever used by whoever took it. Those are Patrick's own three
    /// from <c>PLAN-guild-lands.md</c>. Off again is one dial — <c>dial BotRegard.Warring false</c> through
    /// Argus, or <c>"War": false</c> in <c>bot-estate.json</c> — and a war already declared ends by itself
    /// when the two guilds trade their way back above <see cref="Amity"/>.
    /// </para>
    /// </summary>
    public static bool Warring { get; set; } = true;

    /// <summary>
    /// Below this, one guild declares war on another.
    ///
    /// <para>
    /// <b>Forty rather than a hundred, and the hundred was never reached by anything.</b> Measured over the
    /// eight-hour session of 10.09.2026: eleven pieces of work finished on somebody else's land, one refusal
    /// to move along, four killings and twelve border drifts, spread over fourteen pairs of guilds - and the
    /// worst opinion anybody held of anybody was <b>-36</b>. So the war threshold was not a high bar, it was
    /// a bar in a different room; <c>Declared</c> read nought and <c>BotRegard.AtWar</c> had no caller in the
    /// assembly to notice. A threshold and the rate that has to reach it are one decision and were set on
    /// different days, which is this project's commonest defect and its own note in the memory.
    /// </para>
    ///
    /// <para>
    /// Forty means the pair that already grinds hardest against its neighbour goes to war roughly once in a
    /// long session, and the rest do not. If that turns out to be too often, this is the number to move -
    /// and the number to read beside it is <c>Declared</c> against <c>BotQuarrel.Felled</c>: wars declared
    /// with no blows struck means the war is reachable and the fighting is not.
    /// </para>
    /// </summary>
    public static double Enmity { get; set; } = -40.0;

    /// <summary>And above this, the war ends. The gap is what stops it flickering.</summary>
    public static double Amity { get; set; } = -15.0;

    /// <summary>As low and as high as an opinion may go, so one bad afternoon cannot be past mending.</summary>
    public static double Floor { get; set; } = -200.0;

    /// <summary>And the ceiling. Friendship does not accumulate for ever either.</summary>
    public static double Ceiling { get; set; } = 100.0;

    // ---- What moves an opinion. Every one of these has a bucket, because a war nobody can explain is a war
    // nobody can tune. ------------------------------------------------------------------------------------

    /// <summary>What finishing a piece of work on another guild's land costs, each time.</summary>
    public static double Trespass { get; set; } = -0.5;

    /// <summary>
    /// What being told to move along and not going costs.
    ///
    /// Twelve rather than six. It is the most personal thing that happens between two guilds here - one bot
    /// walked over, said the words, and was ignored to its face - and it was worth a fifth of what a border
    /// happening to be nearby is worth over an hour. Patrick watched exactly this on 10.09.2026 and asked
    /// why nothing came of it.
    /// </summary>
    public static double Defiance { get; set; } = -12.0;

    /// <summary>
    /// What laying claim to a square they hold costs.
    ///
    /// Written as "twenty: about half a killing, so two claims against the same guild is roughly a war" when the line
    /// for war stood at forty; the estate's file has put it at a hundred since, so it took five claims, and under a
    /// day's truce and a day between declarations nobody fought for anything. <b>Fifty from 25.09.2026, Patrick's order:
    /// "it is a disgrace that they simply take lands from each other in turns; they must feel the consequences and
    /// actively fight for their lands"</b> — two claims against the same guild are a war again. A claim on a square by
    /// the holder's hall costs <see cref="HallClaim"/>. <c>BotFeud</c> needs a war before a blow may land, and this is
    /// what produces one. See <c>BotClaim</c>.
    /// </summary>
    public static double Claim { get; set; } = -50.0;

    /// <summary>
    /// What laying claim to a square by the holder's hall costs — its own or one next to it (<c>BotClaim.Important</c>):
    /// the war line at once. Taking that square moves the hall and wipes out the guild's standing (Patrick, 24.09.2026),
    /// and nobody stands for that with a grievance at half a war.
    /// </summary>
    public static double HallClaim { get; set; } = -100.0;

    /// <summary>Opinions worsened by a claim on ground the other guild holds.</summary>
    public static long Claims { get; private set; }

    /// <summary>What killing one of theirs costs.</summary>
    public static double Blood { get; set; } = -25.0;

    /// <summary>
    /// What two halls standing on top of each other costs, per drift, before distance softens it.
    ///
    /// <para>
    /// <b>Four, and the first cut of one was measured wrong within five minutes.</b> At one a drift, against
    /// trade at two a sale both ways, the first reading was 22 trades against 12 drifts — so every opinion on
    /// the island was rising and the whole of stage four was inert: a system that can only ever end in peace
    /// is not a system, it is a decoration. The two numbers had never been put beside each other.
    /// </para>
    ///
    /// <para>
    /// At four, softened by distance, the closest pair of halls on the island loses about thirty-seven an
    /// hour and reaches war in something under three hours of shard time. That is slow enough to be a
    /// quarrel rather than a switch and fast enough that an evening shows one.
    /// </para>
    /// </summary>
    public static double Border { get; set; } = -4.0;

    /// <summary>
    /// How near two halls must be for their guilds to count as neighbours at all.
    ///
    /// <para>
    /// Twice the reach of a land claim, and taken from it rather than chosen: two guilds are neighbours
    /// exactly when their yards touch, which is a fact about <see cref="BotLand.Reach"/> and not a second
    /// number to keep in step with it. Moving the claim moves this.
    /// </para>
    /// </summary>
    public static int Neighbouring => BotLand.Reach * 2;

    /// <summary>How often the border drift is applied, in milliseconds.</summary>
    public static int DriftMs { get; set; } = 300000;

    /// <summary>
    /// What trading with them is worth. The only thing that mends an opinion.
    ///
    /// Half, and small on purpose: a sale is one bot buying one thing, it happens a few times a minute
    /// across the island, and at two it drowned every other mover put together. Mending a quarrel should
    /// take a trading relationship rather than a transaction.
    /// </summary>
    public static double Trade { get; set; } = 0.5;

    // ---- What mends an opinion besides trade, and what an alliance is. Patrick's order of 13.09.2026:
    // relations improve through joint expeditions and mutual aid. ---------------------------------------

    /// <summary>
    /// What one corpse divided between members of two guilds is worth to each guild's opinion of the other.
    ///
    /// Two: a company that hunts together and shares the spoils is the joint expedition the order names,
    /// and a night of it — thirty corpses — is an alliance. Four times a trade, because a trade is a bot
    /// buying a thing and this is a bot standing in a line beside another under something with teeth.
    /// </summary>
    public static double Comradeship { get; set; } = 0.5;

    /// <summary>
    /// How many allies one guild may have. One: the first half hour of 13.09.2026 at two a corpse made four
    /// alliances out of five guilds, and an island where everybody is everybody's ally is an island with no
    /// wars and no allies either. Blocs of two are what an alliance means.
    /// </summary>
    public static int MostAllies { get; set; } = 1;

    /// <summary>What a member of one guild rescuing a member of another is worth, both ways.</summary>
    public static double Aid { get; set; } = 5.0;

    /// <summary>
    /// At or above this, both ways, two guilds are allies: the engine's own alliance, so their members read
    /// as allies to each other's notoriety, and when one goes to war the other joins it (see BotWar.Declare).
    /// </summary>
    public static double Alliance { get; set; } = 60.0;

    /// <summary>And below this, either way, an alliance ends. The gap is what stops it flickering.</summary>
    public static double Estrangement { get; set; } = 20.0;

    /// <summary>Corpses shared between guilds, rescues across guilds, alliances made and ended.</summary>
    public static long Comradeships { get; private set; }

    public static long Aids { get; private set; }

    public static long Allied { get; private set; }

    public static long Estranged { get; private set; }

    /// <summary>Members of <paramref name="one"/> and <paramref name="other"/> divided a corpse between them.</summary>
    public static void Comraded(string one, string other)
    {
        if (one == null || other == null || one == other)
        {
            return;
        }

        Comradeships++;
        Move(one, other, Comradeship, "a corpse shared");
        Move(other, one, Comradeship, "a corpse shared");
    }

    /// <summary>Somebody of <paramref name="helper"/> came to the rescue of somebody of <paramref name="helped"/>.</summary>
    public static void Helped(string helper, string helped)
    {
        if (helper == null || helped == null || helper == helped)
        {
            return;
        }

        Aids++;
        Move(helped, helper, Aid, "help in a fight");
        Move(helper, helped, Aid / 2.0, "help given");
    }

    /// <summary>Whether these two guilds are allies, asked of the engine. Never on a hot path.</summary>
    public static bool AreAllied(string one, string other) =>
        one != null && other != null && one != other
        && BaseGuild.FindByName(one) is Guild a && BaseGuild.FindByName(other) is Guild b && a.IsAlly(b);

    /// <summary>Opinions worsened by somebody working on land that was not theirs.</summary>
    public static long Trespasses { get; private set; }

    /// <summary>Opinions worsened by somebody refusing to move.</summary>
    public static long Defiances { get; private set; }

    /// <summary>
    /// Killings between two guilds already at war, which move no opinion.
    ///
    /// Its own bucket beside <c>Bloodshed</c>, so the two can be read as a pair: blood that made a war and
    /// blood that <em>is</em> one. A shard where the second is large and the first is nought is a shard whose
    /// wars start themselves.
    /// </summary>
    public static long Expected { get; private set; }

    /// <summary>Opinions worsened by a killing.</summary>
    public static long Bloodshed { get; private set; }

    /// <summary>Opinions worsened for sharing a border.</summary>
    public static long Borders { get; private set; }

    /// <summary>Opinions mended by trade.</summary>
    public static long Trades { get; private set; }

    /// <summary>Wars declared.</summary>
    public static long Declared { get; private set; }

    /// <summary>Wars ended.</summary>
    public static long Ended { get; private set; }

    /// <summary>Times a war would have been declared and the switch was off.</summary>
    public static long Withheld { get; private set; }

    private static readonly Dictionary<(string Of, string For), double> _regard = [];

    /// <summary>Scratch for the mending pass. See <see cref="Drift"/>.</summary>
    private static readonly List<(string Of, string For, double Held)> _mending = [];

    private static readonly List<(string Of, string For, double Held)> _grieving = [];

    /// <summary>Drifts on which a standing grievance asked the war's rules and was told to wait.</summary>
    public static long Waiting { get; private set; }

    /// <summary>Wars declared on a drift, by a grievance that had been refused at its crossing.</summary>
    public static long Overdue { get; private set; }

    private static long _drifted;

    /// <summary>Whether the drift has ever run. A tick count of nought is a legitimate reading; see rule 20.</summary>
    private static bool _everDrifted;

    /// <summary>What <paramref name="of"/> thinks of <paramref name="about"/>. Nought between strangers.</summary>
    public static double Of(string of, string about)
    {
        if (of == null || about == null || of == about)
        {
            return 0.0;
        }

        return _regard.TryGetValue((of, about), out var held) ? held : 0.0;
    }

    /// <summary>The same, of two guilds.</summary>
    public static double Of(Guild of, Guild about) => Of(of?.Name, about?.Name);

    /// <summary>Every opinion held, as (of, about) and its value. For the store.</summary>
    public static IEnumerable<KeyValuePair<(string Of, string For), double>> Opinions => _regard;

    /// <summary>
    /// An opinion read back off the save. See <c>BotWarStore</c>.
    ///
    /// <para>
    /// <b>Until 04:39 on 14.09.2026 a restart was a reconciliation.</b> Every opinion started at nought
    /// again, so every standing war was above the peace line the moment the shard came up, and the
    /// Crown–Hammer war "ended in peace" at fifteen minutes with the Hammer exiled on three dead to two.
    /// </para>
    /// </summary>
    public static void Restore(string of, string about, double value)
    {
        if (!string.IsNullOrEmpty(of) && !string.IsNullOrEmpty(about) && of != about)
        {
            _regard[(of, about)] = value;
        }
    }

    /// <summary>
    /// Moves an opinion, and asks afterwards whether it has crossed either threshold.
    ///
    /// <para>
    /// Both ways round, because an opinion is between two guilds and not one: a bot of the Blade digging on
    /// the Crown's land is a thing the Crown minds, and it is the Crown's opinion of the Blade that moves.
    /// Only that one. The Blade has no opinion about having been somewhere.
    /// </para>
    /// </summary>
    public static void Move(string of, string about, double by, string why)
    {
        if (!Running || of == null || about == null || of == about || by == 0.0)
        {
            return;
        }

        var was = Of(of, about);
        var now = Math.Clamp(was + by, Floor, Ceiling);

        if (Math.Abs(now - was) < 0.0001)
        {
            return;
        }

        _regard[(of, about)] = now;

        // The same move written under its cause, so an envoy can state the grievance and the duke judge it (29.09.2026).
        BotGrievances.Noted(of, about, now - was, why);

        Reckon(of, about, was, now, why);
    }

    /// <summary>Somebody of <paramref name="who"/> finished a piece of work on <paramref name="whose"/> land.</summary>
    public static void Trespassed(string who, string whose)
    {
        if (who == null || whose == null || who == whose)
        {
            return;
        }

        Trespasses++;
        Move(whose, who, Trespass, "working our land");
    }

    /// <summary>Somebody of <paramref name="who"/> was told to move along and did not.</summary>
    public static void Defied(string who, string whose)
    {
        if (who == null || whose == null || who == whose)
        {
            return;
        }

        Defiances++;
        Move(whose, who, Defiance, "refusing to move along");
    }

    /// <summary>Somebody of <paramref name="who"/> laid claim to a square <paramref name="whose"/> holds.</summary>
    public static void Claimed(string who, string whose, bool byTheHall = false)
    {
        if (who == null || whose == null || who == whose)
        {
            return;
        }

        Claims++;

        if (byTheHall)
        {
            Move(whose, who, Math.Min(HallClaim, Enmity - Of(whose, who) - 1.0), "claiming the ground by our hall");

            return;
        }

        Move(whose, who, Claim, "claiming our ground");
    }

    /// <summary>Somebody of <paramref name="killer"/> killed a member of <paramref name="fallen"/>.</summary>
    public static void Killed(string killer, string fallen)
    {
        if (killer == null || fallen == null || killer == fallen)
        {
            return;
        }

        Bloodshed++;

        // Counted for the war as well as for the opinion, and they are two different books: the opinion is
        // what starts a war, this is what decides who won it. See BotWar, which counts only inside one.
        BotWar.Killed(killer, fallen);

        // <b>A killing inside a war moves no opinion, and without this nothing a war does can ever end
        // it.</b> Blood is -25 and a trade is +0.5, so one death costs fifty trades between exactly that
        // pair; by the second hour of 10.09.2026 the worst pair sat at -200, the floor of the scale, with
        // 88 killings behind it and no arithmetic that could climb back to the -15 that makes peace. A war
        // that cannot end is a war whose result can never be judged — see BotExile, where the whole point of
        // ending one is that somebody has lost it. Blood before a war is the reason for the war; blood
        // during one is the war happening, and it is already what decides who wins.
        if (BaseGuild.FindByName(fallen) is Guild ours && BaseGuild.FindByName(killer) is Guild theirs &&
            ours.IsWar(theirs))
        {
            Expected++;

            return;
        }

        Move(fallen, killer, Blood, "blood");
    }

    /// <summary>Somebody of <paramref name="buyer"/> bought from or filled an order of <paramref name="seller"/>.</summary>
    public static void Traded(string buyer, string seller)
    {
        if (buyer == null || seller == null || buyer == seller)
        {
            return;
        }

        Trades++;
        Move(seller, buyer, Trade, "trade");
        Move(buyer, seller, Trade, "trade");
    }

    /// <summary>
    /// The slow souring between neighbours, applied on a timer rather than by an event.
    ///
    /// <para>
    /// This is the one mover with no incident behind it, and it is here because without it nothing ever
    /// happens: two guilds whose members never meet have no reason to quarrel, and quarrels between guilds
    /// at opposite ends of the island would be arbitrary. Sharing a border is the reason, and it is a fact
    /// about the ground rather than a number anybody chose.
    /// </para>
    /// </summary>
    public static void Drift()
    {
        if (!Running || Border == 0.0)
        {
            return;
        }

        var now = Core.TickCount;

        if (_everDrifted && now - (_drifted + DriftMs) < 0)
        {
            return;
        }

        _drifted = now;
        _everDrifted = true;

        // Time itself, first, on every opinion there is. See Mend. Collected before moving anything, because
        // Move writes the dictionary being read.
        if (Mend > 0.0)
        {
            _mending.Clear();

            foreach (var (pair, held) in _regard)
            {
                if (held != 0.0)
                {
                    _mending.Add((pair.Of, pair.For, held));
                }
            }

            for (var i = 0; i < _mending.Count; i++)
            {
                var (of, about, held) = _mending[i];
                var step = held < 0.0 ? Math.Min(Mend, -held) : -Math.Min(Mend, held);

                // <b>A seizure is not forgotten in an evening</b> (Patrick, 06.10.2026: "a risky step with a great price"). While
                // the guild thought of is remembered for one, time lifts no opinion of it above its ceiling; trade and a meeting
                // still can. See BotSeizure.Ceiling.
                if (held < 0.0 && BotSeizure.Ceiling(about, out var ceiling) && held + step > ceiling)
                {
                    step = Math.Max(0.0, ceiling - held);
                    BotSeizure.Kept();

                    if (step <= 0.0)
                    {
                        continue;
                    }
                }

                Mended++;
                Move(of, about, step, "time");
            }
        }

        // <b>A grievance that was refused a war keeps asking.</b> The war branch in Reckon fires on the
        // crossing of the line and never again, so a pair refused for the once-a-day clock at the crossing
        // — the Crown of the Lantern at 04:52 on 14.09.2026, "declared one 31 minutes ago" — stood at −178
        // for the rest of the night with nobody asking twice. Every drift, every pair held at or below the
        // line and not at war puts the question again; the rules answer, and the counters say how often.
        if (Warring)
        {
            _grieving.Clear();

            foreach (var (pair, held) in _regard)
            {
                if (held <= Enmity)
                {
                    _grieving.Add((pair.Of, pair.For, held));
                }
            }

            for (var i = 0; i < _grieving.Count; i++)
            {
                var (of, about, held) = _grieving[i];

                if ((BotGuilds.Named(of) ?? BaseGuild.FindByName(of) as Guild) is not Guild mine
                    || (BotGuilds.Named(about) ?? BaseGuild.FindByName(about) as Guild) is not Guild theirs
                    || mine.IsWar(theirs))
                {
                    continue;
                }

                if (!BotWar.MayDeclare(of, about, out _))
                {
                    Waiting++;

                    continue;
                }

                // No war without a meeting (Patrick, 29.09.2026): the grievance calls one at the other guild's town, or waits
                // for the one already called, or for the duke's last word between them to run out. See BotParley.Grieve.
                if (BotParley.Grieve(mine, theirs, $"a standing grievance (regard {held:F1})"))
                {
                    continue;
                }

                if (mine.IsAlly(theirs))
                {
                    mine.RemoveAlly(theirs);
                    Estranged++;
                }

                BotWar.Declare(mine, theirs, $"a standing grievance (regard {held:F1})");
                Declared++;
                Overdue++;
            }
        }

        foreach (var (mine, ours) in BotEstate.Held)
        {
            if (ours is not { Deleted: false })
            {
                continue;
            }

            foreach (var (theirs, yours) in BotEstate.Held)
            {
                if (mine == theirs || yours is not { Deleted: false } || yours.Map != ours.Map)
                {
                    continue;
                }

                var gap = Math.Max(Math.Abs(ours.X - yours.X), Math.Abs(ours.Y - yours.Y));

                if (gap >= Neighbouring)
                {
                    continue;
                }

                // <b>How much their yards actually overlap, not merely that they do.</b> A flat penalty for
                // every pair within reach makes an island of four halls one quarrel — every guild at war
                // with every other within the same three hours, which is not neighbours falling out, it is
                // a scheduled event. Scaled, the two halls eighteen tiles apart lose four times as much a
                // drift as the two seventy-six apart, and the shard produces a rivalry rather than a war.
                Borders++;
                Move(mine, theirs, Border * (1.0 - gap / (double)Neighbouring), "a shared border");
            }
        }
    }

    /// <summary>
    /// Whether these two guilds are at war, asked of the engine rather than of the number above.
    ///
    /// The opinion decides when to declare; the engine holds whether it is declared. Keeping a second copy
    /// of that here is how a bot ends up striking somebody the engine still calls an ally.
    /// </summary>
    public static bool AtWar(Guild a, Guild b) => a != null && b != null && a != b && a.IsWar(b);

    /// <summary>Whether these two bots' guilds are at war.</summary>
    public static bool AtWar(Mobile a, Mobile b) => AtWar(a?.Guild as Guild, b?.Guild as Guild);

    /// <summary>
    /// Whether two bots' guilds are hostile: at war, or either thinks of the other at or below <see cref="Enmity"/>, the war
    /// line. Nobody is hostile to its own guild or to a bot of none. Patrick, 30.09.2026 evening, on delves: «everybody goes
    /// with everybody, only hostile guilds may not go together» — everybody goes with everybody, only hostile guilds do not.
    /// </summary>
    public static bool Hostile(Mobile a, Mobile b)
    {
        if (a?.Guild is not Guild ga || b?.Guild is not Guild gb || ga == gb)
        {
            return false;
        }

        return AtWar(ga, gb) || Of(ga, gb) <= Enmity || Of(gb, ga) <= Enmity;
    }

    private static void Reckon(string of, string about, double was, double now, string why)
    {
        if (BaseGuild.FindByName(of) is not Guild mine || BaseGuild.FindByName(about) is not Guild theirs)
        {
            return;
        }

        var warring = mine.IsWar(theirs);

        // Friends, before enemies: an opinion that has climbed to the alliance line, and is met from the
        // other side, makes allies of the two; one that has fallen below the estrangement line ends it.
        if (!warring)
        {
            if (!mine.IsAlly(theirs) && now >= Alliance && Of(about, of) >= Alliance
                && (mine.Allies?.Count ?? 0) < MostAllies && (theirs.Allies?.Count ?? 0) < MostAllies)
            {
                // Sworn at a meeting, not made by a number (Patrick, 29.09.2026, point 2). See BotParley.Befriend.
                if (BotParley.Befriend(mine, theirs, $"{why} (regard {now:F1} and {Of(about, of):F1})"))
                {
                    return;
                }

                mine.AddAlly(theirs);
                Allied++;

                logger.Warning("{Mine} and {Theirs} are allies: regard {Now:F1} and {Back:F1} over {Why}", of, about, now, Of(about, of), why);
            }
            else if (mine.IsAlly(theirs) && now < Estrangement)
            {
                mine.RemoveAlly(theirs);
                Estranged++;

                logger.Warning("{Mine} is no longer allied with {Theirs}: regard fell to {Now:F1} over {Why}", of, about, now, why);
            }
        }

        if (!warring && now <= Enmity && was > Enmity)
        {
            if (!Warring)
            {
                Withheld++;

                return;
            }

            if (mine.IsAlly(theirs))
            {
                mine.RemoveAlly(theirs);
                Estranged++;
            }

            // <b>The opinion says when; the war ledger says whether.</b> A truce, a declaration too soon
            // after the last, or a guild already fighting somebody all refuse it, each in its own bucket
            // over there. The opinion stays where it fell, so the war is declared the moment the rule
            // lifts — unless something mends it first, which is what the truce is for.
            if (!BotWar.MayDeclare(of, about, out var refused))
            {
                Refused++;

                logger.Information(
                    "{Mine} would declare war on {Theirs} (regard {Now:F1} over {Why}) but for {Refused}",
                    of,
                    about,
                    now,
                    why,
                    refused
                );

                return;
            }

            // <b>No war without a meeting (Patrick, 29.09.2026).</b> The crossing calls one at the other guild's town, and the
            // war — or a peace, or terms — comes of the duke's word there. A pair already meeting, or whose last word still
            // stands, waits; the drift asks again. Declared here only when meetings are off. See BotParley.
            if (BotParley.Grieve(mine, theirs, $"{why} (regard {now:F1})"))
            {
                return;
            }

            BotWar.Declare(mine, theirs, $"{why} (regard {now:F1})");
            Declared++;

            return;
        }

        // <b>Peace takes both opinions and a war old enough to have been one.</b> The first cut ended a war
        // when either side's opinion climbed above Amity — and the side that never hated anybody was above
        // it already, so one trade landing on the wrong pair ended a war seven seconds after it was declared.
        if (warring && now >= Amity && Of(about, of) >= Amity && BotWar.MayPeace(of, about))
        {
            Ended++;

            logger.Information("{Mine} is at peace with {Theirs} again: regard back up to {Now:F1}", of, about, now);

            // A war that is over has a loser, and on this island losing costs ground. Patrick's order of
            // 10.09.2026: see BotWar.Peace, which judges it and sentences through BotExile.
            BotWar.Peace(of, about);
        }
    }

    /// <summary>Declarations the opinion called for and the war ledger refused. See <c>BotWar.MayDeclare</c>.</summary>
    public static long Refused { get; private set; }

    /// <summary>Pairs whose opinions were put back to nought because a war between them ended.</summary>
    public static long Settled { get; private set; }

    /// <summary>Opinions moved a step towards nought by nothing but time. See <see cref="Mend"/>.</summary>
    public static long Mended { get; private set; }

    /// <summary>
    /// What five minutes of nothing happening is worth, towards nought, on every opinion.
    ///
    /// <para>
    /// <b>One, and it exists because the floor was where every opinion ended up.</b> The drift was written
    /// to sour neighbours and nothing was written to let anybody forget: with a refusal to move along at
    /// twelve and a trade at half, every pair on the island reached -200 within the hour on 13.09.2026 and
    /// stayed there for eleven more. A grievance that never fades is a war that never ends. This is small
    /// on purpose — a dozen an hour against a single defiance at twelve — so it decides nothing while
    /// anything is actually happening between two guilds and everything once nothing is.
    /// </para>
    /// </summary>
    public static double Mend { get; set; } = 1.0;

    /// <summary>
    /// A war between these two is over and the quarrel with it: both opinions go back to nought.
    ///
    /// Written straight into the register rather than through <see cref="Move"/>, because a settlement is
    /// not an event to be reckoned — nothing about it should be able to declare or end anything.
    /// </summary>
    public static void Settle(string one, string other)
    {
        if (one == null || other == null || one == other)
        {
            return;
        }

        _regard.Remove((one, other));
        _regard.Remove((other, one));
        Settled++;

        // And the books of the quarrel closed: what has been settled is not brought before the duke again. See BotGrievances.
        BotGrievances.Settled(one, other);
    }

    /// <summary>The worst opinion anybody holds, for the summary.</summary>
    private static string Worst()
    {
        string worst = null;
        var lowest = 0.0;

        foreach (var ((of, about), held) in _regard)
        {
            if (held < lowest)
            {
                lowest = held;
                worst = $"{of} of {about} at {held:F0}";
            }
        }

        return worst ?? "nobody thinks ill of anybody";
    }

    public static string Describe() =>
        !Running
            ? "guilds form no opinions of each other"
            : $"{_regard.Count} opinions between guilds, worst {Worst()}; moved by {Trespasses} pieces of work on somebody else's land, "
              + $"{Defiances} refusals to move along, {Bloodshed} killings ({Expected} more between guilds already at war, which move nothing), {Borders} border drifts, {Claims} claims on somebody's ground, {Trades} trades, {Comradeships} corpses shared between guilds, {Aids} rescues across guilds and {Mended} steps of forgetting at {Mend:F1} a drift; {Allied} alliances made at {Alliance:F0} and {Estranged} ended below {Estrangement:F0}; "
              + $"{Declared} wars declared, {Refused} called for and refused by the war's rules, {Waiting} drifts a standing grievance waited on them and {Overdue} wars it got when they allowed, {Ended} ended in peace, {Settled} pairs settled to nought by a war ending"
              + (Warring ? "" : $", and {Withheld} withheld because war is switched off");

    public static void Forget()
    {
        _regard.Clear();
        _drifted = 0;
        _everDrifted = false;
        Trespasses = 0;
        Defiances = 0;
        Bloodshed = 0;
        Expected = 0;
        Claims = 0;
        Borders = 0;
        Trades = 0;
        Declared = 0;
        Ended = 0;
        Withheld = 0;
        Refused = 0;
        Waiting = 0;
        Overdue = 0;
        Settled = 0;
        Mended = 0;
        Comradeships = 0;
        Aids = 0;
        Allied = 0;
        Estranged = 0;
        _mending.Clear();
    }
}
