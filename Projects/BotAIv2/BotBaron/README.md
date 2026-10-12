# BotBaron — the bot that is not making a living

Every other class on this shard answers the question *how does this bot get by*. The Baron answers a
question nothing else on the island asks: **who deals with the ground that has already killed people.**

Nobody else will. Hunting goes where hunting pays, and the places that have proved they kill are precisely
the places nobody profits from — a captain's patrol comes closest, but a patrol is answerable to a *decaying*
reading and can be satisfied by the trouble simply wandering off. So this folder is one bot who is paid
nothing, hands away everything his company takes, and is finished only by a count of corpses or a clock.

## The pieces

| File | What it is |
|---|---|
| `BotHarrower.cs` | Proposer `Baron`. Offers the deadliest reachable ground and five bots to take there. |
| `BotHarrow.cs` | The deed. Calls six, marches, walks a 75-tile box, kills everything in it, clears the square. |
| `BotStroll.cs` | Proposer `Stroll`. Offers his own town. |
| `BotRounds.cs` | The deed. Walks the town. Pays nothing, and is meant to. |
| `BotRegalia.cs` | Halberd, gold plate, red cloak. Bound, and none of them wears out. |
| `BotStipend.cs` | The crown's money — the one purse on the shard that is not earned. |
| `BotBaronModule.cs` | Registration, `Configuration/bot-baron.json`, and one summary line every five minutes. |

The class itself is `BotClasses/BotBaron.cs`, with the rest of the classes, because that is where a class
lives.

## The four decisions worth knowing

**Sworn, not priced.** He is not offered ordinary work at all — `BotClass.Sworn` names the five proposers he
may hear from and `BotWill` withholds everything else. The alternative was to price his errands high enough
to win, which is a thumb on the scale, or to price them honestly, which leaves him mining. A gate is the only
honest way to say "this bot works for a reason the arithmetic cannot see".

**Deaths, not the reading.** `BotPeril.Worst` is a decaying frequency and answers *where is blood being spilt
lately* — right for a captain deciding where to stand before anything happens. `BotPeril.Deadliest` is a count
that does not fade and answers *where has it already gone wrong*. The two lists disagree constantly and both
are correct about their own question.

**Cleared, not swept.** A patrol knocks a square's reading down and lets it climb back, because standing in a
place proves nothing about what is still in it. A harrowing removes the row. That is not a stronger version of
the same idea: twenty corpses or forty minutes of six bots inside seventy-five tiles is the ground having been
spent, and leaving the dead on the count afterwards would send the same company to the same coordinates for
ever — the dead are the one number that never decays.

**Worth what it hands to the others.** He takes no share, so measured the ordinary way — coin in his own pack
— this work pays nothing, and the ledger would have learned, correctly by its own arithmetic, that leading
companies into deadly ground is worthless. `BotSquad.Won` keeps what the company divided, and the deed reports
that as what it made.

## What to watch in the log

- `The Baron:` — every five minutes. Offers, marches, why not, squares emptied against squares timed out.
- `is harrowing (x, y) with N of them` — a company actually set out.
- `has been harrowed off the board altogether` — a square is gone. This is the only line on the shard that
  says that.
- `the crown made it up to` — what he has cost, in gold, since the shard came up.

## The thing to be suspicious of

`Least` is six and `Company` is six. If `TooFewNear` and `Undermanned` are large and `Marches` is nought, the
gate never opens — that is the shape of defect this project keeps finding, a threshold that reads as a policy
and behaves as a veto. Both numbers are in `bot-baron.json` and neither needs a rebuild.
