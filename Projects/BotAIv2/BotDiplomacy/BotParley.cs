using System;
using System.Collections.Generic;
using Server.Guilds;
using Server.Logging;

namespace Server.BotAI.V2;

/// <summary>
/// Meetings: one guild's envoy walks to another guild's town, or to its own town's hall, and a word is given there before a
/// witness — war, peace, trade, standing orders, an alliance, or a task from the town.
///
/// <para>
/// <b>Patrick's order of 29.09.2026, evening.</b> "Guilds may declare war on each other only if they meet in the main building
/// of a town. One of the debuggers is invited as the witness: he becomes visible and hears both sides. The bots can come to an
/// agreement or start a war. The meeting must be on the guild's own ground: if The Blade wants war with The Needle and The Needle
/// is in Cove, The Blade's envoy must go to Cove and talk to them there. This removes lightning decisions and gives the guilds
/// time for their work." And the same way: alliances, orders, trade and safety for each other; and a guild's dealings with its
/// town, whose voice gives it a task.
/// </para>
///
/// <para>
/// <b>What it replaces.</b> <c>BotRegard</c> declared a war the moment an opinion crossed <c>Enmity</c> and the war's rules let
/// it, and made an alliance the moment both opinions crossed <c>Alliance</c>. Both now call a meeting instead
/// (<see cref="Grieve"/>, <see cref="Befriend"/>), and the war or the alliance comes of the meeting or does not. Two more topics
/// are called by this file's own look: a guild another thinks ill of goes to ask for peace before the grievance reaches the war
/// line (<see cref="WaryShare"/>), and two guilds on good terms go to agree trade (<see cref="Cordial"/>). Wars already standing
/// are left alone.
/// </para>
///
/// <para>
/// <b>A meeting is walked, waited for and heard; nothing in it is instant.</b> An envoy (any member; a guild of one sends its
/// founder) takes the errand (<see cref="BotEnvoy"/>) and walks to the place (<see cref="Seat"/>); the host guild is asked to
/// send somebody once the envoy is near; a witness is summoned from the debugger's squad through <see cref="Summon"/>; the
/// lines are said a few seconds apart; the duke's word is asked of the model through <see cref="Judge"/> and, if it does not
/// come in <see cref="JudgeWaitMs"/>, given by rule (<see cref="BotDuke.Rule"/>). Every stage has its own clock, so a meeting
/// that cannot happen lapses and says why, and the errands that served it end honestly — finished when the word was given,
/// failed when it lapsed.
/// </para>
/// </summary>
public static class BotParley
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotParley));

    /// <summary>Whether wars and alliances wait for a meeting. Off, and <c>BotRegard</c> declares and allies at once, as before.</summary>
    public static bool Running { get; set; } = true;

    /// <summary>
    /// Whether the module has started. Until it has — the first seconds of a boot, or a shard with the module switched off
    /// (<c>bots.diplomacy.enabled</c>) — a grievance is declared as it always was, rather than held for a meeting nobody runs.
    /// </summary>
    public static bool Active { get; internal set; }

    /// <summary>
    /// How many meetings between guilds may stand at once, and how many audiences with towns. Two and two: the debugger's squad
    /// is three watchers and a marshal, one witness each, and a meeting spends most of its life on the road, not before one.
    /// </summary>
    public static int MostMeetings { get; set; } = 2;

    public static int MostAudiences { get; set; } = 2;

    /// <summary>How often the meetings are advanced, in milliseconds. Two seconds: a line every few seconds needs no finer clock.</summary>
    public static int BeatMs { get; set; } = 2000;

    /// <summary>How often the guilds are looked over for a meeting worth calling, and the towns' tasks checked. A minute.</summary>
    public static int LookMs { get; set; } = 60000;

    /// <summary>How long a called meeting waits for anybody of the guest guild to set out. Twenty minutes.</summary>
    public static int OfferMs { get; set; } = 1200000;

    /// <summary>How long an envoy may walk before it gives up the road. Thirty minutes: Cove is some eight hundred tiles from Britain.</summary>
    public static int MarchMs { get; set; } = 1800000;

    /// <summary>How long an envoy's or host's errand may go without saying it is still on it before the meeting looks for another.</summary>
    public static int ClaimMs { get; set; } = 180000;

    /// <summary>How near the place the envoy must be before the host is asked to send somebody. See <see cref="BotHearing.HostWanted"/>.</summary>
    public static int HostCallTiles { get; set; } = 150;

    /// <summary>How long after the envoy arrives the host is waited for before the meeting is heard without it. Four minutes.</summary>
    public static int HostWaitMs { get; set; } = 240000;

    /// <summary>How long after the envoy arrives a witness is waited for before the meeting goes on unwitnessed. Three minutes.</summary>
    public static int WitnessWaitMs { get; set; } = 180000;

    /// <summary>Between two lines of a hearing, in milliseconds. Five seconds: long enough to read, short enough to watch.</summary>
    public static int LineMs { get; set; } = 5000;

    /// <summary>
    /// How long after the last line the duke's word is waited for before the rule gives it. Two minutes: a cold load of the
    /// model is half a minute, and the one slot may be held by a watcher's own question.
    /// </summary>
    public static int JudgeWaitMs { get; set; } = 120000;

    /// <summary>The longest a meeting may stand, from the call, before it lapses whatever stage it is at. Ninety minutes.</summary>
    public static int LongestMs { get; set; } = 5400000;

    /// <summary>
    /// How long two guilds wait after a meeting that ended in an agreement or a war before they may be called to another. Two
    /// hours: the duke's word stands, and a grievance that crosses the line again meanwhile waits for it.
    /// </summary>
    public static int MeetEveryMs { get; set; } = 7200000;

    /// <summary>And after one that ended in nothing, or lapsed. Half an hour.</summary>
    public static int RetryMs { get; set; } = 1800000;

    /// <summary>How near the place an envoy or host must stand to be at the meeting.</summary>
    public static int ArriveTiles { get; set; } = 4;

    /// <summary>
    /// The share of the war line (<c>BotRegard.Enmity</c>) at which a guild that is thought ill of goes to ask for peace. A half:
    /// with the war line at −100, a guild asks at −50, before the other's grievance becomes a war.
    /// </summary>
    public static double WaryShare { get; set; } = 0.5;

    /// <summary>At or above this, both ways, two guilds on good terms go to agree trade. Twenty-five.</summary>
    public static double Cordial { get; set; } = 25.0;

    /// <summary>The chance, per look, that a pair eligible for a peace or a trade meeting calls one. A fifth: on average in five minutes.</summary>
    public static double CallOdds { get; set; } = 0.2;

    /// <summary>How far a guild's seat may lie from a town's square and still be that town's. Three hundred tiles.</summary>
    public static int SeatTownTiles { get; set; } = 300;

    /// <summary>
    /// Whether a guild whose seat stands inside its town's walls — a building of its own there — is met at its seat rather than
    /// at the town's hall. Off, by the order's words ("the main building of the town"), and because a guild's own doors may
    /// open to its members only: an envoy that cannot get through the door cannot be heard.
    /// </summary>
    public static bool MeetAtSeat { get; set; } = false;

    /// <summary>
    /// The town's main building, by town name, as [x, y], where the file says one; everywhere else the town's bank. Configured
    /// in <c>bot-diplomacy.json</c> as <c>"Halls": { "Britain": [1434, 1699] }</c>.
    /// </summary>
    public static Dictionary<string, int[]> Halls { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// The model the duke and the towns' voice are asked with. Empty means the population's own (<c>BotOllama.Model</c>,
    /// qwen3.5:9b), which answers this kind of question in a couple of seconds warm and without thinking; the watchers' own
    /// thinking model would hold the one card for minutes over a word that is half a sentence.
    /// </summary>
    public static string JudgeModel { get; set; }

    /// <summary>
    /// How long that model is kept on the card after a word. Ten seconds, the watchers' own figure: a meeting is rare, and a
    /// model held for half an hour would push the watchers' model off the card for every one of their questions meanwhile.
    /// </summary>
    public static string JudgeKeepAlive { get; set; } = "10s";

    /// <summary>How long one ask may take before the transport gives it up. Ninety seconds: a cold load is half a minute.</summary>
    public static int JudgeTimeoutMs { get; set; } = 90000;

    // ---- the hooks the thinking assembly fills; this one may not reference it ------------------------------------------

    /// <summary>
    /// Puts a witness at the meeting and returns its body, or null when none is free. Filled by the debugger (<c>BotWitness</c>),
    /// which teleports one of its watchers there and makes it visible; null when the debugger is not loaded.
    /// </summary>
    public static Func<BotHearing, Mobile> Summon { get; set; }

    /// <summary>The witness goes back to watching, hidden again. Filled by the debugger.</summary>
    public static Action<BotHearing> Dismiss { get; set; }

    /// <summary>
    /// Asks the model for the word: the duke for two guilds, the town's voice for a town. Returns whether it was asked; the
    /// answer comes back on the game thread, or null. Filled by the debugger, which owns the one slot's queue.
    /// </summary>
    public static Func<BotHearing, Action<BotVerdict>, bool> Judge { get; set; }

    // ---- counters ------------------------------------------------------------------------------------------------------

    public static long Called { get; private set; }

    private static readonly long[] _calledBy = new long[5];

    public static long Heard { get; private set; }

    public static long Witnessed { get; private set; }

    public static long Unwitnessed { get; private set; }

    public static long HostsAbsent { get; private set; }

    /// <summary>Meetings that lapsed, by why.</summary>
    public static long NoEnvoy { get; private set; }

    public static long Lost { get; private set; }

    public static long TooLong { get; private set; }

    public static long Vanished { get; private set; }

    /// <summary>Grievances that found a meeting of the pair already called, or the duke's word still standing, or no room.</summary>
    public static long Pending { get; private set; }

    public static long Standing { get; private set; }

    public static long Queued { get; private set; }

    /// <summary>Meetings that could not be called because the guest could not walk to the host's town at all.</summary>
    public static long Unreachable { get; private set; }

    /// <summary>Wars declared at a meeting, wars the war's rules refused at the verdict, alliances sworn.</summary>
    public static long Wars { get; internal set; }

    public static long Forbidden { get; internal set; }

    public static long Alliances { get; internal set; }

    /// <summary>Model answers that came back after the rule had already given the word, and that could not be read.</summary>
    public static long Late { get; private set; }

    public static long Unread { get; private set; }

    private static readonly List<BotHearing> _meetings = [];

    private static readonly List<string> _recent = [];

    private static readonly Dictionary<(string A, string B), long> _clocks = [];

    private static readonly Dictionary<string, Point3D> _halls = new(StringComparer.OrdinalIgnoreCase);

    private static int _nextId;

    private static long _lookedTick;

    private static bool _everLooked;

    /// <summary>The meetings standing, for the page, the proposer and Argus.</summary>
    public static IReadOnlyList<BotHearing> Meetings => _meetings;

    /// <summary>The last few meetings' endings, newest last.</summary>
    public static IReadOnlyList<string> Recent => _recent;

    private static (string A, string B) Pair(string one, string other) =>
        string.CompareOrdinal(one, other) <= 0 ? (one, other) : (other, one);

    // ---- where -----------------------------------------------------------------------------------------------------------

    /// <summary>
    /// A town's main building, where its meetings are held: the file's word for the town (<see cref="Halls"/>), else the bank
    /// nearest its middle (<c>BotSettle.Amenities.Hearth</c>, settled on the ground beside the banker), else its square.
    /// Asked once per town and kept.
    /// </summary>
    public static Point3D Hall(BotTowns.Town town)
    {
        if (town == null)
        {
            return Point3D.Zero;
        }

        if (_halls.TryGetValue(town.Name, out var known))
        {
            return known;
        }

        var at = Point3D.Zero;
        var map = BotPopulation.Home;

        if (map != null && map != Map.Internal && Halls != null && Halls.TryGetValue(town.Name, out var set) && set is { Length: >= 2 }
            && BotTowns.Place(map, set[0], set[1], 4, out var spot))
        {
            at = spot;
        }

        if (at == Point3D.Zero)
        {
            at = BotSettle.Of(town)?.Hearth ?? town.Square;
        }

        if (at == Point3D.Zero)
        {
            at = town.Square;
        }

        // Kept only once the gates can say whether it is walked to; before that it is asked again next time.
        if (!BotGates.Ready)
        {
            return at;
        }

        at = Doorstep(map, town, at);

        _halls[town.Name] = at;

        return at;
    }

    /// <summary>How far from a hall standing in a pocket of its own its doorstep is looked for. See <see cref="Doorstep"/>.</summary>
    public static int DoorstepTiles { get; set; } = 16;

    /// <summary>Halls moved to their doorstep; halls with no doorstep found joined to their town.</summary>
    public static long Doorsteps { get; private set; }

    public static long Doorless { get; private set; }

    /// <summary>
    /// <b>A hall no road reaches is met at its door (30.09.2026, build 340).</b> Britain's hall — the bank nearest its middle,
    /// settled beside the banker — stood at (1427, 1686) in land 7648 of the gates' graph, a floor the walkers' graph keeps
    /// apart from the street; <c>BotHerald</c> asks <see cref="BotGates.Joined"/> of every envoy, and 550 of 655 members asked
    /// on 339 were refused "no walk there", so The Blade's audience with its own town stood called and never heard. The
    /// nearest tile within <see cref="DoorstepTiles"/> that the gates join to the town's square is the meeting place instead.
    /// </summary>
    private static Point3D Doorstep(Map map, BotTowns.Town town, Point3D at)
    {
        if (map == null || at == Point3D.Zero || town.Square == Point3D.Zero || BotGates.Joined(map, at, town.Square))
        {
            return at;
        }

        for (var r = 1; r <= DoorstepTiles; r++)
        {
            for (var dx = -r; dx <= r; dx++)
            {
                for (var dy = -r; dy <= r; dy++)
                {
                    if (Math.Max(Math.Abs(dx), Math.Abs(dy)) != r)
                    {
                        continue;
                    }

                    if (!BotStep.Settle(map, at.X + dx, at.Y + dy, out var z) || !map.CanSpawnMobile(at.X + dx, at.Y + dy, z))
                    {
                        continue;
                    }

                    var spot = new Point3D(at.X + dx, at.Y + dy, z);

                    if (!BotGates.Joined(map, spot, town.Square))
                    {
                        continue;
                    }

                    Doorsteps++;

                    logger.Information(
                        "{Town}'s hall at {From} stands where no road from the town reaches; its meetings are held at {To}, {Tiles} tiles off",
                        town.Name,
                        at,
                        spot,
                        r
                    );

                    return spot;
                }
            }
        }

        Doorless++;

        logger.Information(
            "{Town}'s hall at {From} stands where no road from the town reaches, and nothing within {Tiles} tiles does",
            town.Name,
            at,
            DoorstepTiles
        );

        return at;
    }

    /// <summary>
    /// Where a guild is to be met, and in which town: <b>the one question another patch may answer from a building later</b>.
    ///
    /// <para>
    /// Today it asks what the shard already knows of where a guild lives. Its seat (<c>BotSeat.Of</c>: set by hand, in the
    /// file, chosen at founding, or where its hall stands) names the town nearest it, when that is within
    /// <see cref="SeatTownTiles"/>; a guild with no seat lives where most of its members do (<c>BotResidence</c>), else in the
    /// home town. The place is that town's main building (<see cref="Hall"/>); a seat with no town near it is met at the seat.
    /// When guilds come to own a house in a town, this is the function to teach it: return the house's door and its town.
    /// </para>
    /// </summary>
    public static bool Seat(Guild guild, out BotTowns.Town town, out Point3D at)
    {
        town = null;
        at = Point3D.Zero;

        var map = BotPopulation.Home;

        if (guild == null || map == null || map == Map.Internal || !BotTowns.Surveyed)
        {
            return false;
        }

        var seat = BotSeat.Of(guild.Name);

        if (seat != Point3D.Zero)
        {
            var near = BotTowns.Nearest(seat);

            if (near != null && Utility.InRange(near.Square, seat, SeatTownTiles))
            {
                town = near;
                at = MeetAtSeat && near.Bounds.Contains(seat) && BotStep.Settle(map, seat.X, seat.Y, out var inside)
                    ? new Point3D(seat.X, seat.Y, inside)
                    : Hall(near);

                return at != Point3D.Zero;
            }

            if (BotStep.Settle(map, seat.X, seat.Y, out var z))
            {
                at = new Point3D(seat.X, seat.Y, z);

                return true;
            }
        }

        town = LivesIn(guild) ?? BotSettle.HomeTown ?? BotTowns.Nearest(BotPopulation.Where);
        at = Hall(town);

        return at != Point3D.Zero;
    }

    /// <summary>The town most of a guild's members live in, or null. Asked when a meeting is called, which is rare.</summary>
    public static BotTowns.Town LivesIn(Guild guild)
    {
        if (guild?.Members == null)
        {
            return null;
        }

        BotTowns.Town best = null;
        var most = 0;
        Dictionary<BotTowns.Town, int> counts = [];

        for (var i = 0; i < guild.Members.Count; i++)
        {
            if (guild.Members[i] is not BotMobile { Deleted: false } member || BotResidence.Of(member) is not { } town)
            {
                continue;
            }

            var n = counts.GetValueOrDefault(town) + 1;

            counts[town] = n;

            if (n > most)
            {
                most = n;
                best = town;
            }
        }

        return best;
    }

    // ---- what calls a meeting ------------------------------------------------------------------------------------------

    /// <summary>
    /// A grievance has reached the war line and the war's rules would let it be declared: a meeting is called instead, or one
    /// already stands, or the duke's last word between them still does. Returns false only when meetings are off, which leaves
    /// <c>BotRegard</c> to declare as it did before. Called by <c>BotRegard.Reckon</c> and <c>BotRegard.Drift</c>.
    /// </summary>
    public static bool Grieve(Guild mine, Guild theirs, string why) => Ask(BotTopic.Grievance, mine, theirs, why);

    /// <summary>
    /// Two guilds think well enough of each other to be allies: a meeting is called to swear it. Returns false only when
    /// meetings are off, which leaves <c>BotRegard</c> to make the alliance at once, as before.
    /// </summary>
    public static bool Befriend(Guild mine, Guild theirs, string why) => Ask(BotTopic.Friendship, mine, theirs, why);

    private static bool Ask(BotTopic topic, Guild mine, Guild theirs, string why)
    {
        if (!Running || !Active)
        {
            return false;
        }

        // Nothing is ever agreed with The Shadow: it is every guild's enemy already, with no ledger page (BotWar.MayDeclare).
        if (mine == null || theirs == null || mine == theirs || mine.Name == BotUnderworld.GuildName || theirs.Name == BotUnderworld.GuildName)
        {
            return true;
        }

        if (Between(mine.Name, theirs.Name) != null)
        {
            Pending++;

            return true;
        }

        // The first seconds of a boot, before the gates say which towns can be walked to: the grievance waits for the next
        // drift rather than finding every town unreachable and being shut out for half an hour.
        if (!BotTowns.Ready)
        {
            Queued++;

            return true;
        }

        if (_clocks.TryGetValue(Pair(mine.Name, theirs.Name), out var until) && Core.TickCount - until < 0)
        {
            Standing++;

            return true;
        }

        if (!Room || Busy(mine.Name) || Busy(theirs.Name))
        {
            Queued++;

            return true;
        }

        Call(topic, mine, theirs, why);

        return true;
    }

    /// <summary>The meeting standing between these two guilds, either way round, or null.</summary>
    public static BotHearing Between(string one, string other)
    {
        for (var i = 0; i < _meetings.Count; i++)
        {
            var m = _meetings[i];

            if (m.Between && (m.Guest == one && m.Host == other || m.Guest == other && m.Host == one))
            {
                return m;
            }
        }

        return null;
    }

    /// <summary>
    /// The meeting standing between these two, if any, ended unheard because war has overtaken it: one of them laid claim to
    /// the other's ground, which is war without negotiations (Patrick, 06.10.2026; <c>BotSeizure</c>). Ended before the war is
    /// declared, so no word given at it can swear a peace or terms between two guilds at war. True when one was ended.
    /// </summary>
    public static bool Overtake(string one, string other, string why)
    {
        var m = Between(one, other);

        if (m == null || m.Stage == BotHearingStage.Over)
        {
            return false;
        }

        End(m, $"overtaken by war: {why}", false);

        return true;
    }

    /// <summary>Whether this guild stands in a meeting with another guild now, either side.</summary>
    public static bool Busy(string guild)
    {
        for (var i = 0; i < _meetings.Count; i++)
        {
            var m = _meetings[i];

            if (m.Between && (m.Guest == guild || m.Host == guild))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Whether this guild has an envoy out to this town now.</summary>
    public static bool Treating(string guild, BotTowns.Town town)
    {
        for (var i = 0; i < _meetings.Count; i++)
        {
            var m = _meetings[i];

            if (!m.Between && m.Guest == guild && ReferenceEquals(m.Town, town))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Whether a guild has an envoy out to any town now. One audience at a time per guild.</summary>
    public static bool Treating(string guild)
    {
        for (var i = 0; i < _meetings.Count; i++)
        {
            if (!_meetings[i].Between && _meetings[i].Guest == guild)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Whether another meeting between guilds may be called now. See <see cref="MostMeetings"/>.</summary>
    public static bool Room => Running && Count(true) < MostMeetings;

    /// <summary>Whether another audience with a town may be called now. See <see cref="MostAudiences"/>.</summary>
    public static bool AudienceRoom => BotBurgh.Running && Count(false) < MostAudiences;

    private static int Count(bool between)
    {
        var n = 0;

        for (var i = 0; i < _meetings.Count; i++)
        {
            if (_meetings[i].Between == between)
            {
                n++;
            }
        }

        return n;
    }

    private static BotHearing Call(BotTopic topic, Guild guest, Guild host, string why)
    {
        if (!Seat(host, out var town, out var at))
        {
            Unreachable++;
            _clocks[Pair(guest.Name, host.Name)] = Core.TickCount + RetryMs;

            return null;
        }

        var map = BotPopulation.Home;

        // The guest must be able to walk there at all. Asked of the gates from where the guest lives, once; each envoy is asked
        // again from where it stands (BotHerald).
        Seat(guest, out _, out var from);

        if (from != Point3D.Zero && !BotGates.Joined(map, from, at))
        {
            Unreachable++;
            _clocks[Pair(guest.Name, host.Name)] = Core.TickCount + RetryMs;

            logger.Information(
                "{Guest} would meet {Host} over {Topic}, but no walk joins where it lives to {Town}; nothing is called for {Minutes} minutes",
                guest.Name,
                host.Name,
                BotDuke.Word(topic),
                town?.Name ?? "their seat",
                RetryMs / 60000
            );

            return null;
        }

        var m = Open(topic, guest.Name, host.Name, town, map, at, why);

        logger.Warning(
            "{Guest} calls a meeting with {Host} over {Topic} ({Why}): its envoy is to walk to {Town} at {X},{Y}, and nothing is decided until it has been heard",
            guest.Name,
            host.Name,
            BotDuke.Word(topic),
            why,
            town?.Name ?? "their seat",
            at.X,
            at.Y
        );

        return m;
    }

    /// <summary>A guild's envoy to a town. Called by <c>BotBurgh.Look</c>, which decides when.</summary>
    public static BotHearing Audience(Guild guild, BotTowns.Town town, string why)
    {
        if (guild == null || town == null || !AudienceRoom)
        {
            return null;
        }

        var at = Hall(town);

        if (at == Point3D.Zero)
        {
            return null;
        }

        var m = Open(BotTopic.Town, guild.Name, null, town, BotPopulation.Home, at, why);

        logger.Information(
            "{Guild} sends an envoy to {Town}'s hall at {X},{Y} to hear what the town wants of it ({Why})",
            guild.Name,
            town.Name,
            at.X,
            at.Y,
            why
        );

        return m;
    }

    private static BotHearing Open(BotTopic topic, string guest, string host, BotTowns.Town town, Map map, Point3D at, string why)
    {
        var now = Core.TickCount;
        var m = new BotHearing
        {
            Id = ++_nextId,
            Topic = topic,
            Guest = guest,
            Host = host,
            Town = town,
            Map = map,
            Place = at,
            Why = why,
            Stage = BotHearingStage.Called,
            CalledTick = now,
            StageTick = now
        };

        _meetings.Add(m);
        Called++;
        _calledBy[(int)topic]++;

        BotEvents.Post("parley", guest, null, guest, at.X, at.Y, $"{guest} calls a meeting {(host != null ? $"with {host}" : $"with the town of {town?.Name}")} over {BotDuke.Word(topic)}: {why}", null, null);

        return m;
    }

    // ---- who goes --------------------------------------------------------------------------------------------------------

    /// <summary>
    /// The meeting this bot's guild wants somebody at, and in which part: its envoy, or its answerer. Asked by the proposer for
    /// every free bot, and answers from a list of a handful (<see cref="MostMeetings"/>, <see cref="MostAudiences"/>).
    /// </summary>
    public static BotHearing Wanting(BotMobile bot, out bool asHost)
    {
        asHost = false;

        if (_meetings.Count == 0 || bot?.Guild is not Guild guild)
        {
            return null;
        }

        var now = Core.TickCount;

        for (var i = 0; i < _meetings.Count; i++)
        {
            var m = _meetings[i];

            if (m.Stage >= BotHearingStage.Hearing || m.Turned.Contains(bot.Serial))
            {
                continue;
            }

            if (m.Guest == guild.Name && m.Stage <= BotHearingStage.Walking && !Held(m.Envoy, m.EnvoyTick, now))
            {
                return m;
            }

            if (m.Between && m.Host == guild.Name && m.HostWanted && !Held(m.HostRep, m.HostTick, now))
            {
                asHost = true;

                return m;
            }
        }

        return null;
    }

    private static bool Held(BotMobile who, long tick, long now) =>
        who is { Deleted: false, Alive: true } && now - tick < ClaimMs;

    /// <summary>A bot has taken the errand. Called from <c>BotEnvoy.Taken</c>, never at the offer (DECISIONS A7).</summary>
    public static void Took(BotHearing m, BotMobile bot, bool asHost)
    {
        if (m == null || bot == null)
        {
            return;
        }

        var now = Core.TickCount;

        if (asHost)
        {
            m.HostRep = bot;
            m.HostTick = now;

            return;
        }

        m.Envoy = bot;
        m.EnvoyTick = now;

        if (m.Stage == BotHearingStage.Called)
        {
            m.Stage = BotHearingStage.Walking;
            m.StageTick = now;
        }

        logger.Information(
            "{Name} the {Class} of {Guild} sets out as envoy to {Where} over {Topic}",
            bot.Name,
            bot.Class?.Name,
            m.Guest,
            m.Between ? $"{m.Host} at {m.Town?.Name ?? "their seat"}" : $"the town of {m.Town?.Name}",
            BotDuke.Word(m.Topic)
        );
    }

    /// <summary>The errand said it is still on it. Called on every beat of <c>BotEnvoy.Advance</c>.</summary>
    public static void Still(BotHearing m, BotMobile bot, bool asHost)
    {
        if (m == null || bot == null)
        {
            return;
        }

        if (asHost && ReferenceEquals(m.HostRep, bot))
        {
            m.HostTick = Core.TickCount;
        }
        else if (!asHost && ReferenceEquals(m.Envoy, bot))
        {
            m.EnvoyTick = Core.TickCount;
        }
    }

    /// <summary>The errand is over. Its bot is let go of the meeting, and one that could not get there is not asked again.</summary>
    public static void Let(BotHearing m, BotMobile bot, bool asHost, bool couldNotReach)
    {
        if (m == null || bot == null)
        {
            return;
        }

        if (couldNotReach)
        {
            m.Turned.Add(bot.Serial);
        }

        if (asHost)
        {
            if (ReferenceEquals(m.HostRep, bot))
            {
                m.HostRep = null;
            }

            return;
        }

        if (!ReferenceEquals(m.Envoy, bot))
        {
            return;
        }

        m.Envoy = null;

        if (m.Stage is BotHearingStage.Walking or BotHearingStage.Gathered)
        {
            m.Stage = BotHearingStage.Called;
            m.StageTick = Core.TickCount;
        }
    }

    /// <summary>Whether this bot stands at the meeting's place.</summary>
    public static bool There(BotHearing m, Mobile who) =>
        who is { Deleted: false, Alive: true } && who.Map == m.Map && who.InRange(m.Place, ArriveTiles);

    // ---- the clock ---------------------------------------------------------------------------------------------------------

    /// <summary>Every meeting a step on. On the module's own timer, every <see cref="BeatMs"/>.</summary>
    public static void Beat()
    {
        var now = Core.TickCount;

        if (!_everLooked || now - (_lookedTick + LookMs) >= 0)
        {
            var since = _everLooked ? now - _lookedTick : LookMs;

            _everLooked = true;
            _lookedTick = now;

            // Two switches: meetings between guilds (Running) and the towns' dealings (BotBurgh.Running). A meeting already
            // called is seen through whichever is turned off meanwhile.
            if (Running)
            {
                Look();
            }

            BotBurgh.Look(since);
        }

        for (var i = _meetings.Count - 1; i >= 0; i--)
        {
            var m = _meetings[i];

            try
            {
                Step(m, now);
            }
            catch (Exception e)
            {
                logger.Error(e, "Meeting #{Id} between {Guest} and {Host} threw at {Stage}; it is ended", m.Id, m.Guest, m.Host ?? m.Town?.Name, m.Stage);
                Lost++;
                End(m, $"it threw: {e.Message}", false);
            }

            if (m.Stage == BotHearingStage.Over)
            {
                _meetings.RemoveAt(i);
            }
        }
    }

    private static void Step(BotHearing m, long now)
    {
        if (m.Stage == BotHearingStage.Over)
        {
            return;
        }

        if (BotGuilds.Named(m.Guest) == null || m.Between && BotGuilds.Named(m.Host) == null)
        {
            Vanished++;
            End(m, "a guild is gone", false);

            return;
        }

        if (now - m.CalledTick >= LongestMs && m.Stage < BotHearingStage.Hearing)
        {
            TooLong++;
            End(m, $"it stood {LongestMs / 60000} minutes without being heard", false);

            return;
        }

        if (m.Envoy != null && !Held(m.Envoy, m.EnvoyTick, now) && m.Stage < BotHearingStage.Hearing)
        {
            // The envoy died, went to rest, or its errand was taken off it without a word: somebody else may go.
            m.Envoy = null;
            m.Stage = BotHearingStage.Called;
            m.StageTick = now;
        }

        switch (m.Stage)
        {
            case BotHearingStage.Called:
                {
                    if (now - m.StageTick >= OfferMs)
                    {
                        NoEnvoy++;
                        End(m, $"nobody of {m.Guest} would go in {OfferMs / 60000} minutes", false);
                    }

                    return;
                }
            case BotHearingStage.Walking:
                {
                    if (m.Between && !m.HostWanted && m.Envoy is { } walker && walker.Map == m.Map
                        && walker.InRange(m.Place, HostCallTiles))
                    {
                        m.HostWanted = true;
                    }

                    if (There(m, m.Envoy))
                    {
                        m.Stage = BotHearingStage.Gathered;
                        m.StageTick = now;
                        m.ArrivedTick = now;
                        m.HostWanted = m.Between;
                    }

                    return;
                }
            case BotHearingStage.Gathered:
                {
                    Gather(m, now);

                    return;
                }
            case BotHearingStage.Hearing:
                {
                    if (now - m.SaidTick < LineMs)
                    {
                        return;
                    }

                    if (m.Said < m.Lines.Count)
                    {
                        Speak(m, m.Lines[m.Said].Who, m.Lines[m.Said].Line);
                        m.Said++;
                        m.SaidTick = now;

                        return;
                    }

                    m.Stage = BotHearingStage.Awaiting;
                    m.StageTick = now;

                    return;
                }
            case BotHearingStage.Awaiting:
                {
                    if (m.Verdict == null && (m.ModelFailed || !m.Asked || now - m.StageTick >= JudgeWaitMs))
                    {
                        var because = !m.Asked ? "the model was not asked" : m.ModelFailed ? "the model gave no answer that could be read" : $"no answer in {JudgeWaitMs / 1000}s";

                        m.Verdict = m.Between ? BotDuke.Rule(m, because) : BotTownTask.Rule(m, because);
                        m.JudgedBy = $"the rule ({because})";
                        BotDuke.Given(false);
                    }

                    if (m.Verdict != null)
                    {
                        Pronounce(m, now);
                    }

                    return;
                }
            case BotHearingStage.Verdict:
                {
                    if (now - m.SaidTick < LineMs)
                    {
                        return;
                    }

                    if (m.Said < m.Lines.Count)
                    {
                        Speak(m, m.Lines[m.Said].Who, m.Lines[m.Said].Line);
                        m.Said++;
                        m.SaidTick = now;

                        return;
                    }

                    End(m, m.Ending, true);

                    return;
                }
        }
    }

    /// <summary>The envoy is there: the witness is sent for, the host is waited for, and when both are settled the hearing opens.</summary>
    private static void Gather(BotHearing m, long now)
    {
        if (!There(m, m.Envoy))
        {
            // Walked off — a flight, a fight — and is on its way back, or is not: the walk stage decides which.
            m.Stage = BotHearingStage.Walking;
            m.StageTick = now;

            return;
        }

        if (m.Witness == null && !m.Unwitnessed)
        {
            var body = Summon?.Invoke(m);

            if (body != null)
            {
                m.Witness = body;
                m.WitnessName = body.Name;
                Witnessed++;
            }
            else if (Summon == null || now - m.ArrivedTick >= WitnessWaitMs)
            {
                m.Unwitnessed = true;
                Unwitnessed++;
            }
        }

        var hostReady = !m.Between || There(m, m.HostRep);

        if (!hostReady && now - m.ArrivedTick >= HostWaitMs)
        {
            m.HostAbsent = true;
            HostsAbsent++;
            hostReady = true;
        }

        if (!hostReady || m.Witness == null && !m.Unwitnessed)
        {
            return;
        }

        Open(m, now);
    }

    /// <summary>The hearing opens: the lines are written, the model is asked, and the first line is due.</summary>
    private static void Open(BotHearing m, long now)
    {
        m.Stage = BotHearingStage.Hearing;
        m.StageTick = now;
        m.HearingTick = now;
        m.Said = 0;
        m.SaidTick = now - LineMs;

        if (m.Between)
        {
            m.Allowed = BotDuke.Allowed(m.Topic, BotGuilds.Named(m.Guest), BotGuilds.Named(m.Host));
            BotDuke.Hearing(m);
            m.Brief = BotDuke.Brief(m);
        }
        else
        {
            BotTownTask.Hearing(m);
        }

        m.Asked = Judge != null && m.Allowed is { Length: > 0 } && Judge(m, v => Answered(m, v));

        logger.Information(
            "Meeting #{Id}: {Guest} and {Other} are heard at {Town} before {Witness}; the word may be {Allowed}, and is asked of {Who}",
            m.Id,
            m.Guest,
            m.Host ?? $"the town of {m.Town?.Name}",
            m.Town?.Name ?? "their seat",
            m.WitnessName ?? "no witness",
            string.Join(", ", m.Allowed ?? []),
            m.Asked ? "the model" : "the rule"
        );
    }

    /// <summary>The model's answer, on the game thread. Used if it is in time and names something allowed.</summary>
    private static void Answered(BotHearing m, BotVerdict v)
    {
        if (m.Stage > BotHearingStage.Awaiting || m.Verdict != null)
        {
            Late++;

            return;
        }

        if (v == null || v.Outcome == null || m.Allowed == null || Array.IndexOf(m.Allowed, v.Outcome) < 0 || string.IsNullOrWhiteSpace(v.Speech))
        {
            Unread++;
            m.ModelFailed = true;

            return;
        }

        m.Verdict = v;
        m.JudgedBy = "the model";
        BotDuke.Given(true);
    }

    /// <summary>The word is given: carried out at once, then said, then the closing lines.</summary>
    private static void Pronounce(BotHearing m, long now)
    {
        var v = m.Verdict;

        if (m.Between)
        {
            BotDuke.Apply(m);
        }
        else
        {
            BotBurgh.Give(m);
        }

        m.Stage = BotHearingStage.Verdict;
        m.StageTick = now;
        m.Lines.Clear();
        m.Said = 0;
        m.SaidTick = now - LineMs;

        var fill = BotDuke.Fill(m);
        var speaker = m.Witness != null ? BotSpeaker.Witness : BotSpeaker.Envoy;

        if (!m.Between && !string.IsNullOrWhiteSpace(v.Report))
        {
            Words(m, speaker, v.Report, fill);
        }

        Words(m, speaker, v.Speech, fill);

        if (m.Between)
        {
            m.Lines.Add((BotSpeaker.Envoy, BotDuke.Phrase($"parley:end:{m.Applied ?? BotDuke.Nothing}", m.Envoy, fill)));
        }
        else
        {
            m.Lines.Add((BotSpeaker.Envoy, BotDuke.Phrase("parley:town-accept", m.Envoy, fill)));
        }

        logger.Warning(
            "Meeting #{Id}: {Guest} and {Other} at {Town} before {Witness} — the word, by {By}, is {Outcome}: \"{Speech}\" ({Reason}); {Ending}",
            m.Id,
            m.Guest,
            m.Host ?? $"the town of {m.Town?.Name}",
            m.Town?.Name ?? "their seat",
            m.WitnessName ?? "no witness",
            m.JudgedBy,
            v.Outcome,
            v.Speech,
            v.Reason,
            m.Ending
        );
    }

    /// <summary>A long speech cut at sentences into lines the voice will carry. See <c>BotVoice.MostLetters</c>.</summary>
    private static void Words(BotHearing m, BotSpeaker who, string text, Dictionary<string, string> fill)
    {
        // With no witness the envoy carries the word home in its own mouth: "The duke's word: …", or the town's.
        var speech = who == BotSpeaker.Envoy && m.Witness == null
            ? BotDuke.Phrase(m.Between ? "parley:relay" : "parley:relay-town", m.Envoy, new Dictionary<string, string>(fill) { ["speech"] = text })
            : text;

        var most = Math.Max(40, BotVoice.MostLetters - 10);

        while (speech.Length > most)
        {
            var cut = speech.LastIndexOf(". ", most, StringComparison.Ordinal);

            if (cut < most / 3)
            {
                cut = speech.LastIndexOf(' ', most);
            }

            if (cut <= 0)
            {
                break;
            }

            m.Lines.Add((who, speech[..(cut + 1)].Trim()));
            speech = speech[(cut + 1)..].Trim();

            if (m.Lines.Count > 8)
            {
                return;
            }
        }

        if (speech.Length > 0)
        {
            m.Lines.Add((who, speech));
        }
    }

    /// <summary>One line said aloud by whoever it belongs to, if they are there to say it; the event stream has it either way.</summary>
    private static void Speak(BotHearing m, BotSpeaker who, string line)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            return;
        }

        Mobile body = who switch
        {
            BotSpeaker.Witness => m.Witness,
            BotSpeaker.Host => m.HostRep,
            _ => m.Envoy
        };

        if (body is { Deleted: false } && body.Map == m.Map && body.InRange(m.Place, ArriveTiles * 3))
        {
            BotVoice.Say(body, "local", line, true);

            return;
        }

        BotEvents.Post("parley", body?.Name ?? who.ToString(), null, m.Guest, m.Place.X, m.Place.Y, line, "unsaid", null);
    }

    /// <summary>The meeting is over: the witness is sent back, the pair's clock is set, and the ending is written down.</summary>
    private static void End(BotHearing m, string ending, bool heard)
    {
        var now = Core.TickCount;

        m.Stage = BotHearingStage.Over;
        m.OverTick = now;
        m.Heard = heard;
        m.Ending = ending ?? (heard ? "heard" : "lapsed");

        if (m.Witness != null)
        {
            try
            {
                Dismiss?.Invoke(m);
            }
            catch (Exception e)
            {
                logger.Warning("The witness of meeting #{Id} could not be sent back: {Message}", m.Id, e.Message);
            }
        }

        if (heard)
        {
            Heard++;
        }

        if (m.Between)
        {
            var agreed = heard && m.Applied != null && m.Applied != BotDuke.Nothing;

            _clocks[Pair(m.Guest, m.Host)] = now + (agreed ? MeetEveryMs : RetryMs);
        }
        else
        {
            BotBurgh.Ended(m);
        }

        var said = $"#{m.Id} {m.Guest} {(m.Between ? $"and {m.Host}" : $"at {m.Town?.Name}")} over {BotDuke.Word(m.Topic)}: {m.Ending}"
            + (heard ? $" (by {m.JudgedBy}, before {m.WitnessName ?? "no witness"})" : "");

        _recent.Add(said);

        if (_recent.Count > 8)
        {
            _recent.RemoveAt(0);
        }

        if (!heard)
        {
            logger.Information("Meeting #{Id} lapsed: {Said}", m.Id, said);
        }

        BotEvents.Post("parley", m.Guest, null, m.Guest, m.Place.X, m.Place.Y, said, heard ? "heard" : "lapsed", null);
    }

    // ---- the look: peace asked for, trade proposed ------------------------------------------------------------------------

    /// <summary>
    /// Once a look, over every pair of guilds: one that another has come to think ill of goes to ask it for peace, and two on
    /// good terms go to agree trade. The pairs are the guilds squared — sixty-four at most — and each question is a lookup.
    /// </summary>
    private static void Look()
    {
        BotPact.Sweep();

        if (!Room)
        {
            return;
        }

        var wary = BotRegard.Enmity * Math.Clamp(WaryShare, 0.0, 1.0);
        var now = Core.TickCount;

        foreach (var mine in BotGuilds.Standing)
        {
            if (mine == null || mine.Disbanded || mine.Name == BotUnderworld.GuildName || Busy(mine.Name))
            {
                continue;
            }

            foreach (var theirs in BotGuilds.Standing)
            {
                if (theirs == null || theirs == mine || theirs.Disbanded || theirs.Name == BotUnderworld.GuildName || mine.IsWar(theirs)
                    || Busy(theirs.Name))
                {
                    continue;
                }

                if (_clocks.TryGetValue(Pair(mine.Name, theirs.Name), out var until) && now - until < 0)
                {
                    continue;
                }

                var theyOfUs = BotRegard.Of(theirs.Name, mine.Name);
                var weOfThem = BotRegard.Of(mine.Name, theirs.Name);

                // Peace, asked for by the guild that is thought ill of, while it is still above the line.
                if (theyOfUs <= wary && theyOfUs > BotRegard.Enmity && !BotPact.Holds(mine.Name, theirs.Name, BotPact.Kind.Peace)
                    && Utility.RandomDouble() < CallOdds)
                {
                    Call(BotTopic.Safety, mine, theirs, $"{theirs.Name} thinks {theyOfUs:F0} of it, and war is declared at {BotRegard.Enmity:F0}");

                    return;
                }

                // Trade, proposed by the smaller of two guilds on good terms, once.
                if (weOfThem >= Cordial && theyOfUs >= Cordial && !mine.IsAlly(theirs) && !BotPact.Holds(mine.Name, theirs.Name, BotPact.Kind.Trade)
                    && !BotPact.Holds(mine.Name, theirs.Name, BotPact.Kind.Orders) && (mine.Members?.Count ?? 0) <= (theirs.Members?.Count ?? 0)
                    && Utility.RandomDouble() < CallOdds)
                {
                    Call(BotTopic.Commerce, mine, theirs, $"each thinks well of the other ({weOfThem:F0} and {theyOfUs:F0})");

                    return;
                }
            }
        }
    }

    // ---- by hand ------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// <c>do meet &lt;guest&gt; ; &lt;host or town&gt; [; topic]</c> at Argus's door: a meeting called by hand, the pair's clock and the
    /// grievance line ignored, so a person can watch one from the first step. Topics: grievance, safety, friendship, commerce;
    /// a town's name calls an audience.
    /// </summary>
    public static string ByHand(string tail)
    {
        var parts = (tail ?? "").Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length < 2)
        {
            return "meet wants a guild and another guild or a town, and may name a topic: meet The Blade ; The Needle ; grievance — or meet The Needle ; Cove";
        }

        var guest = BotGuilds.Find(parts[0]);

        if (guest == null)
        {
            return $"no guild is called \"{parts[0]}\".";
        }

        var host = BotGuilds.Find(parts[1]);

        if (host == null)
        {
            var town = BotTowns.Find(parts[1]);

            if (town == null)
            {
                return $"\"{parts[1]}\" is neither a guild nor a town.";
            }

            if (!AudienceRoom)
            {
                return BotBurgh.Running ? $"{MostAudiences} audiences with towns stand already." : "the towns' dealings are switched off (BotBurgh.Running).";
            }

            if (Treating(guest.Name))
            {
                return $"{guest.Name} has an envoy out to a town already.";
            }

            var audience = Audience(guest, town, "called at the keyboard");

            return audience == null ? "the audience could not be called." : $"called: {audience.Tell()}";
        }

        if (!Room)
        {
            return Running ? $"{MostMeetings} meetings between guilds stand already." : "meetings are switched off (BotParley.Running).";
        }

        if (Between(guest.Name, host.Name) != null || Busy(guest.Name) || Busy(host.Name))
        {
            return "one of those guilds stands in a meeting already.";
        }

        var topic = parts.Length > 2 && Enum.TryParse<BotTopic>(parts[2], true, out var named) && named != BotTopic.Town ? named : BotTopic.Grievance;
        var m = Call(topic, guest, host, "called at the keyboard");

        return m == null ? "the meeting could not be called: no walk joins them, or the host has no town." : $"called: {m.Tell()}";
    }

    /// <summary><c>do parley</c>: the meetings standing, the last few endings, the agreements, and the towns' favour.</summary>
    public static string Tell()
    {
        using var say = Server.Text.ValueStringBuilder.Create(512);

        say.Append(_meetings.Count == 0 ? "no meetings stand" : $"{_meetings.Count} meetings stand: ");

        for (var i = 0; i < _meetings.Count; i++)
        {
            say.Append(i > 0 ? "; " : "");
            say.Append(_meetings[i].Tell());
        }

        say.Append(". Lately: ");
        say.Append(_recent.Count == 0 ? "nothing" : string.Join("; ", _recent));
        say.Append($". {BotPact.Tell()}. {BotBurgh.Tell()}");

        return say.ToString();
    }

    // ---- the summary ---------------------------------------------------------------------------------------------------------

    public static string Describe()
    {
        if (!Running)
        {
            return "meetings are off: wars and alliances are made the moment an opinion crosses its line";
        }

        using var say = Server.Text.ValueStringBuilder.Create(1024);

        say.Append($"{_meetings.Count} meetings standing");

        for (var i = 0; i < _meetings.Count; i++)
        {
            say.Append(i == 0 ? " (" : "; ");
            say.Append(_meetings[i].Tell());
            say.Append(i == _meetings.Count - 1 ? ")" : "");
        }

        say.Append(
            $"; {Called} called ({_calledBy[(int)BotTopic.Grievance]} over a grievance, {_calledBy[(int)BotTopic.Safety]} to ask for peace, {_calledBy[(int)BotTopic.Friendship]} to ally, {_calledBy[(int)BotTopic.Commerce]} to trade, {_calledBy[(int)BotTopic.Town]} to a town), "
        );
        say.Append(
            $"{Heard} heard ({Witnessed} before a witness, {Unwitnessed} with none free, {HostsAbsent} with the host absent), {NoEnvoy} lapsed for want of an envoy, {TooLong} for taking longer than {LongestMs / 60000} minutes, {Vanished} for a guild gone, {Lost} for a fault, {Unreachable} not called for no walk to the host; "
        );
        say.Append(
            $"grievances at the war line: {Pending} found a meeting already called, {Standing} the duke's word still standing ({MeetEveryMs / 60000} minutes after an agreement, {RetryMs / 60000} after nothing), {Queued} no room; "
        );
        say.Append($"{BotDuke.Describe()}, {Late} of the model's answers late and {Unread} unreadable; {Wars} wars declared at a meeting, {Forbidden} refused by the war's rules at the word, {Alliances} alliances; ");
        say.Append($"{BotEnvoy.Describe()}; {BotHerald.Describe()}; {Doorsteps} halls met at their doorstep for standing where no road reaches, {Doorless} with none found; {BotPact.Describe()}; {BotGrievances.Describe()}");

        // Ultimatums and the tribute paid instead of war (07.10.2026). See BotTribute.
        say.Append("; ");
        say.Append(BotTribute.Describe());

        return say.ToString();
    }

    /// <summary>The pair clocks, for the store: who, and how long until they may meet again.</summary>
    public static IEnumerable<(string A, string B, long LeftMs)> Clocks()
    {
        var now = Core.TickCount;

        foreach (var (pair, until) in _clocks)
        {
            var left = until - now;

            if (left > 0)
            {
                yield return (pair.A, pair.B, left);
            }
        }
    }

    public static void RestoreClock(string a, string b, long leftMs)
    {
        if (!string.IsNullOrEmpty(a) && !string.IsNullOrEmpty(b) && a != b && leftMs > 0)
        {
            _clocks[Pair(a, b)] = Core.TickCount + leftMs;
        }
    }

    /// <summary>A world reload: meetings in flight are dropped (their witnesses sent back) and the counters put back to nought.</summary>
    public static void Forget()
    {
        for (var i = 0; i < _meetings.Count; i++)
        {
            if (_meetings[i].Witness != null)
            {
                try
                {
                    Dismiss?.Invoke(_meetings[i]);
                }
                catch (Exception)
                {
                    // A witness that cannot be sent back is the debugger's to raise again.
                }
            }
        }

        _meetings.Clear();
        _recent.Clear();
        _halls.Clear();
        Array.Clear(_calledBy);
        Called = 0;
        Heard = 0;
        Witnessed = 0;
        Unwitnessed = 0;
        HostsAbsent = 0;
        NoEnvoy = 0;
        Lost = 0;
        TooLong = 0;
        Vanished = 0;
        Pending = 0;
        Standing = 0;
        Queued = 0;
        Unreachable = 0;
        Wars = 0;
        Forbidden = 0;
        Alliances = 0;
        Late = 0;
        Unread = 0;
        _everLooked = false;
        BotDuke.Forget();
    }

    /// <summary>Patrick's reset: the clocks between guilds forgotten as well.</summary>
    public static void Wipe()
    {
        Forget();
        _clocks.Clear();
    }
}
