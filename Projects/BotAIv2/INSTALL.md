# BotAI — installation

Autonomous bots for [ModernUO](https://github.com/modernuo/ModernUO). Two assemblies, and the second one is
optional:

| | |
|---|---|
| **BotAIv2** | the population: trades, combat, market, squads, guilds, halls, land, wars, dungeons, crime and the law, errands, the quadrant map. No external dependencies. |
| **BotMindAI** | `mindedBots/` — Argus's squad of observers, and optional thinking crafters that choose with a local LLM instead of pure arithmetic. Needs [Ollama](https://ollama.com) to think; builds and runs without it. |

`BotMindAI` references `BotAIv2` and never the other way round. You can ship the first without the second; you
cannot ship the second alone.

> **Ten engine patches are required**, all in `engine-patches/`, and one folder of new engine code. From the root
> of the ModernUO checkout, before building:
>
> ```bash
> git apply Projects/BotAIv2/engine-patches/*.patch
> ```
>
> - `CraftItem-heat-source.patch` adds two accessors to `Projects/UOContent/Engines/Craft/Core/CraftItem.cs`,
>   exposing two questions the engine already answers privately — is this tile a fire, is this mobile standing near
>   one.
> - `CraftItem-said.patch` adds one event to the same file, raised wherever the craft system tells the crafter
>   something, so a bot can hear why an attempt produced nothing.
> - `DefInscription-knows.patch` adds a hook to `Projects/UOContent/Engines/Craft/DefInscription.cs` that a caller may
>   set to let a mobile write a spell it does not hold in a book. Left null, as the engine leaves it, the spell must
>   be in the scribe's own book, as before.
> - `HarvestDefinition-said.patch` adds one event to `Projects/UOContent/Engines/Harvest/Core/HarvestDefinition.cs`,
>   raised where the harvest system sends a player its message, so a bot can hear why a swing produced nothing.
> - `Titles-skill-title.patch` adds an overload to `Projects/UOContent/Misc/Titles.cs` that titles a mobile by a skill
>   the caller chooses, so a bot is ranked by what it earned rather than by what every bot is handed.
> - `StepProbe-standable-with-multis.patch` adds a method to `Projects/UOContent/Engines/Pathing/Cache/StepProbe.cs`
>   that finds the surfaces a walker can stand on with house and boat floors counted, beside the old one, which sees
>   only the map's own tiles.
> - `BaseHouse-navigation.patch` tells the tiered navigation, from `Projects/UOContent/Multis/Houses/BaseHouse.cs`,
>   when a house is raised, moved or taken down, so the ground under it is drawn again.
> - `BaseGuard-killing.patch` adds a static event to `Projects/UOContent/Mobiles/Guards/BaseGuard.cs`, raised just
>   before a guard kills its focus outright, and names the guard as the killer, so a bot can count a guard's kill as
>   its rescue.
> - `GuardedRegion-hostile-creatures.patch` makes a wild creature that attacks on sight and is fighting a player a
>   guard candidate in `Projects/UOContent/Regions/GuardedRegion.cs`, whoever struck first. It covers only players
>   with no client (the bots) unless `guards.hostileCreaturesForClients` is set; `guards.hostileCreatures` turns it
>   off.
> - `GenericSell-coloured-ingots.patch` lets the blacksmith and the smith-tools shopkeeper buy coloured ingots below
>   iron's price, consumed rather than put up for sale again, through a new `GenericSellInfo.Add(type, price,
>   resellable)` and a new file, `Projects/UOContent/Mobiles/Vendors/SBInfo/ColouredIngots.cs`.
>
> The new code is the tiered navigation, `Projects/UOContent/Engines/Pathing/Tiered/`, with its tests in
> `Projects/UOContent.Tests/Tests/Engines/Pathing/Tiered/`: a graph of the whole map that answers a route of any
> length in a few milliseconds. It draws the graph in slices of the game loop at boot (Felucca takes about 100 s, 18 s
> of it on the loop), keeps it in `Distribution/Data/Pathfinding/` between boots, and is switched in
> `modernuo.json` by `pathfinding.tiered.enable` and `pathfinding.tiered.persist`.
>
> Without them `BotAIv2` does not compile (the coloured ingots apart: without that patch the smiths simply buy no
> coloured bar). Nothing in the engine subscribes to the three events, every message is still sent exactly as before,
> and the old `GetSkillTitle` and the old surface probe answer exactly as they did. The two patches that change what
> the engine does, the guards and the coloured ingots, are the shard's rules, and say so above their diffs.
> Each file explains itself above its diff; `git apply` skips that text. The diffs need LF line endings, which this repository's
> `.gitattributes` keeps on every checkout.

---

## Requirements

| | |
|---|---|
| Server | ModernUO at `be3a085` (newer ModernUO uses 4.x serialization packages, which `BotAIv2` does not build against yet), .NET 10 SDK |
| **Expansion** | **Renaissance (UOR)** — `"core.expansion": "UOR"` in `Distribution/Configuration/modernuo.json` |
| Client | any UO client ModernUO supports; ClassicUO was used throughout |
| Ollama | only for the thinking bots and the observers — see below |

The era matters. Every weapon, spell circle, armour rating and crafting recipe the bots reason about is read off the
shard's own tables for **this** expansion. On a later expansion the code still builds and the numbers stop meaning
what they say.

---

## 1. Ordinary bots

**Copy the projects**

```
Projects/BotAIv2/                                        → your ModernUO checkout, same place
Projects/UOContent/Engines/Pathing/Tiered/               → same place
Projects/UOContent.Tests/Tests/Engines/Pathing/Tiered/   → same place (the tests; optional)
```

**Add them to the solution** (`ModernUO.slnx`):

```xml
<Project Path="Projects/BotAIv2/BotAIv2.csproj" />
<Project Path="Projects/BotAIv2/mindedBots/BotMindAI.csproj" />
```

**Load them** — `Distribution/Data/assemblies.json`:

```json
[
  "UOContent.dll",
  "BotAIv2.dll",
  "BotMindAI.dll"
]
```

**The switches** — `Distribution/Configuration/modernuo.json`. Every one defaults to `"True"` and is written there
at first boot: `"bots.enabled"` for the whole thing, then one per module, there to switch one off while diagnosing:

```
bots.alarm.enabled      bots.auction.enabled    bots.baron.enabled      bots.classes.enabled
bots.craft.enabled      bots.dashboard.enabled  bots.debugger.enabled   bots.delve.enabled
bots.drill.enabled      bots.estate.enabled     bots.harvest.enabled    bots.hunt.enabled
bots.mend.enabled       bots.mind.enabled       bots.movement.enabled   bots.population.enabled
bots.quests.enabled     bots.shops.enabled      bots.spells.enabled     bots.squads.enabled
bots.will.enabled       bots.mind.thinking
```

**Copy the configuration** — `Distribution/Configuration/bot-*.json`. Every file is optional: leave one out and the
code's own defaults apply, and the file writes itself on first boot.

**Build and run**

```bash
dotnet build ModernUO.slnx -c Release
cd Distribution
dotnet ModernUO.dll
```

The first start asks where the client files are, which expansion — `UOR` — and for the owner account, so it has to
be run from a real console; on Windows that is PowerShell or cmd, not the Git Bash window. After that
`START_SHARD.bat` starts it detached. The shard raises the population on start. Confirm it in the log:

```
Population raised: 80 bots at (1440, 1470, 0) on Felucca
```

**Give them a world.** A fresh ModernUO world is bare. As the owner, in game: `[Decorate`, then
`[ImportSpawners Data/Spawns/shared/felucca/*.json`, then `[Save` and `[Restart`. The bots find shops, forges,
fires and creatures by surveying the ground as they go, and a survey is kept for the rest of the session, so the
restart is what makes them look again at a world that now has something in it.

**Where to start tuning** — `Distribution/Configuration/bot-population.json`:

```json
{
  "Map": "Felucca",
  "Home": [1440, 1470, 0],
  "Purse": 400,
  "Run": true,
  "Roam": 1000,
  "Classes": { "Warrior": 15, "Archer": 13, "Gatherer": 12, "Mage": 10, "WarriorMage": 8, "WarriorArcher": 8,
               "Crafter": 5, "Healer": 4, "Brawler": 1, "Captain": 1, "Baron": 1, "Architect": 1, "Sage": 1 }
}
```

`Classes` is the whole population: names come from the classes in `BotClasses/`, counts are yours. `Home` is where a
bot with no guild lives; a bot in a guild is born and raised again at its guild's seat.

> Config keys are **PascalCase**. A lower-case key is silently ignored and the default applies, with nothing in the
> log to say so.

**In game** — `[bots` opens the dashboard (administrator only), eleven tabs.

**Stopping** — write `do save` into `Distribution/argus-in.txt`, wait for the answer in `argus-out.txt`, then
`taskkill /F /IM ModernUO.exe`. A kill without the save rolls the world back to the last autosave.

---

## 2. The observers and thinking crafters (Ollama)

Optional. The rest of the population is untouched either way, and if Ollama is not running the shard **does not
fail** — the minds log that they could not reach the model, the observers go on measuring, and every bot decides by
arithmetic like everybody else.

**Install Ollama and pull the models**

```bash
ollama pull deepseek-r1:14b
ollama pull qwen3.5:9b
```

The first is the observers' model. The second is the crafters', chosen for latency, and needed only if crafters
are to think. One model fits in 12 GB of video memory at a time; both configuration files are tuned around that.

**The observers** — on by default (`"bots.debugger.enabled"`), and optionally configured in
`Distribution/Configuration/bot-debugger.json`. In game, `[argus` takes you to the lead observer and `[argus here`
brings it to you; from the keyboard, write a line into `Distribution/argus-in.txt`.

**The thinking crafters** — off by default: `CrafterNames` in `Distribution/Configuration/bot-mind.json` is empty,
so no minds run. Naming crafters there switches them on:

```json
{
  "CrafterNames": ["Roderic", "Emeric", "Ulric", "Wulfric", "Alaric"]
}
```

Each name is a mind that claims a crafter and renames it, so keep at least as many crafters in
`bot-population.json` as there are names. Without Ollama nothing breaks: the calls fail and those crafters choose by
arithmetic. `bot-minds.json` holds the rules each mind has written for itself; the shard writes it.

**Everything else about models and prompts** — which model for how much video memory, every key of `bot-mind.json`
and `bot-debugger.json` with its default, connecting LM Studio, llama.cpp, vLLM or a hosted API through
`"Api": "openai"`, what the model is shown, the answer schema, the rules for writing a prompt, and where to look
when it goes wrong — is in [`mindedBots/LLM.md`](mindedBots/LLM.md).

---

## Notes for a fork

- **`mindedBots/` is nested but is not part of `BotAIv2`.** `BotAIv2.csproj` excludes it explicitly
  (`<Compile Remove="mindedBots\**" />`). Without that exclusion an SDK project would compile those files into
  `BotAIv2`, and since `BotMindAI` references `BotAIv2`, the build would refuse the cycle.
- **The bots live in the world save**, like any character: a restart brings each one back where it stood, with its
  pack and its bank. Do not delete the world save — it holds real characters too.
- **Eleven stores are written beside it under `Distribution/Saves/`**: `BotProgress` (what each bot learned, by
  name), `BotCommons` (what each trade pays where), `BotQuads` (what the population found out about the island),
  `BotClaims` (who owns which square), `BotSeats` (where each guild lives), `BotChests` (the guilds' chests),
  `BotWars` (wars, truces, clocks and the guilds' opinions of each other), `BotCity` (the city's standing orders and
  bounties), `BotQuests` (the board of errands), `BotOutlaws` (the murderers and the cells) and `BotUnderworld` (the
  criminal record and The Shadow). Deleting one resets that and nothing else.
- **A running shard holds the assemblies.** Building `BotAIv2` while it runs still reports compile errors; building
  anything under `mindedBots/` does not, because MSBuild skips it once its dependency's copy has failed. Stop the
  shard before trusting that build.
- **Everything is measured in the log.** Each subsystem prints a line naming every refusal separately. If something
  is not happening, the reason is usually already written down.
