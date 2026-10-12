# Movement: the analysis this was designed from

The most expensive part of the project. In v1 it accumulated more measured defects than everything else put
together, and almost none of them were navigation defects — they were the price of having no navigation. This
document is what was found there, what is worth carrying over verbatim, and what was left unresolved.

---

## 1. The main finding: the constraint everything was built around does not exist

The engine's `MovementPath` refuses to search further than **38 tiles** and can only search from a living
mobile's feet. All of v1's navigation was, for six months, a superstructure on top of that:

- a lattice of nodes whose edges were **guesses**;
- a nine-direction fan that walked at an obstacle until it gave up;
- ring probes looking for a way out of a yard;
- a memory of gates, by district;
- learning about walls through a bot pressing against a fence for twenty-five seconds.

The bill for an hour and a half across 51 bots:

```
1728  got no closer to ... for 25s; abandoning it
2522  Road from ... proved impassable
 370  Lattice point ... refused 3 approaches and is dropped
1468  A way out of ... recorded
```

Not one of those lines was a bug in the code. It is an invoice for the same knowledge, bought over and over with
bot working days.

**And the engine already has `StepCache`:** for every tile, a mask of "which of the eight directions can be
stepped in and what height you land at", computed by **the same rules** `MovementImpl` uses to allow a step. With
no mobile. With no distance ceiling. `StepProbe` computes it directly when the cache does not answer, so
correctness does not depend on the cache being warm — a cold shard is merely slower.

Everything else follows from that. A fence is not **discovered** — it is **visible**.

---

## 2. What makes a pathfinder correct

### A failed search must answer, and must distinguish two kinds of "no"

An A\* that did not reach the goal has nevertheless established which tiles *can* be reached. But it matters
fundamentally **why** it stopped:

| Stopped because | What that means | What to do |
|---|---|---|
| the ground ran out | there is no way; a statement about the world | drop the goal at once, record it for everybody |
| the budget ran out | the goal may be reachable | walk to the best tile reached — that is real progress |
| it arrived | there is a way | walk |

Conflating the two is the defect. "Get as close as possible" when the ground has run out is **exactly harmful**:
the reachable tile nearest the goal behind a wall is the tile **at the wall**, and walking there to ask again is
the fence-hugging.

### A refusal can be trusted

That inverts everything. "Stuck" stops being a state a bot is in and becomes **an answer it received**. An
unreachable place is dropped in one tick and recorded for the whole population — instead of costing a quarter of a
minute of somebody's life.

### Getting out of a yard is not a separate mechanic

A gate is not found by searching for a gate. An ordinary search, read correctly, returns the **reachable** tile
nearest the goal — and that *is* the gate, because it is the closest thing to what is outside. No ring probes, no
memory of gates.

---

## 3. Three disagreements with the engine. Each broke everything on its own

None was visible without a log line naming what actually blocked the step.

**Items.** `StepCache` bakes land and statics. This shard's world is built with `[Decorate` — that is, out of
**items**: the graveyard fence, the gravestones, the crates. The planner confidently drew straight lines through
exactly the fences the whole thing existed for, and the bot then failed the step and abandoned the errand,
insisting the route was fine.

**A player's diagonal.** The engine does not let a player cut a corner unless **both** flanking tiles pass the
full check — and the full check includes items. A half version of the rule (statics only) meant that every
diagonal walk past a gravestone failed. In a graveyard made of decorations that is a diagonal every few tiles.

**The cache's height tolerance.** A tile is baked at one standing height and served to any query within a step of
it. For the engine that is correct; for a planner it is not, because reachability inverts at exactly one step. A
climb of four is legal from Z=2 and illegal from Z=0, and **both queries are inside the tolerance**. Bots at
Britain's gates were handed a diagonal onto a Z=3 kerb, believed it, and failed the step for ever. The plan was
right — about a bot standing two units higher.

The conclusion worth carrying through the whole project: **the plan and the engine must answer the same question
by the same rules.** Any simplification of the rules in the planner is not an optimisation, it is twenty-five
future seconds at a fence.

---

## 4. Roofs: why a bot ended up somewhere it could not walk to

Two different defects with one root.

**Lattice nodes landed on roofs.** A node's walkability was established like this:

```csharp
map.CanSpawnMobile(x, y, -128, 127, false, false, out var found)
```

The search window is **the entire world vertically**, and `CanSpawnMobile` answers with **any** walkable surface it
finds. In a built-up world that regularly means a roof. The node looked legal, A\* routed through it, the engine's
own check refused — 25 seconds and one abandoned errand **per approach direction**.

**Goals were chosen on roofs.** A wraith at Z=30, the party at Z=10, three tiles horizontally and twenty units
vertically: a crypt roof. Every distance check in the project (`InRange`, `GetDistanceToSqrt`) **ignores** height,
so the creature looked like ideal prey: close, dangerous, standing still. Five bots declared a party, marched,
"fought" and took off not one point of health.

**The rule that closes it:** height is taken from the floor **nearest the feet**, not from a wide window.
`GetAverageZ` under the point first, then a search within ±8 of it; a wide window only as a fallback, because
cellars, bridges and dungeon floors are real places, and a node not on the ground is better than no node.

**And one subtlety worth more than it looks:** an unreachable creature **stays in the danger assessment**. "I
cannot hit it" and "it cannot hit me" are different statements, and that wraith casts down perfectly well. A bot
that called the place safe on that basis would stand in it and die.

One more from the same family: v1 had `Climb`, which teleported a bot onto any surface within ±20 of its height. It
was introduced for fences and became the **single largest source of sticking** — handing bots roofs and inner
courtyards that cannot be walked to or out of. Removed.

---

## 5. Doors: where a wall stops being a wall

A door is not an obstacle but an **action**. Three states and three different answers:

| State | To the planner | To the stepper |
|---|---|---|
| open | passable | step |
| shut, unlocked | **passable** | open it and step |
| locked | impassable | — |

A shut unlocked door treated as a wall means bots milling about outside a bank for ever: the banker is behind the
door and the counter is three tiles inside. A locked one treated as passable means a population led again and
again to a shop's back door it has no key for.

A **third mode** is also needed: "what is reachable **without** going through doors". That is how the question "is
this point outdoors or inside somebody's house" gets answered — and without it a bot that needs a spot outside gets
a point in somebody's pantry.

The moment of opening is measured too: try the handle on the **first** blocked step, not after eight. A bot that
was merely shoulder-shoved should step; a bot in front of a bank should open. Waiting for several failures means
standing in the street looking stupid.

---

## 6. What navigation must not know

Masks describe **static geometry only**: land, statics, houses, boats.

Creatures, dropped items and shut doors are **deliberately not in the plan**. They move, and a planner that treats
a skeleton in a doorway as a wall will teach itself that the doorway is a wall. All of that is handled where it
belongs: **at the moment the step is actually taken**.

Hence the last line of defence, the one place where the engine is trusted more than the plan: **if the plan will
not walk and some step is possible, take it.** The engine is the only authority on "is this step legal right now",
and a bot that can go somewhere is worth more than a bot that is right about not being able to go where it
intended.

---

## 7. Once navigation stopped being at fault, four things were found underneath

And this is the most useful part of the analysis: **three of the four are not about navigation at all.**

**Bots are obstacles to each other.** Six bots walked to one corpse, queued three deep in a crypt passage, locked
each other in place for twenty-five seconds each, and all six gave up. The routes were flawless. The cure is not a
path but a **claim**: a corpse is worth one bot's attention, the others see that it is taken and go and do
something else. And when somebody genuinely is standing on the needed tile, the next plan is built **excluding that
tile** — the difference between "walk round a person" and "ask them to move for the twentieth time".

**The stuck detector punished progress — twice.** It measured distance to the goal. That is correct while the only
way to travel is straight at it, and wrong afterwards: a bot leaving a closed yard walks twenty tiles **away** from
the goal because the gate is on the other side. What must be measured is **progress along the plan**. And a long
road is walked one leg at a time, each with its own plan — so if a new plan is not marked as new, every leg looks
like moving backwards, and a bot correctly crossing a continent is reprimanded every twenty-five seconds.

**A casting mage looked stuck.** The engine does not let a caster move, so a mage closing distance mid-spell
honestly failed its step — and a stepper that cannot tell a spell from a wall counted the failures, redrew the
route twice and dropped the goal. Mages were being told that their own casting was a dead end.

**Work that is done standing still cannot be judged by the detector.** Trading at a counter, working at a forge,
mining. A bot standing at a counter was measured against the distance to a home it was not walking to and had no
intention of visiting — and after 25 seconds it got "stuck", a reset errand and a five-minute ban on trading.
Precisely for trading.

---

## 8. What it cost in time

Measured after all the fixes, same shard, the same 51 bots:

```
Pathfinding: 538 searches, 90006 tiles examined, 19 refused, 215ms total (0.40ms each)
```

Over a minute and a half: **zero** "has no way to", **one** "got no closer", zero disagreements between planner and
engine, zero errors. Against 1728 abandoned errands in an hour and a half on the previous build.

The price: **0.40 ms a search** and about **0.2 % of the game loop** for the whole population.

The governor holding that: a per-search budget (12000 expansions by default, 700 minimum) plus a ceiling for the
whole population — 60000 expansions a second. When the budget runs out, searches **shrink** and return "partial"
more often, so the population moves in short dashes instead of the world stuttering.

**That is the whole argument of the project: thinking is cheaper than pushing.**

---

## 9. What was left unresolved

This is where v2's work began rather than a port.

**Connectivity is not precomputed.** "There is no way" currently costs a full search — 12000 expansions to prove
there is no land across the sea. That is the most expensive possible answer to the most frequent question.
Partitioning the walkable ground into **connected components** (union-find over sectors) answers "unreachable" in
one comparison, before any search. It was on v1's open list and never done.

**The lattice remained a guess by origin.** It was introduced as a way round the 38-tile window. The window is gone
and the lattice is not, and its edges are verified by the same tile search — so it had become a cache on top of the
real search. Open question: is it needed at all once connectivity is precomputed.

**A claim on a goal was not generalised.** "One bot is on this corpse" was solved for corpses. A vein, a forge, a
counter and a doorway are the same thing.

**Combat during a journey was not designed.** In v1 fleeing was a **goal**, so the route was lost. The
specification for v2 is the reverse: the route is sacred and a fight is an interruption on it.

---

## 10. What to carry over verbatim

Numbers and rules that have already been paid for. Changing them without measuring means buying the same lessons a
second time.

| Rule | Value |
|---|---|
| step-up height | 2 |
| a standing man's height | 16 |
| diagonal | both flanking tiles, the full check **including items** |
| cache height tolerance | reject a mask promising a climb higher than `StepUp` from the requested Z |
| footing for a point | `GetAverageZ` ±8; a wide window only as a fallback |
| search frame | 28…96 tiles from the straight line between start and goal |
| search budget | 12000, minimum 700 |
| population ceiling | 60000 expansions a second |
| step cost | 10 straight, 14 diagonal — as the engine's own A\* |
| doors | passable unless locked; open on the **first** blocked step |
| creatures and items | **not in the plan**, only at the moment of the step |

---

## 11. The decisions for v2

Taken 20.08.2026, after the analysis above.

### 11.1. The ceiling is time, not expansions

A ceiling of 1–2 ms **per search** was asked for. v1's figures show that a budget in expansions does not deliver
it:

```
90,006 tiles / 538 searches = 167 tiles a search
215 ms / 538 searches       = 0.40 ms a search
                            ≈ 2.4 µs a tile
```

At a budget of 12,000 expansions, a search that used all of it costs around **30 ms** — thirty times the ceiling.
The average of 0.40 ms is honest, but a cheap search and an expensive proof of "there is no way" differ by two
orders of magnitude, and what must be bounded is what was promised.

And a tile's price is not constant: on a warm `StepCache` it is reading a mask, on a cold one it is recomputing a
`StepProbe`. A budget in tiles means a different amount of time on a cold shard than on a warm one.

**The rule:** the search checks the clock every N expansions and returns "partial" at the ceiling. The bot moves in
a dash, and the next search starts further along the road with a fresh ceiling. The frame never stutters, however
many bots decided to cross a continent in the same tick.

The tick meanwhile stays close to the step (~200 ms against a 400 ms step). A scan every 1–2 ms would be 200–400
times more often than a bot can move; v1's sticking came not from a rare scan but from a failed step not being
dealt with in the tick it happened in.

### 11.2. Connectivity comes free, out of failures

Connected components are not built by a separate pass. Felucca is 6144 × 4096, twenty-five million tiles; at 2.4 µs
a tile a full walk costs about a minute, and that is a poor price to pay at startup.

But it is not needed. From section 2: **a search that ran out of ground has by definition enumerated the whole
connected component.** So:

- a search that ended in "the ground ran out" → every visited tile belongs to one component, and that is recorded
  **for the whole population**;
- the next "can I get from here to there" inside known components is answered by **comparing two numbers**, before
  any search;
- the expensive proof is done **once per component for the life of the shard**.

A yard, a crypt, an island, somebody's garden — each pays for itself exactly once. A continent will not become a
component (a search across it hits the time ceiling before the edge of the land), and that is right: the questions
worth cutting off for free are "behind the fence" and "across the sea", not "at the other end of a continent".

The knowledge should survive a restart, like the map of the land and the map of the shops in v1: knowledge thrown
away on restart is knowledge bought twice. **This has not been done** — see `README.md`: a wrong "impossible" is
invisible and permanent, and that argument won for now.

### 11.3. There will be no node lattice

It was introduced as a way round the 38-tile window. The window is gone. v1's edges were already verified by the
same tile search — so the lattice had degenerated into a cache on top of the real search while remaining a **second
source of truth about where one can walk**, with its own nodes on roofs and its own list of struck-off edges.

A long road is walked in dashes of the same tile search. One mechanic instead of two.

### 11.4. The route: the goal is untouchable, the way bends

| Event on the road | What happens |
|---|---|
| hit, and the fight is winnable | fight back; the road waits |
| hit, and the enemy is stronger | **carry on along the route**, hitting back as you go |
| an enemy standing exactly on the road | the square is excluded and the path redrawn **to the same goal** |
| somebody else's tile is occupied | the tile is excluded from the next plan |
| a door is shut | it is opened on the first blocked step |
| the ground ran out | the goal is dropped and the component recorded for everybody |
| death | the only thing that removes the obligation |

Fleeing stops being a **goal** — in v1 it was one, which is why the route was lost. Here fleeing is the same
movement along the route, only under fire.

What to measure so as not to wreck your own road (three v1 mistakes, all three in the stuck detector): progress
**along the plan** rather than distance to the goal; a new leg of the route **not counted** as moving backwards;
and work that is done standing still **not judged** by the detector at all.

### 11.5. What this adds up to

| File | Responsibility |
|---|---|
| `BotStep.cs` | one tile: the mask, items, diagonals, the height tolerance, footing |
| `BotPath.cs` | a tile A\* with a time ceiling, three outcomes: arrived / partial / no ground |
| `BotReach.cs` | connected components: recorded out of failures, answered in one comparison |
| `BotJourney.cs` | the A→B obligation: holds the goal, bends the way, judges progress by the plan |
| `BotWalk.cs` | the moment of the step: doors, occupied tiles, a caster, the last line of defence |
| `BotMovementModule.cs` | phase `World`, counters and a summary |

The module is in the `World` phase: everything here asks about the map, and there is no map before the world
loads.
