using System;
using System.Collections.Generic;
using System.Text.Json;
using Server.Guilds;
using Server.Logging;
using Server.Mobiles;

namespace Server.BotAI.V2;

/// <summary>
/// The herald's work: a recruiting stand by the bank of the guild's town, the guild's news read aloud there, and a challenge to
/// a rival guild's bout.
///
/// <para>
/// <b>A shift like a shop's.</b> The herald walks to the banker nearest its guild's seat (<see cref="BotShopkeep.BankNear"/>)
/// and stands <see cref="ShiftMinutes"/> there, then rests <see cref="RestMs"/>. Every <see cref="LookMs"/> it looks round
/// <see cref="Reach"/> tiles: a bot in no guild that may join one, or a member of another guild that is discontented in it — it
/// thinks <see cref="SourRegard"/> or worse of its head, or its guild has nothing to show (<see cref="BotGuilds.Worth(Guild)"/>
/// nought) while the herald's has a hall or ground — is invited once in <see cref="InviteRestMs"/>, and joins at
/// <see cref="JoinChance"/> (the unguilded) or <see cref="CrossChance"/> (the discontented), by the guild's own doors
/// (<see cref="BotGuilds.Recruit"/>, <see cref="BotGuilds.Poach"/>) with every one of their rules. Never across a war, never an
/// outlaw, never a head or an officer, never out of a guild at its fewest.
/// </para>
///
/// <para>
/// <b>News.</b> Every <see cref="NewsEveryMs"/> the herald reads one piece aloud: what the officers and the folders have posted
/// (<see cref="BotOfficers.News"/>), else a standing fact — a war and its score, the chest and what it saves for, the store.
/// </para>
///
/// <para>
/// <b>A bout.</b> Once in <see cref="BotGuildBout.EveryMs"/> a herald at its stand challenges the guild nearest its own in might
/// that is at war with nobody's side of it (<see cref="BotGuildBout"/>).
/// </para>
/// </summary>
public sealed class BotHeraldStand : IBotProposer
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotHeraldStand));

    public string Name => "Herald stand";

    public BotStanding Rung => BotStanding.Free;

    public static double Prior { get; set; } = 26.0;

    public static int ShiftMinutes { get; set; } = 10;

    public static int RestMs { get; set; } = 2700000;

    /// <summary>How far from the stand a herald is offered its shift.</summary>
    public static int Far { get; set; } = 200;

    public static int StandTiles { get; set; } = 5;

    public static int LookMs { get; set; } = 6000;

    public static int Reach { get; set; } = 14;

    public static int InviteRestMs { get; set; } = 7200000;

    public static double JoinChance { get; set; } = 0.6;

    public static double CrossChance { get; set; } = 0.35;

    /// <summary>What a member must think of its head, or worse, to be counted discontented. Politics' own bar.</summary>
    public static int SourRegard { get; set; } = -30;

    /// <summary>Whether discontented members of other guilds are invited at all.</summary>
    public static bool Poaching { get; set; } = true;

    public static int NewsEveryMs { get; set; } = 75000;

    // ---- counters ----------------------------------------------------------------------------------------------------------

    public static long Stands { get; private set; }

    public static long StandMs { get; private set; }

    public static long Looks { get; private set; }

    public static long Invited { get; private set; }

    public static long InvitedSour { get; private set; }

    public static long Joined { get; private set; }

    public static long Crossed { get; private set; }

    public static long Declined { get; private set; }

    public static long Refused { get; private set; }

    public static long NewsRead { get; private set; }

    public static long FactsRead { get; private set; }

    private static readonly Dictionary<string, long> _gates = new(StringComparer.Ordinal);

    private static readonly Dictionary<string, (int Invited, int Joined, int Read)> _byGuild = new(StringComparer.Ordinal);

    private static readonly Dictionary<Serial, long> _rested = [];

    private static readonly Dictionary<Serial, long> _invited = [];

    private static readonly Dictionary<string, int> _facts = new(StringComparer.Ordinal);

    private static void Gate(string why) => _gates[why] = _gates.GetValueOrDefault(why) + 1;

    public BotDeed Propose(IBotWilful bot)
    {
        if (!BotOfficers.On(BotOfficerPost.Herald) || bot?.Self is not BotMobile body || body.Guild is not Guild guild)
        {
            return null;
        }

        if (!BotOfficers.Holds(body, BotOfficerPost.Herald) || !body.Alive || body.Tired || body.Squad != null || body.Resolve?.Deed is Stand)
        {
            return null;
        }

        var now = Core.TickCount;

        if (_rested.TryGetValue(body.Serial, out var last) && now - last < RestMs)
        {
            return null;
        }

        var map = body.Map;

        if (map == null || map != BotPopulation.Home || BotDungeon.Under(body.Location))
        {
            Gate("off the home ground");

            return null;
        }

        var seat = BotSeat.Of(guild);
        var bank = BotShopkeep.BankNear(map, seat == Point3D.Zero ? BotPopulation.Where : seat);

        if (bank == Point3D.Zero)
        {
            Gate("no bank near the guild's seat");

            return null;
        }

        if (!body.InRange(bank, Far))
        {
            Gate("far from the stand");

            return null;
        }

        if (!BotGates.Joined(map, body.Location, bank))
        {
            Gate("no road to the stand");

            return null;
        }

        return new Stand(guild, map, bank);
    }

    public sealed class Stand : BotDeed
    {
        public const string Trade = "herald-stand";

        private readonly Guild _guild;

        private readonly Map _map;

        private readonly Point3D _bank;

        private bool _in;

        private long _since;

        private long _looked;

        private long _read;

        private bool _challenged;

        private int _invited;

        private int _joined;

        private int _read2;

        public Stand(Guild guild, Map map, Point3D bank)
        {
            _guild = guild;
            _map = map;
            _bank = bank;
        }

        public override string Kind => Trade;

        public override Map Map => _map;

        public override Point3D Where => _bank;

        public override double Minutes => ShiftMinutes + 2.0;

        public override double Expects => Prior;

        public override bool Unpaid => true;

        public override bool Committed => _in;

        public override bool Still => _in;

        public override string Stage => _in ? $"speaking for {_guild.Name} by the bank" : $"on the way to speak for {_guild.Name}";

        public override void Taken(IBotWilful bot)
        {
            if (bot?.Self is { } body)
            {
                _rested[body.Serial] = Core.TickCount;
            }
        }

        public override void Drop(IBotWilful bot) => End(bot?.Self, false);

        private void End(Mobile body, bool stood)
        {
            if (_since != 0 && stood)
            {
                Stands++;
                StandMs += Core.TickCount - _since;
            }

            _since = 0;

            if (body != null)
            {
                _rested[body.Serial] = Core.TickCount;
            }
        }

        public override BotDoing Advance(IBotWilful bot)
        {
            if (bot?.Self is not BotMobile herald)
            {
                return BotDoing.Failed("no body");
            }

            if (_guild.Disbanded || !BotOfficers.Holds(herald, BotOfficerPost.Herald))
            {
                End(herald, false);

                return BotDoing.Done($"no longer {_guild.Name}'s herald");
            }

            if (!herald.InRange(_bank, StandTiles))
            {
                _in = false;

                return BotDoing.Walk(_map, _bank, BotArrival.Within(StandTiles - 1), $"to {_guild.Name}'s stand by the bank");
            }

            var now = Core.TickCount;

            if (!_in)
            {
                _in = true;

                if (_since == 0)
                {
                    _since = now;
                    _looked = now;
                    _read = now - NewsEveryMs + 5000;
                    herald.Journey?.Finish();
                }
            }

            if (now - _looked >= LookMs)
            {
                _looked = now;
                Looks++;

                var (i, j) = Recruit(herald, _guild, now);

                _invited += i;
                _joined += j;
            }

            if (now - _read >= NewsEveryMs)
            {
                _read = now;

                if (Read(herald, _guild))
                {
                    _read2++;
                }
            }

            if (!_challenged && now - _since >= 60000)
            {
                _challenged = true;
                BotGuildBout.Challenge(herald, _guild, _bank, now);
            }

            if (now - _since < ShiftMinutes * 60000L)
            {
                return BotDoing.Work($"speaking for {_guild.Name} by the bank");
            }

            var minutes = (now - _since) / 60000.0;

            End(herald, true);

            return BotDoing.Done($"spoke for {_guild.Name} by the bank {minutes:F1} minutes: {_invited} invited, {_joined} joined, {_read2} pieces of news read");
        }
    }

    /// <summary>One look round the stand. Returns the invited and the joined.</summary>
    private static (int Invited, int Joined) Recruit(BotMobile herald, Guild guild, long now)
    {
        var invited = 0;
        var joined = 0;

        foreach (var m in herald.Map.GetMobilesInRange<BotMobile>(herald.Location, Reach))
        {
            if (m is not { Deleted: false, Alive: true } || ReferenceEquals(m, herald) || ReferenceEquals(m.Guild, guild))
            {
                continue;
            }

            if (_invited.TryGetValue(m.Serial, out var last) && now - last < InviteRestMs)
            {
                continue;
            }

            var why = Invitable(m, guild, out var from);

            if (why != null)
            {
                if (why.Length > 0)
                {
                    Gate(why);
                }

                continue;
            }

            _invited[m.Serial] = now;
            invited++;
            Invited++;

            if (from != null)
            {
                InvitedSour++;
            }

            Tally(guild.Name, 1, 0, 0);

            var fill = new Dictionary<string, string> { ["other"] = m.Name };

            BotVoice.Say(herald, "local", BotVoice.Phrase("officers:invite", null, herald, fill) ?? $"{m.Name}! {guild.Name} has room for you.", force: true);

            if (Utility.RandomDouble() >= (from == null ? JoinChance : CrossChance))
            {
                Declined++;
                BotVoice.Say(herald, "local", BotVoice.Phrase("officers:declined", null, herald, fill) ?? $"Another time, then, {m.Name}.");

                logger.Information(
                    "{Herald}, {Guild}'s herald, invited {Bot} the {Class}{From} at the bank, and it declined",
                    herald.Name,
                    guild.Name,
                    m.Name,
                    m.Class?.Name,
                    from == null ? " (in no guild)" : $" (discontented in {from.Name})"
                );

                continue;
            }

            var reason = $"{herald.Name}, {guild.Name}'s herald, invited it at the bank{(from == null ? "" : $" away from {from.Name}")}";
            var took = from == null ? BotGuilds.Recruit(guild, m, reason) : BotGuilds.Poach(from, guild, m, reason);

            if (!took)
            {
                Refused++;
                Gate("the guild's own door refused");

                continue;
            }

            joined++;
            Tally(guild.Name, 0, 1, 0);

            if (from == null)
            {
                Joined++;
            }
            else
            {
                Crossed++;
            }

            BotVoice.Say(herald, "local", BotVoice.Phrase("officers:joined", null, herald, fill) ?? $"Welcome to {guild.Name}, {m.Name}!", force: true);
            BotEvents.Post("officers", m, from == null ? $"joined {guild.Name} at its herald's invitation" : $"left {from.Name} for {guild.Name} at its herald's invitation");
            BotOfficers.News(guild.Name, $"{m.Name} the {m.Class?.Name} has joined {guild.Name}.");
        }

        return (invited, joined);
    }

    /// <summary>
    /// Null when this bot may be invited (and <paramref name="from"/> is the guild it would leave, or null); otherwise why not,
    /// empty for a reason not worth counting.
    /// </summary>
    private static string Invitable(BotMobile m, Guild ours, out Guild from)
    {
        from = null;

        if (m.Squad != null || m.Map == null || m.Map == Map.Internal)
        {
            return "";
        }

        if (BotUnderworld.Outlawed(m) || BotUnderworld.Member(m))
        {
            return "an outlaw";
        }

        if (m.Guild is not Guild theirs)
        {
            if (BotGuilds.Outside(m))
            {
                return "";
            }

            return BotGuilds.Cools(m) ? "inside its walk-out bar" : null;
        }

        if (!Poaching || theirs.Disbanded)
        {
            return "";
        }

        if (BotWar.Of(ours.Name, theirs.Name) != null || ours.Enemies != null && ours.Enemies.Contains(theirs))
        {
            return "of a guild at war with ours";
        }

        if (BotGuilds.IsHead(m) || BotOfficers.OfficeOf(m) != null)
        {
            return "";
        }

        if (theirs.Members.Count <= BotGuilds.Least)
        {
            return "its guild at its fewest";
        }

        var head = BotGuilds.Head(theirs);
        var sour = head != null && BotTies.Regard(m, head) <= SourRegard;
        var bare = BotGuilds.Worth(theirs) == 0 && BotGuilds.Worth(ours) >= 2;

        if (!sour && !bare)
        {
            return "content in its guild";
        }

        from = theirs;

        return null;
    }

    /// <summary>One piece of news aloud, posted or standing. Returns whether anything was said.</summary>
    private static bool Read(BotMobile herald, Guild guild)
    {
        var text = BotOfficers.NextNews(guild.Name);

        if (text != null)
        {
            NewsRead++;
        }
        else
        {
            text = Fact(guild);

            if (text == null)
            {
                return false;
            }

            FactsRead++;
        }

        Tally(guild.Name, 0, 0, 1);

        return BotVoice.Say(herald, "local", $"Hear ye! {text}", force: true);
    }

    /// <summary>A standing fact about the guild, in turn: its wars, its chest, its store, its members.</summary>
    private static string Fact(Guild guild)
    {
        var turn = _facts.GetValueOrDefault(guild.Name);

        _facts[guild.Name] = turn + 1;

        for (var tries = 0; tries < 4; tries++)
        {
            string fact = ((turn + tries) % 4) switch
            {
                0 => War(guild),
                1 => BotChest.Holds(guild.Name) is > 0 and var chest
                    ? BotDues.Goal(guild, out var saving) > 0 && saving != null
                        ? $"{guild.Name}'s chest holds {chest}gp, saving for {saving}."
                        : $"{guild.Name}'s chest holds {chest}gp."
                    : null,
                2 => BotGuildStore.Worth(guild.Name) > 0 ? $"{guild.Name}'s store at the hall holds {BotGuildStore.Stock(guild.Name, 3)}." : null,
                _ => BotGuilds.Head(guild) is { } head
                    ? $"{guild.Name} is {guild.Members.Count} strong, led by {head.Name}."
                    : $"{guild.Name} is {guild.Members.Count} strong."
            };

            if (fact != null)
            {
                return fact;
            }
        }

        return null;
    }

    private static string War(Guild guild)
    {
        foreach (var war in BotWar.Standing)
        {
            var other = war.Against(guild.Name);

            if (other == null)
            {
                continue;
            }

            var (ours, _) = war.Score(guild.Name);
            var (theirs, _) = war.Score(other);

            return $"{guild.Name} is at war with {other}: {ours} of theirs fallen to {theirs} of ours, {war.Minutes} minutes in.";
        }

        return null;
    }

    private static void Tally(string guild, int invited, int joined, int read)
    {
        var (i, j, r) = _byGuild.GetValueOrDefault(guild);

        _byGuild[guild] = (i + invited, j + joined, r + read);
    }

    public static string Tell(string guild)
    {
        var (i, j, r) = _byGuild.GetValueOrDefault(guild);

        return $"herald: {i} invited, {j} joined, {r} pieces of news read; {BotGuildBout.Tell(guild)}; {BotOfficers.Pending(guild)} pieces of news waiting";
    }

    public static void Write(Utf8JsonWriter w, string guild)
    {
        var (i, j, r) = _byGuild.GetValueOrDefault(guild);

        w.WriteNumber("invited", i);
        w.WriteNumber("joinedByHerald", j);
        w.WriteNumber("newsRead", r);
        BotGuildBout.Write(w, guild);
    }

    public static string Describe()
    {
        var say = "";

        foreach (var (why, n) in _gates)
        {
            say += (say.Length == 0 ? "" : ", ") + $"{why} {n}";
        }

        return $"{Stands} stands of {StandMs / 60000} minutes, {Looks} looks round, {Invited} invited ({InvitedSour} discontented elsewhere, poaching {(Poaching ? "on" : "OFF")}), {Joined} joined from no guild, {Crossed} crossed over, {Declined} declined, {Refused} refused by the guild's door; "
               + $"{NewsRead} pieces of news and {FactsRead} standing facts read aloud; not invited: {(say.Length == 0 ? "none" : say)}";
    }

    public static void Forget()
    {
        Stands = 0;
        StandMs = 0;
        Looks = 0;
        Invited = 0;
        InvitedSour = 0;
        Joined = 0;
        Crossed = 0;
        Declined = 0;
        Refused = 0;
        NewsRead = 0;
        FactsRead = 0;
        _gates.Clear();
        _byGuild.Clear();
        _rested.Clear();
        _invited.Clear();
        _facts.Clear();
    }
}
