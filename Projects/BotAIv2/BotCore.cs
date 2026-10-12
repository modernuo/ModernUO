using Server.Logging;

namespace Server.BotAI.V2;

/// <summary>
/// The way in. Everything this assembly does starts here, and this file does as little as possible.
///
/// The server finds this assembly through <c>Data/assemblies.json</c> and calls the static
/// <see cref="Configure"/> and <see cref="Initialize"/> by reflection, the same way it drives the
/// shard's own content. Nothing in the engine references this project, so it stays clear of the
/// upstream rebase path entirely.
///
/// <para>
/// <b>It registers modules and names the moments. It does not order them.</b> The list below is
/// "what exists", and its order means nothing — each module declares what it needs and
/// <see cref="BotModules"/> works out the sequence. That is the one thing this file is designed to
/// avoid becoming: the first version's equivalent was a run of thirty calls held together by fifteen
/// comments saying "this must come after that", and a mistake in it was undetectable by reading. The
/// worst of them cost eleven nulls in an index that then behaved plausibly for a whole session.
/// </para>
///
/// <para>
/// Three moments, and the third is the one that is easy to get wrong. <see cref="Configure"/> is
/// before the world exists. <see cref="OnWorldLoad"/> is after it is in memory — and it can happen
/// more than once, because a world can be reloaded, which is why modules are reset first rather than
/// asked to start twice. <see cref="Initialize"/> only reports.
/// </para>
/// </summary>
public static class BotCore
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotCore));

    private static bool _loadedOnce;

    /// <summary>
    /// Master switch, read from <c>bots.enabled</c> in modernuo.json and written back on first boot so
    /// it is discoverable in the config file rather than buried in code. Each module has its own switch
    /// besides this one — <c>bots.&lt;name&gt;.enabled</c>.
    /// </summary>
    public static bool Enabled { get; private set; }

    // <b>Before the minds and the debugger (02.10.2026, build 395).</b> They read Enabled in their own Configure, and the engine
    // sorts every Configure of the default priority 50 with an unstable sort: from build 391 on, with more Configures in the list,
    // BotMindCore came first, read Enabled as false, said "the bot system is not [on]" and registered nothing, so the thinking
    // bots were off from 15:14 to this build. 40 is after RegenRates' 10, which BotMeal.Configure below needs.
    [CallPriority(40)]
    public static void Configure()
    {
        Enabled = ServerConfiguration.GetOrUpdateSetting("bots.enabled", true);

        if (!Enabled)
        {
            return;
        }

        // Before the modules and before the world: a persistence must be registered by the time the save is
        // read, and this one carries the only thing about a bot that is worth keeping across a restart.
        BotProgress.Configure();

        // What the population found out about the island, kept the same way. See BotQuadStore: the ground's
        // reputation is a fact about the world rather than about anybody standing on it, and it took a
        // company most of an evening to learn.
        BotQuadStore.Configure();

        // And what pays where, which every restart used to throw away — an hour of relearning after each of the
        // hourly deploys. See BotCommonsStore: Patrick's choice of 16.09.2026.
        BotCommonsStore.Configure();

        // And the murderers and the cells, whose clocks a restart used to wipe while the engine kept the kills. See
        // BotOutlawStore.
        BotOutlawStore.Configure();

        // And the city's treasury with its standing orders and bounties, which every restart used to open at the
        // opening sum again. See BotCityStore.
        BotCityStore.Configure();

        // And the Needs board: the wants and their lines, whose clocks a restart used to start again (02.10.2026, build 391).
        // See BotMarketStore.
        BotMarketStore.Configure();

        // And the guilds' chests, the claims' income. See BotChestStore.
        BotChestStore.Configure();

        // And the board of errands, each with its count and its held reward. See BotQuestStore.
        BotQuestStore.Configure();

        // And who owns which square of it, which is the one board on this shard that a guild pays for. See
        // BotClaimStore: everything else is cleared on a reload, deliberately, and this must not be.
        BotClaimStore.Configure();

        // And where each guild was told to live, when that was said at the keyboard rather than in the
        // file. See BotSeat: the file is read every start, the store holds only what the hand said.
        BotSeatStore.Configure();

        // And the wars: who is fighting whom and for how long, who may not declare yet, and who still owes a
        // move. See BotWarStore: without it a restart was an amnesty.
        BotWarStore.Configure();

        // And the seizures each guild is remembered for, whose price compounds over two days of the shard (06.10.2026). See
        // BotSeizure: without it every deploy would forgive them.
        BotSeizureStore.Configure();

        // And the island's criminal record with The Shadow, the guild of thieves founded by murderers. See BotUnderworldStore.
        BotUnderworldStore.Configure();

        // And the town's books on crime: the thieves' roll, the wanted list and its bounties, the terms and repentance (07.10.2026,
        // crime17). See BotCrimeStore.
        BotCrimeStore.Configure();

        // The engine's regeneration hooks, wrapped rather than replaced, so that a bot which has eaten
        // recovers faster and nothing else on the shard changes at all. After RegenRates, which carries
        // CallPriority(10) — see BotMeal.Configure for what happens if that ever inverts.
        BotMeal.Configure();

        // Everything this assembly is made of. Order here is not meaningful; dependencies are.
        BotModules.Register(new BotClassModule());
        BotModules.Register(new BotMovementModule());
        BotModules.Register(new BotSquadModule());
        BotModules.Register(new BotWillModule());
        BotModules.Register(new BotHarvestModule());
        BotModules.Register(new BotShopsModule());
        BotModules.Register(new BotCraftModule());
        BotModules.Register(new BotMarketModule());
        BotModules.Register(new BotSpellsModule());
        BotModules.Register(new BotHuntModule());
        BotModules.Register(new BotMendModule());
        BotModules.Register(new BotPopulationModule());

        // After the population, because a captain deciding whether to hold a class reads the whole roster to
        // find out whether anybody on the island is worth teaching.
        BotModules.Register(new BotDrillModule());

        // After the population, for the same reason as the captain: it hands every hall in the world back to
        // the guild that owns it, and a guild has no leader until the population has enrolled.
        BotModules.Register(new BotEstateModule());

        // The board of errands: what the door and the marshal ask of the population, for a price. See BotQuests —
        // Patrick's order of 16.09.2026, evening.
        BotModules.Register(new BotQuestModule());

        // After the squads, whose module it needs, and it says so itself rather than relying on this order.
        BotModules.Register(new BotBaronModule());

        // The dungeons. After the population as well as the squads, because a delve is called by the maker
        // of a guild and there are no guilds until the population has been dealt out; it says so itself.
        BotModules.Register(new BotDelveModule());

        // The towns, the roads between them and the journeys: Patrick's order of 29.09.2026. See BotTravel.
        BotModules.Register(new BotTravelModule());

        // Towns of residence: each bot lives in a town of its own, born there or walked there. After the population and the
        // travel, which it says itself. Patrick's order of 29.09.2026, point 5. See BotResidence.
        BotModules.Register(new BotResidenceModule());

        // Guild houses: a guild settles in an empty town building — a door put in, a sign hung — or buys a house in Britain.
        // After the estate, the travel and the residences, which it says itself. Patrick's idea of 29.09.2026, evening. See
        // BotGuildHouses.
        BotModules.Register(new BotGuildHouseModule());

        // Bots' own shops by the bank of the town they live in, and the auction cut to a few kinds a bot: Patrick's order of
        // 29.09.2026, evening. It says what it needs itself. See BotShopkeep.
        BotModules.Register(new BotShopkeepModule());

        // The traders: a class neutral in wars that buys where a thing is to be had and carries it to whoever said aloud that it
        // wants it, writing every order into a book. Patrick's point 5 of 30.09.2026 and plan v2's point 3 (01.10.2026, build
        // 385). After the market and the shops, which it says itself. See BotTraders/README.md.
        BotModules.Register(new BotTradersModule());

        // Fires in the woods, company round them, and a word on the road: Patrick's point 6 of 29.09.2026. See BotCamp.
        BotModules.Register(new BotCampModule());

        // The establishments of the professions: a cook's tavern at an inn, a moneylender's desk at a shop by the bank. After the
        // camp and the shops, which it says itself. Patrick's approval of 07.10.2026. See BotTrades.
        BotModules.Register(new BotTradesModule());

        // Meetings: no war and no alliance without an envoy walking to the other guild's town and a word given before a
        // witness; agreements between guilds; each guild's dealings with its town. Patrick's order of 29.09.2026, evening. See
        // BotParley. After the population, the will and the travel, which it says itself.
        BotModules.Register(new BotDiplomacyModule());

        // Politics inside guilds and towns: elections of the head, plots against it, the chest's purpose by vote, the towns'
        // councils, and ultimatums with tribute instead of war. Patrick's order of 07.10.2026, night. See BotPolitics.
        BotModules.Register(new BotPoliticsModule());

        // Runes: Mark, Recall and a company's Gate Travel, and the guilds' rune libraries. Patrick's order of 29.09.2026,
        // point 7. See BotRunes.
        BotModules.Register(new BotRunesModule());

        // The sea: ships to the towns no road reaches, Patrick's point 7 of 29.09.2026. After travel, whose towns it docks at.
        BotModules.Register(new BotSeaModule());

        // Argus's proving ground in Green Acres: a bot's double against each dungeon's worst creature, and the
        // strength the delve judges a band by once it has been measured. After the dungeons, whose ladder it is.
        // Patrick's order of 26.09.2026.
        BotModules.Register(new BotProvingModule());

        // The danger and safety zones: where creatures that attack on sight patrol and how far they see, how strong they are
        // against a bot, and the debuggers' tour that wakes the ground to watch them. Patrick's order of 30.09.2026, evening.
        // See BotZones.
        BotModules.Register(new BotZonesModule());

        // The reach map: every tile named by what a walker from home can do with it — open, isle, pocket, water, blocked — and
        // no walk formed into anything but open ground. Patrick's order of 30.09.2026, night, point 4. See BotReachMap.
        BotModules.Register(new BotReachModule());
        BotModules.Register(new BotDashboardModule());

        // Last, and it reads only. The smoke alarm: it divides numbers the other modules keep, so it wants
        // them started, and it is registered here rather than with the debugger because a shard watched by
        // nobody should still be able to shout.
        BotModules.Register(new BotAlarmModule());

        // The voices: what the bots say about what they do, by occasion, out loud and on the event stream.
        // Patrick's order of 28.09.2026.
        BotModules.Register(new BotVoiceModule());

        // Bots answering each other's moods: the help a neighbour in earshot offers, through the auction, to a bot that said it is
        // hurt, tired, grieving, poor, lonely, bored or proud; never across hostile guilds. Patrick's point 4 of 01.10.2026.
        BotModules.Register(new BotSocialModule());

        // The underworld's trades and the town's law: cutpurses by the banks, ambushes on the roads, the fence, the wanted list
        // with its bounties, the cells' terms, ransoms and repentance (07.10.2026, crime17). See BotCrime.
        BotModules.Register(new BotCrimeModule());

        // The web dashboard: the whole shard as JSON on the loopback address, and the page that draws it. Reads
        // only, and last, for the same reason as the alarm. Patrick's order of 28.09.2026.
        BotModules.Register(new BotWebModule());

        BotModules.Start(BotPhase.Settings);

        EventSink.WorldLoad += OnWorldLoad;
    }

    public static void Initialize()
    {
        logger.Information(
            "BotAI v2 loaded ({Status}): {Modules}",
            Enabled ? "enabled" : "disabled",
            Enabled ? BotModules.Describe() : "nothing registered"
        );
    }

    /// <summary>
    /// The world is in memory. Anything that had to ask about the map may now do so.
    ///
    /// Reachable more than once. On a reload the modules that are already running are put back to
    /// nothing first, because their counters describe a population that is about to be rebuilt — and a
    /// total that is never reset does not look wrong, it looks like a population twice the size.
    /// </summary>
    private static void OnWorldLoad()
    {
        if (_loadedOnce)
        {
            BotModules.Rewind(BotPhase.World);
        }

        _loadedOnce = true;

        BotModules.Start(BotPhase.World);
    }
}
