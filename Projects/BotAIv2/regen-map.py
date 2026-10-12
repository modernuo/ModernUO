"""Rebuilds MAP.md's subsystem blocks and the whole of DIALS.md from this folder's own source.

Run from this directory:  python regen-map.py

Everything between the SECTION2 markers in MAP.md is replaced; nothing else is touched.

Each block is half generated and half kept here:

* generated from the code — the module class, the configuration file, the work the subsystem offers (every
  `IBotProposer` with the rung it offers on and the deeds it hands out), and the file table, whose row text is
  the first sentence of that file's own class summary;
* kept in BLOCKS below — the paragraph saying what the subsystem is for, and the traps worth knowing before
  touching it.

`regen-map.py` also reports drift in the subsystem READMEs rather than fixing it: those carry hand-written
reasoning that a generator has no business rewriting, but their file tables silently fall behind the folder
and that is how they came to say nine classes where there are thirteen. The check is a printed line, and
acting on it is a person's job.

DIALS.md is wholly generated: every `public static X Y { get; set; } = default;` in the assembly, with the
default it was written with and the configuration key that overrides it where one exists. It is kept out of
MAP.md so that the map stays cheap to read and a number stays one file away.

Written this way because the two halves rot differently. Anything derived from the code cannot drift, and the
prose is small enough to keep true by hand — which the subsystem READMEs, written once and left, are not: as
of 05.09.2026 several of them describe a shard that no longer exists.
"""

import json
import os
import re

BEGIN = "<!--SECTION2:BEGIN-->"
END = "<!--SECTION2:END-->"

ROOT = "(root)"

# Reading order: the way in, the clock, the decision, then everything a decision reaches for, then the
# subsystems that watch rather than act.
ORDER = [
    ROOT, "BotModules", "BotPopulation", "BotWill", "BotMovement", "BotClasses", "BotOutfit",
    "BotHarvest", "BotCraft", "BotHunt", "BotShops", "BotMarket", "BotShopkeep", "BotSpells",
    "BotCombat", "BotMend", "BotSquad", "BotDrill", "BotBaron", "BotDelve", "BotTravel", "BotResidence", "BotGuildHouse", "BotCamp", "BotRunes", "BotSea", "BotProving", "BotEstate", "BotDiplomacy", "BotRelics", "BotQuest", "BotQuad", "BotRanger",
    "BotDashboard", "BotAlarm", "BotVoice", "BotSocial", "BotWeb", "mindedBots", "mindedBots/debugger",
]

# folder -> (title, what it is, [traps])
BLOCKS = {
    ROOT: (
        "the way in",
        "One file. `BotCore.Configure` and `BotCore.Initialize` are found by the engine's reflection the same "
        "way it finds content, and everything this assembly does hangs off them: the stores that must exist "
        "before the world is read, the regeneration hooks, and the registration of every module in the order "
        "they are listed there.",
        ["`BotCore.Configure` runs after `RegenRates.Configure` by loader ordering rather than by contract. "
         "If that ever inverts, meals stop quickening recovery — they do not break anything."],
    ),
    "BotModules": (
        "the loading frame",
        "Holds the subsystems, works out what order to start them in, starts them, and says what happened. "
        "Two phases: `Settings` runs before the world exists and may only read numbers and files; `World` "
        "runs after it is in memory and may ask where things are. A module can be switched off from its own "
        "configuration file without touching anything else.",
        ["A subsystem that asks the map a question in the `Settings` phase gets a world that is not there "
         "yet. Phase is the first thing to check when something is null only at boot."],
    ),
    "BotPopulation": (
        "what a bot is, and the clock",
        "`BotMobile : PlayerMobile` is the bot. One timer serves the whole population and gives each bot a "
        "turn on its own schedule — `BotBeat`, which is also where four of the summary lines are written. "
        "This folder also owns what survives a restart (`BotProgress`: skills, fame, karma, savings) and the "
        "three pieces of work that belong to no trade: walking home, going back for what death took, and "
        "taking a full pack to a counter.",
        ["A bot must be Player-flagged or death deletes it outright, and a dead bot counts as alive and "
         "silently drops out of the beat.",
         "`BotProgress` is the only place skills persist. Zeroing it is not the same as touching the world "
         "save, and the world save holds Patrick's own character — never delete it."],
    ),
    "BotWill": (
        "the decision",
        "Every turn a bot asks one question and this answers it. Three stages: the ladder says which rung the "
        "bot is on from facts alone, obligations are taken before anything is auctioned, and then every "
        "proposer registered by every other subsystem offers work which is priced per minute and the best "
        "offer wins. Prices are *measured* — `BotLedger` corrects a trade's opening guess by what the work "
        "really paid — rather than assigned by weights. Every `took on` / `finished` / `failed at` line in "
        "the log is written here, and the factors printed beside the price are the whole reckoning.",
        ["A multiplier that can reach zero is a veto: every factor needs a floor.",
         "A deed that answers `Work` for ever, or answers with the same walk for ever, is immortal and "
         "invisible. Both have happened; both cost a session.",
         "The walk order a deed returns must be stable between ticks, because arrival is compared against "
         "it. A destination recomputed every tick resets the route every tick."],
    ),
    "BotMovement": (
        "path, road and step",
        "Getting a bot from where it is to where the work is. The most expensive part of the assembly and the "
        "one with the longest history of measured defects — `RESEARCH.md` beside it is the analysis the "
        "current design came out of. A journey is a destination, a queue of things put aside to do first, and "
        "the plan currently being walked.",
        ["A `Point3D` whose Z came out of arithmetic is a place nothing can stand on: it works on flat ground "
         "and fails on a hill. Settle it against the map.",
         "Arriving is judged by asking the engine whether the work would be accepted here, never by the "
         "distance to a remembered point."],
    ),
    "BotClasses": (
        "what a kind of bot is",
        "Thirteen classes, one file each, plus a registry. A class is **a description and a set of limits with "
        "no behaviour** — it decides nothing and commands nobody. `BotWill` reads a class the way it reads "
        "the map: as a fact about the world. Four of the thirteen cast; three produce; the rest fight, shoot "
        "or heal.",
        ["Adding a class means a file here and a line in the registry. Nothing else in the assembly holds a "
         "list of permitted archetypes, and nothing should — the tool a bot carries is what decides which "
         "trades it is offered."],
    ),
    "BotOutfit": (
        "what a bot owns",
        "Turns a class's kit into items actually in a pack, decides what death has no right to take, and owns "
        "the parts of a bot that are property rather than behaviour: its bond with a weapon, its harness, its "
        "armour, and its horse.",
        ["`Mobile.EquipItem` does not replace an occupied layer, it refuses. Whatever wants a hand has to "
         "free it first, and has to put back what it took."],
    ),
    "BotHarvest": (
        "ore, wood, herbs, and the survey",
        "Where raw material comes from, and — more used than the digging — the survey of the ground that the "
        "whole population navigates by. `BotGround` sweeps the tiles around wherever bots go and remembers "
        "four things: seams of ore, forges with an anvil beside them (`Fires`), every kind of fire the "
        "engine will cook over (`Hearths`), and counters. Everything that needs a *place* asks this.",
        ["The survey is built by walking and is not saved. A restart empties it — which silently cures "
         "\"the island has run out of ore\" and makes any measurement about exhaustion meaningless for the "
         "first two or three hours.",
         "An axe must be worn for the engine to accept a swing; a pick works from the pack. That difference "
         "is why mining worked from the first day and no log was ever cut."],
    ),
    "BotCraft": (
        "turning materials into goods",
        "Five trades that make things: the smith at a forge, the tailor out of leather or cloth, the "
        "alchemist over a bottle, the fletcher out of shafts and feathers, and the cook at any fire. Each is "
        "a proposer that decides whether there is a piece of work, and a deed that walks to the place, swings "
        "until something comes of it, and puts the result either into the order it was made for or onto the "
        "market. `BotCraftwork` is the shared half — choosing a recipe, swinging, counting what appeared.",
        ["Crafting is asynchronous: `CraftItem.Craft` starts a timer. Count what the *last* swing produced at "
         "the top of the next tick, never after the swing you just made.",
         "The engine refuses in silence, because it answers a refusal by sending a message to a screen the "
         "bot does not have. Requirements live on the *recipe* (`SetNeedHeat`, `SetNeedOven`, `SetNeedMill`), "
         "not on the system's `CanCraft`.",
         "A failed attempt eats half the material. A round set up for exactly one item is a round that ends "
         "in `out of metal` after two misses — see `BotAnvil.Tries`."],
    ),
    "BotHunt": (
        "the only new gold in the world",
        "Everything else on this shard moves money about; this brings it in. Choosing what is worth fighting, "
        "prowling for it, killing it, carving it and going through what it left. Carving is folded into "
        "looting rather than made a choice, because the engine charges nothing for it and there is no "
        "decision in it worth a bot's turn.",
        ["Everything lifted off a corpse is listed for sale except the bot's own ammunition and a cook's "
         "raw meat. Anything else a trade needs as *input* will be sold before that trade ever sees it."],
    ),
    "BotShops": (
        "buying and selling over a counter",
        "A capability for every bot rather than a trade of its own: reagents for a caster, bandages for "
        "anybody, metal for a smith, and whatever the population would not buy goes back over the same "
        "counter for coin. Also the board of standing orders — what a bot has put money down for and has not "
        "received yet.",
        ["The engine pays out of the pack, and a bot only walks to a bank above a threshold. Three files "
         "once closed on each other so that money existed and could not be spent."],
    ),
    "BotMarket": (
        "the bots' market, hand to hand",
        "Trade between bots with the auction taken out (Patrick's order of 30.09.2026): Britain's square by the "
        "bank, the wants bots say aloud in the trade chat (no gold held), the carrier who walks what somebody "
        "wants over to them and the meeting at which goods go pack to pack and coin pack to pack, the prices "
        "learned from goods that really changed hands, and the city's treasury, which buys at the bots' shops. "
        "Nothing changes hands between two bots that are not standing within two tiles of each other.",
        ["A want and the goods can both exist and never meet: the carrier offers a walk only within its reach "
         "and to a buyer on its land who could pay for one. When trade is low, read the carriers' clause of "
         "`The market:` before the wants.",
         "A want holds no gold, so the buyer pays at the meeting in the middle of its own work; the meeting "
         "books that coin aside (`BotYield.Aside`) or the buyer's hunt would learn that hunting loses money."],
    ),
    "BotShopkeep": (
        "bots' shops by the bank",
        "Trade between bots in a place, and since 01.10.2026 the market itself. A maker or gatherer holding what "
        "the island is short of or somebody asks for stands by Britain's bank (a guild's member by its guild's "
        "own town's) for a shift and calls out what it sells in the trade chat; a bot short of "
        "a supply, or with a want said, weighs the shop against the shopkeeper on price and the walk, walks "
        "over, and buys out of the keeper's bank box and pack within two tiles of it. The goods never leave the "
        "world. The city's buys, fairs and standing orders are served here too.",
        ["Fighters keep no shop (`BotShopkeeper.Merchant`): 88 of 103 shift-hours on 30.09.2026 were theirs "
         "and 53 % of shifts sold nothing.",
         "A shop's stock is a count, not the goods: it is taken every few seconds and at every sale, and the "
         "till counts again before anything moves. A customer can arrive to find the thing gone; a supplies "
         "errand then goes on to a shopkeeper."],
    ),
    "BotPolitics": (
        "elections, plots, councils and tribute",
        "Politics inside the guilds and the towns, Patrick's order of 07.10.2026: a guild's head elected by its members on "
        "their records (renown, kills of other guilds, wars won, dues given) every six hours or when the head is gone or has "
        "fallen, and deposed by a vote of no confidence when enough are discontented; what the chest saves for, by vote; each "
        "town's tax collector and captain of the guard by the guilds' standing there, paid out of the treasury, and a surcharge "
        "on the town's outcasts taxed into it; and an ultimatum before a war on a guild half as strong, with tribute paid "
        "chest to chest instead.",
        ["The elected head sticks only because every member's fealty is sworn to it (`BotGuilds.Crown`): the engine "
         "recounts fealty once a day at a save and would otherwise put the founder back.",
         "An ultimatum stands only in front of a war the existing rules already allowed; it can lower the wars per hour, "
         "never raise them. A tribute standing refuses any declaration between the pair (`BotWar.MayDeclare`)."],
    ),
    "BotQuest": (
        "the board of errands",
        "What the door and the marshal of events ask of the population, for a price: kill so many of a "
        "creature near a place, bring so many of a thing to a place, scout a place. The reward is held by "
        "the treasury from the moment of posting and paid to whichever bot does the errand; an errand "
        "untaken for two hours lapses and gives the money back. One board instead of a list and a verb for "
        "each thing somebody wants done; the marshal who posts on his own lives with the watchers.",
        ["An offer is not an errand: the board marks an errand taken in the deed's Taken, never in the "
         "proposer, so a bot that loses the auction has not taken anything."],
    ),
    "BotSpells": (
        "a book that grows",
        "Scrolls, reagents and spellbooks: the first work in the project whose output no shopkeeper sells, "
        "and the first buyer on the bots' market that is itself a bot. A caster wants spells it does not "
        "have, a scribe makes them, and the armoury decides what a book is short of.",
        [],
    ),
    "BotCombat": (
        "is it worth stopping",
        "The fight itself belongs to the engine — `Warmode` and `Combatant` are enough, and its own skill "
        "checks train skills exactly as they do for a player. What is here is the judgement around it: is "
        "this thing worth fighting, is this fight being lost, is somebody calling for help, and how a shooter "
        "keeps its distance.",
        ["Three separate reasons a bot fails to land a blow on something it is standing next to — no line to "
         "it, a shooter that moved too recently, a broken cast — and none of the three says anything in the "
         "log unless a counter is put there."],
    ),
    "BotMend": (
        "bandages",
        "The smallest subsystem here, and the only thing that puts a bot on the `Failing` rung. A bot binds "
        "its own wounds, and a surgeon binds somebody else's.",
        [],
    ),
    "BotSquad": (
        "standing companies",
        "A leader, a few followers, a formation, and dividing what the fight left. Companies are formed from "
        "three places — a hunt that found something too big for one, a patrol, and the Baron's harrowing — "
        "and dissolve when there is nothing left to fight. This folder also *assembles* five of the summary "
        "lines out of counters that mostly live elsewhere.",
        ["A bot on the `Bound` rung takes no work of its own: the auction is switched off for it. A company "
         "with no charge is therefore a company of idle bots.",
         "The subsystem's own README still says nothing calls `BotSquads.Form`. It has three callers and "
         "companies form on every session."],
    ),
    "BotDrill": (
        "the captain",
        "One class exists for the others rather than for itself. A captain holds a field, teaches whoever "
        "pays the fee, marches a company at ground the danger map says is bad, and puts armour orders on the "
        "board for bodies that have none.",
        ["A lesson is worth about fifteen times what a rescue is in skill gained, so any rule that keeps "
         "captains from teaching is a rule against the population levelling at all."],
    ),
    "BotBaron": (
        "the ground that has already killed people",
        "Every other class answers *how does this bot get by*. The Baron answers the one question nobody "
        "else on the island asks, because there is no profit in it: who goes back to the places that have "
        "proved they kill. He raises a levy for it, walks his rounds, tours the towns, and pays a stipend out "
        "of his own account.",
        [],
    ),
    "BotDelve": (
        "the dungeons",
        "Five bots taken underground by a member of their guild, for twenty minutes or twenty corpses. Since "
        "29.09.2026 the party marches from the muster to the cave mouth and walks through it — the world's "
        "teleporters are gates the journey routes through (`BotGates`) — and walks out by it at the end; the "
        "descent by hand is kept only for a dungeon no gate joins. Which dungeon is decided by measuring what "
        "lives in each against what the band is worth. What they take is swept into one pot as it fills and "
        "divided at the end, half to the leader. A fighter alone may venture into a dungeon whose worst it "
        "outmatches (`BotVenture`); inside, the hunt, the band and the walk home do the rest.",
        ["A refused road underground is ordinary rather than fatal — a wall between a bot and its place in "
         "the line is what a cavern is made of — so `BotDelve.Bend` shrugs it off and moves the party.",
         "Whoever is still below ten minutes after a delve settled is lifted by `BotDelveParty.Watch`, and the "
         "count is printed against those who walked out: a backstop doing the ordinary work has become the "
         "design."],
    ),
    "BotTravel": (
        "the towns and the roads",
        "The towns read from the map's regions, which of them can be walked to from home (the gates' answer), "
        "one road per pair drawn by the long tier and walked into the book, and the journey a bot with nothing "
        "pressing takes to a town it has not seen — telling its guild what the road read on the danger map. "
        "Guilds founded on the shard may be seated by another town (`BotSeat.Choose`). Patrick's order of "
        "29.09.2026.",
        ["The roam round home is now the roam round every reachable town, so a proposer that looks further "
         "than it walks sees further than it should.",
         "A town reached only through a gate has no road in the book and is not offered to travellers."],
    ),
    "BotResidence": (
        "where each bot lives",
        "Every bot has a town of its own: born in it, risen in it, walked home to it, hunting and gathering round it. "
        "`BotPopulation.HomeOf` is the one answer to every per-bot home and asks `BotResidence.Home`: in the population's "
        "home town that is the guild's hall or seat or the population's home, as before; elsewhere the town's bank, or the "
        "guild's hall or seat when it stands there. A member of a guild seated abroad lives by its guild; everybody else "
        "chooses among the towns fit to live in (a road in the book, a bank, shops), weighed towards the emptier and away "
        "from roads too strong for it, and may move — by walking there — after a poor living or deaths where it lives. "
        "Patrick's order of 29.09.2026, point 5.",
        ["A move changes the residence when it is taken, not when it is offered and not when it arrives: a bot that dies on "
         "the road rises in its new town.",
         "Population-wide questions — the roads' flood, the gates, the city's errands, the Baron — still ask "
         "`BotPopulation.Where`; only a per-bot home asks `HomeOf`."],
    ),
    "BotGuildHouse": (
        "guild houses in the towns",
        "A guild settles in an empty town building: the towns' regions are read for the engine's own door frames "
        "(`DoorGenerator`), both sides of every doorway flooded with the planner's step model, and the enclosed, roofed "
        "rooms of 64 to 400 tiles with at most two doorless ways in kept (cached in `Data/bot-guild-houses.bin` by the map "
        "files' fingerprint). What the world has put in one since — a spawner, a shopkeeper's home, a door, an inn's region, "
        "a house — is asked live. A guild wishing abroad takes a free building in a fit town with room (two a town), for "
        "nothing; a guild wishing for Britain raises its hall first, then pays 10,000gp to the city. A door keyed to the "
        "guild goes into every doorway and a sign naming it on the wall; the building's heart is the guild's seat "
        "(`BotSeat.Of`), where members are born and rise (`BotSeat.Home`), and its town is where they live "
        "(`BotRelocate.Forced`). The hall and the claims are still earned the old way. Patrick's idea of 29.09.2026, evening.",
        ["The door is unlocked in the engine's terms and refuses at the handle (`BotGuildDoor.Use`), because a locked shut "
         "door is a wall to the planner for members too: a door a member opened lets anybody through for twenty seconds.",
         "A guild's house outranks every other seat, but the hall is still carried towards the seat the hall itself last "
         "set (`BotSeat.Moving`), or an exile's move would be undone by the next carry."],
    ),
    "BotCamp": (
        "fires in the woods and a word on the road",
        "Some bots light a campfire in the wild when tired, bored or idle — the engine's own kindling, struck and fed by "
        "the engine's roll — and free bots within forty tiles are offered a seat at it, where they face the flame and talk: "
        "roads walked, the worst ground near, trade, their guild. Sitting at a lit fire is rest at the inn's rate, and a tired "
        "bot secure at one goes offline there, unpaid, for a sixth of its rest as regeneration. Two free bots passing in the "
        "open may stop for a word. Patrick's point 6 of 29.09.2026.",
        ["The engine's campfire burns a hundred seconds; a fire nobody feeds is out, and `BotCamp.Warm` carries it only "
         "thirty seconds between flames.",
         "Every deed here is unpaid, so its proposer asks every reason it fails before it is offered."],
    ),
    "BotRunes": (
        "Mark, Recall and the company's gate",
        "A mage of Magery 40 and up marks a rune where its guild holds none — its seat, a cave mouth, the town it "
        "stands in — and keeps it in its own book; the guild's knowledge of the place is kept in a store and "
        "written into the guild's library, a runebook in the hall's chest or with its keeper, which lends copies "
        "at a blank rune each. A far walk (a journey, the walk home, a far counter) asks for a recall first; a "
        "delve or a sweep asks for a Gate Travel, and the company steps through, the leader last. Every cast is "
        "the engine's spell. Patrick's order of 29.09.2026, point 7.",
        ["Nobody marks inside a Felucca dungeon (`SpellHelper` travel rules): a mouth's rune stands on the island "
         "side, and the company walks the last tiles through the mouth.",
         "A bot has no screen for the gate's confirmation gump: members are put through by the engine's own "
         "`Moongate.EndConfirmation`, not by stepping on the gate."],
    ),
    "BotSea": (
        "ships between the islands",
        "The water of the map charted once from the client files (cached in `Data/bot-sea-chart.bin`), a dock for "
        "each town a ship can berth at, and lanes between every pair of docks no road or gate joins. A traveller "
        "bound for a town it cannot walk to buys a small ship (the guild may help pay), boards it by the engine's "
        "own plank and key, and the helm moves it with `BaseBoat.Move` and `Turn` at the engine's speeds; ashore, "
        "the ship is dry-docked into the pack for the next voyage. Patrick's order of 29.09.2026, point 7.",
        ["A bot's speech carries no keywords, so the tillerman's commands are not used: the helm drives the ship "
         "itself on its own clock.",
         "A voyage with no headway for three minutes is wrecked: captain and crew are carried to the nearest dock "
         "and the ship is dry-docked — the only hand-move, counted and logged."],
    ),
    "BotProving": (
        "Argus's proving ground",
        "A bot's double against a creature in Green Acres, and a guild's company of doubles against a dungeon's "
        "worst room: the engine fights, the doubles answer with the bots' own hands, and what comes of it is the "
        "strength the delve judges a band by instead of health times weapon damage. Patrick's order of 26.09.2026.",
        ["A walkover is a lower bound, not a measurement: R is capped at 5 and nothing measured on a lower rung is "
         "carried up the ladder at more than the creature it beat, times 0.75. The first boot extrapolated an orcish "
         "mage beaten at R 20 into a Balron beaten at R 1.4.",
         "A band is as far as its weakest member goes, and a room is not its worst creature one at a time: the "
         "delve counts the leader, wants every member at a fifth of the worst, and past the easiest dungeon wants "
         "the guild's company to have cleared the worst room lately."],
    ),
    "BotEstate": (
        "the guilds' halls",
        "The one thing this population builds that outlives it. A guild saves in its chest — the members' dues, a "
        "fifth of what their work brings in (`BotDues`), and the tithes of its ground — levies the rest off its "
        "members, finds ground the engine will take a house on, raises a hall, and fits it out with the tools of its "
        "own trade. Next start the halls are read back out of the world by the name on their signs and handed to "
        "whoever leads that guild now.",
        ["A house is permanent and a population is not, so an unowned hall decays: `BotEstate.Adopt` is the "
         "only thing standing between an evening's building and an empty ruin.",
         "The price is a dial rather than the engine's 35,250gp, which no bot on this shard could ever "
         "reach. Watch the `short` clause in the summary before moving it.",
         "`BotEstate.Fund` is the chest plus the members' spare above the keep; the members' spare is the same pool "
         "`BotGuilds.Stand` spends on their armour and supplies every minute, so on a poor shard it reads nought and "
         "only the chest ever grows. The `Treasuries:` line prints both halves per guild."],
    ),
    "BotDiplomacy": (
        "meetings between guilds, and with their towns",
        "No war and no alliance without a meeting. A grievance at the war line, or a friendship at the alliance line, calls "
        "one (`BotRegard` → `BotParley.Grieve`/`Befriend`); a guild thought ill of asks for peace, and two on good terms "
        "propose trade. Any member walks as envoy (a guild of one sends its founder) to the host guild's town hall — the bank "
        "unless the file names one (`BotParley.Seat`) — the host sends an answerer, one of Argus's squad is summoned as a "
        "visible witness (`mindedBots/debugger/BotWitness`), the sides state their grievances from the books "
        "(`BotGrievances`), and the duke's word is asked of the model with the outcome an enumeration, or given by rule "
        "(`BotDuke`): war, peace, trade terms, standing orders, an alliance or nothing. Agreements change prices and "
        "service on the bots' market and forbid war (`BotPact`). Each guild also sends an envoy to its towns, is set a "
        "checkable task (`BotTownTask`) and pays the town's shops half a percent less or more per task (`BotBurgh`); three "
        "failures in a row put it out of the town for 48 hours. Patrick's order of 29.09.2026, evening.",
        ["A restart ends every meeting in flight; the grievance calls it again at its next drift. Agreements, the books, "
         "the pair clocks and the towns' standing are kept (`BotDiplomacyStore`).",
         "The duke's model answer is waited for two minutes after the last line and then given by rule; the log line "
         "`the word, by <who>` says which."],
    ),
    "BotRelics": (
        "relics and banners",
        "Five town relics — the Crown of Britannia, the Bell of Minoc, the Anchor of Trinsic, the Lantern of Vesper, the Scales "
        "of Yew — and one banner per guild, each an engine item the world save keeps and a registry (`BotRelics`, kept by "
        "`BotRelicStore`) that says whose it is. A town relic is laid in a lair behind a warden (`BotRelicLair`); the company "
        "that clears it takes it and walks it home (`BotRelicHomeward`); its holder's members pay 95 % at that town's counters "
        "(through `BotBurgh.Factor`), the guild gains appeal (`BotGuilds.Appeal`) and calls the Great Fair once a day "
        "(`BotFair.CallGreat`). A thief may lift one from a carrier (`BotRelicSnatch`) and the fence sells it back; a relic in a "
        "hall is taken only by a war: the loser's banner goes to the winner (`BotWar.End` → `BotRelics.WarEnded`), the loser "
        "may declare a war of revenge past a truce (`BotWar.MayDeclare` → `BotRelics.Avenges`) and buys its banner back chest "
        "to chest. Patrick's order of 07.10.2026.",
        ["A relic item is `Movable = false` and blessed: everything that moves it is code, and the registry's look every 30 "
         "seconds puts it where the registry says. Never delete a `BotRelicItem` by hand without `do relic <name> ; hide`.",
         "A restart hides a relic that was in a lair and takes its warden away; every other relic stays where it was."],
    ),
    "BotQuad": (
        "the island as squares",
        "The map cut into squares thirty tiles across, each carrying one number: how safe the population has "
        "found it to be. It is written by everything that dies or kills and read by the captain, the Baron "
        "and anything choosing where to go. Also the frontier — the nearest square nobody has ever stood in "
        "— and the scouting that fills it in.",
        ["This one *is* saved between restarts, unlike the ground survey."],
    ),
    "BotRanger": (
        "livery",
        "One file: the King's Rangers' kit, which is the Baron's livery on five more bodies.",
        [],
    ),
    "BotAlarm": (
        "the smoke alarm",
        "The one place the shard speaks first. Six rules are read once a minute, each dividing a number the "
        "other subsystems already keep by something that gives it meaning, and anything past a threshold "
        "becomes a line of JSON in `logs/alerts.ndjson` — raised once, repeated at most every fifteen "
        "minutes, and cleared in its own event when it goes back to normal. An hourly heartbeat says so "
        "when nothing is wrong. Errors are counted by reading the tail of the shard's own session log, "
        "which is the only way to see them all without modifying the engine's logger.",
        ["Every threshold is a dial, so tuning happens through the debugger's door on a living shard rather "
         "than by restarting: `dial BotSigns.ErrorsAt 3`.",
         "A window equal to the tick fires every other tick. `BotSigns.Due` carries half a tick of slack for "
         "that reason, and every event reports the window it actually measured rather than the one intended.",
         "The error count is read through a buffered redirect and can lag the world by a buffer. Good for "
         "noticing, useless for timing."],
    ),
    "BotVoice": (
        "what the bots say",
        "Speech as a by-product of doing: a line rides on work taken up or ended, a blow, a death, a war, a "
        "hall, an order, and now and then a mood. Five channels — local, guild, world, mood, cry — each an "
        "audience; every line is also an event on the stream and a line in `logs/bot-speech.log`. The phrase "
        "bank is written whole to `bot-voice.json` so the words can be changed without the code. Rationed per "
        "bot and channel, never per shard.",
        ["Nothing here is offered to the auction and nothing costs a beat: the voices must never change what "
         "the population does.",
         "The occasions are the will's own hooks (`BotWill.Started`, `BotWill.Ended`), the damage and death "
         "hooks on `BotMobile`, and three lines in `BotWar` and `BotHall`."],
    ),
    "BotSocial": (
        "bots answering each other's moods",
        "A mood line carries one word behind it — hurt, tired, grieving, poor, lonely, bored, proud, or a shade of "
        "content (`BotMoods`, read off facts the shard keeps) — and is said only when somebody is in earshot, waiting up "
        "to ten minutes for an audience. The nearest two listeners not of a hostile guild answer it a few seconds later: "
        "always in words, and where they can with help — a bandage, a fire laid beside the tired, the nearest quarry "
        "called as a hunt for two for the poor, a place in a company for the lonely — written against the listener as "
        "an invitation and handed to its own auction by `BotFellow`, where a seam that pays keeps the bot and an idle bot "
        "goes. Offers taken are followed through the will's own hooks to how they ended. Patrick's point 4 of 01.10.2026.",
        ["The listener answers the tag, never the speaker's insides; the deed that carries the help out checks the world "
         "itself (a bandage is wound only on a wound).",
         "No handouts: a poor bot is pointed at a hunt. A company's call (`BotSquads.Enrol`) is the one answer that is "
         "not an auction, and it takes only a bot whose work resumes and is not on paying work.",
         "`Society:` counts what was heard by mood, offered by kind, accepted, finished, lapsed, and refused for hostility; "
         "`Voice:` counts moods said to somebody against moods said to nobody."],
    ),
    "BotWeb": (
        "the shard as a page",
        "The whole population as one JSON document on `127.0.0.1:2599`, rebuilt on the game loop every two "
        "seconds and handed to an `HttpListener` that reads nothing from the world; an event stream "
        "(`bot-events.ndjson`) with sequence numbers and server-sent events; a picture of the map from the "
        "client's `radarcol.mul`; the crafting chain as the craft systems define it; a minute of history at a "
        "time. The page is `Distribution/Data/bot-web/index.html`.",
        ["Every number on the page is a counter the log already prints, so the two cannot disagree; the one "
         "addition is the split of searches by purpose in `BotWalk`.",
         "The map is sampled in slices on the loop because the tile matrix is not safe off it, and encoded off "
         "the loop because encoding needs no world."],
    ),
    "BotDashboard": (
        "watching it happen",
        "`[bots` — an administrator command opening five tabs: the population, their market, what they are "
        "short of, what the city wants, and what the population is doing.",
        [],
    ),
    "mindedBots": (
        "bots that think",
        "The four crafters — and from 07.09.2026 nobody else — choose what to do next through a local "
        "language model over Ollama rather than through the auction. The model is given what the bot can "
        "see and returns a choice; everything else about them is an ordinary bot. The warrior, architect, "
        "sage and Baron minds are commented out in `BotMinds.Start` rather than deleted.",
        ["The model is asked on a wall clock and the answer costs real seconds. Anything that waits on it "
         "must not be holding the game loop.",
         "A crafter mind is shown four blocks nothing else gets: its own bench, the board of wants (with "
         "the craft and skill each needs), what is on the stalls, and what the other three are holding. "
         "The last of those exists because four minds given identical state make identical choices."],
    ),
    "mindedBots/debugger": (
        "Argus, the observer",
        "A thinking thing that is not one of the population: an invisible figure that nobody in the world can "
        "see, that cannot be hurt and cannot hurt anything, whose whole job is to watch the bots and write "
        "what it believes into `logs/bot-debugger.log`. It has a door for a person at the keyboard "
        "(`argus-in.txt`) and a small set of administrator gestures.",
        ["Five false alarms in one day, all of them artefacts of the instrument rather than faults in the "
         "shard. Check the watcher before believing it about the population."],
    ),
}

# folder -> the summary lines it writes into. Verified against a live log rather than derived, because a
# clause can reach a line through two levels of Describe().
LINES = {
    "BotPopulation": ["Getting about:", "The market:", "What we know:", "Money:"],
    "BotWill": ["What we know:"],
    "BotMovement": ["Getting about:"],
    "BotOutfit": ["The ground:"],
    "BotHarvest": ["The ground:"],
    "BotCraft": ["Needs:"],
    "BotShops": ["Needs:"],
    "BotMarket": ["The market:", "The board:"],
    "BotShopkeep": ["Bot shops:"],
    "BotSpells": ["Arms:"],
    "BotCombat": ["Arms:", "Bows:"],
    "BotMend": ["Arms:"],
    "BotSquad": ["Companies:", "Arms:", "Bows:", "Needs:", "The ground:"],
    "BotHunt": ["Companies:"],
    "BotDrill": ["The captain:"],
    "BotBaron": ["The Baron:"],
    "BotDelve": ["Delving:"],
    "BotResidence": ["Residences:"],
    "BotSea": ["Sea:"],
    "BotProving": ["Proving:"],
    "BotEstate": ["Estate:", "Treasuries:"],
    "BotDiplomacy": ["Diplomacy:"],
    "BotRelics": ["Relics:"],
    "BotGuildHouse": ["Houses:"],
    "BotQuest": ["Quests:"],
    "BotQuad": ["The captain:"],
    "mindedBots": ["Minds:"],
}

TYPE_SUMMARY = re.compile(
    r"///\s*<summary>\s*(.*?)///\s*</summary>\s*(?:///.*?\n\s*)*?(?:\[[^\]]*\]\s*)*"
    r"(?:public|internal|sealed|static|abstract|partial)[^\n]*\b(?:class|struct|enum|interface|record)\b",
    re.S,
)


def read(path):
    with open(path, "rb") as handle:
        return handle.read().decode("utf-8-sig", errors="replace")


def purpose(text):
    found = TYPE_SUMMARY.search(text)

    if not found:
        return ""

    body = re.sub(r"^\s*///\s?", "", found.group(1), flags=re.M)
    body = re.sub(r"<[^>]+>", "", body)
    body = " ".join(body.split())
    sentence = re.match(r"(.{5,200}?[.])(\s|$)", body)

    return (sentence.group(1) if sentence else body[:160]).strip()


DIAL = re.compile(r"public static ([\w<>?\[\]]+) (\w+) \{ get; set; \} = ([^;]{1,60});")

# A configuration file can only reach a dial through a line like `BotMarket.StaleMs = settings.StaleMs ??`
# in that subsystem's *Config.cs. Anything not on the left of one of those needs a rebuild to change.
SETTABLE = re.compile(r"(\w+)\.(\w+)\s*=\s*(?:settings|cfg|c)\.(\w+)\s*\?\?")


def collect():
    """Everything the blocks are built out of, read once per file."""
    folders = {}

    for root, dirs, files in os.walk("."):
        dirs[:] = [d for d in dirs if d not in ("obj", "bin")]

        folder = os.path.relpath(root, ".").replace(os.sep, "/")
        folder = ROOT if folder == "." else folder

        for name in sorted(f for f in files if f.endswith(".cs")):
            text = read(os.path.join(root, name))
            stem = name[:-3]
            here = folders.setdefault(
                folder,
                {"files": [], "proposers": [], "module": None, "config": None, "deeds": set(),
                 "dials": [], "settable": {}},
            )

            here["files"].append((name, purpose(text)))

            for dial in DIAL.finditer(text):
                here["dials"].append((stem, dial.group(2), " ".join(dial.group(3).split())))

            for reach in SETTABLE.finditer(text):
                here["settable"][(reach.group(1), reach.group(2))] = reach.group(3)

            if re.search(r"class\s+%s\s*:\s*[^{\n]*BotModule" % re.escape(stem), text):
                here["module"] = stem

            if re.search(r"class\s+%s\s*:\s*[^{\n]*BotDeed" % re.escape(stem), text):
                here["deeds"].add(stem)

            found = re.search(r"Configuration/(bot-[a-z-]+\.json)", text)

            if found and not here["config"]:
                here["config"] = found.group(1)

            if re.search(r"class\s+%s\s*:\s*[^{\n]*IBotProposer" % re.escape(stem), text):
                offers = re.search(r'public string Name\s*=>\s*"([^"]+)"', text)
                rung = re.search(r"Rung\s*=>\s*BotStanding\.(\w+)", text)
                here["proposers"].append(
                    (stem, offers.group(1) if offers else stem, rung.group(1) if rung else "?", text)
                )

    # A proposer's deeds are whatever it constructs that is a deed anywhere in the assembly.
    every = set().union(*(f["deeds"] for f in folders.values())) if folders else set()

    for here in folders.values():
        here["proposers"] = [
            (stem, offers, rung, sorted(set(re.findall(r"new (Bot[A-Z]\w+)\(", text)) & every))
            for stem, offers, rung, text in here["proposers"]
        ]

    return folders


def block(folder, data):
    title, says, traps = BLOCKS.get(folder, ("", "", []))
    out = []

    out.append("")
    out.append("### `%s/`%s" % (folder, " — " + title if title else "") if folder != ROOT
               else "### root%s" % (" — " + title if title else ""))
    out.append("")

    if says:
        out.append(says)
        out.append("")

    facts = []

    if data["module"]:
        facts.append("**Module** `%s`" % data["module"])

    if data["config"]:
        facts.append("**Config** `%s`" % data["config"])

    for line in LINES.get(folder, []):
        facts.append("**Writes** `%s`" % line)

    if facts:
        out.append(" · ".join(facts))
        out.append("")

    if data["proposers"]:
        out.append("| offers work | as | on the rung | handing out |")
        out.append("|---|---|---|---|")

        for stem, offers, rung, deeds in sorted(data["proposers"]):
            out.append("| `%s` | %s | %s | %s |" % (
                stem, offers, rung, ", ".join("`%s`" % d for d in deeds) or "—"))

        out.append("")

    for trap in traps:
        out.append("- **Trap.** %s" % trap)

    if traps:
        out.append("")

    out.append("| file | decides |")
    out.append("|---|---|")

    for name, says_it in data["files"]:
        out.append("| `%s` | %s |" % (name, says_it.replace("|", "/")))

    return out


def dials(folders, order):
    """DIALS.md: every tunable in the assembly, its default, and whether a config file can reach it."""
    out = [
        "# BotAI v2 — Dials",
        "",
        "Every `public static` number in the assembly, the value it is written with, and the configuration",
        "key that overrides it. Generated by `regen-map.py`; see `MAP.md` for where any of these live.",
        "",
        "**A default here is not what the shard is running.** Whatever `Distribution/Configuration/bot-*.json`",
        "sets wins, and a key spelled in the wrong case is silently ignored rather than refused — so the only",
        "proof of a live value is the line the module writes at boot. A dial marked *code only* cannot be",
        "changed without a rebuild.",
    ]

    for folder in order:
        if folder not in folders or not folders[folder]["dials"]:
            continue

        here = folders[folder]
        out.append("")
        out.append("## `%s/`%s" % (folder, "" if not here["config"] else "  —  `%s`" % here["config"]))
        out.append("")
        out.append("| class | dial | default | config key |")
        out.append("|---|---|---|---|")

        for owner, field, value in sorted(here["dials"]):
            key = here["settable"].get((owner, field))
            out.append("| `%s` | `%s` | `%s` | %s |" % (
                owner, field, value, "`%s`" % key if key else "*code only*"))

    with open("DIALS.md", "w", encoding="utf-8", newline="\n") as handle:
        handle.write("\n".join(out) + "\n")

    return sum(len(f["dials"]) for f in folders.values()), sum(len(f["settable"]) for f in folders.values())


ROW = re.compile(r"^\| `([A-Za-z0-9_]+\.cs)` \|")


def drift():
    """Subsystem READMEs whose file table no longer matches the folder. Reported, never rewritten."""
    out = []

    for folder in sorted(os.listdir(".")):
        path = os.path.join(folder, "README.md")

        if not os.path.isfile(path):
            continue

        with open(path, encoding="utf-8") as handle:
            lines = handle.read().split("\n")

        # The first contiguous run of file rows is the "What is in it" table; later tables are other things.
        first = last = None

        for i, line in enumerate(lines):
            if not ROW.match(line):
                continue

            if first is None:
                first = last = i
            elif i == last + 1:
                last = i
            else:
                break

        if first is None:
            continue

        listed = {ROW.match(lines[i]).group(1) for i in range(first, last + 1)}
        actual = {f for f in os.listdir(folder) if f.endswith(".cs")}

        if actual - listed or listed - actual:
            out.append((folder, sorted(actual - listed), sorted(listed - actual)))

    return out


def main():
    folders = collect()
    order = ORDER + [f for f in sorted(folders) if f not in ORDER]
    lines = []
    rows = 0

    for folder in order:
        if folder not in folders:
            continue

        rows += len(folders[folder]["files"])
        lines += block(folder, folders[folder])

    text = read("MAP.md")

    if BEGIN not in text or END not in text:
        raise SystemExit("MAP.md has lost its SECTION2 markers")

    head, rest = text.split(BEGIN, 1)
    _, tail = rest.split(END, 1)

    with open("MAP.md", "w", encoding="utf-8", newline="\n") as handle:
        handle.write(head + BEGIN + "\n" + "\n".join(lines) + "\n" + END + tail)

    undescribed = [f for f in folders if f not in BLOCKS]
    blank = [n for f in folders.values() for n, s in f["files"] if not s]

    counted, reachable = dials(folders, order)

    print("%d files in %d subsystems" % (rows, len(folders)))
    print("%d dials in DIALS.md, %d of them reachable from a configuration file" % (counted, reachable))

    if undescribed:
        print("no block written for: " + ", ".join(sorted(undescribed)))

    if blank:
        print("no class summary, so no row text: " + ", ".join(blank))

    for folder, undocumented, ghosts in drift():
        parts = []

        if undocumented:
            parts.append("missing " + ", ".join(undocumented))

        if ghosts:
            parts.append("lists absent " + ", ".join(ghosts))

        print("%s/README.md has drifted: %s" % (folder, "; ".join(parts)))


if __name__ == "__main__":
    main()
