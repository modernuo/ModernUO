# UOBot — the long read

Everything about the population that is not the quick start: what each kind of work is, who the bots are, how one
of them decides, how guilds, wars and crime work, where the money comes from, and how to run, watch and tune a
shard. The quick start and the short version are in `README.md`; installing into a fork of your own is in
`INSTALL.md`.

---

## What a bot can do

Fifty-nine kinds of work. These are the names that appear in the log.

**Getting raw material**

| work | what it is |
|---|---|
| `mine` | walk to a seam, dig it, smelt the ore into ingots, put the metal away |
| `chop` | fell a tree for logs — and the axe has to be *worn*, which is why nothing was ever cut until that was found |
| `herbs` | pick reagents out of the grass |
| `forage` · `prospect` | anything else worth lifting off the ground; a walk past the last swept ground to find rock |
| `pickings` · `glean` · `plunder` | go through a corpse, pick spent ammunition up, go through a chest standing in the world |

**Making things**

| work | out of | into |
|---|---|---|
| `forge` | ingots of any of eight metals, at a forge with an anvil | weapons and armour |
| `sew` | leather off a carcass, or cloth off a counter | armour and clothing |
| `brew` | a reagent and an empty bottle | potions |
| `fletch` | shafts and feathers | arrows and bolts |
| `tinker` | iron ingots, anywhere the bot stands | the tools every other trade wears out: pickaxes, hatchets, hammers, sewing kits, scissors, skinning knives, skillets, mortars, pens, fletcher's and tinker's tools — made to the board's orders first, then whenever the stalls hold fewer than two of one |
| `cook` | raw meat off a kill, at any fire | meals, which are eaten and quicken recovery for ten minutes |
| `inscribe` | a blank scroll and reagents | spell scrolls, which no shopkeeper on this shard sells |

**Trading**

| work | what it is |
|---|---|
| `peddle` | carry goods to an NPC counter and sell them |
| `restock` | buy from a counter, a stall or the guild's own merchant: reagents, bandages, cloth, bottles, blank scrolls |
| `order` | put money down on the bots' own board for something a bot cannot make itself |
| `acquire` | get a spell the book is short of, off a shelf, a stall or the board |
| `unload` | take a full pack to a counter: coin into the account, everything spare onto a stall |

**Fighting and surviving**

| work | what it is |
|---|---|
| `prowl` | look for something worth fighting |
| `hunt` | close with a chosen creature, kill it, carve it, go through it |
| `band` | call a company together for something one bot cannot take |
| `rescue` | go to somebody who cried for help, or hit back at what is hitting you |
| `mend` | bandages and heals: your own wounds first, somebody else's as ordinary work |
| `escort` | a healer standing by one of ours who is fighting — paid a wage by the minute when the fighter has hired it |
| `housecall` | a healer with nobody to mend walking to where our fighters are |
| `flee` | break off and run, which is what the `Failing` rung offers |
| `sweep` · `enlist` | a captain's company patrolling the worst square on the island, and falling in with a company already fighting |
| `liberate` | get a prisoner out of a camp and home |

**The guild**

| work | what it is |
|---|---|
| `hall` · `fit` · `hire` | raise the guild's hall with a levy, buy it a workbench, hire it a merchant |
| `supply` | fetch a batch of what the members keep running out of and leave it on the guild's counter |
| `stake` | stand in a square the guild is claiming, for as long as the claim takes |
| `evict` | walk over to somebody working the guild's land and tell them to move along |
| `rally` · `quarrel` | fall in with the guild's war company; close with a member of the guild it is at war with |
| `remove` | carry a beaten guild's hall out of the winner's yard |

**Crime and the law**

| work | what it is |
|---|---|
| `rob` | set on one of our own for what is in its pack — sometimes demanding a share of the purse first — and go red for it |
| `skulk` | practise hiding, and once hidden well enough, moving a few quiet steps at a time |
| `waylay` | a thief that has found nobody to rob, going to where the work is and waiting there |
| `lielow` | a murderer getting away from the body: a dozen tiles off, hidden, not moving, for a few minutes |
| `stash` · `fetch` | carry the takings to the band's chest at the hideout; the fence's run out to that chest for the goods |
| `silence` | the fence's chase after a witness it could not buy off |
| `holeup` | the fence going to ground at the hideout while a price stands on its head |
| `manhunt` | the Baron's patrol, or the law-abiding pressed into one, set on a murderer |
| `raid` | the Baron and a posse marching on The Shadow's hideout once a caught thief has given it away |
| `sentence` | sitting in a cell until the clock lets the bot go |

**Leading, teaching, errands and the island itself**

| work | what it is |
|---|---|
| `drill` · `drill-in` | a captain or a sage holds a class; a bot pays the fee and attends one |
| `errand` | an errand off the board: kill what it names, bring what it asks, or stand where it points |
| `scout` | walk into a square nobody has ever stood in and write down what is there |
| `harrow` · `stroll` | the Baron marches a levy at ground that has killed people; walks his own town |
| `delve` | the head of a guild takes five bots down a dungeon for twenty minutes or twenty corpses |
| `homeward` · `reclaim` | walk back to where the bot lives; go back for what death took |

Some things are not work at all but conditions checked on every beat, because they take no journey and would lose
every auction they entered: eating a meal, banking above a threshold, putting on better armour, taking the bow back
up after a fight, identifying what it picked up.

---

## The population

Thirteen classes. A class here is **a description and a set of limits with no behaviour** — it decides nothing and
commands nobody, and `BotWill` reads it the way it reads the map. The mix shipped in `bot-population.json`:

| class | what it is | of 80 |
|---|---|---|
| `Warrior` | the plain fighter | 15 |
| `Archer` | the bow and nothing else, and the only class that can triple a hit | 13 |
| `Gatherer` | ore and timber, and the only bot that can find a reagent in the grass | 12 |
| `Mage` | a spellbook, a blue staff, and no metal | 10 |
| `WarriorMage` | plate, a blade, and spells anyway | 8 |
| `WarriorArcher` | shoots, and has a knife for when that stops working | 8 |
| `Crafter` | metal, cloth and leather | 5 |
| `Healer` | the green staff; fights only what has laid hands on it | 4 |
| `Brawler` | fights with its hands, and is therefore never holding anything it has to put down | 1 |
| `Captain` | the one bot that exists for the others: holds a training field and leads patrols | 1 |
| `Baron` | the one bot that is not trying to make a living: harrows, hunts murderers, raids the hideout, pays a stipend | 1 |
| `Architect` | paid a hundredth of every sale on the market, so it is paid by the health of the market | 1 |
| `Sage` | the captain's opposite number, teaching the half of the population a captain cannot | 1 |

Every bot starts with 400gp and roams within a thousand tiles of home. A bot in a guild is born — and raised again
after it dies — at its guild's seat; `Home` in `bot-population.json` is where a bot with no guild lives.

**Bots are ordinary characters in the world save.** A restart brings each one back where it stood, with its pack,
its bank and its skills; a bot that was dead at the moment of the save is raised by the reviver. Skills, fame and
karma are also kept by name in `Saves/BotProgress`, for a bot that has to be made again.

---

## How a bot decides

Three stages, every turn, in `BotWill`:

1. **The ladder.** Which rung the bot is on, from facts alone: `Failing` (hurt, fleeing), `Hunted`, `Bound`
   (charged by a company), `Busy`, `Free`. A rung decides which work is even offered.
2. **Obligations.** Anything already promised is taken before anything is auctioned.
3. **The auction.** Every proposer registered by every subsystem is asked whether it has work for this bot. Each
   offer is priced **per minute** and the best wins. The losing runner-up is printed beside it, so the log always
   says what a choice was made *against*.

The prices are not weights. Every trade opens at a guess and `BotLedger` corrects it by what the work actually paid —
so a trade that stops paying stops being chosen, without anybody editing a number. A representative line:

```
Doran took on sew: after Leather: 135/min = 99 × 1.05, that being the fifth root of near 1.00 × new 1.00 ×
room 1.00 × safe 1.00 × purse 1.00 × load 1.00 × revel 1.00 × ground 1.25 × charter 1.00; × 1.30 for its own
trade; 3 of 4 offers worth anything
```

99 per minute is what the ledger has learned sewing pays; the factors after it are nearness, novelty, room in the
pack, safety of the ground, what is in the purse and so on, and 1.25 is the bonus for working the guild's own land;
× 1.30 is for training one of the bot's own class skills. **Every factor has a floor**, because a multiplier that can
reach zero is a veto — a lesson this project paid for when an empty purse silently forbade a bot from looking for
work at all.

**What the population has learned is shared and kept.** `BotCommons` is the board the auction reads when a bot has
never worked a place — what each trade pays in each patch of ground — and it survives a restart in
`Saves/BotCommons`, so a deploy is not an hour of relearning.

**A choice is kept once it is made.** Work a bot sees through — mining, woodcutting, cooking, carrying goods to a
counter, getting a spell, standing for the guild, a healer standing by a fighter — is held against better offers
for as long as it reckoned it would take. Only events get through: something that will not wait, a call from
outside the bot's own business (a comrade in trouble, the guild's muster, a war company, a paid lesson), or trouble
in the work itself. When an event does take a bot off steadfast work, the work is put down and taken up again
afterwards rather than thrown away. This follows the oldest experiment on the question, Kinny and Georgeff's: an
agent that reconsiders at every new opportunity does worse than one that never reconsiders, and one that commits
but reacts to the right events beats both. Mining went from 44% of trips finished to 74%, and from 37% abandoned to
5%.

**A failure is not offered again unchanged.** Work one bot keeps failing for the same reason — six times in five
minutes — is not offered to that bot for a while (`BotBreaker`). It was written after thirty-five separate loops of
that shape had each been found by its symptom.

**A class shows in what it picks.** Work that trains one of the bot's own class skills is worth a third more, work
that trains another class's skill a little over half, and the rest — selling, carrying, standing for the guild — is
neutral. A healer does not go looking for fights. The reasoning, the measurements and what is still missing are in
`RESEARCH-decisions.md`.

---

## Guilds, land and war

The rules are in `GUILDS.md`; the wars and seats are in `PLAN-wars-and-seats.md`. In short:

- **Anybody founds a guild.** Every five seconds one bot without a guild joins the smallest guild under ten members,
  or, if there is none, founds its own with the four nearest bots that have none, up to eight guilds and fifteen
  members each. The five single-seat classes — Captain, Baron, Architect, Sage, Brawler — stay outside. A guild is
  a real engine guild with a guildstone, so it survives a restart; a member unhappy with its guild may go over to
  one that is doing better.
- **A seat and a hall.** Each guild lives around its seat, where its members are born and raised again. A hall is
  raised by a levy of 5,000gp on the members, fitted with the tools of the guild's trade and a merchant, and read
  back out of the world by the name on its sign at the next start.
- **Land** is claimed a thirty-tile square at a time, by members standing in it. Working somebody else's land sours
  one guild's opinion of the other, and trading or fighting beside each other improves it. Hunting on ground a guild
  holds pays that guild a tithe into its **chest**, and the chest pays the next claim or hall before any member's
  pack is asked. When a square the guild holds turns deadly, the guild raises its own company onto it.
- **War** starts when one guild's opinion of another falls below -100, and ends by rules rather than by exhaustion:
  twenty-five dead or 5,000gp of plunder wins it, there is no peace inside fifteen minutes, the clock judges it at
  three hours, a day's truce follows, a guild may declare once a day and fight one war at a time, and an enemy on the
  guild's own ground is answered from 250 tiles before anything else. Members fall in with the guild's war company.
  The loser's hall is carried outside the winner's yard.
- **All of it survives a restart**: wars, truces, clocks and opinions in `Saves/BotWars`, claims in
  `Saves/BotClaims`, seats in `Saves/BotSeats`, chests in `Saves/BotChests`.

**Dungeons.** The dungeon block has no road from the island, so a party is put down and lifted back out; which
dungeon is chosen by measuring what lives in it against what the band is worth, and what the party takes is swept
into one pot and divided at the end, half to the leader.

---

## Crime and the law

The thought of robbing is a rare one, and it costs.

- **Red.** A bot that kills another of ours goes red by the engine's own notoriety, for an hour: friendly to other
  reds, an enemy to everybody else, and kept out of the guarded towns. The record of who murdered whom is kept by
  name in `Saves/BotUnderworld`.
- **The Shadow** is the thieves' guild. It is founded once two bots have killed from hiding, led by the better of them
  at hiding, and takes in at most five. It has a hideout far out of town, with a fire and a chest that only the band
  opens, where its members rise and hide. Its business is robbery, blackmail and the ambush out of hiding — a blow
  worth three that leaves the victim stunned; a victim beaten down to a quarter of its health fights on, runs for
  the town, or throws its gold. A **fence** — a Sage or an Architect, the one member who commits no crime — keeps the
  band's goods and can walk into any town; seen in the band's company it turns criminal, and a witness it cannot buy
  off it has to catch.
- **The law.** The body of a murdered bot is found: the finder searches for the killer with Detect Hidden, calls
  out, and tells the Baron. The Baron raises a patrol against a red, the law-abiding set on one when they can take it
  between them, and the city puts prices on heads. Once a caught thief gives the hideout away, the Baron raids it.
- **The cell.** A red that the patrol kills is caught: stripped at the cell door of its purse and its gear, fined
  from its bank — both into the city's treasury — and put in one of the engine's own jail cells for its sentence,
  after which it is lifted back to its guild's seat with a clean name.

`BotHunt/README.md` has the whole of it, including what a catch costs and what the fence keeps.

---

## The city, errands and the championship

- **The city's treasury** is the one purse on the shard that mints coin: 3,000gp an hour, up to 20,000. Everything
  the city does is paid from it — buying whole lots off the stalls that have stood longest, standing orders that take
  a thing off the stalls whenever it is on offer at or under a price, bounties on squares and prices on heads, and a
  fair of its own every six hours when the purse can carry one. A purchase it cannot pay for is refused and counted.
- **The board of errands** holds up to twelve paid errands at once: kill so many of a creature near a place, bring so
  many of a thing to a place, or scout a place. The watchers post them and the city pays; the reward is set aside the
  moment an errand is posted, so nothing is posted that cannot be paid, and a lapsed errand gives the money back.
  Any bot fit for an errand may take it, and it is paid on the spot when it is done. The board survives a restart in
  `Saves/BotQuests`.
- **The championship.** Once a week Argus calls the strongest thirty to a ring and sets them against each other one
  pair at a time, healing them between fights. A duel ends when either fighter is beaten down or the clock runs out —
  nobody dies for a title. The champion's name turns yellow and it is handed a weapon or a piece of armour fit for
  its class. `do tourney` through the door holds one whenever somebody wants to see it.

---

## The economy

There is no shopkeeper handing out an allowance and no spawner dropping gold on the ground. Every coin in the
population's hands came in one of three ways.

**Where gold comes from**

- **Kills.** A creature's purse is new money, which is why `prowl` and `hunt` together are so much of what the
  fighters do.
- **Selling to an NPC.** A shopkeeper's own money enters the world when a bot sells it something — `peddle`, and it
  is what the raw end of every gathering trade is worth when nobody else wants the stuff.
- **The city.** Its treasury mints coin and pays for errands, bounties, standing orders and the lots it buys off the
  stalls. What is seized from a caught red goes back into it.

**Where gold goes**

- **Buying from an NPC.** Reagents, bandages, cloth, bottles, blank scrolls — `restock`; and a horse, which is the
  single largest purchase a bot makes.
- **Guild halls.** The levy for a hall, its workbenches and its merchant's wages.

**What circulates between them** is the bots' own market: stalls and wants, both sides run by bots, with a hundredth
of every settled sale paid to the Architect.

- A **stall** is a standing offer: one kind of thing, a quantity, a price, and what that price has learned.
- A **want** is money already down for something the bot cannot make itself. A want is what turns speculative
  crafting into filled orders.
- Prices move on evidence. A stall that sits unsold is cut; one that empties fast is raised; a want that goes
  unfilled bids up.
- A guild's **counter** in its hall sells to its members what the guild's couriers bought in town, so nine members
  do not each walk to Britain for it.
- A fighter with money **hires a healer** to stand by it and pays a wage out of its pack for every minute.

**The chains that close.** Each is a sequence in which every step is a separate bot acting for its own reasons:

```
kill → carve → hides → scissors → leather → stall → tailor's want → leather armour → worn
seam → dig → smelt → ingots → stall → smith's want → weapon → filled order → carried
kill → carve → raw meat → kept back → fire → cooked meal → eaten → recovers twice as fast
grass → reagent → stall → alchemist's want → potion → drunk in a fight
NPC counter → blank scroll → scribe → spell scroll → market → mage's book
```

**Money has weight, and a bot pays out of its pocket.** The engine takes payment from the pack, not the account,
and a bot only walks to a bank above a threshold. That interaction once produced money that existed and could not
be spent.

---

## Running a shard

After the first start from a console (see `README.md`), `START_SHARD.bat` at the root of this repository does the
rest on Windows. Copy it to the root of the ModernUO checkout; it expects `Distribution\ModernUO.exe` beside it.

```bat
START_SHARD.bat
```

Double-click it, or run `START_SHARD.bat --nopause` from a script. It starts the shard through a scheduled task with
no triggers, so the shard outlives the window or app that started it, and waits until it reports listening on
`127.0.0.1:2593`. Its whole console goes to `logs/session-<yyyy-MM-dd_HH-mm>.log`, one file per start. Nothing starts
the shard on its own: after a reboot, run it again.

Stop it by saving first — write `do save` into `Distribution/argus-in.txt` and wait for the answer in
`Distribution/argus-out.txt`, or type `[Save` in game — and then:

```bash
taskkill /F /IM ModernUO.exe
```

A kill without the save rolls the world back to the last autosave, five minutes at most.

**A running shard holds the assemblies.** Building while it runs still compiles; it fails only at the copy into
`Distribution/Assemblies`. `dotnet build … | grep "error CS"` is therefore a valid syntax check for `BotAIv2`
against a live shard — but not for `BotMindAI`, which MSBuild skips after its dependency's copy fails. Stop the shard
before trusting a build that touched `mindedBots/`.

`bash shard-status.sh` prints one screen about a running shard: wars with their score and clock, blood, halls and
seats, work and commitment, and the newest alarms.

---

## Watching it

**The web dashboard.** `http://127.0.0.1:2599/` while the shard runs: every bot on a picture of the map with its
route and target, the work finished share against 95 %, the paths reached share against 99 %, who is stuck and why,
the guilds and their wars, the crafting chain, and a live feed of everything the bots say and do. It is one page,
`Distribution/Data/bot-web/index.html`, over a JSON API (`/api/state`, `/api/events`, `/api/stream`, `/api/paths`,
`/api/craft`, `/api/history`, `/api/log`). Nothing on it can change the shard. See `BotWeb/README.md`.

**The event stream and the transcript.** `logs/bot-events.ndjson` is one JSON line per thing that happened — work
taken up and ended with the reason, a search that did not reach its goal with where it was headed, a death, a war,
an alarm, a line said — and `logs/bot-speech.log` is what the bots said, by channel. Both are appended across
restarts.

**The dashboard in game.** `[bots` as an administrator: eleven tabs — the population, their market, what they are
short of, the city, what they have learned, the island's squares, revels, halls, claims, guilds, and the band.

**The summary.** Every five minutes the population writes a block of lines, each one a complete accounting of one
mechanism with no bucket called "other":

```
Resolve: 1150 of 1492 endings finished (77%), 225 failed, 117 dropped, 0 died; of the drops 15 for
something that would not wait, 13 for a call from outside, 47 for a rung above, 0 for a full pack
and 55 outbid; 35 better offers refused inside a hold and 4 holds lifted for trouble; 13 put down,
13 taken up again and 0 not; …
```

Every number there is a static counter on one class, and `MAP.md` §1 maps each line prefix to the file that
assembles it. A hard zero next to a healthy denominator is the shape most defects here take. **They are running
totals since the shard started, not figures for the last five minutes**: a rate is the difference between two
summaries over the time between them, and two runs compare only at the same age.

**Per-bot lines.** `took on`, `finished`, `failed at`, `dropped` — with the reason in the deed's own words, and on a
drop the numbers that decided it: how far into its own reckoning the work was, what it was taken at and what it was
worth by then. `put down … to take up again` and `took up … again` are an interruption and its end.

**The alarm channel.** `logs/alerts.ndjson` — the shard raises its own alarms there, one JSON line each, raised once,
repeated at most every fifteen minutes, cleared in their own event, with an hourly heartbeat so silence can be
trusted. Wars are announced there too, and a boot that loads far fewer mobiles than the best boot on record raises
`world`: the save was probably cut short.

**The door.** Write a line into `Distribution/argus-in.txt` and the answer appears in `Distribution/argus-out.txt`
within a couple of seconds — no client, no character, no login. It knows `state`, `bot <name>`, `idle`, `trades`,
`fighting`, `rollcall`, `memory`, `note <text>`, `think <question>`, `hands`, `do <command>`, `dials <word>` and
`dial <Class.Name> <value>`. The last two find a number and change it on the running shard; every change is
journalled to `logs/bot-dials.log` and lost on restart.

**Believe the shard before the watcher.** Five false alarms in one day were all artefacts of the instrument.

---

## Configuration

One file per subsystem in `Distribution/Configuration/`:

```
bot-alarm     bot-auction   bot-baron     bot-classes   bot-craft      bot-debugger   bot-delve
bot-drill     bot-estate    bot-harvest   bot-hunt      bot-mend       bot-mind       bot-movement
bot-names     bot-population  bot-proving bot-shops    bot-spells     bot-squad      bot-travel
bot-voice     bot-web       bot-will
```

`bot-names.json` is the bank of names and peoples the bots are born from — see `BotPopulation/NAMES.md`;
`bot-travel.json` is the journeys between towns; the inns are four keys in `bot-population.json` (`Inns`, `InnReach`,
`InnPricePerHour`, `InnBuffShare`); the guilds' seats and the share that settle by another town are in
`bot-estate.json` (`Seats`, `SeatAbroadShare`).

and two that the shard writes for itself: `bot-minds.json` (the rules the thinking bots have written) and
`bot-dungeon-halls.json` (which rooms of each dungeon a party has found it can walk between).

Any file that does not exist writes itself on first boot. The whole thing goes off with `"bots.enabled": "False"`
in `modernuo.json`; the per-module switches beside it are there for diagnosis, because halving the number of running
modules is faster than reading a long log.

**Keys are PascalCase, and a wrong key is silent.** A key in lower case is not an error and not a warning — the value
simply stays at its default. The only proof of what a dial is actually set to is the line its module writes at boot.

`DIALS.md` lists every tunable in the assemblies with its default and, where one exists, the configuration key that
overrides it. Most can also be changed on a running shard through the door.

---

## The thinking layer

**Nothing in the population thinks by default.** Every bot chooses by the auction. What thinks is Argus's squad of
watchers, and only if [Ollama](https://ollama.com) is running; without it nothing breaks and the watchers go on
measuring.

**The watchers** are not part of the population. They take no work, join no auction and own nothing; they are
invisible to everybody below an administrator, cannot be hurt and cannot hurt anything. There are four: Argus,
who leads, and Lynceus, Heimdall and Hermes, each charged with a share of the shard — the work and the money;
getting about, the ground and the fighting; the wars, the guilds and the estate. Hermes is also the marshal of
events: revels, camps, tournaments, errands, standing orders, bounties and fairs.

- **Measure.** Every two seconds every bot is read: where it is, whether it moved, what it is doing, and for how
  long. Everything reported was measured on the squad's own clock, never taken from the bots' own counters.
- **Ask two questions** every ten minutes: *is anybody stuck*, and *is anybody doing something that produces
  nothing*.
- **Reflect** every half hour on their own recent findings, and **remember** what they have come to believe in
  `Distribution/Configuration/bot-debugger-memory.json`.
- **Answer a person** through the door, or anywhere in game: say "Hey Argus, …" and it goes into its log and its
  next report.

**The hands** are a bounded set, and `none` heads the list on purpose: do nothing, which is the right answer most
of the time. The rest read a bot, move it somewhere it can stand, raise it, free it from work it is holding, or
summon it to be watched. Nothing there deletes anything, sets a property, touches an account or an access level, or
acts on a mobile that is not one of ours. Every use is written to `logs/bot-debugger-commands.log`, apart from the
observations, so a hand cannot quietly alter what it is watching without the record showing it. `hands` through the
door lists the verbs and what each has been used for.

In game, `[argus` (or `[debugger`) takes you to Argus and `[argus here` brings it to you; Lynceus and Heimdall answer
to their own names the same way. Observations go to `logs/bot-debugger.log`.

**Thinking crafters** are optional. Name crafters in `Distribution/Configuration/bot-mind.json`
(`"CrafterNames": ["Roderic", "Emeric"]`) and those crafters let a local model choose their trade from a fixed list
instead of the auction. The choice still runs as an ordinary undertaking under the same auction and the same
commitment, and its forecast is measured rather than believed — so it can only win a place by being right. They
have been off since 18.09.2026. `mindedBots/README.md` describes the minds and the state they are shown; `INSTALL.md`
has the models to pull.

**Two protocols.** The transport (`BotOllama`) speaks either Ollama's own `/api/chat` with a `format` schema
(`"Api": "ollama"`, the default) or the OpenAI chat-completions API with `response_format` (`"Api": "openai"`), which
is what LM Studio, llama.cpp's server, vLLM, OpenRouter and the hosted services all answer to; an `ApiKey` goes as a
bearer token. The questions, the schemas and the reading of answers are the same either way — only the envelope
differs, and a hosted model is paid per token at the minds' asking rate. Every key, every default, the models
measured on this card, what the model is shown and how to write a prompt for a bot are in
[`mindedBots/LLM.md`](mindedBots/LLM.md).

---

## The documents

| read this | for |
|---|---|
| `MAP.md` | **where anything is.** A block per subsystem, every file with the one thing it decides, and the table that turns a summary line into the file that wrote it |
| `DIALS.md` | what every number is set to, and whether a config file can reach it |
| `DECISIONS.md` | the decision log: mechanisms and their invariants, the defect classes with every recorded case, and what was tried, measured and kept |
| `ARCHITECTURE.md` | how the whole thing is put together: the rules, the vocabulary, how to add work |
| `RESEARCH-decisions.md` | how game and agent brains decide — commitment, correctness, roles — and what this shard was measured doing |
| `GUILDS.md` | the guilds' manifesto and rules, and the model of a social circle behind them |
| `PLAN-wars-and-seats.md` · `PLAN-guild-lands.md` | wars with an end, guilds with a seat, and the land they claim |
| `INSTALL.md` · `BUILD.md` | installing it, building it, and what the boot log should say |
| `HANDOFF.md` | the state of the work, newest first |
| `<Subsystem>/README.md` | why each decision in that subsystem is the way it is |

The subsystem READMEs are written by hand — read them for reasoning, and `MAP.md` §2 and `DIALS.md`, which are
generated from the source by `regen-map.py`, for facts. The generator also reports any README whose file table has
drifted from its folder.

---

## Working order

Changes go into the fork — [DmitriyBol/ModernUO-fork](https://github.com/DmitriyBol/ModernUO-fork), in
`Projects/BotAIv2`. Shakedown happens on a live shard. What has actually run comes to
[DmitriyBol/UOBot](https://github.com/DmitriyBol/UOBot). The paths there mirror the paths in the fork, so moving work
either way is a copy of the tree.

**Every change is measured on the shard before it is believed.** The pattern this project keeps returning to is that
a mechanism which looks broken is usually two numbers that never met, and that the engine refuses in silence — it
answers a refusal by sending a message to a screen the bot has not got. `DECISIONS.md` §3 lists the shapes those
defects take.
