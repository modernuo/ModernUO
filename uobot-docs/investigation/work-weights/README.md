# Work weights

How a bot on this shard decides what a piece of work is worth, and so what it does next.

The weighing is read out of the code in `Projects/BotAIv2/BotWill/` and checked against every decision the bots
logged between **18.09.2026 00:08** and **26.09.2026 15:30**: 462,156 take lines from 145 sessions, 116 bots and 59
kinds of work, plus the 742 lines of the shard's `overstated` alarm (16.09–26.09). Every number in section 2 is
produced by `extract.py` and can be found in `data/`; numbers in sections 1 and 3 come from the code and from the
decision log (`Projects/BotAIv2/DECISIONS.md`) and say so.

| file | what it is |
|---|---|
| `README.md` | this write-up |
| `extract.py` | regenerates `data/` and the numbers inside `index.html` from a logs folder (section 5) |
| `data/` | the extracted tables (section 6) |
| `index.html` | the charts, one self-contained page |

---

## Findings in brief

1. **The typed claim is a starting point, and experience moves it a long way.** Work paid nothing on purpose
   (`Unpaid`) was read at exactly its typed claim on every take — restock 12, stake 200, supply 260, evict 380. Paid work
   was read far from it: mining's typed 45 at 11 to 89 a minute (quartiles), hunting's 60 at 86 to 162, fleeing's 2,000
   at 167 to 333 — below the quarter floor of the island's correction, because a bot's own ledger has no floor.
2. **Nearness and novelty bend most winners, and the fifth root keeps the bend small.** On winning offers `near` was
   below 1 on 77.9% of takes, `room` on 78.8%, `new` on 53.3%, `purse` on 10.1%, `safe` on 0.43%. The smallest factor of
   a take had a median of 0.59; the combined factor, the fifth root of the product, had a median of 0.86. `near` was the
   smallest factor on 40.5% of takes and `new` on 37.5%.
3. **A novelty of zero still leaves about a third of the score.** One smith, Edda, took forge 21,929 times (69.7% of all
   forge takes), 18,518 of them with `new` printed as 0.00, at a median estimate of 536 a minute. Forge's combined factor
   had a median of 0.30. Repetition could not outbid a ledger that believed the work paid hundreds a minute; the decision
   log traced the loop to the forge counting a bound war fork as made (build 227).
4. **Most choices were not close.** 37.3% of takes had one offer worth anything. Where there was a runner-up (62.7%), the
   winner's rate was a median 3.04 times the runner-up's; 10.9% were won by less than 1.25 times.
5. **Two gates make more than nine refusals in ten.** Of the 174,464 take lines (37.8%) that name a refused offer, 54.3%
   are "expected to pay nothing or less here" (78% of those are prowls) and 38.9% "cannot pay to start" (69% restocks).
6. **Claims that live on the floor, and readings that do not move.** Inside those refusals prowl's claim was 2.0 in the
   median — a quarter of its typed 8, the floor under the island's correction — while the bot expected −10.4. The same
   negative reading was printed unchanged for an hour or more 996 times within one session: for up to 6.55 hours, and
   in one case 958 times to 64 different bots ("prowl is expected to pay −51.8/min here, against a claim of 2.0"). In the
   code only a positive reading of the island's record of a place fades, and a bot's own ledger has no clock at all,
   although the ledger's own comment says a bad place is tried again "when the row has faded".
7. **The overstated alarm named 30 trades in 591 lines** between 16.09 and 26.09 — band 128, sweep 73, mend 71, drill 41,
   prowl 38 — mostly claims held up by the quarter floor over trades that paid less than nothing.
8. **The danger veto changed shape inside the window.** Until 26.09 14:05 ground was closed by counts of the dead
   (9,695 first refusals on take lines; 240,814 refused appraisals in the census). From 14:05:56 it closes only where the
   hostile strength is three times what goes there: 182 first refusals and 647 refused appraisals in the 84 minutes that
   followed, at a median ratio of 5.4.

---

## 1. How a piece of work is weighed (from the code)

The pipeline, in the order `BotAppraisal.Weigh` runs it:

```
claim (BotDeed.Expects)
  -> vetoes that need no arithmetic (map, dungeon edge, outlay, coin room, breaker, becalmed, danger, red in town)
  -> island correction (BotCommons.Corrected / Realised / typed for Unpaid)
  -> own ledger (BotLedger.Expect, or the island's record of the patch)   -> veto if <= 0
  -> factors: (near x new x room x safe x purse x load x revel x ground x charter) ^ (1/5)   -> veto if 0
  -> x calling x toll
  -> the auction (BotWill.Auction): rank, then score; margin, dwell and hold for the work in hand
```

### 1.1 One currency

Every want competes in gold-equivalent per minute. What a piece of work paid is measured when it ends
(`BotYield.Settle`):

```
takings per minute = (change in money + goods made + skill gained x 500) / minutes
```

- money is pack, bank and escrow on the market board; money moved by somebody else's decision (a levy, a guild's draw)
  is booked aside and left out;
- skill counts only for work that finished, and at 0.3 for a skill the bot's class is not for;
- a death adds 3 minutes to the divisor; the divisor is at least 0.25 minutes; one settlement is clamped to ±2,000;
- work that ended because the bot could not get there is not priced at all (`Settle(unreached)`).

### 1.2 Offers and claims

Each subsystem registers proposers (`IBotProposer`), and each proposer offers at most one deed per review: the best it
can find. A bot with work looks up every 15 seconds (`BotWill.ReviewMs`), a bot without every 2 (`IdleMs`). A proposer
is not asked at all when the bot's class is sworn to other work (the census counted 514,010 offers withheld this way).

A deed's claim is `BotDeed.Expects`, gold-equivalent a minute — "a prior, not a promise". Most claims are a typed dial,
a few are computed per offer (a peddle from its goods, a board errand from its reward over walk and work). Some typed
claims:

| kind | claim | where |
|---|---|---|
| mine | 45 | `BotDig.Prior` |
| hunt | 60 | `BotSlay.Prior` |
| prowl | 8 | `BotProwl.Prior` |
| forge | 120 (55 until 07.09) | `BotForge.Prior` |
| unload | 30, or 120 when the bot cannot move or its pack is jammed | `BotUnload.Prior`, `Stuck` |
| restock | 12, unpaid | `BotRestock.Prior` |
| stake | 200, unpaid | `BotHold.Prior` |
| rally | 400, unpaid | `BotRally.Prior` |
| flee | 2,000 | `BotBolt.Prior` |

Flags on the deed change how it is weighed:

| flag | what it does to the weighing |
|---|---|
| `Unpaid` | a duty: the typed claim stands — no island correction, no ledger, and never refused for expecting to earn nothing (it would enter at 0.01) |
| `Posted` | a price somebody posted (the board's errands): corrected by the share of posted prices actually earned |
| `Outlay`, `Coin` | gold needed to start (the outlay veto; the yardstick of need), and the share of the pay that is money (the purse factor) |
| `Minutes` | how long the work itself takes (the near factor, the dwell) |
| `Standing` | can be done without a step (ranks first for a bot over its carrying ceiling) |
| `Braves`, `Brings` | goes where the fighting is on purpose, and the strength it takes there (the danger veto, the toll) |
| `AtCounter` | changes money over a shopkeeper's counter (the coin-room veto) |
| `Steadfast`, `Summons`, `Committed`, `Paperwork`, `Guess`, `Pressing` | how the work in hand is held against new offers (1.8) |

### 1.3 First correction: what the trade has paid across the island

`BotCommons.Corrected(kind, claim)`:

```
corrected = max( claim / 4 ,  (2 x claim + paid x n) / (2 + n) ),   n = min(outcomes of this kind, 25)
```

`paid` is a running average of what work of this kind came to anywhere on the island (each outcome moves it a fifth of
the way, an outcome from a bot with a mind two fifths), fed only by work that reached its place. At 25 outcomes the
measurement is 25 parts in 27 and the typed claim keeps 2. The floor is deliberate: the code's comment says measurement
may bring a trade down to a quarter of its claim but may not say the trade does not exist. (The same comment calls it
the floor the appraisal uses for crowding and an empty purse; those two floors are 0.10, not a quarter.)

Two kinds of claim are read differently (`BotAppraisal.Reads`):

- **Unpaid:** the typed number stands. Unpaid work pays nothing by construction, so the correction used to drag every
  such claim to its floor (section 3, item 5).
- **Posted:** `BotCommons.Realised`: `claim x (2 + s x n) / (2 + n)`, where `s` is what posted work of the kind earned
  over what it claimed, clamped to 0..2, and the same quarter floor (build 95).

### 1.4 Second correction: what this bot has found at this place

`BotLedger.Expect(kind, place, corrected claim)`, per kind per patch of 64 by 64 tiles, at most 48 rows per bot:

```
estimate = (2 x claim + earned x n) / (2 + n),   n = min(outcomes settled here, 12)
```

`earned` is a running average (each outcome moves it 35% of the way). A bot with no row of its own reads the island's
record of the patch instead (`BotCommons.Expect`: the same blend with `n` at most 4, and a one-hour half-life); with no
record at all the claim stands, which the code calls the whole of exploration in this design. Unpaid work skips the
step.

The estimate has no floor. At or below nought the offer is refused — `is expected to pay X/min here, against a claim of
Y` — except Unpaid work (enters at 0.01) and a prowl for a bot that has had nothing to do for two minutes (enters at
0.01, `RestlessAfter`, build 181).

The ledger also holds two things the factors read: how often the bot has done this here lately (`spins`, halved every
15 minutes), and whether the place is under caution (after a death, or a failure that produced nothing: 5 minutes,
doubling with each repeat up to an hour, cleared by one finished piece of work there).

Two properties that matter in 2.7: a bot's own `earned` has no clock — only the spins fade — and the island's record of a
patch fades only while it is positive (`BotCommons.Faded` returns a reading at or below nought unchanged).

### 1.5 The factors, under a fifth root

| factor | computed as | floor | the question |
|---|---|---|---|
| `near` | work minutes / (work minutes + walk minutes); a tile walked is 0.4 s; from 26.09 the walk is measured by road (`BotRoads.Farther`) | above 0 | is the walk worth the work |
| `new` | 1 / (1 + spins x 0.35 x (1 + boredom)) | above 0 | has this bot just been doing this here |
| `room` | 1 − 0.8 x (bots doing this kind / bots doing anything) | 0.10 | is everybody already doing it |
| `safe` | 0.15 if the place is under caution for this kind, else 1 | – | did it go badly here lately |
| `purse` | 1 − need x (1 − coin share); need = 1 − wealth / the largest outlay on offer | 0.10 | can a purse of skill buy a pickaxe |
| `load` | 0.02 when the bot is over its carrying ceiling and the work needs a step | – | can the bot walk at all |
| `revel` | a multiplier the watcher declares for a kind of work (3.00 in this window) | – | what the watcher wants done this quarter hour |
| `ground` | 1.25 on the bot's own guild's land, 0.70 on another guild's, 1 on open ground | – | whose land it is |
| `charter` | 1.50 when the bot's guild has asked for this work | – | what the guild asked for |

The product is taken to the fifth root (`BotAppraisal.Root`, a dial since 15.09; every line in this window says
"fifth"). The code's reason: multiplying normalised factors drags every score towards nought as factors are added, and a
geometric mean keeps the scale. The cost is that a root flattens everything under it: 0.10 reaches the score as 0.63,
caution's 0.15 as 0.68, the load factor's 0.02 as 0.46. A zero anywhere is a veto (`weighed out at nothing`), which is
why every factor that ordinary life can drive to nought has a floor.

### 1.6 Outside the root: who the bot is, whose land it is

- **calling** (`BotCalling`): × 1.30 when the work trains a skill the bot's class works towards, × 0.60 when it trains a
  skill of another class, × 1 otherwise; never below 0.10. Kept outside the root on purpose, as a statement about who the
  bot is rather than about a place.
- **toll** (`BotToll.Factor`): on another guild's land, for work that braves the ground, 1 − the toll: 0.90 where the
  toll is posted, 0.80 where it is patrolled and the bot has been told.

```
score = estimate x (near x new x room x safe x purse x load x revel x ground x charter) ^ (1/5) x calling x toll
```

### 1.7 The vetoes

In the order `Weigh` asks them. Each ends the weighing at nought, and the offer never reaches the auction.

| # | veto, as the take line words it | not asked of |
|---|---|---|
| 1 | `is on another map` | – |
| 2 | `is on the island and this bot is underground` (and the reverse; build 180) | work that needs no step |
| 3 | `costs Ngp and it has Mgp` — cannot pay to start | the work in hand |
| 4 | `goes through a counter and the pack has no room for a coin` (build 37) | – |
| 5 | `failed 6 times running for the same reason and rests Ns more` — the breaker | – |
| 6 | `is N tiles off, and a walk of M from about here closed nothing` — becalmed (build 178) | the work in hand |
| 7 | danger. Until 26.09: `lies in or beside ground where N bots have died lately` — 2 keep ordinary work out, 3 close the ground. From 26.09 (build 260): `lies where the hostile strength is H, R times the S going there`, closed at R ≥ 3 | running away, always; until 26.09 the keep-out gate also let through work that braves the ground and calls from outside |
| 8 | `lies in a guarded town and NAME is red` | running away, a sentence |
| 9 | `is expected to pay X/min here, against a claim of Y` — after the ledger | Unpaid work; a restless prowl |
| 10 | `weighed out at nothing` — the factors' product is 0 | – |

### 1.8 The choice

`BotWill.Auction` weighs every offer and then:

1. **rank** — for a bot past its carrying ceiling, work that needs no step outranks everything else whatever the price;
   for every other bot the ranks are equal (09.09.2026);
2. **score** — the highest wins; the second best is printed as the runner-up.

Against work already in hand:

- the held work is weighed again (without the price of starting) and multiplied by `Inertia` 1.25, and the new offer must
  beat that by `SwitchMargin` 1.25 — 1.5625 times in all;
- fresh work is untouchable for its own reckoning, 30 seconds to 2 minutes (`Dwell`);
- steadfast work — mining, woodcutting, herbs, cooking, peddling, unloading and others — is held against ordinary offers
  for 1.5 times its reckoning, up to 8 minutes, unless its walk runs into trouble (14.09.2026);
- work that has spent money on itself (`Committed`) is not weighed against a better price at all; paperwork waits for the
  next choice; a second guess of the same kind does not replace a guess;
- what gets through: work that will not wait (`Pressing`, a creature beside the bot) and calls from outside (`Summons`:
  a comrade in trouble, the guild's muster, a paid lesson).

All 146 boot lines in this window print these numbers as the code has them ("a point of skill is worth 500
gold … replaced only above ×1.25 against ×1.25 … a bot's own trade is worth ×1.3 and another class's ×0.6; a crowd
bites 0.8, repetition 0.35"); the will's configuration file is empty.

Every win is logged. Anatomy of one, from 26.09.2026 13:48:47:

```
Harlan took on hunt: after an eagle: 31/min = 25 × 0.97, that being the fifth root of near 0.93 × new 1.00 ×
room 0.93 × safe 1.00 × purse 1.00; × 1.30 for its own trade; 3 of 4 offers worth anything; refused: harrow lies in or
beside ground where 4.6 bots have died lately, against 3.0 that close it to everything but running; over peddle: taking
1 Garlic to Lynn at 21/min
```

| part | meaning |
|---|---|
| `31/min` | the score it won at |
| `25` | the estimate: the claim after the island's correction and the bot's ledger (the typed claim is not printed) |
| `0.97` | the combined factor, (0.93 × 1 × 0.93 × 1 × 1)^(1/5) |
| `× 1.30 for its own trade` | the calling, outside the root |
| `3 of 4 offers worth anything` | offers that scored above nought / offers made |
| `refused: …` | the first refused offer of this review, and why |
| `over peddle … at 21/min` | the runner-up and its score |

`load`, `revel`, `ground` and `charter` are printed only when one of them is not neutral (16.85% of lines here); the
toll only when it bites. A line may end with the work it displaced: `dropped …`, `put down … to take up again (…)`, or
`finished …, met by …`.

### 1.9 The overstated alarm

`BotSigns.Overstated` (build 78, 16.09.2026) reads every trade's claim against what it paid on the island's board and
raises `overstated` for the worst one when all of these hold:

- at least 25 outcomes (`BotCommons.TradeConfidence`);
- the claim as the auction reads it — typed for Unpaid, `Realised` for posted, `Corrected` for the rest (since build 95;
  before, the typed number) — is more than 1.5 times what the trade pays (`OverstatedBy`) and at least 5 gold a minute
  above it (`OverstatedGap`, build 98);
- the trade is not Unpaid, has been won in an auction this session, and is not on the `Ranked` list of claims that are
  ranks on purpose or are paid in a currency the board does not price: flee, unload, drill-in, rescue, harrow, band.

Since build 95, what it names is in effect a claim held up by the quarter floor while the trade pays less: a number in
the source still steering the population.

---

## 2. What the logs show

### 2.1 The data, and a check on the reading

`extract.py` read 152 session logs (145 with decisions) and parsed every take line: 462,156, none it could not read.
Two checks run on every line: the printed rate equals estimate × combined factor × calling × toll, and the combined
factor equals the fifth root of the printed factors' product, both within the printed rounding. All 462,156 lines pass
both, so the arithmetic in section 1 is the arithmetic the shard ran.

### 2.2 The claim as the auction read it

| kind | takes | typed claim | read as | estimate p25 / median / p75 | rate taken at (median) |
|---|---|---|---|---|---|
| prowl | 93,608 | 8 | corrected (floor 2), then ledger | 1 / 2 / 6 | 2 |
| mine | 53,019 | 45 | corrected, then ledger | 11 / 45 / 89 | 50 |
| restock | 41,826 | 12 | unpaid: typed | 12 / 12 / 12 | 11 |
| hunt | 36,089 | 60 | corrected, then ledger | 86 / 119 / 162 | 151 |
| forge | 31,471 | 120 | corrected, then ledger | 315 / 469 / 615 | 182 |
| supply | 6,338 | 260 | unpaid: typed | 260 / 260 / 260 | 240 |
| stake | 4,713 | 200 | unpaid: typed | 200 / 200 / 200 | 173 |
| flee | 2,853 | 2,000 | corrected (floor 500), then ledger | 167 / 250 / 333 | 223 |
| evict | 2,052 | 380 | unpaid: typed | 380 / 380 / 380 | 369 |

Unpaid work reads its typed number every time. The typed number of paid work is a starting point the ledger leaves
behind in both directions. And the island's quarter floor does not hold at the ledger: flee's 2,000 cannot be
corrected below 500, yet it was read at 167 to 333, because a bot's own row — up to 12 outcomes against a prior weight
of 2 — has no floor. Mining and hunting were taken above their estimates because both are the taker's own trade
(× 1.30). Full table: `data/by_kind.csv`.

### 2.3 How far the factors bent the winners

| factor | below 1 on | median when below 1 | 10th percentile when below 1 | above 1 on |
|---|---|---|---|---|
| near | 77.9% | 0.81 | 0.43 | – |
| room | 78.8% | 0.92 | 0.73 | – |
| new | 53.3% | 0.59 | 0.02 | – |
| purse | 10.1% | 0.38 | 0.10 | – |
| ground | 6.44% (all at 0.70) | 0.70 | 0.70 | 6.88% (1.25) |
| safe | 0.43%, 1,977 takes (all at 0.15) | 0.15 | 0.15 | – |
| load | 6 takes (0.02) | 0.02 | 0.02 | – |
| revel | – | – | – | 2.55% (3.00) |
| charter | – | – | – | 1.64% (1.50) |

The smallest factor of a take had a median of 0.59; the combined factor had a median of 0.86. `new` alone was below
0.30 on 12.0% of winning offers; the combined factor was below 0.30 on 3.4% and below 0.60 on 9.1%, and never below
0.10. Every factor was at 1 or above on 3.4% of takes. Sources: `data/factors.csv`, `data/factor_bins.csv`.

### 2.4 Which factor bent the winner most

The factor with the lowest printed value on each take (`data/smallest_factor.csv`):

| near | new | room | purse | ground | safe | load | a tie | nothing below 1 |
|---|---|---|---|---|---|---|---|---|
| 40.5% | 37.5% | 7.3% | 7.1% | 2.6% | 0.4% | 6 takes | 1.2% | 3.4% |

By kind (`data/smallest_factor_by_kind.csv`), `near` decides the walks to far ground — herbs 92% of their takes, prowl
79% — and `new` decides the work repeated at one place: forge 97%, chop 87%, sew 80%, mine 66%.

Counting each consideration as it reaches the score — a factor f under the root as f^(1/5), the calling and the toll in
full — another class's trade (× 0.60) was the deepest cut on 4,088 takes (0.9%); a factor under the root would have to
be below 0.08 to cut as deep.

This measures what bent the winner, not what decided the auction: the runner-up's factors are not printed.

### 2.5 How close the choices were

- 21.5% of takes had one offer made and 37.3% one offer worth anything. Prowl, the designated answer to having nothing to
  do, was the only offer worth anything on 91.8% of its 93,608 takes.
- Where a runner-up existed (62.7% of takes), the winner's rate was a median 3.04 times the runner-up's (quartiles 1.71
  and 7.79); 10.9% were won by less than 1.25 times.
- On 204 takes the runner-up scored higher than the winner: the carrying-ceiling rank overturning a price (the census
  counts 198 such times in the sessions it covers).
- The bot's own trade (× 1.30) was on 37.5% of winning offers, another class's (× 0.60) on 0.9%; the toll bit 17 takes.
- 13,399 takes displaced work in hand: 5,891 dropped it, 4,181 put it down to take up again (1,448 summoned, 1,278
  interrupted, 1,002 jumped, 453 dislodged), and 3,327 counted it as met (a prowl or a walk home that found what it went
  for).

Sources: `data/summary.json`, `data/by_kind.csv`, `data/runner_up_pairs.csv`.

### 2.6 What was refused

First refused offer printed on a take line (`data/refusals.csv`, `data/refusals_by_kind.csv`):

| reason | first refusals | share | refused most |
|---|---|---|---|
| expected to pay nothing or less here | 94,696 | 54.3% | prowl 74,041; unload 9,395; band 4,036 |
| cannot pay to start | 67,810 | 38.9% | restock 46,690; acquire 7,227; sew 6,867 |
| bots died there lately: closed to all but running (until 26.09) | 8,920 | 5.1% | prowl 5,408; reclaim 1,078; harrow 818 |
| the other side of a dungeon's edge | 1,149 | 0.7% | peddle 471; homeward 273 |
| bots died there lately: ordinary work kept out (until 26.09) | 775 | 0.4% | reclaim 376; mine 245 |
| a long walk from here lately closed nothing | 657 | 0.4% | unload 243; herbs 108 |
| rested after failing the same way again and again | 203 | 0.1% | rally 135; peddle 31 |
| hostile strength three times what goes (from 26.09 14:05:56) | 182 | 0.1% | reclaim 137; restock 33 |
| a red bot and a guarded town | 66 | 0.04% | unload 42 |
| a counter, and no room in the pack for a coin | 6 | – | peddle 5 |

The take line names only the first refused offer, in the order the proposers were asked, and only in reviews that ended
in a take. The five-minute census (`Will:` line) counts every refusal for the gates that keep a counter; summed over the
last census of 116 sessions (`data/census.csv`):

| census counter | total |
|---|---|
| refused: bots died there lately, ordinary work kept out (until 26.09) | 154,514 |
| refused: so many died there it was closed to all but running (until 26.09) | 86,300 |
| refused: across a dungeon's edge | 70,954 |
| refused: a long walk from here closed nothing | 31,741 |
| refused while the breaker rested the work | 1,828 |
| refused: hostile strength three times what goes (26.09 from 14:05) | 647 |
| refused: no room in the pack for a coin | 44 |
| prowls let in at 0.01 after two idle minutes | 44 |
| unpaid work let past the earnings veto | 0 |
| times nothing at all was worth doing | 48,287 |
| better offers turned down because the work in hand had already spent money | 126,711 |

These are appraisals: the same offer refused at each review counts again, and an idle bot reviews every two seconds.
The census counted no unpaid work let in at 0.01: every unpaid claim weighed in this window was above nought, and the
0.01 entry exists for one that is not.

### 2.7 Claims on the floor, and readings that do not move

Inside the "expected to pay" refusals the take line prints both numbers: the claim after the island's correction, and
the bot's expectation after the ledger (`data/expected_to_pay_nothing.csv`):

| refused kind | refusals | claim (median) | typed | expected (median) | expected below 0 |
|---|---|---|---|---|---|
| prowl | 74,041 | 2.0 | 8 | −10.4 | 99.2% |
| unload | 9,395 | 7.5 | 30 | −12.6 | 99.5% |
| band | 4,036 | 7.5 | 30 | −5.8 | 100% |
| drill | 650 | 8.8 | 35 | −3.3 | 100% |

Each median claim is a quarter of the typed number: the correction's floor. The claim can fall no further, so the
refusal comes from the ledger side.

Of 32,567 distinct readings (session, kind, expected, claim) in these refusals, 996 were printed again, unchanged, an hour
or more after they first appeared, and 979 of those were read by more than one bot (`data/unchanged_expectations.csv`):

| session began | reading | times | bots | hours standing |
|---|---|---|---|---|
| 18.09 06:30 | prowl expected to pay −51.8 against a claim of 2.0 | 958 | 64 | 3.89 |
| 22.09 05:11 | prowl expected to pay −38.0 against a claim of 2.0 | 879 | 61 | 4.07 |
| 18.09 03:05 | prowl expected to pay −28.3 against a claim of 2.0 | 733 | 66 | 2.33 |
| 25.09 08:44 | prowl expected to pay −11.0 against a claim of 2.0 | 36 | 21 | 6.55 |
| 20.09 08:22 | drill-in expected to pay −4.7 against a claim of 72.0 | 83 | 24 | 4.74 |

The −38.0 is the reading the decision log found at The Needle's hall on the morning of 22.09 (build 181, lesson 13). A
reading identical to the tenth for sixty bots is the island's record of a patch, not sixty private rows — an inference,
since the line does not name its source, but private rows are running averages of each bot's own outcomes and would not
agree. The code explains how such a reading stands: the island's record fades only while positive, a bot's own row has
no clock, and the refusal stands in the way of the very work that would write a new outcome — only bots with rows of
their own there, or restless prowls, can still go. A bot's row goes only by eviction,
when it is the least recently touched of more than 48. The ledger's comment promises the opposite ("tries again later
when the row has faded"); only its repetition count fades. Build 181's restless prowl patches one face of this: after
two idle minutes a prowl enters at 0.01 whatever the record says (44 times in this window).

### 2.8 The overstated alarm in practice

Between 16.09 and 26.09 the alarm wrote 591 raised or repeated lines naming 30 trades, and 151 clears
(`data/overstated_alarms.csv`, `data/overstated_by_trade.csv`):

| trade | lines | claim as compared | paid | most outcomes |
|---|---|---|---|---|
| band | 128 | 3 to 90 | −58 to 11 | 2,236 |
| sweep | 73 | 11 | −2 to 3 | 76 |
| mend | 71 | 8 to 74 | −69 to 20 | 2,376 |
| drill | 41 | 9 | −9 to 4 | 337 |
| prowl | 38 | 2 | −160 to −3 | 69,079 |
| liberate | 36 | 55 | −2 to 7 | 45 |
| mine | 29 | 11 | −31 to 6 | 38,959 |
| harrow | 28 | 38 | −1 to 25 | 46 |

Its first word, at 10:48:06 on 16.09, was `unload claims 120/min and pays 21/min over 171 outcomes`; flee (2,000
against 1 to 28) followed within fifteen minutes. The alarm names one trade at a time, so a trade it cannot stop naming
hides every other behind it — the reason harrow (17.09) and band (25.09) joined the ranked list. Prowl is the clearest
case of the floor at work: read at 2, a quarter of its typed 8, while paying below nothing over 69,079 outcomes.

### 2.9 A smith and a novelty of zero

Edda took forge 21,929 times between 20.09 and 25.09 — 69.7% of all forge takes — 18,518 of them with `new` printed as
0.00, at a median estimate of 536 a minute and a median rate of 164 (`data/bot_kind_top.csv`). Forge's combined factor
had a median of 0.30 over all its takes, and its usual runner-up was mining at a median of 69 a minute
(`data/runner_up_pairs.csv`). The decision log (build 227) found the cause: from 10:18 to about 12:00 on 25.09 she
finished "handing over WarFork" 20,494 times, three a second, because the forge counted a war fork bound to her, carried
in her pack, as made; each finish paid, the forge's learned worth rose to 276 a minute, and the next offer read the work
at hundreds a minute. The weighing did what it was built to do — novelty fell to nothing — and could not stop it:
under the fifth root a novelty near nought still leaves roughly a third of the estimate, and a third of 536 beats 69. A
repetition discount under a root is a preference, not a brake; the brake had to be the cause.

---

## 3. Where a weight went wrong: lessons from the decision log

Each item: what happened, with the numbers the decision log or the code comment recorded, and what changed. The rule
names (A1, A2, …) are the invariants in section 2.1 of `DECISIONS.md`.

1. **A factor without a floor is a veto (25.08.2026).** The purse factor was 1 − need × (1 − coin) with no floor, so an
   empty purse made it exactly nought for any work that pays no coin — including prowling, the answer to having nothing
   to do. Cedric spent his last coins and stood in a field for ten minutes. `LeastPurse` 0.10 was added, the floor
   `LeastRoom` already had; the log records failures falling from 45% to 20%. Rule A2: every factor that ordinary state
   can drive to nought has a floor; nought is kept for "this cannot happen".
2. **A number that is both a promise and a wager (25.08.2026).** A thinking bot's forecast was also its bid, and the
   model found the exploit within a day: Aldric wrote himself the rule "always predict zero return on this shard" and bid
   nought on three trades running — 24 decisions, 2 taken up. Now the bid is the work's own claim × 1.25
   (`BotMindDeed.Insistence`) and the forecast is only measured. The same shape in a typed claim: unload's 120 was both
   a promise of pay and a wager meant to carry a full pack to the counter; on 16.09 the promise came down to 30 and the
   wager stayed as `Stuck` 120 for a bot that cannot move (build 91).
3. **An opening number that seals itself (07.09.2026).** Forge's claim of 55 never beat mining's 60 to 67: in an hour,
   51 offers and not one take. A claim is corrected only by work done, so a claim too low to win is never corrected.
   Raised to 120, the first daggers came off the anvil within fifteen minutes at 940 and 1,300 a minute. Rule A9: an
   estimate that can only be corrected by winning does not open below the winners.
4. **Unpaid work refused for being unpaid (07.09 and 09.09.2026).** The Baron's rounds pay nothing by design; the ledger
   measured them at −40.5 a minute, the "expected to pay nothing" veto refused his only work, and he stood in Britain for
   twelve minutes with 600gp. The fix was a flag, `Unpaid`, not a floor on the estimate — a floor would have rescued every
   unprofitable errand along with it. The walk home had the same defect two days later: 416 "nothing worth doing" in 80
   minutes, all in Britain; 22 after it was made unpaid.
5. **The correction reached the duty one step earlier (night of 13–14.09.2026).** The island's correction still ran on
   unpaid claims and dragged each to its quarter floor: rally 400 offered at 175, then 100; stake 200 at 50 in 318 of 370
   offers; a war company's founder lost the auction to a reclaim at 41 a minute. An unpaid claim is now taken as typed.
   The other face of the flag: unpaid work learns nothing, so its proposer is its only gate. Restocking, made unpaid the
   next morning without one, failed 8,719 times in half an hour for one bot whose pack could not take a coin — hence the
   counter veto (build 37). Rule A4: an `Unpaid` flag arrives with a gate for every reason the work fails.
6. **Collective payment read as personal loss (09.09.2026).** Takings are measured on the bot's own purse and guild-paid
   work drew on it: raising a hall read −50 a minute, hiring −93, and the island's correction taught the shard not to
   build. Such work now declares what the bot's own purse put in as goods made (A1).
7. **A road that does not exist is not a price (08.09.2026).** Work that never reached its place settled at nought and was
   written into the bot's ledger, the island's record of the place and the trade's correction: cooking read −309.9 a
   minute in the middle of Britain, and "nothing was worth doing" reached 9,099 in a session. Unreached work is no longer
   priced; the count fell to 2 (A8).
8. **"Cannot" is not a factor under a fifth root (08–09.09.2026).** A bot over its carrying ceiling cannot take a step.
   As a veto, it left three crafters idle for 1,677, 1,640 and 1,632 seconds, never reaching the unloading that would free
   them; as a factor of 0.02 it reached the score as 0.46, and Lysa took chopping at 229 a minute over her own unloading at
   74. It became a rank, and then work that had already spent money was marked `Committed` so the rank could not take it
   off a courier mid-purchase. Rule A2: *cannot* is a veto, *a poor idea* a factor with a floor, *must come first* a rank,
   *paid nothing on purpose* a flag.
9. **A line that cannot be checked against itself.** The take line once printed `8/min = 14 × 0.97 × 0.49 × 0.93 × 0.15 ×
   1.00`, which multiplies out to 0.9; the arithmetic was a geometric mean and only the sentence was wrong, and it cost an
   hour of chasing a defect that was not there. Then load, revel, ground and charter were added over a fortnight without
   being printed. The line now names the root and prints the four when they bite; section 2.1 checks all 462,156 lines
   against it.
10. **"safe 1.00" in a killing field, and the fight instead of the dead (14.09 and 26.09.2026).** The only caution the
    appraisal read was the bot's own ledger, keyed by trade: from 20:40 on 14.09, 28 bots died within 20 minutes in one
    field north-east of Britain on offers that read "safe 1.00". A danger factor was rejected — under the fifth root a
    floor of 0.15 is 0.68, too weak to stop one of them — for a veto on recent deaths, exempting work that goes where the
    fighting is (build 39). With the world awake on 26.09 that count of the dead closed the ground round the guilds' halls
    (150 restocks, 84 harrows and 72 reclaims were the first veto printed in forty minutes), while of 83 bots killed by
    creatures only 19 had met something three times their strength. From build 260 ground is closed only where the
    hostile strength is at least three times what goes there.
11. **A posted price corrected like a guess (16.09.2026).** A board errand's reward was pulled towards what errands had
    paid on average — nothing, since each was let go before it paid — so one scout errand was taken at 49, 23, 13, 9 and 6
    a minute in seventeen minutes and lost to a cook at 18. Posted claims are now corrected by the share of posted
    rewards actually earned (`Posted`, `Realised`, build 95).
12. **The alarm and the claims that are ranks (16–25.09.2026).** The alarm first compared typed numbers and named trades
    the correction had already tamed (mend "74 against 20" was read by every auction as 24); from build 95 it compares
    the claim as the auction reads it. On its word four claims came down on 16.09 (unload 120 → 30, rescue 400 → 200, band
    90 → 30, reclaim 80 → 20), and enlist 70 → 24 and band 30 → 12 were dialled on the running shard on 22.09. Claims that
    are ranks, or are paid in skill the board does not price, went on a named list instead of being lowered.
13. **The walk to somewhere else, refused to the bots with nowhere to be (22.09.2026).** Six archers stood at their hall's
    door reading "prowl is expected to pay −38.0/min here, against a claim of 2.0" — 7 of the session's 13 idle reports at
    one hall. A prowl now enters at 0.01 after two idle minutes (build 181). Making prowl unpaid was rejected because it
    would throw the ledger away for the bots that have a choice. Section 2.7 shows the wider shape behind it.
14. **A novelty of zero (25.09.2026).** Section 2.9: a repetition discount under a fifth root cannot stop a loop that the
    ledger believes pays; the forge now counts only what the pack gains (build 227).

---

## 4. What is uncertain

- **What decided an auction is not in the log.** The take line prints the winner's factors and estimate and only the
  runner-up's score. "Smallest factor" says what bent the winner most, not what made it win.
- **The typed claim and the island-corrected claim are not on the take line.** The estimate is both corrections at once.
  Only the "expected to pay" refusals print the corrected claim and the expectation side by side; the typed claims in
  this write-up come from the code.
- **Refusal counts are partial.** The take line names the first refused offer only, in proposer order, and only for
  reviews that ended in a take. The census counts appraisals (repeated at every review), misses whatever happened after
  a session's last census (up to five minutes), and misses sessions shorter than one census.
- **Rounding.** Estimates and rates are printed as whole numbers and factors to two places, so margins between small
  rates are coarse (2 against 1 is "2.0 times") and a factor printed 0.00 is anything below 0.005.
- **The window is nine days of a shard that kept changing.** The logs in the folder start on 18.09; the rules changed
  inside the window (the danger veto at 26.09 14:05, road-priced nearness on 26.09, the forge fix on 25.09 around 15:25,
  live dials on 22.09). `data/by_date.csv` and `data/sessions.csv` allow splitting by time.
- **One loop dominates one kind.** 69.7% of forge takes are one bot's (2.9); forge's figures describe that loop more than
  smithing.
- **The unchanged readings are grouped by printed value.** Two places with the same printed reading in one session fall
  into one row; that a reading shared by many bots comes from the island's record of a patch is an inference.
- **Dials move at runtime.** Code defaults and the boot line agree for this window, but values dialled on a running shard
  (the enlist and band claims on 22.09, for example) are not on the take line.

---

## 5. Regenerating

```
python extract.py <logs folder> --until "2026-09-26 15:30:00"
```

`<logs folder>` holds the `session-YYYY-MM-DD_HH-MM.log` files and `alerts*.ndjson` (the current alarm file and any
archived one; duplicates are dropped). The cutoff makes a run repeatable while the shard keeps writing; the numbers
here were produced with exactly that command, and a second run writes the same bytes. Python 3 standard library only;
about 35 seconds for the 372 MB of logs. The script writes `data/` and replaces the JSON between the markers in
`index.html` (`--no-html` leaves the page alone). Take lines carry only the time of day, so a session that runs past
midnight is dated by watching the clock wrap.

---

## 6. Files in `data/`

| file | rows | what it holds |
|---|---|---|
| `takes.csv.gz` | 462,156 | one row per take line (columns below), gzip-compressed |
| `takes-sample.csv` | 1,849 | every 250th row of the above, uncompressed |
| `summary.json` | – | the headline figures, the self-check and the census totals |
| `by_kind.csv` | 59 | per kind: takes, bots, estimate and rate quartiles, combined factor, calling, refusal and runner-up shares, how often each factor bites, the top bot's share |
| `factors.csv` | 9 | per factor: how often below and above 1, median and 10th percentile when below |
| `factor_bins.csv` | 70 | each factor and the combined factor, by printed value |
| `smallest_factor.csv` | 13 | which factor was smallest, and which had the largest effect on the score |
| `smallest_factor_by_kind.csv` | 59 | the same, per kind |
| `refusals.csv` | 10 | first refusals by reason, with the kinds refused most and an example |
| `refusals_by_kind.csv` | 126 | first refusals by reason and refused kind |
| `expected_to_pay_nothing.csv` | 24 | the "expected to pay" refusals by kind: claim and expectation |
| `unchanged_expectations.csv` | 996 | "expected to pay" readings printed unchanged for an hour or more within a session |
| `runner_up_pairs.csv` | 60 | the most common winner and runner-up pairs, with median rates |
| `bot_kind_top.csv` | 40 | the bot and kind pairs with the most takes |
| `by_date.csv` | 8 | per date: takes, bots, shares, first refusals by reason |
| `sessions.csv` | 152 | per session log: first and last take, takes, first refusals by reason |
| `census.csv` | 116 | the last `Will:` census of each session, as numbers |
| `overstated_alarms.csv` | 742 | every overstated alarm line: time, state, trade, claim as compared, pay, outcomes |
| `overstated_by_trade.csv` | 30 | the alarm by trade |

Columns of `takes.csv.gz`: `date`, `time`, `session`, `bot`, `kind`, `stage` (the deed's own words), `rate` (the score it
won at), `estimate` (the claim after both corrections), `factor` (the combined factor), `near`, `new`, `room`, `safe`,
`purse`, `load`, `revel`, `ground`, `charter` (1.00 where the line did not print them), `calling` and `calling_word`,
`toll`, `viable` and `offered` ("N of M offers worth anything"), `refused_kind`, `refused_class`, `refused_reason` (the
first refusal), `runner_kind`, `runner_rate`, `displaced_as` (`dropped`, `put down`, `met`) and `displaced_kind`,
`smallest_factor` and `smallest_value`.
