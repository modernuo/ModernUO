# Wars with an end, and guilds with a seat — Patrick's order of 13.09.2026

Four things were asked for, in this order, and this file is what was built for each, what it was measured
against, and what is left open. Where the code disagrees with this file, the code is right.

1. A way to watch the shard while somebody is working on it.
2. Wars that stop: a cooldown on declaring, a victory condition (fifty dead or ten thousand gold of
   plunder), a cooldown on ending, and defence of the guild's own land first.
3. The bots live somewhere other than the graveyard: south, east and west as well; one guild moved south.
4. More responsiveness and more boldness in what the bots do.

---

## 0. What the twelve hours before this looked like

`logs/session-2026-09-13_02-12.log`, 02:12 to 14:21, eighty bots, five guilds, four halls.

| | |
|---|---|
| wars declared / ended | 12 / 5, seven standing at the end |
| worst opinion between two guilds | -200, the floor of the scale, from the first hour to the last |
| what drove it | 4,337 refusals to move along at -12 each; 12,354 trespasses at -0.5; 2,025 trades at +0.5 |
| bots killed by other bots | 3,054 — about 250 an hour, one every fifteen seconds |
| deaths at work of all kinds | 2,900; `died doing quarrel` 1,687 |
| quarrels | 22,007 taken, 10,446 finished, 9,507 failed, nearly all "no way through to <bot>" |
| where bots rose again | 3,597 revivals, every one at 1440,1470 |
| where the halls stood | 1376..1470 × 1398..1416, four yards of forty tiles on top of each other |
| companies | 4,787 sent to fall in, 64 arrived: the company disbands eight seconds after its fight and the reach was forty tiles |

A war could begin — the opinion crossed the threshold within minutes of boot because every yard touched
every other — and could not end: the only way out was the same opinion climbing back above -20, and nothing
on the island moved an opinion upward faster than the bailiff moved it down. The exile that follows a lost
war (`BotExile`) had fired once.

---

## 1. Watching it

Three instruments, none of them new numbers — all of it is what the shard already prints.

- **`shard-status.sh`** at the repo root: one screen. Whether the process is up, the wars standing with
  their score and clock, blood in the last hour, where the halls and seats are and where bots have been
  rising, the work line, the newest alarms. `bash shard-status.sh`, or `WINDOW=30 bash shard-status.sh`.
- **The alarm channel**, `logs/alerts.ndjson`, now carries `kind: war` notes — declared, won, drawn — so a
  watcher tailing it sees every war begin and end without reading the log.
- **Argus by hand**: `do wars` (every war standing, score and clock), `do seats` (where each guild lives and
  how far its hall is from it), `do seat <guild> <x> <y>` (move a guild).

For a session where somebody is working on the code while the shard runs, the shape that worked on the
night of 13.09.2026: two tails on the log — one on the war/hall/seat lines, one on the alarm channel — that
wake the watcher only when one of those lines appears, plus the status script by hand every half hour.
Nothing polls; the shard speaks first.

---

## 2. Wars

New file `BotEstate/BotWar.cs`: the ledger. `BotRegard` still decides *when* — an opinion below `Enmity`
— and asks the ledger *whether*. The engine still holds the war (`Guild.IsWar`) and still does the
enforcing.

| rule | dial | default |
|---|---|---|
| won at this many of the enemy dead | `BotWar.Kills` / `WarKills` | 50 |
| or at this much gold's worth taken off their corpses | `BotWar.Loot` / `WarLoot` | 10,000 |
| no peace by opinion before | `BotWar.LeastMs` / `WarLeastMinutes` | 15 min |
| judged on blood when it reaches | `BotWar.LongestMs` / `WarLongestMinutes` | 180 min |
| truce between the pair afterwards | `BotWar.TruceMs` / `TruceMinutes` | 60 min |
| one declaration a guild per | `BotWar.DeclareEveryMs` / `DeclareEveryMinutes` | 30 min |
| wars a guild may stand in at once | `BotWar.MostWars` / `MostWars` | 1 |

Plunder is counted where a corpse is gone through (`BotPickings`): the coin as coin, the goods at what they
were listed for. When a war ends, by whatever route, **both opinions between the pair go back to nought**
(`BotRegard.Settle`) — a war settles the quarrel that started it, and without this the pair sat at the floor
and re-declared the minute the truce lifted. Peace by opinion now needs **both** sides above `Amity`; the
first cut ended a war when either side's opinion rose, and the side that never hated anybody was above the
line already, so one trade landing on the wrong pair ended a war seven seconds after it was declared.

Opinions also forget: `BotRegard.Mend` (1.0 a drift, every five minutes) moves every opinion a step towards
nought. Small on purpose — a dozen an hour against one refusal at twelve — so it decides nothing while
something is happening between two guilds and everything once nothing is.

At boot, wars the engine still holds from the last session and the ledger knows nothing about are ended
(`BotWar.Reconcile`); the session starts at peace.

**Defence.** `BotFeud.Watch` sweeps every war every five seconds for an enemy standing on our ground — the
hall's yard or a claimed square, or claiming one — and files it as a threat. `BotFeuder` answers a threat
before the guild's call and before anything the bot can see, from `BotFeud.Defend` (250) tiles, with a
quarrel priced at `BotQuarrel.DefendPrior` (700 a minute, above a company's hunt). Still a price and never a
command: the floor that protects paid work still holds, and so does the health check.

**The battle is a company's, not a hundred duels** (ninth build, 22:51, Patrick's fifth order of the
evening). The first member to answer a call or a threat forms the guild's war company — the squad the hunts
use, charged so it keeps its members through the lulls, with a ceiling of a whole guild — and points it at
the enemy; every member after it rallies to it (`BotRally`, alongside the company like a hunt's enlistment)
and the company's own beat does the fighting: station, formation, blow, bandage, spell. `BotFeud.Watch`
points a company whose enemy is down at the next enemy on its ground or its call, and lets a company go
when it has nobody to fight and no war to fight in; the end of a war lets both companies go. A member that
cannot form or join one falls back to a quarrel of its own, as before.

**The loser** still moves its hall out of the winner's yard (`BotExile.Sentence`), now with an explicit
winner rather than a second book of blood; a drawn war moves nobody (`BotExile.Draw`).

---

## 3. Seats

New file `BotEstate/BotSeat.cs`. One point per guild, in `bot-estate.json`:

```json
"Seats": {
  "The Crown":   [1438, 1398],
  "The Blade":   [1376, 1410],
  "The Needle":  [1500, 1900],
  "The Hammer":  [1800, 1520],
  "The Lantern": [1250, 1450]
}
```

Everything follows from the seat: the plot search spirals out from it (`BotPlot.Find` keeps one search per
origin now); a member is born and rises beside its guild's hall, or at the seat until it has one
(`BotPopulation.TryPlace` via `BotSeat.Home`); the homeward walk goes there (`BotHomer`); and a hall standing
further than `SeatSettled` (120) from its seat is carried to it by the same errand that carries a beaten
guild out of the winner's yard (`BotRemover`, `BotRemove`), merchant and stock along with it.

The three points were probed through Argus before they were written down: a body stands on all three,
outside every town region. Britain's town region runs x 1093..1740, y 1498..1907 and the graveyard's belt
is x 1313..1437, y 1421..1543, which is why the four halls had been where they were.

`MaxHalls` is 8 in the file now; it was 4 in the code against five guilds, so The Needle could never build.

**Measured on the first run (session 20:57):** both far halls were carried within eight minutes of boot —
The Lantern to 1250,1422 at 21:01, The Hammer to 1808,1550 at 21:05 — and the first thirteen deaths rose
in four different hundred-tile squares instead of one.

Set by hand on a live shard: `do seat The Needle 1500 1900` through Argus; kept across restarts in
`Saves/BotSeats`.

---

## 4. Responsiveness and boldness

What was changed, each with the number that argued for it:

| what | was | now | why |
|---|---|---|---|
| `BotEnlist.Reach` | 40 | 32 | 4,787 sent to fall in, 64 arrived; companies disband 8 s after a fight, 40 tiles is 8 s of walking; 24 sent nobody at all |
| `BotEnlist.Left` | — | 0.4 | no walking to a fight that is nearly won, unless the company is under three |
| `BotFeuder.Fit` | 0.6 | 0.5 | 54,298 refusals to start a quarrel for being hurt |
| `BotFeud.Answer` | 60 | 100 | a guild's call reached one and a half yards |
| `BotFeud.Defend` | — | 250 | an enemy on our ground is answered from most of the island |
| `BotRescuer.Reach`, `BotCry.Carries` | 40 | 60 | a guildmate's cry carries further |
| `BotSquads.Note` | any attacker | not a bot of a guild we are at peace with | company 19 was handed five bots of our own to deal with in a quarter of a minute — splash from potions and area spells |

Left as dials, not moved, with the reading that would argue for moving them:

- `BotDelver.Odds` 1.5 — a party of the four best fighters is ~13,500 against the Orc Caves' worst at 6,052
  and Wrong's at 14,912; opening Wrong needs either a stronger band or odds under 0.9.
- `BotHunter.FitAt` 0.8, `BotSlay.FleeAt` 0.4 — the hunt's own courage; 75,876 "too hurt to be any help" in
  twelve hours of permanent war says less about courage than about being at war.
- `BotWill.TrekLimit` 600 beats — the walk backstop measures straight-line closing, and a walk round Britain's
  walls can stop closing for a minute while making progress; the remover, the delve muster and the stake all
  hit it on the first run. Not moved: it is what ends a stuck walk, and the retry succeeded each time.

---

## 5. Speed

Sampled on the running shard, 20:59–21:01, eighty bots: **20–26 % of one core, 650 MB.** The twelve-hour log
says where the bot side of that goes: `1,317,804 searches, 5,647,380 ms total (4.29 ms each, worst 61.6 ms)`
— ninety-four minutes of the twelve hours, so the pathfinder is about two thirds of what the bots cost, and
`256,744 asked for the whole ceiling because they were not closing` says most of it is spent on searches that
do not arrive. That is the one thing worth optimising if speed is ever the problem; it was not on this night.
The engine's own loop profile is readable in-game with `[LoopStats`.

---

## 5a. Second run, 21:07–21:30, forty bots' worth of blood in twenty minutes

- The Lantern declared on The Needle at 21:16:47 over refusals to move along; **three sightings of an
  enemy on its ground sent 98 members to defend it**, from up to 250 tiles; the score at 21:27 was 4:3 dead
  and 1108:1033gp of plunder. Ten minutes later `The Lantern would declare war on The Crown (regard -115.5
  over blood) but for The Lantern declared one 6 minutes ago` — the cooldown held.
- 18 bots killed in 20 minutes against 250 an hour before: one war at a time is the whole difference.
- Where the killing is: (1411..1413, 1497..1498, z 10) — the east fence of Britain's graveyard. The new
  message says why quarrels fail there: `no way through to Emrys at (1383, 1504, 10) in Britain Graveyard`,
  12 of 32 refusals inside the graveyard, the rest at coordinates just outside it. **The graveyard is a
  battleground the pursuer cannot path into.** Open; the coordinates are in the log now.
- Enlist with reach 24: 4,728 asked, 0 sent, while 71 companies formed. Reach 32 in the third build; if it
  is still nought, the reach is not the reason.
- Ten bots revived at their guild's hall or seat; 16 revivals in five different hundred-tile squares.
- Opinions forget: 39 steps of mending in 20 minutes against 17 refusals to move along.
- A restart does not carry a war over: the engine disbands a guild whose leader is gone and `BotGuilds.Form`
  revives it, and disbanding clears its enemies — so `BotWar.Reconcile` has nothing to do and every session
  starts at peace. `do save` through Argus was verified at 21:30:35.

## 5a'. Third run, 21:30–21:40: a regression caught in ten minutes, and what it taught

With the enlist reach at 32 the shard fell to 21 % of work finishing inside five minutes: 2,830 enlists taken,
none finished, 2,966 of `falling in with company 1, 1 of them` ending "the company was gone before it got
there" in the same second they were taken. `BotEnlist.Standing` refuses a company of one; the offer did not,
so the errand's refusal went straight back into the auction, every beat, for one bot after another. The fix
is the same bar in the offer (`BotEnlist.Lonely` counts it). The shape is the one this project has met before
as "an offer is not an errand": a proposer and its deed asking two different questions is a loop, and the
loop is invisible in the summary except as a completion rate that fell off a cliff. Caught by reading the
first five-minute summary after a deploy, which is the rule that found it.

## 5b. Speed and the shape of decisions — the pass Patrick asked for

Measured, not felt. The loop's own cost is now printed every five minutes in the clock line: milliseconds
spent in the population's tick, the worst single tick, and the split between deciding and walking
(`BotBeat.SpentMs`, `BotWill.SpentMs`, `BotWalk.SpentMs`). Before this the only figure was the pathfinder's
own, and "are the bots slow" had no number to answer with.

**What was changed in the pathfinder, and why it is the right place.** Two thirds of what the bots cost was
searching, and most of that was searches that did not arrive: a journey that had drawn a plan and got no
closer asked for the whole sixty milliseconds on every plan after that, up to twelve times, with the far side
already probed and found to be the world after the second. Now (`BotWalk.Plan`) the third plan is one search at
the stranded ceiling (150 ms) and its answer is taken: a way round, or the errand ends. A starved search is
not a verdict. And a chase redraws its plan when the target has drifted more than a quarter of the plan
left to walk, not two tiles (`BotJourney.NeedsPlan`) — forty tiles out, the first thirty tiles of the plan
are right whatever the target did. The bill is itemised in the `Getting about:` line: which errands burned
the ceiling and which were lost as hopeless, top five each. The measure that says whether this was right is
the same one the previous tuning was judged on: work finished in minutes five to ten of a run, beside the
search time per hour.

**Decisions.** The auction is polled — a busy bot looks for better work every fifteen seconds, an idle one
every few — and everything urgent had to wait for that poll. The right shape is events: a bot is marked due
the moment something it would act on happens. That existed for one event (a job finishing) and now for a
second (an enemy on the guild's ground, `BotFeud.Look` marks the whole guild due). The next three are the
same one-line change each and are worth making as they are measured: a guildmate's cry (`BotCry.Raise` →
guildmates within reach), an order of ours being filled (`BotAuction` → the buyer), a company forming within
reach (`BotSquads.Form` → fighters nearby). None of them costs anything until it fires.

**The defect shape this day kept finding is two numbers on one shelf, set on different days**: enlist reach
against the company's idle clock (40 tiles against 8 seconds), the war threshold against the rate that
reaches it (−100 against −12 a refusal, six a minute), peace against the side whose opinion moved. Every
dial that only means anything beside another dial should carry that other dial's name in its own comment,
and the three above now do.

**What is worth doing next, in order of what it buys:**
1. A coarse connectivity map over the island's thirty-tile squares — which squares border which passably —
   built the way the halls map was built (walked once by Argus, saved). It answers "is there a way at all"
   in one lookup for a target three hundred tiles off, which is exactly the search the ceiling is spent on
   today, and it would make the graveyard's fence a known edge rather than a surprise every quarrel.
2. The summary as lines a person can read: `Estate:` was 3,000 characters and every number in it was
   invisible for being there; `Wars:` is its own line now, and the supplier, the shelf and the plots should
   follow. A line nobody can read is a line nobody reads.
3. Source control. `Projects/BotAIv2/` is untracked in this fork — seventy thousand lines with no history
   but the log's. The mirror repository exists; every evening's work should end with a commit there.

## 5c. Fourth and fifth runs, 21:41–22:00: what "no way through" actually was

- Asked directly through the new Argus verb `do road 1420 1500 1383 1504`, the pathfinder found the road
  into Britain's graveyard in 0.1 ms, thirty-six tiles. There was never a wall. The quarrels that ended "no
  way through to Faron" had drawn twelve plans without getting nearer - a target walking away as fast as the
  bot walked after it - and the will printed every walk ending, proof or chase or stall, with the same
  words. The message now names the ending: `could not get nearer to`, `could not take a step towards`, or
  `no way through to` for the pathfinder's own refusal. The instrument (`road`) stays.
- Fourth run, fourteen minutes: 998 taken, 661 finished, 193 failed; a war (Lantern on Crown) declared at
  21:50, 46 defenders sent from two sightings; 2.4-5.7 ms a search with the chases running; 6 escalations,
  2 found a way round, 4 ended an errand nine searches early. The whole ceiling was burned by `looking for a
  fight` (737) and `taking my place in the ranks` (354) before any chase - the prowl and the formation are
  the next two to look at, in that order.
- Enlist with the company-of-one bar: 3,227 asked, 10 sent, 1,233 passed over a company of one. The loop is
  gone and the mechanism barely fires; whether that is right depends on how often a real fight has room,
  which `formed and disbanded` (36 and 30 in fourteen minutes) says it rarely does for long.

## 5d. The clock line, first reading (fifth build, 21:56–22:01)

```
2923 looks, 112911 turns; 28950ms of the loop in all (9.90ms a look, worst 467.9ms),
of which deciding 1677ms over 112911 turns and walking 26612ms
```

Deciding costs 0.015 ms a turn - nothing. Walking is 92 % of what the population costs the loop, and all of
walking is the pathfinder (26,632 ms of searching in the same window). Ten milliseconds of every hundred, on
one core, for eighty bots. The bill for the whole ceiling in that window: `taking my place in the ranks` 179,
`station` 100, `looking for a fight` 77 - a three-tile move to a formation slot somebody was standing on,
handed the island-crossing sixty milliseconds for "not closing". Sixth build: the whole ceiling is bought in
proportion to the distance (eight times the tile charge, sixteen milliseconds at the least, sixty at the
most), and a tick over 100 ms says which of its nine segments took it and whose turn was the costliest
(`A slow tick:` line, once per five minutes). The worst tick of 468 ms is the next thing that line will name.

## 5e. Sixth build, 22:05–22:20: the cheap search broke the shops, and was reverted

The distance-bounded ceiling did what it promised to the numbers — 2.54 ms a look against 9.90, 0.42 ms a
search against 5.11 — and broke restocking: **1,362 of 1,597 errands failed in ten minutes**, every one
"no way through to Abira inside a house". A shopkeeper stands ten tiles off through a wall; the road runs
round the shop to its door; a sixteen-millisecond search stops at the wall; two of those send the far side
to look; the far side finds the counter's enclosure and seals it in the reach ledger; and every later walk
to that shop is refused without a search (`1153 refused outright`). Reverted at 22:20 to the whole ceiling
for any journey that is not closing — that is what finds the door. Kept: the itemised bill, the one
escalation after the far side, the follow slack, the clock line and the slow-tick line.

The lesson is the one the previous tuning of this exact line had already written down and I read as
history rather than as a rule: **a search that is cheap by distance is a search that stops at the first
wall, and this island's shops are all walls with one door.** The reach ledger's proof compounds it — one
wrong "enclosed" is a shop nobody visits until the restart. Whoever touches this next: the number to read is
`failed at restock` in the first five-minute summary after the deploy.

## 5f. Seventh build, 22:18–22:30: the revert held, and the next thousand failures were a cave

Restock was back to nothing worth counting; the loop cost 1.67 ms a look. The failures moved: `failed at
stake` 1,027 in twelve minutes, 858 of them "no way through to (1455, 1455, 0)" by two bots, Isolde and
Faron, who were standing in the Orc Caves. Roderic's delve had taken them down at 22:19; the company let them
go at 22:30 "for doing nothing" - a rule written for the island, applied three hundred tiles under it - and
the delve ended at 22:32 without them, surfacing only the members its own book said were still down. The
island offered them the guild's stake, the cave is a proven pocket, every plan was refused, and the rescue
refused to carry anybody who was "delving". Three fixes in the eighth build: a charged company keeps its
members (`BotSquad.Release`); a settled party surfaces everybody its dungeon still holds, whatever the book
says; the rescue carries a delver its company has let go. And the staker asks the reach ledger before
offering the square or choosing it (`BotHolder.Walled`, `Unreachable`), which is the offer-is-not-an-errand
rule applied where it was missing.

## 5g. The first battle fought as companies (ninth build, 22:53), and what it showed

The Lantern declared on The Blade at 22:53:56. Two seconds later Kerrin saw Isolde on the Lantern's ground
and formed the guild's war company; three mates took `rally` in the same second and the company was
fifteen strong at 22:55:51 — Kelda, Gwendra, Elspeth, Brannoc, Faron, Lorcan, Hollis falling in by name.
Joss formed The Blade's company fifty seconds later and had twelve. They fought as companies: 72 spells
from the back ranks, 49 heals by the medics, the Lantern's company bringing down Piers, Bertram, Wynn and
Selwyn in turn and dividing each corpse among ten to fifteen. Fourteen dead in seven minutes at the Blade's
hall; the war's book read Lantern 5, Blade 2.

What the battle showed in its first minute: `Isolde fell in with company 18` — Isolde of The Blade, in The
Lantern's company. The hunt's enlistment offers any fighting company with room, and a war company is one;
thirty seconds later the company was `dealing with Kerrin`, its own founder, because the enemy inside it
was being hit by him. Tenth build: nobody at war with a company's leader may join it (`BotSquads.Join`,
the one door in), a company never engages one of its own (`BotSquad.Has`), and the hunt's enlistment passes
the enemy's company over. Eleventh build: the company's share-out of an enemy's corpse counts towards the
war's plunder (`BotSpoils.Share` → `BotWar.Looted`); before it only a lone bot's pickings did, and the
war's plunder read nought through 611gp of it.

## 6. Open, in the order they are worth taking

The eighth build (22:34) is the one left running: 700 of 1,042 pieces of work finished in its first twelve
minutes, no failure above fifty, a war standing with the rules on it, and the stragglers' cave empty.

1. **The pathfinder's bill is now one errand's.** Twelve minutes of the eighth build: 9,916 searches, 99.9 s
   of loop, 10.07 ms each, 3,622 partial; `looking for a fight` burned the whole ceiling 955 times, `station`
   277, `taking my place in the ranks` 195. A prowl walks at the middle of a dangerous square, and a square's
   middle can be a mountain top; the refused-ground ledger learns it, but only after twelve plans. Read
   `Getting about:` and the clock line (18 ms a look when prowls are running, 1.7 when they are not) before
   deciding whether to make a prowl ask the reach ledger, choose a reachable tile in the square, or accept
   the cost.
2. Enlist: 10 sent of 3,227 asked with the company-of-one bar; companies live seconds. Decide whether the
   mechanism should exist.
3. The war rate: one declaration inside ten minutes of every start, over refusals to move along at −12;
   the rules bound the count, the price decides the pace. Read `Wars:` over a night.
4. `Projects/BotAIv2/` is untracked. Commit the evening to the mirror.
5. The slow-tick line's first answer every start is Roderic's 270–290 ms on `delve` — the party's muster
   at boot. Once a start is one thing; if it recurs the line will say.


## 7. The night of 13→14.09 (builds 13–17): two wars run to the end, five defects under them

Patrick's night order is in `NIGHT-2026-09-13.md` with the scale and every round. What it changed here:

- **Wars end.** Two wars ran declaration → companies → 25 dead → verdict → exile in 12–13 minutes each
  (02:30 Crown→Blade, 02:55 Blade→Lantern), with nothing done by hand; reaction from a sighting on a
  guild's own ground to the first member moving is 0–1.5 s, from a declaration to the first company
  under 5 s. A third (Lantern→Crown, declared 51 s after the Lantern lost) was ended by a restart.
- **The war ledger survives restarts** (`BotWarStore`, `Saves/BotWars`): wars with their clocks and
  scores, truces, declaration clocks, owed moves. Until build 17 every restart was an amnesty.
- **The war company was born in the proposer** and dissolved whenever the rally lost its auction (15
  formations in 10 minutes). It is formed on the rally's first beat now (`BotFeud.Form`), and it stands
  while it is in the list with anybody in it, not only while its leader is alive (`BotFeud.Standing`).
- **Unpaid deeds were quartered.** `BotCommons.Corrected` measured rally, stake, evict, hire, hall
  against takings they never have; the typed claim stands (`BotAppraisal`). The flip side: unpaid work
  learns nothing, so a proposer's gate is its only defence against a loop — the hire loop (two keep-backs
  on one fund, 4,365 failures in 43 minutes) and the stake crowd (nine on one square) followed within
  minutes and are gated (`BotGuilds.Keep => BotEstate.Keep`; `BotHold.Spare`).
- **Stalled steps counted as proven refusals**; nine bots were teleported out of crowds and fights in
  twenty-two minutes. Only Refused and GaveUp count now (`BotMobile.Beat`).
- A scribe with a full pack was offered a seam (`BotDig.Burdened`); a chop forgave "out of range" on
  every swing and stood at one tree for 220 swings (`BotChop.AdriftMost`); the seat line no longer calls
  an exile's seat "by hand"; the watchers' trade table marks unpaid work so "rally is a drain" stops
  being a finding.

Open after the night, replacing the list in §6:

1. **The pathfinder's bill** (unchanged): build 17 at ten minutes, 7,732 searches at 11 ms, 3,046
   partial, the ceiling burned by station 351, looking for a fight 314, ranks 188, to a fire 140. 15.5 ms
   a look and falling. Same three choices as §6.1.
2. **Whether a lost war should cool the loser's own declaration clock.** The Lantern declared on the
   Crown a minute after losing to the Blade. Lawful; Patrick's call.
3. **Two overloaded bots stood for eight minutes** (Merrick 242/240, Alden 245/222, both 0 stamina):
   the appraisal's load factor of 0.02 comes out of the fifth root at 0.46, so cook at 163 beats an
   unload, and the auction's "no-step work first" rule only bites when an unload is offered at all —
   which for both it was not, and the door's `bot` verb does not say why for a busy bot. Instrument
   first: print the vetoed offers for a busy bot.
4. Companies whose leader is underground while a revived member stands on the surface (station at
   5318,1315 from the Crown's hall); the squad's `Stranded` release should have let them go.
5. `Projects/BotAIv2/` is untracked. Commit the night to the mirror.
6. The squad: eleven findings in forty minutes under build 15, one true (Heimdall's "stuck, Emrys" was
   the claim jam). The template is the model's, not the prompt's; the findings section of the prompt
   could carry the last five findings' outcomes ("checked by hand: false") so the model sees its own record.

### 7a. Builds 18–23 (03:54–04:45): the war across a restart, and the war across the map

- A restart lost the engine's enemy lists (bots re-created, guild disbanded and raised again) while the
  ledger kept the war: `Reconcile` now re-asserts every restored war to the engine on the live guild
  objects (`BotGuilds.Named`) and logs it; the feuder counts the two books disagreeing.
- A restart zeroed every opinion, so a standing war "ended in peace" at its fifteen-minute floor with a
  3 : 2 score and an exile: the store carries opinions (shape 2), and a peace names a winner only past
  `BotWar.PeaceMargin` (5) kills.
- A war between halls 400 tiles apart never met: the sortie (`BotFeud.Sortie`/`Foe`, `SortieMs` 3 min)
  sends the company after the nearest enemy, the leader marches at the focus (`BotRally.Marched`), and
  the feud's watch re-points an idle company the same way. First march led at 04:38; the rally is no
  longer offered to a bot in another company.
- A war company's charge is the feud's (`BotSquad.Warring`); inheritance prefers a survivor holding the
  company's errand. Four re-formings a war → none.
- An immobile bot's unload is `Unpaid`; a busy bot's reason line names its first refused offer.
- A member with a dozen proven refusals to its station leaves the company rather than being carried home.

Open, added: whether the sortie should also pick a target when the enemy is in a town (`Quarrel` refuses
it); the seat geography of the Crown and the Blade (62 tiles apart, land reach 40); a lost war cooling
the loser's own declaration clock.

- **The station cap (build 24, 05:00).** A company re-issues every member's place every beat the anchor
  moves; a station that has stopped closing is now held to `BotWalk.StationCeilingMs` (15 ms) instead of
  the whole sixty. At ten minutes: 13.7 ms a look against 24.4, two slow ticks against 475, partials a
  third instead of half. Errands keep the whole ceiling (the shop-door rule of 13.09 stands). The prowl
  is now the top burner and the open question of §6.1 is unchanged for it.
