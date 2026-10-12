# BotAI v2 — Map

**Read this instead of searching.** `ARCHITECTURE.md` says why the design is the way it is; this file says
*where things are*, so that a question can be answered by opening one or two files rather than by walking a
tree of 315 of them. Every table here is a lookup. Nothing here explains a decision.

The tables in §2 are generated from each file's own class summary. When a file's purpose changes, its doc
comment is the thing to change; this file is regenerated from those, never edited row by row.

- Where a thing is → **here**, §2: one block per subsystem, and every file in it
- What a number is set to → `DIALS.md`, generated beside this one
- The shape of the whole thing → `ARCHITECTURE.md`
- Why a decision is the way it is → that subsystem's `README.md`

**A warning about those READMEs.** They are written by hand and they fall behind. On 05.09.2026 four of
them described a shard that no longer existed — nine classes where the engine reports thirteen, three
dashboard tabs where it opens five, "nothing calls `BotSquads.Form`" where three things do, and a Baron
"not raised" who has been raised for days — and fourteen listed fewer files than their folders held.
All of that is corrected, and `regen-map.py` now prints a line for any README whose file table has drifted
again, because the way it accumulated was silently. Read them for reasoning; read §2 below, which is
generated from the source, for facts.

---

## 0. Running it

| | |
|---|---|
| Start | `START_SHARD.bat` from the repo root (`--nopause` from a script) — hidden, detached through a scheduled task with no triggers, waits until it reports listening. No autostart |
| Stop | `do save` through Argus first (a kill without it rolls the world back to the last autosave, up to five minutes of halls and moves), then `taskkill /F /IM ModernUO.exe`. Not while a war stands or a hall is owed a move if the running build is older than 17: until `BotWarStore`, a restart ended every war and forgot every truce |
| Watch while working | `bash shard-status.sh` — one screen: process, wars with score and clock, blood, halls and seats, work, newest alarms; see `PLAN-wars-and-seats.md` §1 |
| Log | `logs/session-<yyyy-MM-dd_HH-mm>.log`, one file per start |
| Port | `127.0.0.1:2593` |
| Build | `dotnet build ModernUO.slnx -c Release` |
| Configuration | `Distribution/Configuration/bot-*.json`, one file per subsystem |
| Watch for trouble | `tail -f logs/alerts.ndjson` — the shard raises its own alarms there, one JSON line each, with an hourly heartbeat so silence can be trusted |
| Move a guild | `do seat <guild> <x> <y>` through Argus; `do seats` and `do wars` to look |
| Meetings between guilds, and with towns | `do parley` through Argus to look; `do meet <guild> ; <guild or town> [; grievance\|safety\|friendship\|commerce]` to call one now |
| Change a number without restarting | write `dial <Class.Name> <value>` into `Distribution/argus-in.txt`, read `argus-out.txt`; `dials <word>` to find one. All 571 are reachable, journalled to `logs/bot-dials.log`, and lost on restart |
| Rebuild §2 and `DIALS.md` | `python regen-map.py` from this folder |

Three facts worth having in front of you rather than rediscovering:

**A running shard holds the assemblies.** Building while it runs still *compiles* — it fails only at the copy
into `Distribution/Assemblies`. So `dotnet build … | grep "error CS"` is a valid syntax check against a live
shard; `Errors: 2` (localised on a non-English SDK) with nothing but `MSB3021`/`MSB3027` means the code is fine
and the shard is up.

**Configuration keys are PascalCase and a wrong key is silent.** A lowercase key is not an error and not a
warning — the value is simply the code default. The only proof of what a dial is actually set to is the boot
lines the modules write at startup.

**Restarting resets what was surveyed.** The ore map, the hearth list and the counter list are built by bots
walking around and are not saved. A restart therefore *cures* "the island has run out of ore" and makes any
measurement about exhaustion impossible for the first two or three hours. Never delete
`Distribution/Saves/Mobiles` or `Items` — Patrick's own character lives there.

---

> **The level sits outside the bracket: `[19:57:35 ERR]`.** A pattern of `\[ERR` matches nothing in this log
> and reports a clean run for ever — which it did here for a whole evening, through twenty-six real errors,
> while the shard was saying a Baron had stopped moving and that bronze ingots nobody wants were piling up on
> the market. Grep for `' ERR]'`, with the space. The instrument that says everything is fine is the one
> worth checking first.

## 1. Reading a summary line

Every five minutes (`BotBeat.SummaryMs`, `BotSquads.SayEveryMs`, both 300000) the population writes one block
of lines. Each line is assembled from `Describe()` on the classes that own the counters. **A number in the
summary is always a static counter on one class**, so the line prefix is the fastest route into the code
there is.

> **Every number is cumulative since the shard started, not a figure for the last five minutes.** The
> matching `Forget()` exists but is called from each module's `Reset()`, which runs on a *world reload* and
> nothing else. The naming invites the opposite reading and it is worth being certain about, so here is the
> proof — three consecutive summaries of one run:
>
> ```
> asked to brew:  293   554   857
> asked to cook: 1207  2354  3592
> market sales:    46    79   120
> ```
>
> **What follows from it.** Two readings of the same counter with the same value mean nothing happened in
> between, not that it happened twice. A rate has to be taken as a difference between summaries and divided
> by the interval. And two runs can only be compared at the same age: a first summary against a first
> summary. Comparing a five-minute-old shard's total against a forty-minute-old one's is comparing a rate
> against nothing at all — which is a mistake that has been made in this project's own notes.

| line begins | assembled in | fed by |
|---|---|---|
| `Will:` | `BotWill/BotWill.cs` | itself — the census: what the whole population is doing right now |
| `Companies:` | `BotSquad/BotSquads.cs` | `BotMuster`, `BotEnlist`, `BotSpoils`, `BotSquad`, `BotCompanyRecord` (the company record: kinds priced by companies' own fights — 03.10.2026), `BotRaise` (raised where they fell), `BotLifeline` |
| `Arms:` | `BotSquad/BotSquads.cs` | `BotCry`, `BotArms`, `BotArmoury`, `BotMedic` |
| `Bows:` | `BotSquad/BotSquads.cs` | `BotBolt` |
| `Needs:` | `BotSquad/BotSquads.cs` | `BotUpkeep`, `BotBullion`, `BotSmith`, `BotTailor`, `BotFletcher`, `BotAlchemist`, `BotScribes` (the `scrolls:` clause: scribes able by circle, what each look offered, sessions and scrolls by what they were for, scroll wants at the price of writing them — 03.10.2026), `BotCook`, `BotStores` |
| `The ground:` | `BotSquad/BotSquads.cs` | `BotGround`, `BotWoodsman`, `BotStable` |
| `Getting about:` | `BotPopulation/BotBeat.cs` | `BotPath`, `BotStep`, `BotJourney`, `BotDanger`, `BotSight` (the `sight:` clause: creatures a walker saw and could not beat, the walks changed for them by kind, and deaths by what the bot had made of its killer — 03.10.2026) |
| `The market:` | `BotPopulation/BotBeat.cs` | `BotAuction`, `BotHaggle`, `BotListing` |
| `Lines:` | `BotPopulation/BotBeat.cs` | `BotLines` — Patrick's Needs measure: each line a bot asks for, met in its first hour or not, by whom, and the cause of each failure; the `needs` block of `/api/state` holds the same counters (02.10.2026, build 391) |
| `Bot shops:` | `BotShopkeep/BotShopkeepModule.cs` | `BotShopkeep`, `BotShopkeeper`, `BotPatron`, `BotShopBook`, `BotAuction.Capped` — shops by the banks: shifts, sales to bots short of their kit or with a want on the board, the customer's weighing against counter and stall, the till, and the auction's lot cap (29.09.2026) |
| `What we know:` | `BotPopulation/BotBeat.cs` | `BotLedger` |
| `Money:` | `BotPopulation/BotBeat.cs` | `BotPurse` |
| `The island:` | `BotPopulation/BotBeat.cs` | `BotQuad`, `BotHunter` |
| `At death's door:` | `BotPopulation/BotBeat.cs` | `BotMobile`, `BotShopper` |
| `Standing still:` | `BotPopulation/BotBeat.cs` | `BotStall`, `BotHomer` |
| `Gathering:` | `BotPopulation/BotBeat.cs` | `BotForager`, `BotHerbalist`, `BotPicker`, `BotOutfit` |
| `Trade:` | `BotPopulation/BotBeat.cs` | `BotShops`, `BotPeddler`, `BotQuarry`, `BotPopulation`, `BotAskFirst` — restocks asked of the island before the walk to a counter, by family (03.10.2026) |
| `The captain:` | `BotDrill/BotDrillModule.cs` | `BotDrill`, `BotSchool`, `BotLesson`, `BotAttend`, `BotArmourer` |
| `The Baron:` | `BotBaron/BotBaronModule.cs` | `BotHarrow`, `BotRounds`, `BotStroll`, `BotStipend` |
| `Minds:` | `mindedBots/BotMinds.cs` | `BotMind`, `BotMindChoice`, `BotOllama` |
| `The clock:` | `BotPopulation/BotBeat.cs` | itself, `BotWill.SpentMs`, `BotWalk.SpentMs` — loop milliseconds spent on the population, split into deciding and walking (13.09.2026) |
| `Wars:` | `BotPopulation/BotBeat.cs` | `BotWar` (the wars standing, their score and rules), `BotSeat` (where each guild lives) — split off `Estate:` on 13.09.2026 |
| `Diplomacy:` | `BotPopulation/BotBeat.cs` | `BotParley` (meetings called, walked, witnessed and heard; the duke's verdicts by `BotDuke`), `BotEnvoy`/`BotHerald`, `BotPact` (agreements and what they changed on the market), `BotGrievances`, then `BotBurgh` (each guild's standing in its towns, tasks set, done and failed, exiles, rebates and surcharges) — 29.09.2026 |
| `Treasuries:` | `BotEstate/BotDues.cs` | `BotChest`, `BotEstate.Spare`, `BotGuilds.Stood`, `BotGuildHouses`, `BotClaim` — per guild: chest and members' spare against what it saves for, dues paid in, and its seat, hall and squares (29.09.2026) |
| `Houses:` | `BotGuildHouse/BotGuildHouses.cs` | itself, `BotHouseSurvey` — which guild lives in which town building, who waits for one and why, the free buildings town by town (29.09.2026) |
| `Camp:` | `BotCamp/BotCamp.cs` | itself, `BotCamper`, `BotKindle`, `BotBeckon`, `BotFireside`, `BotCampTalk`, `BotMeetings` — fires lit, joined, slept by, chats on the road (29.09.2026) |
| `Society:` | `BotSocial/BotSocial.cs` | itself, `BotMoods`, `BotFellow` — mood lines heard by mood, answers, offers by kind, accepted, finished, lapsed, refused for hostility (01.10.2026) |
| `Resolve:` | `BotWill/BotWill.cs` | itself — commitment: endings by kind, drops by cause (would not wait, summoned, a rung above, a full pack, outbid), offers a hold refused, holds lifted for trouble, work put down and taken up again, trades taken back within ten minutes of being dropped; the same per trade (14.09.2026) |
| `Roles:` | `BotClasses/BotCalling.cs` | `BotWill.Settle` — each class's working minutes split into its own trade, anybody's work and another class's trade, with the four kinds it spent most on (14.09.2026) |
| `Relics:` | `BotRelics/BotRelicsModule.cs` | `BotRelics` (the registry: town relics hidden and held, banners and trophies, hangings, handovers, war's spoils, grudges, revenge let past a truce, banner ransoms and returns, the fence's ransoms and sales, rebates at the relic's town and Great Fairs), `BotRelicLair` (lairs laid, captains, companies, wardens, cleared, lapsed, deaths), `BotRelicHomeward`, `BotRelicSnatch` — 07.10.2026 |
| `Stewards:` | `BotPolitics/BotStewardship.cs` | each town's steward (`BotStewardship`): tax, coffer, regard; chosen and deposed; decrees by kind and by rule, model or Argus (`BotStewardshipDecrees`); the coffers' coin drawn, taxed, paid and held; bars, refused work and walks turned back — 07.10.2026 |
| `Events:` | `BotEvents/BotSeasons.cs` | `BotInvasion` (raids called, answered, waves, heads, kills, deaths on the field, head money), `BotMilitia`, `BotFair` (counter prices, fairgoers, lottery), `BotCaravans` (posted, led, arrived, lost, wages, premiums), `BotNight`, `BotRumour`, and the city's coin held, paid and handed back — 07.10.2026 |

`Spells ready:` is written once at boot by `BotSpells/BotSpellsModule.cs`, not every five minutes.

Per-bot lines, which are the other half of the log:

| line shape | written by | says |
|---|---|---|
| `X took on <trade>: <stage>: N/min = …` | `BotWill/BotWill.cs` | the auction was held and this won; the factors are the whole reckoning |
| `X finished <trade>: … — <why>` | `BotWill/BotWill.cs` | a round ended having produced something |
| `X failed at <trade>: … — <why>` | `BotWill/BotWill.cs` | a round ended having produced nothing; `<why>` is the deed's own words |
| `over <trade>: <stage> at N/min` | `BotWill/BotWill.cs` | what the winner beat — the runner-up and its price |
| `; put down <trade> to take up again (<cause> by …)` on a take line | `BotWill/BotWill.cs` | steadfast work paused rather than dropped, because what took the bot off it would not wait |
| `X took up <trade> again after <other>, Ns after putting it down` | `BotWill/BotWill.cs` | the paused work resumed when the interruption ended |
| `X dropped <trade>: … — outbid by <other> at N/min after A of B minutes reckoned, taken at T/min and worth W/min by then` | `BotWill/BotWill.cs` | a real change of mind, with the numbers that decided it |
| `X opened a shop by the bank at (x, y) in <town>: …` / `X shut its shop in <town> after N minutes: …` | `BotShopkeep/BotShopkeep.cs` | a shift of a bot's shop begun and ended, with its stock and its takings |
| `X bought N <kind> at Y's shop in <town> for Ngp, P each — short of it for its kit` / `— against its own want on the board` | `BotShopkeep/BotShopkeep.cs` | one sale at a bot's shop, and which need brought the buyer |

---

## 2. Every file, and the one thing it decides

<!-- Generated from each file's class summary by `regen-map.py`. Change the doc comment, then
     rerun the script; never edit a row here. -->

<!--SECTION2:BEGIN-->

### root — the way in

One file. `BotCore.Configure` and `BotCore.Initialize` are found by the engine's reflection the same way it finds content, and everything this assembly does hangs off them: the stores that must exist before the world is read, the regeneration hooks, and the registration of every module in the order they are listed there.

- **Trap.** `BotCore.Configure` runs after `RegenRates.Configure` by loader ordering rather than by contract. If that ever inverts, meals stop quickening recovery — they do not break anything.

| file | decides |
|---|---|
| `BotCore.cs` | The way in. |

### `BotModules/` — the loading frame

Holds the subsystems, works out what order to start them in, starts them, and says what happened. Two phases: `Settings` runs before the world exists and may only read numbers and files; `World` runs after it is in memory and may ask where things are. A module can be switched off from its own configuration file without touching anything else.

- **Trap.** A subsystem that asks the map a question in the `Settings` phase gets a world that is not there yet. Phase is the first thing to check when something is null only at boot.

| file | decides |
|---|---|
| `BotModule.cs` | One switchable, ordered piece of the population. |
| `BotModules.cs` | Holds the modules, works out the order, starts them and says what happened. |
| `BotPhase.cs` | When a module is allowed to run. |

### `BotPopulation/` — what a bot is, and the clock

`BotMobile : PlayerMobile` is the bot. One timer serves the whole population and gives each bot a turn on its own schedule — `BotBeat`, which is also where four of the summary lines are written. This folder also owns what survives a restart (`BotProgress`: skills, fame, karma, savings) and the three pieces of work that belong to no trade: walking home, going back for what death took, and taking a full pack to a counter.

**Module** `BotPopulationModule` · **Config** `bot-names.json` · **Writes** `Getting about:` · **Writes** `The market:` · **Writes** `What we know:` · **Writes** `Money:`

- **Trap.** A bot must be Player-flagged or death deletes it outright, and a dead bot counts as alive and silently drops out of the beat.
- **Trap.** `BotProgress` is the only place skills persist. Zeroing it is not the same as touching the world save, and the world save holds Patrick's own character — never delete it.

| file | decides |
|---|---|
| `BotBeat.cs` | The population's clock. |
| `BotBirth.cs` | A newborn's temperament: the same birth points as its class deals, leaned towards one skill of the class's own. |
| `BotCharter.cs` | What a guild's maker has told the guild to be doing: a trade to gather at, a trade to make at, and somewhere to be. |
| `BotCritical.cs` | The posts the island is never left without: a floor of each kept in the world at every hour, whoever is past their hours. |
| `BotGrowth.cs` | Newcomers: a couple of novices raised every two hours, so the island has old hands and beginners at once. |
| `BotGrowthStore.cs` | Keeps the newcomers across restarts. |
| `BotGuildSplitStore.cs` | Keeps across restarts who left which guild and when, and the clocks of the splits. |
| `BotGuildWidenStore.cs` | Keeps the guilds' widenings across restarts. |
| `BotGuilds.cs` | Who each bot belongs to, and what that belonging is worth. |
| `BotHomeward.cs` | Walking back to where the population lives, when there is nothing else to do and the bot is a long way from it. |
| `BotInns.cs` | The inns and taverns of the map, and what a bot gets for sleeping in one. |
| `BotMobile.cs` | An autonomous inhabitant of the shard. |
| `BotPeoples.cs` | One people of the population, as Configuration/bot-names.json describes it. |
| `BotPopulation.cs` | Who exists. |
| `BotPopulationConfig.cs` | What Configuration/bot-population.json is allowed to say. |
| `BotPopulationModule.cs` | The population as a module: reads who should exist, deletes whoever came back from the save, raises the rest, and starts the clock. |
| `BotPrime.cs` | Prime time: how much of the island is meant to be in the world at each hour of the day. |
| `BotProgress.cs` | What a bot has become, kept across restarts: its skills, its fame, its karma and its savings. |
| `BotPurse.cs` | What a bot keeps in its pocket, and what it puts away the moment it is standing somewhere it can. |
| `BotReclaim.cs` | Going back for what death took. |
| `BotRest.cs` | How long a bot plays before it is tired, and the rest it takes before it plays again. |
| `BotRestConfig.cs` | One band of the prime time schedule as the file writes it: from an hour, to an hour, the share of the island in the world. |
| `BotRestStore.cs` | Keeps every bot's play and rest across restarts. |
| `BotRoster.cs` | Who a guild's leader may put out and who it may take in, and how often. |
| `BotStall.cs` | Notices a bot that has stopped getting anywhere, and says so as an error. |
| `BotSurplus.cs` | A bot's bank box kept in order, and where the surplus of its capped kinds went: the box, a shopkeeper, another bot, the ground, or nowhere because the box had no place for it (06.10.2026, box16). |
| `BotTidy.cs` | What a bot carries and wears, kept in order: an unidentified thing is identified — off a corpse, in the pack or worn — and the robe the engine dresses a risen body in is taken off. |
| `BotUnload.cs` | Going to the counter when the pack is getting heavy: coin into the account, everything spare into the bank box — except what somebody near has said it wants, wh |

### `BotWill/` — the decision

Every turn a bot asks one question and this answers it. Three stages: the ladder says which rung the bot is on from facts alone, obligations are taken before anything is auctioned, and then every proposer registered by every other subsystem offers work which is priced per minute and the best offer wins. Prices are *measured* — `BotLedger` corrects a trade's opening guess by what the work really paid — rather than assigned by weights. Every `took on` / `finished` / `failed at` line in the log is written here, and the factors printed beside the price are the whole reckoning.

**Module** `BotWillModule` · **Config** `bot-will.json` · **Writes** `What we know:`

- **Trap.** A multiplier that can reach zero is a veto: every factor needs a floor.
- **Trap.** A deed that answers `Work` for ever, or answers with the same walk for ever, is immortal and invisible. Both have happened; both cost a session.
- **Trap.** The walk order a deed returns must be stable between ticks, because arrival is compared against it. A destination recomputed every tick resets the route every tick.

| file | decides |
|---|---|
| `BotAppraisal.cs` | What a score was made of. |
| `BotBreaker.cs` | Work one bot keeps failing for the same reason is not offered to that bot for a while. |
| `BotCommons.cs` | What the population as a whole has found out about what pays where. |
| `BotCommonsStore.cs` | Keeps what the population has found out about what pays where across restarts. |
| `BotDeed.cs` | One undertaking: a piece of work with stages, held until it finishes, fails or is dropped. |
| `BotDoing.cs` | What an undertaking wants of the bot at this moment. |
| `BotLadder.cs` | Which rung the bot is on, from facts only. |
| `BotLedger.cs` | What this bot has learned about what pays. |
| `BotResolve.cs` | Everything one bot has resolved, feels and has learned. |
| `BotStanding.cs` | Which rung of the survival ladder a bot is standing on. |
| `BotTreadmill.cs` | The same deed against the same thing, failing the moment it is taken, is not offered to that bot again for a few seconds. |
| `BotUrges.cs` | The two things a bot feels, and why there are only two. |
| `BotWill.cs` | The decision. |
| `BotWillConfig.cs` | What Configuration/bot-will.json is allowed to say. |
| `BotWillModule.cs` | Deciding as a module: reads its numbers, turns itself on, and says what the population will be judging work by. |
| `BotYield.cs` | How a piece of work ended. |
| `IBotProposer.cs` | The supply side of the auction: something that knows about one kind of work and can offer a bot a piece of it. |
| `IBotWilful.cs` | What deciding needs a bot to be. |

### `BotMovement/` — path, road and step

Getting a bot from where it is to where the work is. The most expensive part of the assembly and the one with the longest history of measured defects — `RESEARCH.md` beside it is the analysis the current design came out of. A journey is a destination, a queue of things put aside to do first, and the plan currently being walked.

**Module** `BotMovementModule` · **Config** `bot-movement.json` · **Writes** `Getting about:`

- **Trap.** A `Point3D` whose Z came out of arithmetic is a place nothing can stand on: it works on flat ground and fails on a hill. Settle it against the map.
- **Trap.** Arriving is judged by asking the engine whether the work would be accepted here, never by the distance to a remembered point.

| file | decides |
|---|---|
| `BotArrival.cs` | What counts as having got there. |
| `BotAvoid.cs` | Ground a single plan is to keep out of. |
| `BotBarred.cs` | Ground the population is never to want, however attractive whatever is standing on it. |
| `BotChart.cs` | The coarse chart of the ground round home: squares of tiles, the ways across each square's edges, and which of those ways join inside the square — so that a wal |
| `BotDanger.cs` | The road goes round the danger (29.09.2026, Patrick's second answer of the morning). |
| `BotDangerRoute.cs` | The road the planner would draw, judged where the straight line refuses (06.10.2026, road15; Patrick's targets of that night: paths 99 %, work 95 %, Needs 95 %, |
| `BotErrand.cs` | One thing a bot is trying to get to. |
| `BotFooting.cs` | Tiles a walk has proved nobody can stand at, kept for everybody for half an hour. |
| `BotGates.cs` | The teleporters of the world read as gates between lands, so that a walk can be drawn through a cave mouth instead of stopping at it. |
| `BotJourney.cs` | What a bot is trying to get to, what it has put aside to do first, and the plan it is walking now. |
| `BotMovementConfig.cs` | What Configuration/bot-movement.json is allowed to say. |
| `BotMovementModule.cs` | Movement as a module: reads its numbers, lets the population walk, and puts its counters back on a world reload. |
| `BotPath.cs` | What a search concluded. |
| `BotReach.cs` | What the reach ledger can say about a journey before anybody searches for it. |
| `BotRefused.cs` | Places the population could not get to, remembered for everybody and forgotten again when somebody proves otherwise. |
| `BotRoads.cs` | How far each piece of ground around the population's home lies from it by road, rather than in a straight line. |
| `BotSight.cs` | The reach of a few creatures a walker has seen: a square round each, as many tiles as the creature notices a body at (the engine measures range in squares, GetMobilesInRange), plus a margin. |
| `BotStep.cs` | One tile, and everything the engine knows about stepping off it. |
| `BotWalk.cs` | Something standing in the way that can be asked to move. |

### `BotClasses/` — what a kind of bot is

Thirteen classes, one file each, plus a registry. A class is **a description and a set of limits with no behaviour** — it decides nothing and commands nobody. `BotWill` reads a class the way it reads the map: as a fact about the world. Four of the thirteen cast; three produce; the rest fight, shoot or heal.

**Module** `BotClassModule` · **Config** `bot-classes.json`

- **Trap.** Adding a class means a file here and a line in the registry. Nothing else in the assembly holds a list of permitted archetypes, and nothing should — the tool a bot carries is what decides which trades it is offered.

| file | decides |
|---|---|
| `BotArcher.cs` | The bow and nothing else, and the only class that can triple a hit. |
| `BotArchitect.cs` | The maker of both halves of the armour chain: the seam and the forge. |
| `BotArsenal.cs` | The weapons of the era, named once, each beside the skill that swings it. |
| `BotBaron.cs` | The one bot on the shard that is not trying to make a living. |
| `BotBrawler.cs` | Fights with its hands, and is therefore never holding anything it has to put down. |
| `BotCalling.cs` | Whether a piece of work is this bot's own trade, another class's trade, or anybody's work — what that does to its price, and how the population's working minutes actually divide between the three. |
| `BotCaptain.cs` | The one bot on the shard that exists for the others rather than for itself. |
| `BotClass.cs` | What a bot is: its build, what it is born owning, what it may carry, and the one thing it can do that nobody else can. |
| `BotClassConfig.cs` | What Configuration/bot-classes.json is allowed to say. |
| `BotClassModule.cs` | The class layer as a module: reads its file, then says what the nine came out as. |
| `BotClasses.cs` | Every class, built once, looked up by name. |
| `BotCrafter.cs` | Metal, cloth and leather. |
| `BotGatherer.cs` | Ore and timber, and the only bot that can find a reagent in the grass. |
| `BotHealer.cs` | The green staff. |
| `BotKit.cs` | What the world hands a bot at birth, and on what terms. |
| `BotLeaning.cs` | Which ladder of materials a leaning's band is read off, when the engine has one for its trade. |
| `BotMage.cs` | A spellbook, a blue staff, and no metal. |
| `BotPotionKind.cs` | Potions grouped by what they do, which is the granularity a carrying limit needs. |
| `BotRole.cs` | What a class contributes to a group, as opposed to what it is called. |
| `BotSage.cs` | The captain's opposite number, for the half of the population a captain cannot teach. |
| `BotTamer.cs` | A bow, and a beast beside it. |
| `BotTrader.cs` | The island's merchant: buys where a thing is to be had, carries it to whoever said aloud that it wants it, and keeps a book of what it has promised. |
| `BotWarrior.cs` | The plain fighter. |
| `BotWarriorArcher.cs` | Shoots, and has a knife for when that stops working. |
| `BotWarriorMage.cs` | Plate, a blade, and spells anyway. |
| `BotWeaponOption.cs` | One weapon a class may be born holding, together with the skill that makes it land, how far the bot will train that skill, and whatever the weapon needs in order to work at all. |

### `BotOutfit/` — what a bot owns

Turns a class's kit into items actually in a pack, decides what death has no right to take, and owns the parts of a bot that are property rather than behaviour: its bond with a weapon, its harness, its armour, and its horse.

**Config** `bot-population.json` · **Writes** `The ground:`

- **Trap.** `Mobile.EquipItem` does not replace an occupied layer, it refuses. Whatever wants a hand has to free it first, and has to put back what it took.

| file | decides |
|---|---|
| `BotBinding.cs` | What "bound" means, in one place. |
| `BotBond.cs` | What one bot was given, and therefore what death may not take from it. |
| `BotBrawlerGloves.cs` | The gloves a brawler fights in. |
| `BotCasterStaff.cs` | The staff a caster leans on. |
| `BotChampionSteed.cs` | A champion's steed: a mount of a kind no stable sells, won in the championship. |
| `BotHarness.cs` | What a bot ought to be wearing, worked out from what this shard can actually make and what it costs. |
| `BotOutfit.cs` | Turns a class's kit into things a bot is actually holding. |
| `BotStable.cs` | Buying a horse, and calling it up. |
| `BotSteed.cs` | A horse a bot carries in its pack and calls up when it has somewhere to be. |

### `BotHarvest/` — ore, wood, herbs, and the survey

Where raw material comes from, and — more used than the digging — the survey of the ground that the whole population navigates by. `BotGround` sweeps the tiles around wherever bots go and remembers four things: seams of ore, forges with an anvil beside them (`Fires`), every kind of fire the engine will cook over (`Hearths`), and counters. Everything that needs a *place* asks this.

**Module** `BotHarvestModule` · **Config** `bot-harvest.json` · **Writes** `The ground:`

| offers work | as | on the rung | handing out |
|---|---|---|---|
| `BotMiner` | Miner | Free | `BotDig`, `BotProspect` |
| `BotWoodsman` | Woodsman | Free | `BotChop` |

- **Trap.** The survey is built by walking and is not saved. A restart empties it — which silently cures "the island has run out of ore" and makes any measurement about exhaustion meaningless for the first two or three hours.
- **Trap.** An axe must be worn for the engine to accept a swing; a pick works from the pack. That difference is why mining worked from the first day and no log was ever cut.

| file | decides |
|---|---|
| `BotChop.cs` | Cutting wood: walk to the nearest tree and swing until the pack has enough or the tree has nothing left. |
| `BotDig.cs` | Dig, melt, put away. |
| `BotForage.cs` | Picking up the reagents the world leaves lying about, and putting them on the board. |
| `BotGround.cs` | One remembered patch of workable rock, and what is in it. |
| `BotHarvestConfig.cs` | What Configuration/bot-harvest.json is allowed to say. |
| `BotHarvestModule.cs` | Getting a living out of the ground, as a module: reads its numbers and offers the trade to the decision layer. |
| `BotHeard.cs` | What the harvest system last said to each bot, and which of its sentences mean what. |
| `BotHerbs.cs` | A walk into the woods that comes back with herbs. |
| `BotMiner.cs` | Offers a mining trip to anybody carrying a pick. |
| `BotOre.cs` | Ore: what is in a hill, whether this bot can get it out, how it is dug, and what it becomes. |
| `BotProspect.cs` | A walk out past the last swept ground, so that there is rock on the board somewhere nobody has been. |
| `BotTimber.cs` | What a woodcutter needs to know: the axe, the trees within reach, and how much wood is worth a trip. |
| `BotWoodsman.cs` | Offers a bot with an axe a trip to the woods, and only when somebody wants the wood. |

### `BotCraft/` — turning materials into goods

Five trades that make things: the smith at a forge, the tailor out of leather or cloth, the alchemist over a bottle, the fletcher out of shafts and feathers, and the cook at any fire. Each is a proposer that decides whether there is a piece of work, and a deed that walks to the place, swings until something comes of it, and puts the result either into the order it was made for or onto the market. `BotCraftwork` is the shared half — choosing a recipe, swinging, counting what appeared.

**Module** `BotCraftModule` · **Config** `bot-craft.json` · **Writes** `Needs:`

| offers work | as | on the rung | handing out |
|---|---|---|---|
| `BotAlchemist` | Alchemist | Free | `BotBrew` |
| `BotBowyer` | Bowyer | Free | — |
| `BotCook` | Cook | Free | `BotBake` |
| `BotCraftFirst` | CraftFirst | Free | `BotDraw` |
| `BotFletcher` | Fletcher | Free | `BotFletch` |
| `BotSmith` | Smith | Free | `BotForge` |
| `BotTailor` | Tailor | Free | `BotSew` |
| `BotTinkerer` | Tinkerer | Free | `BotTinker` |
| `BotTutor` | Tutor | Free | — |

- **Trap.** Crafting is asynchronous: `CraftItem.Craft` starts a timer. Count what the *last* swing produced at the top of the next tick, never after the swing you just made.
- **Trap.** The engine refuses in silence, because it answers a refusal by sending a message to a screen the bot does not have. Requirements live on the *recipe* (`SetNeedHeat`, `SetNeedOven`, `SetNeedMill`), not on the system's `CanCraft`.
- **Trap.** A failed attempt eats half the material. A round set up for exactly one item is a round that ends in `out of metal` after two misses — see `BotAnvil.Tries`.

| file | decides |
|---|---|
| `BotAdvance.cs` | A brewer's glass and herbs for a draught somebody has asked for, paid out of that want's own reservation and settled at the hand-over; what is not settled so the brewer pays back. |
| `BotAlchemist.cs` | Offers the mortar to anybody carrying one, and offers the board's orders first. |
| `BotAnvil.cs` | What the smith's trade knows that no other trade does: its system, its skill, its hammer, its metal, and the one thing about blacksmithing that is genuinely different — it cannot be done just anywhere. |
| `BotBake.cs` | One turn at the skillet: raw meat in the pack becomes suppers, and what the cook does not keep goes out to the market. |
| `BotBowyer.cs` | A bow out of seven logs, and a crossbow out of seven more: the last weapon on this shard nobody made. |
| `BotBrew.cs` | Brewing: buy the glass if it is short of it, work the mortar, and hand the bottles to whoever put the money down. |
| `BotCook.cs` | Offers a turn at the skillet to anybody carrying one and some meat. |
| `BotCraftConfig.cs` | What Configuration/bot-craft.json is allowed to say. |
| `BotCraftEar.cs` | What the craft system last told each bot, and how often it has said each thing. |
| `BotCraftFirst.cs` | A crafter takes the work asked of it before it goes mining: an order's piece, a delivery to a want, its own trade's material for one, or that material out of its own bank box. |
| `BotCraftModule.cs` | Making things, as a module. |
| `BotCraftwork.cs` | The part of making things that is the same whatever is being made. |
| `BotDraw.cs` | A crafter's walk to its own bank box for the material of an order it passed over, before it goes digging for more. |
| `BotFlask.cs` | Brewing: what a potion is made of, and the swing that makes it. |
| `BotFletch.cs` | Making arrows: buy the wood if it is short of it, cut the shafts, feather them, and hand them to whoever put the money down. |
| `BotFletcher.cs` | Offers a bot with a fletching tool and a handful of feathers a turn at making arrows, and offers it the board's orders first. |
| `BotFletching.cs` | What a fletcher needs to know: the craft system, the tool, and the two-step chain that turns a log and a feather into an arrow. |
| `BotForge.cs` | Beating iron into something, at a forge, and handing it to whoever asked for it. |
| `BotMeal.cs` | A bot eating a cooked meal: what it does to the bot, and for how long. |
| `BotOven.cs` | Cooking: what a meal is made of, and who can make one. |
| `BotSew.cs` | Buy cloth, make something of it, sell it hand to hand. |
| `BotSmith.cs` | Offers a bot with a hammer and some metal a turn at an anvil, and offers it the board's orders first. |
| `BotTailor.cs` | Offers the needle to anybody carrying a sewing kit. |
| `BotThread.cs` | Sewing: what can be made out of cloth, and the swing that makes it. |
| `BotTinker.cs` | Making a tool out of iron, wherever the bot is standing, and handing it to whoever asked for it or to the market. |
| `BotTinkerer.cs` | Offers a bot with tinker's tools and some iron a stint at making the tool the island is shortest of, and offers it the board's orders first. |
| `BotTinkering.cs` | What a tinker needs to know: the craft system, the tool, and which of the things it can make are the tools the rest of the population wears out. |
| `BotTutor.cs` | The first steps of a trade, bought from a shopkeeper who knows it — the engine's own teaching, paid for in gold. |
| `BotWeave.cs` | The spinning wheels and looms of the towns, read from the world once. |

### `BotHunt/` — the only new gold in the world

Everything else on this shard moves money about; this brings it in. Choosing what is worth fighting, prowling for it, killing it, carving it and going through what it left. Carving is folded into looting rather than made a choice, because the engine charges nothing for it and there is no decision in it worth a bot's turn.

**Module** `BotHuntModule` · **Config** `bot-hunt.json` · **Writes** `Companies:`

| offers work | as | on the rung | handing out |
|---|---|---|---|
| `BotHunter` | Hunter | Free | `BotProwl`, `BotSlay` |
| `BotMuster` | Muster | Free | `BotBand` |

- **Trap.** Everything lifted off a corpse is listed for sale except the bot's own ammunition and a cook's raw meat. Anything else a trade needs as *input* will be sold before that trade ever sees it.

| file | decides |
|---|---|
| `BotAmbush.cs` | The blow struck out of hiding: three swings' worth in one, and the victim stunned where it stands. |
| `BotBand.cs` | Calling a company together for something one bot cannot take, and seeing it through. |
| `BotBarrier.cs` | Where walks have stopped getting any nearer, and which way they were heading, so the hunt stops throwing darts past those places. |
| `BotBurgle.cs` | A bandit breaks into a guild's hall or a bot's own house: creeps up hidden, picks the lock of the chest with a lockpick, and carries off a share of what was put by — and is wanted if anybody sees it. |
| `BotCompanyRecord.cs` | What a kind costs a company, learned from companies' own fights against it, apart from what it costs the island as a whole (, which stays the lone bot's price). |
| `BotFence.cs` | The band's fence: the one member of The Shadow who commits no crime, and keeps its goods and its errands. |
| `BotFetch.cs` | The fence's run out to the chest for the band's goods. |
| `BotFreedom.cs` | Getting a prisoner out of a camp and home again. |
| `BotGlean.cs` | Picking spent ammunition up off the ground. |
| `BotHoleUp.cs` | The keeper going to ground at the hideout while a price stands on its head. |
| `BotHuntConfig.cs` | What Configuration/bot-hunt.json is allowed to say. |
| `BotHuntModule.cs` | Fighting for a living, as a module. |
| `BotHunter.cs` | Offers a fight to any bot healthy enough to want one. |
| `BotInquest.cs` | A murdered bot's body found: the finder searches for the killer with Detect Hidden, calls out, and tells the Baron. |
| `BotLair.cs` | The Shadow's camp at its hideout: a fire at the middle of it and a chest beside the fire that only the band opens. |
| `BotLawful.cs` | The law-abiding set on a murderer they can take between them. |
| `BotLieLow.cs` | A murderer getting away from the body and lying low: a dozen tiles off, hidden, not moving, for a few minutes. |
| `BotMuster.cs` | Offers a bot the chance to call a company against something it must otherwise walk past. |
| `BotOutlaw.cs` | Who has murdered lately, who is in a cell for it, and the clock that lets both go. |
| `BotOutlawStore.cs` | Keeps the murderers and the cells across restarts. |
| `BotPickings.cs` | Going through something this bot killed without meaning to. |
| `BotPlunder.cs` | Going through a chest, a crate or a barrel standing out in the world. |
| `BotProwl.cs` | Going to look for a fight, when there is nothing to fight where the bot is standing. |
| `BotQuarry.cs` | Finding something worth fighting, and finding what it left behind. |
| `BotQuarter.cs` | What a mark does when a robbery has beaten it down to a quarter of its health. |
| `BotRaid.cs` | The Baron's raid on The Shadow's hideout, once a caught thief has given it away: the Baron and a posse march there, search it with Detect Hidden, and set on eve |
| `BotRob.cs` | Setting on one of our own for what is in its pack: kill it, go through the corpse, and go red for it. |
| `BotShadow.cs` | Hiding, moving unseen and searching for the hidden, as a bot does them: the engine's own three skills, used without a client. |
| `BotSilence.cs` | The fence's chase after a witness it could not buy: kill it before it gets away, or it tells. |
| `BotSkulk.cs` | Practising being unseen: hiding again and again where it stands, and once it hides well enough for the engine to allow it, moving a few quiet steps at a time. |
| `BotSlay.cs` | Close, fight, go through what is left. |
| `BotStash.cs` | Carrying the takings to the band's chest at the hideout. |
| `BotTame.cs` | The beasts a bot has tamed: kept beside it, set on whatever it fights, and fed. |
| `BotUnderworld.cs` | The island's criminal record, and The Shadow: the sixth guild, of thieves and brigands, founded by murderers. |
| `BotUnderworldStore.cs` | Keeps the island's criminal record and The Shadow across restarts: each bot's murders, robberies and times caught by name, and the guild's members and hideout. |
| `BotWaylay.cs` | A thief that has thought of robbery and found nobody to rob, going to where the work is and waiting there. |

### `BotShops/` — buying and selling over a counter

A capability for every bot rather than a trade of its own: reagents for a caster, bandages for anybody, metal for a smith, and whatever the population would not buy goes back over the same counter for coin. Also the board of standing orders — what a bot has put money down for and has not received yet.

**Module** `BotShopsModule` · **Config** `bot-shops.json` · **Writes** `Needs:`

| offers work | as | on the rung | handing out |
|---|---|---|---|
| `BotBullion` | Bullion | Free | — |
| `BotPeddler` | Peddler | Free | `BotPeddle` |
| `BotShopper` | Shopper | Free | `BotRestock` |
| `BotUpkeep` | Upkeep | Free | — |

- **Trap.** The engine pays out of the pack, and a bot only walks to a bank above a threshold. Three files once closed on each other so that money existed and could not be spent.

| file | decides |
|---|---|
| `BotAskFirst.cs` | A restock of a supply some bot near holds over its own keep, asked of the island at the counter's price before the walk. |
| `BotBullion.cs` | A crafter with money buys its metal instead of going and digging it. |
| `BotCapital.cs` | Britain as the centre of trade: the capital's shopkeepers pay better for goods, a load drawn out of a bank box walks to them when the better price pays for the |
| `BotNeeds.cs` | How often a bot reconsiders what it is short of. |
| `BotOrder.cs` | Saying aloud what the bot wants and what it will pay: a want on the market, spoken in the trade chat. |
| `BotPeddle.cs` | Taking what the population would not buy to somebody who will. |
| `BotPeddler.cs` | Offers a trip to a counter with goods the population is not asking for: out of the bot's own bank box while it stands at a bank, or out of a crowded pack. |
| `BotProvision.cs` | What a bot must carry before it goes into a fight it chose — a dungeon's delve, or a war company sent against somebody else's ground — and the guild's money for what it cannot buy itself. |
| `BotRestock.cs` | Going to a shop and buying what the bot has run out of. |
| `BotShopper.cs` | Offers a trip to the shops to any bot that has run out of something its class needs. |
| `BotShops.cs` | Buying things from the shopkeepers. |
| `BotShopsConfig.cs` | What Configuration/bot-shops.json is allowed to say. |
| `BotShopsModule.cs` | Trading with the shopkeepers, as a module. |
| `BotStores.cs` | A crafter short of the raw material of its trade, putting the order to the population. |
| `BotUpkeep.cs` | Asking the population, by name, for a replacement for something that is wearing out. |

### `BotMarket/` — the bots' market, hand to hand

Trade between bots with the auction taken out (Patrick's order of 30.09.2026): Britain's square by the bank, the wants bots say aloud in the trade chat (no gold held), the carrier who walks what somebody wants over to them and the meeting at which goods go pack to pack and coin pack to pack, the prices learned from goods that really changed hands, and the city's treasury, which buys at the bots' shops. Nothing changes hands between two bots that are not standing within two tiles of each other.

**Module** `BotMarketModule` · **Config** `bot-market.json` · **Writes** `The market:` · **Writes** `The board:`

| offers work | as | on the rung | handing out |
|---|---|---|---|
| `BotCourier` | Courier | Free | `BotDeliver` |

- **Trap.** A want and the goods can both exist and never meet: the carrier offers a walk only within its reach and to a buyer on its land who could pay for one. When trade is low, read the carriers' clause of `The market:` before the wants.
- **Trap.** A want holds no gold, so the buyer pays at the meeting in the middle of its own work; the meeting books that coin aside (`BotYield.Aside`) or the buyer's hunt would learn that hunting loses money.

| file | decides |
|---|---|
| `BotCity.cs` | The city's treasury: the one purse on the shard that mints coin, and everything the city does is paid from it. |
| `BotCityStore.cs` | Keeps the city's treasury, its standing orders and its bounties across restarts. |
| `BotCourier.cs` | Offers a bot carrying what somebody has said it wants the walk to that somebody, and the sale at the other end. |
| `BotDeliver.cs` | Carrying what somebody said it wants to where it is, and handing it over: the maker's walk to the one who ordered, the hunter's walk to the cook, the miner's walk to the smith. |
| `BotGlut.cs` | Supply against spoken demand, by kind: whether one more of a thing is work or a pile. |
| `BotHandover.cs` | Goods from one bot's pack into another's, and coin the other way, with the two standing together. |
| `BotLine.cs` | Why a bot able to answer a want passed it over. |
| `BotLines.cs` | The Needs board counted by Patrick's measure: every line a bot asks for, whether it was met within its first hour, by whom, and, when it was not, why (02.10.202 |
| `BotMarket.cs` | The bots' market with the auction taken out of it: Britain's square by the bank, the wants the bots say aloud, the prices the island has learned from goods that |
| `BotMarketConfig.cs` | What Configuration/bot-market.json may say: speeds and distances, never prices. |
| `BotMarketModule.cs` | The market as a module: reads its numbers, starts the wants' beat and the city's clock, and offers the carrier's walk. |
| `BotMarketStore.cs` | Keeps the Needs board — the spoken wants and their lines — across restarts (02.10.2026, build 391; Patrick: "the board may be saved across restarts"). |
| `BotRendezvous.cs` | A delivery is a meeting: once the carrier is near, the buyer comes to it — or both go to safe ground — instead of the carrier walking after a buyer that is at work and moving. |
| `BotRoom.cs` | A buyer whose pack refused a hand-over: marked "no room", passed over by every carrier while it stays so, and pressed to make room (06.10.2026, deliver16). |
| `BotUnreached.cs` | Buyers down the dungeon block, and the ground they stood on, that a carrier's walk or a trader's haul from outside found no way to — remembered for every carrie |
| `BotWant.cs` | One bot's spoken want: what it wants, how many, and what it says it will pay for one. |
| `BotWayless.cs` | Places a carrier's walk found no way to, shared by every carrier: a cell of tiles that answered "no way" times is struck for , so no third carrier walks there a |
| `IBotWantCarrier.cs` | Work that has reserved units on a spoken want and is bringing them: the carrier's walk () and the trader's haul (BotHaul). |

### `BotShopkeep/` — bots' shops by the bank

Trade between bots in a place, and since 01.10.2026 the market itself. A maker or gatherer holding what the island is short of or somebody asks for stands by Britain's bank (a guild's member by its guild's own town's) for a shift and calls out what it sells in the trade chat; a bot short of a supply, or with a want said, weighs the shop against the shopkeeper on price and the walk, walks over, and buys out of the keeper's bank box and pack within two tiles of it. The goods never leave the world. The city's buys, fairs and standing orders are served here too.

**Module** `BotShopkeepModule` · **Config** `bot-botshops.json` · **Writes** `Bot shops:`

| offers work | as | on the rung | handing out |
|---|---|---|---|
| `BotPatron` | Patron | Free | `BotRestock` |
| `BotShopkeeper` | Shopkeeper | Free | `BotKeepShop` |

- **Trap.** Fighters keep no shop (`BotShopkeeper.Merchant`): 88 of 103 shift-hours on 30.09.2026 were theirs and 53 % of shifts sold nothing.
- **Trap.** A shop's stock is a count, not the goods: it is taken every few seconds and at every sale, and the till counts again before anything moves. A customer can arrive to find the thing gone; a supplies errand then goes on to a shopkeeper.

| file | decides |
|---|---|
| `BotKeepShop.cs` | A shift keeping shop: walk to a pitch beside the bank, open, call out what is for sale, serve whoever comes, and shut when the goods or the shift run out. |
| `BotPatron.cs` | Offers a bot with a spoken want a walk to a bot's shop that sells the thing for no more than it says it will pay. |
| `BotShopBook.cs` | The shops' book: what each town's bots bought from bots' shops and from shopkeepers, by kind, at what price, and what customers came for and found gone. |
| `BotShopkeep.cs` | Bots' own shops: a bot with goods to spare stands beside the bank of the town it lives in, calls out what it sells, and the bots that are short of those goods walk over and buy them out of its pack. |
| `BotShopkeepConfig.cs` | What Configuration/bot-botshops.json is allowed to say. |
| `BotShopkeepModule.cs` | Bots' own shops as a module: reads its numbers, offers the shift and the customer's walk, and says every five minutes what the shops did. |
| `BotShopkeeper.cs` | Offers a bot with goods the island is asking for a shift keeping shop by Britain's bank — the market — or, for a guild's member, by its guild's own town's bank. |
| `BotStorefront.cs` | One bot's shop while it is open: who keeps it, where it stands, what it holds and at what price, and what it has taken. |

### `BotSpells/` — a book that grows

Scrolls, reagents and spellbooks: the first work in the project whose output no shopkeeper sells, and the first buyer on the bots' market that is itself a bot. A caster wants spells it does not have, a scribe makes them, and the armoury decides what a book is short of.

**Module** `BotSpellsModule` · **Config** `bot-spells.json` · **Writes** `Arms:`

| offers work | as | on the rung | handing out |
|---|---|---|---|
| `BotArmoury` | Armoury | Free | — |
| `BotScribe` | Scribe | Free | `BotInscribe` |
| `BotSeeker` | Seeker | Free | — |

| file | decides |
|---|---|
| `BotAcquire.cs` | Getting hold of one spell the book is short of: off a shelf, at a bot's shop by the bank, or by asking the population for it aloud and waiting for a scribe to bring one. |
| `BotArmoury.cs` | Offers any bot with a mana pool the chance to lay in a few attack scrolls. |
| `BotGrimoire.cs` | What a book holds, what it is short of, and how a scroll becomes a spell in it. |
| `BotInscribe.cs` | Buy paper, write scrolls, keep what the book is short of and sell the rest. |
| `BotQuill.cs` | Writing scrolls: what can be written, what it takes, and the attempt itself. |
| `BotScribe.cs` | Offers the pen to anybody carrying one. |
| `BotScribes.cs` | Who on the island can write which circle, what a scroll no shelf sells may be asked for, which scribe is writing which want, and the scribes' counts for the Needs: line. |
| `BotSeeker.cs` | Offers a caster the next spell its book is short of, by whichever route exists for it. |
| `BotSpellsConfig.cs` | What Configuration/bot-spells.json is allowed to say. |
| `BotSpellsModule.cs` | Magic as a trade and as an appetite, as a module. |
| `BotStrike.cs` | Casting at something, as opposed to casting at somebody who is hurt. |

### `BotCombat/` — is it worth stopping

The fight itself belongs to the engine — `Warmode` and `Combatant` are enough, and its own skill checks train skills exactly as they do for a player. What is here is the judgement around it: is this thing worth fighting, is this fight being lost, is somebody calling for help, and how a shooter keeps its distance.

**Writes** `Arms:` · **Writes** `Bows:`

| offers work | as | on the rung | handing out |
|---|---|---|---|
| `BotFugitive` | BotFugitive | ? | `BotBolt` |
| `BotRescuer` | Rescuer | Free | `BotBrawl`, `BotRescue`, `BotSlay` |

- **Trap.** Three separate reasons a bot fails to land a blow on something it is standing next to — no line to it, a shooter that moved too recently, a broken cast — and none of the three says anything in the log unless a counter is put there.

| file | decides |
|---|---|
| `BotArms.cs` | Whether a bot has anything in its hands, asked at the moment it matters. |
| `BotBolt.cs` | Getting away from whatever is killing it. |
| `BotBrawl.cs` | A fight with one of our own — a duel, a robbery, a manhunt — as a piece of work: close, hit, and stop when told. |
| `BotCry.cs` | Somebody of ours is being killed and has said so out loud. |
| `BotDuel.cs` | Two of ours set against each other on purpose, and the rule that says when it is over. |
| `BotFugitive.cs` | Offers a bot whose health is going the one thing that was missing from that rung: leaving. |
| `BotHaven.cs` | The nearest guarded town a beaten bot can realistically run to, and whether the guards there can do anything about what follows it. |
| `BotPeril.cs` | Where the shard is dangerous, learned from the only two facts that actually say so: where bots are being hit, and where they are dying. |
| `BotReflex.cs` | How long a bot takes to answer what happens to it and to its own: the stopwatch between an event and the first decision that answers it (06.10.2026, react14; Pa |
| `BotRescue.cs` | Going to somebody's aid, or hitting back at whatever is hitting you. |
| `BotRescuer.cs` | Offers a free bot the chance to go to somebody's aid. |
| `BotThreat.cs` | One of this population, counted when adding up our side of a fight. |

### `BotMend/` — bandages

The smallest subsystem here, and the only thing that puts a bot on the `Failing` rung. A bot binds its own wounds, and a surgeon binds somebody else's.

**Module** `BotMendModule` · **Config** `bot-mend.json` · **Writes** `Arms:`

| offers work | as | on the rung | handing out |
|---|---|---|---|
| `BotMedic` | Medic | Failing | `BotSalve` |
| `BotSurgeon` | Surgeon | Free | `BotSalve` |

| file | decides |
|---|---|
| `BotAccompany.cs` | A healer standing by one of ours who is fighting, close enough to bind a wound the moment there is one. |
| `BotAwaitMend.cs` | A hurt bot a healer is walking to stops and waits for it (29.09.2026, build 321; Patrick's order of the morning: "full mutual aid"). |
| `BotHouseCall.cs` | A healer with nobody to mend going to where our fighters are. |
| `BotMedic.cs` | Offers a bot the chance to look after itself, and it is the only thing on the rung that says so. |
| `BotMend.cs` | Mending: what a bot can heal with, who needs it, and the two ways of doing it. |
| `BotMendConfig.cs` | What Configuration/bot-mend.json is allowed to say. |
| `BotMendModule.cs` | Looking after each other, as a module. |
| `BotRetainer.cs` | A fighter with money hiring a healer to stand by it, and the wage that changes hands. |
| `BotSalve.cs` | Patching somebody up — itself or somebody else, by spell if it can and by cloth if it cannot. |
| `BotSurgeon.cs` | Offers whoever can mend the worst-hurt bot within sight — itself included — as ordinary work. |

### `BotSquad/` — standing companies

A leader, a few followers, a formation, and dividing what the fight left. Companies are formed from three places — a hunt that found something too big for one, a patrol, and the Baron's harrowing — and dissolve when there is nothing left to fight. This folder also *assembles* five of the summary lines out of counters that mostly live elsewhere.

**Module** `BotSquadModule` · **Config** `bot-squad.json` · **Writes** `Companies:` · **Writes** `Arms:` · **Writes** `Bows:` · **Writes** `Needs:` · **Writes** `The ground:`

| offers work | as | on the rung | handing out |
|---|---|---|---|
| `BotClearer` | Clearer | Free | `BotHarrow` |
| `BotPatrol` | Patrol | Free | `BotSweep` |

- **Trap.** A bot on the `Bound` rung takes no work of its own: the auction is switched off for it. A company with no charge is therefore a company of idle bots.
- **Trap.** The subsystem's own README still says nothing calls `BotSquads.Form`. It has three callers and companies form on every session.

| file | decides |
|---|---|
| `BotClearer.cs` | Offers any fighter the square where bots lately died, and a company to clear it. |
| `BotEnlist.cs` | Falling in with a company that is already fighting, rather than starting a fight of your own beside it. |
| `BotFormation.cs` | Where each member of a squad ought to be standing, worked out rather than assigned. |
| `BotPatrol.cs` | Offers a captain the worst square on the island and a company to take there. |
| `BotRaise.cs` | A company raises its own where they fell, with what this era gives it to raise them with (02.10.2026, Patrick: "It is not right that bots dying in dungeons cannot loot themselves. |
| `BotScatter.cs` | How a squad stands on ground that has nothing left on it: broken into small knots, spread out, covering the place instead of standing in it. |
| `BotSpoils.cs` | Dividing what a squad took. |
| `BotSquad.cs` | What a squad is doing. |
| `BotSquadConfig.cs` | What Configuration/bot-squad.json may say. |
| `BotSquadField.cs` | The company fights on the strength that is on the field, not on the map (06.10.2026, company16). |
| `BotSquadMember.cs` | What a squad needs a bot to be. |
| `BotSquadModule.cs` | Squads as a module: reads its numbers, starts the squads' own beat, and clears the board on a world reload. |
| `BotSquadRaise.cs` | The company's half of raising its fallen (02.10.2026). |
| `BotSquadSpare.cs` | A company whose fight is well in hand spares a member or two to a dire cry near it (06.10.2026, react18). |
| `BotSquads.cs` | Every squad on the shard, and the four things that can happen to one: it forms, somebody joins, somebody leaves, it dies. |
| `BotSweep.cs` | A company called together for a place rather than for a creature, and kept together until the place stops killing people. |

### `BotDrill/` — the captain

One class exists for the others rather than for itself. A captain holds a field, teaches whoever pays the fee, marches a company at ground the danger map says is bad, and puts armour orders on the board for bodies that have none.

**Module** `BotDrillModule` · **Config** `bot-drill.json` · **Writes** `The captain:`

| offers work | as | on the rung | handing out |
|---|---|---|---|
| `BotArmourer` | Armourer | Free | — |
| `BotDrill` | Drill | Free | `BotAttend`, `BotLesson` |

- **Trap.** A lesson is worth about fifteen times what a rescue is in skill gained, so any rule that keeps captains from teaching is a rule against the population levelling at all.

| file | decides |
|---|---|
| `BotArmourer.cs` | Puts an order on the board for the best piece of armour this bot is not wearing. |
| `BotAttend.cs` | The student's half of a class: pay, walk to the field, find your place in the block, and stay in it. |
| `BotDrill.cs` | Offers a captain an afternoon of teaching, when there is anybody on the island worth teaching. |
| `BotDrillModule.cs` | What Configuration/bot-drill.json may say. |
| `BotLesson.cs` | The captain's half of a class: take the field, wait for whoever comes, then pace the ranks for an hour saying things. |
| `BotSchool.cs` | The training field: where it is, who is standing on it, where each of them stands, and what an hour of being shouted at is actually worth. |

### `BotBaron/` — the ground that has already killed people

Every other class answers *how does this bot get by*. The Baron answers the one question nobody else on the island asks, because there is no profit in it: who goes back to the places that have proved they kill. He raises a levy for it, walks his rounds, tours the towns, and pays a stipend out of his own account.

**Module** `BotBaronModule` · **Config** `bot-baron.json` · **Writes** `The Baron:`

| offers work | as | on the rung | handing out |
|---|---|---|---|
| `BotHarrower` | Baron | Free | `BotHarrow`, `BotRaid` |
| `BotStroll` | Stroll | Free | `BotRounds` |

| file | decides |
|---|---|
| `BotBaronModule.cs` | What Configuration/bot-baron.json may say. |
| `BotHarrow.cs` | Six bots taken to ground that has killed people, and kept there until it has been emptied or the afternoon is gone. |
| `BotHarrower.cs` | Offers the Baron the ground that has killed the most people, and five bots to take there. |
| `BotRegalia.cs` | What the Baron wears, and the one promise all seven pieces make: they do not wear out. |
| `BotRounds.cs` | The Baron walking his town, because nowhere has taken anybody lately. |
| `BotStipend.cs` | The one purse on this shard that is not earned, and the argument for allowing exactly one. |
| `BotStroll.cs` | Offers the Baron his own town to walk through. |

### `BotDelve/` — the dungeons

Five bots taken underground by a member of their guild, for twenty minutes or twenty corpses. Since 29.09.2026 the party marches from the muster to the cave mouth and walks through it — the world's teleporters are gates the journey routes through (`BotGates`) — and walks out by it at the end; the descent by hand is kept only for a dungeon no gate joins. Which dungeon is decided by measuring what lives in each against what the band is worth. What they take is swept into one pot as it fills and divided at the end, half to the leader. A fighter alone may venture into a dungeon whose worst it outmatches (`BotVenture`); inside, the hunt, the band and the walk home do the rest.

**Module** `BotDelveModule` · **Config** `bot-delve.json` · **Writes** `Delving:`

| offers work | as | on the rung | handing out |
|---|---|---|---|
| `BotDelver` | Delver | Free | `BotDelve` |

- **Trap.** A refused road underground is ordinary rather than fatal — a wall between a bot and its place in the line is what a cavern is made of — so `BotDelve.Bend` shrugs it off and moves the party.
- **Trap.** Whoever is still below ten minutes after a delve settled is lifted by `BotDelveParty.Watch`, and the count is printed against those who walked out: a backstop doing the ordinary work has become the design.

| file | decides |
|---|---|
| `BotDelve.cs` | A party taken down a dungeon by a member of its guild — two for every creature of the dungeon's worst room, five at the fewest — and kept there for twenty minutes or twenty corpses. |
| `BotDelveModule.cs` | What Configuration/bot-delve.json may say. |
| `BotDelveParty.cs` | Five bots underground: what they have taken, whose it is, how many of them may still be put back on their feet, and the promise that every one of them comes home. |
| `BotDelver.cs` | Offers the maker of a guild a dungeon its band could actually come back out of. |
| `BotDungeon.cs` | The dungeons of this era: where each one is, what rooms it has, and how hard it is. |
| `BotHalls.cs` | Which rooms of a dungeon can actually be walked between, found by walking between them. |
| `BotVenture.cs` | One fighter's walk into a dungeon by its mouth, alone. |

### `BotTravel/` — the towns and the roads

The towns read from the map's regions, which of them can be walked to from home (the gates' answer), one road per pair drawn by the long tier and walked into the book, and the journey a bot with nothing pressing takes to a town it has not seen — telling its guild what the road read on the danger map. Guilds founded on the shard may be seated by another town (`BotSeat.Choose`). Patrick's order of 29.09.2026.

**Module** `BotTravelModule` · **Config** `bot-travel.json`

- **Trap.** The roam round home is now the roam round every reachable town, so a proposer that looks further than it walks sees further than it should.
- **Trap.** A town reached only through a gate has no road in the book and is not offered to travellers.

| file | decides |
|---|---|
| `BotRoadbook.cs` | The roads between the towns: how long each is, how dangerous the ground along it reads, and how often the population has walked it. |
| `BotTowns.cs` | The towns of the map, read from its regions, and which of them the population can walk to. |
| `BotTravel.cs` | A walk to another town, for its own sake. |
| `BotTravelModule.cs` | The numbers of Configuration/bot-travel.json. |

### `BotResidence/` — where each bot lives

Every bot has a town of its own: born in it, risen in it, walked home to it, hunting and gathering round it. `BotPopulation.HomeOf` is the one answer to every per-bot home and asks `BotResidence.Home`: in the population's home town that is the guild's hall or seat or the population's home, as before; elsewhere the town's bank, or the guild's hall or seat when it stands there. A member of a guild seated abroad lives by its guild; everybody else chooses among the towns fit to live in (a road in the book, a bank, shops), weighed towards the emptier and away from roads too strong for it, and may move — by walking there — after a poor living or deaths where it lives. Patrick's order of 29.09.2026, point 5.

**Module** `BotResidenceModule` · **Config** `bot-residence.json` · **Writes** `Residences:`

| offers work | as | on the rung | handing out |
|---|---|---|---|
| `BotMover` | Mover | Free | `BotResettle` |

- **Trap.** A move changes the residence when it is taken, not when it is offered and not when it arrives: a bot that dies on the road rises in its new town.
- **Trap.** Population-wide questions — the roads' flood, the gates, the city's errands, the Baron — still ask `BotPopulation.Where`; only a per-bot home asks `HomeOf`.

| file | decides |
|---|---|
| `BotMover.cs` | Offers a bot that has been marked to move house the walk to its new town, when it is free to go. |
| `BotRelocate.cs` | Who should move house, and why: first sight, a guild seated abroad, a poor living, or dying where it lives. |
| `BotResettle.cs` | Moving house: the walk to the town a bot is going to live in, and the moment it lives there. |
| `BotResidence.cs` | Where each bot lives: the town it was born in or chose, and what "home" means to it there. |
| `BotResidenceModule.cs` | The numbers of Configuration/bot-residence.json. |
| `BotResidenceStore.cs` | Keeps where every bot lives across restarts: by name, the town, when it settled there, how often it has moved and the town it last left. |
| `BotSettle.cs` | Which towns a bot can live in, and which one it settles in. |

### `BotGuildHouse/` — guild houses in the towns

A guild settles in an empty town building: the towns' regions are read for the engine's own door frames (`DoorGenerator`), both sides of every doorway flooded with the planner's step model, and the enclosed, roofed rooms of 64 to 400 tiles with at most two doorless ways in kept (cached in `Data/bot-guild-houses.bin` by the map files' fingerprint). What the world has put in one since — a spawner, a shopkeeper's home, a door, an inn's region, a house — is asked live. A guild wishing abroad takes a free building in a fit town with room (two a town), for nothing; a guild wishing for Britain raises its hall first, then pays 10,000gp to the city. A door keyed to the guild goes into every doorway and a sign naming it on the wall; the building's heart is the guild's seat (`BotSeat.Of`), where members are born and rise (`BotSeat.Home`), and its town is where they live (`BotRelocate.Forced`). The hall and the claims are still earned the old way. Patrick's idea of 29.09.2026, evening.

**Module** `BotGuildHouseModule` · **Config** `bot-guildhouse.json` · **Writes** `Houses:`

- **Trap.** The door is unlocked in the engine's terms and refuses at the handle (`BotGuildDoor.Use`), because a locked shut door is a wall to the planner for members too: a door a member opened lets anybody through for twenty seconds.
- **Trap.** A guild's house outranks every other seat, but the hall is still carried towards the seat the hall itself last set (`BotSeat.Moving`), or an exile's move would be undone by the next carry.

| file | decides |
|---|---|
| `BotGuildDoor.cs` | The door a guild puts into the doorway of the town building it settles in: the engine's own dark wooden door, which opens for the guild's members and for nobody else. |
| `BotGuildHouseModule.cs` | The numbers of Configuration/bot-guildhouse.json. |
| `BotGuildHouseStore.cs` | Keeps which guild holds which town building, and each guild's wish, across restarts. |
| `BotGuildHouses.cs` | The guilds' houses in the towns: which guild lives in which empty building, the door it put into the doorway and the sign it hung beside it, and the one price — Britain's. |
| `BotGuildSign.cs` | The sign a guild hangs on the wall beside the door of its town house, reading the guild's name and tag. |
| `BotHouseSurvey.cs` | The empty buildings of the towns: walls round a floor, a roof over it, a doorway with no door in it — read once from the map's own statics, the way the engine's |

### `BotCamp/` — fires in the woods and a word on the road

Some bots light a campfire in the wild when tired, bored or idle — the engine's own kindling, struck and fed by the engine's roll — and free bots within forty tiles are offered a seat at it, where they face the flame and talk: roads walked, the worst ground near, trade, their guild. Sitting at a lit fire is rest at the inn's rate, and a tired bot secure at one goes offline there, unpaid, for a sixth of its rest as regeneration. Two free bots passing in the open may stop for a word. Patrick's point 6 of 29.09.2026.

**Module** `BotCampModule` · **Config** `bot-camp.json`

- **Trap.** The engine's campfire burns a hundred seconds; a fire nobody feeds is out, and `BotCamp.Warm` carries it only thirty seconds between flames.
- **Trap.** Every deed here is unpaid, so its proposer asks every reason it fails before it is offered.

| file | decides |
|---|---|
| `BotCamp.cs` | Fires in the woods: who has lit one, who sits round it, and what an hour beside one is worth to a tired bot. |
| `BotCampModule.cs` | The numbers of Configuration/bot-camp.json. |
| `BotCampTalk.cs` | What is said round a fire and on the road: a greeting and a welcome, then news of the roads, the worst ground near, trade and the speaker's guild, and small talk, with an answer now and then. |
| `BotChat.cs` | Two bots that met in the open, stopped, and are having a word: facing each other until the talk is over, then back to whatever each was doing. |
| `BotFireside.cs` | Sitting at somebody else's fire: a walk over, a seat on the ring round the hearth, facing the flame, a greeting, and a few minutes of listening and talking — or, for a tired bot, a night's sleep there. |
| `BotKindle.cs` | Making camp: kindling in hand, a hearth picked on wild ground, the engine's flame struck on it, and a seat by it kept until the evening is over. |

### `BotRunes/` — Mark, Recall and the company's gate

A mage of Magery 40 and up marks a rune where its guild holds none — its seat, a cave mouth, the town it stands in — and keeps it in its own book; the guild's knowledge of the place is kept in a store and written into the guild's library, a runebook in the hall's chest or with its keeper, which lends copies at a blank rune each. A far walk (a journey, the walk home, a far counter) asks for a recall first; a delve or a sweep asks for a Gate Travel, and the company steps through, the leader last. Every cast is the engine's spell. Patrick's order of 29.09.2026, point 7.

**Module** `BotRunesModule` · **Config** `bot-runes.json`

- **Trap.** Nobody marks inside a Felucca dungeon (`SpellHelper` travel rules): a mouth's rune stands on the island side, and the company walks the last tiles through the mouth.
- **Trap.** A bot has no screen for the gate's confirmation gump: members are put through by the engine's own `Moongate.EndConfirmation`, not by stepping on the gate.

| file | decides |
|---|---|
| `BotMark.cs` | A rune-mage marking a rune for a place its guild holds none for: walk to a tile the engine will let a rune be marked on, cast Mark, aim it at a blank, and keep the rune. |
| `BotPassage.cs` | A company put through a Gate Travel: a mage of the company opens the gate where the company stands, the members step through one by one, the leader last, and the march goes on from the far side. |
| `BotRecall.cs` | Recall instead of a long walk: the shortcut a far walk asks for before it walks. |
| `BotRuneBuy.cs` | A rune-mage buying blank runes where the engine sells them — a mage's shop, at fifteen gold — with its guild's money when its own will not stretch to it. |
| `BotRuneLibrary.cs` | The guild's rune library: a runebook in the hall's chest, or in the pack of the guild's keeper where there is no hall, written from what the guild knows (), and |
| `BotRuneShelf.cs` | One place a guild holds a rune for: what it is called, what kind of place, and where the rune puts a body down. |
| `BotRunes.cs` | Runes: a mage marks where it has stood, recalls there alone, and opens a gate there for its company. |
| `BotRunesModule.cs` | What Configuration/bot-runes.json is allowed to say. |

### `BotSea/` — ships between the islands

The water of the map charted once from the client files (cached in `Data/bot-sea-chart.bin`), a dock for each town a ship can berth at, and lanes between every pair of docks no road or gate joins. A traveller bound for a town it cannot walk to buys a small ship (the guild may help pay), boards it by the engine's own plank and key, and the helm moves it with `BaseBoat.Move` and `Turn` at the engine's speeds; ashore, the ship is dry-docked into the pack for the next voyage. Patrick's order of 29.09.2026, point 7.

**Module** `BotSeaModule` · **Config** `bot-sea.json` · **Writes** `Sea:`

- **Trap.** A bot's speech carries no keywords, so the tillerman's commands are not used: the helm drives the ship itself on its own clock.
- **Trap.** A voyage with no headway for three minutes is wrecked: captain and crew are carried to the nearest dock and the ship is dry-docked — the only hand-move, counted and logged.

| file | decides |
|---|---|
| `BotDocks.cs` | One place a ship is put in the water at a dock: the shore a bot boards from, the berth, and the harbour from it to the open sea. |
| `BotHelm.cs` | One ship at sea with a bot at its helm: where it is bound, how far along, and how it is going. |
| `BotSea.cs` | The sea as the rest of the population sees it: which towns a traveller may sail to, the dock it sets out from, whether it can have a ship, and the one summary line. |
| `BotSeaChart.cs` | What keeps a ship off a berth. |
| `BotSeaCourse.cs` | A course over the open cells of the chart: A* from one cell to another, sliced across ticks, one search at a time. |
| `BotSeaLanes.cs` | A way by sea between two docks: its course, how long it is, and how often it has been sailed. |
| `BotSeaModule.cs` | The numbers of Configuration/bot-sea.json. |
| `BotVoyage.cs` | A voyage: a ship bought if the bot has none, a walk to the dock, the ship put in the water and boarded, the sea, and the walk from the far dock into the town — or home. |

### `BotProving/` — Argus's proving ground

A bot's double against a creature in Green Acres, and a guild's company of doubles against a dungeon's worst room: the engine fights, the doubles answer with the bots' own hands, and what comes of it is the strength the delve judges a band by instead of health times weapon damage. Patrick's order of 26.09.2026.

**Module** `BotProvingModule` · **Config** `bot-proving.json` · **Writes** `Proving:`

- **Trap.** A walkover is a lower bound, not a measurement: R is capped at 5 and nothing measured on a lower rung is carried up the ladder at more than the creature it beat, times 0.75. The first boot extrapolated an orcish mage beaten at R 20 into a Balron beaten at R 1.4.
- **Trap.** A band is as far as its weakest member goes, and a room is not its worst creature one at a time: the delve counts the leader, wants every member at a fifth of the worst, and past the easiest dungeon wants the guild's company to have cleared the worst room lately.

| file | decides |
|---|---|
| `BotDriver.cs` | One double's hands in a fight: the bot's own reflexes, beat by beat, against whatever it is set on. |
| `BotProving.cs` | Argus's proving ground in Green Acres: who is fought against what, what each fight measured, and the strength a bot is judged by once it has been measured. |
| `BotProvingModule.cs` | What Configuration/bot-proving.json may say. |
| `BotProvingStore.cs` | Keeps what the proving ground has measured across restarts. |
| `BotRoomTrial.cs` | A guild's company of doubles against a dungeon's worst room, on the proving ground: whether the band clears it, what it cost, and who fell. |
| `BotStandIn.cs` | A bot's double for one fight on the proving ground: the same body, build, skills and kit, and nothing that makes it one of the population. |
| `BotTrial.cs` | What a bot is built of, as far as a fight is concerned: enough to say whether a measurement made last hour still describes it. |
| `BotWake.cs` | Wakes the ground round the bots the way the engine wakes it round a player. |

### `BotEstate/` — the guilds' halls

The one thing this population builds that outlives it. A guild saves in its chest — the members' dues, a fifth of what their work brings in (`BotDues`), and the tithes of its ground — levies the rest off its members, finds ground the engine will take a house on, raises a hall, and fits it out with the tools of its own trade. Next start the halls are read back out of the world by the name on their signs and handed to whoever leads that guild now.

**Module** `BotEstateModule` · **Config** `bot-townground.json` · **Writes** `Estate:` · **Writes** `Treasuries:`

| offers work | as | on the rung | handing out |
|---|---|---|---|
| `BotBailiff` | bailiff | Free | `BotEvict` |
| `BotFeuder` | feuder | Free | `BotQuarrel` |
| `BotFitter` | fitter | Free | `BotBench` |
| `BotHirer` | hirer | Free | `BotHire` |
| `BotReeve` | Reeve | Free | `BotHarrow` |
| `BotSteward` | steward | Free | `BotHall` |
| `BotSupplier` | supplier | Free | `BotSupply` |
| `BotTollman` | tollman | Free | `BotWard` |

- **Trap.** A house is permanent and a population is not, so an unowned hall decays: `BotEstate.Adopt` is the only thing standing between an evening's building and an empty ruin.
- **Trap.** The price is a dial rather than the engine's 35,250gp, which no bot on this shard could ever reach. Watch the `short` clause in the summary before moving it.
- **Trap.** `BotEstate.Fund` is the chest plus the members' spare above the keep; the members' spare is the same pool `BotGuilds.Stand` spends on their armour and supplies every minute, so on a poor shard it reads nought and only the chest ever grows. The `Treasuries:` line prints both halves per guild.

| file | decides |
|---|---|
| `BotAbode.cs` | Houses of the population's own: a bot that has done well buys one, spends its leisure there, and keeps its belongings in a chest by the wall. |
| `BotBailiff.cs` | Whether anybody is standing on this guild's land who should not be, and who is going to say so. |
| `BotBench.cs` | Buying one workbench for the guild's hall and setting it up. |
| `BotBounds.cs` | Ground another guild holds, and what may not be put on it. |
| `BotChest.cs` | The guild's chest: what the guild's ground earns, and what it pays for before any member's pack is asked. |
| `BotChestStore.cs` | Keeps the guilds' chests across restarts. |
| `BotClaim.cs` | What a guild has claimed of the island itself, square by square, and how a claim is won. |
| `BotClaimStore.cs` | Keeps who owns which square of the island across restarts. |
| `BotDues.cs` | A guild's dues: the share of what its members' work brings in that goes into the guild's chest, and what the chest keeps back from everyday spending while the g |
| `BotEnlarge.cs` | Moves a guild out of the hall it has filled and into the next size up: new ground proved, the price levied, the old hall taken down with its counter and benches carried across, the bigger one raised. |
| `BotEstate.cs` | The halls the guilds own: where they stand, who paid for them, and what they hold. |
| `BotEstateConfig.cs` | What Configuration/bot-estate.json is allowed to say. |
| `BotEstateModule.cs` | The guilds' halls, as a module. |
| `BotEvict.cs` | Walking over to somebody working your guild's land and telling them to move along. |
| `BotExile.cs` | Who won a war, and the debt the loser owes because of it: its hall goes outside the winner's yard. |
| `BotFeud.cs` | Who each guild at war has called its members onto, and how long the call stands. |
| `BotFeuder.cs` | Offers a member of a guild at war somebody of the enemy to fight. |
| `BotFitter.cs` | Who decides the guild can afford its next workbench, and sends somebody to fetch it. |
| `BotFittings.cs` | What goes inside a hall: a chest, and the guild's own tools of the trade. |
| `BotHall.cs` | Raising the guild's hall: walk to the plot, call the levy, put the house up. |
| `BotHallKind.cs` | The sizes a guild's hall comes in, smallest first: what each is, how many members it holds, and what it costs. |
| `BotHire.cs` | Hiring a merchant for the guild's hall: buy the contract in town, walk it home, and set the shopkeeper up. |
| `BotHirer.cs` | Whether the guild wants a merchant in its hall, can pay for one, and who is going to fetch it. |
| `BotHold.cs` | Standing in a square your guild has laid claim to, for as long as the claim takes. |
| `BotLand.cs` | The ground round a guild's hall, and what belonging to a guild does to work done on it. |
| `BotOffice.cs` | One guild, one officer, one errand at a time — and the difference between having been offered an errand and actually being on one. |
| `BotOutpost.cs` | A guild's second house, out on the far edge of its land: where its members rise when they fall nearer to it than to the hall, and a yard of the guild's own out there — land the toll can be charged on. |
| `BotPlot.cs` | Finding ground a house will actually stand on. |
| `BotQuarrel.cs` | Closing with a member of a guild yours is at war with, and fighting them. |
| `BotRally.cs` | Falling in with the guild's war company and fighting with it, as a company, until the company is done. |
| `BotReeve.cs` | The guild's own company for the guild's own ground. |
| `BotRegard.cs` | What one guild thinks of another, and the two thresholds that turn an opinion into a war. |
| `BotRemove.cs` | Carrying a beaten guild's hall out of the winner's yard and putting it down again. |
| `BotRepose.cs` | Offers a bored bot with a house of its own the walk home, to pass some time there. |
| `BotSeat.cs` | Where each guild lives: the point its hall is raised near, its members are born at and rise again at, and the place "home" means to them. |
| `BotSeizure.cs` | A claim on a square another guild holds: war at once, without a meeting, the price of it in every guild's and every town's regard for the claimant, and the weig |
| `BotSeizureStore.cs` | Keeps the seizures each guild is remembered for across restarts. |
| `BotShelf.cs` | The guild's own counter: finding the merchant in the hall, putting goods on it, buying off it, and carrying its takings back to the guild. |
| `BotShift.cs` | Shifting a hall off another guild's held ground onto the guild's own: within its own square first, then onto the guild's other squares near it, with the standing hall's own tiles counted as free. |
| `BotSteward.cs` | Who decides the guild can afford a hall, and offers to go and raise one. |
| `BotSupplier.cs` | Whether the guild's counter is short of something its members keep running out of, and who is going to fetch it. |
| `BotSupply.cs` | Fetching a batch of something the population keeps running out of and leaving it on the guild's own counter, so that the other nine members do not each walk to Britain for it. |
| `BotToll.cs` | How a guild charges strangers for hunting its land. |
| `BotTollman.cs` | Sends a fighter of a patrolling guild to a stranger hunting the guild's land, to tell it the hunting there is tolled. |
| `BotTownGround.cs` | What Configuration/bot-townground.json may say. |
| `BotWar.cs` | The wars themselves: which pairs are fighting, what each side has taken off the other, and the rules that begin, end and forbid one. |
| `BotWarStore.cs` | Keeps the war ledger across restarts: the wars standing, with their clocks, kills and plunder; the truces and the once-a-day declaration clocks; and the halls a lost war still owes a move. |
| `BotWard.cs` | A warden's walk to a stranger hunting the guild's land, and the sentence that makes the toll owed. |

### `BotDiplomacy/` — meetings between guilds, and with their towns

No war and no alliance without a meeting. A grievance at the war line, or a friendship at the alliance line, calls one (`BotRegard` → `BotParley.Grieve`/`Befriend`); a guild thought ill of asks for peace, and two on good terms propose trade. Any member walks as envoy (a guild of one sends its founder) to the host guild's town hall — the bank unless the file names one (`BotParley.Seat`) — the host sends an answerer, one of Argus's squad is summoned as a visible witness (`mindedBots/debugger/BotWitness`), the sides state their grievances from the books (`BotGrievances`), and the duke's word is asked of the model with the outcome an enumeration, or given by rule (`BotDuke`): war, peace, trade terms, standing orders, an alliance or nothing. Agreements change prices and service on the bots' market and forbid war (`BotPact`). Each guild also sends an envoy to its towns, is set a checkable task (`BotTownTask`) and pays the town's shops half a percent less or more per task (`BotBurgh`); three failures in a row put it out of the town for 48 hours. Patrick's order of 29.09.2026, evening.

**Module** `BotDiplomacyModule` · **Config** `bot-diplomacy.json` · **Writes** `Diplomacy:`

| offers work | as | on the rung | handing out |
|---|---|---|---|
| `BotHerald` | Herald | Free | `BotEnvoy` |
| `BotTownWatch` | TownWatch | Free | — |

- **Trap.** A restart ends every meeting in flight; the grievance calls it again at its next drift. Agreements, the books, the pair clocks and the towns' standing are kept (`BotDiplomacyStore`).
- **Trap.** The duke's model answer is waited for two minutes after the last line and then given by rule; the log line `the word, by <who>` says which.

| file | decides |
|---|---|
| `BotBurgh.cs` | Each guild's standing in each town: what the town's shops charge it, the task the town has set it, how many it has failed in a row, and whether it has been put out of the town. |
| `BotDiplomacyModule.cs` | The numbers of Configuration/bot-diplomacy.json. |
| `BotDiplomacyStore.cs` | Keeps the guilds' dealings across restarts: the agreements sworn and how long each has left, how long each pair must wait before the duke hears it again, the bo |
| `BotDiplomacyWeb.cs` | The guilds' dealings on the page: what each pair of guilds thinks of the other and what stands between them, each guild's standing in each town it deals with, and the meetings standing. |
| `BotDuke.cs` | The duke: what two guilds' envoys say before the witness, the facts the duke is given, the answers he may give, the rule that answers when the model cannot, and what each answer does. |
| `BotEnvoy.cs` | Going to a meeting for one's guild: its envoy walking to the other guild's town or to its own town's hall, or its answerer walking to where the envoy has come — |
| `BotGrievances.cs` | What each guild holds against each other, by cause: the book an envoy reads its grievances from and the duke judges by. |
| `BotHearing.cs` | One meeting: two guilds, or a guild and a town, a place, an envoy, a host, a witness, what was said and what came of it. |
| `BotHerald.cs` | Offers a free member of a guild that has a meeting called the walk as its envoy, or as its answerer when the other guild's envoy is near. |
| `BotPact.cs` | What two guilds have agreed before a witness, and the three places on the shard that each agreement changes. |
| `BotParley.cs` | Meetings: one guild's envoy walks to another guild's town, or to its own town's hall, and a word is given there before a witness — war, peace, trade, standing o |
| `BotTownReport.cs` | How a town stands, from the shard's own numbers: what its shops are out of, what the market wants, how dangerous the ground round it reads, who lives there. |
| `BotTownTask.cs` | One task a town sets a guild, and the catalogue of the kinds it may set: each one something the shard can check by itself. |
| `BotTownWatch.cs` | A guard a town set is worked, not waited for (30.09.2026, build 339). |

### `BotRelics/` — relics and banners

Five town relics — the Crown of Britannia, the Bell of Minoc, the Anchor of Trinsic, the Lantern of Vesper, the Scales of Yew — and one banner per guild, each an engine item the world save keeps and a registry (`BotRelics`, kept by `BotRelicStore`) that says whose it is. A town relic is laid in a lair behind a warden (`BotRelicLair`); the company that clears it takes it and walks it home (`BotRelicHomeward`); its holder's members pay 95 % at that town's counters (through `BotBurgh.Factor`), the guild gains appeal (`BotGuilds.Appeal`) and calls the Great Fair once a day (`BotFair.CallGreat`). A thief may lift one from a carrier (`BotRelicSnatch`) and the fence sells it back; a relic in a hall is taken only by a war: the loser's banner goes to the winner (`BotWar.End` → `BotRelics.WarEnded`), the loser may declare a war of revenge past a truce (`BotWar.MayDeclare` → `BotRelics.Avenges`) and buys its banner back chest to chest. Patrick's order of 07.10.2026.

**Module** `BotRelicsModule` · **Config** `bot-relics.json` · **Writes** `Relics:`

| offers work | as | on the rung | handing out |
|---|---|---|---|
| `BotRelicSnatch` | RelicThief | Free | — |

- **Trap.** A relic item is `Movable = false` and blessed: everything that moves it is code, and the registry's look every 30 seconds puts it where the registry says. Never delete a `BotRelicItem` by hand without `do relic <name> ; hide`.
- **Trap.** A restart hides a relic that was in a lair and takes its warden away; every other relic stays where it was.

| file | decides |
|---|---|
| `BotRelicHomeward.cs` | Offers the bot carrying a relic for its guild the walk home to hang it in the hall (07.10.2026, relics20). |
| `BotRelicItem.cs` | A relic in the world: the Crown of Britannia, a guild's banner, the Bell of Minoc (07.10.2026, relics20). |
| `BotRelicLair.cs` | A town relic in a lair behind its warden (07.10.2026, relics20; Patrick: "the Crown of Britannia — a chest behind a lair boss"). |
| `BotRelicSnatch.cs` | Offers a thief a relic in a carrier's pack (07.10.2026, relics20; Patrick: "a relic can be stolen — thieves get a target"). |
| `BotRelicStore.cs` | Keeps the relics' registry across restarts (07.10.2026, relics20): every relic with its holder, its place, the guild a stolen relic or a captured banner came fr |
| `BotRelics.cs` | Where a relic is: nowhere yet, in a lair's chest, in a bot's pack, on a hall's floor, kept by a guild with no hall, or stolen. |
| `BotRelicsModule.cs` | The relics as a module (07.10.2026, relics20): registers itself from its own Configure (no line in BotCore), reads Configuration/bot-relics.json, offers the thr |

### `BotQuest/` — the board of errands

What the door and the marshal of events ask of the population, for a price: kill so many of a creature near a place, bring so many of a thing to a place, scout a place. The reward is held by the treasury from the moment of posting and paid to whichever bot does the errand; an errand untaken for two hours lapses and gives the money back. One board instead of a list and a verb for each thing somebody wants done; the marshal who posts on his own lives with the watchers.

**Module** `BotQuestModule` · **Writes** `Quests:`

| offers work | as | on the rung | handing out |
|---|---|---|---|
| `BotQuester` | Quester | Free | `BotQuestDeed` |

- **Trap.** An offer is not an errand: the board marks an errand taken in the deed's Taken, never in the proposer, so a bot that loses the auction has not taken anything.

| file | decides |
|---|---|
| `BotLairs.cs` | Where a creature lives on the island, read from the spawners that keep it. |
| `BotQuest.cs` | What an errand on the board asks for. |
| `BotQuestDeed.cs` | Doing an errand off the board: killing what it names, bringing what it asks, or standing where it points. |
| `BotQuestModule.cs` | The board of errands as a module: opens the board's clock and offers the errands to the population. |
| `BotQuestStore.cs` | Keeps the board of errands across restarts, each errand with its count and its held reward; a taker is not kept, so every errand is open again after a boot. |
| `BotQuester.cs` | Offers a bot the errand on the board that fits it best: a kill to a fighter who is no novice and dares the ground, a delivery to a bot already carrying the goods, a scouting to anybody. |
| `BotQuests.cs` | The board of errands: what the watchers and the door ask of the population, for a price. |

### `BotQuad/` — the island as squares

The map cut into squares thirty tiles across, each carrying one number: how safe the population has found it to be. It is written by everything that dies or kills and read by the captain, the Baron and anything choosing where to go. Also the frontier — the nearest square nobody has ever stood in — and the scouting that fills it in.

**Writes** `The captain:`

| offers work | as | on the rung | handing out |
|---|---|---|---|
| `BotScoutmaster` | Scoutmaster | Free | `BotScout` |
| `BotWarden` | Warden | Free | `BotScout` |

- **Trap.** This one *is* saved between restarts, unlike the ground survey.

| file | decides |
|---|---|
| `BotKept.cs` | What the island's spawners keep alive near a place, reckoned as a fight and set beside what the square asks and what a bot brought there — counted and written down, and nothing refused. |
| `BotMarkers.cs` | Writes what the population knows about the island into the client's own world-map pins. |
| `BotQuad.cs` | The island cut into squares thirty tiles across, each carrying one number: how safe the population has found it to be. |
| `BotQuadStore.cs` | Keeps the island's reputation across restarts. |
| `BotScout.cs` | A captain taking a paid party out to ground nobody has ever stood in. |
| `BotScoutmaster.cs` | Offers a captain the nearest ground nobody has ever stood in. |
| `BotSweepRecord.cs` | What sweeps of each square of the island paid lately: how many sweeps walked it, how many fights they found in it, and what the hunts those finds led to came to. |
| `BotWarden.cs` | The Baron walking his own ground: the nearest square nobody has stood in, alone if need be. |

### `BotRanger/` — livery

One file: the King's Rangers' kit, which is the Baron's livery on five more bodies.

| file | decides |
|---|---|
| `BotRangerRegalia.cs` | The King's Rangers' kit: the Baron's livery on five more bodies. |

### `BotDashboard/` — watching it happen

`[bots` — an administrator command opening five tabs: the population, their market, what they are short of, what the city wants, and what the population is doing.

**Module** `BotDashboardModule`

| file | decides |
|---|---|
| `BotCrier.cs` | What the watcher has announced, in a shape this assembly can read. |
| `BotDashboardGump.cs` | One window onto the whole population, onto the market it trades in, and onto what it cannot get hold of. |
| `BotDashboardModule.cs` | The dashboard as a module: one command, registered once. |

### `BotAlarm/` — the smoke alarm

The one place the shard speaks first. Six rules are read once a minute, each dividing a number the other subsystems already keep by something that gives it meaning, and anything past a threshold becomes a line of JSON in `logs/alerts.ndjson` — raised once, repeated at most every fifteen minutes, and cleared in its own event when it goes back to normal. An hourly heartbeat says so when nothing is wrong. Errors are counted by reading the tail of the shard's own session log, which is the only way to see them all without modifying the engine's logger.

**Module** `BotAlarmModule` · **Config** `bot-alarm.json`

- **Trap.** Every threshold is a dial, so tuning happens through the debugger's door on a living shard rather than by restarting: `dial BotSigns.ErrorsAt 3`.
- **Trap.** A window equal to the tick fires every other tick. `BotSigns.Due` carries half a tick of slack for that reason, and every event reports the window it actually measured rather than the one intended.
- **Trap.** The error count is read through a buffered redirect and can lag the world by a buffer. Good for noticing, useless for timing.

| file | decides |
|---|---|
| `BotAlarm.cs` | The channel the shard raises its own alarms into: one line of JSON per event in logs/alerts.ndjson, meant to be watched rather than searched. |
| `BotAlarmModule.cs` | What Configuration/bot-alarm.json may say. |
| `BotBoot.cs` | The one question asked once, at the boot: did the world come back? Written after five hours on an empty island. |
| `BotSigns.cs` | The rules that decide when the shard should speak up: read once a minute, each one comparing a number the shard already keeps against a threshold, over a window it names. |
| `BotTail.cs` | Counts the errors the shard is printing, by reading the tail of its own session log. |

### `BotVoice/` — what the bots say

Speech as a by-product of doing: a line rides on work taken up or ended, a blow, a death, a war, a hall, an order, and now and then a mood. Five channels — local, guild, world, mood, cry — each an audience; every line is also an event on the stream and a line in `logs/bot-speech.log`. The phrase bank is written whole to `bot-voice.json` so the words can be changed without the code. Rationed per bot and channel, never per shard.

**Module** `BotVoiceModule` · **Config** `bot-voice.json`

- **Trap.** Nothing here is offered to the auction and nothing costs a beat: the voices must never change what the population does.
- **Trap.** The occasions are the will's own hooks (`BotWill.Started`, `BotWill.Ended`), the damage and death hooks on `BotMobile`, and three lines in `BotWar` and `BotHall`.

| file | decides |
|---|---|
| `BotMoods.cs` | A bot's mood in one word, read off facts the shard already keeps, so that a mood line carries a tag that the person at the log and the bot in earshot can both r |
| `BotPhrases.cs` | What a bot says, by occasion. |
| `BotVoice.cs` | The bots' voices: what they say out loud, to whom, and how often. |
| `BotVoiceConfig.cs` | What Configuration/bot-voice.json may say. |
| `BotVoiceModule.cs` | The voices as a module: reads the file, opens the speech log, and hooks the occasions. |

### `BotSocial/` — bots answering each other's moods

A mood line carries one word behind it — hurt, tired, grieving, poor, lonely, bored, proud, or a shade of content (`BotMoods`, read off facts the shard keeps) — and is said only when somebody is in earshot, waiting up to ten minutes for an audience. The nearest two listeners not of a hostile guild answer it a few seconds later: always in words, and where they can with help — a bandage, a fire laid beside the tired, the nearest quarry called as a hunt for two for the poor, a place in a company for the lonely — written against the listener as an invitation and handed to its own auction by `BotFellow`, where a seam that pays keeps the bot and an idle bot goes. Offers taken are followed through the will's own hooks to how they ended. Patrick's point 4 of 01.10.2026.

**Module** `BotSocialModule` · **Config** `bot-social.json`

| offers work | as | on the rung | handing out |
|---|---|---|---|
| `BotFellow` | Fellow | Free | — |

- **Trap.** The listener answers the tag, never the speaker's insides; the deed that carries the help out checks the world itself (a bandage is wound only on a wound).
- **Trap.** No handouts: a poor bot is pointed at a hunt. A company's call (`BotSquads.Enrol`) is the one answer that is not an auction, and it takes only a bot whose work resumes and is not on paying work.
- **Trap.** `Society:` counts what was heard by mood, offered by kind, accepted, finished, lapsed, and refused for hostility; `Voice:` counts moods said to somebody against moods said to nobody.

| file | decides |
|---|---|
| `BotFellow.cs` | The one place a thing heard becomes an offer: hands a bot the help it undertook to offer when it heard a neighbour's mood — the bandage, the fire, the hunt, the |
| `BotSocial.cs` | Bots answering each other's moods: what a bot in earshot does about a neighbour saying it is hurt, tired, grieving, poor, lonely, bored or proud (01.10.2026, bu |
| `BotSocialModule.cs` | The numbers of Configuration/bot-social.json. |
| `BotSocialTalk.cs` | What a bot says back to a mood it heard: a word for each of the seven, and the line that goes with an offer — a bandage, a fire, a hunt, a place in a company — and the thanks that may come back. |

### `BotWeb/` — the shard as a page

The whole population as one JSON document on `127.0.0.1:2599`, rebuilt on the game loop every two seconds and handed to an `HttpListener` that reads nothing from the world; an event stream (`bot-events.ndjson`) with sequence numbers and server-sent events; a picture of the map from the client's `radarcol.mul`; the crafting chain as the craft systems define it; a minute of history at a time. The page is `Distribution/Data/bot-web/index.html`.

**Module** `BotWebModule` · **Config** `bot-web.json`

- **Trap.** Every number on the page is a counter the log already prints, so the two cannot disagree; the one addition is the split of searches by purpose in `BotWalk`.
- **Trap.** The map is sampled in slices on the loop because the tile matrix is not safe off it, and encoded off the loop because encoding needs no world.

| file | decides |
|---|---|
| `BotArtFiles.cs` | A decoded picture: ARGB pixels, its size, and the point inside it that stands on the tile. |
| `BotEarnings.cs` | What the bots earn, for the page (29.09.2026, build 327; Patrick: "add money to the web dashboard — how much the bots earn; leave the special classes out"). |
| `BotEvents.cs` | One thing that happened to one bot, as the dashboard and the event log see it. |
| `BotWebArt.cs` | The pictures the live window draws its scene with, decoded from the client's own files on the listener's threads and kept (02.10.2026: "the picture straight from the game"). |
| `BotWebConfig.cs` | What Configuration/bot-web.json may say. |
| `BotWebCraft.cs` | The crafting chain as the engine defines it and the population fills it: for each material the craft systems consume, where it comes from, how much is on the ma |
| `BotWebHistory.cs` | One minute of the shard, for the page's charts. |
| `BotWebHooks.cs` | What the thinking layer says about itself, for the page. |
| `BotWebLive.cs` | The live window (Patrick, 02.10.2026: "a window that shows in real time what is happening in the game and with a bot, say once a second"): a frame for each bot |
| `BotWebLivePicture.cs` | The live window's picture (Patrick, 02.10.2026: "can the picture be shown straight from the game?"): what stands where round a watched bot, in the client's own |
| `BotWebMap.cs` | A picture of the map for the page to draw the bots on, made from the client's own radar colours. |
| `BotWebModule.cs` | The web dashboard as a module: the event stream, the snapshot clock, the map picture and the listener. |
| `BotWebServer.cs` | The dashboard's door: a small HTTP server on the loopback address that hands out what the snapshot has already written. |
| `BotWebSnapshot.cs` | The whole shard as one JSON document, rebuilt on the game loop every couple of seconds and handed to the web server as a string. |

### `mindedBots/` — bots that think

The four crafters — and from 07.09.2026 nobody else — choose what to do next through a local language model over Ollama rather than through the auction. The model is given what the bot can see and returns a choice; everything else about them is an ordinary bot. The warrior, architect, sage and Baron minds are commented out in `BotMinds.Start` rather than deleted.

**Module** `BotMindModule` · **Config** `bot-mind.json` · **Writes** `Minds:`

| offers work | as | on the rung | handing out |
|---|---|---|---|
| `BotMindProposer` | Mind | Free | — |

- **Trap.** The model is asked on a wall clock and the answer costs real seconds. Anything that waits on it must not be holding the game loop.
- **Trap.** A crafter mind is shown four blocks nothing else gets: its own bench, the board of wants (with the craft and skill each needs), what is on the stalls, and what the other three are holding. The last of those exists because four minds given identical state make identical choices.

| file | decides |
|---|---|
| `BotMind.cs` | One thing a mind chose, and what it turned out to be worth. |
| `BotMindChoice.cs` | What a mind came back with: one trade, one number it is prepared to be judged on, and its reason. |
| `BotMindClaims.cs` | Which mind has said it will fill which row of the board, so the others leave it alone. |
| `BotMindConfig.cs` | What Configuration/bot-mind.json is allowed to say. |
| `BotMindCore.cs` | The way in, and the whole of this assembly's contact with the rest of the shard. |
| `BotMindDeed.cs` | A real piece of the shard's work, taken up because a mind asked for it, and measured because a mind predicted something about it. |
| `BotMindLog.cs` | A log of its own, beside the shard's, holding what a mind decided and why. |
| `BotMindModule.cs` | Two thinking bots, as a module of the shard's own bot system — registered from outside it. |
| `BotMindProposer.cs` | The one place a thought becomes an offer. |
| `BotMindSight.cs` | The world as one bot can see it, written out for the model. |
| `BotMindTalk.cs` | The one place the thinking bots can hear each other. |
| `BotMinds.cs` | What is kept between sessions: one bot's name and the rules it has written for itself. |
| `BotOllama.cs` | The only thing in this assembly that talks to the model, and the only thing that leaves the game thread. |

### `mindedBots/debugger/` — Argus, the observer

A thinking thing that is not one of the population: an invisible figure that nobody in the world can see, that cannot be hurt and cannot hurt anything, whose whole job is to watch the bots and write what it believes into `logs/bot-debugger.log`. It has a door for a person at the keyboard (`argus-in.txt`) and a small set of administrator gestures.

**Module** `BotDebugModule` · **Config** `bot-debugger.json`

- **Trap.** Five false alarms in one day, all of them artefacts of the instrument rather than faults in the shard. Check the watcher before believing it about the population.

| file | decides |
|---|---|
| `BotAudit.cs` | The roll-call: every two minutes, three questions asked of every bot, and a hand laid on the ones that answer no to all of them. |
| `BotConsole.cs` | A door into the running shard for whoever is holding the keyboard rather than a character: write a line into argus-in.txt and the answer appears in argus-out.txt within a couple of seconds. |
| `BotDebugConfig.cs` | What Configuration/bot-debugger.json is allowed to say. |
| `BotDebugCore.cs` | The debugger's way in, and the whole of its contact with the rest of the shard. |
| `BotDebugLog.cs` | The debugger's own file: logs/bot-debugger.log, and nothing else is written into it. |
| `BotDebugMemory.cs` | One thing the debugger has come to believe, and how many times it has come to believe it. |
| `BotDebugModule.cs` | The debugger as a module of the shard's own bot system, registered from outside it. |
| `BotDebugNote.cs` | What the debugger came back with after one look at the population: one claim, the numbers it was made from, a guess at the cause and one change worth making. |
| `BotDebugSight.cs` | What the debugger is told: who it is, what it is looking at, and what it has already learned about the shapes defects take here. |
| `BotDebugWeb.cs` | The debugger squad on the dashboard's live window. |
| `BotDebugger.cs` | The body the debugger looks out of: one figure in a white robe that nobody in the world can see, cannot be hurt, cannot hurt anything, and gets about by appearing somewhere else. |
| `BotDials.cs` | Every tunable number in the bot assemblies, readable and settable while the shard is running. |
| `BotEventsArgus.cs` | The events of the world, as Argus's squad serves them (07.10.2026, events17): the herald goes to the place and calls the line in the world channel (, ), the cro |
| `BotHail.cs` | The debugger's ear, and the one door in the world through which a person can reach it. |
| `BotHand.cs` | The debugger's hands: the handful of things a person with an administrator's account would type at a stuck shard, made available to Argus by name, bounded, and written down every single time. |
| `BotMarshal.cs` | The marshal of events: the fourth of Argus's watchers, who organises rather than diagnoses. |
| `BotOfficersArgus.cs` | The guilds' bouts before Argus (07.10.2026, officers20): a herald's challenge, and its end, called at the place by Argus's herald and in the world channel (, ), as the events' are. |
| `BotRevel.cs` | Something for the population to do that nobody planned: a bounty, a hunt, a contest. |
| `BotStewardArgus.cs` | The model as a town's steward (07.10.2026, steward20): asked, when the slot is free, to choose one of the decrees the rule set found lawful in the town this rou |
| `BotTourney.cs` | The championship: every hour Argus's herald calls the strongest of the ranks to a ring, sets them against each other one pair at a time for the right to challen |
| `BotTourneyArena.cs` | The day's championship at the round arenas east of Papua (07.10.2026, arena27): twelve rings found in the map's statics round (6563, 3168) at boot, at least 50 free bots once a real day from 20:00, ported in batches with a moongate flash, one pair to a ring, the referee shown at the main ring, losers sent home; `do arena [force|state|stop]`. |
| `BotVenues.cs` | Where the watchers hold what they hold: a revel's feast in a town, a hunt's waves and an orc camp in the countryside, the championship's ring — each chosen from |
| `BotVigil.cs` | The debugger itself: the body, the watch it keeps, and the two questions it asks. |
| `BotWatch.cs` | One bot as the debugger has actually seen it: everything here was measured by this file, on this file's own clock, since the moment the debugger first laid eyes on the bot. |
| `BotWatcher.cs` | One watcher of the squad: a name, a robe, a body, a charge, and what it has said. |
| `BotWaves.cs` | Something to fight, when the watcher has called a hunt and the island has nothing to offer. |
| `BotWitness.cs` | The debugger as witness to the guilds' meetings, and the voice of the duke and of the towns: one of the squad appears at the meeting, shows itself, hears both sides, and says the word the model gives. |

### `BotCharacter/`

**Module** `BotCharacterModule` · **Config** `bot-character.json`

| file | decides |
|---|---|
| `BotBiography.cs` | A bot's life story: the three most important things that ever happened to it, in its own words with the real names and numbers, and the counts behind them; writ |
| `BotCharacterModule.cs` | The numbers of Configuration/bot-character.json. |
| `BotCharacterStore.cs` | Keeps every bot's character across restarts: its traits, its kills and veteran's title, the counts and the three tales of its biography, and its ties. |
| `BotGrudge.cs` | Seeking out a personal enemy to have it out: a walk to the one this bot counts an enemy (), ended by a challenge to a duel when it is reached. |
| `BotLife.cs` | One remembered event of a bot's life, told in its own words: what happened, who to, and how much it weighed. |
| `BotTies.cs` | Friendships and feuds between two bots, as one number each way: what this bot thinks of that one, from −100 to +100. |
| `BotTraits.cs` | The ten natures a bot can have, in five opposed pairs. |

### `BotCharters/`

**Module** `BotChartersModule` · **Config** `bot-charters.json`

| file | decides |
|---|---|
| `BotCharters.cs` | Town charters: a monopoly on a craft in a town, sold at a sealed auction every six hours, and the levy everybody else pays its holder on each sale of that craft's goods there. |
| `BotChartersModule.cs` | Configuration/bot-charters.json: every key optional, PascalCase; nothing written means the code's own number. |

### `BotColonies/`

| file | decides |
|---|---|
| `BotColonies.cs` | The guilds' colonies on the islands: who has one and where, the parties at the docks, the store each colony fills, and the line, the page and the door that say so. |
| `BotColony.cs` | One guild's colony on an island: the square it landed on, the store its colonists fill, who is on its land and since when, and what its ship has carried home. |
| `BotColonyRun.cs` | Offers the holder of a colony's ship, on the colony's land, the run home: when the store holds units, or a colonist's shift is over — and no oftener than . |
| `BotColonyStore.cs` | Keeps the guilds' colonies across restarts: whose, on which island, where the store stands and what the colony has brought in. |
| `BotExpedition.cs` | Offers a guild's captain an expedition: the member holding the guild's ship, or its head when the guild has none and can adopt or buy one; to found a colony on |
| `BotPiracy.cs` | Pirates: The Shadow waits on the quay for a colony's loaded ship and boards it as it comes in. |
| `BotSailing.cs` | One sailing of a guild's ship with a party aboard: the muster at the dock, the voyage, and the landing. |
| `BotSignOn.cs` | Offers a member of a guild with a party mustering the place in it: for a passage out, a member on the dock's land within of it — one with a pick first, at a bet |

### `BotCrime/`

**Module** `BotCrimeModule` · **Config** `bot-crime.json`

| offers work | as | on the rung | handing out |
|---|---|---|---|
| `BotCutpurse` | Cutpurse | Free | — |
| `BotFencer` | Fencer | Free | — |

| file | decides |
|---|---|
| `BotBounty.cs` | Offers a free fighter of any guild a head on a town's wanted list, priced at the bounty. |
| `BotCrime.cs` | The town's side of crime: the thieves' roll, the reports a victim makes, the wanted list with a bounty on each head paid out of the town's purse, the term in a |
| `BotCrimeModule.cs` | The numbers of Configuration/bot-crime.json. |
| `BotCrimeStore.cs` | Keeps the town's books on crime across restarts: the thieves' roll, every report, the wanted list with its bounties, the terms served, the release and repentance clocks and the traces against a fence. |
| `BotCutpurse.cs` | Offers a thief a turn in the crowd by a bank: the engine's own Stealing on a bot's pack. |
| `BotFencer.cs` | Offers whoever holds stolen goods the way to turn them into coin: the walk to the band's fence, who buys at half their worth, or — for the fence itself, or a th |
| `BotHighway.cs` | The ambush on the road: two or three of The Shadow lie in wait ahead of a lone carrier with goods worth taking, out of any guarded town, and fall on it as it passes. |

### `BotEvents/`

**Module** `BotEventsModule` · **Config** `bot-events.json`

| file | decides |
|---|---|
| `BotCaravan.cs` | Offers a trader near a posted caravan's source the lead (07.10.2026): a trader of the neutral class, fit, in the world, within of the source's counter, able to |
| `BotCaravans.cs` | Caravans (07.10.2026, Patrick's third idea: "a town needs goods carried to another town, the caravan hires guards from the bots, it is attacked on the way"). |
| `BotEventsModule.cs` | The events of the world as a module (07.10.2026): registers itself from its own Configure (no line in BotCore), reads Configuration/bot-events.json, offers the |
| `BotEventsStore.cs` | Keeps what the events owe across a restart, so that no coin is made or lost by one (07.10.2026). |
| `BotFair.cs` | The fair (07.10.2026, Patrick's second idea: "a fair in a town once a day: lower prices, traders and crafters come, a tournament before Argus, a lottery"). |
| `BotFairgoer.cs` | Offers a trader or a crafter within of an open fair the walk to it (07.10.2026): once a fair, while at least half an hour of it is left, when the way is not sea |
| `BotInvasion.cs` | The invasion (07.10.2026, Patrick's first idea: "every few hours a wave comes out of a dungeon onto the road by a town: an announcement to all, a militia musters, the town pays a reward"). |
| `BotMilitia.cs` | Offers a fighter of an invaded town the town's call: to raise the militia and march on the place (07.10.2026). |
| `BotNight.cs` | The night (07.10.2026, Patrick's fourth idea: "roads are more dangerous at night, night deliveries pay more"). |
| `BotRumour.cs` | Rumours (07.10.2026, Patrick's fifth idea: "once an hour Argus spreads a rumour of a treasure or a lair, the first to reach it takes it"). |
| `BotSeasons.cs` | The events of the world (07.10.2026, Patrick's five ideas approved at 00:10: "let's build them tonight, and keep watching the key numbers"): an invasion out of |

### `BotOfficers/`

**Module** `BotOfficersModule` · **Config** `bot-officers.json`

| offers work | as | on the rung | handing out |
|---|---|---|---|
| `BotGuildScout` | Scout | Free | — |
| `BotHeraldStand` | Herald | Free | — |
| `BotQuartermaster` | Quartermaster | Free | — |

| file | decides |
|---|---|
| `BotGuildBout.cs` | A bout between two guilds, called by a herald: each guild's champion against the other's at the herald's stand, before Argus, for a purse paid chest to chest. |
| `BotGuildScout.cs` | The scout's work, in the order it is offered: a rumoured chest reached first, another guild's company shadowed and reported, and a patrol of the dangerous ground by a plan. |
| `BotGuildStore.cs` | The guild's store at its hall: the members' consumables held in common, a kit issued from it as a debt, and the debts repaid from the member's next spoils. |
| `BotHeraldStand.cs` | The herald's work: a recruiting stand by the bank of the guild's town, the guild's news read aloud there, and a challenge to a rival guild's bout. |
| `BotOfficers.cs` | The three posts a guild's head gives out: the store, the stand by the bank, and the road. |
| `BotOfficersModule.cs` | The numbers of Configuration/bot-officers.json. |
| `BotQuartermaster.cs` | The quartermaster's work: a shift at the guild's store, the kits it issues, the surplus it takes in, the wants it posts for the guild and the surplus it sells. |

### `BotPolitics/` — elections, plots, councils and tribute

Politics inside the guilds and the towns, Patrick's order of 07.10.2026: a guild's head elected by its members on their records (renown, kills of other guilds, wars won, dues given) every six hours or when the head is gone or has fallen, and deposed by a vote of no confidence when enough are discontented; what the chest saves for, by vote; each town's tax collector and captain of the guard by the guilds' standing there, paid out of the treasury, and a surcharge on the town's outcasts taxed into it; and an ultimatum before a war on a guild half as strong, with tribute paid chest to chest instead.

**Config** `bot-politics.json`

| offers work | as | on the rung | handing out |
|---|---|---|---|
| `BotGuardDuty` | GuardDuty | Free | — |

- **Trap.** The elected head sticks only because every member's fealty is sworn to it (`BotGuilds.Crown`): the engine recounts fealty once a day at a save and would otherwise put the founder back.
- **Trap.** An ultimatum stands only in front of a war the existing rules already allowed; it can lower the wars per hour, never raise them. A tribute standing refuses any declaration between the pair (`BotWar.MayDeclare`).

| file | decides |
|---|---|
| `BotCouncil.cs` | The towns' councils: in each town the guild with the best standing collects the town's taxes for a tenth of them, the next captains its guard for the town's wag |
| `BotElection.cs` | Who leads each guild, by the members' vote: elections of the head every few hours or when the head is gone or has fallen, and the plot that can depose a head the members have lost faith in. |
| `BotGuardDuty.cs` | Offers a member of a guild that captains a town's guard a paid watch in that town: the captain's post as work. |
| `BotPolitics.cs` | Politics inside guilds and towns, as one clock: the page's politics object, the phrases, and the seams other folders wire. |
| `BotPoliticsStore.cs` | Keeps the guilds' politics across restarts: each guild's office (its elected head, the votes, when, its run of lost wars) and every member's record it is electe |
| `BotPurpose.cs` | What a guild's chest saves for, by the members' vote: a hall, a house in Britain, a widening, a war chest, a ransom fund, or nothing kept back at all. |
| `BotStewardStore.cs` | Keeps the towns' stewardships across restarts (07.10.2026, steward20): each town's steward guild and how long it has held it, its coffer (held out of the treasu |
| `BotStewardship.cs` | Each town's steward: the head of the guild with the best standing in it, holding the town's coffer and its levers, and answering for both. |
| `BotStewardshipDecrees.cs` | The steward's decrees: what is lawful in a town now and what each costs (), the rule that picks one, the model's question, what each does (), and Argus's two commands (do steward, do decree). |
| `BotTribute.cs` | Ultimatums and tribute: a strong guild about to make war on a weak one — at the duke's word, or by seizing its ground — demands so much a day instead, and the weak pays from its chest while it can. |

### `BotReachMap/`

**Module** `BotReachModule` · **Config** `bot-reach.json`

| file | decides |
|---|---|
| `BotReachBuild.cs` | The ground the reach map is surveyed from: the navigation graph's own probe (: land, statics and the houses standing on it), with two differences, both measured against what a bot's walker does. |
| `BotReachCheck.cs` | What the check found, published whole for the dashboard through one volatile reference. |
| `BotReachFile.cs` | The reach map's clusters kept on disk between boots, so a restart reads a few megabytes on a worker instead of drawing every tile of the map again on the loop. |
| `BotReachMap.cs` | Where a walker from home can get to, tile by tile: the reach map. |
| `BotReachModule.cs` | What Configuration/bot-reach.json may say. |
| `BotReachSnapshot.cs` | The reach map as one solve left it: a class for every tile, and for a tile whose standing places differ (a bridge over a road, a roof over a room) the class of each with its height. |
| `BotReachSolve.cs` | What one tile of the map is to a walker. |
| `BotReachWeb.cs` | The reach map on the dashboard: a summary the loop writes, and the tiles themselves, cut out of the immutable snapshot on the listener's own threads. |

### `BotTraders/`

**Module** `BotTradersModule` · **Config** `bot-traders.json`

| offers work | as | on the rung | handing out |
|---|---|---|---|
| `BotHauler` | Trader | Free | `BotHaul` |

| file | decides |
|---|---|
| `BotHaul.cs` | A trader's haul: the orders written into its book, the purchase, the road — by land, gate or sea — and the handing over to each buyer in turn, each line struck as it ends. |
| `BotHauler.cs` | Where a haul buys its goods. |
| `BotOrderBook.cs` | A trader's order book: a blue book in its pack, written by the trader and read by anybody who opens it. |
| `BotTradersModule.cs` | The traders as a module (01.10.2026, build 385): reads Configuration/bot-traders.json, offers the haul (), and writes the Traders: line every . |
| `BotTrading.cs` | The traders as the rest of the shard sees them: what neutrality closes, what a haul is allowed to be, the shopkeepers that sell a kind, and the Traders: line. |

### `BotTrades/`

**Module** `BotTradesModule` · **Config** `bot-trades.json`

| offers work | as | on the rung | handing out |
|---|---|---|---|
| `BotRagman` | Ragman | Free | `BotPeddle` |

| file | decides |
|---|---|
| `BotBorrow.cs` | A poor bot's walk to a moneylender — a rich keeper's shop by the bank — and the loan made there, hand to hand (07.10.2026, trades17). |
| `BotInfirmary.cs` | One infirmary while it is open: the healer keeping it, its guild, where it stands, who waits, and the body it went out to. |
| `BotKeepTavern.cs` | A cook's shift keeping a tavern: its meals out of the bank box if the pack is short of them, the walk to the inn, the table opened, supper served to whoever the |
| `BotLibrary.cs` | One library while it is open: the scribe keeping it, its guild, where it stands, its spare scrolls and what it has sold. |
| `BotLoans.cs` | The moneylenders' book: who owes whom, how much is left, the lien that takes a share of every gain the borrower makes until the debt is paid, and the defaulters |
| `BotRagman.cs` | One ragman's buying post while it is open: the trader keeping it, the cave mouth it serves, where it stands, and what it bought. |
| `BotSupper.cs` | Supper at a tavern: the walk to the cook, a meal bought hand to hand and eaten, and a few minutes at the table resting and talking — the fireside's evening, indoors and fed (07.10.2026, trades17). |
| `BotTavern.cs` | One tavern while it is open: the cook keeping it, the inn it stands in, the table round it, and what it has sold. |
| `BotTrades.cs` | The establishments of the professions: a cook's tavern at an inn and a moneylender's desk at a shop by the bank — each a bot's own shift, with its customers sen |
| `BotTradesModule.cs` | What Configuration/bot-trades.json may say. |

### `BotZones/`

**Module** `BotZonesModule` · **Config** `bot-zones.json`

| file | decides |
|---|---|
| `BotHaunts.cs` | One kind of creature as the zones know it: whether it attacks on sight, casts, how far it sees, whether it can leave the water, and how strong one is. |
| `BotZone.cs` | How dangerous a piece of ground is to a bot walking across it, in the four words the planners, the hunt and the dashboard share. |
| `BotZoneBuild.cs` | Turns what is known about the haunts, the living creatures, the observed patrols and the deaths into zones: which cells are patrol, which are aggro fringe, what |
| `BotZoneInput.cs` | The numbers a build works by, copied off the dials when its input is gathered, so a build running off the loop never reads a property the door may be changing under it. |
| `BotZoneMemory.cs` | What the zones learned, kept across a restart: each haunt's awake seconds, sightings, observed range and tour stops, and the cells patrols were seen in. |
| `BotZoneRaster.cs` | Where attacking creatures have actually been seen awake: one number per cell of four tiles, fading with time. |
| `BotZoneScan.cs` | The sweep: every living creature of the home map passed once every few seconds, a window of sectors at a time, within a budget of loop time per beat. |
| `BotZoneShapes.cs` | The outline of a patch of cells as a few smooth rings: marching squares round the cells, Douglas–Peucker to drop the staircase, Chaikin to round the corners. |
| `BotZoneTour.cs` | The debuggers' walk round the zones: each goes to the least-watched zone, wakes the ground round it the way a client would, holds it awake long enough for the patrol to move, and goes on. |
| `BotZoneWatch.cs` | The zone watch: how often the danger map is right, counted both ways and in two parts over a rolling window, and the end of the watching once both parts are right often enough. |
| `BotZones.cs` | The dashboard's copy of the zones: the JSON and the tag a browser may send back to be told nothing changed. |
| `BotZonesModule.cs` | What Configuration/bot-zones.json may say. |
<!--SECTION2:END-->

---

## 3. Recipes

| task | open, in order |
|---|---|
| A trade reports work but produces nothing | the trade's deed (`BotForge`, `BotBake`, `BotSew`, `BotBrew`, `BotFletch`, `BotInscribe`), then `BotCraft/BotCraftwork.cs`, then the engine's `Def<Craft>.cs` for the recipe's own gates |
| A summary bucket is stuck at zero | the class that owns that counter (§1), then the gate immediately above the counter |
| A bot is standing still | `BotWill/BotWill.cs` for what it chose, then that deed's `Advance`, then `BotMovement/BotJourney.cs` |
| Bots will not take a kind of work | the proposer's `Describe()` denominators first; the auction price second (`BotWill`, the deed's `Prior`, `BotLedger`) |
| Something is priced wrong on the market | `BotAuction/BotAuction.cs` for the ask, `BotHaggle` for the movement, `BotListing` for what one stall remembers |
| Add a new craft | a proposer (`IBotProposer`), a deed (`BotDeed`), a `Describe()`/`Forget()` pair, registration in that folder's `*Module.cs` |
| Add a new dial | the field, then the subsystem's `*Config.cs`, then `Distribution/Configuration/bot-*.json` (PascalCase) |
| Change what a class of bot is | `BotClasses/BotClasses.cs` and the class file beside it |

---

## 4. Defect shapes that keep recurring

Recognising the shape is most of the hunt. Each of these has cost this project a session at least once.

**Two thresholds on one shelf.** One number decides whether to set out, a different number decides whether
the work can be done, and the two were written in different files. The symptom is a hard zero in the summary
next to a healthy denominator. Look for a pair of numbers that never meet, not for broken code.

**A new gate needs a new bucket.** A check added without a counter either breaks a denominator or tips its
answers into somebody else's bucket, and the summary then lies about a mechanism nobody can see. Every gate
gets its own name and its own number; there is no bucket called "other".

**The engine refuses in silence.** `CraftItem.Craft` and its family answer a refusal by sending a message to
the player's screen. A bot has no screen. Anything that reads as "the action happened and nothing changed" —
swings with no output, steps with no movement, a spell that never lands — is this. Find the gate in the
engine, not in the bot.

**A multiplier without a floor is a veto.** Any factor that can reach zero silently forbids whatever it
multiplies. Every multiplier needs a floor.

**Work judged before it can pay.** A round that takes six minutes cannot be measured against a thirty-second
window; the symptom is work abandoned exactly on a boundary.

**A number that is both a promise and a wager.** When one field is read as both "what this is worth" and
"what I am betting", the model drifts apart within a day. Split them.

**Seeing is not reaching.** A place on a list is not a place a bot can stand. Ask the engine whether the work
would be accepted *here*, never the distance to a remembered point.

**An invented height is an unreachable place.** A `Z` produced by arithmetic works on flat ground and fails on
a hill. Anything that builds a `Point3D` must settle it against the map.

**The producer sells its own material.** A bot that lists everything it lifts has sold the input of the trade
it carries the tool for. Check what a hunter does with what comes off a corpse before concluding the trade
has no supply.

**A one-way ratchet.** A meter the engine only ever increases — hunger is the known one — makes a mechanism
work a few times and then stop for good, with nothing in the log saying so.

**Two errands wearing one name.** `BotSeeker` buys a scroll to write into a book; `BotArmoury` buys one to
throw. Both built the same undertaking, which then tried to put every scroll into a book — so 233 of 430
rounds in a session reported "the book would not take it" about warriors who had no book, had never wanted
one, and had got exactly what they set out for. A round that succeeded, filed as a failure, 233 times, with
the ledger pricing the trade off it. Underneath the mislabelling was a real refusal in the other direction: a
caster that knew a spell could never buy a scroll of it to throw. When one undertaking serves two callers,
ask what each of them wanted, and make it say which.

**The first one of that type is not necessarily yours.** `FindItemByType<T>` returns whichever it meets
first, and this population goes through every corpse it makes — so one looted necromancer's spellbook sitting
in a caster's pack would answer "where is your spellbook" for ever after. Only a lookup whose *subtype*
matters is exposed: the other twelve in this assembly ask for a concrete tool, where any skillet really is a
skillet. Audited on 05.09.2026; only the spellbook was exposed, and it was **not** the cause of the scroll
losses above — that theory was wrong, and the line above it is what actually did it.

**A counter that stops dead is a mechanism with no input.** Not a quiet mechanism — a disconnected one.
`BotQuarry` pays a bounty when a carcass carries what the board is asking for, and its count read
`4 18 45 45 45 45`: three windows of growth and then a flat line, while 68 orders for ribs were being filled
and 209 hunts taken. Meat was simply not in the list it checked. The same file already carried a note about
the identical freeze on feathers three weeks earlier — `122 for three half-hourly readings` against 179
standing arrow orders — which is what makes the shape worth naming rather than fixing twice. Read cumulative
counters as a series, not as a value: growth that stops while demand continues is the tell.

**Flat supply against skewed demand.** Every number healthy and the trade still failing: gatherers working,
stalls full, sales brisk — and the population meeting 41% of its own reagent demand, buying the rest over a
counter. Demand was lopsided fourfold (sulfurous ash was over half of it) while the pick was one of eight at
random. The ash ran dry at once; the garlic nobody wanted made up a thousand unsold. **No single number shows
this.** It appears only when two distributions are laid side by side — what is bought outside against what
the population itself supplies. Picking the scarcest kind on the stalls took it to 96%.

**A bonus moves the oscillation; it does not remove it.** A reward for the work already under way — inertia, a
margin, a protection window — only shifts the scores at which two choices trade places, which is Mike Lewis's
warning in *Game AI Pro 3* and was this shard's two-minute cap: 224 of 734 drops on 14.09.2026 came between 1.95 and
2.25 minutes in, and four in five went straight back to the same trade. **The symptom is drops piled on a
boundary.** The cure was not a bigger bonus but a rule about *when* to look again — events, not opportunities — see
`BotDeed.Steadfast` and `RESEARCH-decisions.md` §2.

**A rule nobody sets reads exactly like a rule that holds.** `BotClass.DefendsOnly` was enforced by `BotHunter` for
twelve days after the only class that set it was deleted, and healers spent a quarter of their time looking for
fights under it. A flag read in one file and set in another can lose its setter without a compile error or a log
line. **Check who sets a safety rule, not only who reads it.**

**Taking the price off a piece of work takes its brake off too.** The auction learns from what work pays, and that
learning is also what stops a failing errand being chosen again: a trip that fails at nought a minute prices itself
out within a few reviews. `BotDeed.Unpaid` switches the pricing off and the learning with it, so an unpaid deed repeats
a failure for as long as its proposer keeps offering it. Restocking was made unpaid on 14.09.2026 so that it would stop
being refused for costing money; five hours later one bot whose pack had gone past the engine's cap of 125 things
failed it 8,719 times in half an hour, and the shard's alarm read 11% of work finished. **Every `Unpaid` needs the
reasons it fails asked before it is offered**, the way `BotYield.Pocket` now asks the one a counter fails on.

**Instrument before you fix.** Both of the entries above were first diagnosed wrong, on theories that were
plausible and had the shape of defects this project has really had. What settled them was making the sentence
in the log say which of its four ways it had failed. A guess costs a rebuild and a restart; a named counter
costs the same once and never lies again.

**The exit taken between the swing and the timer.** Crafting is asynchronous: the last swing takes the
material before it gives back the thing. Counting before the next swing is only half the rule — a round that
*ends* the moment its material runs out ends inside exactly that gap, and reports, truthfully as far as it can
see, that nothing came of it. Any exit condition on "the material is gone" needs one swing's worth of
patience.

**A trade that can only use what it happens to be holding.** Every other craft here puts a funded order on
the board for what it is short of; the cook could not, so 94% of every look at the skillet answered "no meat"
while the hunters carrying it walked past. The counterweight is the glass rule: **a material with no producer
must not be ordered by the armful**, because an order nobody can fill freezes the buyer's money in escrow
until the market gives up on it.

---

## 5. The engine, where the bots touch it

Everything above is this assembly. These are the files *outside* it that the population depends on, and
every one of them has cost a session at least once because its rule is not where it looks like it should be.

| what | where | the rule |
|---|---|---|
| Craft gates and refusals | `Projects/UOContent/Engines/Craft/Core/CraftItem.cs` | `NeedHeat` / `NeedOven` / `NeedMill` are checked **per recipe**, not by the system's `CanCraft`; the heat table lists forges, ovens, fireplaces, campfires, firepits, heating stands and braziers; a refusal is a message to a screen the bot has not got |
| What an attempt costs | same file, `ConsumeRes` | a **failed** attempt still consumes half the material (`ConsumeOnFailure` is true for smithing), which is why a round set up for one item ends in "out of" after two misses |
| Craft timing | same file, `Craft` | ends by starting a timer: the item appears a second later, so anything counting must count before the *next* swing |
| Which recipe wants what | `Projects/UOContent/Engines/Craft/Def*.cs` | one file per trade; this is where the per-recipe requirements are declared |
| Eating and hunger | `Projects/UOContent/Items/Food/Food.cs` | `FillHunger` adds and refuses at twenty, and **nothing in this fork ever subtracts**; `BotMeal` treats its own ten minutes as the clock instead |
| Recovery rates | `Projects/UOContent/Misc/RegenRates.cs` | fills the mana handler always and the other two **only under AOS**; this shard is Renaissance, so health and stamina ran at flat defaults until `BotMeal.Configure` wrapped them |
| Wearing a tool | `Projects/Server/Mobiles/Mobile.cs`, `EquipItem` | an occupied layer is **refused**, not replaced |
| Harvesting | `Projects/UOContent/Engines/Harvest/Lumberjacking.cs`, `Mining.cs` | the axe must be worn; the pick need not |
| Carving a kill | `Projects/UOContent/Items/Misc/Corpses/Corpse.cs`, `Carve` | costs nothing, no skill roll, doubles on Felucca; yields hides, and tailoring wants leather |
| Repo-wide conventions | `CLAUDE.md` at the repo root | the audit rules every `.cs` file here is held to |
