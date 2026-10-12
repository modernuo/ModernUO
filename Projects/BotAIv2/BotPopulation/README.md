# Population: the bot, and the beat

The object six subsystems were waiting for, and the clock that drives them. **This is the folder from which the
project first does anything at all.**

| File | What is in it |
|---|---|
| `BotMobile.cs` | the bot itself: a `PlayerMobile` that holds the bond, the journey and the resolve, and can take one turn; its re-arm wears the best of each place and wields the better weapon of its own kind, keeping the bound one in the pack (build 55), never the same weapon twice within five minutes (build 59) |
| `BotBeat.cs` | the population's clock: one timer, each bot with its own due time |
| `BotPopulation.cs` | who exists: purging saved bots, birth, placement, raising the fallen |
| `BotPopulationConfig.cs` | `Configuration/bot-population.json` — the only configuration file with working values in it |
| `BotPopulationModule.cs` | module, phase `World`, requires `Classes`, `Movement`, `Will` |
| `BotHomeward.cs` | walking back to where the population lives, when there is nothing else to do and the bot is a long way from it |
| `BotGuilds.cs` | which of the four guilds a bot belongs to, by its trade, and what a guildmate is worth |
| `BotCharter.cs` | what a guild's maker has charged its band with: a trade to gather at, one to make at, and somewhere to be |
| `BotRoster.cs` | who a guild's leader may put out and who it may take in, and how often |
| `BotProgress.cs` | what a bot has become, kept across restarts: its skills, its fame, its karma and its savings; kept by name and class since build 65, so a class-mix edit no longer wipes a bot's levelling |
| `BotPurse.cs` | what a bot keeps in its pocket, and what it puts away the moment it is standing somewhere it can; `KeepBack`, the one reserve the buying proposers keep (build 64) |
| `BotTidy.cs` | what a bot carries and wears kept in order on its beat: an unidentified magic weapon or armour identified with Item Identification's check (every bot has it at 100) — off a corpse before it is listed, and in the pack or worn, and the death robe the engine dresses a risen body in cut into bandages with scissors or thrown away (build 98) |
| `BotReclaim.cs` | going back for what death took |
| `BotStall.cs` | notices a bot that has stopped getting anywhere, and says so as an error |
| `BotUnload.cs` | going to the counter when the pack is getting heavy: coin into the account, everything spare into the bank box |
| `BotSurplus.cs` | a bank box kept in order: stacks of one kind lying apart joined (the engine's `StackWith`), a miner's bars put onto the box's stack of the metal; and where a capped kind's surplus went over the last hour, by kind: box, shopkeeper, a bot, the ground, refused by a full box, no box and no buyer (06.10.2026, box16) |
| `BotGrowth.cs` | Newcomers: a couple of novices raised every two hours, so the island has old hands and beginners at once. Since 29.09.2026 a guild of `LeastToWant` (3) bots with nobody of a role gets the next newcomer in that role — a producer first, a medic second — instead of a roll of the mix (`BotGuilds.Lacking`). |
| `BotGrowthStore.cs` | Keeps the newcomers across restarts. |
| `BotBirth.cs` | A newborn's temperament (01.10.2026, Patrick's point 7): the birth's own points leaned towards one skill of the class's own (`BotClass.Leanings`), seeded by name and class; a smith is born at the first coloured metal's floor (dull copper, 65, read off the engine). The birth line per bot and `births:` in `Rest:`. |
| `BotGuildWidenStore.cs` | Keeps the guilds' widenings across restarts. |
| `BotGuildSplitStore.cs` | Keeps across restarts who left which guild and when (the walk-out bar), and the clocks of the splits: five of a full guild founding a new one (`BotGuilds.Hive`, 06.10.2026). |
| `BotRest.cs` | How long a bot plays before it is tired, and the rest it takes before it plays again. |
| `BotRestStore.cs` | Keeps every bot's play and rest across restarts. |
| `BotRestConfig.cs` | what `bot-rest.json` may say (02.10.2026): the rest clock's own dials, which had no file before, the prime time schedule (`PrimeTime`) and the critical floors (`Critical.Floors`) |
| `BotPrime.cs` | prime time (02.10.2026, Patrick): bands of the day, each a share of the island to be in the world — 00–08 100 %, 08–16 75 %, 16–24 50 %, local time — read by `BotRest.Look` as the floor in both directions: over it the ones past their hours go, under it the resting come back whatever they owed; the target ramps over `RampMinutes` (30) at a band's start and `TirePerLook`/`ReturnPerLook` (5) cap a look; `FloorShare` only when it is off |
| `BotCritical.cs` | the posts the island is never left without (02.10.2026): a floor per class (Healer, Crafter, Gatherer, Mage, Trader, Captain, Baron) or trade by best skill (Smith, Tailor, Fletcher among crafters; Miner, Woodsman among gatherers); a bot past its hours whose post would fall under its floor plays on, a post under its floor brings its own back from rest up to `EarlyMinutes` (480) early, and a post the whole island lacks gets the next newcomer in its class |
| `BotInns.cs` | the inns and taverns read from the world; a tired bot walks to one, pays for a bed and leaves from there; a paid night buys a third of the rest as regeneration (29.09.2026) |
| `BotPeoples.cs` | who a bot is born as: three peoples and two hundred names from `bot-names.json`, the people drawn for the class, the race before the body; see `NAMES.md` |

---

## Why `PlayerMobile` and not `BaseCreature`

Because **this project's entire measure of work rests on that choice.** Inheriting from `PlayerMobile` gives
every player system without reimplementation: skill gain from use, fame and karma, a bank, criminal flags, a
lootable corpse. Skill gain is the important one: the decision layer appraises work by the skill it produced, and
here the skill is raised by the **engine**, after its own check. On a `BaseCreature` there would be nothing to
measure and the metric would have to be invented — and an invented metric is the first thing an agent breaks.

The price: a `PlayerMobile` has no think loop, so `BotBeat` provides the turn. And one more consequence carries
weight: nothing has a `NetState`, and `Mobile.Move` skips movement throttling when `NetState == null` — so a
bot's pace is set **only** by how often it is asked.

---

## One turn is three calls

```csharp
BotWill.Decide(this);
var result = BotWalk.Advance(this, Journey, Runs);
BotWill.Note(this, result);
```

The order is the whole contract between the subsystems. Decide first, because a decision may put a new
destination at the bottom of the journey. Then one step towards whatever is on the journey now. Then report
**what that step did**: two of its outcomes — "proved there is no way" and "not moving from this spot" — are facts
the decision layer has no other way of seeing, and caution about places is built out of them.

---

## The beat cannot be slower than the step

This is the tick's one measured defect in v1, and here it is closed by construction rather than by care.

There the period was `interval × phases` — two numbers in a configuration file whose product nobody checked — and
it drifted to 800 ms against a 400 ms step. The bots were not stuck, and nothing in the log looked wrong: they
simply walked at half a pedestrian's pace for a whole session, because you cannot step more often than you are
asked.

Here a bot's next due time is set from `BotWalk.StepDelayMs` at the moment its turn is handed out. **The product
that could be spoiled does not exist.** The timer's interval (100 ms) is only the schedule's resolution: how
precisely due times are honoured. The spread is not configurable either — bots are born with due times laid out
one step apart, so the step's work falls across the beats evenly. That was what "phases" were for, and this way
it needs no configuration and cannot be set to a value that strangles movement.

---

## Who is born, and where

The population is **rebuilt from configuration on every world load** rather than restored from the save. Not out
of laziness: a bot's state lives in objects — the bond, the journey, the ledger of takings — and none of them is
worth a save format. Loading a saved bot would mean building all of that again anyway, and the only thing that
would come back intact is its pack, so issuing the kit a second time would produce a bot with two of everything.
So bots arriving from a save are deleted.

`PurgeSaved` is **the only place in the assembly that walks the whole world**, and that is deliberate: the
shard's rules forbid enumerating `World.Mobiles` in favour of spatial queries, but there is no query for
"everywhere", this happens once per world load, and the alternative is a population that doubles on every
restart.

The birthplace is Britain — and specifically the point the **shard's own location list**
(`Data/Locations/felucca.json`) calls Britain, not a coordinate somebody remembers. That matters more than it
looks: the first bot that wants work sweeps the ground around itself for veins, fires and counters — so the
birthplace decides what the population is able to do at all.

What puts a bot on the ground is the engine rather than a guess: `Map.CanSpawnMobile`, the same test the shard's
own spawners use, including the region's opinion and a search for a floor within a couple of units of height. A
bot placed inside a wall is a bot whose first act is to prove there is no way out.

---

## The starting set — four bots

Two gatherers, a crafter and a warrior. The first three are born with a pickaxe (and the pickaxe is what decides
who can dig). **The warrior is there precisely because it has nothing to do:** it is living proof that "nothing
was worth doing" in the census is a fact about the world rather than a broken bot.

This is the only configuration file in the project written with working values rather than empty ones: elsewhere
"empty" means "keep the number the code has", and here an empty class list means "there are no bots at all".

---

## What a bot must be told rather than notice

A bot learns it has been hit from `OnDamage`, not by looking. A caster strikes from eight tiles and never enters
contact, so any check of the form "is there something next to me" never fires — in v1 six bots stood in a ring
while a lich killed them one at a time, and not one rung of their ladder ever triggered.

`BotThreat` then decides, and the decision is binary: put the journey aside and deal with it, or walk on and hit
back as you go. Standing still is not allowed at any numbers. The target is **the strongest** within range, not
whoever hit last: in a graveyard the nearest thing is always a skeleton, and v1's parties reliably went for the
skeleton while the lich kept casting.

The interruption is queued **once per target**: otherwise every blow would add another errand, and four blows
would fill the queue with the same fight four times over.

---

## Death and rising

Death: `BotBinding.TrimAmmunition` (the corpse is the only place where the ammunition count can still be
trimmed), then `BotWill.Died` counts how the work ended, then leaving the squad and clearing the journey.

Rising: a minute later `BotPopulation.Revive` brings the bot home and raises it — home first, because a ghost
raised where it was killed is a bot standing inside whatever killed it. The kit comes back in
`OnAfterResurrect`.

Somebody has to do this: nothing else in the project resurrects anybody, and a dead bot is a ghost for the rest
of the shard's life. The minute is not decoration — death has to cost something, and to a bot it costs time. The
decision layer counts the same price in its own units (`BotYield.DeathMinutes`); here it is in wall-clock.

---

## What to check with a client

1. `Population raised: 4 bots at (1592, 1680, 10) on Felucca; …` — and beside it the clock's line with the
   interval and the step.
2. Walk into Britain and look at them: dressed, carrying a pickaxe, moving. If they are standing still, look for
   `took on` in the log: either they took no work (and the reason is written there) or they took some and cannot
   reach it.
3. `finished mine: … N ingots put away` — the chain reached the end. That is the first real proof that all the
   subsystems are working at once.
4. Kill one and wait a minute: it should get up and come home with its kit.
5. `bots.population.enabled = false` — the shard becomes exactly what it was before this folder: subsystems
   loaded and nobody to use them. The cleanest A/B for "is this the bots or is this the shard".

---

## Rest at an inn, and what a paid night buys (29.09.2026)

A tired bot (`BotRest`) no longer logs out where it stands. After the walk home to a house of its own
(`BotRepose.HomeToRest`), `BotInns.ToInn` presses `BotLodge` on it: the walk to the nearest inn or tavern within
`InnReach` (400) that the gates join it to. The inns are read from the world twice over — the engine's `NoLogoutDelay`
regions, one per town, and the spawners of `TavernKeeper` and `Innkeeper` — and never written down. At the door the bed
is paid for at `InnPricePerHour` (5) from the purse and then the bank; `BotRest.Leave` writes the inn and the price into
the rest's record (store shape 4). On return a paid night gives `WellRestedUntil` — `InnBuffShare` (a third) of the rest —
during which `BotInns.Regen` on the bot's beat returns 2 points of hits, stamina and mana every 4 s on top of the engine's
own rate, and 200 of cheer comes off the day's boredom, so the mood the dashboard shows rises. A bot that cannot pay
sleeps under the roof and gets nothing for it; one with no inn within reach leaves where it stands and is counted
(`LeftOutside`). Four keys in `bot-population.json`: `Inns`, `InnReach`, `InnPricePerHour`, `InnBuffShare`.

## The peoples: who a bot is born as (29.09.2026)

`BotPeoples` reads `Configuration/bot-names.json` — written from the bank in the code at first boot — and the population
deals a newborn's people for its class (share × the class's weight in the people), its sex by a coin, and its name as the
next unworn one of that people and sex, a house past the end and never a number. `BotMobile.Become` sets the race before
the body (`Race.Human`, `Race.Elf`), the race's own skin and hair hues, and a beard where the people wears one. Which
people a bot is, is read off its name, so nothing new is saved. `NAMES.md` beside this file is the settings document.

## Known rough edges

**A pack is full by count as well as by weight.** The porter sends a bot to the counter when its pack is heavy, rich,
wanted, worth losing or hoarding — and, since 14.09.2026, when it has no room for a coin (`BotYield.Pocket`). A pack at
the engine's cap of 125 things cannot pay or be paid at a counter, so every trade that does either is refused to it,
and the trip is offered unpaid, as it is to a bot too heavy to walk. Loot, spoils and deliveries off the board still
go into a pack without asking it, which is how a pack gets there. The `Will:` line counts the refusals and the offers
of the trip; both are counted once a review while the state lasts, so they measure how long it lasted, not how many
bots were in it.

**Surplus of a capped kind goes to the box (06.10.2026).** Two more reasons for the trip: units of a kind the bot keeps a
number of (draughts, bandages, reagents, tools) more than `KitSlack` over that number, on the way when a counter is within
`KitNear` tiles and on its own once it has ridden `KitWaitMs`; and standing within `BankBy` tiles of a banker with anything
over the allowance. A want near keeps its goods in the pack for no longer than `HoldMs`. Both are made false by the trip,
and what a full box cannot take is not counted, so a full box cannot send a bot back. A full box (`BotPeddler.FullShare`)
lets its own capped kinds over one refill, and what the island asks for, go to a shopkeeper. The `Packs:` line names the
bots over the allowance in capped kinds, by units, the worst pack, and the full bank boxes with what fills them;
`/api/state` carries `over` and `boxItems` per bot.

**Nobody musters a squad.** `BotSquads.Form` is still called by nothing, so formation, scouting and sharing are
written and do not run. That is the next proposer, not a defect.

**Clothes are bound.** Not for looks: an unbound shirt is lootable, and a population that has died twice stands
around naked.

**The `Failing` rung is served** as of `BotMend/` — a bot whose health is going now mends itself above all other
work, spell first if it can cast and cloth if it cannot, and drinks a bottle below forty per cent.

**The engine surface was read out of the fork:** `PlayerMobile(Serial)` and the parameterless constructor,
`Serialize/Deserialize` (the fork is on the legacy pattern, not the source generator),
`OnDamage/OnDeath/OnAfterResurrect/OnAfterDelete`, `Mobile.Resurrect`, `MoveToWorld`, `Map.CanSpawnMobile` (the
overload that searches for Z), `Map.TryParse`, `World.Mobiles`,
`Race.RandomSkinHue/RandomHair/RandomHairHue`, `Utility.RandomDyedHue/RandomMinMax/Random`, `StatCap`,
`RawStr/RawDex/RawInt`, `HairItemID/HairHue`, `Warmode`, `Combatant` (a `Mobile`), `Female`, `Body`.
