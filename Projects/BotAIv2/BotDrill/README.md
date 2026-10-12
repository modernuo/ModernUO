# The Captain

One bot on this shard exists for the others rather than for itself.

| File | What is in it |
|---|---|
| `../BotClasses/BotCaptain.cs` | the class: born Expert, bow and blade, allowed to lead and to teach |
| `../BotCombat/BotPeril.cs` | where the island is dangerous, learned from blows and deaths |
| `../BotSquad/BotPatrol.cs` | the offer only a captain ever gets: a company and the worst square on the map |
| `../BotSquad/BotSweep.cs` | the patrol itself — march, hold, clear, come home |
| `BotSchool.cs` | the training field: the block, the roster, the fee and the formula; places spoken for by students on their way (`Promise`); ten students a class and twelve minutes' rest between lessons since build 64 |
| `BotDrill.cs` | two proposers — the captain offering a class, and a student offering itself a place |
| `BotLesson.cs` | the captain's half: open the field, close the roll, pace the ranks; a post of the ring with no road is walked past, not the end of the class |
| `BotAttend.cs` | the student's half: speak for a place when chosen, pay, take your place, keep it; a station with no road is given up for where it stands |
| `BotArmourer.cs` | the first demand for armour this shard has ever had |
| `BotDrillModule.cs` | module and `Configuration/bot-drill.json` |

## Three offices, one rule

A captain does three things no other bot does, and every one of them is an **offer**, never an order:

1. **It calls companies for places.** Ordinary musters form against one creature and end when it dies. A
   patrol is dispatched to a *square* that has been hurting people and stays until the square is quiet.
2. **It teaches, for money.** Warriors and archers may buy points up to the captain's own standing.
3. **It wants armour.** It is the first bot on the island ever to notice it is fighting in its shirt.

Nobody is obliged to follow, to attend, or to sell it anything. Authority here is exactly one thing: a
proposer that answers one class. Everything else goes through the same auction as mining.

## Born finished, and only this class

`BotClass.Seasoned` skips the 50/30/20 starting ladder and gives the class its declared skills outright.
Exactly one class sets it, and the reason is not "it would be stronger" — it is that **both of its offices
are impossible without it**. It cannot lead a company into ground that has killed somebody while it is still
learning, and it cannot teach what it does not know.

`BotClass.Closes` is the other half of its identity, and it is one moment of arithmetic: when something
comes inside an archer's standoff, an archer's whole case for existing is that it is somewhere else, and a
captain's is that it is exactly here. `BotArms.Suit` is asked from **both** places a bot can be in a fight —
`BotSlay` when hunting alone and `BotSquad.Press` when fighting in a company — because a rule written into
only one of those is a captain that draws steel alone and stands holding a bow at arm's length the moment it
is leading the company it exists to lead.

## The peril map

Two hooks feed it and nothing else can: `OnDamage` and `OnDeath`. A blow counts 1, a death counts 25, and
every reading **halves every twenty minutes**.

The decay is the whole design. A tally that only rises names the graveyard for ever, because the graveyard
has always been the worst place on the map — which is a fact about history, not about tonight. What a
captain needs is where blood is being spilt *lately*.

A sweep knocks a square down to a quarter rather than clearing it: cleared outright, a square reads as safe
the instant the company arrives, the patrol ends, and the same square — still full of whatever was killing
people — is the worst on the map again by the time the walk home is over.

## The formula

Per beat, per student:

```
gain = Rate × Room × Attention          capped at (captain's skill − student's skill)

Room       = clamp((ceiling − held) / (ceiling − 30), LeastRoom, 1)
Attention  = 1.0 within Voice tiles of the captain, else Distant
ceiling    = the captain's own base in that skill
```

**The ceiling is the captain's own skill, and there is no second constant anywhere saying "Expert".** He is
an Expert, therefore he teaches to Expert. Two numbers on one shelf is the defect this project keeps paying
for, and the cheapest way not to have it is not to have the second number.

**Attention is why the walking matters.** A lesson that granted its points on arrival would be a shop that
sells skill and the captain pacing the ranks would be scenery. He circles the block one post per beat, so
whoever he is standing over this beat learns three times what the far corner does — visibly, in the numbers.

**Which skill** is the student's own fighting trade first, and the widest gap only among equals. Ranked on
the gap alone — which is how it was first written — a captain taught every single pupil *Healing*: it holds
72 of it and a young warrior holds almost none, so the arithmetic was right and the answer was absurd. Six
warriors and archers stood on a drill field paying to be taught first aid by a man with a bow.

## The chessboard

Stations are **derived, not assigned** — the squad's own rule applied to a different problem. The shared
facts are the ground, the roster and the order the roster is in; every student's tile falls out of those
three identically for everybody who asks. Nobody is told where to stand, two students cannot be sent to the
same tile, and a student that dies orphans no assignment.

`Pace = 2` is what makes it a chessboard rather than a huddle: every station lands on a tile of the same
parity as the ground, so the occupied tiles are the light squares and the dark ones are the aisles.

The captain walks the **outside** of the block. Walking between the ranks would put him on a station, shove
a student off its tile, and re-form the whole block behind him.

## What it cost to get right

- **Standing companies did nothing.** A squad member is `Bound`, and a Bound bot's own auction is skipped on
  purpose — the company owns where it stands and what it hits. The consequence nobody had followed through:
  a company with no focus is bots with *no source of work at all*. Every muster on this shard ended with
  five bots standing in a graveyard for five minutes until the quiet clock disbanded them. `BotSquad.Hunt`
  is the missing line; the idle cap came down from 300s to 45s in the same edit, because those two numbers
  were the pair.
- **A price is not a fee.** Forty gold for a lesson delivering points this island values at five hundred
  apiece is not a price, it is a subsidy, and the whole population would rationally do nothing else.
- **A question must not leave a mark.** Counting pupils by briefly installing the captain as the field's
  master is a query that mutates what it queries: one throw and the shard believes a class is running that
  nobody is teaching. The master is a parameter.
