using System;
using System.Collections.Generic;
using System.Text.Json;
using Server.Guilds;
using Server.Logging;
using Server.Mobiles;
using Server.Text;

namespace Server.BotAI.V2;

/// <summary>
/// Who leads each guild, by the members' vote: elections of the head every few hours or when the head is gone or has fallen,
/// and the plot that can depose a head the members have lost faith in.
///
/// <para>
/// <b>Patrick's order of 07.10.2026, night: "elections of the guild head (members vote by the candidate's appeal: kills,
/// generosity, victories); a conspiracy: the discontented can depose the head".</b> Until now a guild's head was whoever the
/// engine had put there — the founder, or, when a head was deleted, the member the engine's getter drew by fealty
/// (<c>Guild.CalculateGuildmaster</c>) — and nothing the members did or thought ever changed it.
/// </para>
///
/// <para>
/// <b>The vote.</b> Every <see cref="EveryMs"/> of running time, at once when the head has left or been deleted, and when the
/// head falls (<see cref="OnFall"/>, not inside <see cref="GapMs"/> of the last vote), the guild's <see cref="Candidates"/> best
/// members by their record stand, with the head. A candidate's appeal is its renown (the engine's fame, which kills earn,
/// times <see cref="FameWeight"/>), its kills of other guilds' members (<see cref="KillWeight"/>), the wars its guild won while
/// it was a member (<see cref="WinWeight"/>), and the coin it gave the chest as dues (one point per <see cref="GiftPer"/>gp);
/// each voter weighs that by its own needs — a poor voter counts gifts <see cref="PoorGifts"/> times, a fighter counts kills
/// <see cref="FighterKills"/> times — and adds its own regard of the candidate and the candidate's being a veteran when those
/// are wired (<see cref="BotPolitics.TiesRegard"/>, <see cref="BotPolitics.Veteran"/>). Every member votes for the candidate
/// that appeals to it most (a candidate for itself); the most votes win, the better record breaks a tie. The winner becomes the
/// engine's leader with every member's fealty sworn to it (<see cref="BotGuilds.Crown"/>), so the engine's own daily count
/// agrees, and the "head never leaves" rule holds for it as for any head.
/// </para>
///
/// <para>
/// <b>The plot.</b> Every <see cref="PlotLookMs"/>, not while the guild is at war and not inside <see cref="GapMs"/> of a vote,
/// the members are asked whether they are discontented: the guild lost <see cref="LostInRow"/> wars in a row, or the member
/// stands under the keep while the chest holds the rich mark and has paid for nothing in <see cref="UnpaidMs"/>, or its regard
/// of the head is <see cref="LowRegard"/> or worse (when wired). At <see cref="DiscontentShare"/> of the members they call a
/// vote of no confidence: the discontented vote against, and so does any content member to whom a rival appeals
/// <see cref="RivalMargin"/> times as much as the head. Above <see cref="CarryShare"/> it carries, and an election is held at
/// once without the deposed head, who stays a member.
/// </para>
/// </summary>
public static class BotElection
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotElection));

    /// <summary>Whether guilds elect their heads. Off, and the head is whoever the engine has, as before.</summary>
    public static bool Running { get; set; } = true;

    /// <summary>A head's term, in milliseconds of running time: six hours.</summary>
    public static int EveryMs { get; set; } = 21600000;

    /// <summary>A guild with no election on record holds its first within this of the module starting, spread. An hour.</summary>
    public static int FirstMs { get; set; } = 3600000;

    /// <summary>The least running time between two votes of one guild when the head fell or a plot was afoot: an hour.</summary>
    public static int GapMs { get; set; } = 3600000;

    /// <summary>Whether a head's fall calls a vote (outside <see cref="GapMs"/>). The fallen head stands again on its record.</summary>
    public static bool OnFall { get; set; } = true;

    /// <summary>How many members stand besides the head: the best by record.</summary>
    public static int Candidates { get; set; } = 3;

    /// <summary>Appeal per point of the engine's fame: a hundred fame is one point.</summary>
    public static double FameWeight { get; set; } = 0.01;

    /// <summary>Appeal per kill of another guild's member.</summary>
    public static double KillWeight { get; set; } = 5.0;

    /// <summary>Appeal per war the guild won while the candidate was in it.</summary>
    public static double WinWeight { get; set; } = 10.0;

    /// <summary>Gold given to the chest as dues for one point of appeal.</summary>
    public static int GiftPer { get; set; } = 100;

    /// <summary>Appeal per point of the voter's regard of the candidate, when <see cref="BotPolitics.TiesRegard"/> is wired.</summary>
    public static double RegardWeight { get; set; } = 0.2;

    /// <summary>Appeal a veteran candidate has, when <see cref="BotPolitics.Veteran"/> is wired.</summary>
    public static double VeteranBonus { get; set; } = 15.0;

    /// <summary>Appeal the sitting head has over an equal rival: the known hand.</summary>
    public static double Incumbency { get; set; } = 5.0;

    /// <summary>How many times a voter under the keep counts a candidate's gifts to the chest.</summary>
    public static double PoorGifts { get; set; } = 2.0;

    /// <summary>How many times a fighting voter counts a candidate's renown and kills.</summary>
    public static double FighterKills { get; set; } = 1.5;

    /// <summary>Whether a candidate votes for itself.</summary>
    public static bool SelfVote { get; set; } = true;

    /// <summary>Whether the discontented may depose a head.</summary>
    public static bool Plotting { get; set; } = true;

    /// <summary>The share of members (the head apart) that must be discontented to call a vote of no confidence.</summary>
    public static double DiscontentShare { get; set; } = 0.4;

    /// <summary>The share of members (the head apart) whose vote against carries it.</summary>
    public static double CarryShare { get; set; } = 0.5;

    /// <summary>Wars lost in a row that make every member discontented.</summary>
    public static int LostInRow { get; set; } = 2;

    /// <summary>How long a rich chest may have paid for nothing before a member under the keep is discontented: three hours.</summary>
    public static int UnpaidMs { get; set; } = 10800000;

    /// <summary>A member whose regard of the head is this or worse is discontented, when ties are wired.</summary>
    public static double LowRegard { get; set; } = -30.0;

    /// <summary>How often each guild is asked whether a plot is afoot: ten minutes.</summary>
    public static int PlotLookMs { get; set; } = 600000;

    /// <summary>
    /// How long a head elected after a vote of no confidence is spared another: three hours. Without it a reason no head can mend
    /// in an hour — a rich chest nobody needed to draw on — would depose one head an hour.
    /// </summary>
    public static int GraceMs { get; set; } = 10800000;

    /// <summary>A content member votes against the head when a rival appeals to it this many times as much.</summary>
    public static double RivalMargin { get; set; } = 1.25;

    /// <summary>Elections held, by what called them.</summary>
    public static long Held { get; private set; }

    public static long ByTerm { get; private set; }

    public static long ByGone { get; private set; }

    public static long ByFall { get; private set; }

    public static long ByVote { get; private set; }

    /// <summary>Elections that changed the head, and that kept it.</summary>
    public static long NewHeads { get; private set; }

    public static long Kept { get; private set; }

    /// <summary>Votes cast in all elections.</summary>
    public static long Votes { get; private set; }

    /// <summary>Heads elected before a restart put back at the head of their guilds after it.</summary>
    public static long Restored { get; private set; }

    /// <summary>Heads fallen inside <see cref="GapMs"/> of a vote, so no vote.</summary>
    public static long FellInGap { get; private set; }

    /// <summary>Plot looks that found discontent at all, and the members found discontented by reason.</summary>
    public static long PlotLooks { get; private set; }

    public static long ForLosses { get; private set; }

    public static long ForUnpaid { get; private set; }

    public static long ForDislike { get; private set; }

    /// <summary>Votes of no confidence called, carried and failed; plot looks held for a war.</summary>
    public static long Called { get; private set; }

    public static long Carried { get; private set; }

    public static long Failed { get; private set; }

    public static long AtWar { get; private set; }

    /// <summary>Kills of other guilds' members, wars won and dues given, counted towards the members' records.</summary>
    public static long KillsCounted { get; private set; }

    public static long WinsCounted { get; private set; }

    public static long GiftsCounted { get; private set; }

    /// <summary>One guild's office: who heads it by whose vote, and its run of lost wars.</summary>
    private sealed class Office
    {
        /// <summary>The head's name: elected, or as found when the guild was first looked at.</summary>
        public string Head;

        /// <summary>When the last vote was held (or the office opened), as a tick.</summary>
        public long At;

        public bool Elected;

        public int For;

        public int Of;

        public int Held;

        public int LostRow;

        public bool FellSeen;

        public long PlotAt;

        /// <summary>Before this tick no vote of no confidence: one failed.</summary>
        public long PlotCool;

        public int Discontent;

        public string Why;

        /// <summary>Read back from the store: the head is put back at the first look, if still a member.</summary>
        public bool Restore;
    }

    /// <summary>One member's record, by name: what it is elected by.</summary>
    private sealed class Record
    {
        public int WarKills;

        public int WarsWon;

        public long Given;
    }

    private static readonly Dictionary<string, Office> _offices = new(StringComparer.OrdinalIgnoreCase);

    private static readonly Dictionary<string, Record> _records = new(StringComparer.Ordinal);

    private static readonly List<(BotMobile Bot, double Score)> _cands = [];

    private static readonly List<Guild> _guilds = [];

    private static Office Get(string guild, long now)
    {
        if (!_offices.TryGetValue(guild, out var office))
        {
            // The first vote within FirstMs, spread, so the guilds do not all vote in one minute.
            var delay = Math.Max(0, FirstMs) / 2 + Utility.Random(Math.Max(1, FirstMs));

            _offices[guild] = office = new Office
            {
                At = now - Math.Max(0, EveryMs) + delay,
                PlotAt = now - Utility.Random(Math.Max(1, PlotLookMs)),
                PlotCool = now
            };
        }

        return office;
    }

    private static Record Of(string name)
    {
        if (!_records.TryGetValue(name, out var rec))
        {
            _records[name] = rec = new Record();
        }

        return rec;
    }

    // ---- what feeds the records --------------------------------------------------------------------------------------------

    /// <summary>A member of one guild killed a member of another. From <c>BotMobile.OnDeath</c>.</summary>
    public static void Slew(BotMobile slayer, Mobile fallen)
    {
        if (slayer?.Name == null || slayer.Guild is not Guild mine || fallen?.Guild is not Guild theirs || mine == theirs)
        {
            return;
        }

        Of(slayer.Name).WarKills++;
        KillsCounted++;
    }

    /// <summary>A member paid dues into its guild's chest. From <c>BotDues.Pay</c>.</summary>
    public static void Gave(BotMobile bot, int gold)
    {
        if (bot?.Name == null || gold <= 0)
        {
            return;
        }

        Of(bot.Name).Given += gold;
        GiftsCounted += gold;
    }

    /// <summary>
    /// A war ended: <paramref name="winner"/> null for a draw, which breaks both sides' runs. Every member of the winner has a war
    /// won to its name; the loser's run of losses grows. From <c>BotWar.End</c>.
    /// </summary>
    public static void WarEnded(string a, string b, string winner)
    {
        if (a == null || b == null)
        {
            return;
        }

        var now = Core.TickCount;

        if (winner == null)
        {
            Get(a, now).LostRow = 0;
            Get(b, now).LostRow = 0;

            return;
        }

        var loser = winner == a ? b : a;

        Get(winner, now).LostRow = 0;
        Get(loser, now).LostRow++;

        if (BotGuilds.Named(winner) is { Members: { } members })
        {
            for (var i = 0; i < members.Count; i++)
            {
                if (members[i] is BotMobile { Deleted: false, Name: { } name })
                {
                    Of(name).WarsWon++;
                    WinsCounted++;
                }
            }
        }
    }

    // ---- the look ----------------------------------------------------------------------------------------------------------

    /// <summary>Once a minute: heads gone or fallen, terms run out, and plots.</summary>
    public static void Look(long now)
    {
        if (!Running)
        {
            return;
        }

        _guilds.Clear();

        foreach (var guild in BotGuilds.Standing)
        {
            _guilds.Add(guild);
        }

        for (var g = 0; g < _guilds.Count; g++)
        {
            var guild = _guilds[g];

            if (guild is not { Disbanded: false } || BotUnderworld.Band(guild) || guild.Members == null || guild.Members.Count == 0)
            {
                continue;
            }

            var office = Get(guild.Name, now);
            var head = BotGuilds.Head(guild);

            // The head elected before a restart is put back, if it is still a member.
            if (office.Restore)
            {
                office.Restore = false;

                if (office.Head != null && head?.Name != office.Head && Member(guild, office.Head) is { } elected && BotGuilds.Crown(guild, elected))
                {
                    Restored++;
                    head = elected;

                    logger.Information("{Guild}'s elected head {Head} is its head again after the restart", guild.Name, elected.Name);
                }
            }

            // A guild first looked at: its head is taken as found, and the first vote is due within FirstMs.
            if (office.Head == null)
            {
                office.Head = head?.Name;

                continue;
            }

            if (head == null || head.Name != office.Head)
            {
                Elect(guild, office, head == null ? $"{office.Head} is gone" : $"{office.Head} is no longer its head", 1, null, now);

                continue;
            }

            if (!head.Alive)
            {
                if (OnFall && !office.FellSeen)
                {
                    office.FellSeen = true;

                    if (now - office.At >= GapMs)
                    {
                        Elect(guild, office, $"{head.Name} fell", 2, null, now);

                        continue;
                    }

                    FellInGap++;
                }
            }
            else
            {
                office.FellSeen = false;
            }

            if (now - office.At >= EveryMs)
            {
                Elect(guild, office, $"{head.Name}'s term of {EveryMs / 3600000}h ran out", 0, null, now);

                continue;
            }

            if (Plotting && now - office.PlotAt >= PlotLookMs)
            {
                office.PlotAt = now;
                Plot(guild, office, head, now);
            }
        }
    }

    private static BotMobile Member(Guild guild, string name)
    {
        for (var i = 0; i < guild.Members.Count; i++)
        {
            if (guild.Members[i] is BotMobile { Deleted: false } bot && bot.Name == name)
            {
                return bot;
            }
        }

        return null;
    }

    // ---- appeal ------------------------------------------------------------------------------------------------------------

    /// <summary>Whether this bot fights for a living: its best fighting skill is fifty or more.</summary>
    public static bool Fighter(Mobile bot)
    {
        if (bot == null)
        {
            return false;
        }

        var s = bot.Skills;
        var best = Math.Max(
            Math.Max(Math.Max(s.Swords.Value, s.Macing.Value), Math.Max(s.Fencing.Value, s.Archery.Value)),
            Math.Max(s.Wrestling.Value, s.Magery.Value)
        );

        return best >= 50.0;
    }

    /// <summary>A candidate's record as anybody would read it: renown, kills, victories, gifts and veteran standing.</summary>
    private static double Record0(BotMobile cand)
    {
        var rec = _records.GetValueOrDefault(cand.Name);
        var score = Math.Max(0, cand.Fame) * FameWeight;

        if (rec != null)
        {
            score += rec.WarKills * KillWeight + rec.WarsWon * WinWeight + (GiftPer > 0 ? rec.Given / (double)GiftPer : 0.0);
        }

        return score + (BotPolitics.IsVeteran(cand) ? VeteranBonus : 0.0);
    }

    /// <summary>What a candidate is worth to this voter: the record weighed by the voter's needs, its regard, and its own name.</summary>
    private static double Appeal(BotMobile voter, BotMobile cand, BotMobile incumbent)
    {
        var rec = _records.GetValueOrDefault(cand.Name);
        var deeds = Math.Max(0, cand.Fame) * FameWeight + (rec?.WarKills ?? 0) * KillWeight;
        var gifts = rec == null || GiftPer <= 0 ? 0.0 : rec.Given / (double)GiftPer;

        if (Fighter(voter))
        {
            deeds *= FighterKills;
        }

        if (BotYield.Spendable(voter) < BotEstate.Keep)
        {
            gifts *= PoorGifts;
        }

        var appeal = deeds + gifts + (rec?.WarsWon ?? 0) * WinWeight + (BotPolitics.IsVeteran(cand) ? VeteranBonus : 0.0)
                     + RegardWeight * BotPolitics.RegardOf(voter, cand);

        if (ReferenceEquals(cand, incumbent))
        {
            appeal += Incumbency;
        }

        if (SelfVote && ReferenceEquals(cand, voter))
        {
            appeal += 1000000.0;
        }

        return appeal;
    }

    /// <summary>The candidates: the best <see cref="Candidates"/> members by record, and the head, none of them <paramref name="barred"/>.</summary>
    private static void Stand(Guild guild, BotMobile head, BotMobile barred)
    {
        _cands.Clear();

        var most = Math.Max(1, Candidates);

        for (var i = 0; i < guild.Members.Count; i++)
        {
            if (guild.Members[i] is not BotMobile { Deleted: false } m || ReferenceEquals(m, barred) || ReferenceEquals(m, head)
                || BotGuilds.Outside(m))
            {
                continue;
            }

            var score = Record0(m);
            var at = _cands.Count;

            while (at > 0 && _cands[at - 1].Score < score)
            {
                at--;
            }

            if (at < most)
            {
                _cands.Insert(at, (m, score));

                if (_cands.Count > most)
                {
                    _cands.RemoveAt(_cands.Count - 1);
                }
            }
        }

        if (head is { Deleted: false } && !ReferenceEquals(head, barred) && head.Guild == guild)
        {
            _cands.Add((head, Record0(head)));
        }
    }

    // ---- the vote ----------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Holds an election: candidates stand, every member votes, the winner is crowned. <paramref name="cause"/>: 0 a term, 1 the
    /// head gone, 2 the head fallen, 3 no confidence (<paramref name="barred"/> the deposed head).
    /// </summary>
    private static void Elect(Guild guild, Office office, string why, int cause, BotMobile barred, long now)
    {
        var was = BotGuilds.Head(guild);

        Stand(guild, was, barred);

        // Whatever happens, the clock moves: a guild that cannot elect is not asked again every minute.
        office.At = now;

        if (_cands.Count == 0)
        {
            office.Head = was?.Name;

            return;
        }

        Span<int> counts = stackalloc int[_cands.Count];
        var voters = 0;

        for (var i = 0; i < guild.Members.Count; i++)
        {
            if (guild.Members[i] is not BotMobile { Deleted: false } voter)
            {
                continue;
            }

            var best = 0;
            var bestAppeal = double.MinValue;

            for (var c = 0; c < _cands.Count; c++)
            {
                var appeal = Appeal(voter, _cands[c].Bot, was);

                if (appeal > bestAppeal)
                {
                    bestAppeal = appeal;
                    best = c;
                }
            }

            counts[best]++;
            voters++;
        }

        var win = 0;

        for (var c = 1; c < _cands.Count; c++)
        {
            if (counts[c] > counts[win] || counts[c] == counts[win] && _cands[c].Score > _cands[win].Score)
            {
                win = c;
            }
        }

        var second = -1;

        for (var c = 0; c < _cands.Count; c++)
        {
            if (c != win && (second < 0 || counts[c] > counts[second]))
            {
                second = c;
            }
        }

        var winner = _cands[win].Bot;

        if (!BotGuilds.Crown(guild, winner))
        {
            office.Head = was?.Name;

            return;
        }

        var changed = !ReferenceEquals(winner, was);

        office.Head = winner.Name;
        office.Elected = true;
        office.For = counts[win];
        office.Of = voters;
        office.Held++;
        office.Why = why;
        office.FellSeen = !winner.Alive;
        office.PlotCool = now;

        if (changed)
        {
            office.LostRow = 0;
            NewHeads++;
        }
        else
        {
            Kept++;
        }

        Held++;
        Votes += voters;

        switch (cause)
        {
            case 1:
                {
                    ByGone++;

                    break;
                }
            case 2:
                {
                    ByFall++;

                    break;
                }
            case 3:
                {
                    ByVote++;

                    break;
                }
            default:
                {
                    ByTerm++;

                    break;
                }
        }

        var rec = _records.GetValueOrDefault(winner.Name);
        var runner = second >= 0 ? $"; {_cands[second].Bot.Name} had {counts[second]}" : "";

        logger.Information(
            "{Guild} {Did} {Head} the {Class} its head over {Why}: {For} of {Of} votes{Runner}; renown {Fame}, {Kills} kills of other guilds, {Wins} wars won, {Given}gp given to the chest{Was}",
            guild.Name,
            changed ? "elected" : "re-elected",
            winner.Name,
            winner.Class?.Name ?? "?",
            why,
            counts[win],
            voters,
            runner,
            winner.Fame,
            rec?.WarKills ?? 0,
            rec?.WarsWon ?? 0,
            rec?.Given ?? 0,
            changed && was != null ? $", in place of {was.Name}" : ""
        );

        var fill = new Dictionary<string, string>
        {
            ["head"] = winner.Name,
            ["votes"] = counts[win].ToString(),
            ["of"] = voters.ToString(),
            ["was"] = was?.Name ?? "nobody"
        };

        BotVoice.ToGuild(guild.Name, changed ? "politics:elected" : "politics:reelected", fill);
        BotEvents.Post("politics", winner, $"{winner.Name} {(changed ? "elected" : "re-elected")} head of {guild.Name}: {counts[win]} of {voters} votes ({why})");
    }

    // ---- the plot ----------------------------------------------------------------------------------------------------------

    private static void Plot(Guild guild, Office office, BotMobile head, long now)
    {
        if (now - office.At < GapMs || now - office.PlotCool < 0)
        {
            return;
        }

        if (BotWar.Fighting(guild.Name) > 0)
        {
            AtWar++;

            return;
        }

        // Under this head: a chest that paid nothing for three hours counts against the head only for the hours it has led.
        var lost = LostInRow > 0 && office.LostRow >= LostInRow;
        var unpaid = BotChest.Holds(guild.Name) >= BotGuilds.RichChest && Math.Min(BotChest.SinceDraw(guild.Name), now - office.At) >= UnpaidMs;
        var ties = BotPolitics.TiesRegard != null;

        if (!lost && !unpaid && !ties)
        {
            office.Discontent = 0;

            return;
        }

        var members = 0;
        var discontented = 0;
        BotMobile plotter = null;
        string reason = null;

        for (var i = 0; i < guild.Members.Count; i++)
        {
            if (guild.Members[i] is not BotMobile { Deleted: false } m || ReferenceEquals(m, head))
            {
                continue;
            }

            members++;

            string why = null;

            if (lost)
            {
                why = $"{office.LostRow} wars lost in a row";
                ForLosses++;
            }
            else if (unpaid && BotYield.Spendable(m) < BotEstate.Keep)
            {
                why = $"the chest holds {BotChest.Holds(guild.Name)}gp and has paid for nothing in {BotChest.SinceDraw(guild.Name) / 3600000}h";
                ForUnpaid++;
            }
            else if (ties && BotPolitics.RegardOf(m, head) <= LowRegard)
            {
                why = "nobody likes the head";
                ForDislike++;
            }

            if (why != null)
            {
                discontented++;

                if (plotter == null && m.Alive && m.Map != null && m.Map != Map.Internal)
                {
                    plotter = m;
                    reason = why;
                }
            }
        }

        office.Discontent = discontented;

        if (discontented == 0)
        {
            return;
        }

        PlotLooks++;

        if (members == 0 || discontented < DiscontentShare * members)
        {
            return;
        }

        Called++;

        // The vote: the discontented against; a content member against when a rival appeals to it enough more than the head.
        Stand(guild, null, head);

        var against = discontented;

        for (var i = 0; i < guild.Members.Count; i++)
        {
            if (guild.Members[i] is not BotMobile { Deleted: false } m || ReferenceEquals(m, head) || _cands.Count == 0)
            {
                continue;
            }

            if (lost || unpaid && BotYield.Spendable(m) < BotEstate.Keep || ties && BotPolitics.RegardOf(m, head) <= LowRegard)
            {
                continue;
            }

            var mine = Appeal(m, head, head);
            var rival = double.MinValue;

            for (var c = 0; c < _cands.Count; c++)
            {
                if (!ReferenceEquals(_cands[c].Bot, m))
                {
                    rival = Math.Max(rival, Appeal(m, _cands[c].Bot, head));
                }
            }

            if (rival > 0.0 && rival > mine * RivalMargin)
            {
                against++;
            }
        }

        var fill = new Dictionary<string, string>
        {
            ["head"] = head.Name,
            ["reason"] = reason ?? "",
            ["against"] = against.ToString(),
            ["of"] = members.ToString()
        };

        if (plotter != null && BotVoice.Phrase("politics:plot", null, plotter, fill) is { } line)
        {
            BotVoice.Say(plotter, "guild", line, force: true);
        }

        if (against > CarryShare * members)
        {
            Carried++;

            logger.Warning(
                "{Guild} has deposed {Head}: {Against} of {Members} voted no confidence, {Discontented} of them discontented ({Reason}); an election at once without it",
                guild.Name,
                head.Name,
                against,
                members,
                discontented,
                reason
            );

            BotVoice.ToGuild(guild.Name, "politics:deposed", fill);
            BotEvents.Post("politics", head, $"{guild.Name} deposed {head.Name}: {against} of {members} voted no confidence ({reason})");

            Elect(guild, office, $"no confidence in {head.Name}", 3, head, now);
            office.PlotCool = now + GraceMs;

            return;
        }

        Failed++;
        office.PlotCool = now + GapMs;

        logger.Information(
            "{Guild} kept {Head} at its head: a vote of no confidence called by {Discontented} discontented ({Reason}) found {Against} of {Members} against it",
            guild.Name,
            head.Name,
            discontented,
            reason,
            against,
            members
        );

        BotVoice.ToGuild(guild.Name, "politics:confidence", fill);
    }

    // ---- telling -----------------------------------------------------------------------------------------------------------

    /// <summary>The guild's office on the page: its head by vote, the votes, when, and the run of lost wars.</summary>
    public static void Write(Utf8JsonWriter w, string guild)
    {
        if (guild == null || !_offices.TryGetValue(guild, out var office))
        {
            w.WriteBoolean("elected", false);

            return;
        }

        w.WriteBoolean("elected", office.Elected);
        BotJson.StringOrNull(w, "electedHead", office.Head);
        w.WriteNumber("votes", office.For);
        w.WriteNumber("voters", office.Of);
        w.WriteNumber("electedMin", (int)((Core.TickCount - office.At) / 60000));
        w.WriteNumber("elections", office.Held);
        BotJson.StringOrNull(w, "electedOver", office.Why);
        w.WriteNumber("lostInRow", office.LostRow);
        w.WriteNumber("discontent", office.Discontent);
    }

    /// <summary>The <c>Guilds:</c> line's clause.</summary>
    public static string Describe()
    {
        if (!Running)
        {
            return "elections are OFF: the engine's head stands";
        }

        using var say = ValueStringBuilder.Create(512);

        say.Append(
            $"elections every {EveryMs / 3600000}h of running, at once for a head gone{(OnFall ? $", for a head fallen outside {GapMs / 60000}m of a vote" : "")}: {Held} held ({ByTerm} for a term, {ByGone} for a head gone, {ByFall} for a head fallen, {ByVote} after no confidence), {NewHeads} new heads and {Kept} re-elected, {Votes} votes cast, {Restored} elected heads put back after a restart, {FellInGap} falls inside the gap; "
        );
        say.Append(
            $"records: {KillsCounted} kills of other guilds, {WinsCounted} wars won to members' names, {GiftsCounted}gp of dues given; "
        );
        say.Append(
            Plotting
                ? $"plots: {PlotLooks} looks found discontent ({ForLosses} for {LostInRow} wars lost in a row, {ForUnpaid} under the keep while a rich chest paid nothing for {UnpaidMs / 3600000}h, {ForDislike} disliking the head), {Called} votes of no confidence at {DiscontentShare:P0}, {Carried} carried over {CarryShare:P0} and {Failed} failed, {AtWar} looks held for a war; "
                : "plots are OFF; "
        );
        say.Append(BotPolitics.TiesRegard == null ? "ties not wired; " : "ties wired; ");
        say.Append(BotPolitics.Veteran == null ? "veterans not wired; heads: " : "veterans wired; heads: ");

        var first = true;

        foreach (var (name, office) in _offices)
        {
            if (office.Head == null)
            {
                continue;
            }

            say.Append(first ? "" : ", ");
            first = false;
            say.Append($"{name} {office.Head}");
            say.Append(
                office.Elected
                    ? $" ({office.For}/{office.Of}, {(Core.TickCount - office.At) / 60000}m ago, {office.Held} votes{(office.LostRow > 0 ? $", {office.LostRow} lost" : "")}{(office.Discontent > 0 ? $", {office.Discontent} discontented" : "")})"
                    : " (as found)"
            );
        }

        if (first)
        {
            say.Append("none yet");
        }

        return say.ToString();
    }

    // ---- the store ---------------------------------------------------------------------------------------------------------

    internal static void Save(IGenericWriter writer)
    {
        var now = Core.TickCount;

        writer.WriteEncodedInt(_offices.Count);

        foreach (var (name, office) in _offices)
        {
            writer.Write(name);
            writer.Write(office.Head ?? "");
            writer.Write(Math.Max(0L, now - office.At));
            writer.Write(office.Elected);
            writer.WriteEncodedInt(office.For);
            writer.WriteEncodedInt(office.Of);
            writer.WriteEncodedInt(office.Held);
            writer.WriteEncodedInt(office.LostRow);
            writer.Write(office.Why ?? "");
        }

        var kept = 0;

        foreach (var (_, rec) in _records)
        {
            if (rec.WarKills != 0 || rec.WarsWon != 0 || rec.Given != 0)
            {
                kept++;
            }
        }

        writer.WriteEncodedInt(kept);

        foreach (var (name, rec) in _records)
        {
            if (rec.WarKills == 0 && rec.WarsWon == 0 && rec.Given == 0)
            {
                continue;
            }

            writer.Write(name);
            writer.WriteEncodedInt(rec.WarKills);
            writer.WriteEncodedInt(rec.WarsWon);
            writer.Write(rec.Given);
        }
    }

    internal static (int Offices, int Records) Load(IGenericReader reader)
    {
        var now = Core.TickCount;
        var offices = reader.ReadEncodedInt();

        for (var i = 0; i < offices; i++)
        {
            var name = reader.ReadString();
            var head = reader.ReadString();
            var ago = reader.ReadLong();
            var elected = reader.ReadBool();
            var votes = reader.ReadEncodedInt();
            var of = reader.ReadEncodedInt();
            var held = reader.ReadEncodedInt();
            var lost = reader.ReadEncodedInt();
            var why = reader.ReadString();

            if (string.IsNullOrEmpty(name))
            {
                continue;
            }

            _offices[name] = new Office
            {
                // A head only adopted, never elected, is adopted afresh: the population after a restart may have another.
                Head = elected && !string.IsNullOrEmpty(head) ? head : null,
                At = now - Math.Clamp(ago, 0L, (long)int.MaxValue),
                Elected = elected,
                For = votes,
                Of = of,
                Held = held,
                LostRow = lost,
                Why = string.IsNullOrEmpty(why) ? null : why,
                Restore = elected && !string.IsNullOrEmpty(head),
                PlotAt = now - Utility.Random(Math.Max(1, PlotLookMs)),
                PlotCool = now
            };
        }

        var records = reader.ReadEncodedInt();

        for (var i = 0; i < records; i++)
        {
            var name = reader.ReadString();
            var kills = reader.ReadEncodedInt();
            var wins = reader.ReadEncodedInt();
            var given = reader.ReadLong();

            if (!string.IsNullOrEmpty(name))
            {
                _records[name] = new Record { WarKills = kills, WarsWon = wins, Given = given };
            }
        }

        return (offices, records);
    }

    public static void Forget()
    {
        Held = 0;
        ByTerm = 0;
        ByGone = 0;
        ByFall = 0;
        ByVote = 0;
        NewHeads = 0;
        Kept = 0;
        Votes = 0;
        Restored = 0;
        FellInGap = 0;
        PlotLooks = 0;
        ForLosses = 0;
        ForUnpaid = 0;
        ForDislike = 0;
        Called = 0;
        Carried = 0;
        Failed = 0;
        AtWar = 0;
        KillsCounted = 0;
        WinsCounted = 0;
        GiftsCounted = 0;
    }

    /// <summary>
    /// Every guild's office and every member's record forgotten, for Patrick's reset (07.10.2026, reset26): exactly what
    /// <see cref="Save"/> writes. Kept by the guild's name and the member's, so a new world that founds a guild under an old name
    /// would otherwise seat the old head again by its record. See <see cref="BotPolitics.Wipe"/>. Returns the offices forgotten.
    /// </summary>
    public static int Wipe()
    {
        var offices = _offices.Count;

        _offices.Clear();
        _records.Clear();
        _cands.Clear();
        _guilds.Clear();
        Forget();

        return offices;
    }
}
