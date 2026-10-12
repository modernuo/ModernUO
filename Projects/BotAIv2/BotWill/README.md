# Will: where the decision is made

What a bot does and why. One survival ladder, one queue of obligations, one auction of offers and one ledger of
measurements.

**This folder in two sentences.** The brain does not hold a list of goals — it holds *proposers*, and those are
registered by the subsystems that own the work. And the value of work here is not assigned by weights but
**measured**: takings per minute, where the takings are skill gained at the exchange rate plus money plus goods
produced, less what was spent.

| File | What is in it |
|---|---|
| `BotStanding.cs` | the ladder's rungs, top to bottom. The order *is* the content |
| `BotLadder.cs` | the rung from facts: alive, overloaded, health, being hit, in a squad |
| `BotDeed.cs` | an obligation: work with its own stages. The subclass is written by the subsystem; `Taken` is told when it wins, `Foe` names what it fights, `Paperwork` never takes a bot off work in hand (build 50), `Guess` is not turned round for another guess of the same kind (build 56) |
| `BotDoing.cs` | what the obligation wants done now: walk, work, done, failed |
| `IBotProposer.cs` | offer work. The extension point, and the slow tier's vote |
| `BotUrges.cs` | boredom and need — all that is left of motives-as-deficits |
| `BotLedger.cs` | what has paid this bot, and where. All the memory of work there is |
| `BotYield.cs` | the takings: the skill-to-gold rate, the price of death, the measurement ceiling, and the aside ledger — money moved by somebody else's decision, kept out of the work in hand |
| `BotAppraisal.cs` | the appraisal: estimate × considerations, geometric mean, inertia; the work in hand is not asked the price of starting (build 50); the root is the dial `Root`, 5, named on the take line (build 64) |
| `BotBreaker.cs` | work one bot keeps failing for the same reason is not offered to that bot for a while; and the loudest loop of an alarm window, for the alarm (`Loudest`, build 64) |
| `BotTreadmill.cs` | the same deed against the same target failing at once is rested against that target for seconds, defences included; a fight judged lost ("X was winning") rests against that foe at once and the flight is weighed as on the failing rung (07.10.2026, loops18) |
| `BotResolve.cs` | state on the bot: feelings, ledger, what was taken on and why, what it scored then and how long it reckoned; and `BotPause`, work put down for something that would not wait |
| `BotWill.cs` | the decision itself: settle → advance → auction. And the census. A refused road the work in hand never sent — a company's station — is not charged to that work (`Foreign`, build 52); a prowl displaced by the fighting it went for is settled finished without crediting its place (`Met`, switch `MetCounts`, build 65) |
| `BotWillConfig.cs` | `Configuration/bot-will.json` |
| `BotWillModule.cs` | module, phase `World`, requires `Classes` |
| `BotCommons.cs` | what the population as a whole has found out about what pays where |
| `BotCommonsStore.cs` | keeps what pays where across restarts — every patch, trade and seam written with its age so the half-life goes on from where it was; a restart used to be an hour of relearning (build 74) |
| `IBotWilful.cs` | what deciding needs a bot to be |

---

## One currency

```
takings = (Δmoney + goods produced + Δskill × rate) / minutes
```

Money is purse and account together. Goods produced is whatever the obligation itself declared it made. Skill is
**one named** skill, and only for an obligation that **reached the end**.

Three rules around this formula are worth more than the formula.

**Points are for change, not for state.** "Has 500 coins" is worth nothing; "banked 500" is worth 500. Not a
matter of taste: an addition to a reward provably leaves the best behaviour unchanged only if it is a difference
of potentials (Ng, Harada & Russell, 1999), and the classic price of breaking that is an agent which discovers
that standing in the right place pays.

**Skill counts only in finished work.** Otherwise the best strategy on the shard is the cheapest repeatable twitch
that trains: a dummy, a spell cast at nothing, two bots sparring in a field until the restart.

**Death is expensive because everything else in the project made it cheap.** The kit is bound: it survives death,
comes back on resurrection and is not merchandise. That is right for the kit — and it means dying is nearly free.
If the takings were only skill and money, the best way for a young fighter to train would be to attack something
too strong and die on a loop. So death adds `DeathMinutes` to the divisor and marks the place with caution. It is
the honest version of the same fact: death costs a bot the walk back.

---

## The ladder is data

Top to bottom: `Dead` → `Failing` → `Hunted` → `Bound` → `Busy` → `Free`. The auction runs on `Free` and on any
rung above it that has a proposer of its own. Survival is a ladder rather than an appraisal: a dying bot does not
weigh options.

**One deliberate departure from v1.** There "I am being hit" stood **above** "my health is going", because it was
a rung with a goal of its own — it decided whether to fight or run. In v2 that decision is not on the ladder at
all: it lives in `BotThreat.Decide` and answers in the same instant, from `OnDamage`. All that is left of the rung
is "do not go looking for new work in the middle of a fight", and keeping that above health would suppress flight
for the whole duration of any fight — because a bot that is being killed is being hit continuously. The same
defect the flight rule was written against, in a new hat. So health is higher.

**The second departure, and it was a deadlock.** In v1 "overloaded" was a rung near the top, because nothing else
cured it: the goal was re-chosen every tick, and a bot buried in ore had no undertaking that carried it anywhere.
Here there is one — "carry it" is the next stage of the work — and a rung that puts the undertaking aside would be
putting aside the only thing that ends the problem: the bot stands there, the work is set aside, ten minutes later
it is dropped, offered again, set aside again, for ever. So overload is a **fact** that gets read
(`BotLadder.Overloaded`) rather than a rung. When there is something to offer for it, it will arrive through the
auction with a high bid, like any other want.

There is no "carrying a parcel" rung: in this version nothing can be carried for somebody else, and a rung that no
fact can produce is a branch that never executes.

---

## An obligation is held, not reconsidered

Three mechanisms, and all three are about one measured v1 defect.

| Mechanism | What it does |
|---|---|
| `ReviewMs` = 15 s | how often a busy bot looks up at all |
| `DwellMs` = 30 s | how long fresh work is untouchable, whatever the numbers say |
| `Inertia` ×1.25 and `SwitchMargin` ×1.25 | the new thing has to be **clearly** better, not slightly better |

The defect: a bot in state `Trade`, walking a graveyard. It was honestly trading — one tick at a time: two steps
towards town, a skeleton ten tiles away, back to hunting, town again. **Any intention longer than a second was
impossible in principle**, and the only reason it did not look broken is that a bot walking in circles looks
busy.

An interruption does not lose the work: a higher rung puts the obligation **aside** rather than cancelling it, the
way the journey in `BotJourney` waits under a fight. After `AsideCapMs` (10 minutes) what was set aside is dropped
after all: the market has closed, the vein is worked out, and a bot returning to a task an hour later acts on an
hour-old fact.

### And since 14.09.2026, held until something happens

Those three mechanisms stopped a bot changing its mind every tick; they did not stop it changing its mind at two
minutes. Fresh work was protected for its own reckoning but never longer than `DwellCapMs`, and past that any offer
worth about half as much again took the bot off — whether it was half a minute into a trip or half a trip. On the
morning of 14.09.2026, 224 of 734 drops happened between 1.95 and 2.25 minutes in, and four bots in five took the
same trade up again within ten minutes. Mining finished 52% of its trips and dropped 28%, thirty-one of them on
the walk to the fire with the ore already in the pack.

The repair follows Kinny and Georgeff's reactive bold agent (see `RESEARCH-decisions.md` §2):

| Mechanism | What it does |
|---|---|
| `BotDeed.Steadfast` | work a bot sees through: mining, woodcutting, herbs, cooking, peddling, unloading, getting a spell, standing for the guild |
| `CommitStretch` ×1.5, `CommitCapMs` 8 min | a steadfast deed is held against ordinary offers for its own reckoning — the walk it expected plus its `Minutes` — stretched and capped |
| `BotDeed.Summons` | what gets through a hold besides `Pressing`: a comrade in trouble, a wound to bind, the guild's muster, a war company, a paid lesson, a prisoner |
| trouble | a walk told its way is blocked, or one that has stopped closing for `TroubleShare` of the trek limit, lifts the hold |
| `BotPause`, `Resume`, `ResumeHealth` 0.5 | work displaced by something that would not wait is put down and taken up again when that ends, unless the bot died, came back too hurt, changed map or was away past `AsideCapMs`; its stake is moved so the interruption's takings are not booked to it |

The watchdogs — the trek limit, the labour clock, the stall watch — run whether a hold stands or not, which is
what makes a hold of eight minutes safe: nothing stuck can hide behind one. `CommitStretch` 0 puts the auction back
exactly as it was, and the `Resolve:` census line and Argus's `resolve <bot>` / `resolves` say what the holds did.

Measured over the first twenty-two minutes of a shard, the hold off against the hold on: drops 12% → 8% of all
endings; mining 44% finished / 37% dropped → 74% / 5%; cooking 63/21 → 79/7; unloading 75/19 → 97/3; woodcutting
62/10 → 100/0; herbs 79/17 → 92/4. Work the hold does not touch — looking for a fight, hunting — was unchanged.

---

## The auction and the proposers

```
IBotProposer → BotDeed → appraisal → taken → advance every beat → settle → ledger
```

A proposer gives **one** best offer: only it can compare two veins — richness, distance, whether somebody is
already there — and a brain sorting forty candidate tiles per bot per decision is v1's cost model in a new hat.

An obligation knows **its own stages**. Mining is not "walk to a vein", it is dig, smelt and bank what came of it,
because an obligation that ends at the vein leaves a bot underground holding ore nobody will buy. The brain never
learns what ore is.

**And the same construction is how the slow tier gets a vote.** In v1 a language model sat behind the brain as an
advisor, and the brain took 85 of the 135 plans it managed to review — the model's suggestion lost to any errand
the brain had of its own, and nothing recorded that it had lost, so it spent the night learning from noise and
finished with 0 of 119 predictions borne out. A model proposing through this interface bids in the same units,
loses on the same arithmetic, and has its actual takings written into the same ledger.

**Some refusals are about whether the work can happen at all, and those are vetoes, not factors.** Among the refusals
`BotAppraisal.Weigh` names are another map, an outlay the bot cannot afford, and — since 14.09.2026 — work that changes
money over a shopkeeper's counter offered to a bot whose pack has no room for a coin (`BotDeed.AtCounter`,
`BotYield.Pocket`). A factor under a fifth root cannot say "cannot": a fiftieth still comes out at 0.46. The third was
needed because of `Unpaid`: a deed paid nothing on purpose learns nothing from failing, so the reasons it fails must be
asked before it is offered. Restocking, made unpaid that morning, failed 8,719 times in half an hour for one bot whose
pack had gone past the cap.

Two more since that evening, both of them the auction reading a record that already existed:

- **A rest after a loop** (`BotBreaker`). Six failures of one kind of work for one bot, for the same reason with its
  numbers taken out, inside five minutes and since the bot last finished that work, rest that kind for that bot for five
  minutes, doubled at every further trip up to forty and lifted by a finish. It is a backstop for the class of defect
  this project has met most — a failure offered again unchanged — and it logs every trip with the bot, the work and the
  reason, because the loop it stops still has to be mended where it starts. Replayed over the logs of 14.09.2026 before
  it was written, it stopped every loop of the day and tripped at most once in a session without one.
- **A rest after a treadmill** (`BotTreadmill`, 07.10.2026, loops18). The breaker counts six in five minutes and never rests a
  defence; since react14 a fight that ends is decided again at the clock's next look, so a deed that fails on its first beat
  turns at ten a second (Orlando, build 418: "hitting back at a troll" 39 times in eight seconds, each "a troll was winning").
  Keyed on the bot, the kind and the target (the foe's serial, else the place): a fight ending on the losing judgement rests
  against that foe for `LosingRestMs` (15 s) at once, and `BotFugitive`'s early flight is weighed as on the failing rung for
  those seconds (fight what you beat, leave what you do not); the fight to the end (`BotDefender.Stand`) is let through, so a
  bot with no flight on offer still fights. Any deed failing within `InstantMs` (2 s) of being taken, having made nothing, twice
  inside `TwiceMs` (2 s) against one target rests against it for `RestMs` (15 s); a flight never (counted). A veto in the
  appraisal, so the hurried decision meets it too; never the work in hand. Counted at the end of the Will line; `Running` off
  undoes it.
- **Ground where bots have been dying** (`BotPeril.Lethal`). The danger map held every death and was read only by
  captains; the appraisal's caution was the bot's own ledger, keyed by trade. Work whose place lies in or beside squares
  where two bots have died lately is refused, unless it goes where the fighting is on purpose (`BotDeed.Braves`: hunt,
  prowl, sweep, pickings, plunder, band, harrow, delve, escort, scout, flee) or is a call from outside (`Summons`) —
  those are how such ground is dealt with. Twenty-eight bots died in one field in twenty minutes on 14.09.2026 on
  offers that read "safe 1.00". The map lives in memory, so after a restart it starts empty and the refusal begins
  again only after new deaths.

---

## What bends and what does not

A refusal from the road (`Refused`, `GaveUp`) is not a sentence on the obligation: it is asked to `Bend`, and it
may name somewhere else. The same symmetry as movement one level down: the goal is untouchable and the way bends.
By default `Bend` returns `false` — a subsystem with nothing to say here had better not leave its bot spinning.

---

## Boredom and need: what is left of them

Motivation as a set of deficits is a trap, and it was measured: 38 bots out of 51 on patrol with drive frozen at
0.62. Not an arithmetic defect — **they had run out of things to want**. Takings-as-a-derivative is free of that:
a mastered occupation stops paying by itself.

So exactly two feelings are left, and neither of them competes with work.

**Boredom** rises only when there is nothing to do, and it does not choose for the bot: it makes repetition wear
out faster (`novelty` in the appraisal) — the same field should cost a bored bot more. Being busy **holds** boredom
in place rather than relieving it: relief comes from the takings, when the work closed. In v1 patrolling relieved
boredom, so a bot with nothing to do had something to do, and it never came back from it.

**Need** is a fact about the purse relative to **what the bot was about to do**: `1 − money / the largest outlay
among the offers`. Not a comfort threshold: v1 compared every purse to a flat 250 and issued 100 at birth, so the
whole population read as short of money from its first second — and a signal that is on for everybody always is not
a signal. Need matters in exactly one place, `Coin`: a purse of skill does not buy a pickaxe.

---

## The crowd

The appraisal is divided by how many bots are already doing this work. That is the whole answer to what v1's
population degenerated into: **116 traders to 14 fighters**. No individual decision was wrong — trading paid, so
everybody traded. A want whose value does not fall with crowding is a want everybody ends up having, and a pure
utilitarian appraisal starves roles. It is computed arithmetically from a shared fact, like a squad's stations:
nobody has to be told anything.

---

## A ledger instead of a model

When work closes, its takings per minute are folded into what is already known about **this kind of work in this
place**. Next time, that is the estimate. No model, no training, no serialisation, and it recovers by itself: a
worked-out patch stops paying and its row falls with it.

An unfamiliar place gets the proposer's bid unmodified — and that is the whole of exploration: a place never tried
is judged on the promise, so it will be tried once. No randomness is needed for that, and its absence is worth
something: a population that does the same thing twice from the same facts can be diagnosed by reading.

**Punishment is not a mechanism.** A failure is a low number in the row for this work in this place, so the bot
stops choosing it and tries again once the row has faded. v1 punished with bans: a bot judged stuck lost its
errand and was barred from trading for five minutes — that is, it was punished for trading. Here nothing is ever
forbidden.

---

## How this can be gamed

There is no unbreakable proxy, so the holes are named in advance, together with what closes them.

| Hole | What closes it |
|---|---|
| train on a dummy | skill counts only in finished work |
| die on something strong for the skill | `DeathMinutes` in the divisor plus caution about the place |
| declare work finished instantly | `LeastMinutes` floors the divisor, `MostPerMinute` caps the measurement |
| take everything to a shopkeeper | `Made` counts as takings alongside coin |
| sell your own kit | the kit is bound: not merchandise, not lost, weightless |
| stand in a bank because "there is money" | points only for change |

The shopkeeper is worth a line of its own, because it is about the economy rather than the honesty of the metric:
trade between bots **moves** gold, and selling to an NPC **creates** it. A brain that counted only coin would drive
the whole population into one faucet — the one that dried v1's world by 110,900 in a night. Until the money
balances, **the brain cannot be judged by the population's wealth**; it can be judged by whether skills rise and
undertakings close.

---

## What the bot calls

Five calls. The brain has neither a timer nor a registry, like everything else here.

| Where | What to call |
|---|---|
| in the beat | `BotWill.Decide(this)` — once per beat |
| after `BotWalk.Advance` | `BotWill.Note(this, result)` |
| `OnDamage` | `BotWill.Hurt(this)` — beside `BotThreat.Decide` |
| `OnDeath` | `BotWill.Died(this)` |
| when the bot is deleted | `BotWill.Forget(this)` |

Plus implementing `IBotWilful`: that is `IBotSquadMember`, one field `BotResolve Resolve { get; } = new();`, and
`BotBond Bond { get; }`.

`Forget` is not decoration. The census counts what is being done right now, and a bot deleted mid-task would leave
its task in that count for ever. A count that is never released does not look wrong — it looks like a busier
population.

---

## What is not here

**Eight proposers, from five subsystems.** Harvest (mine), Craft (sew), Spells (inscribe, acquire), Hunt (hunt),
Mend (mend, twice — on two different rungs), Shops (restock, peddle). Switch them all off and every auction finds
nothing, and it is visible: `Barren` in the census rises and there is one line in the log saying exactly that.
Work subsystems are plugged in one at a time and not one of them edits this folder.

**`Failing` is served** as of `BotMend/BotMedic` — the last empty rung of the ladder. While it was empty the
brain's answer to failing health was "hold on to what you are doing": harmless while nothing fought, and "go back
to the skeleton" from the day hunting appeared.

**There is no stack of obligations.** Work from a higher rung is not laid on top — it **replaces** what was there
(with an honest measurement and no blame on the place). A stack is needed from the second customer, not the first.

The order this was worth building in: the gatherer's chain first (dig → smelt → bank), because it alone exercises
everything at once — stages, skill training, coin, the bank and the definition of done. Then hunting, then orders,
then mustering a squad — `BotSquads.Form` is still called by nobody, and that is a decision rather than an
omission.

---

## What to check with a client

1. Bring the shard up and look at the `Will ready:` line — the skill rate and both margins. That is the cheapest
   place to notice that the rate has been set to something absurd.
2. `bots.will.enabled = false` in `modernuo.json`: squads form, roads get walked, combat answers — and nobody
   chooses anything.
3. Watch the `took on` and `finished` pairs: the estimate is written there, what it was made of, and who came
   second. That is the answer to "why is it doing this", which did not exist in v1.
4. Every five minutes, the `Will:` census. Watch `nothing was worth doing`: that number is about the **world**,
   not about the bots.

---

## What was changed outside this folder

- `BotJourney.Rebase` gained an overload for things that walk: an obligation may be a creature rather than a
  place, and `Interrupt` will not do for that — an interruption is by definition what is happening now rather than
  what the bot is engaged in.
- `BotClass.cs` had a reference to a `Decision/` folder that never existed.
- `BotCore` — one line of registration.
- `IBotWilful` gained `Bond`, so that "buy another of what I had" is answerable: a class offers six blades and the
  roll hands over one, together with the skill that swings it.

---

## What remains unverified

**The numbers.** Nobody has checked them and there is nothing to check them with but a live run: the rate of `500`
a skill point, `1.25` on the margins, `0.8` on crowding. Those are where to start from, not what is correct.

The engine surface is verified against the fork — `Skills[...].Base`, `Backpack.GetAmount(typeof(Gold))`,
`Banker.GetBalance`, `Mobile.BodyWeight`, `StaminaSystem.StonesOverweightAllowance`, `Map.MapID`,
`Core.TickCount` — and it compiles clean. What it does when it runs is still reasoning.
