# Building it at home: from these files to a working assembly

For anybody with a clone of the engine fork to hand. Everything below was **read out of the fork itself**
(`github.com/DmitriyBol/ModernUO-fork`, branch `main`) rather than guessed or copied from the first version.

> **What to expect, honestly.** After these steps the shard builds, starts, **and eighty bots in five guilds
> appear around Britain** who dress themselves, take on work and see it through: they mine, cut wood, pick
> reagents, forge, sew, brew, fletch, cook, write scrolls, trade over counters and with each other, bind wounds,
> hunt, form companies, teach each other for a fee, raise halls, claim land, go to war under rules that end the
> war, and take parties into dungeons. A squad of three invisible observers watches the rest if Ollama is running,
> and four crafters can be made to think through a local model.
>
> **This file was first written in August, before the shard had ever run, and its later sections still read that
> way.** Since then it has run for weeks on end. The numbers that matter live in
> `Distribution/Configuration/bot-*.json` and in the boot log, `MAP.md` and `DIALS.md` are generated from the
> source, and `INSTALL.md` is the short, current version of §2 below. Trust those over prose.

---

## 1. What has already been verified against the fork

So that it does not have to be checked again at home.

| What | The fact, from the fork |
|---|---|
| Branch, framework | `main`, `net10.0`, `LangVersion 14` (`Directory.Build.props`) |
| **Warnings** | `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>` — any warning is a build failure |
| Packages | `ModernUO.Serialization.Annotations 2.14.2`, `Generator 2.14.3` — exactly as in `BotAIv2.csproj` |
| Serilog | `Directory.Build.props` gives 4.3.1, `UOContent.csproj` lifts it to **4.4.0** with `Update` — `BotAIv2.csproj` does the same, and it is not redundant |
| Project references | `Projects/{Server,UOContent,Logger}` exist; the `Private="false" PrivateAssets="All" IncludeAssets="None"` pattern for `Server` is taken from `UOContent.csproj` |
| Where the output goes | `OutDir = ..\..\Distribution\Assemblies`, as `UOContent` does |
| Assembly loading | `Projects/Server/Main.cs` reads `Data/assemblies.json` |
| Settings | `Projects/Server/Configuration/ServerConfiguration.cs` — `Configuration/modernuo.json` |
| The solution | `ModernUO.slnx` lists 7 projects and `BotAIv2` is **not** among them |

**Every engine call in v2 has been checked against the fork.** Separately, the thing that stood in the handoff for
a long time as a "known risk": `Item.Weight` at `Projects/Server/Items/Item.cs:474` is a **real setter**, so
`BotBinding.Weightless` compiles and works. Risk closed.

Also verified by name and signature: `Banker` (in `Server.Mobiles`, `Deposit(Mobile,int,bool=true)`,
`GetBalance(Mobile)`), `StaminaSystem.StonesOverweightAllowance` (`Server.Misc`),
`PlayerMobile.MaxWeight = (ML && human ? 100 : 40) + 3.5×Str`, `Mobile.BodyWeight`, `Map.MapID`,
`Map.GetMobilesInRange<T>(Point3D,int)`, `Map.GetItemsInRange(Point3D,int)`,
`Tiles.GetStaticTiles/GetStaticAndMultiTiles/GetLandTile`, `Map.GetAverageZ`, `Skills.Length`,
`Skills[SkillName]`, `Skill.Base/Value`, `Container.GetAmount/ConsumeTotal/FindItemByType<T>/DropItem/Items`,
`Mobile.BankBox/Backpack/Target/EquipItem/Move/GetDirectionTo/InRange`, `Utility.InRange`,
`GetDistanceToSqrt` (an extension on `IEntity`), `Notoriety.Compute/Innocent`, `TownRegion`,
`BaseCreature.DamageMin/DamageMax/Controlled/Summoned/IsDeadBondedPet`, `BaseWeapon.MinDamage/MaxDamage`,
`Mobile.CanBeHarmful`, `Item.LootType`, `Item.IsStandardLoot`, `Target.Invoke(Mobile,object)`,
`StaticTarget`/`LandTarget`, `JsonConfig.Deserialize<T>/Serialize`, `ServerConfiguration.GetOrUpdateSetting`
(bool/int/double), `EventSink.WorldLoad`, `LogFactory.GetLogger`, `ILogger.Information/Error(params object[])`,
`Type.CreateInstance<T>()`.

Harvesting, entirely from the fork: `Mining.System.OreAndStone`, `HarvestDefinition.GetVeinAt(map, x, y)`
(**bank** coordinates — `GetBank` divides for you and `GetVeinAt` does not), `BankWidth/BankHeight`,
`GetBank(...).Current` (the vein's remaining ore is readable), `MaxRange = 2` (the swing radius comes from there
rather than being set), `HarvestSystem.GetDefinition(tileID, isLand)`, `StartHarvesting(Mobile, Item, object)`,
and most importantly `CheckTool` **does not require the tool in a hand**, so a pickaxe works from the pack.

Trading, both ways: `BaseVendor.OnBuyItems/OnSellItems/GetBuyInfo/GetSellInfo/Restock/UpdateBuyInfo/
IsActiveSeller/IsActiveBuyer/LastRestock/RestockDelay`, `GenericBuyInfo.Type/Price/Amount/GetDisplayEntity`,
`BuyItemResponse`, `SellItemResponse`, `IShopSellInfo.IsSellable/GetSellPriceFor`.

Crafting: `DefTailoring.CraftSystem`, `DefInscription.CraftSystem`, `CraftSystem.CraftItems`,
`CraftItem.Resources/Skills/ItemType/Mana/Craft(from, system, typeRes, tool)`, `CraftRes.ItemType/Amount`,
`CraftSkill.SkillToMake/MinSkill`, `SewingKit`/`ScribesPen : BaseTool`, `BlankScroll`, `Cloth`.

Magic and mending: `Spellbook.Content/HasSpell/SpellCount/OnDragDrop/SpellbookType`, `SpellScroll.SpellID`,
`Loot.RegularScrollTypes`, `SpellRegistry.NewSpell`, `Spell.Cast/Reagents/OnCasterHurt`,
`SpellTarget<Mobile>`, `IRangedSpell.TargetRange`, `BandageContext.BeginHeal/GetContext/Slip`, `Bandage.Range`,
`BasePotion.CanDrink/Drink`.

Combat: `Mobile.Combatant` **starts a server-side timer by itself** (`Mobile.CheckCombatTime`), so a bot fights
without a client; `BaseWeapon` wears down on every landed hit and is destroyed at zero; `Corpse.Owner` and
`Corpse.CheckLoot`.

The bot itself: `PlayerMobile` has both a parameterless constructor and `(Serial)`; serialisation in the fork is
on the **legacy pattern** (`base.Serialize(writer); writer.Write(version);`) rather than the source generator, so
the subclass does the same; `OnDamage(int,Mobile,bool)`, `OnDeath(Container)`, `OnAfterResurrect()`,
`OnAfterDelete()`, `Mobile.Resurrect()`, `MoveToWorld(Point3D,Map)`, `Map.CanSpawnMobile` (the overload that
searches for Z), `Map.TryParse`, `Map.Felucca`, `World.Mobiles`,
`Race.RandomSkinHue/RandomHair/RandomHairHue` (`Mobile.Race` returns `DefaultRace` on its own and does not need
assigning), `Utility.RandomDyedHue/RandomMinMax/Random`, `StatCap`, `RawStr/RawDex/RawInt`,
`HairItemID/HairHue`, `Warmode`, `Combatant` (a `Mobile`), `Female`, `Body`, and `PlayerMobile.MaxWeight`, which
is why the weight ceiling is computed with overflow protection.

**And it builds.** On the development machine, against a clone of this same fork:

```
dotnet build Projects/BotAIv2/BotAIv2.csproj -c Release
→ BotAIv2 -> Distribution/Assemblies/BotAIv2.dll
   0 Warning(s)  0 Error(s)
```

**What cannot be claimed:** it has never been **run**. There are no client files (maps, art) on the development
machine and the server will not come up without them, so everything about behaviour remains reasoning rather than
observation.

One trap if you build in a fresh clone: `Nerdbank.GitVersioning` fails on a **shallow clone**
(`git clone --depth 1`) — "Shallow clone lacks the objects required to calculate version height". A full clone,
`git fetch --unshallow`, or temporarily moving `.git` aside all cure it. That is about the clone, not the project.

---

## 2. Installing

**1. Put the folder in place.** Copy `botAiv2` into the fork as `Projects\BotAIv2`:

```
ModernUO-fork\Projects\BotAIv2\BotAIv2.csproj
ModernUO-fork\Projects\BotAIv2\BotCore.cs
ModernUO-fork\Projects\BotAIv2\BotModules\ ...
```

The paths in the `.csproj` are relative and assume exactly that depth — the folder must sit **beside**
`UOContent`, `Server` and `Logger`. One level deeper and every path needs another `..\`.

**2. Apply the engine patches** from the fork root, before building: every file in
`Projects/BotAIv2/engine-patches/` with `git apply` — `CraftItem-heat-source.patch`, `CraftItem-said.patch`,
`HarvestDefinition-said.patch`, `Titles-skill-title.patch` and `DefInscription-knows.patch` (25.09.2026). Each file
begins with what it changes and why; BotAIv2 uses every one of them, so none may be left out.

**3. Add both projects to the solution** (needed if you run `dotnet build` from the root). In `ModernUO.slnx`,
alphabetically among the others:

```xml
  <Project Path="Projects/BotAIv2/BotAIv2.csproj" />
  <Project Path="Projects/BotAIv2/mindedBots/BotMindAI.csproj" />
```

**4. Build.**

```bash
dotnet build ModernUO.slnx -c Release
```

The results land in `Distribution\Assemblies\BotAIv2.dll` and `BotMindAI.dll`.

**5. Register the assemblies.** `Distribution\Data\assemblies.json` should read:

```json
[
  "UOContent.dll",
  "BotAIv2.dll",
  "BotMindAI.dll"
]
```

**6. Switch the modules on** in `Configuration/modernuo.json`: `bots.enabled` and one `bots.<module>.enabled` per
module. The list is in `INSTALL.md`.

**7. Start the shard** with `START_SHARD.bat` from the fork root (`START_SHARD.bat --nopause` from a script). It
starts the shard through a scheduled task with no triggers, so the shard outlives the shell that ran it, and waits
until it reports listening.

**8. Read the log** — `logs/session-<yyyy-MM-dd_HH-mm>.log` — and compare it with §3. The configuration files create
themselves on the first start.

---

## 3. What the log should say

> **The lines below were written before the first run and are a guide to the shape, not a transcript.** On a
> current build these are the lines worth finding first, in `logs/session-<date>.log`:
>
> - `Will ready: … steadfast work is held for ×1.5 its own reckoning up to 480000ms … a bot's own trade is worth ×1.3
>   and another class's ×0.6 …` — the decision layer and the numbers it will judge work by
> - `Proposer <Name> offers work on the <Rung> rung`, once per proposer — forty-eight of them, `Attendant` among them
> - `Mending ready: …` and `Flight ready: …`
> - `Population raised: 80 bots at (1440, 1470, 0) on Felucca`
> - `Argus is listening at …\argus-in.txt`, if the debugger is on
>
> and, five minutes in, the first summary block: `Will:`, `Resolve:`, `Roles:`, `Arms:` and the rest, each prefix
> listed in `MAP.md` §1.

At startup, before the world loads:

```
Classes: 9 — 3 melee, 2 ranged, 1 caster, 1 medic, 2 producing; 3 of them cast
The dashboard is on [bots — three tabs: the population, their market, and what they are short of
Bot modules, Settings: 2 of 2 started
BotAI v2 loaded (enabled): 2 modules running, 0 switched off, 0 that should be running and are not
```

Three casters with one mage is not a typo: the warrior-mage and the healer read spells too.

After the world loads, **eleven** `World`-phase modules, in an order the loader derives itself (`Harvest` and
`Population` after `Will`, `Spells` and `Hunt` after `Auction`, because they declared them in `Requires`):

```
Movement ready: ...
Squads ready: ...
Will ready: a point of skill is worth 500 gold, dying costs 3 minutes; work is reviewed every 15000ms, ...
Harvest ready: a trip is reckoned at 45 a minute over 8 minutes, an ingot at 6 gold; the ground is swept 160 tiles ...
Shops ready: shopkeepers swept 160 tiles around the first bot to ask, traded with from 3 tiles, ...
Craft ready: a tailor buys 20 cloth at a time, works 5 points below its own skill, ...
The market is open on both sides: prices rise 15 % when the same goods sell inside 600000ms, fall 10 % after 1800000ms untouched, ... one supplier may fill at most 5 units of a want at a time
Read 64 of 64 scrolls; circles 1 to 3 can be bought and the rest have to be written
Spells ready: 64 scrolls mapped, circles 1-3 on shop shelves and 40 that have to be written; a scribe buys 20 blanks at a time, writes 5 points below its own Inscribe and attempts every 3000ms
The population's clock is running: looked at every 100ms, each bot taking a turn every 400ms
Population raised: 4 bots at (1592, 1680, 10) on Felucca; 0 looks, 0 turns handed out, 0 faults; every 100ms, a turn each every 400ms
The hunt is on: quarry looked for 30 tiles out and taken only when our power beats its ×1.5, set out above 80 % health and given up below 40 %
Mending ready: a bot looks after itself below 70 % health and stops at 95 %, spell before cloth; a caster watches 20 tiles for somebody worse off
Bot modules, World: 11 of 11 started
```

**`Read 64 of 64` is the first line worth looking at.** Fewer than sixty-four means the scroll-to-spell map was
not built completely, and the whole magic subsystem will be silent without a single error.

**And a second or two later, the thing all of this was written for:**

```
Swept 160 tiles around (1592, 1680, 10) on Felucca in 47ms: 12 seams, 2 fires, 3 counters (now 12, 2, 3)
Alden took on mine: 45/min = 45 × near 0.62 × new 1.00 × room 1.00 × safe 1.00 × purse 1.00
Alden finished mine: 118 in 6.4 min (18/min): 0 coin, 108 made, 0.2 skill
```

**A mage with a trade and a book that grows:**

```
Perrin took on inscribe: 60/min = 60 × near 0.71 × new 1.00 × room 1.00 × safe 1.00 × purse 1.00
Perrin bought 20 BlankScroll from Rance for 100gp
Perrin finished inscribe: 11 HealScroll in 19 attempts, 1 into its own book, 0 sold to order
Elowen took on acquire: 12/min = ...
Elowen bought 1 CureScroll from Rance for 22gp
Elowen finished acquire: learned CureScroll for 22gp
```

**The first gold to enter the world**, which is the line the whole economy hangs from:

```
Alden took on hunt: 60/min = ...
Alden finished hunt: 2 things and 43gp off a skeleton
Alden sold 1 things to Verity for 80gp
```

And when somebody's book reaches the fourth circle, which nobody sells — the first trade between two bots in the
project's history:

```
Elowen wants 1 LightningScroll and has put 42gp down for them
Perrin filled Elowen's want for 1 Lightning Scroll and was paid 42gp
Elowen finished acquire: learned LightningScroll for 0gp
```

Then there is what can be looked at directly: **`[bots`**. The first tab is the population row by row (what it is
doing, how content it is, how much money it has, and how far along its own vector it is); the second is their own
market with icons of the goods; the third is what the population cannot get hold of and how much it will pay.

The numbers will be different. What matters is the shape: an estimate, what it was made of, and a closed chain
with what it produced.

**The line to look at first for any oddity** is the last part of the fourth: "so many that should be running and
are not". That is the one number answering "did everything actually come up", and above it the log will say which
ones and why.

The first start also creates the settings files — one per subsystem, each with every dial, empty by default:

```
Distribution\Configuration\bot-classes.json
Distribution\Configuration\bot-movement.json
Distribution\Configuration\bot-squad.json
Distribution\Configuration\bot-will.json
Distribution\Configuration\bot-harvest.json
Distribution\Configuration\bot-population.json
Distribution\Configuration\bot-auction.json
Distribution\Configuration\bot-shops.json
Distribution\Configuration\bot-craft.json
Distribution\Configuration\bot-spells.json
Distribution\Configuration\bot-hunt.json
Distribution\Configuration\bot-mend.json
```

An empty value means "keep the number the code chose". No dial is mandatory — **except in
`bot-population.json`**, which is written with working values (Britain, four bots), because an empty class list
means "there are no bots at all". Who to raise and where is the one thing this project cannot decide for you.

---

## 4. The switches

All in `Distribution\Configuration\modernuo.json`, written on first read:

| Key | What it kills |
|---|---|
| `bots.enabled` | everything; not one module is registered |
| `bots.classes.enabled` | the nine classes (and then everything requiring them fails to start — and says so) |
| `bots.movement.enabled` | pathfinding and the step |
| `bots.squads.enabled` | squads; every bot acts alone |
| `bots.will.enabled` | deciding; everything else works and nobody chooses anything |
| `bots.harvest.enabled` | mining; then there is no work in the world and the census shows it |
| `bots.population.enabled` | the bots and the clock. The shard becomes what it was before the last folder: subsystems loaded, nobody to use them |
| `bots.shops.enabled` | NPC trade, both ways. **The world gets no money and spends none** |
| `bots.craft.enabled` | sewing. The crafter goes back to the pickaxe |
| `bots.auction.enabled` | the market, both sides. Metal goes to the bank box; no stalls and no wants |
| `bots.spells.enabled` | magic. Books stay at three spells, reagents are not consumed, and the mage has no work again |
| `bots.hunt.enabled` | hunting. **And then there is no gold in the world again** — everything with an outlay fails |
| `bots.mend.enabled` | mending. `Failing` is empty again: a hurt bot holds its task and dies with it |
| `bots.dashboard.enabled` | the `[bots` command. The population works as before with nothing to watch it |

Not for flexibility but for diagnosis: halving the number of running modules is faster than reading a 37 MB log.

---

## 5. If it does not build

| Symptom | Cause and what to do |
|---|---|
| `CS0246` on `Server.*` types | the folder is at the wrong depth. It must be `Projects\BotAIv2\`, beside `UOContent` |
| a warning became an error | the fork sets `TreatWarningsAsErrors=true`. Fix the cause; do not silence it with `NoWarn` |
| `NU1605` / a Serilog conflict | `BotAIv2.csproj` has `<PackageReference Update="Serilog" Version="4.4.0" />` and it must match `UOContent.csproj`. If the fork bumped it again, bump it here too |
| `NU1101` on `ModernUO.Serialization.*` | no NuGet access for this user; the versions should be 2.14.2 / 2.14.3 as in `UOContent.csproj` |
| it built but the shard does not see it | `"BotAIv2.dll"` was not added to `Distribution\Data\assemblies.json` |

Build `-c Release`. The `-c Analyze` configuration additionally enables the analysers and `Rules.ruleset`
(`Directory.Build.props`) — useful as a separate pass, but with `TreatWarningsAsErrors` its style remarks become
errors too, and those are not about whether the thing works.

---

## 6. If it builds but "nothing happens"

There are bots now, so "nothing happens" is a diagnosis rather than an expected state. Check in order; every item
is a line in the log:

1. `No bots were raised` — a class name was not parsed, or there is nowhere to place them. Check the names in
   `bot-population.json` (they must match the nine) and the `Home` point.
2. `Nothing proposes any work at all` — no proposer is registered, i.e. the work modules are switched off or did
   not start.
3. `Mining is not offered on <map>: no a fire has been found near any bot yet` — the sweep found no forge with an
   anvil near where the bots are standing. That is about the world rather than the code: look at the
   `Swept 160 tiles around ...` line, which prints what was found. Zero fires means "move the spawn point nearer a
   smithy" or "raise `SweepReach`".
4. `Nothing within 30 tiles ... is worth fighting` — no gold will enter the world. Either the spawn point is in a
   place with no monsters, or `BotQuarry.Reach` is too small.
5. Bots standing still with `took on mine` in the log — the work was taken and cannot be reached. Then look for the
   movement lines: `has no way to`, `made no progress towards`.
6. Every five minutes, the `Will: ...` census. `times nothing was worth doing` is about the **world**, not about
   the bots.
7. `threw on its turn` — an exception inside one bot. The others carry on, and the line has the stack.
8. On reload, the shops line: `N things bought for Xgp, M sold for Ygp`. **The difference between those numbers is
   the health of the economy.** If the second is zero, nothing is hunting or nothing is selling.

---

## 7. The fork's rules this code obeys

They are not to be "simplified back" — each is taken from the fork's `CLAUDE.md` and `dev-docs/`.

**Tick counts are compared by subtraction only, and no zero sentinels on tick fields**
(`dev-docs/tick-counts.md`). On some hosts the tick counter is the physical machine's uptime passed through: it
starts enormous and can go negative, so zero is a legitimate value rather than "never". That is why the code keeps
flags beside its tick fields — `Struck`, `Aside`, `Due`, `IsBarren`, `Cautioned`, `_stamped`, `_censused`,
`_dangerous` — and that is exactly what they are for.

**No `System.Text.StringBuilder`** — `ValueStringBuilder` from `Server.Text` (rule 17). The only places are
`BotWill.Describe` and `BotListing`/`BotWant`'s label helper.

**No enumeration of `World.Items` / `World.Mobiles`** — spatial queries only (rule 4). The first version walked the
world to find forges; there is one exception here, in `BotPopulation.PurgeSaved`, with its reasoning written at the
call site.

**Braces on every branch** (rule 15), and `LogFactory` rather than `Console` (rule 2).

**Not one edit in the engine.** The project is a separate assembly; nothing in `Projects/Server` or
`Projects/UOContent` references it, so an upstream rebase of the fork does not touch it.
