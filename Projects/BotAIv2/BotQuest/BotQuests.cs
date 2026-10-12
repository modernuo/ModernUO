using System;
using System.Collections.Generic;
using Server.Items;
using Server.Logging;
using Server.Mobiles;
using Server.Regions;
using Server.Text;

namespace Server.BotAI.V2;

/// <summary>
/// The board of errands: what the watchers and the door ask of the population, for a price.
///
/// <para>
/// <b>Every mechanism the city had was an errand without a board.</b> A bounty on a square, a price on a head, a
/// standing order, a house call: each is "somebody wants this done and pays for it", each with its own list, its
/// own verb and its own taker. Patrick's order of 16.09.2026, evening: one board, with the power to post on it
/// given to the door (Argus and the debuggers) and to the marshal of events among them, paid from the city's
/// treasury; and any bot fit for the errand may take it (<see cref="BotQuester"/>) and is paid on the spot when it
/// is done. The guilds' masters do not post: Patrick took that back the same evening — they are ordinary bots.
/// </para>
///
/// <para>
/// Three kinds to begin with — kill so many of a creature near a place, bring so many of a thing to a place, scout a
/// place — because each maps onto work the population already knows how to do (<see cref="BotSlay"/>, a walk with a
/// pack, a walk). The reward is taken from the treasury the moment the errand is posted and held by the board, so
/// nothing is posted that cannot be paid, and a lapsed or withdrawn errand gives the money back. The engine's own
/// quests (Uzeraan, the Witch's apprentice, the Solen) are not here yet; when they are, they will be a further source
/// of errands on this board, not a second board.
/// </para>
///
/// <para>
/// Kept across restarts by <see cref="BotQuestStore"/>; a taker is not kept, so a restart puts every errand back on the
/// board with its count. "An offer is not an errand": an errand is taken in the deed's <c>Taken</c>, never in the
/// proposer, and a taken errand with no progress in <see cref="HoldMs"/> goes back on the board.
/// </para>
/// </summary>
public static class BotQuests
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotQuests));

    /// <summary>Whether the board takes errands at all.</summary>
    public static bool Running { get; set; } = true;

    /// <summary>Most errands on the board at once.</summary>
    public static int MostOpen { get; set; } = 12;

    /// <summary>How long an errand stands untaken before it lapses and its reward goes back. Two hours.</summary>
    public static int LapseMs { get; set; } = 7200000;

    /// <summary>How long a taken errand may go without progress before it goes back on the board. Twenty minutes.</summary>
    public static int HoldMs { get; set; } = 1200000;

    /// <summary>The most an errand may ask for: kills or things.</summary>
    public static int MostAmount { get; set; } = 200;

    /// <summary>The most an errand may pay.</summary>
    public static int MostReward { get; set; } = 20000;

    /// <summary>
    /// A gathering errand pays at most this many times what one of its things fetches on the market (<c>BotMarket.Worth</c>),
    /// a piece; a larger reward is brought down to it, and the answer says so. Nought for no limit.
    ///
    /// <para>
    /// <b>A delivery is done first by whoever already carries the thing, and the treasury paid it as a windfall.</b> Hermes
    /// posted "bring 5 Leather to (1439, 1469), 1000gp" at 18:16:51 on 17.09.2026 and the same again at 18:27:04 — two
    /// hundred a piece for a hide that sells for two to five — after errands for Bloodmoss at twenty a piece had each been
    /// taken and finished within two minutes by a bot with it in its pack. The treasury cannot spend faster than it mints,
    /// so this is not money made from nothing; it is the city's gold handed to one bot for walking home. Ten times the
    /// market still pays a gatherer to go and get it.
    /// </para>
    /// </summary>
    public static int GatherTimesWorth { get; set; } = 10;

    /// <summary>The least a gathering errand may pay a piece however cheap the thing, so that a walk is still worth it.</summary>
    public static int GatherLeastPerPiece { get; set; } = 20;

    /// <summary>Gathering errands whose reward was brought down to <see cref="GatherTimesWorth"/>.</summary>
    public static long Capped { get; private set; }

    /// <summary>
    /// How far from the errand's own place its taker looks for the creature, once it is within half of this of the place. A
    /// kill is credited wherever the creature the taker set on died.
    /// </summary>
    public static int KillRange { get; set; } = 120;

    /// <summary>
    /// How long an errand is not offered again to the bot that has just let it go.
    ///
    /// <para>
    /// <b>The same bot took it back in the same second.</b> At 00:13:27 on build 99 Calla Ashdown let Hermes's errand #4 go
    /// ("saw no Ogre within 120 tiles of (1200, 1500) for 3 minutes") and took it again at once, the only errand on the
    /// board and still the best-paid thing in reach; the second go was stopped by the watch a minute and a half later
    /// ("it had stopped getting anywhere"), and the board counted two takers of three out of one bot. A failure offered
    /// back unchanged (C1). Ten minutes, and to that bot only: another taker standing elsewhere is a new try.
    /// </para>
    /// </summary>
    public static int RetakeMs { get; set; } = 600000;

    /// <summary>
    /// How far round itself the taker of a kill "anywhere" looks for the creature on its way to a lair, and how far from the
    /// lair once it is there. Anywhere is resolved to the nearest place a spawner on the island keeps the creature (see
    /// <see cref="BotLairs"/>); on 16.09.2026 it was three hundred tiles round wherever the taker stood, and the taker stood.
    /// </summary>
    public static int LookRange { get; set; } = 40;

    /// <summary>
    /// How long a taker stands at the errand's place, or at a lair, seeing nothing of the creature before it gives the place
    /// up. Three minutes: under the stall watch's four, so the giving up is the errand's own and says why.
    /// </summary>
    public static int EmptyMs { get; set; } = 180000;

    /// <summary>How many lairs one taker of a kill anywhere tries, nearest first, before it lets the errand go.</summary>
    public static int MostLairs { get; set; } = 3;

    /// <summary>
    /// How many takers may let an errand go without a thing done for it before the board takes it down and gives the reward
    /// back. Three: an errand nobody could get to, or nobody would see through, is a statement about the errand, and a board
    /// that goes on offering it is the loop of a failure offered unchanged (DECISIONS C1) spread across the population.
    /// </summary>
    public static int MostLetGo { get; set; } = 3;

    /// <summary>How near the delivery place the goods are handed over.</summary>
    public static int DeliverReach { get; set; } = 4;

    public static long Posted { get; private set; }

    public static long RefusedPosts { get; private set; }

    /// <summary>Of those, errands refused because one of the same kind and thing was already on the board.</summary>
    public static long Duplicates { get; private set; }

    public static long Taken { get; private set; }

    public static long Released { get; private set; }

    public static long Finished { get; private set; }

    public static long Lapsed { get; private set; }

    public static long Withdrawn { get; private set; }

    /// <summary>Errands taken down after <see cref="MostLetGo"/> takers let them go with nothing done.</summary>
    public static long Abandoned { get; private set; }

    /// <summary>Gold paid to takers, all told.</summary>
    public static long Paid { get; private set; }

    private static readonly List<BotQuest> _quests = [];

    private static int _nextId = 1;

    private static Clock _timer;

    public static IReadOnlyList<BotQuest> All => _quests;

    public static void Start()
    {
        Stop();
        _timer = new Clock();
        _timer.Start();
    }

    public static void Stop()
    {
        _timer?.Stop();
        _timer = null;
    }

    /// <summary>An errand posted by its kind's name, as the door and the marshal say it: kill, gather or scout.</summary>
    public static string PostByName(string kind, string what, int amount, int reward, Map map, int x, int y, string by)
    {
        var known = (kind ?? "").Trim().ToLowerInvariant() switch
        {
            "kill" => BotQuestKind.Kill,
            "gather" or "bring" => BotQuestKind.Gather,
            "scout" or "look" => BotQuestKind.Scout,
            _ => (BotQuestKind?)null
        };

        if (known == null)
        {
            RefusedPosts++;

            return $"the board takes errands to kill, gather or scout; \"{kind}\" is none of them.";
        }

        return Post(known.Value, what, amount, reward, map, x, y, by);
    }

    /// <summary>
    /// An errand posted. The reward is taken from the treasury now and held; the answer says what was posted or why
    /// nothing was. A place of (-1, -1) means none: anywhere for a kill, the population's home for a delivery.
    /// </summary>
    public static string Post(BotQuestKind kind, string what, int amount, int reward, Map map, int x, int y, string by)
    {
        if (!Running)
        {
            RefusedPosts++;

            return "the board is closed.";
        }

        if (map == null || map == Map.Internal)
        {
            RefusedPosts++;

            return "the board has no map to post on.";
        }

        if (Open() >= MostOpen)
        {
            RefusedPosts++;

            return $"the board holds at most {MostOpen} errands; wait for one to be done or withdraw one.";
        }

        if (reward <= 0 || reward > MostReward)
        {
            RefusedPosts++;

            return $"an errand pays between 1 and {MostReward}gp.";
        }

        Type type = null;
        var capped = "";

        if (kind != BotQuestKind.Scout)
        {
            if (amount <= 0 || amount > MostAmount)
            {
                RefusedPosts++;

                return $"an errand asks for between 1 and {MostAmount}.";
            }

            type = string.IsNullOrWhiteSpace(what) ? null : AssemblyHandler.FindTypeByName(what.Trim());

            if (kind == BotQuestKind.Kill && (type == null || !typeof(BaseCreature).IsAssignableFrom(type)))
            {
                RefusedPosts++;

                return $"the board knows no creature called {what}.";
            }

            if (kind == BotQuestKind.Gather && (type == null || !typeof(Item).IsAssignableFrom(type)))
            {
                RefusedPosts++;

                return $"the board knows no thing called {what}.";
            }

            // <b>The same errand twice is the treasury paying twice for one want.</b> Hermes posted "kill 3 Ogre anywhere, 2000gp"
            // at 21:01:16 on 17.09.2026 and again at 21:35:17, while the first stood two ogres down in Oswin's hands — and said
            // so in its own reasoning. Refused while one of the same kind and thing is on the board, taken or not; the answer
            // names it, so the marshal reads what stands.
            for (var i = 0; i < _quests.Count; i++)
            {
                if (_quests[i].Kind == kind && _quests[i].Type == type)
                {
                    RefusedPosts++;
                    Duplicates++;

                    return $"that errand is already on the board: {_quests[i].Tell()}; wait for it to be done.";
                }
            }

            // See GatherTimesWorth.
            if (kind == BotQuestKind.Gather && GatherTimesWorth > 0)
            {
                var piece = Math.Max(GatherLeastPerPiece, BotMarket.Worth(type, 1) * GatherTimesWorth);
                var most = (int)Math.Min(MostReward, (long)piece * amount);

                if (reward > most)
                {
                    capped = $" (brought down from {reward}gp: a gathering errand pays at most {piece}gp a {type.Name})";
                    reward = most;
                    Capped++;
                }
            }
        }

        var where = Point3D.Zero;
        var lairs = 0;

        if (x >= 0 && y >= 0)
        {
            if (!Place(map, x, y, out where))
            {
                RefusedPosts++;

                return $"nothing can stand within {PlaceWithin} tiles of ({x}, {y}).";
            }

            // A scout is paid for standing at the place, so a place inside the walls is gold for a walk across town. See
            // ScoutWithin.
            if (kind == BotQuestKind.Scout && Region.Find(where, map)?.IsPartOf<TownRegion>() == true)
            {
                RefusedPosts++;

                return $"a scout is sent to ground outside the walls, and ({x}, {y}) is inside them. Leave the place out, and the board sends it to the nearest ground nobody has counted for {BotQuad.StaleMs / 3600000} hours.";
            }

            // <b>A place the creature never comes near is an errand nobody can do.</b> A kill near a place is looked for within
            // KillRange of it and given up after EmptyMs, three takers over; Hermes posted "kill 3 Ogre near (1200, 1500)" at
            // 00:07 on 17.09.2026, where the nearest ogre spawner stands 140 tiles off, and Calla Ashdown stood looking for
            // three minutes and let it go; then "kill 5 Ogre near (1420, 1510)", in Britain, at 09:17. Asked of the spawners,
            // which the island keeps whether or not one is alive this minute, and told back with the nearest one.
            if (kind == BotQuestKind.Kill && type != null && BotLairs.Count(map, type) > 0)
            {
                var lair = BotLairs.Closest(map, type, where, out var reach);

                if (lair != Point3D.Zero && reach > KillRange)
                {
                    RefusedPosts++;

                    return $"no spawner keeps a {type.Name} within {KillRange} tiles of ({x}, {y}); the nearest keeps them round ({lair.X}, {lair.Y}), {Math.Max(Math.Abs(lair.X - x), Math.Abs(lair.Y - y))} tiles off. Post it there, or anywhere.";
                }
            }
        }
        else if (kind == BotQuestKind.Kill)
        {
            // Anywhere is where the creature is kept. A creature no spawner on the island keeps is one no taker can find by
            // walking, and posting it would only hold the treasury's money for two hours.
            lairs = BotLairs.Count(map, type);

            if (lairs == 0)
            {
                RefusedPosts++;

                return $"no spawner on the island keeps a {type.Name}, so \"anywhere\" is nowhere a bot can walk to; give the errand a place.";
            }
        }
        else if (kind == BotQuestKind.Scout)
        {
            where = BotQuad.Stalest(map, BotPopulation.Where, ScoutWithin, null);

            if (where == Point3D.Zero)
            {
                RefusedPosts++;

                return $"no ground within {ScoutWithin} tiles of home has gone uncounted for {BotQuad.StaleMs / 3600000} hours, so there is nothing to scout.";
            }
        }
        else
        {
            var home = BotPopulation.Where;

            if (!Place(map, home.X, home.Y, out where))
            {
                RefusedPosts++;

                return "the errand needs a place, and nobody can stand at the population's home.";
            }
        }

        if (!BotCity.Escrow(reward))
        {
            RefusedPosts++;

            return $"the treasury holds {BotCity.Purse}gp and cannot hold {reward}gp for an errand.";
        }

        var quest = new BotQuest
        {
            Id = _nextId++,
            Kind = kind,
            What = type?.Name,
            Type = type,
            Amount = kind == BotQuestKind.Scout ? 1 : amount,
            Map = map,
            Where = where,
            Held = reward,
            By = by ?? "somebody",
            PostedTick = Core.TickCount
        };

        _quests.Add(quest);
        Posted++;

        logger.Information("{By} posted an errand on the board: {Errand}; {Purse}gp left in the treasury", quest.By, quest.Tell(), BotCity.Purse);

        return lairs > 0
            ? $"posted {quest.Tell()}, to be found at the nearest of {lairs} places on the island that keep it; {BotCity.Purse}gp left in the treasury."
            : $"posted {quest.Tell()}{capped}; {BotCity.Purse}gp left in the treasury.";
    }

    /// <summary>
    /// How far from the tile an errand names the board looks for one a bot can stand on.
    ///
    /// <para>
    /// <b>A place named by a model, or by a hand at the door, is a place and not a tile.</b> The board used to settle the
    /// very tile and refuse the errand when nothing stood there: Hermes asked for errands at (1440, 1470) at 04:30 and
    /// 10:55 on 17.09.2026 — the population's home, the one place it is told the coordinates of — and both were refused
    /// "nothing can stand at (1440, 1470)". The same tile is the board's own fallback for a delivery given no place, so
    /// every such errand would have been refused too.
    /// </para>
    /// </summary>
    public static int PlaceWithin { get; set; } = 8;

    /// <summary>
    /// How far from home a scout given no place is sent: to the nearest square outside the walls nobody has counted for
    /// <c>BotQuad.StaleMs</c>, as the scoutmaster sends its own.
    ///
    /// <para>
    /// <b>A scout is paid for standing at its place, and the marshal's places were the middle of Britain.</b> On 17.09.2026
    /// Hermes posted "scout (1403, 1495), 500gp" at 11:25 and "scout (1439, 1469), 100gp" at 11:49 — the population's home,
    /// the one place its prompt gives coordinates for — and a scout finishes the moment a bot stands within two tiles:
    /// 600gp of the treasury for two walks across town, and nothing counted. The board now refuses a scout inside the walls
    /// and gives one with no place the stalest ground near home.
    /// </para>
    /// </summary>
    public static int ScoutWithin { get; set; } = 400;

    /// <summary>The nearest tile to (x, y), ring by ring out to <see cref="PlaceWithin"/>, that a bot can stand on.</summary>
    private static bool Place(Map map, int x, int y, out Point3D where)
    {
        for (var ring = 0; ring <= PlaceWithin; ring++)
        {
            for (var dx = -ring; dx <= ring; dx++)
            {
                for (var dy = -ring; dy <= ring; dy++)
                {
                    if (Math.Max(Math.Abs(dx), Math.Abs(dy)) != ring || !BotStep.Settle(map, x + dx, y + dy, out var z))
                    {
                        continue;
                    }

                    where = new Point3D(x + dx, y + dy, z);

                    return true;
                }
            }
        }

        where = Point3D.Zero;

        return false;
    }

    /// <summary>An errand withdrawn by whoever can see the board; its reward goes back to the treasury.</summary>
    public static string Unpost(int id, string by)
    {
        var quest = Find(id);

        if (quest == null)
        {
            return $"there is no errand #{id} on the board.";
        }

        if (!quest.Open)
        {
            Release(quest, $"withdrawn by {by}");
        }

        Refund(quest);
        _quests.Remove(quest);
        Withdrawn++;

        logger.Information("{By} withdrew the errand {Errand}; its {Held}gp went back to the treasury", by, quest.Tell(), quest.Held);

        return $"withdrew {quest.Tell()}; {quest.Held}gp back in the treasury.";
    }

    public static BotQuest Find(int id)
    {
        for (var i = 0; i < _quests.Count; i++)
        {
            if (_quests[i].Id == id)
            {
                return _quests[i];
            }
        }

        return null;
    }

    /// <summary>Whether this errand is still on the board.</summary>
    public static bool Standing(BotQuest quest) => quest != null && _quests.Contains(quest);

    /// <summary>The errand taken by this bot, from the deed's <c>Taken</c>. False if somebody else has it or it is gone.</summary>
    internal static bool Take(BotQuest quest, BotMobile bot)
    {
        if (quest == null || bot == null || !Standing(quest) || !quest.Open)
        {
            return false;
        }

        quest.Taker = bot;
        quest.TakenTick = Core.TickCount;
        Taken++;

        logger.Information("{Name} took the errand {Errand}", bot.Name, quest.Tell());

        return true;
    }

    /// <summary>
    /// The errand back on the board, its count kept. A taker that let it go with nothing done for it counts towards
    /// <see cref="MostLetGo"/>, and the last such letting go takes the errand down.
    /// </summary>
    internal static void Release(BotQuest quest, string why, bool empty = false)
    {
        if (quest == null || quest.Taker == null)
        {
            return;
        }

        var was = quest.Taker;

        quest.Taker = null;
        quest.LastTaker = was;
        quest.LetGoTick = Core.TickCount;
        Released++;

        if (!empty)
        {
            logger.Information("{Name} let the errand {Errand} go: {Why}; it is back on the board", was.Name, quest.Tell(), why);

            return;
        }

        quest.LetGo++;
        quest.LetGoBecause = why;

        if (quest.LetGo < MostLetGo || !Standing(quest))
        {
            logger.Information(
                "{Name} let the errand {Errand} go with nothing done for it, {LetGo} of {Most} takers so far: {Why}; it is back on the board",
                was.Name,
                quest.Tell(),
                quest.LetGo,
                MostLetGo,
                why
            );

            return;
        }

        Refund(quest);
        _quests.Remove(quest);
        Abandoned++;

        logger.Information(
            "The errand {Errand} came down after {LetGo} takers let it go with nothing done for it, the last of them {Name}: {Why}; its {Held}gp went back to the treasury",
            quest.Tell(),
            quest.LetGo,
            was.Name,
            why,
            quest.Held
        );
    }

    /// <summary>
    /// The taker has come back to the errand after putting it down for something that would not wait: the time it was put
    /// down is not time without progress. See <see cref="BotDeed.Steadfast"/>.
    /// </summary>
    internal static void Touch(BotQuest quest, Mobile taker)
    {
        if (quest != null && taker != null && ReferenceEquals(quest.Taker, taker))
        {
            quest.TakenTick = Core.TickCount;
        }
    }

    /// <summary>Progress on the errand, which also restarts its hold clock.</summary>
    internal static void Progress(BotQuest quest, int done)
    {
        if (quest == null || done <= 0)
        {
            return;
        }

        quest.Done += done;
        quest.TakenTick = Core.TickCount;
    }

    /// <summary>The errand done: the taker is paid what the board held, and the errand comes down.</summary>
    internal static void Finish(BotQuest quest, BotMobile bot)
    {
        if (quest == null || !Standing(quest))
        {
            return;
        }

        var held = quest.Held;

        // Told before the reward leaves it: "finished the errand #1: scout (1875, 855), 0gp" read as an errand that paid
        // nothing, on the very line that says it paid a hundred (build 94, 21:12:46).
        var told = quest.Tell();

        if (bot is { Deleted: false } && held > 0)
        {
            var pack = bot.Backpack;

            if (pack != null)
            {
                pack.DropItem(new Gold(held));
            }
            else
            {
                Banker.Deposit(bot, held);
            }

            Paid += held;
            BotCity.Disbursed(held);
        }

        quest.Held = 0;
        _quests.Remove(quest);
        Finished++;

        logger.Information("{Name} finished the errand {Errand} and was paid {Gold}gp", bot?.Name ?? "nobody", told, held);
    }

    private static void Refund(BotQuest quest)
    {
        if (quest.Held > 0)
        {
            BotCity.Refund(quest.Held);
        }
    }

    private static int Open()
    {
        var open = 0;

        for (var i = 0; i < _quests.Count; i++)
        {
            if (_quests[i].Open)
            {
                open++;
            }
        }

        return open;
    }

    private static void Lapse()
    {
        var now = Core.TickCount;

        for (var i = _quests.Count - 1; i >= 0; i--)
        {
            var quest = _quests[i];

            if (quest.Open)
            {
                if (now - quest.PostedTick < LapseMs)
                {
                    continue;
                }

                Refund(quest);
                _quests.RemoveAt(i);
                Lapsed++;

                logger.Information("The errand {Errand} lapsed after {Hours} hours untaken; its {Held}gp went back to the treasury", quest.Tell(), LapseMs / 3600000, quest.Held);

                continue;
            }

            if (quest.Taker is not { Deleted: false, Alive: true })
            {
                Release(quest, "the taker is dead or gone");

                continue;
            }

            if (now - quest.TakenTick >= HoldMs)
            {
                Release(quest, $"no progress in {HoldMs / 60000} minutes");
            }
        }
    }

    /// <summary>The board, for the door and the prompts.</summary>
    public static string Tell()
    {
        if (_quests.Count == 0)
        {
            return "nothing on the board";
        }

        using var say = ValueStringBuilder.Create(512);

        for (var i = 0; i < _quests.Count; i++)
        {
            if (i > 0)
            {
                say.Append("; ");
            }

            say.Append(_quests[i].Tell());
        }

        return say.ToString();
    }

    public static string Describe() =>
        !Running
            ? "the board is closed"
            : $"{Posted} errands posted ({RefusedPosts} refused, {Duplicates} of them for standing on the board already), {Taken} taken, {Released} let go, {Finished} finished for {Paid}gp, {Lapsed} lapsed, {Withdrawn} withdrawn, {Abandoned} taken down after {MostLetGo} takers did nothing for them; {BotQuester.Describe()}; lairs: {BotLairs.Describe()}; on the board: {Tell()}";

    /// <summary>The board, for the store: each errand without its taker.</summary>
    internal static void Save(IGenericWriter writer)
    {
        var now = Core.TickCount;

        writer.WriteEncodedInt(_nextId);
        writer.WriteEncodedInt(_quests.Count);

        for (var i = 0; i < _quests.Count; i++)
        {
            var quest = _quests[i];

            writer.WriteEncodedInt(quest.Id);
            writer.WriteEncodedInt((int)quest.Kind);
            writer.Write(quest.What ?? "");
            writer.WriteEncodedInt(quest.Amount);
            writer.WriteEncodedInt(quest.Done);
            writer.Write(quest.Map);
            writer.Write(quest.Where);
            writer.WriteEncodedInt(quest.Held);
            writer.Write(quest.By ?? "");
            writer.Write(Math.Max(0L, now - quest.PostedTick));
        }
    }

    /// <summary>The board read back, every errand open again. Returns how many.</summary>
    internal static int Load(IGenericReader reader)
    {
        var now = Core.TickCount;

        _quests.Clear();
        _nextId = Math.Max(1, reader.ReadEncodedInt());

        var count = reader.ReadEncodedInt();

        for (var i = 0; i < count; i++)
        {
            var id = reader.ReadEncodedInt();
            var kind = (BotQuestKind)reader.ReadEncodedInt();
            var what = reader.ReadString();
            var amount = reader.ReadEncodedInt();
            var done = reader.ReadEncodedInt();
            var map = reader.ReadMap();
            var where = reader.ReadPoint3D();
            var held = reader.ReadEncodedInt();
            var by = reader.ReadString();
            var age = reader.ReadLong();
            var type = string.IsNullOrEmpty(what) ? null : AssemblyHandler.FindTypeByName(what);

            if (map == null || map == Map.Internal || held <= 0 || (kind != BotQuestKind.Scout && type == null))
            {
                continue;
            }

            _quests.Add(
                new BotQuest
                {
                    Id = id,
                    Kind = kind,
                    What = type?.Name,
                    Type = type,
                    Amount = Math.Max(1, amount),
                    Done = Math.Max(0, done),
                    Map = map,
                    Where = where,
                    Held = held,
                    By = string.IsNullOrEmpty(by) ? "somebody" : by,
                    PostedTick = now - Math.Max(0L, age)
                }
            );

            BotCity.Held(held);
        }

        return _quests.Count;
    }

    /// <summary>The board cleared and every held reward returned: a reset, or a reload.</summary>
    public static void Wipe()
    {
        for (var i = 0; i < _quests.Count; i++)
        {
            Refund(_quests[i]);
        }

        _quests.Clear();
    }

    public static void Forget()
    {
        Posted = 0;
        RefusedPosts = 0;
        Duplicates = 0;
        Taken = 0;
        Released = 0;
        Finished = 0;
        Lapsed = 0;
        Withdrawn = 0;
        Abandoned = 0;
        Paid = 0;
    }

    private sealed class Clock : Timer
    {
        public Clock() : base(TimeSpan.FromSeconds(60), TimeSpan.FromSeconds(60))
        {
        }

        protected override void OnTick()
        {
            if (Running)
            {
                Lapse();
            }
        }
    }
}
