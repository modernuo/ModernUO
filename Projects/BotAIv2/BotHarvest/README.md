# Harvest: the first work in the world

Dig, smelt, bank it. The first subsystem that gives the population something to want — and the test of whether
the brain works at all.

| File | What is in it |
|---|---|
| `BotOre.cs` | ore: what is in a hill, what digs it, one swing, and what ore becomes in a fire; what a vein is worth against iron by what its metal is paid for (a shopkeeper or a want), so a coloured vein nobody buys ranks below iron (06.10.2026); the counters' hourly purse for coloured bars (`ColouredPerHour`, `CounterRoom`; the smiths buy them at 3/2gp since ingots16) |
| `BotHeard.cs` | what the harvest system last said to each bot, and which sentence means what |
| `BotGround.cs` | what the population knows about places: one sweep yields veins, fires and counters; a seam struck off leaves the cached answers (`Untell`), and a hearth a cook found cold rests for every cook (`Cold`), a forge whose anvil the engine refused for every smith (`Unfit`); a seam on ground that has lately refused somebody is not chosen (`Unwalked`, build 51); nor one whose every rock in reach the engine has emptied — asked of the winner with the trip's own rings (`BotOre.Stocked`), rested as the trip would rest it, and chosen again (`Hollow`, build 63) |
| `BotDig.cs` | an obligation with three legs: vein → fire → counter; a pickaxe worn through with ore worth melting still goes to the fire; the seam is held from the first step of the walk, and a miner whose seam another holds goes on to a free one; a seam whose rocks are all there and worked out rests rather than being struck off |
| `BotMiner.cs` | the proposer: one offer to whoever has a pickaxe |
| `BotHarvestConfig.cs` | `Configuration/bot-harvest.json` |
| `BotHarvestModule.cs` | module, phase `World`, requires `Classes` and `Will` |
| `BotChop.cs` | cutting wood: walk to the nearest tree and swing until the pack has enough or the tree has nothing left |
| `BotForage.cs` | picking up the reagents the world leaves lying about, and putting them on the board |
| `BotHerbs.cs` | a walk into the woods that comes back with herbs |
| `BotProspect.cs` | a walk out past the last swept ground, so that there is rock on the board somewhere nobody has been |
| `BotTimber.cs` | what a woodcutter needs to know: an axe the body can hold, the trees within reach, and how much wood is worth a trip |
| `BotWoodsman.cs` | offers a bot with an axe a trip to the woods, and only when somebody wants the wood |

---

## Why a chain and not just "dig"

**Ore is worth nothing to anybody.** No counter buys it and no bot wants it, and a miner that comes home with a
pack full of rock has produced exactly nothing — which is what v1's miners did all night, because the goal ended
at the vein. So the work here is not "dig" but **dig, carry it to a fire, and put the metal somewhere safe**, and
it is not finished until the last of those has happened.

That is why this chain came first: it exercises everything the brain can do at once. Stages that survive
interruption. A named skill credited only in finished work. Goods that are worth something **before** they are
sold. And a definition of "done" that is **not where the work happened**. Hunting would have tested none of the
last three.

---

## The work is the engine's

Digging goes through the shard's `HarvestSystem` — the same call a player's double-click makes — so the swing,
the skill check, the ore that comes out and the exhaustion of the vein are all real. A bot that digs is a miner,
not a bot that was credited with ore. And that is what makes the thing this project measures measurable: the
skill gain here is the engine raising a number, not us deciding a number should rise.

Four things v1 learned expensively, all four accounted for here.

**Ore is not uniform, and the shard will say which kind.** `GetVeinAt` seeds a stable draw from the bank's
coordinates, so every 8×8 block has its ore type fixed for the life of the shard, and iron is only half of them.
The complaint "my miners bring nothing but iron" had exactly this cause: they were digging the nearest rock. **Ask
with bank coordinates, not tile coordinates** — `GetVeinAt` divides before it asks, so a tile asks the bank
sixty-four times further away, which reads as ore scattered at random instead of ore in veins.

**Sand is not mining.** It is worked by masters with a hundred points and a flag nobody has, and a bot swinging
at a beach gets a message nobody can read and nothing in the pack. Britain is surrounded by sand, and in v1 both
gatherers dug it all night: the ground passed every check the bot could make and produced not one ingot in eight
hours. The test is one line: the tile definition must be **exactly** `OreAndStone`.

**A fire has to be pointed at, not stood next to.** Ore is smelted by double-clicking it and targeting a forge:
the double-click only opens a target and waits for a player. A bot has no client, so in v1 **not one ingot was
ever smelted** — every pile of ore opened a target that hung there until the next one replaced it. The answer is
to fill in the target ourselves, which is exactly what a client does.

**Weight kills.** A pack holds about twelve ore before the engine starts charging stamina per step, and at zero
it refuses the step outright. In v1 three bots stood that way for a whole session while the log confirmed six
hundred times that the step was allowed. It was allowed — stamina was not in the message. So digging ends by
weight, not by count.

---

## The sweep: how the population learns about places

A bot in town cannot see the mine and a bot in the mine cannot see a forge — so knowledge of places cannot come
from looking. It has to be walked once and remembered.

**What one pass looks for:** veins (every fourth tile — mountains are large), fires (every tile — a forge is one
tile wide) and counters (one spatial query for the whole sweep, because bankers are mobiles).

**The sweep goes round the bot, not round a list of towns.** v1 swept vendor clusters and missed the only town
that mattered: the centre of Britain's cluster is the mean of the shopkeepers' spawn points, and it lands inside
a wall 246 tiles from the smithy. The result was eight forges across four facets and **none** on Felucca, where
the entire population lived. A bot asking where it can work starts a sweep around itself — and missing the place
where the bots actually are is then impossible.

**Forges come in two kinds**, which is another reason nothing was ever forged in v1: a player-placed one is an
item and a query finds it; every town one is a **static tile**, part of the map, and no query finds it at all. On
this shard Felucca's smithies turned out to be items while the other facets' were statics, so both kinds have to
be looked for.

None of this is written to disk or survives a world reload: every point in these lists is a fact about a world
that has just been replaced.

---

## How the takings are counted

A finished chain's takings are `Δmoney + goods produced + Δskill × rate`, over minutes. For a miner:

- **Δmoney = 0.** The chain brings in no coin at all, and `Coin = 0` says so honestly: metal in the bank is
  wealth the population can use, but it is not money until somebody has bought it.
- **goods produced** = ingots × `BotAuction.Worth(iron, GoldPerIngot)`. The market is asked first — what somebody
  is offering with the money down, then what iron has actually changed hands for — and `GoldPerIngot` (6) is the
  last resort when the shard has never had an opinion. That ordering is how a shortage reaches a miner: nobody
  tells it to dig, a want for metal raises what its metal counts for, and the ledger raises its estimate of
  digging next time.
- **Δskill** is the only part that is genuinely measured, and it is also the largest: half a point of Mining per
  trip at a rate of 500 outweighs all the metal. That the skill dominates is not an accident of the numbers; it
  is what this population is for.

Skill is credited **only if the chain reached `Done`**, so "train on a dummy" does not work here: the gain has to
arrive alongside metal in the bank.

---

## What is closed, and by what

| Hole | What closes it |
|---|---|
| dig for ever and seize up from the weight | digging ends at `FillFraction` of the engine's own ceiling |
| stand at an exhausted vein | the engine's remaining-ore count is read, and six empty swings write the tile off |
| learn that "the vein is bad" when the forge was at fault | the proposer offers no chain until a fire and a counter are known |
| walk in circles to an unreachable vein | `Bend` picks another vein, but no more than three times |
| mint money on deposit | the gold is taken out of the pack **before** `Banker.Deposit`, because the engine's deposit adds to an account without touching what the depositor carries |
| get paid twice for one bar | what goes into a funded want is paid in coin, so it comes back off `Made` |

---

## What to check with a client

1. At startup, the `Harvest ready:` line with the ingot rate and the bid for a trip.
2. Any bot's first bid prints `Swept 160 tiles around ...` with the numbers: how many veins, fires and counters.
   **Zero fires is the most important thing that can appear there** — it means nobody will offer the chain, and
   there will be a separate line saying so by name.
3. Then watch the brain's pairs: `took on mine: ...` with the estimate, and
   `finished mine: ... N ingots put away`. The second is the proof that the chain reached the end rather than
   stopping at the vein.
4. After a few trips the estimates should diverge between veins: that is the ledger, not a configuration file.

---

## Known rough edges

**A failure at the fire marks the vein.** The ledger is keyed by "work + patch", and the patch comes from the
vein (rightly: what is being learned is "digging here pays"). So if the ore never made it into a fire, the
caution lands on the vein although the forge was at fault. It fades in five minutes, but it is an inaccuracy.

**No more than sixteen sweeps per world.** After that, bots that have walked beyond the swept ground will find
nothing — and there is one line in the log saying so.

**The pickaxe decides who mines, not the class.** A gatherer is born with a pickaxe and a hatchet; anybody with a
pickaxe in the pack is a miner while it lasts. Deliberate: v1 had a list of archetypes permitted to work, and
adding a class silently excluded it from working.

**Every engine call was read out of the fork:** `Mining.System.OreAndStone.GetVeinAt`, `system.GetDefinition`,
`StartHarvesting`, `ore.OnDoubleClick` plus `bot.Target.Invoke`, `map.Tiles.GetStaticTiles`,
`GetStaticAndMultiTiles`, `GetMobilesInRange<Banker>`, `Banker.Deposit`, `pack.ConsumeTotal`, `box.DropItem`,
`Utility.InRange`, `Mobile.InRange`, `HarvestDefinition.GetBank(...).Current`, `MaxRange`.


## The island was never running out of ore

Patrick's order of 09.09.2026 named this as a question of design: *mining finishes 28% against 63–73% for
everything else, and three quarters of the failures are a worked-out seam — so what does mining do when the
island is exhausted?*

It was not exhausted. Three faults, found in that order, each one hiding the next.

**One. Nothing counted the two cases apart.** `emptied N rocks and found no more` was written whether the
engine's bank under the rock was empty or full. Counting them separately took four lines and settled the
question immediately: **16 rocks given up with the bank empty against 35 that still held ore.** Two in three
write-offs were wrong, and each one rested the seam behind it, so the board shrank all afternoon while the
log said the ground was giving out.

**Two. The seam rested for half as long as the engine takes to refill.** `BotGround.DrainedMs` was ten
minutes, chosen here. `HarvestBank.Consume` sets the refill at `MinRespawn + rnd × (MaxRespawn − MinRespawn)`
— ten to twenty minutes for ore — and it starts that clock **at the first swing on a full bank**, not when
the bank empties. So a rested seam came back onto the board at the earliest instant the engine could possibly
have refilled it, and usually before. It is now `BotOre.RespawnMs + 60000`, taken from the engine at start-up
and said out loud in the boot line.

**Three, and this was the whole of it: the dryness counter counted beats and called them swings.** In
`BotDig.Digging` the test *did the last swing produce anything* sat **above** the swing throttle. The
population beats every 100ms; `SwingMs` is 1000. So one real attempt cost up to ten increments of `_dry`, and
at `DryLimit` of six a rock was written off in **under a second** — routinely before it had been struck at
all. Everything above is downstream of that: the misses that looked like an empty vein, the seams rested for
nothing, the 28%.

Moving the whole judgement below the throttle took mining to **85% on the first window, with not one rock
given up that still held ore**. Over the following twenty minutes: 11 rocks given up, every one of them with
the engine's bank genuinely empty, and mining's failures reduced to reachability.

Two things were kept from the middle diagnosis because they are right on their own terms:

- `DryLimit` is now divided by what the engine says the bot's chance actually is (`BotOre.Chance`, floored at
  six and capped at `MostDry`). `CheckSkill(Mining, 0, 100)` means a bot with thirty mining misses seven
  swings in ten; six quiet *swings* would still have been a master's number.
- The engine's own bank is asked first, every swing. `Left <= 0` is the truth about a rock and costs a
  dictionary lookup; everything else in that branch is a backstop for the case where the rock is full.

**The lesson to carry.** A counter that says "N attempts in a row without result", sitting beside the
throttle for those attempts, is worth checking in both directions: what unit does the counter tick in, and
what unit does the event happen in? Both numbers here had been on the screen for weeks and had never been put
beside each other.

**Still open.** Twenty-seven percent of mining trips are *dropped* on the leg `carrying ore to a fire` — the
digging is done, the ore is in the pack, and the auction prefers something else on the way to the forge. The
ore is not lost; `unload` collects it. That is the `work-judged-before-it-can-pay` family and a separate
question from this one.


## Listening to what the harvest system says

Patrick's order of 09.09.2026: *a vein counts as worked out when the bot **sees** the message that there is
nothing in it.*

Everything before this inferred it — a run of quiet swings, or the engine's bank read from the side — and
inference was wrong about two write-offs in three. The engine had been saying it plainly the whole time.
`HarvestSystem` distinguishes six outcomes and names each one:

| it says | it means |
|---|---|
| There is no metal here to mine | the vein is worked out |
| Someone has gotten to the metal before you | worked out by another hand a moment earlier |
| You loosen some rocks but fail to find any useable ore | a missed roll; the rock is fine |
| You have moved too far away to continue | the swing was cancelled, not rolled |
| Your backpack is full, so the ore you mined is lost | the bank was charged and the ore destroyed |
| You have worn out your tool | no pickaxe |

**None of it could reach a bot.** `HarvestDefinition.SendMessageTo` calls `Mobile.SendLocalizedMessage`,
which is not virtual and writes straight to a `NetState` — and a bot has none. Six distinct answers, dropped
on the floor, while this file guessed at them from the outside.

So the engine got one event — `HarvestDefinition.Said`, raised before the send, altering nothing, recorded in
`engine-patches/HarvestDefinition-said.patch` — and `BotHeard` listens on it. `BotDig` now reads the sentence
before anything else: *empty* or *taken* writes the rock off outright, *full pack* takes the trip to a fire
rather than destroying more ore out of a bank it cannot receive, *out of range* does not spend the rock's
patience because the swing was never rolled, and *worn-out tool* ends the errand. The bank check stays
underneath as the guard for the almost.

The whole set is in the `The ground:` line, so a session's mining can be read as the engine's own account of
it:

```
the harvest system said: N times there is nothing left here, N somebody got there first, N a missed swing,
N moved too far to finish, N a full pack, N a worn-out tool, N something else
```

**And the swing itself is two seconds now**, by the same order — `BotDig.SwingMs`, up from one. A harvest
resolves nine tenths of a second after it starts, so a second left almost no margin; two is a pickaxe swung
at the pace of something with arms.

### The axe, by the same rule

`BotChop.StallMs` carried a note that was the plainest possible statement of the problem: *"a tree that has
been cut out answers every swing with nothing and looks exactly like a tree that is simply unlucky, and the
engine says which only by silence — the message it would send goes to a client this bot has not got."* Half a
minute of swinging at a stump, every stump, because the sentence had nowhere to land.

It lands now. `BotChop` reads the same ear against the lumberjacking definition, and the clock is a backstop
behind it — counted apart, so a day when the clock starts firing again is a day the ear has stopped working:

```
N trees given up because the engine said they were cut out against N given up by the clock alone
```

First ten minutes after: **462 against 0.** Chopping finished 76% of what it took on, against 42–52% earlier
the same day.

### An axe the body can hold

`BotTimber.Tool` used to hand over any axe in the pack, and the engine will not put a weapon in the hand of a
player weaker than the weapon asks. A healer with strength 25 carrying a double axe (45) was offered the woods and
failed on the first beat, *"it cannot get the axe into its hand"*, five times in one second. The tool is now an
axe `BotMobile.Suits` says the body can hold — the re-arm's own question, so the two cannot disagree — and the
proposer counts the rest apart from the bots with no axe at all:

```
N asked to cut wood (N not asked for carrying only an axe too heavy to hold): ...
```

### Scoping, which the ear needed and did not have

The first cut recorded one row per bot and nothing else, and the instrument caught its own fault within
twenty minutes: **1,105 "there is nothing left here" against 24 rocks written off.** Most of them were the
woodcutters, and the miners were reading them — a bot that had been cutting wood a minute earlier had its
axe's verdict applied to the first rock it swung at.

Two guards, answering different questions. `BotHeard.Last` takes the definition the caller is asking about,
so a sentence about another craft is not an answer; and `FreshMs` requires it to be younger than the swing
interval, so a sentence about an older swing is not an answer either. Both are counted as *N sentences were
passed over as being about another craft or an older swing* — a number that should stay lively, because a
nought there would mean the guards are not being reached.
