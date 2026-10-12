using System;
using System.Collections.Generic;
using Server.Guilds;
using Server.Logging;

namespace Server.BotAI.V2;

/// <summary>
/// The wars themselves: which pairs are fighting, what each side has taken off the other, and the rules
/// that begin, end and forbid one.
///
/// <para>
/// <b>Patrick's order of 13.09.2026, and the twelve hours that produced it.</b> Between 02:12 and 14:21
/// the shard declared twelve wars and ended five; the worst opinion on the island sat at the floor of the
/// scale for the whole session, seven wars stood at once, and 3,054 bots were killed by other bots —
/// about 250 an hour, one death every fifteen seconds. Nothing in the war ever finished it: it began as a
/// number crossing a threshold and it could only end as the same number climbing back, and 4,337 refusals
/// to move along at twelve apiece meant the number never climbed anywhere. A war with no way to be won is
/// not a war, it is the weather.
/// </para>
///
/// <para>
/// <b>Four rules, each a dial, and the whole design is that a war now has an end it can reach.</b> It is
/// won by <see cref="Kills"/> of the enemy or by <see cref="Loot"/> gold's worth taken off their corpses;
/// it cannot be ended by a change of heart before <see cref="LeastMs"/>, and it is judged on what blood
/// there was when <see cref="LongestMs"/> runs out. A guild may declare one war in <see cref="DeclareEveryMs"/>
/// and stand in <see cref="MostWars"/> at a time, and the two guilds keep a truce of <see cref="TruceMs"/>
/// afterwards — a peace that is broken the next minute was not a peace.
/// </para>
///
/// <para>
/// <b>The opinion still decides when a war begins; this decides whether it may.</b> <c>BotRegard</c> keeps
/// what every guild thinks of every other and asks here before it declares. The engine still holds the war
/// itself (<c>Guild.IsWar</c>) and still does the enforcing — nothing about notoriety or looting changed.
/// What changed is that a war is a thing with a beginning, a score and an end, all of them printed.
/// </para>
///
/// <para>
/// <b>One war begins without an opinion and without a meeting: a claim on a square another guild holds</b> (Patrick,
/// 06.10.2026). <c>BotSeizure.Seize</c> declares it at once, the claimant the declarer of record, past the truce, the clock
/// and the count of wars; the claimant has weighed it before (<c>BotSeizure.Weigh</c>). Everything after the declaration —
/// score, ends, capitulation, the losing side's call — is the same as for any other war here.
/// </para>
/// </summary>
public static class BotWar
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotWar));

    /// <summary>Whether wars are ruled at all. Off, and <c>BotRegard</c> declares and ends them as it did before.</summary>
    public static bool Running { get; set; } = true;

    /// <summary>Enemies one side has to bring down to win. By order: fifty.</summary>
    public static int Kills { get; set; } = 50;

    /// <summary>Or gold's worth taken off the enemy's corpses. By order: ten thousand.</summary>
    public static int Loot { get; set; } = 10000;

    /// <summary>
    /// How long a war stands before a mended opinion may end it.
    ///
    /// A quarter of an hour. On 13.09.2026 The Lantern declared on The Needle at 02:20:22 and The Needle made
    /// peace at 02:20:29 — seven seconds, because the one trade that mended the Needle's opinion happened to
    /// land then. A war that short is a log line, not an event anybody on the island noticed.
    /// </summary>
    public static int LeastMs { get; set; } = 900000;

    /// <summary>And how long one may run before it is judged on whatever blood there was.</summary>
    public static int LongestMs { get; set; } = 10800000;

    /// <summary>How long two guilds that have just fought are kept from fighting again.</summary>
    public static int TruceMs { get; set; } = 3600000;

    /// <summary>How often one guild may declare a war.</summary>
    public static int DeclareEveryMs { get; set; } = 1800000;

    /// <summary>
    /// Whether losing a war starts the loser's clock for declaring one, as declaring does. See <c>End</c>.
    ///
    /// <para>
    /// <b>Only a declaration used to start that clock.</b> A guild beaten in a war it had not declared could declare on a
    /// third guild the minute its truce began, while it still owed the hall move its defeat sentenced it to — the one
    /// state in which the shard may not be restarted. Fable's option, taken on Patrick's word on 15.09.2026.
    /// </para>
    /// </summary>
    public static bool LoserCools { get; set; } = true;

    /// <summary>Losers whose clock for declaring a war was started by the defeat. See <see cref="LoserCools"/>.</summary>
    public static long LosersCooled { get; private set; }

    /// <summary>How many wars one guild may stand in at once.</summary>
    public static int MostWars { get; set; } = 1;

    /// <summary>How often the standing wars are looked at for their time running out.</summary>
    public static int LookMs { get; set; } = 10000;

    /// <summary>
    /// How many of its members a guild fights a war with while the two sides stand level. See <see cref="Wanted"/>.
    /// </summary>
    public static int Engage { get; set; } = 5;

    /// <summary>How many more members a guild brings into its war for every kill it stands behind. See <see cref="Wanted"/>.</summary>
    public static double PerKillBehind { get; set; } = 0.5;

    /// <summary>More than this many in the fight on each side makes a war a big one. Patrick's order of 24.09.2026.</summary>
    public static int BigAt { get; set; } = 10;

    /// <summary>Wars that became big: more than <see cref="BigAt"/> in the fight on each side at one look.</summary>
    public static long Bigs { get; private set; }

    /// <summary>Per guild at war, at the last look: members in the fight, and members in the world who could still join it.</summary>
    private static readonly Dictionary<string, (int Involved, int Free)> _strength = [];

    /// <summary>Per guild at war, at the last look: the members in the world who could still join it.</summary>
    private static readonly Dictionary<string, List<BotMobile>> _free = [];

    /// <summary>The members drawn into their guild's war from beyond their own reach at the last look. See <see cref="Mustered"/>.</summary>
    private static readonly HashSet<Serial> _mustered = [];

    /// <summary>Wars declared.</summary>
    public static long Declared { get; private set; }

    /// <summary>Declarations refused because the pair were under a truce.</summary>
    public static long Truced { get; private set; }

    /// <summary>Declarations refused because the declarer had declared one too recently.</summary>
    public static long Cooling { get; private set; }

    /// <summary>Declarations refused because one of the two already stood in as many wars as it may.</summary>
    public static long Busy { get; private set; }

    /// <summary>
    /// How many times the declarer's might the other guild's may be before the declarer keeps its grievance to itself.
    ///
    /// <para>
    /// <b>Patrick's order of 25.09.2026: newcomers "must understand what they are doing and be more yielding before
    /// veterans".</b> The Anchor — the newcomers' guild, twelve bots — declared on The Lantern at 13:59 over ground and
    /// lost 25 to 3 in fifteen minutes. A war is declared on regard alone; nothing weighed the two sides first. Now the
    /// declarer weighs them (<see cref="Might"/>) and, against a guild this many times its own, does not declare: the
    /// grievance stands and the refusal is counted as <see cref="Daunted"/>.
    /// </para>
    /// </summary>
    public static double DauntedAt { get; set; } = 2.0;

    /// <summary>Declarations refused because the other guild was <see cref="DauntedAt"/> times the declarer's might or more.</summary>
    public static long Daunted { get; private set; }

    /// <summary>
    /// How many kills the other side must have before a guild that cannot cope yields. See <see cref="YieldRatio"/>.
    ///
    /// <para>
    /// <b>The second half of the same order: "if they cannot manage it and the other guild's strength is several times
    /// theirs, they capitulate".</b> Judged on the score rather than on <see cref="Might"/>, because the score is what the
    /// fighting proved: by might The Lantern was 1.18 times The Anchor, and its archer Brannoc Ashdown (1715 of strength)
    /// killed The Anchor's companies from a height they could not reach. A guild whose enemy has at least this many kills
    /// and <see cref="YieldRatio"/> times its own (plus one) capitulates: the war ends as lost, with everything losing
    /// costs, but without the rest of the dead. The Anchor would have yielded at 8 to 1.
    /// </para>
    /// </summary>
    public static int YieldBehind { get; set; } = 8;

    /// <summary>How many times its own kills (plus one) the enemy's must be for a guild to capitulate. See <see cref="YieldBehind"/>.</summary>
    public static double YieldRatio { get; set; } = 3.0;

    /// <summary>
    /// How many times the capitulating guild's <see cref="Might"/> the enemy's must be. Patrick's rule has two halves —
    /// "if they cannot manage it <em>and</em> the other guild's strength is several times theirs" — and the first cut
    /// judged the first alone: in the first hour without cooldowns (25.09.2026, build 231) The Ash capitulated to The Crown
    /// twice, 8 to 1 and 8 to 0, at 1.35 times its might — peers ending a war on one bad skirmish. At half again, peers
    /// fight on to the kills or the clock; The Anchor against The Lantern (1.7) or The Blade (2.5) still yields.
    /// </summary>
    public static double YieldMight { get; set; } = 1.5;

    /// <summary>Wars ended by one side capitulating, for either cause. See <see cref="YieldBehind"/> and <see cref="YieldOdds"/>.</summary>
    public static long Capitulated { get; private set; }

    /// <summary>Of those, on the score (<see cref="YieldBehind"/>), and for fielding under a fifth of the enemy's might (<see cref="YieldOdds"/>).</summary>
    public static long YieldedOnScore { get; private set; }

    public static long YieldedOutmatched { get; private set; }

    /// <summary>
    /// How many times a side's might its enemy's must be for the side to capitulate once the war has stood <see cref="LeastMs"/>,
    /// whatever the score. Five: a side that cannot field a fifth of the other's strength.
    ///
    /// <para>
    /// <b>Patrick, 06.10.2026 ~10:00: "check the rule that the weak capitulate: yes."</b> The Wolf declared on The Ash at 06:36:23 over
    /// a seized square, might 108507 against 17055 (6.4 times); The Ash had five members and put one or two in the field against
    /// nineteen to twenty-one, and capitulated only at 08:15:10, after 98 minutes. The rule then had one door, the score: the
    /// enemy at <see cref="YieldBehind"/> (8) kills and three times the side's own plus one. The Wolf's eight kills took 98
    /// minutes because The Ash had almost nobody in the field to kill: the weaker a side, the longer it took to yield.
    /// </para>
    ///
    /// <para>
    /// <b>The measure is <see cref="Might"/></b> — health times damage times fighting skill over every member, resting members
    /// counted, plus the allies standing in a war joined on this one's account: what a side could field, not who happened to be in
    /// the field at one look, which is nought on both sides between skirmishes. A side not ahead on blood whose might is under a
    /// fifth of its enemy's capitulates at the first look after <see cref="LeastMs"/> (15 minutes, the window in which no peace
    /// may end a war). Between equals nothing changes: The Hammer against The Wolf (1.36 times) is fought out to the kills or the
    /// clock, as on 06.10 at 02:54 and 05:54. Nought switches it off.
    /// </para>
    /// </summary>
    public static double YieldOdds { get; set; } = 5.0;

    /// <summary>Looks at a war past its <see cref="LeastMs"/> where a side was under a fifth of the other's might but ahead on blood, and fought on.</summary>
    public static long HeldByBlood { get; private set; }

    /// <summary>
    /// What a guild would bring to a war: each member's strength (<see cref="BotThreat.Power"/>, health times damage)
    /// times how well it fights — half again for nothing learned, up to half again plus its best fighting skill and half
    /// its Tactics, in hundreds. Resting members count: they can be called back to fight.
    ///
    /// <para>
    /// <b>Power alone does not tell a veteran from a newcomer</b> — a green bot in plate hits as hard as it will ever hit,
    /// and misses most of it. The skill factor runs from about 0.9 for a newcomer (best 30, Tactics 20) to 1.8 for a
    /// veteran (best 90, Tactics 80).
    /// </para>
    /// </summary>
    public static double Might(string guild)
    {
        if (guild == null || BaseGuild.FindByName(guild) is not Guild g || g.Members == null)
        {
            return 0.0;
        }

        var total = 0.0;

        for (var i = 0; i < g.Members.Count; i++)
        {
            if (g.Members[i] is not BotMobile { Deleted: false } member)
            {
                continue;
            }

            var skills = member.Skills;
            var best = Math.Max(
                Math.Max(Math.Max(skills.Swords.Value, skills.Macing.Value), Math.Max(skills.Fencing.Value, skills.Archery.Value)),
                Math.Max(skills.Wrestling.Value, skills.Magery.Value)
            );

            total += BotThreat.Power(member) * (0.5 + best / 100.0 + skills.Tactics.Value / 200.0);
        }

        return total;
    }

    /// <summary>Wars ended, by how.</summary>
    public static long WonByBlood { get; private set; }

    public static long WonByPlunder { get; private set; }

    public static long TimedOut { get; private set; }

    public static long Peaced { get; private set; }

    /// <summary>Of the wars ended by the clock or by peace, the ones nobody was ahead in.</summary>
    public static long Drawn { get; private set; }

    /// <summary>Kills counted towards a war, and gold's worth of plunder counted.</summary>
    public static long Counted { get; private set; }

    public static long Plundered { get; private set; }

    /// <summary>One war.</summary>
    public sealed class War
    {
        /// <summary>The pair, in a fixed order, so one war has one entry however the callers name it.</summary>
        public string A;

        public string B;

        /// <summary>Who declared it.</summary>
        public string Declarer;

        public long Began;

        public int KillsA;

        public int KillsB;

        public int LootA;

        public int LootB;

        public string Why;

        /// <summary>The war this one was joined on account of, as its pair key, or nothing for a war of one's own.</summary>
        public (string A, string B)? Joined;

        /// <summary>Whether more than <see cref="BigAt"/> of each side have been in it at once. Not kept across a restart.</summary>
        public bool Big;

        /// <summary>The other side of this war from the guild named, or nothing if it is not in it.</summary>
        public string Against(string guild) => guild == A ? B : guild == B ? A : null;

        /// <summary>What this side has taken, as kills and as plunder.</summary>
        public (int Kills, int Loot) Score(string guild) => guild == A ? (KillsA, LootA) : (KillsB, LootB);

        public int Minutes => (int)((Core.TickCount - Began) / 60000);
    }

    private static readonly Dictionary<(string A, string B), War> _wars = [];

    private static readonly Dictionary<(string A, string B), long> _truces = [];

    private static readonly Dictionary<string, long> _declared = [];

    private static long _looked;

    private static readonly List<War> _over = [];

    /// <summary>The pair, in a fixed order.</summary>
    private static (string A, string B) Pair(string one, string other) =>
        string.CompareOrdinal(one, other) <= 0 ? (one, other) : (other, one);

    /// <summary>The war between these two, or nothing.</summary>
    public static War Of(string one, string other) =>
        one == null || other == null ? null : _wars.GetValueOrDefault(Pair(one, other));

    /// <summary>Every war standing, for the dashboard, the summary and Argus.</summary>
    public static IEnumerable<War> Standing => _wars.Values;

    /// <summary>The truces, as pair and the tick they end. For the store.</summary>
    public static IEnumerable<KeyValuePair<(string A, string B), long>> Truces => _truces;

    /// <summary>When each guild last declared, as a tick. For the store.</summary>
    public static IEnumerable<KeyValuePair<string, long>> Declarations => _declared;

    /// <summary>A war read back off the save, having run <paramref name="elapsedMs"/> already. See <c>BotWarStore</c>.</summary>
    public static void Restore(War war, long elapsedMs)
    {
        if (war?.A == null || war.B == null)
        {
            return;
        }

        war.Began = Core.TickCount - Math.Max(0, elapsedMs);
        _wars[(war.A, war.B)] = war;
    }

    /// <summary>A truce read back off the save, with <paramref name="leftMs"/> still to run.</summary>
    public static void RestoreTruce(string a, string b, long leftMs)
    {
        if (a != null && b != null && leftMs > 0)
        {
            _truces[Pair(a, b)] = Core.TickCount + leftMs;
        }
    }

    /// <summary>
    /// The truce between these two broken: by a seizure (<c>BotSeizure</c>), whose war begins whatever stood between them.
    /// True when one was still running.
    /// </summary>
    public static bool Untruce(string a, string b)
    {
        if (a == null || b == null || !_truces.Remove(Pair(a, b), out var until))
        {
            return false;
        }

        return until - Core.TickCount > 0;
    }

    /// <summary>A declaration read back off the save, made <paramref name="agoMs"/> ago.</summary>
    public static void RestoreDeclared(string guild, long agoMs)
    {
        if (guild != null && agoMs >= 0)
        {
            _declared[guild] = Core.TickCount - agoMs;
        }
    }

    /// <summary>How many wars this guild stands in.</summary>
    public static int Fighting(string guild)
    {
        var many = 0;

        foreach (var war in _wars.Values)
        {
            if (war.A == guild || war.B == guild)
            {
                many++;
            }
        }

        return many;
    }

    /// <summary>
    /// The most any standing war has this guild behind on kills: the enemy's count less its own, or nought when it is
    /// level or ahead everywhere.
    /// </summary>
    public static int Behind(string guild)
    {
        var worst = 0;

        foreach (var war in _wars.Values)
        {
            var enemy = war.Against(guild);

            if (enemy == null)
            {
                continue;
            }

            worst = Math.Max(worst, war.Score(enemy).Kills - war.Score(guild).Kills);
        }

        return worst;
    }

    /// <summary>
    /// How many of its members a guild at war should have in the fight: <see cref="Engage"/> while it is level or ahead,
    /// one more for every two kills it falls behind (<see cref="PerKillBehind"/>), never more than it has. Nought at peace.
    ///
    /// <para>
    /// <b>Patrick's order of 24.09.2026: the lower the losing side's score, the more of its bots are drawn into the
    /// war; more than ten on each side is a big war.</b> The five wars before it were all won by The Blade, 25 to 0, 25
    /// to 1, 25 to 3, 25 to 4 and 25 to 6, and not by strength (its fighters are fifth of six by the population's own
    /// reckoning) but by number: the losing side fought with two bots against thirteen (The Lantern), five against eight
    /// (The Ash) and seven against seven (The Crown), and the same few of them died again and again while the rest of the
    /// guild went on with its work. A member answered its guild's call only from <c>BotFeud.Answer</c> away, and nothing
    /// about the score changed who answered.
    /// </para>
    /// </summary>
    public static int Wanted(string guild)
    {
        if (guild == null || Fighting(guild) == 0)
        {
            return 0;
        }

        var members = BotGuilds.Named(guild)?.Members?.Count ?? 0;

        return Math.Min(members, Engage + (int)Math.Ceiling(PerKillBehind * Behind(guild)));
    }

    /// <summary>Members of this guild in its war at the last look: rallying, quarrelling, in its war company, or called back for it.</summary>
    public static int Involved(string guild) => guild != null && _strength.TryGetValue(guild, out var s) ? s.Involved : 0;

    /// <summary>Whether a guild at war has fewer in the fight than its score asks for. See <see cref="Wanted"/>.</summary>
    public static bool Short(string guild) => guild != null && Fighting(guild) > 0 && Involved(guild) < Wanted(guild);

    /// <summary>
    /// How many a guild must call back from its rest to have what its score asks for: what is wanted, less those in the
    /// fight, less those in the world who could still join it. Nought at peace.
    /// </summary>
    public static int FromRest(string guild)
    {
        if (guild == null || !_strength.TryGetValue(guild, out var s))
        {
            return 0;
        }

        return Math.Max(0, Wanted(guild) - s.Involved - s.Free);
    }

    /// <summary>
    /// Whether this member is one of those its guild draws into its war from wherever it stands: the nearest to the fight
    /// of the members who could still join it, as many as the score asks for beyond those already in it.
    ///
    /// <para>
    /// <b>Chosen here rather than asked of each bot, so that "more" means as many as the score asks and not the whole
    /// guild.</b> A rule each member read for itself — "my guild is short, so I go" — would send every free member at
    /// the first look of a war, before the count could say that five had gone.
    /// </para>
    /// </summary>
    public static bool Mustered(Mobile bot) => bot != null && _mustered.Contains(bot.Serial);

    /// <summary>Whether this guild stands in a big war. See <see cref="BigAt"/>.</summary>
    public static bool InBig(string guild)
    {
        foreach (var war in _wars.Values)
        {
            if (war.Big && war.Against(guild) != null)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Counts, per guild at war, who is in the fight and who could still join it, and marks the wars that have become big.
    /// Once a look, on the war's own beat: the feuder and the rest ask per bot and per beat, and read this.
    /// </summary>
    private static void Count()
    {
        _strength.Clear();
        _mustered.Clear();

        foreach (var list in _free.Values)
        {
            list.Clear();
        }

        var bots = BotPopulation.Bots;

        for (var i = 0; i < bots.Count; i++)
        {
            if (bots[i] is not { Deleted: false, Alive: true } bot || bot.Guild is not Guild guild || Fighting(guild.Name) == 0)
            {
                continue;
            }

            var (involved, free) = _strength.GetValueOrDefault(guild.Name);

            if (InTheWar(bot, guild))
            {
                involved++;
            }
            else if (Able(bot))
            {
                free++;

                if (!_free.TryGetValue(guild.Name, out var list))
                {
                    _free[guild.Name] = list = [];
                }

                list.Add(bot);
            }

            _strength[guild.Name] = (involved, free);
        }

        foreach (var (name, list) in _free)
        {
            var need = Wanted(name) - Involved(name);

            if (need <= 0 || list.Count == 0)
            {
                continue;
            }

            var guild = BotGuilds.Named(name);
            var target = BotFeud.Threat(guild) ?? BotFeud.On(guild);

            if (target is not { Deleted: false } || target.Map == null)
            {
                continue;
            }

            list.Sort((a, b) => Apart(a, target).CompareTo(Apart(b, target)));

            for (var i = 0; i < list.Count && i < need; i++)
            {
                _mustered.Add(list[i].Serial);
            }
        }

        foreach (var war in _wars.Values)
        {
            if (war.Big)
            {
                continue;
            }

            var a = Involved(war.A);
            var b = Involved(war.B);

            if (a <= BigAt || b <= BigAt)
            {
                continue;
            }

            war.Big = true;
            Bigs++;

            logger.Warning(
                "The war of {Declarer} on {Other} has become a big war: {A} of {GuildA} and {B} of {GuildB} in it, the score {KillsA} to {KillsB}",
                war.Declarer,
                war.Against(war.Declarer),
                a,
                war.A,
                b,
                war.B,
                war.KillsA,
                war.KillsB
            );
        }
    }

    /// <summary>Whether this member is in its guild's war: rallying to it, quarrelling for it, in its war company, or back from rest on its call.</summary>
    private static bool InTheWar(BotMobile bot, Guild guild)
    {
        var deed = bot.Resolve?.Deed;

        if (deed is BotRally rally && ReferenceEquals(rally.Guild, guild) || deed is BotQuarrel quarrel && quarrel.Ours == guild.Name)
        {
            return true;
        }

        if (bot.Squad != null && ReferenceEquals(bot.Squad, BotFeud.Company(guild)))
        {
            return true;
        }

        return string.Equals(BotRest.CalledBy(bot), guild.Name, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Tiles between two bodies on one map, or as far as can be when they are on two.</summary>
    private static int Apart(Mobile a, Mobile b) =>
        a.Map != b.Map ? int.MaxValue : Math.Max(Math.Abs(a.X - b.X), Math.Abs(a.Y - b.Y));

    /// <summary>Whether this member could join its guild's war from where it stands: a fighting class, fit, and above ground.</summary>
    private static bool Able(BotMobile bot)
    {
        var kit = bot.Class?.Kit;

        if (kit == null || kit.Melee.Count == 0 && kit.Ranged.Count == 0)
        {
            return false;
        }

        return bot.Hits >= bot.HitsMax * BotFeuder.Fit && !BotDungeon.Under(bot.Location);
    }

    /// <summary>
    /// Whether <paramref name="mine"/> may declare on <paramref name="theirs"/> now, and if not, why not.
    ///
    /// Three refusals, each with its own bucket: the pair are under a truce, the declarer has declared too
    /// recently, or one of the two already stands in as many wars as it may. Counted here rather than by the
    /// caller, so "no war was declared" always says which rule did it.
    /// </summary>
    public static bool MayDeclare(string mine, string theirs, out string why)
    {
        why = null;

        // The Shadow is every guild's enemy already, with no score and no end (BotUnderworld.EnemyOfAll); a war in this
        // ledger would be ended one day, and its end would take the enmity with it.
        if (mine == BotUnderworld.GuildName || theirs == BotUnderworld.GuildName)
        {
            why = "The Shadow is everybody's enemy already";

            return false;
        }

        if (!Running)
        {
            return true;
        }

        // A tribute standing between the two is the peace the weak bought instead of this war (07.10.2026). See BotTribute.
        if (BotTribute.Forbids(mine, theirs, out var paying))
        {
            why = paying;

            return false;
        }

        var now = Core.TickCount;
        var pair = Pair(mine, theirs);

        // A guild whose banner the other holds may declare a war of revenge on it with a truce or its own clock standing
        // (07.10.2026, relics20). Never past the rules of might, tribute or a war already standing. See BotRelics.Avenges.
        var avenging = BotRelics.Avenges(mine, theirs);

        if (_truces.TryGetValue(pair, out var until))
        {
            if (now - until < 0)
            {
                if (!avenging)
                {
                    Truced++;
                    why = $"a truce with {(until - now) / 60000} minutes left";

                    return false;
                }

                BotRelics.Waive(mine, theirs, "a truce");
            }
            else
            {
                _truces.Remove(pair);
            }
        }

        if (_declared.TryGetValue(mine, out var last) && now - (last + DeclareEveryMs) < 0)
        {
            if (!avenging)
            {
                Cooling++;
                why = $"{mine} declared or lost a war {(now - last) / 60000} minutes ago";

                return false;
            }

            BotRelics.Waive(mine, theirs, "its own clock");
        }

        if (Fighting(mine) >= MostWars || Fighting(theirs) >= MostWars)
        {
            Busy++;
            why = Fighting(mine) >= MostWars ? $"{mine} is already at war" : $"{theirs} is already at war";

            return false;
        }

        // Not against a guild several times its own. See DauntedAt.
        if (DauntedAt > 0)
        {
            var ours = Might(mine);
            var others = Might(theirs);

            if (ours > 0 && others >= ours * DauntedAt)
            {
                Daunted++;
                why = $"{theirs} is {others / ours:F1} times its might ({others:F0} against {ours:F0})";

                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Declares it. The engine's own war, plus a page in this ledger with the score at nought.
    ///
    /// The caller has asked <see cref="MayDeclare"/> already; this does not ask again, so a caller that
    /// wants a war without the rules — a hand at the keyboard — can have one.
    /// </summary>
    public static void Declare(Guild mine, Guild theirs, string why)
    {
        if (mine == null || theirs == null || mine == theirs)
        {
            return;
        }

        var pair = Pair(mine.Name, theirs.Name);

        if (_wars.ContainsKey(pair))
        {
            return;
        }

        mine.AddEnemy(theirs);

        var war = new War
        {
            A = pair.A,
            B = pair.B,
            Declarer = mine.Name,
            Began = Core.TickCount,
            Why = why
        };

        _wars[pair] = war;
        _declared[mine.Name] = war.Began;
        Declared++;

        // <b>Allies join, on both sides.</b> Patrick's order of 13.09.2026. The engine's notoriety only makes
        // an enemy of somebody a guild is itself at war with, so an ally that merely wished its friend well
        // could not lawfully land a blow; each ally is put at war with the other side under a page of its
        // own, marked as joined, which ends when this one ends and is judged on nothing.
        Enlist(mine, theirs, pair);
        Enlist(theirs, mine, pair);

        // <b>And both sides call their members back from rest.</b> Patrick's order of 24.09.2026; see BotRest.Calls. After
        // the allies are in, so a guild now fighting more than one guild calls as for a great war.
        BotRest.Call(mine, $"its war on {theirs.Name}");
        BotRest.Call(theirs, $"{mine.Name}'s war on it");

        logger.Warning(
            "{Mine} has declared war on {Theirs} over {Why}, might {MineMight:F0} against {TheirsMight:F0}; it is won at {Kills} dead or {Loot}gp of plunder, judged after {Longest} minutes, and cannot be ended for {Least}",
            mine.Name,
            theirs.Name,
            why,
            Might(mine.Name),
            Might(theirs.Name),
            Kills,
            Loot,
            LongestMs / 60000,
            LeastMs / 60000
        );

        BotAlarm.Note("war", $"{mine.Name} declared war on {theirs.Name} over {why}", Fighting(mine.Name), _wars.Count, "-");

        BotVoice.ToGuild(mine.Name, "war:declared", new Dictionary<string, string> { ["enemy"] = theirs.Name });
        BotVoice.ToGuild(theirs.Name, "war:declared", new Dictionary<string, string> { ["enemy"] = mine.Name });
    }

    /// <summary>
    /// Boot: wars the engine still holds from the last session, which have no page here, are ended.
    ///
    /// <c>Guild.Enemies</c> is saved with the guild and the population is not, so a restart could begin
    /// with every war of the night before still standing and no score, no clock and no truce on any of
    /// them — exactly the wars this ledger exists to stop. In practice the engine disbands a guild whose
    /// leader has been deleted and <c>BotGuilds.Form</c> revives it, and disbanding clears its enemies,
    /// so this found nothing to end on either restart of 13.09.2026; it stays because that is the engine's
    /// habit and not its promise. The session starts at peace and the opinions, which are not saved, start
    /// at nought; whatever quarrel is real will be back within the hour.
    /// </summary>
    public static void Reconcile()
    {
        var ended = 0;
        List<(Guild, Guild)> standing = [];

        // The wars read back off the save first: a pair whose guilds are gone is dropped, and a pair whose
        // guilds stand is re-asserted to the engine, which is the only side of this that can be argued with.
        List<(string A, string B)> stale = null;

        foreach (var (pair, _) in _wars)
        {
            // A page against The Shadow from an older build: dropped, since ending it would end the enmity.
            if (pair.A == BotUnderworld.GuildName || pair.B == BotUnderworld.GuildName)
            {
                (stale ??= []).Add(pair);

                continue;
            }

            var ga = BotGuilds.Named(pair.A) ?? BaseGuild.FindByName(pair.A) as Guild;
            var gb = BotGuilds.Named(pair.B) ?? BaseGuild.FindByName(pair.B) as Guild;

            if (ga != null && gb != null)
            {
                var knew = ga.IsWar(gb);

                if (!knew)
                {
                    ga.AddEnemy(gb);
                }

                logger.Information(
                    "The war of {A} on {B} read back off the save: the engine {Knew}; the first now lists {Na} enemies and the second {Nb}",
                    pair.A,
                    pair.B,
                    knew ? "already held it" : "was told again",
                    ga.Enemies?.Count ?? 0,
                    gb.Enemies?.Count ?? 0
                );

                continue;
            }

            (stale ??= []).Add(pair);
        }

        if (stale != null)
        {
            for (var i = 0; i < stale.Count; i++)
            {
                _wars.Remove(stale[i]);
            }

            logger.Information("{Many} wars read back off the save were dropped for a guild that no longer stands", stale.Count);
        }

        foreach (var mine in BotGuilds.Standing)
        {
            var enemies = mine?.Enemies;

            if (enemies == null)
            {
                continue;
            }

            for (var i = 0; i < enemies.Count; i++)
            {
                // Not The Shadow: its enmity has no page on purpose and is not a war left over. See BotUnderworld.EnemyOfAll.
                if (enemies[i] is Guild theirs && theirs.Name != BotUnderworld.GuildName && Of(mine.Name, theirs.Name) == null)
                {
                    standing.Add((mine, theirs));
                }
            }
        }

        for (var i = 0; i < standing.Count; i++)
        {
            var (mine, theirs) = standing[i];

            if (mine.IsWar(theirs))
            {
                mine.RemoveEnemy(theirs);
                ended++;
            }
        }

        if (ended > 0)
        {
            logger.Information("{Ended} wars left standing by the last session were ended at boot; the ledger starts empty", ended);
        }
    }

    /// <summary>Every ally of <paramref name="ours"/> goes to war with <paramref name="theirs"/> on our account.</summary>
    private static void Enlist(Guild ours, Guild theirs, (string A, string B) cause)
    {
        var allies = ours?.Allies;

        if (allies == null)
        {
            return;
        }

        for (var i = 0; i < allies.Count; i++)
        {
            if (allies[i] is not Guild ally || ally == theirs || ally.IsWar(theirs) || ally.IsAlly(theirs))
            {
                continue;
            }

            var pair = Pair(ally.Name, theirs.Name);

            if (_wars.ContainsKey(pair))
            {
                continue;
            }

            ally.AddEnemy(theirs);

            _wars[pair] = new War
            {
                A = pair.A,
                B = pair.B,
                Declarer = ally.Name,
                Began = Core.TickCount,
                Why = $"alliance with {ours.Name}",
                Joined = cause
            };

            logger.Warning("{Ally} joins the war on {Theirs} beside its ally {Ours}", ally.Name, theirs.Name, ours.Name);

            BotRest.Call(ally, $"its ally {ours.Name}'s war on {theirs.Name}");
        }
    }

    /// <summary>Somebody of <paramref name="killer"/> killed somebody of <paramref name="fallen"/>.</summary>
    public static void Killed(string killer, string fallen)
    {
        var war = Of(killer, fallen);

        if (war == null)
        {
            return;
        }

        Counted++;

        if (killer == war.A)
        {
            war.KillsA++;
        }
        else
        {
            war.KillsB++;
        }

        Reckon(war);
    }

    /// <summary>Somebody of <paramref name="taker"/> took <paramref name="worth"/> gold's worth off a corpse of <paramref name="victim"/>.</summary>
    public static void Looted(string taker, string victim, int worth)
    {
        var war = Of(taker, victim);

        if (war == null || worth <= 0)
        {
            return;
        }

        Plundered += worth;

        if (taker == war.A)
        {
            war.LootA += worth;
        }
        else
        {
            war.LootB += worth;
        }

        Reckon(war);
    }

    /// <summary>Whether a mended opinion is allowed to end this war yet.</summary>
    public static bool MayPeace(string one, string other)
    {
        if (!Running)
        {
            return true;
        }

        var war = Of(one, other);

        // <b>A war joined on an ally's account has no peace of its own.</b> The two sides hold no grievance
        // against each other, so both opinions sit above the peace line from the first minute, and at the
        // fifteen-minute floor the joined war "made peace" and sentenced the loser while the war it was
        // joined on stood — the Hammer on the Blade, 08:02 on 14.09.2026, twelve dead to four and a hall to
        // move. It ends when its cause ends; see End.
        if (war?.Joined != null)
        {
            return false;
        }

        return war != null && Core.TickCount - (war.Began + LeastMs) >= 0;
    }

    /// <summary>The two have made peace. Judged on whatever blood there was.</summary>
    public static void Peace(string one, string other)
    {
        var war = Of(one, other);

        if (war == null)
        {
            return;
        }

        Peaced++;

        // <b>A peace names a winner only when the blood says so.</b> Three dead to two, ended by mended
        // opinion, sent the Hammer's hall out of the Crown's yard at 04:39 on 14.09.2026; a war that ends
        // because the two sides stopped minding each other is a draw unless one side is plainly ahead.
        var ahead = Ahead(war);
        var margin = Math.Abs(war.KillsA - war.KillsB);

        End(war, margin >= PeaceMargin ? ahead : null, "peace");
    }

    /// <summary>How many more of the enemy one side must have brought down for a peace to name it the winner.</summary>
    public static int PeaceMargin { get; set; } = 5;

    /// <summary>The beat: wars whose time has run out are judged.</summary>
    public static void Beat()
    {
        if (!Running || _wars.Count == 0)
        {
            return;
        }

        var now = Core.TickCount;

        if (now - (_looked + LookMs) < 0)
        {
            return;
        }

        _looked = now;
        _over.Clear();

        Count();

        foreach (var war in _wars.Values)
        {
            if (war.Joined == null && now - (war.Began + LongestMs) >= 0)
            {
                _over.Add(war);
            }
        }

        for (var i = 0; i < _over.Count; i++)
        {
            TimedOut++;
            End(_over[i], Ahead(_over[i]), "the clock");
        }

        Outmatched(now);
    }

    private static readonly List<(War War, string Loser, double Ours, double Theirs)> _yielding = [];

    /// <summary>
    /// A side that cannot field a fifth of its enemy's might capitulates once the war has stood <see cref="LeastMs"/>. See
    /// <see cref="YieldOdds"/>. Asked on the war's beat, so a side with nobody left to lose still yields.
    /// </summary>
    private static void Outmatched(long now)
    {
        if (YieldOdds <= 0.0 || _wars.Count == 0)
        {
            return;
        }

        _yielding.Clear();

        foreach (var war in _wars.Values)
        {
            if (war.Joined != null || now - (war.Began + LeastMs) < 0)
            {
                continue;
            }

            var a = SideMight(war, war.A);
            var b = SideMight(war, war.B);

            string loser = null;
            var ours = 0.0;
            var theirs = 0.0;

            if (a * YieldOdds <= b)
            {
                (loser, ours, theirs) = (war.A, a, b);
            }
            else if (b * YieldOdds <= a)
            {
                (loser, ours, theirs) = (war.B, b, a);
            }

            if (loser == null)
            {
                continue;
            }

            // Ahead on blood, it is not beaten: a small side killing more than it loses fights on.
            var (lostKills, _) = war.Score(loser);
            var (wonKills, _) = war.Score(war.Against(loser));

            if (lostKills > wonKills)
            {
                HeldByBlood++;

                continue;
            }

            _yielding.Add((war, loser, ours, theirs));
        }

        for (var i = 0; i < _yielding.Count; i++)
        {
            var (war, loser, ours, theirs) = _yielding[i];
            var winner = war.Against(loser);

            logger.Warning(
                "{Loser} capitulates to {Winner} after {Minutes} minutes: its side's might {Ours:F0} against {Theirs:F0} ({Ratio:F1} times), under 1/{Odds:0.#} of its enemy's, {Involved} of it in the field against {Others}, {LostKills} to {WonKills} dead",
                loser,
                winner,
                war.Minutes,
                ours,
                theirs,
                ours > 0.0 ? theirs / ours : 0.0,
                YieldOdds,
                Involved(loser),
                Involved(winner),
                war.Score(loser).Kills,
                war.Score(winner).Kills
            );

            Capitulated++;
            YieldedOutmatched++;
            End(war, winner, "the other side's capitulation, outmatched");
        }
    }

    /// <summary>
    /// A side's might in this war: its own and that of every ally standing in a war joined on this one's account against the
    /// same enemy. The same <see cref="Might"/> the seizure weighs and the declaration is daunted by.
    /// </summary>
    private static double SideMight(War war, string side)
    {
        var enemy = war.Against(side);
        var total = Might(side);

        foreach (var other in _wars.Values)
        {
            if (other.Joined is { } cause && cause.A == war.A && cause.B == war.B && (other.A == enemy || other.B == enemy))
            {
                var ally = other.Against(enemy);

                if (ally != null && ally != side)
                {
                    total += Might(ally);
                }
            }
        }

        return total;
    }

    /// <summary>Whether either side has reached a victory, and ends the war if so.</summary>
    private static void Reckon(War war)
    {
        if (!Running || war.Joined != null)
        {
            return;
        }

        if (war.KillsA >= Kills || war.KillsB >= Kills)
        {
            WonByBlood++;
            End(war, war.KillsA >= Kills ? war.A : war.B, "blood");

            return;
        }

        if (war.LootA >= Loot || war.LootB >= Loot)
        {
            WonByPlunder++;
            End(war, war.LootA >= Loot ? war.A : war.B, "plunder");

            return;
        }

        // A guild that cannot cope yields — to a guild well above its might. See YieldBehind and YieldMight.
        var aYields = Beaten(war.KillsA, war.KillsB) && Outmatched(war.A, war.B);

        if (aYields || Beaten(war.KillsB, war.KillsA) && Outmatched(war.B, war.A))
        {
            Capitulated++;
            YieldedOnScore++;
            End(war, aYields ? war.B : war.A, "the other side's capitulation");
        }
    }

    /// <summary>Whether <paramref name="enemy"/>'s might is <see cref="YieldMight"/> times <paramref name="side"/>'s or more.</summary>
    private static bool Outmatched(string side, string enemy)
    {
        var ours = Might(side);

        return ours <= 0 || Might(enemy) >= ours * YieldMight;
    }

    /// <summary>Whether a side with <paramref name="ours"/> kills against the enemy's <paramref name="theirs"/> cannot cope.</summary>
    private static bool Beaten(int ours, int theirs) =>
        YieldBehind > 0 && theirs >= YieldBehind && theirs >= YieldRatio * (ours + 1);

    /// <summary>Who is ahead on blood, or nothing when it is level.</summary>
    private static string Ahead(War war) =>
        war.KillsA > war.KillsB ? war.A : war.KillsB > war.KillsA ? war.B : null;

    /// <summary>
    /// Ends it: the engine's war comes down, the truce goes up, the opinions are settled, and the loser is
    /// sentenced.
    ///
    /// <para>
    /// <b>Both opinions go back to nought, and that is what makes the truce a truce.</b> A war settles the
    /// quarrel that started it; a pair left at the floor of the scale would declare again the minute the
    /// truce ran out, and the island would have one war with intervals rather than wars.
    /// </para>
    /// </summary>
    private static void End(War war, string winner, string how)
    {
        _wars.Remove((war.A, war.B));
        _truces[(war.A, war.B)] = Core.TickCount + TruceMs;

        BotFeud.Disband(war.A);
        BotFeud.Disband(war.B);

        // The wars joined on account of this one end with it, quietly: no verdict, no truce of their own.
        if (war.Joined == null)
        {
            List<War> joined = null;

            foreach (var other in _wars.Values)
            {
                if (other.Joined is { } cause && cause.A == war.A && cause.B == war.B)
                {
                    (joined ??= []).Add(other);
                }
            }

            if (joined != null)
            {
                for (var i = 0; i < joined.Count; i++)
                {
                    var side = joined[i];

                    _wars.Remove((side.A, side.B));

                    if (BaseGuild.FindByName(side.A) is Guild ga && BaseGuild.FindByName(side.B) is Guild gb)
                    {
                        ga.RemoveEnemy(gb);
                    }

                    BotFeud.Disband(side.A);
                    BotFeud.Disband(side.B);
                    BotRegard.Settle(side.A, side.B);

                    logger.Information("{A} leaves the war with {B}, which it had joined for an ally", side.Declarer, side.Against(side.Declarer));
                }
            }
        }

        if (BaseGuild.FindByName(war.A) is Guild a && BaseGuild.FindByName(war.B) is Guild b)
        {
            a.RemoveEnemy(b);
        }

        BotRegard.Settle(war.A, war.B);

        if (winner == null)
        {
            Drawn++;
            BotExile.Draw(war.A, war.B);

            logger.Warning(
                "The war between {A} and {B} ended by {How} after {Minutes} minutes with nobody ahead: {KillsA} to {KillsB} dead, {LootA}gp to {LootB}gp taken; a truce of {Truce} minutes",
                war.A,
                war.B,
                how,
                war.Minutes,
                war.KillsA,
                war.KillsB,
                war.LootA,
                war.LootB,
                TruceMs / 60000
            );

            BotAlarm.Note("war", $"the war between {war.A} and {war.B} ended by {how} after {war.Minutes} minutes, drawn {war.KillsA}:{war.KillsB}", 0, _wars.Count, $"{war.Minutes}m");

            BotVoice.ToGuild(war.A, "war:drawn", new Dictionary<string, string> { ["enemy"] = war.B });
            BotVoice.ToGuild(war.B, "war:drawn", new Dictionary<string, string> { ["enemy"] = war.A });

            // A draw breaks either side's run of lost wars (07.10.2026). See BotElection.WarEnded.
            BotElection.WarEnded(war.A, war.B, null);

            return;
        }

        var loser = war.Against(winner);
        var (wonKills, wonLoot) = war.Score(winner);

        // <b>A side that capitulates gives up the ground it was fighting for</b> (06.10.2026): the winner's claims running on the
        // loser's squares are decided for it at once, by the claims' own settling. See BotClaim.Concede.
        var conceded = how.StartsWith("the other side's capitulation", StringComparison.Ordinal) ? BotClaim.Concede(winner, loser) : 0;

        // <b>The loser's clock starts here, as the declarer's did when it declared.</b> See LoserCools: beaten and owing a
        // hall move, a guild could otherwise declare on somebody else the moment its truce began.
        if (LoserCools && loser != null)
        {
            _declared[loser] = Core.TickCount;
            LosersCooled++;
        }
        var (lostKills, lostLoot) = war.Score(loser);

        logger.Warning(
            "{Winner} won its war with {Loser} by {How} after {Minutes} minutes: {WonKills} to {LostKills} dead, {WonLoot}gp to {LostLoot}gp taken; a truce of {Truce} minutes{Conceded}",
            winner,
            loser,
            how,
            war.Minutes,
            wonKills,
            lostKills,
            wonLoot,
            lostLoot,
            TruceMs / 60000,
            conceded > 0 ? $"; {conceded} of the winner's claims on the loser's ground decided for it" : ""
        );

        BotAlarm.Note("war", $"{winner} won its war with {loser} by {how} after {war.Minutes} minutes, {wonKills}:{lostKills} dead and {wonLoot}:{lostLoot}gp taken", wonKills, lostKills, $"{war.Minutes}m");

        BotVoice.ToGuild(winner, "war:won", new Dictionary<string, string> { ["enemy"] = loser });
        BotVoice.ToGuild(loser, "war:lost", new Dictionary<string, string> { ["enemy"] = winner });

        // A victory counts for every member of the winner at the next election, a second loss in a row is a reason to depose the
        // loser's head (07.10.2026). See BotElection.WarEnded.
        BotElection.WarEnded(winner, loser, winner);

        BotExile.Sentence(winner, loser);

        // The loser's banner — and every relic it holds — goes to the winner's hall, and the loser's grudge is written after the
        // opinions were settled above (07.10.2026, relics20). See BotRelics.WarEnded.
        BotRelics.WarEnded(winner, loser);
    }

    /// <summary>One line per war, for Argus and the dashboard.</summary>
    public static string Tell()
    {
        if (_wars.Count == 0)
        {
            return "no war is standing";
        }

        using var say = Server.Text.ValueStringBuilder.Create(256);

        foreach (var war in _wars.Values)
        {
            if (say.Length > 0)
            {
                say.Append("; ");
            }

            say.Append(
                $"{war.Declarer} on {war.Against(war.Declarer)} for {war.Minutes} min over {war.Why}{(war.Joined == null ? "" : " (joined)")}: {war.A} {war.KillsA} dead {war.LootA}gp, {war.B} {war.KillsB} dead {war.LootB}gp"
            );
            say.Append(
                $"; in it {Involved(war.A)} of {war.A} (wanted {Wanted(war.A)}) and {Involved(war.B)} of {war.B} (wanted {Wanted(war.B)}){(war.Big ? ", a big war" : "")}"
            );
        }

        return say.ToString();
    }

    /// <summary>Part of the estate's line.</summary>
    public static string Describe() =>
        !Running
            ? "wars are not ruled"
            : $"{_wars.Count} wars standing ({Tell()}); won at {Kills} dead or {Loot}gp of plunder, no peace before {LeastMs / 60000} minutes, "
            + $"each side fought with {Engage} and one more for every {1.0 / Math.Max(0.01, PerKillBehind):0.#} kills it is behind, a big war past {BigAt} on each side ({Bigs} so far), "
            + $"judged at {LongestMs / 60000}, a truce of {TruceMs / 60000} after, one declaration a guild per {DeclareEveryMs / 60000} minutes and {MostWars} at a time; "
            + $"{Declared} declared, {Truced} refused for a truce, {Cooling} for declaring too soon ({LosersCooled} clocks started by a defeat), {Busy} for a guild already at war, {Daunted} against a guild {DauntedAt:0.#} times the declarer's might or more; "
            + $"{WonByBlood} won by blood, {WonByPlunder} by plunder, {Capitulated} by the other side's capitulation ({YieldedOnScore} on the score, at {YieldBehind} kills and {YieldRatio:0.#} times its own, against {YieldMight:0.#} times its might; {YieldedOutmatched} outmatched, fielding under 1/{YieldOdds:0.#} of the enemy's might once {LeastMs / 60000} minutes had passed, {HeldByBlood} looks where the smaller side fought on for being ahead on blood; {BotClaim.Conceded} claims on the loser's ground decided for the winner by a capitulation), {TimedOut} judged by the clock, {Peaced} ended in peace, {Drawn} of those drawn; "
            + $"{Counted} kills and {Plundered}gp of plunder counted; {BotSeizure.Describe()}";

    public static void Forget()
    {
        // The seizures remembered go with the wars, on a reload and at Patrick's reset alike. See BotSeizure.
        BotSeizure.Wipe();
        _wars.Clear();
        _truces.Clear();
        _declared.Clear();
        _over.Clear();
        _looked = 0;
        Declared = 0;
        Truced = 0;
        Cooling = 0;
        Busy = 0;
        WonByBlood = 0;
        WonByPlunder = 0;
        TimedOut = 0;
        Peaced = 0;
        Drawn = 0;
        Capitulated = 0;
        YieldedOnScore = 0;
        YieldedOutmatched = 0;
        HeldByBlood = 0;
        Counted = 0;
        Plundered = 0;
        Bigs = 0;
        _strength.Clear();
        _free.Clear();
        _mustered.Clear();
    }
}
