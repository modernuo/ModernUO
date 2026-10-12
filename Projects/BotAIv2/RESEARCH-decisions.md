# How a bot decides — research notes for BotAIv2

*14.09.2026. Written for Patrick's order of that morning: decisions that are correct, decisions that are
logical, and bots that play their role as believably as possible — and, before any of that, the price of a
choice and whether a bot keeps to it once it has made one.*

This document has three halves that are meant to be read together. What the rest of the field knows about
deciding — agent theory, reinforcement learning, and the games that shipped with autonomous characters. What
this shard was actually doing, measured from its own log on the morning the order was given. And what changed
because of the first two, with the number that says whether it worked. Where a claim comes from a source, the
source is named in §7; where it comes from this shard, the log it was measured in is named beside it.

`ARCHITECTURE.md` says how BotAIv2 is built. This says why its decision layer is shaped the way it is now, and
what the next repairs ought to be.

---

## 1. The shapes a game brain comes in

Every autonomous character that has shipped is some mixture of five shapes. None is "the right one"; each
fails in a characteristic way, and knowing the failure is most of the value.

| shape | how it chooses | shipped in | how it characteristically fails |
|---|---|---|---|
| State machine, hierarchical state machine | a hand-written graph of states and transitions | almost everything before 2005; F.E.A.R. kept three states | every new behaviour is a new edge; the graph stops being readable |
| Behaviour tree | a prioritised tree walked every tick; a higher branch interrupts a lower one | Halo 2 (Isla, GDC 2005), most action games since | re-deciding every tick makes it dither between near-equal branches |
| Utility system | every option is scored; the best (or a weighted pick among the best) wins | The Sims (needs and advertised objects), Dave Mark's IAUS, Kevin Dill's dual utility | scores that cross produce oscillation; a factor that reaches zero is a veto nobody meant |
| Planner (GOAP, HTN) | a goal is chosen, then a plan of actions whose preconditions chain to it | F.E.A.R. (GOAP, Orkin, GDC 2006); Killzone 2 and Horizon Zero Dawn (HTN) | a plan built on a fact that has since changed walks into a wall unless it is checked while running |
| Belief–desire–intention (BDI) | options become intentions, and intentions are kept until a reason to drop them | PRS and its descendants; military and robotics agents | commitment set too loose is a cautious agent that finishes nothing; too tight is a fanatic |

A newer, sixth shape sits on top of all of them: **a language model choosing**, as in Generative Agents (Park et
al., 2023), Lyfe Agents (2023) and Project Sid (2024). Every one of those systems ended up wrapping the model in
one of the older shapes — a plan hierarchy, an option that runs until a cheap termination check fires, a
controller that forces what the agent says to agree with what it does — because the model alone does not keep a
course.

**Where BotAIv2 sits.** It is a utility auction with planner-shaped work and a priority ladder:

- *Proposers advertise work*, exactly as The Sims' objects advertise what they satisfy (Zubek's needs-based AI):
  the brain does not know what work exists, the subsystem that owns the work offers it.
- *A deed is a small hierarchical task*: mining is "the seam, then the fire, then the counter", owned by the
  deed rather than by the brain — the HTN idea of a method that knows its own steps.
- *The ladder is bucketing* — Dill's dual utility, Rez Graham's description of The Sims' motive buckets: a bot
  that is failing never weighs a better seam against a bandage, because the rung it stands on only offers
  bandages.
- *Prices are measured, not typed.* `BotLedger` and `BotCommons` correct each trade's claimed rate by what it
  actually paid. That is closer to reinforcement learning than to the hand-set weights most utility systems use,
  and it is the reason this shard has found defects no weight-tuner would: a trade that stops paying stops being
  chosen, and the log says why.

What the auction lacked on the morning of 14.09.2026 was the BDI half: a notion of a choice being *kept*.

---

## 2. Commitment — the price of a choice, and keeping to it

### 2.1 What the field knows

**Intentions are supposed to resist reconsideration.** Bratman's account of practical reason (1987), which the
whole BDI line of work formalises, treats an intention as a commitment that filters what an agent even
considers: a person who has decided to drive to the coast does not re-derive the decision at every junction.
Rao and Georgeff turned this into three commitment strategies — *blind* (keep the intention whatever happens),
*single-minded* (keep it until it is achieved or believed impossible), *open-minded* (keep it while it is still
wanted). The question they leave open is exactly Patrick's: how often should an agent look up?

**Kinny and Georgeff answered it by experiment (IJCAI 1991).** In Tileworld — an agent moving to fill holes that
appear and vanish — they varied how many plan steps an agent carried out before replanning, from one (a
*cautious* agent) to the whole plan (a *bold* one), and how fast the world changed. Three results matter here:

1. In a world that changes slowly relative to the agent, the bold agent beat the cautious one, and an agent
   of intermediate boldness came in between. Replanning costs time, and in a slow world the time is wasted.
2. A committed agent that *reacts to specific events* did much better than a blindly committed one.
   Replanning when its target disappeared improved it significantly; also replanning when a *nearer* hole
   appeared was better still. Replanning on *any* new hole was worse than blind commitment, except in the most
   dynamic worlds.
3. With that one change — commitment from blind to reactive — the bold agent was superior to the more
   cautious ones everywhere they measured.

The lesson is not "commit more". It is **commit, and let the right events — not every new opportunity — reopen
the decision.**

**Deciding whether to reconsider must itself be cheap.** Schut and Wooldridge (2001) framed reconsideration as a
meta-level decision: at each moment an agent either acts or deliberates, and a good policy decides which from a
cheap test rather than by deliberating about deliberating.

**Reinforcement learning reached the same place from the other side.** Sutton, Precup and Singh's options (1999)
are closed-loop behaviours that run over many steps; a policy over options that runs each to completion is a
semi-Markov decision process, and they proved that interrupting an option is only an improvement when continuing
it is worth less than switching. Harb, Bacon, Klissarov and Precup (AAAI 2018) added a *deliberation cost* — a
price for switching — and the learned options became longer, better and easier to interpret. A switching price
is not a hack laid over rational choice; it is what rational choice looks like for a mind that has to pay to
think.

**People work this way too.** Heckhausen and Gollwitzer's Rubicon model separates a *deliberative* mindset before
a decision — open, weighing alternatives — from an *implemental* one after it, which is focused on getting the
thing done and deliberately filters out information that does not serve it.

### 2.2 What shipped games do

- **The Sims Medieval** let a Sim decide only when its interaction queue was empty; once it chose, it carried the
  action out, and chose again only when it had finished or failed (Rez Graham, *Game AI Pro*, ch. 9). The
  original needs-based design pushes an advertisement's whole action sequence onto the queue (Zubek).
- **Game AI Pro's utility chapters** name the failure: two options with similar scores ping-pong. The standard
  remedies are a bonus for the action already under way, cooldowns, or stalling the next decision until the
  current action ends (Graham). Mike Lewis (*Game AI Pro 3*, ch. 13) adds the warning that a commitment bonus
  does not remove oscillation, it only moves the scores at which it happens — which is precisely what this
  shard's two-minute boundary turned out to be.
- **Halo 2** walks a prioritised behaviour tree every tick, lets a higher-priority sibling interrupt the winner,
  and gives the previous tick's winner a bonus against dithering (Isla).
- **RimWorld** consults a pawn's think tree for a new job only when it has none; a small "constant" tree checks
  emergencies on an interval; a job can carry an expiry that triggers an override check; and an interrupted job
  that is marked suspendable is put back at the head of the pawn's queue and resumed afterwards
  (`Pawn_JobTracker`).
- **Dwarf Fortress** is the cautionary tale: a work order whose cause of cancellation persists is re-queued and
  cancelled again and again, and "job cancellation spam" is one of the best-known things about the game.
- **Lyfe Agents** made language-model agents fifty times cheaper largely by committing: a model call picks an
  *option* and a subgoal, and the option runs until a cheap, non-model termination check fires — a time limit,
  repetition, an event.

### 2.3 What this shard was doing (measured, 14.09.2026, 08:26–10:14, build 33)

The decision layer reviewed every held undertaking every 15 seconds. Fresh work was protected from being
replaced for its own reckoning, but never less than 30 seconds and **never more than two minutes**
(`BotWill.DwellCapMs`); after that any offer worth about 1.56 times the work in hand (margin 1.25 × inertia 1.25)
took the bot off it.

| measure | value |
|---|---|
| undertakings taken | 6,688 |
| endings that were finishes / failures / drops | 76% / 13% / 11% |
| drops that happened between 1.95 and 2.25 minutes in — at the cap | **224 of 734 (31%)** |
| bots that took the same trade up again within 10 minutes of dropping it for something else | mine 85%, cook 86%, acquire 84%, mend 82%, peddle 81%, prowl 80% |
| mining drops at the cap with the digging already done | 31 "carrying ore to a fire", 14 "putting the metal away" |
| mining: taken, finished, dropped | 357, 185, 100 |

Four in five dropped trips came straight back to the same trade: that is not a change of mind, it is the Kinny
and Georgeff cautious agent. And the drops sat on a single boundary, which is Lewis's warning made visible — the
bonus had not removed the oscillation, it had moved it to two minutes.

Three more defects sat under the same heading, "a bot cannot finish what it started":

- **The stall watch counted every swing as an errand swapped.** It compared the undertaking's stage *text*, and a
  woodcutter's stage reads "cutting wood (12 logs in 40 swings)". 29 of the 37 "taking and dropping errands"
  reports that morning were gatherers at work.
- **A woodcutter could swing at nothing indefinitely.** An out-of-range swing resets the tree's silence clock on
  purpose; one of those every half minute kept a barren tree in hand. Sable: 293 swings, no log, 10.7 minutes.
  Ulla: 199 swings in 7.6 minutes — and a fresh trip at the same spot cut twenty logs in seven swings.
- **Holding a claimed square is standing still, and the stall watch cancelled it for that.** Eight members were
  taken off a muster for doing exactly what the muster asks.

### 2.4 What changed (build 34)

The design follows Kinny and Georgeff's reactive bold agent, with RimWorld's suspendable jobs for the part they
did not model — interruptions that are not a change of mind.

**Steadfast work is held for its own reckoning.** A deed that answers `BotDeed.Steadfast` — mining, woodcutting,
herbs, cooking, peddling, unloading — is held against ordinary offers for the time it reckoned when it was taken
(the walk there at walking pace plus its own `Minutes`), stretched by `BotWill.CommitStretch` (1.5) and capped by
`CommitCapMs` (eight minutes). The cap is safe to be long because the watchdogs that catch stuck work — the trek
limit, the labour clock, the stall watch — run whether a hold stands or not.

**Events reopen the decision; opportunities do not.** Three things get through a hold:

1. what will not wait — `Pressing`, as before;
2. a call from outside the bot's own business — `BotDeed.Summons`: a comrade in trouble, a wound to bind, the
   guild's muster, the war company, a company already fighting, a lesson already paid for, a prisoner to free;
3. trouble in the work itself — its walk told the way is blocked, or a walk that has stopped closing for a third
   of the trek limit (`TroubleShare`).

**What an interruption takes is put down, not thrown away.** When a pressing offer, a summons, a full pack or a
rung above displaces steadfast work, the work is *paused* (`BotPause`), and when the interruption ends it is taken
up again — unless the bot died, came back below half its health (`ResumeHealth`), is on another map, or was away
longer than anything set aside may be. The stake the work was taken with is moved by whatever changed while it
was away, so a rescue's takings and minutes are not booked to the mine. A deed that opts in promises a
`Resumed()` that puts right any clock measuring time since its last progress.

**Every switch now says what it cost.** The drop line reads, for example, "summoned by stake at 175/min after
2.1 of 3.5 minutes reckoned, taken at 47/min and worth 44/min by then". The five-minute census writes a
`Resolve:` line — finishes, failures and drops; drops by cause (would not wait, summoned, a rung above, a full
pack, outbid); offers the hold refused; holds lifted for trouble; pauses and resumptions; trades taken back within
ten minutes of being dropped; and all of it per trade. Argus has two new verbs: `resolve <bot>` and `resolves`.

The three smaller defects were repaired at the same time: the stall watch counts swaps by the undertaking's
identity rather than its words (and wants six of them now); a woodcutter gives a tree up after twenty swings
without wood whatever resets its clock, and a whole trip after eighty swings without a log; a member standing
in its claimed square is `Still`.

### 2.5 Results

Measured two ways, because a change seen in one direction only is not yet evidence.

**Against the previous build at the same shard age** — the first twenty-two minutes of each session:

| trade | build 33, no hold: finished / dropped | build 34, hold: finished / dropped |
|---|---|---|
| all work | 78% / 12% | 78% / 8% |
| mining | 44% / 37% | 74% / 5% |
| cooking | 63% / 21% | 79% / 7% |
| unloading | 75% / 19% | 97% / 3% |
| woodcutting | 62% / 10% | 100% / 0% |
| herbs | 79% / 17% | 92% / 4% |
| *looking for a fight (not held)* | *63% / 18%* | *58% / 15%* |
| *hunting (not held)* | *69% / 18%* | *74% / 18%* |

**On one running shard, the hold dialled off and back on** (build 34; `CommitStretch` 1.5 → 0 → 1.5, `Resume` with
it; nineteen-minute windows, the second starting eight minutes after the switch so that work held from before had
ended):

| trade | hold on, 10:22–10:41 | hold off, 10:49–11:08 |
|---|---|---|
| all work: finished / dropped | 80% / 8% | 68% / 16% |
| mining | 65% / 7% | 33% / 32% |
| herbs | 83% / 8% | 50% / 40% |
| cooking | 78% / 8% | 69% / 15% |
| peddling | 92% / 2% | 72% / 16% |
| mining trips taken back within ten minutes of being dropped | 9 of 9 drops | 27 of 31 drops |

The effect reverses when the hold is taken away and returns when it is put back, on the same bots, the same ground
and the same hour. The drops that remain are mostly what they should be: work that is not held (looking for a fight,
hunting), mending interrupted by flight, and interruptions that paused steadfast work rather than ending it — 37
pauses and 29 resumptions in the first half hour, the rest refused for a bot that came back hurt or had died.

---

## 3. Correct and logical decisions

### 3.1 How utility systems go wrong

The literature and this project's own history agree on the list.

- **A factor that can reach zero is a veto.** Multiplying considerations drives scores towards zero as their
  number grows; IAUS compensates for the count, BotAIv2 takes a geometric mean — and every factor still needs a
  floor, which this project learned three times (`MAP.md` §4).
- **Near-equal options oscillate** (§2). The cure is not a bigger bonus but a rule about *when* to look again.
- **Unconstrained goal pursuit produces acts nobody would call sensible.** Oblivion's Radiant AI was cut back
  before release because NPCs pursued their goals by any means available — the best-known story is of skooma
  addicts killing the dealer a quest needed alive. A utility score says what is worth most; only a constraint says
  what is not allowed.
- **A plan built on a fact that has changed must notice.** F.E.A.R.'s soldiers re-evaluate their goals when
  something invalidates the plan — a cover position the player has flanked — and record obstacles in a shared
  working memory so the next plan does not repeat them (Orkin). This shard's `BotRefused`, `BotQuarry.Shun` and
  the ledger's caution are the same idea, and "seeing is not reaching" (`MAP.md` §4) is its most expensive lesson.

### 3.2 Enabling work priced on its own takings

In a planner, the value of an action comes from the goal it serves: buying reagents is worth what the spell they
make possible is worth. In a needs-based system the same thing happens through the need a chore satisfies. In
this shard's auction every piece of work is priced by what *it* earned per minute — and a purchase earns nothing
by construction.

Measured on the morning of 14.09.2026 (build 33, 108 minutes): the auction refused unloading **1,008** times and
restocking **721** times on the grounds that the work was "expected to pay" a negative amount here. Settlements
with negative coin: acquire 738, restock 474, sew 172, supply 139. Among the failures that followed: "nothing to
write with", "nothing to brew with", "nothing to dig with", "the shelf holds no Bottle at any price". The
`Unpaid` flag already exists for work that is not about money, and unloading already takes it when a bot cannot
move; supplies and spells do not. This is the most important *logic* defect found in this pass and it is not yet
repaired — see §6.

**What pricing a trip at its claim cost, measured the same evening.** Build 35 made restocking `Unpaid`, and an unpaid
deed learns nothing from failing: its proposer is its only gate. At 16:38 Hale's pack went past the engine's cap of 125
things — oil cloths nobody bought, handed back by the market without asking the pack — after which every coin drawn to
pay at a counter bounced, and the trip failed 8,719 times in half an hour. The shard's alarm read 11% of work finished
while the rest of the population finished as it had all day. Build 37 asks the question the counter fails on before
the work is weighed (`BotDeed.AtCounter`, `BotYield.Pocket`), stops the market handing goods back past the cap
(`BotListing.Return`), and offers the trip that makes room unpaid to a jammed pack. Taking the price off a piece of work
also takes away the one thing that stopped it repeating, so a gate has to arrive with the flag.

### 3.3 A rule nobody sets reads exactly like a rule that holds

Gaia's role model separates a role's *liveness* responsibilities (what it must eventually do) from its *safety*
responsibilities (what must never happen). `BotClass.DefendsOnly` is a safety property — a medic does not go
looking for a fight — and `BotHunter` still enforces it. But the only class that ever set it was the King's
Rangers' healer, cut on 02.09.2026; the ordinary Healer never had it. For twelve days healers spent a quarter of
their working time looking for fights with a rule on the books saying they would not. Build 35 gives it back to
the Healer (`BotHealer.Defends`), and the recommendation in §6 is a census line that lists every safety rule
together with how many bots it applies to, so a rule with nobody under it is visible.

---

## 4. Roles — playing a part believably

### 4.1 What believability asks for

Loyall's thesis for the Oz project (CMU, 1997) lists what a believable agent needs: personality, emotion,
self-motivation, change, social relationships, consistency of expression, and the appearance of being alive —
reactive, aware, pursuing its own goals. Of those, *consistency of expression* is the one a role breaks: a
character that acts out of character even briefly stops being believed.

### 4.2 How games encode a role

- **As what a character is allowed to do.** In F.E.A.R. a soldier, an assassin and a rat can share one goal set
  and behave quite differently, because each has a different set of *actions* available to satisfy it (Orkin).
  Halo 2's "styles" are lists of allowed and disallowed behaviours that a squad's orders carry (Isla). RimWorld
  marks work types a pawn is *incapable* of, from its traits and backstory.
- **As what a character prefers.** RimWorld's *passions* make a pawn learn a skill faster and enjoy the work that
  uses it; Crusader Kings III gives every AI character personality values — boldness, greed, energy, compassion,
  zeal and others — that weight its decisions; The Sims filters which advertisements a Sim with a trait even
  receives (Zubek).
- **As what a character must do first.** Dual utility's rank puts whole categories ahead of others before any
  weight is compared (Dill); the BOID architecture classifies agents by the order in which obligations, desires,
  intentions and beliefs win conflicts — a *social* agent lets obligations beat desires, a *selfish* one the
  reverse (Broersen et al., 2001).
- **As a place in a group.** Horizon Zero Dawn's herds request *roles* — patroller, scavenger, attacker — and the
  individual planner satisfies the role (Guerrilla).

### 4.3 Roles and language-model minds

The model-driven agents show the same failure in a new form. Generative Agents kept characters coherent with an
identity seed, a plan decomposed from the day down to five-to-fifteen-minute chunks, and an explicit question at
each observation — react, or carry on with the plan? — yet still reported agents embellishing what they knew and
drifting to atypical places as their memories grew. Instruction drift has been measured directly: chat models lose
adherence to their system prompt within about eight rounds of conversation, which Li et al. (COLM 2024) trace to
attention decaying over the growing context. Project Sid's answer is a cognitive controller: a bottleneck that
makes one decision and broadcasts it, so that what an agent says and what it does agree; with it, thirty agents
that started identical specialised into distinct roles. Lyfe Agents keep identity with a self-monitoring summary
that runs beside the actions.

This shard's four crafter minds already do the most important of these things — the model chooses from an
enumeration rather than writing free text, and the choice runs as an ordinary deed under the same auction and
the same commitment — so the drift they can show is bounded by construction.

**The observers show the other failure the literature names, and show why a measurement has to stand beside every
conclusion.** Between 10:25 and 10:52 on 14.09.2026 all three of Argus's squad raised the same conjecture eight times,
each "85% sure": that one bot's unloading was an unproductive loop. Asked through the door, the shard said otherwise —
since 10:14 that bot had finished 21 of 26 mining trips, 4 of 5 supply runs and both of its unloads, and was carrying
127 of 243 stones. Three models agreeing with each other is not three observations; it is one embellishment, repeated,
which is Generative Agents' failure in an observer's clothes. The squad's *measurements* — `props`, `pack`, `resolve`
— are the engine answering, and were right; its *findings* are a model's reading of them, and are checked before they
are believed.

### 4.4 What this shard was doing (measured, 14.09.2026, 08:26–10:14, build 33)

Share of each class's working minutes by whose work it was. *Own trade* is work that trains a skill the class
works towards (`BotClass.Wants`); *another's* is work that trains a skill some other class is for; *anybody's* is
work that trains nothing or a skill no class claims — carrying, selling, looking for a fight, standing for the
guild, cooking.

| class | own trade | anybody's | another's | largest single uses of time |
|---|---|---|---|---|
| Crafter | 73% | 27% | 0% | mine 34, chop 16, sew 12, peddle 12, forge 9 |
| Gatherer | 67% | 32% | 1% | chop 34, mine 33, stake 7 |
| Captain | 67% | 33% | 0% | sweep 49, peddle 23, drill 5 |
| Architect | 65% | 12% | 23% | mine 49, chop 23, forge 9 |
| Sage | 33% | 67% | 0% | inscribe 21, prowl 16, herbs 15 |
| Brawler | 27% | 73% | 0% | prowl 44, rescue 10, hunt 10 |
| Archer | 16% | 84% | 0% | prowl 29, hunt 14, peddle 11 |
| Mage | 15% | 85% | 0% | prowl 24, peddle 17, stake 12, acquire 10 |
| Warrior | 12% | 83% | 5% | prowl 27, peddle 14, stake 10, cook 9 |
| Healer | 10% | 90% | 0% | **prowl 26**, peddle 16, acquire 11, stake 11, brew 7 |

The producers play their parts. The fighters and casters do not, and almost none of the gap is them doing another
class's work — it is anybody's work, above all *looking for a fight and not finding one*. That splits the repair
in two: pricing a role (small, done) and giving a role something to do (large, open).

### 4.5 What changed (build 35)

- **`BotCalling`** reads a piece of work as the bot's own trade, another class's or anybody's from what the class
  and the deed already declare, and multiplies its price — own trade ×1.3, another's ×0.6, anybody's unchanged —
  outside the appraisal's fifth root, where a statement about who the bot is cannot be flattened. Both numbers are
  dials, and a `Roles:` census line and Argus's `roles` verb report every class's split.
- **The Healer keeps the medic's rule** (§3.3).
- **A healer has a place: beside a fighter.** `BotAccompany`, offered by `BotAttendant` to healers only, walks a
  healer to the nearest fighter of ours who is engaged and has nobody standing by it, and keeps it within five tiles
  until the fight has been over for twenty seconds or five minutes have passed. It is steadfast, so the wound it is
  there for pauses it rather than ending it and the healer walks back afterwards; and it is unpaid, because the skill
  arrives through the mending and is booked there. This is the first piece of §6.2, done for the class whose gap was
  widest.
- **Supplies are no longer refused for costing money** (§3.2): a trip to the shops is unpaid work now, priced at its
  typed claim and still gated by its proposer and by whether the bot can pay.
- **A class learns its own spells first** (`BotClass.BookFirst`). A book filled cheapest first is a book of curses —
  a healer with fifteen gold was measured buying Weaken, Feeblemind and Clumsy while short of Greater Heal — so the
  healer now fills in cures, protection and the greater heals first, and the mage and warrior-mage the direct damage
  of each circle. The Sims filters which advertisements a Sim with a trait even receives; this is the same idea one
  level down, in what a caster wants to own.

### 4.6 Results

The same approximation for every session — a kind of work filed as a class's own trade when it trains one of that
class's skills — over the first thirty minutes of three sessions on 14.09.2026: build 33 (no hold, no calling), build
34 (the hold) and build 36 (the hold, the calling, the healer's rule and place, the class book order).

| class | own trade, build 33 | build 34 | build 36 | what build 36's minutes went on |
|---|---|---|---|---|
| Healer | 23% | 23% | **40%** | brewing 22%, standing by a fighter 16%; looking for a fight gone from 23–27% to nothing |
| WarriorArcher | 9% | 6% | 18% | looking for a fight 60%, hunting 14% |
| WarriorMage | 6% | 6% | 11% | looking for a fight 31% |
| Warrior | 17% | 13% | 17% | looking for a fight 39%, hunting 11% |
| Archer | 22% | 13% | 15% | looking for a fight 46%, hunting 14% |
| Mage | 26% | 23% | 24% | selling 22%, looking for a fight 20%, writing 11% |
| Crafter | 85% | 78% | 81% | mining 55%, sewing 15%, forging 7% |
| Gatherer | 84% | 73% | 63% | mining 61%; herbs, standing for the guild and selling the rest |

Standing by a fighter, over the same half hour: healers were asked 2,962 times and sent 227 times — 2,702 times there
was nobody of ours fighting within sixty tiles; 45 stints were taken, 40 ended because the fight was over, three because
the healer could not keep up after six tries, and one was put down for a wound and taken up again afterwards, which is
the pause working exactly as it was built to. Healers fell behind their fighter 95 times and walked on instead of
giving up.

**What this says.** The healer is now visibly a healer: its time went from looking for fights to brewing and standing
beside the people who fight. The fighters and casters are where they were, and that is the prediction of §4.4 coming
true rather than a failure of the calling: almost none of their time was ever another class's work, so pricing
another class's work lower had nothing to take away. What they lack is something role-true to do instead of walking
about looking for a fight, and that is §6, item 2.

---

## 5. Research idea → BotAIv2 mechanism

| idea | source | in BotAIv2 | status |
|---|---|---|---|
| Objects advertise what they satisfy | The Sims, Zubek | `IBotProposer` | since v2 |
| Buckets / rank before weight | Dill; Graham on The Sims | `BotLadder` rungs; the full-pack rank in `BotWill.Auction` | since v2; rank added 09.09.2026 |
| Measured rather than typed values | RL generally | `BotLedger`, `BotCommons` | since v2 |
| Bonus for the action under way | Graham; Isla | `BotAppraisal.Inertia`, `BotWill.SwitchMargin` | since v2 |
| Reactive bold commitment | Kinny & Georgeff; Rao & Georgeff | `BotDeed.Steadfast`, `BotWill.Holding` | build 34 |
| Event versus opportunity | Kinny & Georgeff; Schut & Wooldridge | `BotDeed.Summons`, `Pressing`, trouble | build 34 |
| Suspend and resume | RimWorld `jobQueue`; options interruption | `BotPause`, `BotDeed.Resumed` | build 34 |
| A price on switching | Harb et al. | the hold's length and the margin | build 34 (length); no per-switch cost yet |
| Re-plan on invalidation, shared memory of obstacles | Orkin | `BotRefused`, `BotQuarry.Shun`, ledger caution | since v2 |
| Safety properties of a role | Gaia; RimWorld incapabilities; Halo styles | `BotClass.Sworn`, `DefendsOnly` | Sworn for the Baron; DefendsOnly restored build 35 |
| Preferences of a role | RimWorld passions; CK3 personality | `BotCalling` | build 35 |
| Obligations before desires | BOID; dual-utility rank | `Summons` passes holds | partial |
| Group roles | Horizon Zero Dawn | `BotRole`, formations | formations only |
| Controller keeping words and deeds coherent | Project Sid; Lyfe | minds choose from an enum and run as deeds | by construction |

---

## 6. What should come next, in order

1. **Price enabling work by what it enables** (§3.2). Supplies, spells and unloading are the preconditions of the
   trades that pay; they should be worth a share of the work they unlock — the way a planner values a
   precondition — or at least be priced at their typed claim as `Unpaid` work is, never refused for costing money.
   Measure with the refusal counts quoted in §3.2 and the "nothing to … with" failures.
2. **Give fighters, casters and healers role-true work for the minutes they now spend looking for a fight**
   (§4.4). Candidates, in order of how directly they serve the role: healers accompanying a company or a hunter
   rather than walking alone; fighters guarding gatherers at the seams the danger map marks, which is also income;
   more lessons at the captain's field, which trains the fighters' own skills (a captain teaches 5% of its time).
3. **React to a nearer opportunity of the same kind during the walk**, which Kinny and Georgeff found better
   than reacting to the target alone — a miner walking past a seam as good as the one it is walking to.
4. **A census of the rules and whom they bind** (§3.3): for each safety flag, how many bots it applies to. A rule
   with nobody under it would have shown up on the first line.
5. **Break herd synchrony with weighted randomness among near-equal offers** (Dill; Zubek), so that ten bots
   given the same numbers do not all walk to the same seam.
6. **Minds: an option and a termination condition, not only a trade.** Let a crafter mind name how long or until
   what it means to keep at its choice (Lyfe), and check what a mind says it will do against what its deed does
   (Project Sid).

---

## 7. Sources

- Rao, A. S., Georgeff, M. P. *BDI Agents: From Theory to Practice.* ICMAS 1995. https://cdn.aaai.org/ICMAS/1995/ICMAS95-042.pdf
- Kinny, D., Georgeff, M. P. *Commitment and Effectiveness of Situated Agents.* IJCAI 1991. https://www.ijcai.org/Proceedings/91-1/Papers/014.pdf
- Schut, M., Wooldridge, M. *Principles of Intention Reconsideration.* AGENTS 2001. https://www.cs.vu.nl/~schut/pubs/Schut/2001.pdf
- Schut, M., Wooldridge, M., Parsons, S. *The Theory and Practice of Intention Reconsideration.* JETAI 2004. http://www.cs.ox.ac.uk/people/michael.wooldridge/pubs/jetai2004.pdf
- Sutton, R. S., Precup, D., Singh, S. *Between MDPs and semi-MDPs: A Framework for Temporal Abstraction in Reinforcement Learning.* Artificial Intelligence 112, 1999. http://incompleteideas.net/papers/SPS-aij.pdf
- Harb, J., Bacon, P.-L., Klissarov, M., Precup, D. *When Waiting Is Not an Option: Learning Options with a Deliberation Cost.* AAAI 2018. https://arxiv.org/abs/1709.04571
- Heckhausen, H., Gollwitzer, P. M. — the Rubicon model of action phases. https://en.wikipedia.org/wiki/Rubicon_model ; Gollwitzer, *Action Phases and Mind-Sets*: https://www.socmot.uni-konstanz.de/sites/default/files/90_Gollwitzer_Action_Phases_MindSets.pdf
- Bratman, M. E. *Intention, Plans, and Practical Reason.* Harvard University Press, 1987.
- Broersen, J., Dastani, M., Hulstijn, J., Huang, Z., van der Torre, L. *The BOID Architecture: Conflicts Between Beliefs, Obligations, Intentions and Desires.* AGENTS 2001. https://dl.acm.org/doi/10.1145/375735.375766
- Wooldridge, M., Jennings, N. R., Kinny, D. *The Gaia Methodology for Agent-Oriented Analysis and Design.* 2000. Overview: https://www.cs.upc.edu/~jvazquez/teaching/sma-upc/slides/sma04a-Methodologies-GAIA.pdf
- Loyall, A. B. *Believable Agents: Building Interactive Personalities.* PhD thesis, CMU-CS-97-123, 1997. https://www.cs.cmu.edu/Groups/oz/papers/CMU-CS-97-123.pdf
- Isla, D. *Handling Complexity in the Halo 2 AI.* GDC 2005. https://www.gamedeveloper.com/programming/gdc-2005-proceeding-handling-complexity-in-the-i-halo-2-i-ai
- Orkin, J. *Three States and a Plan: The A.I. of F.E.A.R.* GDC 2006. https://www.gamedevs.org/uploads/three-states-plan-ai-of-fear.pdf
- Graham, D. "Rez". *An Introduction to Utility Theory.* Game AI Pro, ch. 9, 2013. https://www.gameaipro.com/GameAIPro/GameAIPro_Chapter09_An_Introduction_to_Utility_Theory.pdf
- Dill, K. *Dual-Utility Reasoning.* Game AI Pro 2, ch. 3, 2015. https://www.gameaipro.com/GameAIPro2/GameAIPro2_Chapter03_Dual-Utility_Reasoning.pdf
- Lewis, M. *Choosing Effective Utility-Based Considerations.* Game AI Pro 3, ch. 13, 2017. https://www.gameaipro.com/GameAIPro3/GameAIPro3_Chapter13_Choosing_Effective_Utility-Based_Considerations.pdf
- Mark, D. — the Infinite Axis Utility System. https://www.gameai.com/iaus.php
- Zubek, R. *Needs-Based AI.* Game Programming Gems 8 (draft). https://robert.zubek.net/publications/Needs-based-AI-draft.pdf
- RimWorld job tracker (decompiled), `Verse.AI/Pawn_JobTracker.cs`. https://github.com/josh-m/RW-Decompile/blob/master/Verse.AI/Pawn_JobTracker.cs ; work, passions and incapabilities: https://rimworldwiki.com/wiki/Work , https://rimworldwiki.com/wiki/Skills
- Guerrilla Games. *The AI of Horizon Zero Dawn*; *HTN Planning in Decima.* https://www.guerrilla-games.com/read/the-ai-of-horizon-zero-dawn , https://www.guerrilla-games.com/read/htn-planning-in-decima
- Crusader Kings III AI personality values. https://www.magicgameworld.com/crusader-kings-iii-ai-personality-guide/
- Radiant AI in The Elder Scrolls IV: Oblivion. https://en.wikipedia.org/wiki/Radiant_AI ; https://blog.paavo.me/radiant-ai/
- Dwarf Fortress job cancellation from work orders. https://dwarffortressbugtracker.com/view.php?id=3144
- Park, J. S. et al. *Generative Agents: Interactive Simulacra of Human Behavior.* UIST 2023. https://arxiv.org/abs/2304.03442
- Kaiya, Z. et al. *Lyfe Agents: Generative agents for low-cost real-time social interactions.* 2023. https://arxiv.org/abs/2310.02172
- Altera.AL. *Project Sid: Many-agent simulations toward AI civilization.* 2024. https://arxiv.org/abs/2411.00114
- Li, K. et al. *Measuring and Controlling Instruction (In)Stability in Language Model Dialogs.* COLM 2024. https://arxiv.org/abs/2402.10962
