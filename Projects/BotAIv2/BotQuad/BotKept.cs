using System.Diagnostics;
using Server.Logging;
using Server.Mobiles;

namespace Server.BotAI.V2;

/// <summary>
/// What the island's spawners keep alive near a place, reckoned as a fight and set beside what the square asks and what a
/// bot brought there — counted and written down, and nothing refused. §5 S8 of DECISIONS.md, in shadow.
///
/// <para>
/// <b>The ground map counts heads, and only as fresh as the last look.</b> A square asks strength of whoever goes there
/// (<see cref="BotQuad.Muscle(Map, Point3D)"/>) from its earned reading less a tenth for each hostile creature somebody
/// standing in it counted in the last five minutes (<see cref="BotQuad.MobWorth"/>, <see cref="BotQuad.SightMs"/>). So an
/// ogre counts as a mongbat, and a square nobody has stood in lately counts nothing at all: the three spawners east of
/// Britain at (1969, 1409), (1986, 1463) and (1983, 1517) keep ettins, gargoyles, gazers, ogres, trolls and water elementals,
/// seven of each, with a walking range of thirty, and at 23:01 on 16.09.2026 the square at (1998, 1494) read +0.08 and asked
/// nothing while seven bots died there in two minutes. The spawners are known at every boot (<see cref="BotLairs"/>), what
/// they keep alive is in their entries, and <see cref="BotThreat.Power"/> speaks the units a square asks in.
/// </para>
///
/// <para>
/// <b>Measured before it is enforced, because it would move the fear of the whole map at once.</b> Every prowl, hunt and
/// sweep refusal reads the same number. Decided by Claude on Patrick's word of 17.09.2026 ("as you consider necessary"):
/// first how often the dead fell where the spawners kept more than they brought while the square asked no more, and how
/// much of the island such a rule would close to a bot alone; then whether to make it a wall.
/// </para>
/// </summary>
public static class BotKept
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotKept));

    public static bool Running { get; set; } = true;

    /// <summary>
    /// How far a bot is reckoned to meet what a spawner keeps: twelve tiles, about the range at which an aggressive creature
    /// notices somebody.
    ///
    /// <para>
    /// <b>Build 103 summed every creature a spawner keeps over all its ground, and closed a third of the island to a
    /// company.</b> Its first survey, at 11:34 on 17.09.2026: of 4942 squares outside the walls and the dungeons, 1929 kept
    /// more than a lone bot of 1116 near them, 1639 more than a company of five, 1330 more than two, the most 161958 at
    /// (1515, 2565) — while at the three spawners that killed seven bots on 16.09 the door found nothing alive within
    /// fourteen tiles. A spawner lets what it keeps wander a box its walking range wide, and a bot meets what is near; from
    /// build 104 each lair counts by the share of its ground in sight (<see cref="BotLairs.MetFight"/>), and the wide sum
    /// is printed beside it.
    /// </para>
    /// </summary>
    public static int Sight { get; set; } = 12;

    /// <summary>The strength of one ordinary bot, the reference figure in <see cref="BotThreat"/>'s notes. For the survey.</summary>
    public static double LoneBot { get; set; } = 1116;

    /// <summary>How often the whole island is reckoned for the summary. Half an hour.</summary>
    public static int SurveyEveryMs { get; set; } = 1800000;

    /// <summary>Deaths reckoned.</summary>
    public static long Deaths { get; private set; }

    /// <summary>Deaths where the spawners near kept a fight larger than the strength the dead bot's company brought.</summary>
    public static long Outmatched { get; private set; }

    /// <summary>Of <see cref="Outmatched"/>, the deaths where the square itself asked no more than that company brought.</summary>
    public static long Unwarned { get; private set; }

    private static long _surveyedTick;

    private static bool _surveyed;

    private static string _survey = "the island not yet reckoned";

    /// <summary>
    /// Deaths where the spawners near keep nothing alive: what killed the bot wandered there, off every map the population
    /// chooses its ground by (03.10.2026). See <see cref="Fell"/>.
    /// </summary>
    public static long Wandered { get; private set; }

    /// <summary>Of <see cref="Wandered"/>, the deaths in a company, and the deaths to something reckoned past what the company brought.</summary>
    public static long WanderedInCompany { get; private set; }

    public static long WanderedBeyond { get; private set; }

    /// <summary>
    /// A bot has fallen: what it brought, what the square asked and what the spawners near kept, in one line.
    /// <paramref name="fellIn"/> is the company it fell in, taken before its work was settled (<c>BotMobile.OnDeath</c>): an
    /// enlisted member is off the roster by the time this is asked, and until 03.10.2026 every one of them read "a company of 1".
    /// </summary>
    public static void Fell(BotMobile bot, Map map, Point3D where, BotSquad fellIn = null)
    {
        if (!Running || bot == null || map == null || map == Map.Internal)
        {
            return;
        }

        // As it stood before the engine stripped the body, not the ghost's fists (03.10.2026). See BotMobile.PowerAtDeath.
        var strength = bot.PowerAtDeath > 0.0 ? bot.PowerAtDeath : BotThreat.Power(bot);
        var company = 1;

        if ((fellIn ?? (bot as IBotSquadMember)?.Squad) is { } squad)
        {
            var members = squad.Members;

            for (var i = 0; i < members.Count; i++)
            {
                if (members[i]?.Self is not { Deleted: false, Alive: true } self || self == bot)
                {
                    continue;
                }

                if (members[i] is IBotAlly { AbleToFight: true })
                {
                    strength += BotThreat.Power(self);
                    company++;
                }
            }
        }

        // A death on a road the appraisal offered though the straight line refused it (06.10.2026). See BotDanger.Fell.
        BotDanger.Fell(bot);

        var asked = BotQuad.Muscle(map, where);
        var fight = BotLairs.MetFight(map, where, Sight, out var kept, out var wide);

        Deaths++;

        if (fight > strength)
        {
            Outmatched++;

            if (asked <= strength)
            {
                Unwarned++;
            }
        }

        var killer = bot.LastKiller;

        // <b>Deaths at contact with a wanderer (03.10.2026).</b> Nothing a spawner keeps within sight, so nothing any map of the
        // ground warned of: the plague beast that walked out of its bog, the air elementals off their hill. Counted with whether the
        // bot was in a company and whether the killer was reckoned past what the company brought — what a company withdraws from
        // (BotSquad.Reckon, BotSquad.WithdrawOdds).
        if (kept == 0 && killer is BaseCreature { Controlled: false })
        {
            Wandered++;

            if (company > 1)
            {
                WanderedInCompany++;
            }

            if (BotSquad.Reckon(killer) > strength * BotSquad.WithdrawOdds)
            {
                WanderedBeyond++;
            }
        }

        // The city's prices on kinds read the killers from here. See BotCity.Fell (02.10.2026, build 389).
        BotCity.Fell(killer);

        // And what a kind costs us in lives prices the choosing of it (build next). See BotQuarry.Price.
        BotQuarry.Fell(killer);

        // And what it costs a company in its own fights, apart from the island (03.10.2026). See BotCompanyRecord.
        (fellIn ?? (bot as IBotSquadMember)?.Squad)?.Fell(killer);

        logger.Information(
            "Kept: {Name} fell at ({X}, {Y}) with {Strength:F0} of strength in a company of {Company}, killed by {Killer}; the square asked {Asked:F0}, and the spawners near keep {Kept} alive, {Fight:F0} to meet within sight and {Wide:F0} over all their ground",
            bot.Name,
            where.X,
            where.Y,
            strength,
            company,
            killer switch
            {
                null => "nobody known",
                BotMobile other => $"{other.Name}, one of ours ({BotThreat.Power(other):F0})",
                _ => $"{killer.GetType().Name} ({BotThreat.Power(killer):F0})"
            },
            asked,
            kept,
            fight,
            wide
        );
    }

    /// <summary>The line for the island's summary, with the whole island reckoned again every <see cref="SurveyEveryMs"/>.</summary>
    public static string Describe()
    {
        if (!Running)
        {
            return "what the spawners keep is not reckoned (BotKept.Running)";
        }

        if (!_surveyed || Core.TickCount - _surveyedTick >= SurveyEveryMs)
        {
            _surveyed = true;
            _surveyedTick = Core.TickCount;
            _survey = Survey();
        }

        return $"what the spawners keep, in shadow: {Deaths} deaths reckoned, {Outmatched} where it came to more than the dead bot's company brought and {Unwarned} of those where the square asked no more than it brought; {Wandered} to a wanderer (nothing kept within sight), {WanderedInCompany} of them in a company and {WanderedBeyond} to one reckoned past ×{BotSquad.WithdrawOdds:F1} what the dead bot's company brought; {_survey}";
    }

    /// <summary>How much of the island a square asking at least what its spawners keep would close, and to whom.</summary>
    private static string Survey()
    {
        var map = BotPopulation.Home;

        if (map == null || map == Map.Internal)
        {
            return "the island not yet reckoned";
        }

        var watch = Stopwatch.StartNew();
        var squares = 0;
        var wideLone = 0;
        var lone = 0;
        var company = 0;
        var twice = 0;
        var unasked = 0;
        var worst = 0.0;
        var worstAt = Point2D.Zero;
        var full = LoneBot * BotSquad.Usual;

        foreach (var quad in BotQuad.All)
        {
            if (quad.Map != map || quad.Deep || quad.Townbound)
            {
                continue;
            }

            squares++;

            var middle = quad.Middle;
            var at = new Point3D(middle.X, middle.Y, 0);
            var fight = BotLairs.MetFight(map, at, Sight, out _, out var wide);

            if (wide > LoneBot)
            {
                wideLone++;
            }

            if (fight <= LoneBot)
            {
                continue;
            }

            lone++;

            if (fight > full)
            {
                company++;
            }

            if (fight > 2 * full)
            {
                twice++;
            }

            if (BotQuad.Muscle(map, at) < fight)
            {
                unasked++;
            }

            if (fight > worst)
            {
                worst = fight;
                worstAt = middle;
            }
        }

        return $"of {squares} squares outside the walls and the dungeons, {lone} have more than a lone bot of {LoneBot:F0} to meet within {Sight} tiles of what the spawners keep, {company} more than a company of {BotSquad.MaxSize}, {twice} more than two, and {unasked} of them ask less than that now ({wideLone} over all the spawners' ground); the most at ({worstAt.X}, {worstAt.Y}), {worst:F0} (reckoned in {watch.ElapsedMilliseconds}ms)";
    }

    /// <summary>Forgotten with the world, as the lairs are.</summary>
    public static void Forget()
    {
        Deaths = 0;
        Outmatched = 0;
        Unwarned = 0;
        Wandered = 0;
        WanderedInCompany = 0;
        WanderedBeyond = 0;
        _surveyed = false;
    }
}
