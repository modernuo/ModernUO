using System;
using System.Collections.Generic;
using Server.BotAI.V2;
using Server.Items;
using Server.Logging;
using Server.Mobiles;

namespace Server.BotAI.Mind;

/// <summary>
/// The championship: every hour Argus's herald calls the strongest of the ranks to a ring, sets them against each other one
/// pair at a time for the right to challenge the champion, and the one left standing meets the champion for the title.
///
/// <para>
/// <b>Patrick's order of 15.09.2026.</b> Thirty entrants, one against one, the winner goes on; the champion's name
/// turns yellow and it is handed a weapon or a piece of armour fit for its class. Held by the calendar, and by hand
/// through the door (<c>do tourney</c>) whenever somebody wants to see one.
/// </para>
///
/// <para>
/// <b>Patrick, 02.10.2026: "I have never once seen a fight for the champion's title; it seems to me this mechanic is
/// completely broken."</b> It had no such fight to show. Every hour was a fresh knockout and the champion one more entrant,
/// seeded by power like anybody: of the 14 championships between the wipe of 01.10.2026 11:32 and build 389, not one ended
/// with the champion meeting a challenger. The champion entered only 6 of the 13 that followed a crown — the other seven it
/// was with a company or resting — and lost before any final in all six. Six of the fourteen finals were never fought at all:
/// one finalist "with a company" forfeited, and twice the bot crowned was the one with the company ("Miriel is with a company
/// and forfeits; Miriel goes on", crowned at 07:27), because a pair with both sides busy sent the busy one on. The record of who held the
/// title was a file under <c>Saves/</c>, which every world save moves into the backups, so each of the five boots since the
/// wipe began "champion nobody" with the hour's clock restarted. And nothing was said where a player could hear it: the
/// session log, and one local line over the winner's head at the ring.
/// </para>
///
/// <para>
/// <b>So now: the ranks fight for the shot, the champion defends, the title changes hands in the ring.</b> The champion is
/// kept out of the bracket. When one challenger is left, the title bout is called: champion against challenger, in the ring,
/// announced in the world channel before and after. With the title vacant the bracket's final is the title bout. A champion
/// who cannot come — resting, with a company, down a dungeon, dead — is waited for <see cref="TitleWaitMs"/>; still away, it
/// is stripped and the challenger crowned, and a challenger who cannot come leaves the champion the title. Every outcome has
/// its own counter in the summary. The record — the champion, when its reign began, its defences, when the last was held —
/// is kept by the world save (<see cref="BotTourneyStore"/>), so a deploy no longer forgets the champion.
/// </para>
///
/// <para>
/// <b>Driven from the watcher's own beat, one pair at a time, and nothing here holds the game loop.</b> Each beat
/// looks at one thing: whether the next pair should be called, whether the pair in the ring has been decided, whether the
/// pause between fights is over, or whether the champion and its challenger are both free for the title bout. The duel itself
/// is <see cref="BotDuel"/> — the pair, the rule, and the two brawls pressed on the fighters — and the fighters' own bodies do
/// the fighting. Argus's part is the summons, the healing, the bracket and the prize: exactly a referee's.
/// </para>
///
/// <para>
/// <b>Only the pair in the ring is summoned.</b> The other entrants go on with their day and are lifted to the ring
/// when their turn comes, so the shard is not stood still for half an hour with thirty bots in a field; a loser walks
/// home from the ring like anybody sent anywhere. Bots in a company are not called — a company owns where its members
/// stand — and neither is anybody with a class paid nothing, which is the Baron. The ring is chosen among the towns each
/// championship (<see cref="BotVenues"/>), weighted by where the entrants are.
/// </para>
///
/// <para>
/// <b>Patrick, 07.10.2026: "Still NOT ONCE have I seen a championship where a champion is chosen in battle", and then: "a minimum of 50
/// participants, 25 sparrings, and only once A DAY".</b> By default the championship is now held at the round arenas east of Papua
/// (<see cref="Arena"/>, the other half of this class in <c>BotTourneyArena.cs</c>): once a real day, at least
/// <see cref="MinEntrants"/> free bots, ported there in batches, a batch's pairs fought at once one pair to a ring, the referee shown at
/// the main ring. What follows in this file is the hourly town-ring knockout, kept behind the dial, and the parts both share: the
/// title, the prize, the record.
/// </para>
/// </summary>
public static partial class BotTourney
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotTourney));

    /// <summary>Whether championships are held at all.</summary>
    public static bool Running { get; set; } = true;

    /// <summary>
    /// How often one is held by the calendar. An hour: Patrick's order of 26.09.2026, "the tournament once an hour". It was a
    /// week, and the marshal called one whenever the ring stood empty — seven between 03:19 and 07:42 that night, the last
    /// two sixteen minutes apart.
    /// </summary>
    public static long EveryMs { get; set; } = 3600000L;

    /// <summary>How long after one ends before another may be called by anybody but the keyboard. An hour.</summary>
    public static long LeastGapMs { get; set; } = 3600000L;

    /// <summary>
    /// How long after a boot before the calendar may call one. The hour is kept across a restart now, and a shard down for
    /// three hours would otherwise call one on its first beat, before the population has found its feet.
    /// </summary>
    public static int BootGraceMs { get; set; } = 300000;

    /// <summary>
    /// How long the title bout waits for the champion and its challenger to be free together. A quarter of an hour: a company
    /// marches, fights and breaks up in about that, and a dead champion is raised again well inside it.
    /// </summary>
    public static int TitleWaitMs { get; set; } = 900000;

    /// <summary>
    /// Whether one may be called now: switched on, nothing in the ring, and the hour since the last one is up. Never at the arenas
    /// (07.10.2026, arena27): that one is the day's, held by the calendar or the keyboard, and the marshal does not call it.
    /// </summary>
    public static bool Ready => Running && !Arena && !ArenaOn && _stage == Stage.Idle && (DateTime.Now - _held).TotalMilliseconds >= LeastGapMs;

    /// <summary>Calls refused for coming inside the hour.</summary>
    public static long TooSoon { get; private set; }

    /// <summary>How many are called. Thirty, by order.</summary>
    public static int Entrants { get; set; } = 30;

    /// <summary>
    /// The drill ground: the ring when no other can be had, and Britain's ring whenever Britain is drawn (see
    /// <see cref="BotVenues"/>). The ring of each championship is <see cref="Venue"/>.
    /// </summary>
    // 1458 from 1460 on 26.09.2026: the east point, 1463, is inside a building ("no body fits, the floor could not be found").
    public static Point3D Ring { get; set; } = new(1458, 1500, 0);

    /// <summary>How far apart the two are put down, either side of the ring's middle.</summary>
    public static int Apart { get; set; } = 3;

    /// <summary>The pause between one fight's end and the next pair's call. Eight seconds: long enough to read.</summary>
    public static int BetweenMs { get; set; } = 8000;

    /// <summary>How long past the duel's own cap a fight may stand undecided before the referee calls it.</summary>
    public static int SlackMs { get; set; } = 30000;

    /// <summary>Championships held.</summary>
    public static long Held { get; private set; }

    /// <summary>
    /// What a champion that already holds every prize its kind may choose is given instead: gold, into its bank. See
    /// <see cref="Prize"/>.
    ///
    /// <para>
    /// <b>Patrick, 25.09.2026: "why has Faron Ashdown got five maces in his pack?"</b> Because he had won five
    /// championships, and the prize was a vanquishing copy of the weapon a bot was born with, bound, every time — every
    /// win after the first handed him a war mace he could neither use nor sell. Hale, Gerda, Emrys and Bryn Ashdown
    /// held two swords each the same way.
    /// </para>
    /// </summary>
    public static int RepeatPurse { get; set; } = 1000;

    /// <summary>Championships held in the fair's town while a fair was open (07.10.2026, events17).</summary>
    public static long AtFair { get; private set; }

    /// <summary>Champions paid in gold because they held every prize their kind may choose. See <see cref="RepeatPurse"/>.</summary>
    public static long Repeats { get; private set; }

    /// <summary>Duplicate prizes already in the population's packs, exchanged for prizes of the owners' choosing. See <see cref="Sweep"/>.</summary>
    public static long Duplicates { get; private set; }

    private static bool _swept;

    /// <summary>Championships called, by hand or by the marshal. See <c>BotMarshal.Act</c>.</summary>
    public static long Called { get; private set; }

    /// <summary>Fights fought in them, title bouts included.</summary>
    public static long Fights { get; private set; }

    /// <summary>Byes given for an odd entrant.</summary>
    public static long Byes { get; private set; }

    /// <summary>Pairs that could not be set against each other and were passed over.</summary>
    public static long Unset { get; private set; }

    /// <summary>Fights the referee had to call.</summary>
    public static long CalledOff { get; private set; }

    /// <summary>Title bouts fought: the champion against its challenger, or the final for a vacant title.</summary>
    public static long TitleBouts { get; private set; }

    /// <summary>Title bouts the champion won.</summary>
    public static long Defences { get; private set; }

    /// <summary>Title bouts the challenger won: the title changed hands in the ring.</summary>
    public static long Changes { get; private set; }

    /// <summary>Vacant titles won, in the final bout or with no bout left to fight.</summary>
    public static long VacantCrowns { get; private set; }

    /// <summary>Of those, won without a bout: the last pair forfeited or a bye was the last word.</summary>
    public static long VacantByDefault { get; private set; }

    /// <summary>Champions stripped for not coming to the title bout within <see cref="TitleWaitMs"/>; the challenger was crowned.</summary>
    public static long Stripped { get; private set; }

    /// <summary>Challengers who did not come within <see cref="TitleWaitMs"/>; the champion kept the title without a bout.</summary>
    public static long Walkovers { get; private set; }

    /// <summary>Championships that ended with the title standing and no bout: neither side free, the ring unset, or nobody left.</summary>
    public static long Unfought { get; private set; }

    /// <summary>Contestants with a company when their pair was called, put to the back of the round once instead of forfeiting.</summary>
    public static long Deferred { get; private set; }

    /// <summary>Pairs in which both were still with a company on their second call: both out, nobody goes on.</summary>
    public static long DoubleForfeits { get; private set; }

    /// <summary>Why champions were away when their title bout was due, by reason: resting, with a company, dead, gone.</summary>
    private static readonly Dictionary<string, int> _away = [];

    /// <summary>The name of the standing champion, or null.</summary>
    public static string Champion { get; private set; }

    /// <summary>When the standing champion took the title.</summary>
    private static DateTime _reignSince;

    /// <summary>Title bouts the standing champion has won.</summary>
    private static int _reignDefences;

    private enum Stage
    {
        Idle,
        Calling,
        Fighting,
        Pausing,
        Title
    }

    private static Stage _stage = Stage.Idle;

    private static readonly List<BotMobile> _round = [];

    private static readonly List<BotMobile> _next = [];

    /// <summary>Contestants already put to the back of a round once for being with a company. See <see cref="Call"/>.</summary>
    private static readonly HashSet<Serial> _deferred = [];

    private static BotMobile _a;

    private static BotMobile _b;

    /// <summary>Whether the pair in the ring fights for the title: the champion and its challenger, or the final for a vacant title.</summary>
    private static bool _title;

    /// <summary>Whether the vacant title was decided in a bout rather than by a forfeit or a bye.</summary>
    private static bool _vacantFought;

    /// <summary>The challenger waiting for, or fighting, the title bout.</summary>
    private static BotMobile _challenger;

    private static long _titleTick;

    private static bool _waitSaid;

    /// <summary>Whether this title's failure to set the ring has been counted and said. See <see cref="Title"/>.</summary>
    private static bool _ringUnset;

    private static long _stageTick;

    private static int _roundNo;

    private static int _entered;

    private static DateTime _held = DateTime.MinValue;

    private static long _hueTick;

    private static string _by;

    private static long _bootTick;

    /// <summary>When the calendar's last call was turned away for too few fighters. See <see cref="Beat"/>.</summary>
    private static long _turnedTick = long.MinValue / 2;

    /// <summary>Whether the world save handed back a record this boot. See <see cref="BotTourneyStore"/>.</summary>
    private static bool _restored;

    /// <summary>The ring of the championship running, or of the last one.</summary>
    public static BotVenues.Venue Venue { get; private set; }

    /// <summary>Where the pair in the ring stands: the venue's middle, or the drill ground.</summary>
    private static Point3D RingNow => Venue?.At ?? Ring;

    private static Map RingMap => Venue?.Map ?? BotPopulation.Home ?? Map.Felucca;

    private static string RingName => Venue?.Name ?? "the drill ground";

    /// <summary>Whether one is running now.</summary>
    public static bool InProgress => _stage != Stage.Idle;

    /// <summary>
    /// At the vigil's start: the boot's clock for <see cref="BootGraceMs"/>, and with no record read back from the save, the
    /// hour starts now and nobody holds the title.
    /// </summary>
    public static void Load()
    {
        _bootTick = Core.TickCount;
        Clear();

        // The round arenas: found in the map's own statics, the hold hooked into the duel's record, strays sent home (07.10.2026, arena27).
        ArenaLoad();

        if (!_restored)
        {
            _held = DateTime.Now;
            Champion = null;
            _reignDefences = 0;

            logger.Information("The championship has no record in the save; the hour starts now and the title is vacant");

            return;
        }

        logger.Information(
            "The championship was last held {Held}; the champion is {Champion}{Reign}",
            _held,
            Champion ?? "nobody",
            Champion == null ? "" : $", holding it since {_reignSince:yyyy-MM-dd HH:mm} with {_reignDefences} defences"
        );
    }

    /// <summary>Written into the world save. See <see cref="BotTourneyStore"/>.</summary>
    public static void Save(IGenericWriter writer)
    {
        // Shape 2 (07.10.2026, arena27): the day's championship at the arenas, as a UTC instant, after the shape-1 record.
        writer.WriteEncodedInt(2);
        writer.Write(_held.ToUniversalTime().Ticks);
        writer.Write(Champion ?? "");
        writer.Write(_reignSince.ToUniversalTime().Ticks);
        writer.WriteEncodedInt(_reignDefences);
        BotVenues.Save(writer);
        writer.Write(_dayHeld.Ticks);
    }

    /// <summary>Read back from the world save, before the vigil starts. False for a shape this build does not write.</summary>
    public static bool Load(IGenericReader reader)
    {
        var shape = reader.ReadEncodedInt();

        if (shape is not (1 or 2))
        {
            return false;
        }

        _held = new DateTime(reader.ReadLong(), DateTimeKind.Utc).ToLocalTime();

        var champion = reader.ReadString();

        Champion = string.IsNullOrWhiteSpace(champion) ? null : champion;
        _reignSince = new DateTime(reader.ReadLong(), DateTimeKind.Utc).ToLocalTime();
        _reignDefences = reader.ReadEncodedInt();
        BotVenues.Load(reader);

        // Shape 1 knew nothing of the day: no day's championship on record, so the calendar may hold today's.
        _dayHeld = shape >= 2 ? new DateTime(reader.ReadLong(), DateTimeKind.Utc) : DateTime.MinValue;
        _restored = true;

        return true;
    }

    /// <summary>Whether the hour is up, and the shard has been up long enough to hold one.</summary>
    public static bool Due(DateTime now) =>
        Running && (now - _held).TotalMilliseconds >= EveryMs && Core.TickCount - _bootTick >= BootGraceMs;

    /// <summary>
    /// Begins one, by hand or by the calendar. Returns what happened, for the door.
    /// </summary>
    public static string Start(string by)
    {
        if (!Running)
        {
            return "championships are switched off (BotTourney.Running).";
        }

        // At the arenas, once a day (07.10.2026, arena27): the keyboard's call is "do arena", anybody else's is refused there.
        if (Arena || ArenaOn)
        {
            return ArenaStart(by, false);
        }

        if (_stage != Stage.Idle)
        {
            return _stage == Stage.Title
                ? $"a championship is waiting for its title bout: {Champion} against {_challenger?.Name}."
                : $"a championship is already running: round {_roundNo}, {_round.Count + _next.Count} still in it.";
        }

        // The keyboard may call one whenever it likes; the marshal and the calendar keep to the hour.
        if (by != "the keyboard" && (DateTime.Now - _held).TotalMilliseconds < LeastGapMs)
        {
            TooSoon++;

            return $"too soon: the last championship ended {(DateTime.Now - _held).TotalMinutes:F0} minutes ago, and they are held once an hour.";
        }

        var fighters = Fighters();

        // A champion needs one challenger; a vacant title needs a final.
        var least = Champion != null ? 1 : 2;

        if (fighters.Count < least)
        {
            return $"only {fighters.Count} fit fighters could be called; a championship {(Champion != null ? $"for {Champion}'s title" : "for the vacant title")} wants {least} at least.";
        }

        List<Mobile> who = [..fighters];

        if (Find(Champion) is { } holder)
        {
            who.Add(holder);
        }

        // At the fair while one is open (07.10.2026, events17): "a tournament before Argus" in the fair's town, when its square
        // holds a ring; the town's turn otherwise. See BotFair.
        Venue = (BotFair.TownName is { } fair ? BotVenues.InTown(BotVenues.Use.Ring, BotPopulation.Home ?? Map.Felucca, fair) : null)
                ?? BotVenues.Choose(BotVenues.Use.Ring, BotPopulation.Home ?? Map.Felucca, who);

        if (BotFair.TownName != null && Venue?.Town == BotFair.TownName)
        {
            AtFair++;
        }

        _round.Clear();
        _next.Clear();
        _deferred.Clear();
        _round.AddRange(fighters);
        _roundNo = 1;
        Called++;
        _entered = fighters.Count;
        _by = by;
        _title = false;
        _vacantFought = false;
        _challenger = null;
        _stage = Stage.Calling;
        _stageTick = Core.TickCount;

        logger.Information(
            "Tourney: a championship is called by {By} at {Venue} ({X},{Y}) for {Title} with {Count} entrants, the strongest first: {Names}",
            by,
            RingName,
            RingNow.X,
            RingNow.Y,
            Champion != null ? $"the right to challenge {Champion}" : "the vacant title",
            fighters.Count,
            string.Join(", ", fighters.ConvertAll(f => $"{f.Name} ({BotThreat.Power(f):F0})"))
        );

        BotVenues.Herald(Venue, BotVenues.HeraldHoldMs);
        BotVenues.Announce(
            Champion != null
                ? $"The championship is called {Where()}: {fighters.Count} from the ranks fight for the right to challenge {Champion}, the champion."
                : $"The championship is called {Where()}: {fighters.Count} from the ranks fight for the vacant title."
        );

        return $"the championship is on {Where()}: {fighters.Count} entrants, {fighters[0].Name} the strongest at {BotThreat.Power(fighters[0]):F0}{(Champion != null ? $", for the right to challenge {Champion}" : ", for the vacant title")}. Watch the log for \"Tourney:\".";
    }

    /// <summary>"at Minoc", "at the drill ground": where the ring of the championship stands, for a sentence.</summary>
    private static string Where() => $"at {RingName}";

    /// <summary>Called off, by hand. Whoever is in the ring is released.</summary>
    public static string Stop()
    {
        if (ArenaOn)
        {
            return ArenaStop("called off by hand");
        }

        if (_stage == Stage.Idle)
        {
            return "no championship is running.";
        }

        Release();
        _round.Clear();
        _next.Clear();
        _challenger = null;
        _title = false;
        _stage = Stage.Idle;
        BotVenues.Release();

        logger.Information("Tourney: the championship was called off");

        return "the championship is called off.";
    }

    /// <summary>The strongest fit fighters, most powerful first, up to <see cref="Entrants"/>; never the champion, who defends.</summary>
    private static List<BotMobile> Fighters()
    {
        var bots = BotPopulation.Bots;
        List<BotMobile> fit = [];

        for (var i = 0; i < bots.Count; i++)
        {
            var bot = bots[i];

            if (bot is not { Deleted: false, Alive: true, Fallen: false } || bot.Class is not { } klass)
            {
                continue;
            }

            if (klass.Role == BotRole.Producer || klass.Unpaid || bot.Squad != null || bot.Murderer || BotDuel.Duelling(bot))
            {
                continue;
            }

            if (bot.Map == null || bot.Map == Map.Internal)
            {
                continue;
            }

            if (Champion != null && string.Equals(bot.Name, Champion, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            fit.Add(bot);
        }

        fit.Sort((x, y) => BotThreat.Power(y).CompareTo(BotThreat.Power(x)));

        if (fit.Count > Entrants)
        {
            fit.RemoveRange(Entrants, fit.Count - Entrants);
        }

        return fit;
    }

    /// <summary>One beat of the referee. Called from the watcher's own clock.</summary>
    public static void Beat(long now)
    {
        if (!Running)
        {
            return;
        }

        // The champion's yellow name, kept on it: the population is rebuilt at every boot and a hue is not saved.
        if (Champion != null && now - _hueTick >= 60000)
        {
            _hueTick = now;
            Crowned(Find(Champion));
            Regalia();
        }

        // Strays in the rings sent home every minute whatever the dial says (07.10.2026, arena27 review). See StraySweep.
        StraySweep(now);

        // At the arenas (07.10.2026, arena27): the day's calendar, the batches, and — for the title — the stages below.
        if (Arena || ArenaOn)
        {
            ArenaBeat(now);

            return;
        }

        switch (_stage)
        {
            case Stage.Idle:
                // A calendar call the field turned away (too few fit fighters) is asked again a minute later, not every beat:
                // each ask weighs the whole population's power.
                if (now - _turnedTick >= 60000 && Due(DateTime.Now))
                {
                    var before = Called;

                    Start("the calendar");

                    if (Called == before)
                    {
                        _turnedTick = now;
                    }
                }

                return;

            case Stage.Calling:
                Call(now);

                return;

            case Stage.Fighting:
                Judge(now);

                return;

            case Stage.Pausing:
                if (now - _stageTick >= BetweenMs)
                {
                    _stage = Stage.Calling;
                    _stageTick = now;
                }

                return;

            case Stage.Title:
                Title(now);

                return;
        }
    }

    /// <summary>Whether a contestant is held by a company or a party, and is not to be summoned. See the note in <see cref="Call"/>.</summary>
    private static bool Busy(BotMobile bot) =>
        bot != null && (bot.Squad != null || BotDelveParty.Delving(bot) || bot.Resolve?.Deed is BotDelve or BotEnlist);

    /// <summary>Bouts forfeited because a contestant was with a company. See <see cref="Busy"/>.</summary>
    public static long Forfeits { get; private set; }

    /// <summary>
    /// Why this one cannot come to the ring now, or null when it can: resting, gone, dead, with a company, down a dungeon,
    /// in another duel. Asked of the champion and its challenger before the title bout.
    /// </summary>
    private static string Away(BotMobile bot, string name)
    {
        if (bot == null)
        {
            var away = BotPopulation.Away;

            for (var i = 0; i < away.Count; i++)
            {
                if (away[i] is { Deleted: false } resting && string.Equals(resting.Name, name, StringComparison.OrdinalIgnoreCase))
                {
                    return "resting";
                }
            }

            return "gone";
        }

        if (bot.Deleted)
        {
            return "gone";
        }

        if (!bot.Alive || bot.Fallen)
        {
            return "dead";
        }

        if (bot.Map == null || bot.Map == Map.Internal)
        {
            return "resting";
        }

        if (bot.Squad != null || bot.Resolve?.Deed is BotEnlist)
        {
            return "with a company";
        }

        if (BotDelveParty.Delving(bot) || bot.Resolve?.Deed is BotDelve)
        {
            return "down a dungeon";
        }

        // The record, not the arena's hold: the challenger waits at the arenas held, and that is not another duel (07.10.2026, arena27).
        return BotDuel.InBout(bot) ? "in another duel" : null;
    }

    /// <summary>The next pair into the ring, or the next round, or the challenger.</summary>
    private static void Call(long now)
    {
        // Anybody who died or left the shard between rounds is out.
        _round.RemoveAll(m => m is not { Deleted: false, Alive: true });
        _next.RemoveAll(m => m is not { Deleted: false, Alive: true });

        if (_round.Count == 0)
        {
            if (_next.Count == 0)
            {
                logger.Information("Tourney: nobody is left standing at {Venue}; no challenger", RingName);
                Finish(
                    Champion != null ? $"nobody from the ranks was left standing; {Champion} keeps the title." : "nobody was left standing; the title stays vacant.",
                    true
                );

                return;
            }

            if (_next.Count == 1)
            {
                Contender(_next[0], now);

                return;
            }

            _round.AddRange(_next);
            _next.Clear();
            _roundNo++;

            logger.Information("Tourney: round {Round} with {Count} left", _roundNo, _round.Count);
        }

        // The only one left in the whole bracket — the others forfeited or the field was one — is the last one standing, not a bye.
        if (_round.Count == 1 && _next.Count == 0)
        {
            var last = _round[0];

            _round.Clear();
            Contender(last, now);

            return;
        }

        if (_round.Count == 1)
        {
            Byes++;
            _next.Add(_round[0]);
            _round.RemoveAt(0);

            logger.Information("Tourney: {Name} has a bye", _next[^1].Name);

            return;
        }

        var a = _round[0];
        var b = _round[1];

        // <b>A bout does not take a bot out of its company (01.10.2026, build next).</b> Entrants are drawn free of any company, but
        // the bracket runs for an hour and the summons carries a bot across the map: on build 384 (00:42–10:20) four delves were
        // "dropped: pressed to duel" — the leader of a party of twelve, called out of the march — and enlistments were dropped
        // the same way, with 1948 beats of marching leaders slowed for members off "duel". A contestant now in a company, or down
        // a dungeon with a party, forfeits the bout; the other goes on. Counted with the pairs passed over.
        //
        // <b>Asked again once before forfeiting, and never the busy one sent on (02.10.2026).</b> A company is often on its way
        // to breaking up, so a contestant with one is put to the back of the round once (Deferred); only on its second call does
        // it forfeit. And a pair with both sides still busy sends nobody on: until build 389 it sent the busy one, and on
        // 02.10.2026 at 07:27 Miriel was crowned champion while "with a company and forfeits".
        var busyA = Busy(a);
        var busyB = Busy(b);

        if (busyA && _deferred.Add(a.Serial))
        {
            Deferred++;
            _round.RemoveAt(0);
            _round.Add(a);

            logger.Information("Tourney: {Name} is with a company; asked again at the end of round {Round}", a.Name, _roundNo);

            return;
        }

        if (busyB && _deferred.Add(b.Serial))
        {
            Deferred++;
            _round.RemoveAt(1);
            _round.Add(b);

            logger.Information("Tourney: {Name} is with a company; asked again at the end of round {Round}", b.Name, _roundNo);

            return;
        }

        if (busyA || busyB)
        {
            Unset++;
            Forfeits++;
            _round.RemoveRange(0, 2);

            if (busyA && busyB)
            {
                DoubleForfeits++;

                logger.Information("Tourney: {A} and {B} are both still with a company; both forfeit and nobody goes on", a.Name, b.Name);

                return;
            }

            var goes = busyA ? b : a;

            logger.Information("Tourney: {Busy} is still with a company and forfeits; {Goes} goes on", busyA ? a.Name : b.Name, goes.Name);

            _next.Add(goes);

            return;
        }

        // The last two of the bracket with nobody holding the title: their bout is the title bout.
        var final = Champion == null && _round.Count == 2 && _next.Count == 0;

        _round.RemoveRange(0, 2);

        if (!Bring(a, b))
        {
            Unset++;
            logger.Warning("Tourney: {A} or {B} could not be put in the ring at {Venue}; the first goes on by default", a.Name, b.Name, RingName);
            _next.Add(a);

            return;
        }

        _a = a;
        _b = b;
        _title = final;
        Fights++;
        _stage = Stage.Fighting;
        _stageTick = now;

        if (final)
        {
            TitleBouts++;

            BotVenues.Herald(Venue, BotDuel.CapMs + SlackMs);
            BotVenues.Announce($"Title bout {Where()}, for the vacant crown: {Billing(a)} against {Billing(b)}!");
        }

        logger.Information(
            "Tourney: round {Round}{Title}, {A} ({PowerA:F0}) against {B} ({PowerB:F0}) in the ring at {Venue} ({X},{Y})",
            _roundNo,
            final ? " — the title bout for the vacant crown" : "",
            a.Name,
            BotThreat.Power(a),
            b.Name,
            BotThreat.Power(b),
            RingName,
            RingNow.X,
            RingNow.Y
        );
    }

    /// <summary>Both into the ring, healed, and set against each other; false, with nobody moved, when either cannot be.</summary>
    private static bool Bring(BotMobile a, BotMobile b) => Bring(a, b, RingMap, RingNow);

    /// <summary>The same, in this ring: the arenas set each pair of a batch in a ring of its own (07.10.2026, arena27).</summary>
    private static bool Bring(BotMobile a, BotMobile b, Map map, Point3D ring)
    {

        if (!Spot(map, ring, ring.X - Apart, ring.Y, out var at) || !Spot(map, ring, ring.X + Apart, ring.Y, out var bt))
        {
            return false;
        }

        // At the arenas the champion is held from here, so it is sent back to where it came from afterwards (07.10.2026, arena27).
        if (ArenaOn)
        {
            Hold(a);
            Hold(b);
        }

        if (!Put(a, map, at) || !Put(b, map, bt))
        {
            return false;
        }

        Heal(a);
        Heal(b);

        // At the arenas each fighter's wait is put down over its own work, so both come back in order (BotDuel.Begin's stack).
        if (!BotDuel.Begin(a, b, Trains(a), Trains(b), ArenaOn))
        {
            logger.Warning("Tourney: {A} and {B} could not be set against each other", a.Name, b.Name);

            return false;
        }

        return true;
    }

    /// <summary>"Odile the WarriorMage": a name and a class, for the herald.</summary>
    private static string Billing(BotMobile bot) => bot?.Class?.Name is { } klass ? $"{bot.Name} the {klass}" : bot?.Name ?? "nobody";

    /// <summary>The last of the ranks standing: crowned to a vacant title, or the champion's challenger.</summary>
    private static void Contender(BotMobile winner, long now)
    {
        if (Champion == null)
        {
            VacantCrowns++;

            if (!_vacantFought)
            {
                VacantByDefault++;
            }

            Crown(
                winner,
                _vacantFought ? "won the vacant title in the ring" : "took the vacant title with nobody left to fight",
                _vacantFought
                    ? $"{Billing(winner)} wins the vacant title {Where()} and is the champion of the island!"
                    : $"{Billing(winner)} is the last of the ranks standing {Where()} and takes the vacant title."
            );

            return;
        }

        _challenger = winner;
        _stage = Stage.Title;
        _titleTick = now;
        _waitSaid = false;
        _ringUnset = false;

        logger.Information(
            "Tourney: {Name} came through {Entered} from the ranks over {Rounds} rounds and challenges {Champion} for the title",
            winner.Name,
            _entered,
            _roundNo,
            Champion
        );

        BotVenues.Announce($"{Billing(winner)} has come through {_entered} from the ranks and challenges {Champion} for the title {Where()}.");
    }

    /// <summary>
    /// The title bout, once the champion and its challenger are both free; or, after <see cref="TitleWaitMs"/>, the title
    /// decided without one: the champion stripped, the challenger's walkover, or the title standing.
    /// </summary>
    private static void Title(long now)
    {
        var holder = Find(Champion);
        var away = Away(holder, Champion);
        var challenger = _challenger;
        var late = challenger is { Deleted: false } ? Away(challenger, challenger.Name) : "gone";

        if (away == null && late == null)
        {
            if (Bring(holder, challenger))
            {
                _ringUnset = false;
                _a = holder;
                _b = challenger;
                _title = true;
                Fights++;
                TitleBouts++;
                _stage = Stage.Fighting;
                _stageTick = now;

                logger.Information(
                    "Tourney: the title bout at {Venue} ({X},{Y}): {Champion} ({PowerA:F0}), champion since {Since:HH:mm} with {Defences} defences, against {Challenger} ({PowerB:F0}){Waited}",
                    RingName,
                    RingNow.X,
                    RingNow.Y,
                    holder.Name,
                    BotThreat.Power(holder),
                    _reignSince,
                    _reignDefences,
                    challenger.Name,
                    BotThreat.Power(challenger),
                    now - _titleTick >= 5000 ? $", after waiting {(now - _titleTick) / 1000}s for them both to be free" : ""
                );

                BotVenues.Herald(Venue, BotDuel.CapMs + SlackMs);
                BotVenues.Announce(
                    $"Title bout {Where()}: {Billing(holder)}, champion{(_reignDefences > 0 ? $" with {_reignDefences} defences" : "")}, defends against {Billing(challenger)}!"
                );

                return;
            }

            // Counted and said once a title, not once a beat: the next beat tries the ring again.
            if (!_ringUnset)
            {
                _ringUnset = true;
                Unset++;
                logger.Warning("Tourney: {Champion} and {Challenger} could not be put in the ring at {Venue}; tried again each beat", Champion, challenger.Name, RingName);
            }
        }

        // Nobody waits for a body that has left the island: a champion gone against a challenger at hand, or a challenger gone.
        if (now - _titleTick < TitleWaitMs && !(away == "gone" && late == null) && late != "gone")
        {
            if (!_waitSaid && (away != null || late != null))
            {
                _waitSaid = true;

                logger.Information(
                    "Tourney: the title bout waits up to {Minutes} minutes: {Who} is {Why}",
                    TitleWaitMs / 60000,
                    away != null ? Champion : challenger?.Name,
                    away ?? late
                );

                BotVenues.Announce(
                    away != null
                        ? $"The champion {Champion} is {away}; the title bout against {challenger?.Name} waits up to {TitleWaitMs / 60000} minutes."
                        : $"The challenger {challenger?.Name} is {late}; the title bout waits up to {TitleWaitMs / 60000} minutes."
                );
            }

            return;
        }

        // The quarter of an hour is up — or the champion is no longer on the island at all, and there is nobody to wait for.
        if (away != null && late == null && (away == "gone" || now - _titleTick >= TitleWaitMs))
        {
            Stripped++;
            _away[away] = _away.TryGetValue(away, out var had) ? had + 1 : 1;

            var was = Champion;

            logger.Information("Tourney: {Champion} did not come to defend the title ({Why}); it passes to {Challenger}", was, away, challenger.Name);

            Crown(
                challenger,
                $"took the title from {was}, who did not come to defend it ({away})",
                $"{was} did not come to defend the title ({away}); {Billing(challenger)} is the champion of the island."
            );

            return;
        }

        if (away == null && late != null)
        {
            Walkovers++;

            logger.Information("Tourney: the challenger {Challenger} did not come ({Why}); {Champion} keeps the title", challenger?.Name, late, Champion);
            Finish($"the challenger {challenger?.Name} did not come ({late}); {Champion} keeps the title.", false);

            return;
        }

        if (away == null)
        {
            logger.Information("Tourney: no title bout: the ring at {Venue} could not take {Champion} and {Challenger}; the title stands", RingName, Champion, challenger?.Name);
            Finish($"no title bout this time: the ring {Where()} could not be set; {Champion} keeps the title.", true);

            return;
        }

        logger.Information("Tourney: no title bout: {Champion} is {Why} and {Challenger} is {Late}; the title stands", Champion, away, challenger?.Name, late);
        Finish($"no title bout this time: {Champion} is {away} and {challenger?.Name} is {late}; the title stands.", true);
    }

    /// <summary>A championship over without a new champion: the clock set, the herald's line; counted as unfought when no title bout was had.</summary>
    private static void Finish(string said, bool unfought)
    {
        if (unfought)
        {
            Unfought++;
        }

        Held++;
        _held = DateTime.Now;
        _stage = Stage.Idle;
        _challenger = null;
        _title = false;

        BotVenues.Announce(char.ToUpperInvariant(said[0]) + said[1..]);
    }

    /// <summary>Whether the pair in the ring has been decided, and the referee's own call when it has stood too long.</summary>
    private static void Judge(long now)
    {
        if (_a == null || _b == null)
        {
            _stage = Stage.Calling;

            return;
        }

        var winner = BotDuel.Winner(_a);

        if (winner == null && now - _stageTick >= BotDuel.CapMs + SlackMs)
        {
            CalledOff++;
            BotDuel.Call(_a, "the referee called it");
            winner = BotDuel.Winner(_a);
        }

        if (winner == null)
        {
            // Both gone from the record: one died in the ring and the bout was cleared. The one standing has it.
            if (!BotDuel.InBout(_a) && !BotDuel.InBout(_b))
            {
                winner = _a is { Deleted: false, Alive: true } ? _a : _b;
            }
            else
            {
                return;
            }
        }

        var loser = ReferenceEquals(winner, _a) ? _b : _a;
        var title = _title;
        var holder = _a;

        BotDuel.End(_a, _b);
        Heal(_a);
        Heal(_b);

        _a = null;
        _b = null;
        _title = false;

        // The title bout of a reigning champion: the champion stands in _a.
        if (title && _stage == Stage.Fighting && _challenger != null && Champion != null)
        {
            BotVenues.Release();

            if (ReferenceEquals(winner, holder))
            {
                Defences++;
                _reignDefences++;

                logger.Information(
                    "Tourney: {Champion} defends the title against {Challenger} at {Venue}: {Defences} defences since {Since:HH:mm}",
                    winner.Name,
                    loser?.Name,
                    RingName,
                    _reignDefences,
                    _reignSince
                );

                // A defence in a forced test is not a defence of the title (see Crown): the story keeps the real ones. See BotStory.Defended.
                if (!(ArenaOn && _forced))
                {
                    BotStory.Defended(winner.Name, loser?.Name, _reignDefences);
                }

                Finish($"{Billing(winner)} defends the title against {loser?.Name} — {_reignDefences} {(_reignDefences == 1 ? "defence" : "defences")} now.", false);
                winner.Say($"The title stays with me, {loser?.Name}.");

                return;
            }

            Changes++;

            var was = Champion;

            logger.Information("Tourney: {Challenger} takes the title from {Champion} at {Venue}", winner.Name, was, RingName);

            Crown(winner, $"took the title from {was} in the ring", $"{Billing(winner)} takes the title from {was} {Where()} and is the champion of the island!");

            return;
        }

        if (title)
        {
            // The final of a vacant title: the winner goes on as the last one standing and is crowned on the next call.
            _vacantFought = true;
            BotVenues.Release();
        }

        _next.Add(winner);

        logger.Information("Tourney: {Winner} goes on, {Loser} is out", winner.Name, loser?.Name);

        _stage = Stage.Pausing;
        _stageTick = now;
    }

    /// <summary>The champion: a yellow name, the prize, the record, the herald's line.</summary>
    private static void Crown(BotMobile winner, string how, string line)
    {
        // A forced test never takes a standing champion's title (07.10.2026, second pass of arena27 review B); with the title
        // vacant it crowns, as Patrick asked to see a champion chosen in battle.
        if (ArenaOn && _forced && !string.IsNullOrEmpty(Champion))
        {
            _stage = Stage.Idle;
            _challenger = null;
            _title = false;
            logger.Information("Tourney: {Name} wins the forced test ({How}); the title stays with {Champion}", winner?.Name, how, Champion);
            BotVenues.Announce($"{winner?.Name} wins the test bouts at the arenas; {Champion} is still the champion.");
            return;
        }

        Sweep();

        Held++;
        _held = DateTime.Now;
        Champion = winner?.Name;
        _reignSince = DateTime.Now;
        _reignDefences = 0;
        _stage = Stage.Idle;
        _challenger = null;
        _title = false;

        Uncrown(Champion);
        Crowned(winner);

        // A forced test at the arenas pays nothing (07.10.2026, arena27 review): a test is not a championship.
        var test = ArenaOn && _forced;
        var prize = test ? "nothing: a forced test is not a championship" : Prize(winner);

        logger.Information(
            "Tourney: {Name} the {Class} is the champion of the shard: {How}, at {Venue}, from {Entered} entrants over {Rounds} rounds, called by {By}; the prize is {Prize}",
            winner?.Name,
            winner?.Class?.Name,
            how,
            RingName,
            _entered,
            _roundNo,
            _by,
            prize
        );

        BotVenues.Announce(test ? $"{line} (A test bout: no prize.)" : line);

        // The island's Chronicle and the champion's own story, at the minute of the crowning (PLAN-life A3, 13.10.2026). See BotStory.Crowned.
        if (!test)
        {
            BotStory.Crowned(winner, how);
        }
        else
        {
            // A test on a vacant title still crowns (Patrick asked to see a champion chosen in battle) and turns the name yellow;
            // the story is told so, and writes neither the crown nor, later, its loss. See BotStory.TestCrowned.
            BotStory.TestCrowned(winner);
        }

        winner?.Say($"I am the champion of this island, and my name is {winner.Name}!");
    }

    /// <summary>A new world has no champion: the record goes, the yellow with it. Called by the reset (<c>BotHand</c>).</summary>
    public static void Forget()
    {
        Clear();
        AtFair = 0;
        Champion = null;
        _held = DateTime.Now;
        _reignDefences = 0;
        _swept = false;
        _away.Clear();
        Venue = null;
        Uncrown(null);
        ArenaForget();
        BotVenues.Forget();
    }

    /// <summary>Nothing in the ring and nobody waiting: a new world, or a vigil starting, has no championship in progress.</summary>
    private static void Clear()
    {
        _stage = Stage.Idle;
        _round.Clear();
        _next.Clear();
        _deferred.Clear();
        _a = null;
        _b = null;
        _challenger = null;
        _title = false;
    }

    /// <summary>Takes the yellow off anybody who is not the champion. The population is one list, so this is one pass.</summary>
    private static void Uncrown(string except)
    {
        var bots = BotPopulation.Bots;

        for (var i = 0; i < bots.Count; i++)
        {
            var bot = bots[i];

            if (bot is { Deleted: false } && bot.NameHue == BotMobile.ChampionHue
                && !string.Equals(bot.Name, except, StringComparison.OrdinalIgnoreCase))
            {
                bot.NameHue = -1;
            }
        }
    }

    private static void Crowned(BotMobile bot)
    {
        if (bot is { Deleted: false } && bot.NameHue != BotMobile.ChampionHue)
        {
            bot.NameHue = BotMobile.ChampionHue;
            bot.InvalidateProperties();
        }
    }

    /// <summary>
    /// The champion's prize, chosen by the champion: the first thing it lacks, in the order its kind needs them.
    ///
    /// <para>
    /// <b>Patrick's orders of 25.09.2026: "prizes must be unique; the bot may choose — armour a mage can meditate in,
    /// robes for mana, or a unique mount".</b> Until then the prize was a vanquishing copy of the weapon a bot was born
    /// with (an invulnerable leather chest for a caster or healer), bound, every time — Faron Ashdown won five
    /// championships and carried five identical war maces he could neither use nor sell. Now a fighter takes the weapon
    /// if it has not got it, then an invulnerable copy of a piece of armour it wears, then a steed of its own; a caster
    /// or a healer takes a robe of the arcane mind (see <see cref="RobeInt"/>), then invulnerable leather — the armour
    /// meditation works in — then the steed. Only a champion holding all of it is paid <see cref="RepeatPurse"/> in gold.
    /// </para>
    /// </summary>
    private static string Prize(BotMobile bot)
    {
        var pack = bot?.Backpack;

        if (bot == null || pack == null)
        {
            return "nothing, there was no pack to put it in";
        }

        var total = 0;

        for (var i = 0; i < Draws.Length; i++)
        {
            total += Draws[i].Weight;
        }

        var roll = Utility.Random(total);
        var drawn = Draws.Length - 1;

        for (var i = 0; i < Draws.Length; i++)
        {
            if (roll < Draws[i].Weight)
            {
                drawn = i;

                break;
            }

            roll -= Draws[i].Weight;
        }

        var draw = Draws[drawn];

        Drawn[drawn]++;

        string won;

        if (draw.Level > 0)
        {
            var top = draw.Level == TopLevel;
            var role = bot.Class?.Role ?? BotRole.Melee;

            won = role is BotRole.Caster or BotRole.Medic
                ? (top ? Robe(bot, pack) : null) ?? Leather(bot, pack, draw.Level) ?? (top ? Steed(bot, pack) : null)
                : Weapon(bot, pack, draw.Level) ?? Armour(bot, pack, draw.Level) ?? (top ? Steed(bot, pack) : null);

            if (won == null)
            {
                Repeats++;
                Pay(bot, pack, RepeatPurse);

                won = $"{RepeatPurse}gp, holding every prize of that rung its kind may choose";
            }
        }
        else if (draw.Gold > 0)
        {
            Pay(bot, pack, draw.Gold);

            won = $"{draw.Gold}gp";
        }
        else
        {
            won = Supplies(bot, pack);
        }

        return $"{won} (the draw fell on {draw.Name}, {draw.Weight} in {total})";
    }

    /// <summary>The magic level of the top prize: vanquishing, invulnerability.</summary>
    private const int TopLevel = 5;

    /// <summary>
    /// The prize scale, Patrick's order of 26.09.2026: "giving a top item every time is really generous — a Fibonacci
    /// principle, where 1 is the top item and 55 is just gold". A rung's weight is its chance, out of the sum (142): the top
    /// rung once, the four below it at the lower magic levels 2, 3, 5 and 8 times, a pack of supplies 13, and gold the rest.
    /// Within an item rung the champion still chooses what it lacks, as before: the weapon it was born with, then a piece of
    /// the armour it wears (a caster: its robe at the top, then leather), and at the top a steed of its own.
    /// </summary>
    private static readonly (int Weight, int Level, int Gold, string Name)[] Draws =
    [
        (1, 5, 0, "the top: vanquishing or invulnerability"),
        (2, 4, 0, "power or fortification"),
        (3, 3, 0, "force or hardening"),
        (5, 2, 0, "might or guarding"),
        (8, 1, 0, "ruin or defense"),
        (13, 0, 0, "a pack of supplies"),
        (21, 0, 1000, "a purse of 1000"),
        (34, 0, 500, "a purse of 500"),
        (55, 0, 250, "a purse of 250")
    ];

    /// <summary>How often each rung of <see cref="Draws"/> has come up since the boot.</summary>
    private static readonly long[] Drawn = new long[9];

    private static readonly string[] WeaponWords = ["", "ruin", "might", "force", "power", "vanquishing"];

    private static readonly string[] ArmourWords = ["", "defense", "guarding", "hardening", "fortification", "invulnerability"];

    /// <summary>How many of each a supply prize holds.</summary>
    public static int SupplyBandages { get; set; } = 40;

    public static int SupplyHeals { get; set; } = 3;

    public static int SupplyCures { get; set; } = 2;

    public static int SupplyReagents { get; set; } = 20;

    public static int SupplyAmmo { get; set; } = 150;

    /// <summary>
    /// A pack of what a fight is fought with: bandages and bottles for anybody, reagents for anybody whose build casts, and
    /// ammunition for a bow the bot was born with. Not bound: it is meant to run out.
    /// </summary>
    private static string Supplies(BotMobile bot, Container pack)
    {
        var said = $"a pack of supplies: {SupplyBandages} bandages, {SupplyHeals} heal and {SupplyCures} cure potions";

        pack.TryDropItem(bot, new Bandage(SupplyBandages), false);
        pack.TryDropItem(bot, new HealPotion { Amount = SupplyHeals }, false);
        pack.TryDropItem(bot, new CurePotion { Amount = SupplyCures }, false);

        if (bot.Class?.Role is BotRole.Caster or BotRole.Medic || bot.Skills.Magery.Base >= 30.0)
        {
            Item[] herbs =
            [
                new BlackPearl(SupplyReagents), new Bloodmoss(SupplyReagents), new Garlic(SupplyReagents), new Ginseng(SupplyReagents),
                new MandrakeRoot(SupplyReagents), new Nightshade(SupplyReagents), new SpidersSilk(SupplyReagents), new SulfurousAsh(SupplyReagents)
            ];

            for (var i = 0; i < herbs.Length; i++)
            {
                pack.TryDropItem(bot, herbs[i], false);
            }

            said += $", {SupplyReagents} of each reagent";
        }

        if (bot.Bond?.Weapon is { Weapon: { } kind } && typeof(BaseRanged).IsAssignableFrom(kind)
            && kind.CreateInstance<Item>() is BaseRanged bow)
        {
            var ammo = bow.AmmoType;

            bow.Delete();

            if (ammo != null && ammo.CreateInstance<Item>() is { } shot)
            {
                shot.Amount = SupplyAmmo;
                pack.TryDropItem(bot, shot, false);

                said += $", {SupplyAmmo} {ammo.Name.ToLowerInvariant()}s";
            }
        }

        return said;
    }

    /// <summary>How much Intelligence the champion's robe lends while it is worn: mana, for a caster. See <see cref="Regalia"/>.</summary>
    public static int RobeInt { get; set; } = 15;

    /// <summary>The robe's colour, which is also how it is known. Deep blue.</summary>
    public const int RobeHue = 0x0555;

    /// <summary>A champion's steed's colour: gold, so it is known across a field.</summary>
    public const int SteedHue = 0x0501;

    private const string RobeMod = "champion's robe";

    /// <summary>
    /// A copy of the weapon the bot was born with at this magic level, if it swings or shoots and holds no prize of that
    /// weapon as good. A better one replaces a worse prize of the same weapon, so a champion never carries two.
    /// </summary>
    private static string Weapon(BotMobile bot, Container pack, int level)
    {
        if (bot.Class?.Role is not (BotRole.Melee or BotRole.Ranged) || bot.Bond?.Weapon is not { Weapon: { } kind })
        {
            return null;
        }

        var owned = Owned(bot, kind);

        for (var i = 0; i < owned.Count; i++)
        {
            if (owned[i] is BaseWeapon held && (int)held.DamageLevel >= level)
            {
                return null;
            }
        }

        if (kind.CreateInstance<Item>() is not BaseWeapon copy)
        {
            return null;
        }

        copy.DamageLevel = (WeaponDamageLevel)level;
        copy.DurabilityLevel = (WeaponDurabilityLevel)level;

        var said = Give(bot, pack, copy, $"{kind.Name} of {WeaponWords[level]}, bound");

        if (said != null)
        {
            Replace(bot, owned);
        }

        return said;
    }

    /// <summary>Takes the lesser prizes a better one has just replaced out of the bond and the world.</summary>
    private static void Replace(BotMobile bot, List<Item> lesser)
    {
        for (var i = 0; i < lesser.Count; i++)
        {
            if (lesser[i] is { Deleted: false } old)
            {
                bot.Bond?.Items.Remove(old.Serial);
                old.Delete();
            }
        }
    }

    /// <summary>
    /// An invulnerable copy of a piece of armour the bot is wearing and has no prize of: chest first, then legs, arms,
    /// gloves, neck and head. What it wears it can wear, so the copy fits; the harness puts it on, as it puts on any
    /// better piece found in the pack.
    /// </summary>
    private static string Armour(BotMobile bot, Container pack, int level)
    {
        Layer[] order = [Layer.InnerTorso, Layer.Pants, Layer.Arms, Layer.Gloves, Layer.Neck, Layer.Helm];

        for (var i = 0; i < order.Length; i++)
        {
            if (bot.FindItemOnLayer(order[i]) is not BaseArmor worn || (int)worn.ProtectionLevel >= level)
            {
                continue;
            }

            var given = Piece(bot, pack, worn.GetType(), level);

            if (given != null)
            {
                return given;
            }
        }

        return null;
    }

    /// <summary>A copy of this piece of armour at this level, unless a prize of it as good is held; it replaces a worse one.</summary>
    private static string Piece(BotMobile bot, Container pack, Type type, int level)
    {
        var owned = Owned(bot, type);

        for (var i = 0; i < owned.Count; i++)
        {
            if (owned[i] is BaseArmor held && (int)held.ProtectionLevel >= level)
            {
                return null;
            }
        }

        if (type.CreateInstance<Item>() is not BaseArmor copy)
        {
            return null;
        }

        copy.ProtectionLevel = (ArmorProtectionLevel)level;
        copy.Durability = (ArmorDurabilityLevel)level;

        var said = Give(bot, pack, copy, $"{type.Name} of {ArmourWords[level]}, bound");

        if (said != null)
        {
            Replace(bot, owned);
        }

        return said;
    }

    /// <summary>Invulnerable leather, a piece the caster has not got: the armour meditation still works in.</summary>
    private static string Leather(BotMobile bot, Container pack, int level)
    {
        Type[] set = [typeof(LeatherChest), typeof(LeatherLegs), typeof(LeatherArms), typeof(LeatherGloves), typeof(LeatherGorget), typeof(LeatherCap)];

        for (var i = 0; i < set.Length; i++)
        {
            var given = Piece(bot, pack, set[i], level);

            if (given != null)
            {
                return given;
            }
        }

        return null;
    }

    /// <summary>
    /// A robe of the arcane mind, if the caster has not got one: put on at once, over whatever it wore, which goes into
    /// the pack. It lends <see cref="RobeInt"/> Intelligence while worn (<see cref="Regalia"/>) — this era's items carry
    /// no mana of their own, so the robe's is kept here.
    /// </summary>
    private static string Robe(BotMobile bot, Container pack)
    {
        if (RobeOf(bot) != null)
        {
            return null;
        }

        var robe = new Robe(RobeHue) { Name = "a robe of the arcane mind" };
        var said = Give(bot, pack, robe, $"a robe of the arcane mind (+{RobeInt} Intelligence while worn), bound");

        if (said != null && !robe.Deleted)
        {
            if (bot.FindItemOnLayer(Layer.OuterTorso) is { } over)
            {
                pack.DropItem(over);
            }

            bot.EquipItem(robe);
            Regalia(bot);
        }

        return said;
    }

    /// <summary>The champion's robe this bot holds, worn or carried, or null.</summary>
    private static Robe RobeOf(BotMobile bot)
    {
        if (bot?.Bond == null)
        {
            return null;
        }

        if (bot.FindItemOnLayer(Layer.OuterTorso) is Robe { Hue: RobeHue } worn && BotBinding.IsBound(worn, bot.Bond))
        {
            return worn;
        }

        if (bot.Backpack is not { } pack)
        {
            return null;
        }

        foreach (var item in pack.FindItemsByType(typeof(Robe), true))
        {
            if (item is Robe { Hue: RobeHue } robe && BotBinding.IsBound(robe, bot.Bond))
            {
                return robe;
            }
        }

        return null;
    }

    /// <summary>
    /// A steed of its own, of a kind no stable sells, if it has not got one: a kirin or a unicorn for a caster or healer,
    /// a swamp dragon for a fighter, an ostard for an archer, a ridgeback for anybody else; gold, and named for its
    /// rider. The bought horse in the pack, if there is one, is traded in for its price, into the bank.
    /// </summary>
    private static string Steed(BotMobile bot, Container pack)
    {
        if (bot.Mount is BotChampionSteed || pack.FindItemByType<BotChampionSteed>() != null)
        {
            return null;
        }

        var (regular, mounted, kind) = bot.Class?.Role switch
        {
            BotRole.Caster => (0x25A0, 0x3E9C, "kirin"),
            BotRole.Medic  => (0x25CE, 0x3E9B, "unicorn"),
            BotRole.Melee  => (0x2619, 0x3E98, "swamp dragon"),
            BotRole.Ranged => (0x2135, 0x3EAC, "ostard"),
            _              => (0x2615, 0x3E9A, "ridgeback")
        };

        var steed = new BotChampionSteed
        {
            RegularID = regular,
            MountedID = mounted,
            Hue = SteedHue,
            Name = $"{bot.Name}'s {kind}"
        };

        if (!pack.TryDropItem(bot, steed, false))
        {
            steed.Delete();

            return null;
        }

        if (pack.FindItemByType<BotSteed>() is { } bought && bought is not BotChampionSteed)
        {
            bought.Delete();
            Banker.Deposit(bot, BotSteed.Price);
        }

        return $"a golden {kind} of its own";
    }

    /// <summary>Into the pack and bound, or nothing if the pack will not take it.</summary>
    private static string Give(BotMobile bot, Container pack, Item prize, string said)
    {
        if (!pack.TryDropItem(bot, prize, false))
        {
            prize.Delete();

            return null;
        }

        BotBinding.Bind(prize, bot.Bond);

        return said;
    }

    /// <summary>
    /// The robe's Intelligence, kept to whoever wears one: added while a champion's robe is worn, taken off when it is
    /// not. Asked of every bot with the champion's yellow each minute, and of a robe's owner the moment it is given.
    /// </summary>
    private static void Regalia(BotMobile bot)
    {
        if (bot is not { Deleted: false })
        {
            return;
        }

        var wearing = bot.FindItemOnLayer(Layer.OuterTorso) is Robe { Hue: RobeHue } robe && BotBinding.IsBound(robe, bot.Bond);
        var lent = bot.GetStatMod(RobeMod);

        if (wearing && lent == null)
        {
            bot.AddStatMod(new StatMod(StatType.Int, RobeMod, RobeInt, TimeSpan.Zero));
        }
        else if (!wearing && lent != null)
        {
            bot.RemoveStatMod(RobeMod);
        }
    }

    /// <summary>Every bot's robe, once a minute. See <see cref="Regalia(BotMobile)"/>.</summary>
    private static void Regalia()
    {
        var bots = BotPopulation.Bots;

        for (var i = 0; i < bots.Count; i++)
        {
            if (bots[i] is { Deleted: false } bot && (bot.GetStatMod(RobeMod) != null || RobeOf(bot) != null))
            {
                Regalia(bot);
            }
        }
    }

    /// <summary>Whether this is a weapon or armour of the kind the championship gives: vanquishing or invulnerable.</summary>
    private static bool IsPrize(Item item) =>
        item is BaseWeapon { DamageLevel: not WeaponDamageLevel.Regular } or BaseArmor { ProtectionLevel: not ArmorProtectionLevel.Regular };

    /// <summary>The prizes of this type bound to the bot, worn or carried, the worn one first.</summary>
    private static List<Item> Owned(BotMobile bot, Type type)
    {
        List<Item> owned = [];

        if (bot?.Bond == null || type == null)
        {
            return owned;
        }

        for (var i = 0; i < bot.Items.Count; i++)
        {
            var item = bot.Items[i];

            if (item.GetType() == type && IsPrize(item) && BotBinding.IsBound(item, bot.Bond))
            {
                owned.Add(item);
            }
        }

        if (bot.Backpack is { } pack)
        {
            foreach (var item in pack.FindItemsByType(type, true))
            {
                if (IsPrize(item) && BotBinding.IsBound(item, bot.Bond) && !owned.Contains(item))
                {
                    owned.Add(item);
                }
            }
        }

        return owned;
    }

    /// <summary>The last resort's gold: into the bank, or the pack if the bank will not take it.</summary>
    private static void Pay(BotMobile bot, Container pack, int amount)
    {
        if (!Banker.Deposit(bot, amount))
        {
            pack?.TryDropItem(bot, new Gold(amount), false);
        }
    }

    /// <summary>
    /// Every duplicate prize the population already holds, exchanged once: the first time a championship is crowned
    /// after a boot, one of each kind is kept — the worn one if one is worn — and each copy beyond it is taken out of the
    /// bond, deleted, and replaced by the prize its owner would choose now (<see cref="Prize"/>). Bots at rest too.
    /// </summary>
    private static void Sweep()
    {
        if (_swept)
        {
            return;
        }

        _swept = true;

        var exchanged = Sweep(BotPopulation.Bots) + Sweep(BotPopulation.Away);

        if (exchanged > 0)
        {
            logger.Information("Tourney: {Count} duplicate prizes the population held were exchanged for prizes of their owners' choosing", exchanged);
        }
    }

    private static int Sweep(IReadOnlyList<BotMobile> bots)
    {
        var exchanged = 0;

        for (var i = 0; i < bots.Count; i++)
        {
            if (bots[i] is not { Deleted: false, Bond: { } bond } bot)
            {
                continue;
            }

            List<Type> seen = [];

            foreach (var serial in new List<Serial>(bond.Items))
            {
                if (World.FindItem(serial) is not { Deleted: false } item || !IsPrize(item) || seen.Contains(item.GetType()))
                {
                    continue;
                }

                seen.Add(item.GetType());

                var owned = Owned(bot, item.GetType());

                for (var k = 1; k < owned.Count; k++)
                {
                    bond.Items.Remove(owned[k].Serial);
                    owned[k].Delete();
                    Duplicates++;
                    exchanged++;

                    logger.Information("Tourney: {Name} exchanged a duplicate {Kind} for {Prize}", bot.Name, item.GetType().Name, Prize(bot));
                }
            }
        }

        return exchanged;
    }

    /// <summary>
    /// A tile beside the spot a fighter can be put down on, in sight of the ring's middle; false when none is found. Public
    /// because the venues ask the same question of a town's square before they offer it as a ring (<see cref="BotVenues"/>).
    /// </summary>
    public static bool Spot(Map map, Point3D ring, int x, int y, out Point3D at)
    {
        at = Point3D.Zero;

        if (map == null || map == Map.Internal)
        {
            return false;
        }

        // Wider than a tile either way, and wider again when that is full: the losers stand about the ring and
        // the school drills on the same ground, so by the third round of the first championship (00:25 on
        // 16.09.2026) six of the last seven pairs "could not be put in the ring" and the title went by default.
        for (var attempt = 0; attempt < 32; attempt++)
        {
            var spread = attempt < 16 ? 3 : 7;
            var sx = x + Utility.RandomMinMax(-spread, spread);
            var sy = y + Utility.RandomMinMax(-spread, spread);

            if (!map.CanSpawnMobile(sx, sy, ring.Z - 12, ring.Z + 12, false, false, out var z))
            {
                continue;
            }

            // <b>And in sight of the ring's middle.</b> The drill ground's east side backs onto a building, and a fighter put
            // down behind its wall could not be reached at all: on 26.09.2026, 12 of 17 duels were called by the referee at
            // 150 s with both fighters at full health ("Selwyn could not be reached for 30s"), and the title went to
            // whoever the clock favoured — Hale the brawler, the weakest of 18 by any measure.
            if (!map.LineOfSight(new Point3D(sx, sy, z + 10), new Point3D(ring.X, ring.Y, ring.Z + 10)))
            {
                continue;
            }

            at = new Point3D(sx, sy, z);

            return true;
        }

        return false;
    }

    /// <summary>
    /// Lifts one onto its mark in the ring, tears up its plan, and points it at nothing yet. Both marks are found before
    /// either fighter is moved (<see cref="Bring"/>), so a pair that cannot be set leaves nobody stranded at the ring.
    /// </summary>
    private static bool Put(BotMobile bot, Map map, Point3D at)
    {
        if (bot is not { Deleted: false, Alive: true } || map == null)
        {
            return false;
        }

        // At the arenas every move is a moongate's flash at both ends (07.10.2026, arena27). See Flash.
        if (ArenaOn)
        {
            Flash(bot.Map, bot.Location);
        }

        bot.MoveToWorld(at, map);
        bot.Journey?.Finish();
        bot.Combatant = null;
        bot.Warmode = false;

        if (ArenaOn)
        {
            Flash(map, at);
        }

        return true;
    }

    /// <summary>Whole again: health, stamina, mana, and no poison. The referee's hands before and after every fight.</summary>
    private static void Heal(BotMobile bot)
    {
        if (bot is not { Deleted: false, Alive: true })
        {
            return;
        }

        if (bot.Poisoned)
        {
            bot.CurePoison(bot);
        }

        bot.Hits = bot.HitsMax;
        bot.Stam = bot.StamMax;
        bot.Mana = bot.ManaMax;
    }

    /// <summary>Whoever is in the ring is let go of its brawl.</summary>
    private static void Release()
    {
        if (_a != null && _b != null)
        {
            BotDuel.Call(_a, "the championship was called off");
            BotDuel.End(_a, _b);
        }

        Heal(_a);
        Heal(_b);
        _a = null;
        _b = null;
    }

    private static SkillName Trains(BotMobile bot) => bot?.Bond?.Weapon?.Skill ?? SkillName.Wrestling;

    private static BotMobile Find(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        var bots = BotPopulation.Bots;

        for (var i = 0; i < bots.Count; i++)
        {
            if (bots[i] is { Deleted: false } bot && string.Equals(bot.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                return bot;
            }
        }

        return null;
    }

    public static string Describe()
    {
        var stage = _stage switch
        {
            Stage.Idle     => "no championship running",
            Stage.Calling  => $"calling the next pair of round {_roundNo} at {RingName}",
            Stage.Fighting => $"round {_roundNo} at {RingName}: {_a?.Name} against {_b?.Name} in the ring{(_title ? ", for the title" : "")}",
            Stage.Title    => $"{_challenger?.Name} waits to challenge {Champion} at {RingName}",
            _              => "between fights"
        };

        var away = _away.Count == 0 ? "" : $" (the champion was {string.Join(", ", Reasons())})";
        var title = Champion == null
            ? "the title is vacant"
            : $"{Champion} holds the title since {_reignSince:dd.MM HH:mm} with {_reignDefences} defences";

        if (Arena || ArenaOn)
        {
            return $"the championship at the arenas: {ArenaState()}; {title}; title: {Called} championships called, {TitleBouts} title bouts fought — {Defences} defences, {Changes} changes of hands in the ring, {VacantCrowns} vacant titles filled, {Stripped} champions stripped for not coming{away}, {Walkovers} challengers who did not come; {ArenaTally()}; {BotDuel.Describe()}";
        }

        return $"the championship: {stage}; {title}; title: {Called} championships called, {TitleBouts} title bouts fought — {Defences} defences, {Changes} changes of hands in the ring, {VacantCrowns} vacant titles filled ({VacantByDefault} without a bout), {Stripped} champions stripped for not coming{away}, {Walkovers} challengers who did not come, {Unfought} ended with no title bout; {Held} held, {Fights} fights, {Byes} byes, {Unset} pairs passed over ({Forfeits} forfeited by a bot with a company, {DoubleForfeits} with both still busy, {Deferred} asked again later first), {CalledOff} called by the referee, {Repeats} champions paid in gold for holding every prize and {Duplicates} duplicate prizes exchanged; draws since the boot {string.Join("/", Drawn)} (top first), {TooSoon} calls refused inside the hour, {AtFair} held at a fair; {BotDuel.Describe()}; next by the calendar {(_held + TimeSpan.FromMilliseconds(EveryMs)):yyyy-MM-dd HH:mm}";
    }

    private static IEnumerable<string> Reasons()
    {
        foreach (var (why, n) in _away)
        {
            yield return $"{why} {n}";
        }
    }
}

/// <summary>
/// Keeps the championship's record in the world save: when the last one was held, who holds the title, since when and with how
/// many defences, and the last places the watchers held their events (<see cref="BotVenues"/>).
///
/// <para>
/// <b>A file of its own under <c>Saves/</c> is not kept.</b> The record was <c>Saves/BotTourney/tourney.txt</c>, written at each
/// crown; every world save moves the whole folder into the backups, so each boot found no file, the champion was nobody and the
/// hour began again — five boots between the wipe of 01.10.2026 and build 389, and Kael (crowned 19:21) and Cirdan (09:50) each lost a
/// title nobody had taken from them. A persistence is written into the save itself, as the other records of this shard are.
/// </para>
/// </summary>
public sealed class BotTourneyStore : GenericPersistence
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotTourneyStore));

    private static BotTourneyStore _store;

    /// <summary>From <c>BotDebugCore.Configure</c>, before the world is read.</summary>
    public static void Configure() => _store ??= new BotTourneyStore();

    public BotTourneyStore() : base("BotTourney", 21)
    {
    }

    public override void Serialize(IGenericWriter writer) => BotTourney.Save(writer);

    public override void Deserialize(IGenericReader reader)
    {
        if (!BotTourney.Load(reader))
        {
            logger.Warning("The championship's saved record is of a shape this build does not read; the title starts vacant");

            return;
        }

        logger.Information("The championship's record read back from the save: the champion is {Champion}", BotTourney.Champion ?? "nobody");
    }
}
