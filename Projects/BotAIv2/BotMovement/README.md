# Movement

Path, road and step. The most expensive part of the project: in the first version it accumulated more measured
defects than everything else put together, and almost all of them turned out to be the price of having no real
pathfinding.

The analysis all of this is built on is in `RESEARCH.md`. This file is only what came out of it.

## Layout

| File | Responsibility |
|---|---|
| `BotArrival.cs` | what "arrived" means. One definition for the whole project |
| `BotStep.cs` | one tile: the step mask, items, diagonals, the height tolerance, footing |
| `BotAvoid.cs` | ground this plan works around: somebody else's tile, a dangerous square, the towns for a red, and (03.10.2026) the reach of creatures the walker has seen, priced per step rather than shut |
| `BotSight.cs` | what a bot sees within `SightTiles` (20) once a second: a creature that attacks on sight and that the bot, its company or our own near it cannot beat at its kind's price (`BotQuarry.Priced`, ×`Odds` 1.5) is a danger; its reach (`RangePerception` + `Margin`) is priced on the plan (`StepCost`) and on the route (`BotDanger.SightCost`), the route and the plan are drawn round it, the walk waits out of reach while its goal or its only way is in it and turns back after `WaitMs` (30 s); a bot at work standing leaves it when a danger comes within reach with a line to it. Never in a town, underground, in a company's line, in a fight; a company, a foe of the bot's choosing and a flight only go round. Counted on `Getting about:` (the `sight:` clause) |
| `BotPath.cs` | a tile A\* with a ceiling in **time**. Three outcomes |
| `BotReach.cs` | sealed pockets of ground, harvested out of failures. Refusal in one comparison |
| `BotRefused.cs` | places the population could not get to, remembered for everybody and cleared by anyone arriving; written when an errand ends unreached and, since build 51, when a proven refusal sends the work elsewhere (`BotWill.Rerouted`); read by the seam chooser too |
| `BotBarred.cs` | ground nobody may ever choose: stated rather than measured, never expiring, never cleared by arriving; four boxes since build 64 — the walled pocket east of Britain, the traps north and south-west of it (sized from where bots were carried home), and the plague beast's bog until groups of thirty to fifty can go there (Patrick's order). Since 29.09.2026 also the **quarantines**: a death at the plague's hands or a creature's past `QuarantinePower` closes `QuarantineHalf` (45) each way for `QuarantineMs` (2 h), and a **killing field** — `KillingDeaths` (3) bot deaths within that distance inside `KillingWindowMs` (30 min), whoever killed them — closes the same way; the home ground, halls and seats are never closed. Kept across restarts in `Data/bot-quarantines.json` (not under `Saves`, which the engine archives wholesale at every save); drawn on the dashboard's `barred` layer. Since 03.10.2026 each quarantine keeps what killed in it (strongest of each wild kind) and whether the plague or a killer past `QuarantinePower` closed it (`Closing`, read by the clearings); a company's work lifts one only when its middle lies on the walked ground, nothing alive in the box is reckoned past `EmptyOdds` (×1.5) a lone bot at its kind's price, and the plague did not close it. |
| `BotDanger.cs` | the danger a route is priced with, node by node, for the walker that asked (`Cost`, the hook the planners ask: blood +60 tiles, a record past the walker ×1.5 +300, patrols by level, barred +800, a seen danger's reach +200), and the road weighed before far work is offered (`Road`: the straight line every 32 tiles first, as on builds 388–402) |
| `BotDangerRoute.cs` | (06.10.2026) where the line refuses, the road the planner would draw is judged instead (`RouteJudged`; off is the old rule): an A\* over the navigation graph at `Cost`'s prices for a walker of that strength, inside the line's box + `RouteDetour` (240) and `RouteMost` (4000) nodes, at most `RoutePerSecond` (12) searches and `RouteMsPerSecond` (15) a second (a walker holding work only within `RouteBusyShare` of them), the verdict kept `RouteKeepMs` (2 min) per origin square of 32, goal square and band of strength. A node past 32 tiles from both ends that reads beyond the walker is not walked through: a patrol at half its fight, a record with a death in `RoadFreshMs` (3 h), creatures counted now or dead of an age unknown since the boot, against ×1.5; an older record past ×`RoadStaleDare` (4.5). A clear road found within `RouteSlack` (300) tiles of the straight distance is the planner's own road: offered (`RouteOffered`), and the walk and any death within `RouteDeathMs` of a leg on it are counted (`RouteWalked`, `RouteDied`); dearer, a detour the planner would not draw (`RouteDetours`); none in the box (`RouteNone`), or not judged: the line's answer stands. Counted in `Getting about:` after the roads, by cause and by trade |
| `BotRoads.cs` | the road distance from home to every tile within `Reach` (640) each way, drawn once per world load by a breadth-first flood that steps as the planner does, a slice of the loop at a time; proves a lower bound on any bot's road to a place, counted by the hunt's darts (`BotHunter.Roundabout`, dial `ReadsRoads`, build 63) |
| `BotJourney.cs` | the A→B obligation. Lives on the bot |
| `BotWalk.cs` | the moment of the step: doors, occupied tiles, casting, a last line of defence |
| `BotMovementConfig.cs` | `Configuration/bot-movement.json` — budgets and deadlines only |
| `BotMovementModule.cs` | module, phase `World`, depends on nothing |
| `BotErrand.cs` | one thing a bot is trying to get to |
| `BotChart.cs` | The coarse chart of the ground round home: squares of tiles, the ways across each square's edges, and which of those ways join inside the square — so that a wal |
| `BotFooting.cs` | Tiles a walk has proved nobody can stand at, kept for everybody for half an hour. |
| `BotGates.cs` | the world's teleporters read as gates between the graph's components, Dijkstra over them, the first gate handed to the journey; the entrances the world lacked raised from `Data/teleporters.json` (29.09.2026) |

---

## "Arrived" has one definition

How much tolerance did we have? In the first version: **it varied, and in two places at once.**

- `BotBrain.Arrived(goal, range)` and `BotNav.AtGoal(..., tolerance)` — two copies of one rule that had to be
  kept in agreement by hand;
- the tolerance itself was an anonymous `int` threaded through six layers of calls, meaning something different at
  each: a doorway, a counter, a creature, a leg of a road;
- long journeys quietly used **2** and everything else **1**, and nowhere was it said why;
- the height check was **switched off** if the tolerance was more than one tile.

In v2 it is `BotArrival` — a type rather than a number:

| Value | Means | For |
|---|---|---|
| `BotArrival.Exactly` | exactly this tile | standing **on** a thing: a corpse, a vein, a rock |
| `BotArrival.Beside` | the tile or any of the eight around it | **the default for almost everything** |
| `BotArrival.Within(n)` | within n tiles | places rather than things: "get to the market" |

`Beside` — "one tile around" — is the default not out of taste: a bot required to stand on the very tile a
shopkeeper is standing on has **nowhere to arrive**, because the shopkeeper is there.

**Height is always checked.** That is the difference from v1 and it closes a whole family of defects. There the Z
check was skipped when the tolerance was more than a tile, and every distance check in the project ignored height
altogether. The bill came in twice: a wraith on a crypt roof — three tiles horizontally, twenty units vertically —
read as ideal prey, five bots declared a party, marched, "fought" and took off not one point of health; and a bot
on a bridge counted as having reached the road underneath it.

Sixteen units rather than zero: a tile's floor is not flat, Z bounces constantly while walking open ground, and
sixteen is a man's height — the engine's own notion of "the same storey".

One function, `Reached`, called by both the search and the stepper. **A plan that thinks it has arrived while the
bot thinks it has not is a form of infinite loop.**

---

## Three outcomes from a search, and the difference between two of them is load-bearing

| Outcome | Means | What the bot does |
|---|---|---|
| `Reached` | there is a way | walks |
| `Partial` | time or ground ran out **inside the frame** | walks to the best tile reached and asks again from there |
| `Sealed` | the ground genuinely ran out, provably | drops the goal immediately |

`Sealed` is a statement **about the world**, not about the search. And "get as close as possible" is exactly
harmful here: the reachable tile nearest the goal behind a wall is the tile **at the wall**, and walking there to
ask again is the fence-hugging this was all written to end.

So `Sealed` is only returned when it can be proved: the queue emptied, time was left, the search frame never got
in the way, and the plan had no tactical exclusions in it.

---

## A ceiling in time, not in tiles

One to two milliseconds a search was asked for. A budget in expansions does not give that, and v1's own figures
show why:

```
90,006 tiles / 538 searches = 167 tiles a search
215 ms       / 538 searches = 0.40 ms a search   →  ≈ 2.4 µs a tile
```

At that version's budget of 12,000 expansions, a search that used all of it would have cost around **30 ms**. The
average of 0.40 ms is honest — a cheap search and an expensive proof of "there is no way" simply differ by two
orders of magnitude.

And a tile has no fixed price: on a warm `StepCache` it is reading a mask, on a cold one it is recomputing a
`StepProbe`. A budget in tiles means different amounts of time on a cold and a warm shard.

So the search looks at the clock every 64 expansions and returns `Partial` at the ceiling. The numbers:

| Dial | Default | What it does |
|---|---|---|
| `ceilingMs` | 2.0 | the ceiling on one search |
| `windowMs` | 60.0 | how much the whole population spends on searching per second |
| `floorMs` | 0.25 | what a search gets once the second's quota is used up |

The window is a governor rather than a correctness boundary, and a very generous one: v1 measured 215 ms over
ninety seconds across fifty bots, i.e. about **2.4 ms a second**. Sixty is twenty-five times that and still less
than a tenth of the game loop. When the quota runs out, searches shrink to the floor and return `Partial` more
often: the population moves in short dashes instead of the world stuttering.

**About the tick.** It stays close to the step, ~200 ms against a 400 ms step. A scan every 1–2 ms would be 200 to
400 times more often than a bot can move at all. Sticking did not come from the scan being rare: a failed step was
not dealt with in the same tick it happened — a door was tried after eight failures, an occupied tile was not
excluded from the next plan, and the engine was trusted less than the plan.

---

## Pockets of ground: refusal for free

Proving that somewhere **cannot** be reached is the most expensive question a bot asks: a cheap search goes almost
straight at the goal, while a refusal has to look at everywhere the bot can get to.

But a search that ran out of ground **has by definition enumerated the whole connected pocket**. So the expensive
proof is paid **once per pocket for the life of the shard**, written down, and available to everybody. A yard, a
crypt, an island, somebody's garden — each bills the population exactly once.

So there is no precomputation and no node grid. Felucca is 6144 × 4096, twenty-five million tiles, about a minute
of work at the measured price: a poor thing to pay at startup. And unnecessary — what needs cutting off for free is
"behind the fence" and "across the sea", and those are pockets. A continent will not become a pocket and should
not.

**What makes a pocket trustworthy:** it may only be recorded from a search whose queue emptied and whose frame
**never** got in the way. A search that ran out of tiles inside its own frame learned nothing about the world — and
recording that as a pocket would fence off half a continent for ever without a line in the log. One flag,
`clipped`, is responsible for that, and it is the most important thing in the file.

**Self-repair.** If a bot does get through between two pockets — somebody built a house, took a wall down, cut a
gate — the pockets are merged on the spot. The world corrects itself, and the correction arrives at the moment a
bot does what the registry believes impossible.

The knowledge is **not written to disk** — yet. The arguments for it (do not buy the proof twice) are good, but a
wrong "impossible" is invisible and permanent: the bot simply never goes anywhere and no log will say so. Saving it
within a single run is already the whole point; a file can come later, once somebody has watched the mechanism
live.

---

## The queue of errands: the destination is not lost, it waits

`BotJourney` holds not one goal but a **queue**. The last thing in it is what the bot is doing now; everything
deeper waits its turn and goes nowhere.

| Event on the road | What happens |
|---|---|
| hit, and the fight is winnable | `Interrupt` — the fight goes **on top** of the road, the destination waits under it |
| the monster died | `Prune` lifts the fight and the road is on top by itself |
| hit, and the enemy is several times stronger | the queue is **not touched at all**: walk on, hit back as you go |
| an enemy on the road ahead, seen and not beatable | `BotSight` (since 03.10.2026): its reach is priced on the plan and the route, which are drawn again round it **to the same goal**; the bot waits out of reach when the goal or the only way is in it, and turns back after `WaitMs`. (`AvoidDanger`, which this row named until then, has never had a caller.) |
| somebody else's tile is occupied | on the second attempt it is excluded from the plan and the occupant is asked to move |
| a door is shut | it is opened on the **first** blocked step |
| casting | not sticking: the engine does not move a caster |
| the plan will not walk | last line of defence — any legal step |
| the ground ran out | **only this** errand is dropped; whatever is under it remains |
| 100 fruitless attempts at a step | only this errand is dropped, with the reason in the log |
| death | the whole queue goes with the bot |

**Nobody "remembers" the destination.** It does not go into a variable and get restored — it simply stays in the
queue under the fight, and returning to it is not code. It is what happens when there is nothing left on top.

**True only while the bottom of the queue has one owner, and it has two.** The decision layer puts a bot's own work
there and a company puts its station there, both with `Rebase`, which overwrites the slot. `BotWill` remembers the
last walk it sent and sends again only when the order changes, so a company that overwrote the slot and then let the
bot go left the bot standing on the company's errand for as long as the stall watch allowed — a "sweep" one tile away
is arrived at on every beat. `BotJourney.Bottom` lets the will ask what the slot actually holds, and it sends its walk
again when the answer is not that walk (14.09.2026, `BotWill.Resent`).

**Fleeing has stopped being a goal.** In v1 it was a goal and it overwrote the errand, so a bot stood in a field
after a fight with no memory of what it had set out to do. Here fleeing and going to market are the same action
with different company.

**The queue is four deep.** A limit is needed because an evening of ambushes should not leave a bot with a to-do
list. When it fills, the **oldest ordinary** errand is dropped rather than the oldest of any kind: an interruption
is by definition happening now, and throwing it out would send the bot away from a fight it has already decided to
accept.

What "several times" is measured with is in `BotCombat/README.md`. The threshold is 1.5, a measured v1 number: one
bot against a graveyard spectre gives 1.90 and walks on; six against a lich give 1.04 and stay.

**What to measure so as not to wreck your own road.** The first version got this wrong in three different ways,
all three in the stuck detector:

1. It measured **distance to the goal**. Correct while the only way to travel is straight at it, and wrong the
   moment a bot leaves a yard: the gate is twenty tiles the other way.
2. It compared the length of a **fresh** plan against the length of a finished one. A long road is walked one leg
   at a time, each with its own plan — so a bot correctly crossing a continent was reprimanded for moving
   backwards every 25 seconds.
3. It judged bots that **were not walking at all**. A bot trading at a counter was measured against the distance
   to a home it had no intention of visiting, and after 25 seconds it got "stuck", a cancelled errand and a
   five-minute ban on trading — for trading.

Hence: progress is measured **along the plan**, it starts over with the plan, and an obligation with no plan is not
judged at all.

---

## What to check at home

In the log at startup:

```
Movement ready: one search may cost 2ms, the population 60ms a second, floor 0.25ms; a plan is trusted
45000ms and a journey is given up after 40000ms without progress
```

In the minute's summary (`BotMovementModule.Summarise()`) — three sentences: searches, steps, pockets. What to
watch:

| Number | Healthy | What growth means |
|---|---|---|
| `worst` on searches | ≤ 2 ms | the time ceiling is not working |
| `partial` | a small share | the frame or the ceiling is too tight |
| `refused outright` | rises and then **plateaus** | pockets are working: we pay once |
| `refused by the engine` (steps) | a small share of `steps` | the plan disagrees with the engine — see `RESEARCH.md` §3 |
| `doors opened` | non-zero | doors work; zero means bots are not going indoors |
| `improvised` | a small share | the plan is systematically unwalkable |
| `journeys given up` | near zero | bots are getting lost |
| `pockets that turned out to be one` | near zero | if it rises, the pocket registry is wrong |

With a client:

1. **A fence.** Put a bot behind a graveyard fence and give it a goal outside. It must head **for the gate** — that
   is, away from the goal at first — and must not be reprimanded for it.
2. **A bank door.** Goal: a counter inside. The door must open on the first blocked attempt, not after eight.
3. **A roof.** Give a goal on a crypt roof. The answer must be `Sealed` within one tick rather than twenty-five
   seconds at a wall. In the log: `has no way to`.
4. **Two in a doorway.** Put two bots on tile (1371, 1477, 10) — the gate of Britain's graveyard, which in v1
   produced 77 step refusals in two minutes — and send a third through it. It must go round or ask them to move,
   not shove.
5. **A pocket.** Lock a bot in a yard. The first refusal is expensive; **the second and all the rest are
   instant**, and there is one line in the log: `a pocket of N tiles ... has been walked to its edges`.

---

## What the bot calls

A bot holds a `BotJourney` the way it holds a `BotBond`, and calls three things:

| Where | What |
|---|---|
| when it decides to go somewhere | `journey.Begin(map, where, BotArrival.Beside, "trade")` |
| in the beat, when a step is allowed | `BotWalk.Advance(this, journey, run)` and schedule the next through `BotWalk.StepDelayMs(run)` |
| hit, and the enemy is stronger | `journey.AvoidDanger(...)` — and **do not touch** `Destination` |

Plus implementing `IBotAside.StepAsideFor`: that is how a bot in a doorway answers a request to move.

---

## What the review found, and what was fixed

The first adversarial review pass — five independent runs against v1's code. Here is what it found in the first
draft of these files; everything listed is **fixed**, and it is written down here because each of these places is
what one would most want to watch live.

**Diagonals were half-implemented.** v1 has two gates: first the `WalkMask` bits of both flanking tiles, then a
check of the items on them. I had only the second. That is precisely the mistake `BotNav` calls "the most expensive
in this file", only mirrored: v1 checked only statics and failed at every walk past a gravestone, whereas I would
have planned diagonals through wall corners that the engine refuses. And `GetWalkZ` was being read for a direction
the mask forbids — the landing height of a step that does not exist.

**Asking somebody to move would have thrown.** `StepAsideFor` was called **inside** a `foreach` over
`GetMobilesAt`, and a step aside moves an entity and mutates the very collection being iterated.
`collection was modified` with a population of bots is immediate and permanent. v1 documents this as a failure that
actually happened. Now the occupant is found first and asked afterwards.

**The item cache was keyed by height band.** `Cell` folds Z into bands of 20 units — correct for "name a place" and
wrong here: whether an item blocks a body is decided by exact height, and two heights 13 units apart share a band
and disagree about a crate. The first query to arrive answered for the second, silently and depending on iteration
order. v1 keyed it the same way. Now it is exact Z.

**The starting tile was not checked.** A start whose mask forbids all eight directions empties the queue on the
first expansion — and that looks exactly like a pocket of ground walked to its edges. A **one-tile** pocket would
have been recorded, poisoning that tile and every height in its band for every journey until the shard restarted.
Such a start is now "no progress" rather than a fact about the world.

**The stuck detector killed a road because the bot was being hit.** It measured seconds, and `StallMs` (40 s) was
less than `PlanStaleMs` (45 s): forty seconds of fighting with a non-empty plan and the obligation was abandoned.
That directly contradicts the rule that the route is sacred. Now **attempts at a step** are measured rather than
time: a ten-minute fight costs zero attempts.

**Partial plans had no limit.** v1 capped how many could run consecutively; I did not. A bot in a large fenced yard
too big to fit inside the time ceiling would get an empty path and `Partial` every tick **for ever** — fence-hugging
in a new suit. Now eight consecutive empty plans and the goal is released, with the reason in the log.

**A dangerous square could lock a bot inside itself.** A bot that was nearly killed is standing where it was nearly
killed; excluding the square excluded every neighbouring tile too, and there was nowhere to plan from. And if the
goal was inside the square the obligation became impossible for two minutes. The square is now lifted in both
cases.

**The pocket registry only answered in one direction.** The case it caught was "bot in a sealed pocket, goal
outside". The reverse — bot on the mainland, goal inside a sealed crypt — returned "unknown", a full search, the
time ceiling and `Partial`: the bot walked towards an unreachable goal until it tired. The pocket is already paid
for; it has to answer. And its own cost is now counted: walking `(2n+1)²` tiles is not "nine cheap searches", it is
441 calls to `Settle` at `Within(10)`, so the walk is limited to a radius of 2.

**Small things that still matter:** `NeedsPlan(at)` did not use its argument — so it never noticed that the bot had
been pushed off the plan; a step refused because a tile was occupied redrew the route **on the first attempt**,
spending a search on a skeleton that was going to walk away anyway; an improvised step did not correct the
registry, although it is the most likely step to disprove it; and a refusal from the registry did not count as a
search, so the summary could show more refusals than searches.

**Accepted with open eyes:** `Cell` folds Z into bands of 20, exactly as the engine's own search does — so a bridge
at Z=25 and the road under it at Z=12 land in the same band and the same A\* node. Same as v1. This now has a
second consequence — the band also keys the pocket registry — so if oddities with bridges and balconies show up
live, look here first.

## Gates: the world's teleporters as a walk (29.09.2026)

The dungeon block from x 5120 eastward shares no ground with the island, and the graph's components say so provably;
until 29.09 every walk to a room in it ended "no way through" and the delve carried its party by `MoveToWorld`.
`BotGates` reads every plain `Teleporter` on the map once (raising from `Data/teleporters.json` the ones the world
lacked), folds a doorway's three into one gate, and places each gate's two ends in the graph's component they stand in
(`NavigationService.ComponentOf`, one flood of a cluster's window). A land is a component; a route from one land to
another is Dijkstra over the gates by straight distance, and what the journey gets is the first gate on the way.

In `BotJourney.Draw`, when the graph answers `Unreachable` and the gates know a way, the route is drawn to the first
gate and ends on its tile, walked `Exactly` (`LegExact`); the engine teleports on the step; the next beat finds the
walker within `Through` (6) of the gate's far side and draws the next leg from there, not counted as a reroute. A walker
standing on a gate and not put through — a criminal at a guarded gate, a body mid-fight where the gate checks — is
counted (`Refused`) and walks straight, as before. Stations and sweeps further than `StationDirect` (160) are routed
too, so a company follows its leader through a mouth.

Everything that asked "is this underground and that not" asks the gates now: the appraisal's veto
(`BotGates.Joined`), the company's sundered stations, the walk home, and `BotPopulation.Within`, which counts the
dungeons as the population's ground. Check at boot: `Gates: N teleporters ... read as M gates`, `{Deep} is reached from
home through {Gate}`; in play: `leads N ... to {Deep} by {Gate}`, `has led N into {Deep} by its mouth after N minutes'
march`, and `Passed` against `Refused` in the movement summary.

## Known unknowns

- Pockets are not written to disk. Deliberate; the arguments are above.
- The "doors shut" mode is available through `BotPath.ReachableWithDoorsShut` but **nothing calls it yet**: it will
  be wanted by whoever picks points for wandering and trading. Before, it was a parameter with no way to reach it —
  wiring that looks finished and is not.
- Only the **first** draft of these files was reviewed. The fixes made in response have not been read again by
  anybody.
- The engine surface is verified against the fork and it compiles clean, but no line of this has ever run: there
  are no client files on the development machine.
