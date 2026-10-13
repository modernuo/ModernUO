using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using Server.BotAI.V2;
using Server.Items;
using Server.Logging;
using Server.Mobiles;
using Server.Multis;
using Server.Network;
using Server.Text;

namespace Server.BotAI.Mind;

/// <summary>
/// The debugger's hands: the handful of things a person with an administrator's account would type at a
/// stuck shard, made available to Argus by name, bounded, and written down every single time.
///
/// <para>
/// <b>Why a set of our own rather than the engine's command system.</b> Almost every GM command in ModernUO
/// ends in a target cursor — <c>[props</c>, <c>[tele</c>, <c>[set</c> all wait for a click — and Argus has
/// no client to click with. Handing him <c>CommandSystem.Handle</c> would hand him a door that opens onto a
/// prompt nobody can answer. So each verb here does what the command of that name does, addressed by the one
/// thing he can name reliably: a bot off the roster.
/// </para>
///
/// <para>
/// <b>Six of the ten verbs only look, and the proportion is on purpose.</b> A watcher's first duty is to find
/// out, and this project's own record says the watcher is wrong far more often than the shard is — ten false
/// alarms against two real defects in the first day of it. Verbs that change the world are for the cases
/// where looking has already produced the answer: a bot in a pocket, a bot dead in a field, a creature
/// nothing can reach.
/// </para>
///
/// <para>
/// <b>What is absent is not an oversight.</b> Nothing here deletes, nothing sets a property, nothing touches
/// an account or an access level, and nothing acts on a mobile that is not one of ours. The world save holds
/// a real person's character; a model that can be talked round by its own previous sentence must not be able
/// to reach it. The engine's own guards sit underneath as well — a bot is only ever put down on ground the
/// engine agrees a body fits on.
/// </para>
///
/// <para>
/// <b>Every use goes in its own file.</b> <c>logs/bot-debugger-commands.log</c>, beside the log the polls and
/// the conclusions go in, because those are read forwards for what the shard is like and this is read
/// backwards from "why did that bot move" — and a hand whose uses are mixed in with its own observations is
/// a hand that can quietly alter what it is observing without the record showing it.
/// </para>
/// </summary>
public static class BotHand
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotHand));

    /// <summary>
    /// The verbs, and <c>none</c> heads them for the same reason <c>nothing</c> heads the kinds of finding:
    /// a hand with no way to stay in its pocket is a hand that is used every time it is offered.
    /// </summary>
    public static readonly string[] Verbs =
    [
        "none",
        "props",
        "sight",
        "where",
        "pack",
        "tile",
        "tele",
        "home",
        "res",
        "free",
        "shun",
        "camp",
        "summon",
        "call",
        "near",
        "resolve",
        "roles",
        "pays",
        "gaps",
        "city",
        "post",
        "unpost",
        "quests",
        "reach"
    ];

    /// <summary>One clause each, for the prompt. Kept short because the prompt has a hard ceiling.</summary>
    public const string Manual =
        "none — do nothing, and it is the right answer most minutes. props <bot> — the engine's own view of it. "
        + "sight <bot> — whether it can see and lawfully strike what it is fighting, and from how far. "
        + "where <bot> — its tile, its region, and who is standing on top of it. pack <bot> — what it is "
        + "carrying, what it is allowed to keep of each, and what is surplus. tile <x> <y> — whether a body "
        + "fits there and what is on it. tele <bot> <x> <y> — lift it onto that tile, for a pocket it cannot "
        + "walk out of. home <bot> — lift it back to where the population lives. res <bot> — raise it if it is "
        + "dead. free <bot> — make it forget its plan and choose again. shun <bot> — leave whatever it is "
        + "fighting alone for a while, for something nothing can reach. camp [<x> <y>] — put an orc camp on the "
        + "ground, here or there, to see whether the population loots it and frees its prisoner. summon <bot> "
        + "— bring one to where I stand, to watch it work. call — bring Patrick to me, when he is on the shard "
        + "and there is something he should see with his own eyes. near <x> <y> — everything alive standing "
        + "around that spot, with its name, health and whether it would fight us. resolve <bot> — what it is "
        + "holding, how far into its own reckoning, whether a better offer could take it off that now and why "
        + "not, what it put down to come back to, and how its last few pieces of work ended. roles — how each "
        + "class spends its working minutes: its own trade, anybody's work, another class's trade. pays [<trade>] — what "
        + "the population has found each kind of work pays, and where, best first: the board the auction reads for a bot "
        + "that has never been somewhere; kept across restarts. gaps — each trade's claim against what it turned out to "
        + "pay, worst overstatement first: a claim far above the payment is a number in the source that stopped being "
        + "true, with the whole population chasing it. city — the treasury: what it holds, what it has spent, its "
        + "standing orders. city buy [<lots>] — the city buys that many whole lots at the bots' shops open by the bank, "
        + "for what is in the treasury and not a coin more: the outside demand that clears what the population "
        + "cannot sell to itself. city want <Thing> <amount> <price> — a standing order: the city takes that thing at "
        + "the bots' shops whenever it is on show at or under the price, until the amount is bought. city forget <Thing> — "
        + "the order withdrawn. city bounty <x> <y> <gp> — a price on a square: the Baron marches there before "
        + "anywhere else, and the company that clears it is paid from the treasury, split evenly. city head <bot> <gp> — "
        + "a price on a murderer's head, paid to whoever catches it. city fair [<minutes>] [<percent>] — a fair: for an "
        + "hour the city takes what the bots' shops by the bank hold at 80% of the asking price, twenty lots a minute, out of the treasury; "
        + "the clock declares one every six hours when the treasury allows. The treasury fills on a clock and never faster; "
        + "spend it where the market is stuck or the ground is bad. "
        + "post kill <Creature> <n> <gp> [<x> <y>] / post gather <Thing> <n> <gp> [<x> <y>] / post scout <x> <y> <gp> — "
        + "an errand on the board, its reward held by the treasury from now and paid to whichever bot does it; a kill "
        + "counts near the place named or anywhere, goods are brought to the place or to home. unpost <id> — the errand "
        + "withdrawn and its reward back. quests — the board and what came of it. reach <x> <y> — what the reach map says of "
        + "a tile (open to a walker from home, an isle only a ship reaches, a sealed pocket, water a ship sails, nowhere to "
        + "stand) and what the walker's own search finds there; reach — the map, and how its check of the borders came out.";

    /// <summary>
    /// Verbs the console may use and the model may not. <see cref="Run"/> accepts one of these when, and
    /// only when, it is told the order came in by hand.
    ///
    /// <para>
    /// <b>The model's choice is bounded by <see cref="Verbs"/> — <c>BotDebugNote</c> writes that array into
    /// its schema as an enumeration — so a verb left out of it is a verb no mind can reach.</b> That is
    /// where anything permanent belongs: razing the halls destroys buildings that outlive the whole
    /// population, and an idle thought at three in the morning is not a good enough reason for it.
    /// </para>
    /// </summary>
    public static readonly string[] HandVerbs = ["halls", "raze", "revel", "wars", "guilds", "seats", "seat", "save", "road", "roads", "peril", "resolves", "jam", "breaks", "trip", "arm", "arms", "rearm", "census", "tourney", "arena", "band", "reset", "forgive", "prove", "proof", "proofs", "awake", "chart", "nav", "navcells", "navbench", "go", "gates", "parley", "meet", "zones", "zone", "zonewatch", "charters", "charter", "relics", "relic", "steward", "decree", "officers", "appoint"];

    /// <summary>One clause each for the two above, for whoever is holding the keyboard.</summary>
    public const string ByHand =
        "halls — what the guilds own and where it stands. raze — take every guild hall off the island, "
        + "which is how an evening's building is undone. revel <trade> [<prize>] [<x> <y>] — declare one "
        + "this second instead of waiting a quarter of an hour for the watcher to think of it; naming a spot "
        + "raises a camp there, pulled into the ring around the population if it is too near or too far. "
        + "wars — every war standing, with its score and its clock. "
        + "rearm — hand every bot a catch left bare the weapon it was born to back; asking twice hands nothing twice. "
        + "parley — the guilds' meetings standing and the last few ended, the agreements between guilds, and each guild's "
        + "standing in its towns with the task in hand. meet <guild> ; <guild or town> [; grievance|safety|friendship|commerce] — "
        + "call a meeting now, the pair's clock ignored: the envoy walks, the host answers, a watcher witnesses, the duke gives "
        + "his word; a town's name sends the guild's envoy to that town's hall for a task. "
        + "relics — every relic and banner, who holds it, where it is and since when, the lair standing and the counters. "
        + "relic <name> ; <guild> — hand a relic to a guild's hall now (a key such as crown or banner:The Hammer, or a piece of its "
        + "name; hide instead of a guild puts a town relic back into hiding), to watch its gifts, a theft or a war's end. "
        + "guilds — every guild in a paragraph: who leads it, how many it has in the world, at rest and dead, what they fight "
        + "with (strength, armour, the skills a fight turns on, bandages and potions), what it owns, and the war it stands in "
        + "with how many of it are in the fight against how many the score asks for. seats — where each guild lives and how far "
        + "its hall is from it. seat <guild> <x> <y> — move a guild's seat; its hall is carried there and its "
        + "members are born and rise there from then on. save — write the world to disk now, before the shard is "
        + "stopped: a kill without one rolls the island back to the last autosave, five minutes of halls and moves. "
        + "census — one line per bot into logs/bot-census.log: class, band, work in hand, health, purse, place, company. "
        + "tourney [stop|state] — hold the championship now instead of waiting for the week: the strongest thirty, one against "
        + "one in the ring, the winner's name yellow and a prize in its pack; stop calls it off, state says where it stands; at the "
        + "arenas (BotTourney.Arena) it is the same as arena. arena [force|state|stop] — the day's championship at the round arenas "
        + "east of Papua now, if today's has not been held: at least fifty free bots, ported in batches, one pair to a ring; force "
        + "holds one anyway for a test without spending the day's and pays no prize; state tells the bracket; stop calls it off and "
        + "sends everybody home. "
        + "prove <bot> [<Creature>] — put the bot's double on the proving ground in Green Acres next, against the creature named "
        + "or the rung of the dungeons' ladder it is due; proof <bot> — every fight its doubles have had, what each proved, and "
        + "its strength against each dungeon's worst; proofs — every bot measured, strongest first, and the ladder. "
        + "reset — the population back to novices: halls razed, claims and hand-set seats let go, wars and opinions forgotten, "
        + "everybody's learning wiped, the world saved; restart after it. "
        + "forgive <kind> — strike out what the population has learned about one kind of work (its patches and its "
        + "correction), so the next bot asked judges it on its promise; for a memory that was true of the doer, not the work. "
        + "road <x1> <y1> <x2> <y2> — ask the pathfinder for a way between two tiles with a generous clock, and say "
        + "what it found: the answer to \"why can nobody get there\". peril <x> <y> — what the death map, which learns "
        + "from the boot, and the quadrant record, which survives a restart, hold about one place, and whether work may "
        + "go there. resolves — how much of what the population "
        + "takes on it sees through, what takes it off the rest, and the same per trade. jam <bot> — bank the coin in its "
        + "pack and fill the pack to the engine's cap with oil cloths that weigh next to nothing, the state Hale was found "
        + "in on 14.09.2026, to watch the bot refused work at a counter and offered the trip that makes room. breaks — "
        + "which bots have a kind of work resting after failing it the same way too often, for how long and after what. "
        + "trip <bot> <kind> — put in as many failures of that work for that bot, for one reason, as trip the breaker, "
        + "to watch the rest and the refusals it causes; the rest lapses on its own clock. arm <bot> — put a vanquishing "
        + "copy of the weapon the bot was born with in its pack, to watch the re-arm wield the better one and put the bound "
        + "one away; the copy is not bound and goes with the bot at the next restart. roads [<x> <y> ...] — how far the road "
        + "map from home has reached and what it cost, or for each tile named the steps of road from home against the "
        + "straight line: the answer to \"is that ground far, or only far round\". zones — the danger zones: how many of "
        + "each level, the ten strongest, and the debuggers' tour. zones tour on|off — the debuggers walk the least-watched zones "
        + "and wake the ground there, or go back to watching the bots. zonewatch on|off|state — the zone watch: one debugger "
        + "wholly on the zones and the others carrying its charge, the accuracy counter both ways (deaths and visits), and the "
        + "end at 99 %; on puts the keeper back, off ends it now, state says where it stands. zone <x> <y> — what the zones say about one tile: its "
        + "level, the zone it lies in, what lives there, its strength against the median bot, how sure the shape is and who "
        + "died there. reach check <n> — examine n border tiles of the reach map with the walker's own search, a debugger "
        + "standing on each; reach check — the tally and the misses; reach redraw <x> <y> [<r>] — draw that square of the map "
        + "again; reach carry on|off — whether a debugger stands on each tile examined. "
        + "charters — every town's charter standing, its holder, price and levies, the next auction and this period's books. "
        + "charter <town> <smith|tailor|woodsman|alchemist|scribe|cook> — auction that town's charter for the craft now, until the "
        + "next general auction: the bids, the winner and what it paid the town. "
        + "steward [<town>] — the towns' stewards: who governs, the coffer, the tax, the regard, the bars, the last decrees and "
        + "what is lawful now (* what the rule would take). decree <town> tax +2|captain [<guild>]|bounty <bot>|militia|bar <guild>|"
        + "holiday|caravan [<town>] — the town's steward gives it now if it is lawful and the coffer can pay; spoken and logged like "
        + "any decree. "
        + "officers [<guild>] — each guild's quartermaster, herald and scout, how long they have held the post, the guild's store and "
        + "the debts owed to it, the herald's invitations and bouts, the scout's watches; with no guild, every guild and the officers' "
        + "clause. appoint <guild> <quartermaster|herald|scout> <bot> — give the post by hand, whatever the fit; the head's next review "
        + "may take it back. None is offered to the minds.";

    /// <summary>How many of each verb this session. For the summary, and for reading the hand's own habits.</summary>
    private static readonly Dictionary<string, long> _used = [];

    /// <summary>Times a verb was asked for and refused. A refusal is a measurement, so it is counted.</summary>
    public static long Refused { get; private set; }

    /// <summary>Times a verb was carried out.</summary>
    public static long Used { get; private set; }

    private static string _path;

    private static bool _broken;

    /// <summary>
    /// How far a bot may be lifted in one go, in tiles. Wide enough to cross Britain and out of a pocket on
    /// its far side, narrow enough that a hallucinated pair of coordinates moves somebody across a field
    /// rather than across a continent.
    /// </summary>
    public static int Reach { get; set; } = 600;

    public static void Open(string who)
    {
        _broken = false;
        _used.Clear();
        Used = 0;
        Refused = 0;

        try
        {
            var folder = Path.GetFullPath(Path.Combine(Core.BaseDirectory, "..", "logs"));

            Directory.CreateDirectory(folder);

            _path = Path.Combine(folder, "bot-debugger-commands.log");

            Write(new string('=', 96));
            Write($"{who} has hands from this moment: {string.Join(", ", Verbs)} — every use of them is on this file");
            Write(new string('=', 96));
        }
        catch (Exception e)
        {
            _broken = true;

            logger.Warning("The debugger's command log could not be opened, so it will have no hands: {Message}", e.Message);
        }
    }

    private static void Write(string line)
    {
        if (_broken || _path == null || line == null)
        {
            return;
        }

        try
        {
            File.AppendAllText(_path, $"[{DateTime.Now:HH:mm:ss}] {line}{Environment.NewLine}");
        }
        catch (Exception e)
        {
            _broken = true;

            logger.Warning("The debugger's command log stopped taking lines: {Message}", e.Message);
        }
    }

    /// <summary>
    /// Runs one verb and says what came of it in one line.
    ///
    /// <para>
    /// <b>Nothing happens unless the log is working.</b> The order is deliberate: a hand whose record can
    /// fail open is a hand nobody can audit, and this file is the whole reason the hand is safe to have.
    /// </para>
    /// </summary>
    /// <param name="who">Who asked — the model, the console, or the roll-call.</param>
    /// <param name="verb">One of <see cref="Verbs"/>.</param>
    /// <param name="tail">Whatever followed it.</param>
    /// <param name="why">The reason given. Written beside the use, because a use without one is a twitch.</param>
    public static string Run(string who, string verb, string tail, string why, bool byHand = false)
    {
        verb = (verb ?? "").Trim().ToLowerInvariant();
        tail = (tail ?? "").Trim();

        if (verb.Length == 0 || verb == "none" || verb == "-")
        {
            return null;
        }

        // Two lists, and which one is consulted depends on who is asking. A mind may only ever pick out of
        // Verbs — BotDebugNote writes that array into its schema as an enumeration — so anything that
        // destroys something permanent lives in HandVerbs, where no model can reach it.
        if (Array.IndexOf(Verbs, verb) < 0 && !(byHand && (Array.IndexOf(HandVerbs, verb) >= 0 || Array.IndexOf(BotColonies.Verbs, verb) >= 0)))
        {
            Refused++;
            Write($"REFUSED {who}: \"{verb} {tail}\" — no such verb");

            return $"I have no verb \"{verb}\".";
        }

        if (_broken)
        {
            Refused++;

            return "my command log is not writable, so I will not use my hands.";
        }

        string answer;

        try
        {
            answer = Do(verb, tail);
        }
        catch (Exception e)
        {
            Refused++;
            Write($"THREW {who}: \"{verb} {tail}\" — {e.Message}");

            return $"{verb} threw: {e.Message}";
        }

        Used++;
        _used[verb] = _used.TryGetValue(verb, out var count) ? count + 1 : 1;

        Write($"{who}: {verb} {tail}");

        if (!string.IsNullOrWhiteSpace(why))
        {
            Write($"    because: {why}");
        }

        Write($"    -> {answer}");

        return answer;
    }

    private static string Do(string verb, string tail)
    {
        switch (verb)
        {
            case "tile":
                return Tile(tail);

            case "tele":
                return Tele(tail);

            case "camp":
                return Camp(tail);

            case "near":
                return Near(tail);

            case "call":
                return Call();

            case "roles":
                return BotCalling.Describe();

            case "pays":
                return Pays(tail);

            case "gaps":
                return Gaps();

            case "city":
                return City(tail);

            case "post":
                return Post(tail);

            case "unpost":
                return Unpost(tail);

            case "quests":
                return $"{BotQuests.Describe()}.";

            // The reach map (01.10.2026, Patrick's order of 30.09.2026, night): a tile, the map, its check. See BotReachMap.
            case "reach":
                return ReachMap(tail);

            // By hand only. See BotHand.ByHand: neither of these is in Verbs, so no mind can ask for them.
            case "halls":
                return BotEstate.Describe();

            case "revel":
                return Revelry(tail);

            case "wars":
                return BotWar.Describe();

            // The guilds' meetings, agreements and towns (29.09.2026). See BotParley.
            case "relics":
                return BotRelics.Report();

            case "relic":
                return BotRelics.Give(tail);

            case "parley":
                return BotParley.Tell();

            case "meet":
                return BotParley.ByHand(tail);

            // Town charters: the list, and one town's craft auctioned now (07.10.2026, charters20). See BotCharters.
            case "charters":
                return BotCharters.Tell();

            case "charter":
                return BotCharters.ByHand(tail);

            case "census":
                return Census();

            case "guilds":
                return Guilds();

            // With no name: every shooter on the island. With one, it falls through to the bot below.
            case "arms" when string.IsNullOrWhiteSpace(tail):
                return Quivers();

            // The bandits a catch left bare before 12.10.2026 get the weapon they were born to back. See BotOutlaw.ReturnWeapons.
            case "rearm":
                return BotOutlaw.ReturnWeapons();

            case "reset":
                return ResetPopulation();

            // Strikes out what the population has learned about one kind of work, so it is judged on its promise again. See
            // BotCommons.Forgive: for a memory that was true of the captain and was filed against the school.
            case "forgive":
                if (string.IsNullOrWhiteSpace(tail))
                {
                    Refused++;

                    return "forgive wants a kind of work: forgive drill-in.";
                }

                return $"{BotCommons.Forgive(tail.Trim())} records of \"{tail.Trim()}\" struck out of what the population knows; the next bot asked judges it on its promise";

            case "tourney":
                return tail.StartsWith("stop", StringComparison.OrdinalIgnoreCase)
                    ? BotTourney.Stop()
                    : tail.StartsWith("state", StringComparison.OrdinalIgnoreCase)
                        ? BotTourney.Describe()
                        : BotTourney.Start("the keyboard");

            // The day's championship at the round arenas (07.10.2026, arena27): "arena" holds it if today's has not been held,
            // "arena force" holds one anyway for a test (logged as forced, the day's not spent), "arena state" tells the bracket,
            // "arena stop" calls it off. See BotTourney.ArenaByHand.
            case "arena":
                return tail.StartsWith("stop", StringComparison.OrdinalIgnoreCase)
                    ? BotTourney.Stop()
                    : tail.StartsWith("state", StringComparison.OrdinalIgnoreCase)
                        ? BotTourney.ArenaBracket()
                        : BotTourney.ArenaByHand(tail.StartsWith("force", StringComparison.OrdinalIgnoreCase));

            case "band":
                return Band();

            // The towns' stewards (07.10.2026, steward20): who governs a town and what it decreed, and a decree given by hand. See
            // BotStewardship.Tell and BotStewardship.ByHand.
            case "steward":
                return BotStewardship.Tell(tail);

            case "decree":
                return BotStewardship.ByHand(tail);

            // Argus's proving ground. See BotProving: Patrick's order of 26.09.2026, a bot's double against a creature on the green.
            case "proofs":
                return BotProving.Board();

            // Whether the creatures round the bots have their minds on at all: the engine sleeps every sector no client is in.
            case "awake":
                return BotWake.Census();

            case "proof":
                return string.IsNullOrWhiteSpace(tail) ? BotProving.Board() : BotProving.Tell(tail.Trim());

            case "prove":
                {
                    var (who, what) = First(tail);

                    return string.IsNullOrWhiteSpace(who)
                        ? "prove wants a bot, and may name a creature: prove Nessa OrcishLord."
                        : BotProving.Prove(who, what?.Trim(), "the keyboard");
                }

            case "seats":
                return BotSeat.Tell();

            case "seat":
                return Seat(tail);

            case "road":
                return Road(tail);

            case "roads":
                return Roads(tail);

            case "chart":
                return Chart(tail);

            case "nav":
                return Nav(tail);

            case "navcells":
                return NavCells(tail);

            case "gates":
                return Gates(tail);

            case "navbench":
                return NavBench(tail);

            case "peril":
                return Peril(tail);

            // The danger zones and the debuggers' tour of them (30.09.2026). See BotZones.
            case "zones":
                return Zones(tail);

            case "zone":
                return Zone(tail);

            // The zone watch by hand: on, off, or where it stands (07.10.2026). See BotZoneWatch.
            case "zonewatch":
                return BotZoneWatch.Turn(tail.Trim());

            // The guilds' officers (07.10.2026, officers20). See BotOfficers.
            case "officers":
                return BotOfficers.Tell(tail);

            case "appoint":
                return BotOfficers.Appoint(tail);

            case "resolves":
                return BotWill.DescribeResolve();

            case "breaks":
                return BotBreaker.List();

            // The guilds' colonies on the islands (07.10.2026, sea20). See BotColonies.Verbs.
            case "expeditions":
                return BotColonies.Tell();

            case "colony":
                return string.IsNullOrWhiteSpace(tail) ? "colony wants a guild: colony The Hammer." : BotColonies.Launch(tail.Trim());

            case "save":
                {
                    if (World.Saving)
                    {
                        return "the world is being saved already.";
                    }

                    World.Save();

                    // <b>The save returns before the snapshot is on disk.</b> World.Save serialises and hands the
                    // writing to a thread; on 16.09.2026 at 00:20 a shard stopped four seconds after this answer was
                    // read, inside the engine's own autosave's write, and the world on disk was 83 mobiles. So the
                    // answer that matters is written later, when the engine is Running again, and the deploy waits
                    // for it.
                    Timer.DelayCall(TimeSpan.FromMilliseconds(500), Settled, 0);

                    return "the world is saved and the snapshot is being written; wait for \"the snapshot is on disk\" here before stopping the shard.";
                }

            case "raze":
                {
                    var gone = BotEstate.Raze() + BotOutpost.Raze();

                    return gone == 0
                        ? "there are no guild halls standing to take down."
                        : $"{gone} guild halls are off the island. What was inside them went with them.";
                }
        }

        var (name, rest) = First(tail);
        var bot = Find(name);

        if (bot == null)
        {
            Refused++;

            return $"there is no bot called \"{name}\".";
        }

        return verb switch
        {
            "props" => Props(bot),
            "sight" => Sight(bot),
            "where" => Whereabouts(bot),
            "go" => Go(bot, rest),
            "pack" => Packed(bot),
            "home" => Homeward(bot),
            "res" => Raise(bot),
            "free" => Free(bot),
            "shun" => Leave(bot),
            "summon" => Summon(bot),
            "resolve" => BotWill.Explain(bot),
            "jam" => Jam(bot),
            "trip" => Trip(bot, rest),
            "arm" => Arm(bot),
            "arms" => Arms(bot),
            _ => $"I have no verb \"{verb}\"."
        };
    }

    /// <summary>
    /// Puts in as many failures of one kind of work for one bot, all for one reason, as trip the breaker - the state a
    /// loop leaves a bot in.
    ///
    /// <para>
    /// <b>By hand only, and for the reason <see cref="Jam"/> exists: to make a state on purpose and watch what answers
    /// it.</b> A loop happens on its own schedule, and a rule that only fires on one cannot be checked after every build
    /// unless the loop can be made. The failures go through <see cref="BotBreaker.Failed"/> itself, so what is exercised
    /// is the breaker's own counting, its rest and its veto; nothing else on the shard is touched, and the rest lapses on
    /// its own clock or at the bot's next finish of that work.
    /// </para>
    /// </summary>
    private static string Trip(BotMobile bot, string rest)
    {
        var kind = (rest ?? "").Trim().Split(' ', 2)[0].ToLowerInvariant();

        if (kind.Length == 0)
        {
            return "trip wants a bot and a kind of work: trip <bot> <kind>.";
        }

        if (!BotBreaker.Running)
        {
            return "the breaker is switched off (BotBreaker.Running), so nothing can trip it.";
        }

        var times = Math.Max(2, BotBreaker.Failures);
        var before = BotBreaker.Trips;

        for (var i = 0; i < times; i++)
        {
            BotBreaker.Failed(bot, kind, "a failure put in by hand to test the breaker");
        }

        return $"{bot.Name}: {times} failures of {kind} put in, the breaker tripped {BotBreaker.Trips - before} times, and {kind} rests {BotBreaker.RestLeftMs(bot, kind) / 1000}s more.";
    }

    /// <summary>
    /// Banks the coin in one bot's pack and fills the pack to the engine's item cap with oil cloths that weigh a tenth of
    /// a stone, which is the state Hale was found in on 14.09.2026.
    ///
    /// <para>
    /// <b>By hand only, and it exists to put a bot into a fault on purpose.</b> A pack reaches the cap after hours of
    /// goods handed to it outside the engine's own test, so the rule that answers that state - work at a counter refused,
    /// the trip that makes room offered unpaid, see <see cref="BotYield.Pocket"/> - cannot be watched unless the state
    /// can be made. The cloths are light so that the bot is jammed rather than overloaded, and what follows is the jammed
    /// branch rather than the immobile one. It creates things in one bot's pack and nowhere else, and they go with the
    /// bot at the next restart.
    /// </para>
    /// </summary>
    private static string Jam(BotMobile bot)
    {
        var pack = bot.Backpack;

        if (pack == null || pack.MaxItems <= 0)
        {
            return $"{bot.Name} has no pack with a cap to fill.";
        }

        var coin = pack.GetAmount(typeof(Gold));
        var banked = 0;

        if (coin > 0 && pack.ConsumeTotal(typeof(Gold), coin))
        {
            if (Banker.Deposit(bot, coin))
            {
                banked = coin;
            }
            else
            {
                pack.DropItem(new Gold(coin));
            }
        }

        var added = 0;
        var most = pack.MaxItems;

        while (pack.TotalItems < most && added < most)
        {
            pack.DropItem(new OilCloth { Weight = 0.1 });
            added++;
        }

        return $"{bot.Name}: {banked}gp banked, {added} oil cloths added, {pack.TotalItems} of {most} things in the pack, room for a coin: {BotYield.Pocket(bot)}.";
    }

    /// <summary>
    /// Puts a vanquishing copy of the weapon a bot was born with into its pack: the state the right drop leaves a
    /// fighter in.
    ///
    /// <para>
    /// <b>By hand only, for the reason <see cref="Jam"/> exists.</b> <c>BotMobile.Rewield</c> wields a better weapon of
    /// the bot's own kind, and after a boot nobody carries one — every bot is rebuilt with its kit, and loot of the right
    /// kind is rare — so build 55 sat its whole first window without a candidate, and the refusal line that used to show
    /// such a weapon riding in a pack was the one that build turned into a skip. The copy is the birth type itself at the
    /// top damage level, so it is better by the engine's own arithmetic whatever the class. It is not bound: a death
    /// leaves it on the corpse like any loot, and it goes with the bot at the next restart.
    /// </para>
    /// </summary>
    /// <summary>
    /// <c>census</c>: every bot on one line each — class, band, what it holds and how far into it, health, mana,
    /// purse and account, where it stands and in what region, its company — appended to <c>logs/bot-census.log</c>.
    ///
    /// <para>
    /// <b>The one instrument that shows the whole population at one instant.</b> The summaries are counters since
    /// the boot and the per-bot lines are events; neither answers "who is doing what right now, and with what in
    /// its pack" without reading a hundred lines backwards. Written on 15.09.2026 for the night watch, so that a
    /// question like "did anybody hire a healer" or "who is red" is one grep on one stamp.
    /// </para>
    /// </summary>
    /// <summary>
    /// <c>guilds</c>: every guild in one paragraph — who leads it, how many it has in the world, at rest and dead, what its
    /// members fight with, what it owns, and the war it stands in, with how many of it are in the fight against how many
    /// its score asks for (<c>BotWar.Wanted</c>).
    ///
    /// <para>
    /// <b>Patrick's order of 24.09.2026: an instrument of its own for reviewing the guilds.</b> The question that day was
    /// why The Blade won every war it was in, and answering it took a census, three log scripts and the tourney's ranking
    /// of that morning; the answer — the losing side fought with two to seven of its fifteen, not with weaker bots — sat
    /// in numbers nothing printed side by side. This prints them side by side, for all the guilds at once, at rest and in
    /// the world alike.
    /// </para>
    /// </summary>
    private static string Guilds()
    {
        List<Server.Guilds.Guild> guilds = [.. BotGuilds.Standing];

        if (guilds.Count == 0)
        {
            return "there are no guilds.";
        }

        guilds.Sort((a, b) => (b.Members?.Count ?? 0).CompareTo(a.Members?.Count ?? 0));

        using var say = ValueStringBuilder.Create(8192);
        List<(string Name, double Power)> ranking = [];
        List<double> powers = [];
        Dictionary<string, int> classes = [];

        foreach (var guild in guilds)
        {
            var members = guild.Members;
            var count = members?.Count ?? 0;
            int inWorld = 0, resting = 0, dead = 0, fight = 0;
            double power = 0, armour = 0, weapon = 0, tactics = 0, anatomy = 0, healing = 0, magery = 0;
            int bandages = 0, potions = 0, bots = 0;
            long purses = 0;

            powers.Clear();
            classes.Clear();

            for (var i = 0; i < count; i++)
            {
                if (members[i] is not BotMobile { Deleted: false } bot)
                {
                    continue;
                }

                bots++;

                if (bot.Map == null || bot.Map == Map.Internal)
                {
                    resting++;
                }
                else if (!bot.Alive)
                {
                    dead++;
                }
                else
                {
                    inWorld++;
                }

                var name = bot.Class?.Name ?? "?";
                classes[name] = classes.GetValueOrDefault(name) + 1;

                if (bot.Class?.Kit is { } kit && (kit.Melee.Count > 0 || kit.Ranged.Count > 0))
                {
                    fight++;
                }

                var strength = BotThreat.Power(bot);
                power += strength;
                powers.Add(strength);
                armour += bot.ArmorRating;

                var arm = (bot.Weapon as BaseWeapon)?.Skill ?? SkillName.Wrestling;
                weapon += bot.Skills[arm].Base;
                tactics += bot.Skills.Tactics.Base;
                anatomy += bot.Skills.Anatomy.Base;
                healing += bot.Skills.Healing.Base;
                magery += bot.Skills.Magery.Base;

                var pack = bot.Backpack;
                bandages += pack?.GetAmount(typeof(Bandage)) ?? 0;
                potions += pack?.GetAmount(typeof(BasePotion)) ?? 0;
                purses += (pack?.GetAmount(typeof(Gold)) ?? 0) + Banker.GetBalance(bot);
            }

            powers.Sort((a, b) => b.CompareTo(a));

            var best = 0.0;

            for (var i = 0; i < powers.Count && i < 5; i++)
            {
                best += powers[i];
            }

            var per = Math.Max(1, bots);
            ranking.Add((guild.Name, power));

            if (say.Length > 0)
            {
                say.Append(Environment.NewLine);
            }

            say.Append(
                $"{guild.Name} [{guild.Abbreviation}] under {guild.Leader?.Name ?? "nobody"}: {count} of {BotGuilds.Ceiling(guild)} places, {inWorld} in the world, {resting} at rest, {dead} dead; "
            );

            say.Append($"{fight} who fight (");

            var first = true;

            foreach (var (name, many) in classes)
            {
                say.Append(first ? "" : ", ");
                say.Append($"{name} {many}");
                first = false;
            }

            say.Append(
                $"); strength {power:F0}, {power / per:F0} a head, the best five {best:F0}; armour {armour / per:F0}, weapon {weapon / per:F0}, tactics {tactics / per:F0}, anatomy {anatomy / per:F0}, healing {healing / per:F0}, magery {magery / per:F0}; {bandages / per} bandages and {potions / per} potions a head; "
            );

            var hall = BotEstate.Hall(guild);

            say.Append(
                $"{(BotGuildHouses.Of(guild.Name) is { } house ? $"a house in {house.Town} at {house.Heart.X},{house.Heart.Y}, " : "")}{(hall is { Deleted: false } ? $"hall at {hall.X},{hall.Y}" : "no hall")}, {BotChest.Holds(guild.Name)}gp in the chest and {purses}gp in its members' purses and accounts, {BotClaim.Holds(guild.Name)} squares held"
            );

            foreach (var war in BotWar.Standing)
            {
                var enemy = war.Against(guild.Name);

                if (enemy == null)
                {
                    continue;
                }

                say.Append(
                    $"; at war with {enemy} for {war.Minutes} min, {war.Score(guild.Name).Kills} to {war.Score(enemy).Kills}, {BotWar.Involved(guild.Name)} of ours in it against {BotWar.Wanted(guild.Name)} the score asks for{(war.Big ? ", a big war" : "")}"
                );
            }

            foreach (var (pair, ends) in BotWar.Truces)
            {
                var other = pair.A == guild.Name ? pair.B : pair.B == guild.Name ? pair.A : null;

                if (other != null && ends - Core.TickCount > 0)
                {
                    say.Append($"; a truce with {other} for {(ends - Core.TickCount) / 60000} more minutes");
                }
            }

            say.Append('.');
        }

        ranking.Sort((a, b) => b.Power.CompareTo(a.Power));
        say.Append(Environment.NewLine);
        say.Append("By strength: ");

        for (var i = 0; i < ranking.Count; i++)
        {
            say.Append(i == 0 ? "" : ", ");
            say.Append($"{ranking[i].Name} {ranking[i].Power:F0}");
        }

        say.Append('.');

        return say.ToString();
    }

    private static string Census()
    {
        var bots = BotPopulation.Bots;
        var away = BotPopulation.Away;
        var stamp = DateTime.Now.ToString("HH:mm:ss");
        using var sb = ValueStringBuilder.Create(16384);
        var alive = 0;
        var idle = 0;

        // Those at rest too, marked so: a census of the island that leaves out a third of the population cannot say what a
        // guild could put in the field.
        for (var i = 0; i < bots.Count + away.Count; i++)
        {
            var resting = i >= bots.Count;
            var bot = resting ? away[i - bots.Count] : bots[i];

            if (bot is not { Deleted: false })
            {
                continue;
            }

            var deed = bot.Resolve?.Deed;

            if (bot.Alive)
            {
                alive++;
            }

            if (deed == null)
            {
                idle++;
            }

            var map = bot.Map;
            var region = map == null || map == Map.Internal ? null : Region.Find(bot.Location, map);
            var guild = bot.Guild is Server.Guilds.Guild g ? g.Abbreviation : "-";
            var company = bot.Squad == null ? "no company" : $"company {bot.Squad.Id} {bot.Squad.Stance}";
            var flags = (bot.Murderer ? " | RED" : "") + (bot.NameHue >= 0 ? $" | name hue {bot.NameHue}" : "");

            sb.Append($"[{stamp}] {bot.Name} | {bot.Class?.Name ?? "?"} | {guild} | {(resting ? "resting" : bot.Alive ? "alive" : "dead")} | {deed?.Kind ?? "nothing"}: {deed?.Stage ?? "-"} | hits {bot.Hits}/{bot.HitsMax} mana {bot.Mana}/{bot.ManaMax} | pack {bot.Backpack?.GetAmount(typeof(Gold)) ?? 0}gp bank {Banker.GetBalance(bot)}gp | at ({bot.X}, {bot.Y}, {bot.Z}) {region?.Name ?? "open ground"} | {company}{flags}");
            sb.Append($" | {Fighting(bot)}");
            sb.Append(Environment.NewLine);
        }

        try
        {
            var folder = Path.GetFullPath(Path.Combine(Core.BaseDirectory, "..", "logs"));
            Directory.CreateDirectory(folder);
            File.AppendAllText(Path.Combine(folder, "bot-census.log"), sb.ToString());
        }
        catch (Exception e)
        {
            return $"the census could not be written: {e.Message}";
        }

        return $"{bots.Count + away.Count} bots written to logs/bot-census.log at {stamp}: {alive} alive, {away.Count} of them at rest, {idle} holding nothing.";
    }

    /// <summary>
    /// What a bot brings to a fight, for the census: its strength by the population's own reckoning, the armour the
    /// engine reads, the weapon in hand and what it hits for, the skills a fight turns on, and what it heals with.
    ///
    /// <para>
    /// <b>Added on 24.09.2026, when the question was why one guild won every war it was in.</b> Health times damage
    /// (<c>BotThreat.Power</c>) is what the tourney ranks by, and it could not tell two guilds apart whose fights ended
    /// 25 to 3: armour, parrying and healing are not in it, and they are exactly what "its health has not moved"
    /// under four blades is made of.
    /// </para>
    /// </summary>
    private static string Fighting(BotMobile bot)
    {
        var weapon = bot.Weapon as BaseWeapon;
        var skills = bot.Skills;
        var held = weapon is null or Fists ? "fists" : $"{weapon.GetType().Name} {weapon.MinDamage}-{weapon.MaxDamage}";
        var arm = weapon?.Skill ?? SkillName.Wrestling;
        var pack = bot.Backpack;

        return $"power {BotThreat.Power(bot):F0} AR {bot.ArmorRating:F0} {held} ({arm} {skills[arm].Base:F0}) tactics {skills.Tactics.Base:F0} anatomy {skills.Anatomy.Base:F0} healing {skills.Healing.Base:F0} parry {skills.Parry.Base:F0} magery {skills.Magery.Base:F0} resist {skills.MagicResist.Base:F0} inscribe {skills.Inscribe.Base:F0} | str {bot.RawStr} dex {bot.RawDex} | bandages {pack?.GetAmount(typeof(Bandage)) ?? 0} potions {pack?.GetAmount(typeof(BasePotion)) ?? 0}";
    }

    /// <summary>
    /// <c>reset</c>: the population back to novices. Patrick's order of 16.09.2026 (and of 13.09 before it): the halls
    /// razed, every claim let go, the hand-set seats forgotten, the wars and truces and opinions between guilds
    /// forgotten, and everybody's learning wiped — then the world saved, so a restart raises the population fresh.
    /// The world save itself, the guilds and the island's map are not touched. Nothing here deletes a file: the stores
    /// write themselves empty at the save this makes.
    /// </summary>
    private static string ResetPopulation()
    {
        var halls = BotEstate.Raze();
        var claims = BotClaim.Wipe();

        // The chests were the claims' income; a reset takes them too. See BotChest.
        BotChest.Wipe();
        var seats = BotSeat.Wipe();

        // And the guilds' houses in the towns: their doors and signs come down and the buildings stand empty for a new world
        // (29.09.2026, evening). See BotGuildHouses.
        var houses = BotGuildHouses.Wipe();

        BotWar.Forget();
        BotRegard.Forget();

        var learned = BotProgress.Wipe();

        // <b>And the bodies themselves, which is new since build 163.</b> A reset used to wipe the learning and
        // leave the rest to the boot, because the boot deleted every bot in the save and raised the population
        // afresh. It does not any more: bots come back with their place, their pack, their bank and their ledger, so
        // a reset that did not take the bodies would hand a "population of novices" four hundred gold each and a
        // full kit. Deleting them here, before the save, is what makes the next boot raise real novices.
        var bodies = BotPopulation.PurgeSaved();

        // <b>Patrick's words of 18.09.2026: "when I say the bots forget everything, they literally begin a NEW
        // world".</b> Everything below outlives a restart on purpose and so has to be said out loud here, one line
        // each. The boards first — what is for sale, what is wanted, what errands stand — then the crime that was
        // done to a population that no longer exists, then the guilds, and last the two records that are the whole
        // of what the island had learned: the danger of its ground and what pays where on it.
        BotMarket.Reset();

        // And the traders' books and counters (01.10.2026, build 385 review): the books themselves go with the packs above, but
        // the pages held in memory are keyed by the serials of bots that no longer exist, and a look's quiet clock with them.
        BotTrading.Forget();

        BotQuests.Wipe();
        BotOutlaw.Forget();
        BotUnderworld.Forget();
        BotGuilds.Forget();
        BotGround.Reset();
        BotOre.Forget();

        // <b>The island's danger map is kept (01.10.2026, build 379).</b> Patrick's order of the night of 30.09: «paths and what
        // is known of the zones live on, and the bots can still improve them» — the paths and what is known of the zones live on through a wipe, and
        // the bots go on improving them. BotQuad's blood by square is that knowledge, as the zones' own file is (BotZones).
        BotCommons.Forget();

        // Everybody's play and rest, and the newcomers laid over the configured mix: a new world is raised from the
        // configuration alone, and every bot in it starts a fresh session.
        BotRest.Forget();
        BotGrowth.Forget();

        // And where everybody lived (29.09.2026): a new world chooses its towns again. See BotResidence.
        BotResidence.Wipe();

        // And the guilds' dealings (29.09.2026): the agreements, the books of grievances, the clocks between pairs and every
        // guild's standing in every town. See BotParley.
        BotParley.Wipe();
        BotPact.Wipe();
        BotGrievances.Wipe();
        BotBurgh.Wipe();

        // <b>And the two records the reset of 16.09.2026 had not yet met, said at the wipe of 25.09.2026 — Patrick: "in
        // short, a WIPE".</b> The city's wants, fair and the bounties on the heads of a population that no longer exists;
        // and the championship's champion, whose yellow would otherwise pass to whichever novice is born with the name.
        BotCity.Forget();
        BotTourney.Forget();

        // And what the proving ground measured: the bodies it measured are gone. See BotProving (26.09.2026).
        BotProving.Forget();

        // And the houses of the population's own, which a wiped island does not keep, and the guilds' outposts. See BotAbode
        // and BotOutpost (26.09.2026).
        BotAbode.Raze();
        BotOutpost.Raze();

        // And who was told of which guild's toll, and how each guild charged. See BotToll (26.09.2026).
        BotToll.Forget();
        BotTollman.Forget();

        // <b>And what the island learned of its roads, and who had been where (29.09.2026).</b> The roads themselves are the
        // map's and are drawn again at the next boot; the walks along them are the population's knowledge and go with the
        // rest of it. The towns, the gates and the inns are read from the world again at boot and keep nothing of their own
        // but counters and the bank's dealing, which start over.
        // The roads walked are kept with the danger map, by the same order (build 379); the travellers' own records go.
        BotTraveller.Forget();
        BotVenturer.Forget();
        BotInns.Forget();
        BotPeoples.Forget();

        // <b>And the modules born since 06.10.2026, each with a store of its own under Saves/ (07.10.2026, reset26).</b> The reset
        // of 20:56 that night wiped everything above and left these whole — the characters, the charters, the colonies, the books
        // on crime, the events, the loans, the officers and the guilds' stores, the politics, the relics and the stewardships —
        // and Patrick moved their folders away by hand before the restart. Each is emptied here by its own Wipe, exactly what its
        // store writes, so the save below writes them empty. After the bodies are gone, so that whatever their deletion settles is
        // settled before the books are read.
        //
        // Money first: the stewards' coffers and the wages of watches on duty (politics), and the invasion's and the caravan's
        // holds (events), go back into the city's purse through BotCity.Refund, so the purse rises and its held count falls by the
        // same coin; the rumoured chest's coin goes back to the crown that filled it. The errands' rewards went back above
        // (BotQuests.Wipe). Whatever is still counted as held after all of them was held for nobody, and goes back last
        // (BotCity.Unhold). The rest hold no coin: a charter, a loan, a kit debt, a tribute or a relic is a line in a book, and
        // the coin it moved moved when it happened.
        //
        // BotRunes is emptied below too (the reset26 review). Four of the fifteen folders were already emptied above and are written
        // again empty by this save: BotSeizures by BotWar.Forget, BotGuildSplits and BotGuildWiden by BotGuilds.Forget, BotTourney by
        // BotTourney.Forget.
        var coffers = BotPolitics.Wipe();
        var held = BotSeasons.Wipe();
        var lives = BotLives.Wipe();

        // And the timetables of the daily cycle (PLAN-life A1), kept by name in their own store as the characters are.
        var timetables = BotTimetables.Wipe();
        var charters = BotCharters.Wipe();
        var colonies = BotColonies.Wipe();
        var wanted = BotCrime.Wipe();
        var loans = BotLoans.Wipe();
        BotTrades.Forget();
        var posts = BotOfficers.Wipe();
        var relics = BotRelics.Wipe();

        // And the guilds' runes (reset26 review): kept by the guild's name, so a guild founded again under an old name inherited them.
        // Guild property, not the island's map: the danger map, the zones and the roads stay. See BotRuneShelf.Wipe.
        var runes = BotRuneShelf.Wipe();

        // And the stories and the island's Chronicle (PLAN-life A3, 13.10.2026): a new world has no history, and a name born again
        // starts a new book. The store writes itself empty at the save below. See BotStory.Wipe.
        var stories = BotStory.Wipe();
        var unheld = BotCity.Unhold();

        var saving = SaveWiped(0);

        return $"reset: {halls} halls razed, {claims} claims let go, {seats} hand-set seats forgotten, {houses} guild houses emptied, wars, truces and opinions forgotten, "
               + $"{learned} bots' learning wiped and {bodies} bodies deleted with everything they were carrying, "
               + $"the market, the wants and the board of errands emptied, the city's wants and bounties and the championship's record forgotten, every crime and the band forgotten, the guilds disbanded, "
               + $"{lives} bots' characters, ties and tales forgotten, {timetables} timetables forgotten, {charters} charters let go, {colonies} colonies given up with their stores, {wanted} names struck off the books on crime, {loans} loans and {posts} officers' posts with the guilds' stores and kit debts, "
               + $"the politics and the stewardships ({coffers}gp of coffers and watches' wages back into the treasury), the events ({held}gp of their holds back to the purse and the crown), "
               + $"{relics} relic items taken out of the world (the town relics hidden again), {runes} guilds' runes forgotten, {stories} bots' stories and the Chronicle forgotten, the seizures, the guild splits and widenings, {unheld}gp held for nobody back into the treasury; "
               + $"everything known about what pays where wiped (the island's danger map, the zones and the roads walked are kept); "
               + (saving
                   ? "the world is being saved"
                   : $"a save was already under way (state {World.WorldState}), so the wiped world is saved as soon as the engine is free")
               + " — wait for \"the snapshot is on disk\", then restart the shard and the population rises as novices.";
    }

    /// <summary>
    /// The reset's own save, taken only when the engine can take it, and <see cref="Settled"/> started only once it has (07.10.2026,
    /// reset26 review). True when the save was accepted at once.
    ///
    /// <para>
    /// <b>Why not <c>World.Save()</c> behind <c>!World.Saving</c>, as before.</b> <c>World.Saving</c> is true only while the
    /// snapshot is taken, which runs on the game loop and so is never seen by a command; while an autosave is <i>writing</i>
    /// (<c>WorldState.WritingSave</c>) the loop runs on, <c>World.Save()</c> returns having done nothing (it saves only when
    /// Running), and <see cref="Settled"/> then said "the snapshot is on disk" for the autosave's write — a snapshot taken before
    /// the wipe, with every store and every body in it. A restart on that word raised the old world. So the save is asked for
    /// only in <c>WorldState.Running</c>, it is known accepted when the state has left Running, and only then is the word waited
    /// for; until then this looks again every half second, for two minutes, and says so if it never could.
    /// </para>
    /// </summary>
    private static bool SaveWiped(int tries)
    {
        if (World.WorldState == WorldState.Running)
        {
            World.Save();

            if (World.WorldState != WorldState.Running)
            {
                if (tries > 0)
                {
                    BotConsole.Say("the wiped world's save is under way now.");
                }

                Timer.DelayCall(TimeSpan.FromMilliseconds(500), Settled, 0);

                return true;
            }
        }

        if (tries >= 240)
        {
            BotConsole.Say($"the wiped world could NOT be saved in two minutes (state {World.WorldState}); do not restart the shard — say \"save\" once the engine is free.");

            return false;
        }

        Timer.DelayCall(TimeSpan.FromMilliseconds(500), () => { SaveWiped(tries + 1); });

        return false;
    }

    /// <summary>Says so through the door once the engine has finished writing, and keeps looking until it has.</summary>
    private static void Settled(int tries)
    {
        if (World.WorldState == WorldState.Running)
        {
            BotConsole.Say("the snapshot is on disk; it is safe to stop the shard now.");

            return;
        }

        if (tries >= 240)
        {
            BotConsole.Say($"the snapshot is STILL being written after two minutes (state {World.WorldState}); do not stop the shard.");

            return;
        }

        Timer.DelayCall(TimeSpan.FromMilliseconds(500), Settled, tries + 1);
    }

    /// <summary>
    /// <c>arms &lt;bot&gt;</c>: what is in its hand, every weapon in its pack, and what it has to fire — bound things
    /// included, which is the whole reason this is not <c>pack</c>.
    ///
    /// <para>
    /// <b>Written on 21.09.2026 because <c>pack</c> could not answer the question.</b> A warrior-archer was failing
    /// every hunt with "100% of it left and not a scratch in 45s", the signature of a bow with nothing in it, and
    /// <c>pack</c> listed no bolts and no blade — but <c>pack</c> leaves out whatever a bot is bound to, on purpose,
    /// and a bot's birth ammunition and its sidearm are both bound. So the instrument said "nothing" about exactly
    /// the things that were in doubt.
    /// </para>
    /// </summary>
    private static string Arms(BotMobile bot)
    {
        var pack = bot.Backpack;

        if (pack == null)
        {
            return $"{bot.Name} has no pack.";
        }

        var held = bot.Weapon as Item;

        if (held != null && held.Parent != bot)
        {
            held = null;
        }

        List<string> weapons = [];

        foreach (var item in pack.Items)
        {
            if (item is BaseWeapon { Deleted: false } weapon)
            {
                var bound = BotBinding.IsBound(weapon, bot.Bond) ? ", bound" : "";

                weapons.Add($"{weapon.GetType().Name} ({weapon.Skill}{(weapon is BaseRanged ? ", ranged" : "")}{bound})");
            }
        }

        var takes = held is BaseRanged bow ? bow.AmmoType : null;
        var loaded = takes == null ? -1 : pack.GetAmount(takes);

        return $"{bot.Name} the {bot.Class?.Name}: holding {(held == null ? "nothing" : held.GetType().Name)}"
               + (takes == null ? "" : $", which takes {takes.Name} and has {loaded} of them to fire")
               + $"; Arrow x{pack.GetAmount(typeof(Arrow))}, Bolt x{pack.GetAmount(typeof(Bolt))}"
               + $"; its class's bows have something to fire: {BotArms.Stocked(bot, bot.Class)}"
               + $"; weapons in the pack: {(weapons.Count == 0 ? "none" : string.Join(", ", weapons))}"
               + $"; archery {bot.Skills.Archery.Base:F1}, fencing {bot.Skills.Fencing.Base:F1}, swords {bot.Skills.Swords.Base:F1}, macing {bot.Skills.Macing.Base:F1}.";
    }

    /// <summary>
    /// <c>arms</c> with no name: every bot whose class is issued a bow, one clause each, the ones holding a bow
    /// they cannot fire said first and counted.
    /// </summary>
    private static string Quivers()
    {
        var bots = BotPopulation.Bots;
        List<string> dry = [];
        List<string> fine = [];

        for (var i = 0; i < bots.Count; i++)
        {
            var bot = bots[i];

            if (bot is not { Deleted: false } || bot.Class?.Kit.Ranged is not { Count: > 0 } || bot.Backpack == null)
            {
                continue;
            }

            var held = bot.Weapon as Item;

            if (held != null && held.Parent != bot)
            {
                held = null;
            }

            var pack = bot.Backpack;
            var takes = held is BaseRanged bow ? bow.AmmoType : null;
            var loaded = takes == null ? 0 : pack.GetAmount(takes);
            var blade = false;

            foreach (var item in pack.Items)
            {
                // Not "and not BaseKnife", which is what this said on its first run: a Dagger is a BaseKnife in this
                // engine, and the column read "no-blade" for all twenty-two shooters with a bound dagger each.
                if (item is BaseWeapon and not BaseRanged and not SkinningKnife and not ButcherKnife and not Cleaver)
                {
                    blade = true;

                    break;
                }
            }

            var say = $"{bot.Name} ({bot.Class.Name}) {(held == null ? "bare" : held.GetType().Name)} A{pack.GetAmount(typeof(Arrow))} B{pack.GetAmount(typeof(Bolt))}{(blade ? "" : " no-blade")}";

            if (takes != null && loaded == 0)
            {
                dry.Add(say);
            }
            else
            {
                fine.Add(say);
            }
        }

        return $"{dry.Count} of {dry.Count + fine.Count} shooters hold a bow with nothing for it to fire: {(dry.Count == 0 ? "nobody" : string.Join("; ", dry))}. "
               + $"The rest: {string.Join("; ", fine)}";
    }

    private static string Arm(BotMobile bot)
    {
        if (bot.Bond?.Weapon is not { Weapon: { } kind })
        {
            return $"{bot.Name} was born with no weapon to copy.";
        }

        var pack = bot.Backpack;

        if (pack == null)
        {
            return $"{bot.Name} has no pack.";
        }

        if (kind.CreateInstance<Item>() is not BaseWeapon copy)
        {
            return $"a {kind.Name} could not be made.";
        }

        copy.DamageLevel = WeaponDamageLevel.Vanq;

        if (!pack.TryDropItem(bot, copy, false))
        {
            copy.Delete();

            return $"{bot.Name}'s pack would not take a {kind.Name}.";
        }

        var held = bot.FindItemOnLayer(Layer.TwoHanded) as BaseWeapon ?? bot.FindItemOnLayer(Layer.OneHanded) as BaseWeapon;

        return $"{bot.Name}: a vanquishing {kind.Name} is in the pack, holding {held?.GetType().Name ?? "nothing"}; the re-arm looks every {BotMobile.DressEveryMs / 1000}s.";
    }

    /// <summary>
    /// Puts an orc camp on the ground: three orcs, a captain, a chest, a locked crate and a prisoner.
    ///
    /// <para>
    /// <b>The one verb here that adds to the world, and it earns that by being the only way to test two
    /// whole subsystems.</b> Looting chests and freeing prisoners were written on 08.09.2026 against a
    /// world that contains no camps unless somebody spawns one, and a camp spawned by hand is gone before
    /// anybody can walk to it: <c>OrcCamp</c> sets its own decay to five minutes and, alone among the camps,
    /// refuses to refresh that timer when somebody walks in. A camp 129 tiles from home therefore expires
    /// while the population is still on its way, which is exactly what happened to Patrick's.
    /// </para>
    ///
    /// <para>
    /// So the camp this puts down is given <see cref="CampMinutes"/> instead — long enough for a bot to
    /// notice it, walk to it, fight through it and walk a prisoner home. Everything else about it is the
    /// engine's own: the same class the world spawns, with the same contents and the same rules.
    /// </para>
    ///
    /// <para>
    /// <b>Where it stands is <see cref="BotRevel.Footing"/>'s business and not this verb's</b>, so a camp
    /// asked for by hand keeps out of the water by the same rule as one raised for a revel. The spot given
    /// is a request: it is settled against the map, and moved inland if the camp's own footprint would
    /// otherwise put half its garrison in the sea.
    /// </para>
    /// </summary>
    private static string Camp(string tail)
    {
        var here = BotVigil.Body;
        var map = here?.Map;

        if (map == null || map == Map.Internal)
        {
            return "I am nowhere, so there is nowhere to put a camp.";
        }

        var (xs, rest) = First(tail);
        var (ys, _) = First(rest);

        var asked = here.Location;

        if (int.TryParse(xs, out var x) && int.TryParse(ys, out var y))
        {
            asked = new Point3D(x, y, 0);
        }

        // Sited by the revel's own rule, water included, and never taken as given — not even the watcher's
        // own tile. Argus hovers rather than walks, so "here" is wherever it last teleported to, which is
        // a height it chose and a shore it never had to cross.
        if (!BotRevel.Footing(map, asked, out var found, out var floor))
        {
            Refused++;

            return $"({asked.X}, {asked.Y}) has no dry ground a camp could stand on, and nor does anything "
                + $"within {BotRevel.CampSweep} tiles of it.";
        }

        var where = new Point3D(found.X, found.Y, floor);

        var camp = new OrcCamp();

        camp.MoveToWorld(where, map);
        camp.DecayDelay = TimeSpan.FromMinutes(CampMinutes);

        Camps++;

        return $"an orc camp is standing at ({where.X}, {where.Y}, {where.Z}) and will last {CampMinutes} minutes: "
            + "three orcs, a captain, an unlocked chest, a locked crate and a prisoner who wants to go to Britain.";
    }

    /// <summary>
    /// Everything alive around a spot: who it is, how hurt it is, and whether it would fight the population.
    ///
    /// <para>
    /// <b>Written because "is the camp still garrisoned" had no answer.</b> The tile verb reports items and
    /// floors; nothing reported creatures, so a chest standing unopened could not be told apart from a chest
    /// nobody had reached — and on 08.09.2026 that was exactly the question, with 300 refusals filed under
    /// "still held by a garrison" and no way to see what was holding it.
    /// </para>
    /// </summary>
    /// <summary>The treasury, and what the city may do with it. See BotCity.</summary>
    private static string City(string tail)
    {
        var words = (tail ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries);

        if (words.Length == 0)
        {
            return $"{BotCity.Describe()}.";
        }

        switch (words[0].ToLowerInvariant())
        {
            case "buy":
                {
                    var lots = words.Length > 1 && int.TryParse(words[1], out var n) ? Math.Clamp(n, 1, 50) : 10;
                    var (taken, units, paid) = BotCity.Buy(lots, "the door");

                    return taken <= 0
                        ? $"the city sent for {lots} lots and bought nothing: {(BotCity.Purse <= 0 ? "the treasury is empty" : "nothing on offer it could pay for")}; {BotCity.Describe()}."
                        : $"the city bought {units} things in {taken} lots at the bots' shops for {paid}gp; {BotCity.Purse}gp left in the treasury.";
                }

            case "want":
                if (words.Length < 4 || !int.TryParse(words[2], out var amount) || !int.TryParse(words[3], out var price))
                {
                    Refused++;

                    return "city want wants a thing, an amount and a price: city want Leather 50 4.";
                }

                return BotCity.Want(words[1], amount, price, "the door");

            case "forget":
                if (words.Length < 2)
                {
                    Refused++;

                    return "city forget wants a thing: city forget Leather.";
                }

                return BotCity.Forget(words[1]);

            case "bounty":
                {
                    if (words.Length < 4 || !int.TryParse(words[1], out var x) || !int.TryParse(words[2], out var y) || !int.TryParse(words[3], out var gold))
                    {
                        Refused++;

                        return "city bounty wants two numbers and a price: city bounty 1995 1395 500.";
                    }

                    var map = BotPopulation.Home;

                    return map == null ? "the population has no map to put a bounty on." : BotCity.Bounty(map, x, y, gold, "the door");
                }

            case "fair":
                {
                    var minutes = words.Length > 1 && int.TryParse(words[1], out var m) ? m : Math.Max(1, BotCity.FairMs / 60000);
                    var share = words.Length > 2 && int.TryParse(words[2], out var percent) ? percent / 100.0 : BotCity.FairShare;
                    return BotCity.Fair(minutes, share, "the door");
                }
            case "head":
                {
                    if (words.Length < 3 || !int.TryParse(words[^1], out var gold))
                    {
                        Refused++;

                        return "city head wants a bot and a price: city head Fendrel 300.";
                    }

                    var name = string.Join(' ', words, 1, words.Length - 2);
                    var red = Find(name);

                    return red == null ? $"no bot of ours is called {name}." : BotCity.Head(red, gold, "the door");
                }

            default:
                Refused++;

                return "city knows: city, city buy [<lots>], city want <Thing> <amount> <price>, city forget <Thing>, city bounty <x> <y> <gp>, city head <bot> <gp>, city fair [<minutes>] [<percent>].";
        }
    }

    /// <summary>What pays where, best first, or for one trade only. Read off <see cref="BotCommons.Best"/>.</summary>
    private static string Post(string tail)
    {
        var words = (tail ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries);

        if (words.Length == 0)
        {
            Refused++;
            return "post wants an errand: post kill <Creature> <n> <gp> [<x> <y>], post gather <Thing> <n> <gp> [<x> <y>], or post scout <x> <y> <gp>.";
        }

        var map = BotPopulation.Home;

        if (map == null)
        {
            return "the population has no map, so there is no board.";
        }

        var kind = words[0].ToLowerInvariant();

        if (kind == "scout")
        {
            if (words.Length < 4 || !int.TryParse(words[1], out var sx) || !int.TryParse(words[2], out var sy) || !int.TryParse(words[3], out var sgp))
            {
                Refused++;
                return "post scout wants a place and a reward: post scout 1875 855 200.";
            }

            return BotQuests.PostByName(kind, null, 1, sgp, map, sx, sy, "the door");
        }

        if (words.Length < 4 || !int.TryParse(words[2], out var amount) || !int.TryParse(words[3], out var gold))
        {
            Refused++;
            return "post wants a thing, an amount and a reward: post kill Ogre 5 300 [x y] or post gather Leather 50 200 [x y].";
        }

        var x = -1;
        var y = -1;

        if (words.Length >= 6 && int.TryParse(words[4], out var px) && int.TryParse(words[5], out var py))
        {
            x = px;
            y = py;
        }

        return BotQuests.PostByName(kind, words[1], amount, gold, map, x, y, "the door");
    }

    private static string Unpost(string tail)
    {
        if (!int.TryParse((tail ?? "").Trim().TrimStart('#'), out var id))
        {
            Refused++;
            return "unpost wants the errand's number: unpost 3.";
        }

        return BotQuests.Unpost(id, "the door");
    }

    /// <summary>
    /// The band, its hideout, and what is actually in its chest.
    ///
    /// <para>
    /// <b>The one question about The Shadow the log cannot answer.</b> Every line the band writes is an event — a
    /// robbery, a stash, a catch — and the state between the events is invisible: whether the chest holds anything,
    /// whether the keeper is carrying the money it needs to buy a witness off, what the band has asked for and not
    /// got. Those are exactly the numbers that decide whether any of this machinery does anything, and on 18.09.2026
    /// they were being guessed at from the absence of log lines.
    /// </para>
    /// </summary>
    private static string Band()
    {
        if (!BotUnderworld.Exists)
        {
            return "There is no band: The Shadow has not been founded.";
        }

        var say = ValueStringBuilder.Create(2048);

        try
        {
            say.Append(BotUnderworld.Describe());

            var chest = BotLair.Chest;

            if (chest == null)
            {
                say.Append(" — and no chest at all.");
            }
            else
            {
                var at = chest.GetWorldLocation();
                Dictionary<string, int> tally = [];
                var coin = 0;

                for (var i = 0; i < chest.Items.Count; i++)
                {
                    if (chest.Items[i] is not { Deleted: false } item)
                    {
                        continue;
                    }

                    if (item is Gold gold)
                    {
                        coin += gold.Amount;

                        continue;
                    }

                    var name = item.GetType().Name;
                    tally.TryGetValue(name, out var had);
                    tally[name] = had + Math.Max(1, item.Amount);
                }

                say.Append($" — the chest at ({at.X}, {at.Y}) holds {coin}gp");

                foreach (var (name, many) in tally)
                {
                    say.Append($", {many} {name}");
                }

                if (coin == 0 && tally.Count == 0)
                {
                    say.Append(" and nothing else at all");
                }
            }

            var wants = BotFence.Wants();

            if (wants.Count == 0)
            {
                say.Append(". The band's order is filled");
            }
            else
            {
                say.Append(". The band still wants");

                foreach (var (kind, many) in wants)
                {
                    say.Append($" {many} {kind.Name},");
                }
            }

            if (BotFence.Who is { Deleted: false } fence)
            {
                say.Append(
                    $" and {fence.Name} carries {fence.Backpack?.GetAmount(typeof(Gold)) ?? 0}gp of hush money at ({fence.X}, {fence.Y}), doing {fence.Resolve?.Deed?.Kind ?? "nothing"}"
                );
            }

            say.Append('.');

            return say.ToString();
        }
        finally
        {
            say.Dispose();
        }
    }

    private static string Pays(string tail)
    {
        var kind = (tail ?? "").Trim();
        var best = BotCommons.Best(kind.Length == 0 ? 16 : 200);
        var say = ValueStringBuilder.Create(1024);

        try
        {
            var shown = 0;

            for (var i = 0; i < best.Count && shown < 16; i++)
            {
                var (trade, _, where, perMinute, settled, minded) = best[i];

                if (kind.Length > 0 && !trade.InsensitiveEquals(kind))
                {
                    continue;
                }

                if (shown > 0)
                {
                    say.Append("; ");
                }

                say.Append(trade);
                say.Append(" at (");
                say.Append(where.X);
                say.Append(", ");
                say.Append(where.Y);
                say.Append(") pays ");
                say.Append((int)perMinute);
                say.Append("/min on ");
                say.Append(settled);
                say.Append(" outcomes");

                if (minded > 0)
                {
                    say.Append(", ");
                    say.Append(minded);
                    say.Append(" of them minded");
                }

                shown++;
            }

            if (shown == 0)
            {
                return kind.Length == 0
                    ? "the population has not found out what pays where yet."
                    : $"nothing is known yet about what {kind} pays anywhere.";
            }

            return $"what pays where, best first ({BotCommons.Describe()}): {say.ToString()}.";
        }
        finally
        {
            say.Dispose();
        }
    }

    /// <summary>Each trade's claim against what it paid, worst overstatement first. Read off <see cref="BotCommons.Gaps"/>.</summary>
    private static string Gaps()
    {
        var gaps = BotCommons.Gaps(24);

        if (gaps.Count == 0)
        {
            return "no trade has been measured against its own claim yet.";
        }

        var say = ValueStringBuilder.Create(1024);

        try
        {
            for (var i = 0; i < gaps.Count; i++)
            {
                var (kind, claimed, measured, settled, minded) = gaps[i];

                if (i > 0)
                {
                    say.Append("; ");
                }

                say.Append(kind);
                say.Append(" claims ");
                say.Append((int)claimed);
                say.Append("/min and pays ");
                say.Append((int)measured);
                say.Append("/min over ");
                say.Append(settled);

                if (minded > 0)
                {
                    say.Append(" (");
                    say.Append(minded);
                    say.Append(" minded)");
                }

                // Red on the dashboard by the same rule: more than half again what the work pays, on enough outcomes
                // to be believed. See BotCommons.TradeConfidence.
                if (settled >= BotCommons.TradeConfidence && measured * 1.5 < claimed)
                {
                    say.Append(" — OVERSTATED");
                }
            }

            return $"each trade's claim against what it paid, worst overstatement first: {say.ToString()}.";
        }
        finally
        {
            say.Dispose();
        }
    }

    private static string Near(string tail)
    {
        var map = BotPopulation.Home;

        if (map == null || map == Map.Internal)
        {
            return "the population has no map to look at.";
        }

        var (xs, rest) = First(tail);
        var (ys, _) = First(rest);

        if (!int.TryParse(xs, out var x) || !int.TryParse(ys, out var y))
        {
            Refused++;

            return "near wants two numbers: near <x> <y>.";
        }

        BotStep.Settle(map, x, y, out var z);

        var at = new Point3D(x, y, z);
        var say = ValueStringBuilder.Create(512);
        var seen = 0;

        try
        {
            foreach (var who in map.GetMobilesInRange<Mobile>(at, NearReach))
            {
                if (who == null || who.Deleted || !who.Alive)
                {
                    continue;
                }

                if (seen++ >= 12)
                {
                    say.Append(", and more");

                    break;
                }

                if (seen > 1)
                {
                    say.Append("; ");
                }

                var beast = who as BaseCreature;
                var hostile = beast is { Controlled: false } && who.Karma < 0;

                say.Append(who.GetType().Name);
                say.Append(' ');
                say.Append(who.Name ?? "unnamed");
                say.Append(" at (");
                say.Append(who.X);
                say.Append(", ");
                say.Append(who.Y);
                say.Append(") ");
                say.Append(who.Hits);
                say.Append('/');
                say.Append(who.HitsMax);
                say.Append(hostile ? " HOSTILE" : beast != null ? " harmless" : " one of ours");
            }

            return seen == 0
                ? $"nothing alive within {NearReach} tiles of ({x}, {y})."
                : $"within {NearReach} tiles of ({x}, {y}): {say.ToString()}.";
        }
        finally
        {
            say.Dispose();
        }
    }

    /// <summary>How far around a spot the near verb looks. A camp is about a dozen tiles across.</summary>
    public static int NearReach { get; set; } = 14;

    /// <summary>How long a camp put down by hand lasts. The engine's own five minutes is shorter than the walk to it.</summary>
    public static int CampMinutes { get; set; } = 45;

    /// <summary>Camps put on the ground.</summary>
    public static long Camps { get; private set; }

    /// <summary>Bots brought to where the watcher stands.</summary>
    public static long Summoned { get; private set; }

    /// <summary>Times Patrick was called over.</summary>
    public static long Called { get; private set; }

    /// <summary>
    /// Brings one bot to where the watcher is standing, so it can be watched doing whatever it is doing.
    ///
    /// The same lift <see cref="Tele"/> makes and with the same warning: it is not travel, nothing prices it,
    /// and a bot moved this way has learned nothing about the road it did not walk.
    /// </summary>
    /// <summary>
    /// Sends a bot walking to a point by the ordinary walk — gates, moongates and all — as its next work (29.09.2026): the way
    /// to prove a road on demand instead of waiting for a traveller to choose it. "go &lt;bot&gt; &lt;x&gt; &lt;y&gt;".
    /// </summary>
    private static string Go(BotMobile bot, string rest)
    {
        var words = (rest ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries);

        if (words.Length < 2 || !int.TryParse(words[0], out var x) || !int.TryParse(words[1], out var y))
        {
            return "go wants a bot and a place: go <bot> <x> <y>.";
        }

        var map = bot.Map;

        if (map == null || map == Map.Internal)
        {
            return $"{bot.Name} is not in the world.";
        }

        var goal = BotStep.Settle(map, x, y, out var z) ? new Point3D(x, y, z) : new Point3D(x, y, map.GetAverageZ(x, y));

        return BotWill.Press(bot, new BotHomeward(map, goal), $"sent by hand to ({x}, {y})")
            ? $"{bot.Name} is walking from ({bot.X}, {bot.Y}) to ({goal.X}, {goal.Y}, {goal.Z}); its route is drawn by the ordinary walk, gates and moongates included."
            : $"{bot.Name} would not take the walk.";
    }

    private static string Summon(BotMobile bot)
    {
        var here = BotVigil.Body;
        var map = here?.Map;

        if (map == null || map == Map.Internal)
        {
            return "I am nowhere, so there is nowhere to bring it.";
        }

        var was = bot.Location;

        for (var attempt = 0; attempt < 16; attempt++)
        {
            var x = here.X + Utility.RandomMinMax(-2, 2);
            var y = here.Y + Utility.RandomMinMax(-2, 2);

            if (!map.CanSpawnMobile(x, y, here.Z - 8, here.Z + 8, false, false, out var z))
            {
                continue;
            }

            bot.MoveToWorld(new Point3D(x, y, z), map);
            bot.Journey?.Finish();
            Summoned++;

            return $"{bot.Name} is beside me now, lifted from ({was.X}, {was.Y}); its errand has been let go.";
        }

        Refused++;

        return $"there is no room beside me for {bot.Name}.";
    }

    /// <summary>
    /// Brings Patrick to the watcher, when he is logged in and there is something worth his own eyes.
    ///
    /// <para>
    /// <b>Only a real person, and only one who is already here.</b> The population is made of PlayerMobiles
    /// — that is what lets a bot die properly — so "a player" is not a test. What is a test is a network
    /// connection: bots have none. Nothing is summoned, nothing is created; somebody already playing is
    /// moved a few tiles, and told why.
    /// </para>
    /// </summary>
    /// <summary>
    /// Declares a revel by hand, on the same terms the model gets: the trade must be one the ledger knows,
    /// and the prize comes out of the same treasury.
    ///
    /// It exists because the watcher thinks about this once a quarter of an hour, and a thing that can only
    /// be watched every fifteen minutes cannot be debugged at all.
    /// </summary>
    private static string Revelry(string tail)
    {
        var (kind, rest) = First(tail);

        if (string.IsNullOrWhiteSpace(kind))
        {
            return $"revel wants a trade: one of {string.Join(", ", BotRevel.Trades)}.";
        }

        if (!BotRevel.Known(kind))
        {
            return $"\"{kind}\" is not a trade this shard measures. It knows: {string.Join(", ", BotRevel.Trades)}.";
        }

        if (BotRevel.Running)
        {
            return $"the {BotRevel.Kind} revel is still running; one at a time.";
        }

        // "revel mine 500 1450 1500" — the prize, then optionally where the camp stands. Parsed
        // positionally because that is how every other verb on this console reads its tail, and a spot
        // without a camp is accepted and ignored rather than refused: the hand-called revel raises none.
        var parts = (rest ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var prize = parts.Length > 0 && int.TryParse(parts[0], out var asked) ? asked : BotRevel.MostPrize / 2;
        var camp = parts.Length >= 3;
        var spot = camp && int.TryParse(parts[1], out var sx) && int.TryParse(parts[2], out var sy)
            ? new Point3D(sx, sy, 0)
            : Point3D.Zero;

        return BotRevel.Declare(
            kind,
            prize,
            $"A {kind} revel! Whoever does most of it before the hour is out takes the purse.",
            "called by hand, from the console",
            spot != Point3D.Zero,
            spot,
            exact: true
        );
    }

    private static string Call()
    {
        var here = BotVigil.Body;
        var map = here?.Map;

        if (map == null || map == Map.Internal)
        {
            return "I am nowhere, so there is nowhere to call anybody to.";
        }

        foreach (var state in NetState.Instances)
        {
            var who = state?.Mobile;

            if (who == null || who.Deleted || who is BotMobile or BotDebugger || who.Map == Map.Internal)
            {
                continue;
            }

            who.MoveToWorld(here.Location, map);
            who.SendMessage($"{here.Name} has called you over: {BotDebugMemory.Recite() ?? "come and look at this"}");
            Called++;

            return $"{who.Name} is standing beside me now and has been told why.";
        }

        Refused++;

        return "nobody is on the shard to call.";
    }

    /// <summary>The engine's own view of a bot, which is what every argument about one eventually needs.</summary>
    /// <summary>The skill this bot's class is actually measured in, and where it has got to.</summary>
    private static string Trade(BotMobile bot)
    {
        var wanted = bot.Class?.Skills;

        if (wanted == null || wanted.Count == 0)
        {
            return "no trade skill";
        }

        var first = wanted[0];

        return $"{first.Skill} {bot.Skills[first.Skill].Base:F0}/{first.Target:F0}";
    }

    private static string Props(BotMobile bot)
    {
        var weapon = bot.Weapon as BaseWeapon;
        var region = bot.Map == null ? null : Region.Find(bot.Location, bot.Map);

        return
            $"{bot.Name} the {bot.Class?.Name ?? "bot"}: {bot.Hits}/{bot.HitsMax} hits, {bot.Stam}/{bot.StamMax} stamina, "
            + $"{bot.Mana}/{bot.ManaMax} mana; str {bot.Str} dex {bot.Dex} int {bot.Int}; "
            + $"holding {(weapon == null ? "nothing" : weapon.GetType().Name)} reaching {weapon?.MaxRange ?? 0}; "
            + $"warmode {bot.Warmode}, fighting {bot.Combatant?.Name ?? "nobody"}; "
            + $"{bot.TotalWeight} of {bot.MaxWeight} stones; alive {bot.Alive}, frozen {bot.Frozen}; "
            + $"at ({bot.X}, {bot.Y}, {bot.Z}) on {bot.Map} in {region?.Name ?? "no named region"}; "
            // Its trade's own skill and the two everybody has. A claim that a population "knows how to
            // appraise" is a claim until something reads it back off a living bot, and nothing here did.
            + $"skills: {Trade(bot)}, ItemID {bot.Skills[SkillName.ItemID].Base:F0}; "
            + $"rank {bot.BotRank ?? "none"}; "
            + $"guild {(bot.Guild == null ? "none" : $"{bot.Guild.Name} [{bot.Guild.Abbreviation}]")}.";
    }

    /// <summary>
    /// The one question that decides whether a bot standing next to something is fighting it or watching it.
    ///
    /// <para>
    /// The engine gates every swing on <c>InLOS</c> and returns without so much as advancing the swing clock
    /// when the line is broken — see <c>Mobile.CheckCombatTime</c>. So "adjacent" and "able to hit" are two
    /// different facts, and until 03.09.2026 nothing on this shard could tell them apart from the outside.
    /// </para>
    /// </summary>
    private static string Sight(BotMobile bot)
    {
        var foe = bot.Combatant;

        if (foe == null)
        {
            return $"{bot.Name} is not fighting anything.";
        }

        var weapon = bot.Weapon as BaseWeapon;
        var away = (int)bot.GetDistanceToSqrt(foe.Location);
        var los = bot.InLOS(foe);
        var lawful = bot.CanBeHarmful(foe, false);
        var range = weapon?.MaxRange ?? 1;

        var verdict = (away <= range, los, lawful) switch
        {
            (false, _, _) => $"too far: {away} tiles against a reach of {range}",
            (true, false, _) => "near enough and no line to it — the engine will not let the blow leave",
            (true, true, false) => "near enough and in plain sight, and the engine refuses the blow",
            _ => "near enough, in sight, and swinging"
        };

        return $"{bot.Name} against {foe.Name} at ({foe.X}, {foe.Y}, {foe.Z}): {verdict}.";
    }

    /// <summary>
    /// Everything in a bot's pack, beside what it is allowed to keep of each.
    ///
    /// <para>
    /// <b>Written because the question could not be asked.</b> Patrick found a bot carrying ten sewing kits
    /// and eight heal potions on 09.09.2026, and there was no way to see a pack from outside the shard at
    /// all: <c>props</c> gives the weight and not what makes it up, and the table that decides what a bot
    /// keeps lived private in one file. The cause turned out to be two lines of <c>int.MaxValue</c> where a
    /// count belonged - and the only reason it survived a month is that nothing could print it.
    /// </para>
    ///
    /// <para>
    /// The surplus is marked rather than the total, because the total is not the interesting half. A pack
    /// holding forty cloth is fine if forty is what the trade eats and a fault if the allowance is five, and
    /// the two look identical in any list that gives only counts.
    /// </para>
    /// </summary>
    private static string Packed(BotMobile bot)
    {
        var pack = bot.Backpack;

        if (pack == null)
        {
            return $"{bot.Name} has no pack at all, which is a fault in itself.";
        }

        var keep = BotUnload.Keeps(bot);
        Dictionary<Type, int> held = [];

        for (var i = 0; i < pack.Items.Count; i++)
        {
            var item = pack.Items[i];

            if (item == null || item.Deleted)
            {
                continue;
            }

            // <b>The same two exclusions the sale itself makes, and leaving them out made this lie on its
            // first run.</b> It reported "Gold x340 (keeps 0, 340 surplus)" and a surplus spellbook on a
            // mage - neither of which BotUnload would ever touch, because Sell skips coin outright and
            // BotBinding protects what a bot is bound to. An instrument that reports surplus the seller
            // will never take is an instrument that raises an alarm about nothing, which is the failure
            // this shard has paid for more often than any other.
            if (item is Gold || BotBinding.IsBound(item, bot.Bond))
            {
                continue;
            }

            var kind = item.GetType();

            held[kind] = (held.TryGetValue(kind, out var many) ? many : 0) + Math.Max(1, item.Amount);
        }

        if (held.Count == 0)
        {
            return $"{bot.Name} is carrying nothing it could part with, at {BotLadder.Load(bot)} of {BotLadder.Ceiling(bot)} stones.";
        }

        List<string> lines = [];
        var over = 0;

        foreach (var (kind, many) in held)
        {
            var allowed = keep.TryGetValue(kind, out var cap) ? cap : 0;

            // A kind with no entry is merchandise outright, and saying "keeps 0" of it reads as a fault
            // rather than as the ordinary state of a thing a bot is carrying to sell.
            if (allowed >= int.MaxValue)
            {
                lines.Add($"{kind.Name} x{many} (kept without limit)");

                continue;
            }

            if (many > allowed)
            {
                over++;
                lines.Add($"{kind.Name} x{many} (keeps {allowed}, {many - allowed} surplus)");
            }
            else
            {
                lines.Add($"{kind.Name} x{many} (keeps {allowed})");
            }
        }

        lines.Sort(StringComparer.Ordinal);

        return $"{bot.Name} at {BotLadder.Load(bot)} of {BotLadder.Ceiling(bot)} stones, "
               + $"{held.Count} kinds and {over} of them over the allowance: {string.Join("; ", lines)}";
    }

    private static string Whereabouts(BotMobile bot)
    {
        var map = bot.Map;
        var region = map == null ? null : Region.Find(bot.Location, map);
        var crowd = 0;

        if (map != null && map != Map.Internal)
        {
            foreach (var other in map.GetMobilesInRange<Mobile>(bot.Location, 2))
            {
                if (other != bot && !other.Deleted)
                {
                    crowd++;
                }
            }
        }

        var standable = map != null && BotStep.Ground(map, bot.X, bot.Y, bot.Z, BotStep.StandingReach, out _);

        return
            $"{bot.Name} at ({bot.X}, {bot.Y}, {bot.Z}) on {map} in {region?.Name ?? "no named region"}; "
            + $"{crowd} others within 2 tiles; the tile itself {(standable ? "holds a body" : "does not hold a body")}; "
            + $"{(BotPopulation.Within(map, bot.Location) ? "inside" : "outside")} the ground the population may want anything on.";
    }

    /// <summary>What is on a tile and whether anybody could be. The question behind every pocket on this shard.</summary>
    /// <summary>
    /// <c>seat &lt;guild&gt; &lt;x&gt; &lt;y&gt;</c>: the guild by name, with or without its "The", and the
    /// last two words the place. See <c>BotSeat.Set</c>.
    /// </summary>
    private static string Seat(string tail)
    {
        var words = (tail ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries);

        if (words.Length < 3 || !int.TryParse(words[^2], out var x) || !int.TryParse(words[^1], out var y))
        {
            Refused++;

            return "seat wants a guild and two numbers: seat <guild> <x> <y>.";
        }

        var named = string.Join(' ', words, 0, words.Length - 2);
        Server.Guilds.Guild found = null;

        foreach (var guild in BotGuilds.Standing)
        {
            if (string.Equals(guild.Name, named, StringComparison.OrdinalIgnoreCase)
                || string.Equals(BotClaim.Short(guild.Name), named.ToUpperInvariant(), StringComparison.Ordinal))
            {
                found = guild;

                break;
            }
        }

        if (found == null)
        {
            Refused++;

            return $"there is no guild called \"{named}\". Standing: {BotSeat.Tell()}";
        }

        var map = BotPopulation.Home;

        if (map == null || !BotStep.Settle(map, x, y, out var z))
        {
            Refused++;

            return $"no body could stand at {x},{y}; a seat has to be ground somebody can be put down on.";
        }

        var seat = new Point3D(x, y, z);

        // Not beside another guild's seat, however it is asked for. See BotSeat.TooNear.
        if (BotSeat.TooNear(found.Name, seat, out var other, out var gap))
        {
            Refused++;

            return $"{x},{y} is {gap} tiles from the seat of {other}, and guilds nearer than {BotRegard.Neighbouring} sour on each other; choose ground further off. {BotSeat.Tell()}";
        }

        if (BotSeat.Roadless(seat, out var roadless))
        {
            Refused++;
            return $"{roadless}; a seat has to be ground with a road from home, or everybody put down there is carried home. {BotSeat.Tell()}";
        }

        // Not on ground another guild holds, however it is asked for (06.10.2026). See BotBounds.
        if (BotBounds.Foreign(found.Name, map, seat, out var holder))
        {
            Refused++;
            BotBounds.SeatRefused();

            return $"{x},{y} lies on {holder}'s held ground, and no guild's seat may stand on another guild's ground; choose ground of its own or open ground. {BotSeat.Tell()}";
        }

        BotSeat.Set(found.Name, seat);

        return $"the seat of {found.Name} is {x},{y} now. {BotSeat.Tell()}";
    }

    /// <summary>The zones in a paragraph, or the tour switched: <c>zones</c>, <c>zones tour on</c>, <c>zones tour off</c>.</summary>
    private static string Zones(string tail)
    {
        var words = tail.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        if (words.Length >= 1 && words[0].Equals("tour", StringComparison.OrdinalIgnoreCase))
        {
            if (words.Length >= 2 && (words[1].Equals("on", StringComparison.OrdinalIgnoreCase) || words[1].Equals("off", StringComparison.OrdinalIgnoreCase)))
            {
                BotZoneTour.Running = words[1].Equals("on", StringComparison.OrdinalIgnoreCase);

                return $"the tour is {(BotZoneTour.Running ? "on: the free debuggers walk the least-watched zones" : "off: the debuggers go back to the bots, and the ground they held sleeps after its grace")}. {BotZoneTour.Describe()}.";
            }

            return $"the tour: {BotZoneTour.Describe()}. Say zones tour on or zones tour off.";
        }

        return BotZones.Tell();
    }

    /// <summary>
    /// The reach map at the door: <c>reach</c> for the map and its check, <c>reach 1432 1690</c> for a tile (the map's word and
    /// the walker's own), <c>reach check 200</c> to examine the borders, <c>reach check</c> for the tally and the misses,
    /// <c>reach redraw 1432 1690 40</c> to draw a square again, <c>reach carry on|off</c>.
    /// </summary>
    private static string ReachMap(string tail)
    {
        var words = tail.Split(new[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries);

        if (words.Length == 0)
        {
            return BotReachMap.Tell();
        }

        if (words[0].Equals("check", StringComparison.OrdinalIgnoreCase))
        {
            if (words.Length >= 2 && int.TryParse(words[1], out var n))
            {
                return BotReachCheck.Start(n, "the door");
            }

            var misses = BotReachCheck.Report.Misses;

            return misses.Length == 0
                ? $"{BotReachCheck.Describe()}."
                : $"{BotReachCheck.Describe()}; the misses: {string.Join("; ", misses.Take(12))}.";
        }

        if (words[0].Equals("redraw", StringComparison.OrdinalIgnoreCase))
        {
            if (words.Length < 3 || !int.TryParse(words[1], out var rx) || !int.TryParse(words[2], out var ry))
            {
                Refused++;

                return "reach redraw wants a tile and perhaps a radius: reach redraw 1432 1690 40.";
            }

            var r = words.Length > 3 && int.TryParse(words[3], out var rr) ? Math.Clamp(rr, 0, 256) : 16;

            return $"{BotReachMap.Redraw(rx, ry, r)} clusters round ({rx}, {ry}) are drawn again, and the map solved after.";
        }

        if (words[0].Equals("carry", StringComparison.OrdinalIgnoreCase))
        {
            BotReachCheck.Carry = words.Length < 2 || !words[1].Equals("off", StringComparison.OrdinalIgnoreCase);

            return $"a debugger {(BotReachCheck.Carry ? "stands" : "no longer stands")} on each tile the check examines.";
        }

        if (words.Length < 2 || !int.TryParse(words[0], out var x) || !int.TryParse(words[1], out var y))
        {
            Refused++;

            return "reach wants nothing, a tile, or check / redraw / carry: reach | reach 1432 1690 | reach check 200 | reach redraw 1432 1690 40.";
        }

        return $"{BotReachMap.TellAt(x, y)} {BotReachCheck.Probe(x, y)}";
    }

    /// <summary>What the zones say about one tile: <c>zone 1440 1690</c>.</summary>
    private static string Zone(string tail)
    {
        var words = tail.Split(new[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries);

        if (words.Length < 2 || !int.TryParse(words[0], out var x) || !int.TryParse(words[1], out var y))
        {
            Refused++;

            return "zone wants a tile: zone 1440 1690.";
        }

        return BotZones.TellAt(x, y);
    }

    /// <summary>
    /// <c>road &lt;x1&gt; &lt;y1&gt; &lt;x2&gt; &lt;y2&gt;</c>: the pathfinder's own answer between two tiles, with a
    /// clock five times the ordinary ceiling, so "nobody can get into the graveyard" can be asked of the
    /// thing that decides it rather than inferred from a hundred failed quarrels.
    /// </summary>
    /// <summary>
    /// What the shard knows about the danger of one place: the death map, which learns from the boot, and the quadrant
    /// record, which survives a restart. Written for the plague beast's field north-east of Britain, which killed nine bots
    /// between 05:25 and 05:37 on build 49 on 15.09.2026 while nothing on this console could say what either map held
    /// about it.
    /// </summary>
    private static string Peril(string tail)
    {
        var words = (tail ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries);

        if (words.Length < 2 || !int.TryParse(words[0], out var x) || !int.TryParse(words[1], out var y))
        {
            Refused++;

            return "peril wants two numbers: peril <x> <y>.";
        }

        var map = BotPopulation.Home;

        if (map == null)
        {
            return "the population has no map, so there is no danger to read.";
        }

        var where = new Point3D(x, y, 0);

        return $"{BotPeril.Tell(map, where)}; {BotQuad.Tell(map, where)}.";
    }

    /// <summary>
    /// The coarse chart (BotChart): <c>chart</c> for its state; <c>chart Ysolt 1084 1406</c> or <c>chart x1 y1 x2 y2</c> for
    /// the route it gives, and whether the planner can walk the first legs of it.
    /// </summary>
    private static string Chart(string tail)
    {
        var words = (tail ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries);

        if (words.Length == 0)
        {
            return BotChart.Describe() + ".";
        }

        Map map;
        Point3D from;
        int gx;
        int gy;

        if (words.Length >= 3 && !int.TryParse(words[0], out _) && int.TryParse(words[^2], out gx) && int.TryParse(words[^1], out gy))
        {
            var who = Find(string.Join(' ', words[..^2]));

            if (who?.Map == null || who.Map == Map.Internal)
            {
                Refused++;

                return $"there is no bot called \"{string.Join(' ', words[..^2])}\" standing anywhere.";
            }

            map = who.Map;
            from = who.Location;
        }
        else if (words.Length >= 4 && int.TryParse(words[0], out var x1) && int.TryParse(words[1], out var y1)
            && int.TryParse(words[2], out gx) && int.TryParse(words[3], out gy))
        {
            map = BotPopulation.Home;

            if (map == null || map == Map.Internal)
            {
                return "the population has no home map yet.";
            }

            from = BotStep.Settle(map, x1, y1, out var fz) ? new Point3D(x1, y1, fz) : new Point3D(x1, y1, map.GetAverageZ(x1, y1));
        }
        else
        {
            Refused++;

            return "chart wants nothing, a bot and two numbers, or four numbers: chart | chart <bot> <x> <y> | chart <x1> <y1> <x2> <y2>.";
        }

        var goal = BotStep.Settle(map, gx, gy, out var gz) ? new Point3D(gx, gy, gz) : new Point3D(gx, gy, map.GetAverageZ(gx, gy));
        var points = new List<Point3D>();
        var expanded = BotChart.Expanded;
        var t0 = System.Diagnostics.Stopwatch.GetTimestamp();
        var found = BotChart.Route(map, from, goal, points);
        var took = (System.Diagnostics.Stopwatch.GetTimestamp() - t0) * 1000.0 / System.Diagnostics.Stopwatch.Frequency;

        if (!found)
        {
            return $"from {from} to {goal}: no route ({BotChart.Describe()}), in {took:F2}ms.";
        }

        var length = 0;
        var last = from;

        for (var i = 0; i < points.Count; i++)
        {
            length += Math.Max(Math.Abs(points[i].X - last.X), Math.Abs(points[i].Y - last.Y));
            last = points[i];
        }

        length += Math.Max(Math.Abs(goal.X - last.X), Math.Abs(goal.Y - last.Y));

        // The first legs, walked by the planner as the walker would walk them.
        var legs = new List<string>();
        var leg = from;
        var plan = new List<Point3D>();

        for (var i = 0; i < points.Count && i < 5; i++)
        {
            var l0 = System.Diagnostics.Stopwatch.GetTimestamp();
            var walked = BotPath.Find(map, leg, points[i], BotArrival.Within(1), plan);
            var lms = (System.Diagnostics.Stopwatch.GetTimestamp() - l0) * 1000.0 / System.Diagnostics.Stopwatch.Frequency;

            legs.Add($"to ({points[i].X},{points[i].Y},{points[i].Z}) {walked} in {lms:F1}ms");
            leg = points[i];
        }

        var shown = new List<string>();

        for (var i = 0; i < points.Count && i < 12; i++)
        {
            shown.Add($"({points[i].X},{points[i].Y})");
        }

        return $"from {from} to {goal}: {points.Count} points, about {length} steps over the chart for "
            + $"{Math.Max(Math.Abs(goal.X - from.X), Math.Abs(goal.Y - from.Y))} straight, {BotChart.Expanded - expanded} nodes "
            + $"expanded in {took:F2}ms; first points {string.Join(" ", shown)}{(points.Count > 12 ? " …" : "")}; legs: {string.Join("; ", legs)}.";
    }

    /// <summary>The gates near a point: <c>gates 5134 1946</c> or <c>gates 5134 1946 40</c>. See <see cref="BotGates.Near"/>.</summary>
    private static string Gates(string tail)
    {
        var words = (tail ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var map = BotPopulation.Home;

        if (words.Length == 0)
        {
            return BotGates.Describe() + ".";
        }

        // gates route x1 y1 x2 y2: the whole chain of gates a walk from one point to the other would take (build 330).
        if (words[0] == "route" && words.Length >= 5 && int.TryParse(words[1], out var rx1) && int.TryParse(words[2], out var ry1)
            && int.TryParse(words[3], out var rx2) && int.TryParse(words[4], out var ry2) && map != null && map != Map.Internal)
        {
            var za = BotStep.Settle(map, rx1, ry1, out var fza) ? fza : map.GetAverageZ(rx1, ry1);
            var zb = BotStep.Settle(map, rx2, ry2, out var fzb) ? fzb : map.GetAverageZ(rx2, ry2);

            return BotGates.Chain(map, new Point3D(rx1, ry1, za), new Point3D(rx2, ry2, zb)) + ".";
        }

        if (words.Length < 2 || !int.TryParse(words[0], out var x) || !int.TryParse(words[1], out var y) || map == null || map == Map.Internal)
        {
            Refused++;

            return "gates wants nothing, or a point and perhaps a radius: gates | gates <x> <y> [radius].";
        }

        var radius = words.Length >= 3 && int.TryParse(words[2], out var r) ? r : 30;
        var z = BotStep.Settle(map, x, y, out var fz) ? fz : map.GetAverageZ(x, y);

        return BotGates.Near(map, new Point3D(x, y, z), radius) + ".";
    }

    /// <summary>
    /// The tiered navigation (NavigationService): <c>nav</c> for its state; <c>nav Ysolt 1084 1406</c> or
    /// <c>nav x1 y1 x2 y2</c> for the route the graph gives, and whether the planner walks its first legs.
    /// </summary>
    private static string Nav(string tail)
    {
        var words = (tail ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries);

        if (words.Length == 0)
        {
            return Server.Engines.Pathing.Tiered.NavigationService.Describe() + ".";
        }

        Map map;
        Point3D from;
        int gx;
        int gy;

        if (words.Length >= 3 && !int.TryParse(words[0], out _) && int.TryParse(words[^2], out gx) && int.TryParse(words[^1], out gy))
        {
            var who = Find(string.Join(' ', words[..^2]));

            if (who?.Map == null || who.Map == Map.Internal)
            {
                Refused++;

                return $"there is no bot called \"{string.Join(' ', words[..^2])}\" standing anywhere.";
            }

            map = who.Map;
            from = who.Location;
        }
        else if (words.Length >= 4 && int.TryParse(words[0], out var x1) && int.TryParse(words[1], out var y1)
            && int.TryParse(words[2], out gx) && int.TryParse(words[3], out gy))
        {
            map = BotPopulation.Home;

            if (map == null || map == Map.Internal)
            {
                return "the population has no home map yet.";
            }

            from = BotStep.Settle(map, x1, y1, out var fz) ? new Point3D(x1, y1, fz) : new Point3D(x1, y1, map.GetAverageZ(x1, y1));
        }
        else
        {
            Refused++;

            return "nav wants nothing, a bot and two numbers, or four numbers: nav | nav <bot> <x> <y> | nav <x1> <y1> <x2> <y2>.";
        }

        var goal = BotStep.Settle(map, gx, gy, out var gz) ? new Point3D(gx, gy, gz) : new Point3D(gx, gy, map.GetAverageZ(gx, gy));
        var points = new List<Point3D>();
        var t0 = System.Diagnostics.Stopwatch.GetTimestamp();
        var status = Server.Engines.Pathing.Tiered.NavigationService.Route(map, from, goal, points);
        var took = (System.Diagnostics.Stopwatch.GetTimestamp() - t0) * 1000.0 / System.Diagnostics.Stopwatch.Frequency;
        var planner = Server.Engines.Pathing.Tiered.NavigationService.Medium;

        if (status != Server.Engines.Pathing.Tiered.NavStatus.Ok)
        {
            return $"from {from} to {goal}: {status}, cost {planner.LastCost}, {planner.LastExpanded} nodes expanded in {took:F2}ms"
                + (status == Server.Engines.Pathing.Tiered.NavStatus.Unreachable
                    ? $"; {Server.Engines.Pathing.Tiered.NavigationService.Explain(map, from, goal)}"
                    : "")
                + ".";
        }

        var legs = new List<string>();
        var leg = from;
        var plan = new List<Point3D>();

        for (var i = 0; i < points.Count && i < 5; i++)
        {
            var l0 = System.Diagnostics.Stopwatch.GetTimestamp();
            var walked = BotPath.Find(map, leg, points[i], BotArrival.Within(1), plan);
            var lms = (System.Diagnostics.Stopwatch.GetTimestamp() - l0) * 1000.0 / System.Diagnostics.Stopwatch.Frequency;

            legs.Add($"to ({points[i].X},{points[i].Y},{points[i].Z}) {walked} in {lms:F1}ms");
            leg = points[i];
        }

        var shown = new List<string>();

        for (var i = 0; i < points.Count && i < 12; i++)
        {
            shown.Add($"({points[i].X},{points[i].Y},{points[i].Z})");
        }

        return $"from {from} to {goal}: {points.Count} points, cost {planner.LastCost} ({planner.LastCost / 100} steps) for "
            + $"{Math.Max(Math.Abs(goal.X - from.X), Math.Abs(goal.Y - from.Y))} straight, {planner.LastExpanded} nodes expanded "
            + $"in {took:F2}ms; first points {string.Join(" ", shown)}{(points.Count > 12 ? " \u2026" : "")}; legs: {string.Join("; ", legs)}.";
    }

    /// <summary>
    /// <c>navcells x y r</c>: every step the engine allows within r tiles of (x, y) whose landing height matches none of the
    /// next cell's standable surfaces within the navigation graph's tolerance — the steps the graph drops — with the gap.
    /// </summary>
    private static string NavCells(string tail)
    {
        var words = (tail ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var map = BotPopulation.Home;

        if (map == null || words.Length < 2 || !int.TryParse(words[0], out var cx) || !int.TryParse(words[1], out var cy))
        {
            return "navcells wants x y and a radius.";
        }

        var r = words.Length > 2 && int.TryParse(words[2], out var rr) ? Math.Clamp(rr, 0, 30) : 8;
        Span<sbyte> zs = stackalloc sbyte[16];
        Span<sbyte> nz = stackalloc sbyte[16];
        var steps = 0;
        var dropped = 0;
        var gaps = new Dictionary<int, int>();
        var shown = new List<string>();
        int[] dx = [0, 1, 1, 1, 0, -1, -1, -1];
        int[] dy = [-1, -1, 0, 1, 1, 1, 0, -1];

        for (var y = cy - r; y <= cy + r; y++)
        {
            for (var x = cx - r; x <= cx + r; x++)
            {
                var n = Server.Engines.Pathing.Cache.StepProbe.ComputeStandableSurfaceZs(map, x, y, zs);

                for (var s = 0; s < n; s++)
                {
                    var mask = Server.Engines.Pathing.Cache.StepProbe.ComputeMaskAt(map, x, y, zs[s]);

                    for (var d = 0; d < 8; d++)
                    {
                        if ((mask.WalkMask & (1 << d)) == 0)
                        {
                            continue;
                        }

                        steps++;

                        var land = mask.GetWalkZ((Direction)d);
                        var m = Server.Engines.Pathing.Cache.StepProbe.ComputeStandableSurfaceZs(map, x + dx[d], y + dy[d], nz);
                        var best = int.MaxValue;

                        for (var t = 0; t < m; t++)
                        {
                            best = Math.Min(best, Math.Abs(nz[t] - land));
                        }

                        if (best <= Server.Engines.Pathing.Tiered.NavWindow.MatchTolerance)
                        {
                            continue;
                        }

                        dropped++;

                        var key = best == int.MaxValue ? -1 : best;

                        gaps[key] = gaps.GetValueOrDefault(key) + 1;

                        if (shown.Count < 8)
                        {
                            var list = new List<string>();

                            for (var t = 0; t < m; t++)
                            {
                                list.Add(nz[t].ToString());
                            }

                            shown.Add($"({x},{y},{zs[s]}) {(Direction)d} lands at {land}, next cell stands at [{string.Join(",", list)}]");
                        }
                    }
                }
            }
        }

        var byGap = new List<string>();

        foreach (var (gap, count) in gaps)
        {
            byGap.Add($"{(gap < 0 ? "no surface" : gap.ToString())}: {count}");
        }

        return $"within {r} of ({cx}, {cy}): {steps} steps the engine allows, {dropped} of them matching no surface of the next cell "
            + $"within {Server.Engines.Pathing.Tiered.NavWindow.MatchTolerance} (by the gap: {string.Join(", ", byGap)}); "
            + $"for example {string.Join("; ", shown)}.";
    }

    /// <summary>
    /// <c>navbench n [radius] [checked]</c>: n random pairs of standable tiles within radius of home, each routed over the
    /// navigation graph; how many were found, direct, unreachable, over budget or handed to the long tier, the time each took
    /// (mean, 95th percentile, worst) and the nodes expanded. For the first <c>checked</c> routed pairs the route is walked
    /// leg by leg with the precise search, and its cost set against the precise search's own plan from end to end at five
    /// times the ceiling — the nearest thing to an oracle the live map has.
    /// </summary>
    private static string NavBench(string tail)
    {
        var words = (tail ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var map = BotPopulation.Home;
        var home = BotPopulation.Where;

        if (map == null || map == Map.Internal)
        {
            return "the population has no home map yet.";
        }

        var n = words.Length > 0 && int.TryParse(words[0], out var nn) ? Math.Clamp(nn, 1, 2000) : 200;
        var radius = words.Length > 1 && int.TryParse(words[1], out var rr) ? Math.Clamp(rr, 16, 2000) : 500;
        var check = words.Length > 2 && int.TryParse(words[2], out var cc) ? Math.Clamp(cc, 0, 50) : 10;
        var random = new System.Random(26092026);
        var points = new List<Point3D>();
        var plan = new List<Point3D>();
        var times = new List<double>();
        var counts = new Dictionary<Server.Engines.Pathing.Tiered.NavStatus, int>();
        var longBefore = Server.Engines.Pathing.Tiered.NavigationService.LongRoutes;
        var expandedBefore = Server.Engines.Pathing.Tiered.NavigationService.Medium.Expanded;
        var checkedRoutes = 0;
        var legsWalked = 0;
        var legsFailed = 0;
        var over = new List<double>();
        var planned = 0;

        for (var i = 0; i < n; i++)
        {
            Point3D a;
            Point3D b;

            if (!RandomStand(map, home, radius, random, out a) || !RandomStand(map, home, radius, random, out b))
            {
                continue;
            }

            var t0 = System.Diagnostics.Stopwatch.GetTimestamp();
            var status = Server.Engines.Pathing.Tiered.NavigationService.Route(map, a, b, points, 1);
            var ms = (System.Diagnostics.Stopwatch.GetTimestamp() - t0) * 1000.0 / System.Diagnostics.Stopwatch.Frequency;

            times.Add(ms);
            counts[status] = counts.GetValueOrDefault(status) + 1;

            if (status != Server.Engines.Pathing.Tiered.NavStatus.Ok || checkedRoutes >= check)
            {
                continue;
            }

            checkedRoutes++;

            var cost = Server.Engines.Pathing.Tiered.NavigationService.Medium.LastCost / 100;
            var leg = a;
            var ok = true;

            foreach (var point in points)
            {
                legsWalked++;

                if (BotPath.Find(map, leg, point, BotArrival.Within(3), plan) != BotPathOutcome.Reached)
                {
                    legsFailed++;
                    ok = false;
                }

                leg = point;
            }

            if (!ok)
            {
                continue;
            }

            if (BotPath.Find(map, a, b, BotArrival.Within(1), plan, default, BotPath.CeilingMs * 3) == BotPathOutcome.Reached && plan.Count > 0)
            {
                planned++;
                over.Add((cost - plan.Count) / (double)plan.Count);
            }
        }

        times.Sort();

        var mean = times.Count > 0 ? times.Average() : 0.0;
        var p95 = times.Count > 0 ? times[Math.Min(times.Count - 1, (int)(times.Count * 0.95))] : 0.0;
        var worst = times.Count > 0 ? times[^1] : 0.0;
        var statuses = new List<string>();

        foreach (var (status, count) in counts)
        {
            statuses.Add($"{status} {count}");
        }

        over.Sort();

        var overMean = over.Count > 0 ? over.Average() * 100 : 0.0;
        var overP95 = over.Count > 0 ? over[Math.Min(over.Count - 1, (int)(over.Count * 0.95))] * 100 : 0.0;

        return $"{times.Count} random routes within {radius} of home: {string.Join(", ", statuses)}; "
            + $"{Server.Engines.Pathing.Tiered.NavigationService.LongRoutes - longBefore} handed to the long tier; "
            + $"{mean:F2}ms a route on average, {p95:F2}ms at the 95th percentile, {worst:F2}ms at worst; "
            + $"{Server.Engines.Pathing.Tiered.NavigationService.Medium.Expanded - expandedBefore} medium nodes expanded in all; "
            + $"of {checkedRoutes} routes walked leg by leg, {legsWalked} legs and {legsFailed} the precise search could not walk; "
            + $"{planned} also planned end to end by the precise search, the route {overMean:F1}% longer on average and "
            + $"{overP95:F1}% at the 95th percentile.";
    }

    /// <summary>A random tile within <paramref name="radius"/> of home a body can stand on, at its lowest standing height.</summary>
    private static bool RandomStand(Map map, Point3D home, int radius, System.Random random, out Point3D at)
    {
        Span<sbyte> zs = stackalloc sbyte[16];

        for (var tries = 0; tries < 50; tries++)
        {
            var x = home.X + random.Next(-radius, radius + 1);
            var y = home.Y + random.Next(-radius, radius + 1);

            if (x < 0 || y < 0 || x >= map.Width || y >= map.Height)
            {
                continue;
            }

            if (Server.Engines.Pathing.Cache.StepProbe.ComputeStandableSurfaceZs(map, x, y, zs) > 0)
            {
                at = new Point3D(x, y, zs[0]);

                return true;
            }
        }

        at = Point3D.Zero;

        return false;
    }

    private static string Road(string tail)
    {
        var words = (tail ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries);

        // <b>From a bot, where it really stands — <c>road Edda 1785 1455</c>.</b> The four-number form settles its
        // origin on the land, and on 21.09.2026 that made it useless for the one question worth asking: Edda was
        // inside her guild's hall at z 7, the land under the hall is at 0, and the answer came back "0 tiles of
        // plan, nobody could be put down" — about a place she was not standing. A bot's own tile and height are
        // the only origin the walker ever plans from, so they are the only origin worth asking about.
        if (words.Length >= 3 && !int.TryParse(words[0], out _)
            && int.TryParse(words[^2], out var gx) && int.TryParse(words[^1], out var gy))
        {
            var who = Find(string.Join(' ', words[..^2]));

            if (who?.Map == null || who.Map == Map.Internal)
            {
                Refused++;

                return $"there is no bot called \"{string.Join(' ', words[..^2])}\" standing anywhere.";
            }

            var goal = BotStep.Settle(who.Map, gx, gy, out var gz) ? new Point3D(gx, gy, gz) : new Point3D(gx, gy, who.Map.GetAverageZ(gx, gy));
            var plan = new List<Point3D>();
            var t0 = System.Diagnostics.Stopwatch.GetTimestamp();
            var found = BotPath.Find(who.Map, who.Location, goal, BotArrival.Within(1), plan, default, BotPath.CeilingMs * 5);
            var took = (System.Diagnostics.Stopwatch.GetTimestamp() - t0) * 1000.0 / System.Diagnostics.Stopwatch.Frequency;
            var firsts = new List<string>();

            for (var i = 0; i < plan.Count && i < 6; i++)
            {
                firsts.Add($"({plan[i].X},{plan[i].Y},{plan[i].Z})");
            }

            return $"from {who.Name} at {who.Location} to {goal}: {found}, {plan.Count} tiles of plan"
                + (plan.Count > 0 ? $" beginning {string.Join(" ", firsts)} and ending at {plan[^1]}" : "")
                + $", in {took:F1}ms; its own tile {(BotStep.Ground(who.Map, who.X, who.Y, who.Z, BotStep.StandingReach, out var fz) ? $"has a floor at {fz}" : "has no floor the step checker can find")}"
                + (BotPath.LastStarved ? " (starved of clock by the population's window; ask again)" : "") + ".";
        }

        if (words.Length < 4 || !int.TryParse(words[0], out var x1) || !int.TryParse(words[1], out var y1)
            || !int.TryParse(words[2], out var x2) || !int.TryParse(words[3], out var y2))
        {
            Refused++;

            return "road wants four numbers: road <x1> <y1> <x2> <y2>.";
        }

        var map = BotPopulation.Home;

        if (map == null)
        {
            return "the population has no map, so there is no road to look for.";
        }

        // The land's own height where nobody could be put down - a crowded muster point or the enemy's own
        // tile fail Settle for being occupied, and the question is about the road, not the footing.
        var stood1 = BotStep.Settle(map, x1, y1, out var z1);
        var stood2 = BotStep.Settle(map, x2, y2, out var z2);

        map.GetAverageZ(x1, y1, out _, out var avg1, out _);
        map.GetAverageZ(x2, y2, out _, out var avg2, out _);

        var from = new Point3D(x1, y1, stood1 ? z1 : avg1);
        var to = new Point3D(x2, y2, stood2 ? z2 : avg2);
        var footing = (stood1 ? "" : $" (nobody could be put down at {from}, the land's height is used)")
            + (stood2 ? "" : $" (nobody could be put down at {to}, the land's height is used)");
        var path = new List<Point3D>();
        var began = System.Diagnostics.Stopwatch.GetTimestamp();
        var outcome = BotPath.Find(map, from, to, BotArrival.Within(1), path, default, BotPath.CeilingMs * 5);
        var ms = (System.Diagnostics.Stopwatch.GetTimestamp() - began) * 1000.0 / System.Diagnostics.Stopwatch.Frequency;
        var end = path.Count > 0 ? path[^1] : from;

        return $"from {from} to {to}: {outcome}, {path.Count} tiles of plan ending at {end}, in {ms:F1}ms{footing}"
            + (BotPath.LastStarved ? " (starved of clock by the population's window; ask again)" : "")
            + $"; the far side says {BotPath.Enclose(map, to, BotArrival.Within(1))}.";
    }

    /// <summary>
    /// The road map's state, or the steps of road from home to each tile named against the straight line. The instrument
    /// <c>BotHunter.Detour</c> is set with: a dart's ground either is far, or is only far round.
    /// </summary>
    private static string Roads(string tail)
    {
        var words = (tail ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries);

        if (words.Length == 0)
        {
            return $"{BotRoads.Describe()}.";
        }

        if (words.Length % 2 != 0)
        {
            Refused++;

            return "roads wants pairs of numbers: roads <x> <y> [<x> <y> ...].";
        }

        var map = BotPopulation.Home;
        var home = BotPopulation.Where;

        if (map == null)
        {
            return "the population has no map, so there are no roads to read.";
        }

        if (!BotRoads.Ready)
        {
            return $"{BotRoads.Describe()}; ask again when it is drawn.";
        }

        var said = new List<string>(words.Length / 2);

        for (var i = 0; i + 1 < words.Length; i += 2)
        {
            if (!int.TryParse(words[i], out var x) || !int.TryParse(words[i + 1], out var y))
            {
                Refused++;

                return $"roads wants whole numbers, and \"{words[i]} {words[i + 1]}\" is not two of them.";
            }

            var road = BotRoads.FromHome(map, x, y);
            var straight = Math.Max(Math.Abs(x - home.X), Math.Abs(y - home.Y));

            said.Add(road < 0 ? $"({x}, {y}) no road from home, {straight} straight" : $"({x}, {y}) {road} by road, {straight} straight");
        }

        return $"{string.Join("; ", said)}.";
    }

    private static string Tile(string tail)
    {
        var (xs, rest) = First(tail);
        var (ys, _) = First(rest);

        if (!int.TryParse(xs, out var x) || !int.TryParse(ys, out var y))
        {
            Refused++;

            return "tile wants two numbers: tile <x> <y>.";
        }

        var map = BotPopulation.Home;

        if (map == null)
        {
            return "the population has no map, so there is no tile to look at.";
        }

        var near = BotPopulation.Where;
        var stands = BotStep.Ground(map, x, y, near.Z, BotStep.StandingReach, out var z);
        var settles = BotStep.Settle(map, x, y, out var floor);
        var at = new Point3D(x, y, stands ? z : floor);
        var region = Region.Find(at, map);

        var items = ValueStringBuilder.Create(256);
        var seen = 0;

        try
        {
            foreach (var item in map.GetItemsInRange(at, 0))
            {
                if (seen++ >= 6)
                {
                    items.Append(", and more");

                    break;
                }

                if (seen > 1)
                {
                    items.Append(", ");
                }

                items.Append(item.GetType().Name);
                items.Append(" at z ");
                items.Append(item.Z);
            }

            var lying = seen == 0 ? "nothing lying on it" : $"on it: {items.ToString()}";

            return
                $"({x}, {y}): {(stands ? $"a body fits, standing at z {z}" : "no body fits at the height asked")}; "
                + $"the floor {(settles ? $"is at z {floor}" : "could not be found")}; "
                + $"region {region?.Name ?? "none named"}; "
                + $"{lying}.";
        }
        finally
        {
            items.Dispose();
        }
    }

    /// <summary>
    /// The GM's cure for a pocket: lift the bot out rather than argue with the pathfinder.
    ///
    /// <para>
    /// Bounded three ways, and each bound is one way a wrong pair of coordinates could do harm. Only
    /// somewhere a body actually fits, so nobody is posted into rock. Only within <see cref="Reach"/> of
    /// where the bot already is. And only ever a bot of ours.
    /// </para>
    /// </summary>
    private static string Tele(string tail)
    {
        var (name, rest) = First(tail);
        var (xs, more) = First(rest);
        var (ys, _) = First(more);

        var bot = Find(name);

        if (bot == null)
        {
            Refused++;

            return $"there is no bot called \"{name}\".";
        }

        if (!int.TryParse(xs, out var x) || !int.TryParse(ys, out var y))
        {
            Refused++;

            return "tele wants a bot and two numbers: tele <bot> <x> <y>.";
        }

        var map = bot.Map;

        if (map == null || map == Map.Internal)
        {
            return $"{bot.Name} is not on a map.";
        }

        if (!Utility.InRange(bot.Location, new Point3D(x, y, bot.Z), Reach))
        {
            Refused++;

            return $"({x}, {y}) is further than {Reach} tiles from {bot.Name}; I will not throw anybody that far.";
        }

        if (!BotStep.Ground(map, x, y, bot.Z, BotStep.StandingReach, out var z))
        {
            Refused++;

            return $"no body fits on ({x}, {y}) anywhere near {bot.Name}'s own height; nobody was moved.";
        }

        var from = bot.Location;

        bot.MoveToWorld(new Point3D(x, y, z), map);

        // The plan it was holding was a plan from somewhere else. Left alone, the bot walks straight back
        // into the pocket it has just been lifted out of.
        bot.Journey?.Discard();

        return $"{bot.Name} lifted from ({from.X}, {from.Y}, {from.Z}) to ({x}, {y}, {z}); its plan was torn up with it.";
    }

    private static string Homeward(BotMobile bot)
    {
        var map = BotPopulation.Home;
        // Where this bot lives (29.09.2026): its guild's hall or seat, or its town. See BotPopulation.HomeOf.
        var where = BotPopulation.HomeOf(bot);

        if (map == null)
        {
            return "the population has no home to send anybody to.";
        }

        // <b>The configured home tile is not necessarily a tile.</b> Asked on 04.09.2026 what was at
        // (1440, 1470) — this shard's own home — the answer was "no body fits and the floor could not be
        // found", while all eight of its neighbours held one: a static sitting on the exact point somebody
        // typed into the config. The population never noticed because its own birth scatters over six tiles
        // and takes the first spot that holds a body. Anything that walks to the literal point does notice,
        // so this does the same scatter rather than trusting the number.
        var at = Point3D.Zero;

        for (var ring = 0; ring <= BotPopulation.Spread && at == Point3D.Zero; ring++)
        {
            for (var dx = -ring; dx <= ring && at == Point3D.Zero; dx++)
            {
                for (var dy = -ring; dy <= ring; dy++)
                {
                    if (Math.Max(Math.Abs(dx), Math.Abs(dy)) != ring)
                    {
                        continue;
                    }

                    if (BotStep.Ground(map, where.X + dx, where.Y + dy, where.Z, BotStep.StandingReach, out var found))
                    {
                        at = new Point3D(where.X + dx, where.Y + dy, found);

                        break;
                    }
                }
            }
        }

        if (at == Point3D.Zero)
        {
            Refused++;

            return $"nothing within {BotPopulation.Spread} tiles of home holds a body; nobody was moved.";
        }

        var from = bot.Location;

        bot.MoveToWorld(at, map);
        bot.Journey?.Discard();

        return $"{bot.Name} brought home from ({from.X}, {from.Y}, {from.Z}) to ({at.X}, {at.Y}, {at.Z}).";
    }

    private static string Raise(BotMobile bot)
    {
        if (bot.Alive)
        {
            return $"{bot.Name} is not dead.";
        }

        bot.Resurrect();

        return bot.Alive
            ? $"{bot.Name} is back on its feet."
            : $"{bot.Name} is still down; the engine refused it.";
    }

    private static string Free(BotMobile bot)
    {
        // The roll-call's own two steps, in the same order and for the same reason: the plan first, so that
        // whatever the work has learned about where it was going is written down before the work is ended.
        // Ending it first is how the debugger spent a night deleting the shard's own lessons. See BotAudit.
        var held = bot.Resolve?.Deed?.ToString() ?? "nothing";

        bot.Journey?.Discard();
        BotWill.Abandon(bot, "the debugger shook it loose by name");

        return $"{bot.Name} was holding {held}; its plan is torn up and the work is off it.";
    }

    private static string Leave(BotMobile bot)
    {
        var foe = bot.Combatant;

        if (foe == null)
        {
            return $"{bot.Name} is not fighting anything, so there is nothing to leave alone.";
        }

        BotQuarry.Shun(foe);

        return $"{foe.Name} is left alone for a while; nobody of ours will be offered it.";
    }

    /// <summary>
    /// A bot of ours by name, and nothing else, ever.
    ///
    /// <para>
    /// The whole safety of this file is one line long and it is this one: the roster is the only place a
    /// target may come from. A player's character, a shopkeeper, a staff member and a spawned creature are
    /// all simply not findable from here, whatever the model writes.
    /// </para>
    /// </summary>
    private static BotMobile Find(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        var asked = name.Trim();
        var joint = asked.IndexOf(" the ", StringComparison.OrdinalIgnoreCase);

        if (joint > 0)
        {
            asked = asked[..joint].Trim();
        }

        var bots = BotPopulation.Bots;

        for (var i = 0; i < bots.Count; i++)
        {
            var bot = bots[i];

            if (bot is { Deleted: false } && string.Equals(bot.Name, asked, StringComparison.OrdinalIgnoreCase))
            {
                return bot;
            }
        }

        return null;
    }

    /// <summary>
    /// First word and the rest — except that a name may hold a space now (see <c>BotPopulation.Christen</c>),
    /// so the two-word reading is tried against the roster before the one-word split is trusted.
    /// </summary>
    private static (string Head, string Tail) First(string text)
    {
        text = (text ?? "").Trim();

        var space = text.IndexOf(' ');

        if (space < 0)
        {
            return (text, "");
        }

        var second = text.IndexOf(' ', space + 1);
        var two = second < 0 ? text : text[..second];

        if (Find(two) != null)
        {
            return (two, second < 0 ? "" : text[(second + 1)..].Trim());
        }

        return (text[..space], text[(space + 1)..].Trim());
    }

    public static string Describe()
    {
        if (Used == 0 && Refused == 0)
        {
            return "the debugger has not used its hands at all";
        }

        var sb = ValueStringBuilder.Create(256);

        try
        {
            sb.Append(Used);
            sb.Append(" commands run and ");
            sb.Append(Refused);
            sb.Append(" refused");

            if (_used.Count > 0)
            {
                sb.Append(": ");

                var first = true;

                foreach (var (verb, count) in _used)
                {
                    if (!first)
                    {
                        sb.Append(", ");
                    }

                    sb.Append(count);
                    sb.Append(' ');
                    sb.Append(verb);
                    first = false;
                }
            }

            return sb.ToString();
        }
        finally
        {
            sb.Dispose();
        }
    }
}
