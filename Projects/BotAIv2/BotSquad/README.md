# Squads

A standing company: a leader, a few followers, a formation, scouting, and dividing the spoils.

> **This ran, and the note that used to stand here saying it never had is why the top of `MAP.md` warns
> against reading these files for facts.** `BotSquads.Form` has three callers now — a prowl that finds
> something one bot cannot take, a patrol, and the Baron's harrowing — and companies form and disband on
> every session. What follows was written before that and describes intent; where it disagrees with the
> `Companies:` line in a live log, the log is right.

| File | What is in it |
|---|---|
| `BotSquad.cs` | the squad itself: members, leader, focus, three states, anchor and axis |
| `BotSquads.cs` | the registry: forming, joining, leaving, disbanding, its own beat once a second; `Join` refuses a bot more than `JoinReach` from the leader, because the dungeons share the island's facet, and `Reaches` is that question for anything that sends a bot to a company (build 49) |
| `BotFormation.cs` | where each one stands — derived, not assigned |
| `BotScatter.cs` | scouting: knots of 2–3 ten tiles apart |
| `BotSpoils.cs` | dividing by worth, gold by sum |
| `BotSquadMember.cs` | what a squad needs from a bot. Four things |
| `BotSquadConfig.cs` | `Configuration/bot-squad.json` — numbers only |
| `BotSquadModule.cs` | module, phase `World`, requires `Classes` |
| `BotEnlist.cs` | falling in with a company that is already fighting, rather than starting a fight of your own beside it; not offered a company whose leader stands too far off for the door (build 49) |
| `BotPatrol.cs` | offers a captain the worst square on the island and a company to take there |
| `BotSweep.cs` | a company called together for a place rather than for a creature, and kept together until the place stops killing people |

## One kind of group, not two

In v1 there were two, and both were called "a group" — including in the documentation.

| | warband (`BotSocial`) | band (`BotBands`) |
|---|---|---|
| where it came from | a bot met something it could not take alone | the system raised one every minute |
| size | however many arrived | exactly five |
| a medic | if you were lucky | mandatory, and the leader |
| goal | one creature | a square of the map |
| end | the creature died | the square was cleared |

Here there is one: it forms, it lives, it marches, it scouts, it fights, it divides, and it moves on.

## Collective intelligence is arithmetic, not mail

The most valuable thing v1 found about group behaviour was buried in how it scattered across a square:

> Handing out is a **division, not a search**: the bots sort by serial, the ground is cut into a grid, each takes
> the cell at its own index. Everybody derives the same answer from the same facts, so **nobody has to be told
> anything**, and two of them never pick the same patch.

The whole design comes from that. The shared facts are the membership, the anchor and the threat axis. Each bot's
station, its scouting patch and its share of the spoils are **derived** from them, identically by everybody. No
desynchronisation, no orphaned assignments, no message queue.

The only things that remain messages are the things that are **events**: "I am being hit" (`BotSquads.Note`) and
"get off this tile" (`IBotAside`).

**A station is written where the bot's own walk is kept, and leaving gives nothing back.** Stationing rebases the
bottom of the member's road (`BotJourney.Rebase`) — the same slot `BotWill` keeps the bot's own work in — so there is no
errand underneath to fall back to when the company lets the bot go. Its last station stays there: a "sweep" one tile
away reads as arrived on every beat, and on 14.09.2026 Kestrel, Lorcan and Cassia each stood four minutes on one after
their companies were done with them. `BotWill` now sends a bot's own walk again whenever the bottom of the road is no
longer that walk (`BotWill.Resent`).

## No state is "stand and wait"

Three states, and in each one a bot is going somewhere:

| State | Anchor | What they do |
|---|---|---|
| `Marching` | the leader | hold formation around it |
| `Scouting` | the leader | spread out in knots of 2–3 to ten tiles |
| `Fighting` | **whoever is being hit** | the formation moves onto them, so everybody is already heading there |

**This rule was paid for in bodies.** In v1 both mustering (`DoMuster`) and settling up (`DoSettle`) began with
`Warmode = false` and standing still. And the muster point was almost always already inside the muster radius, so
"go to the muster" degenerated into "stand and look at the target". A lich strikes from eight tiles and **never
enters contact** — so not one rung of the survival ladder ever fired, because every one of them tested for
contact. Six bots stood politely in a circle while it killed them one at a time.

## Rescue is not a mechanic

There is no separate rescue. A squad has an anchor, and everybody's station is derived from the anchor. When a
member is attacked the anchor moves **onto them** — and everybody is already heading there, deciding nothing and
calling nobody. Including whoever is in a distant scouting knot at that moment: their patch was derived from the
same anchor.

The same thing answers "why is scouting safe". Spreading out is only dangerous in a world where nobody hears a
shout.

## Formation: any shape, but not any order

The shape cannot be dictated — "any shape at all". The invariant is the **order along the threat axis**:

| Role | Rank | Where that is |
|---|---|---|
| `Melee` | **+2** | in front of the anchor |
| `Ranged` | 0 | on the anchor |
| `Caster`, `Medic` | −1 | behind it |
| `Producer` | −2 | at the back |

The depth is only four tiles, on purpose. A squad has to stay within five, and a deeper formation looks good on
paper and produces a back rank that cannot hear its own front rank.

Within a rank: 0, +1, −1, +2, −2 at two tiles apart — the rank grows away from the axis in both directions rather
than trailing off in one, and there is room to walk between the files instead of asking somebody to move.

**It is built from the leader, and "in front" means in front of the leader.** If the leader is an archer, the
melee bots still take the front rank: in front of *it*. That is not a defect to be fixed but the behaviour that
was asked for.

Every station is checked twice before it is handed out, and both checks come out of v1's bill: the point has to be
somewhere a body **can stand** (height taken from the ground next to the anchor, never from a wide window — a wide
window in a built-up world returns a roof), and somewhere the bot **can reach**, verified with the same pathfinder
that will then walk it. v1's muster points were not checked at all and landed inside houses and behind the target.

## The gap a mage was standing in

The case as put: two trees with a gap between them, a mage in the gap, an enemy beyond the trees. The mage dies
there in seconds and a melee bot in minutes. So the gap belongs to the melee bot.

`BotFormation.OutranksFor` answers that with **a question about role, not about the path**: the asker's rank is
ahead of the occupant's, so the occupant yields. Nobody recognised a chokepoint; ground nearer the threat simply
belongs to whoever stands nearer the threat, and the formation has already said who that is.

It does not work the other way: a mage cannot move a melee bot. The melee bot is standing exactly where it
belongs.

The mage yields **away from the focus** (`YieldAwayFrom`) — so it does not merely vacate the tile, it vacates it in
the direction it wanted to go anyway: back behind the line, onto its own station.

## Dividing: by worth, gold by sum

v1 dealt round the circle by item count and called it "fair enough that nobody has grounds to complain" — but one
bot walked away with a katana and another with a rotten skull, and the gold went in one heap to whoever opened the
corpse.

Here: gold is cut by sum, and everything else goes to **whoever has had least**, starting with the most valuable
thing. That last detail is what makes it work: hand out the valuable things first and the small change can even
out the difference; hand them out last and it cannot.

Worth comes from an external function, `BotSpoils.Worth`, which the economy will provide. Without it everything is
worth the same and dividing degenerates into counting items — that is, into v1's rule arrived at as a **limiting
case** rather than as a design. It is now a one-line fix: `BotAuction.Worth(type, 0)` is a price.

**It is settled immediately, and the dead get nothing.** v1 held the corpse undivided until every fallen bot had
been resurrected, with the survivors standing around it — and standing around is what killed the six in a circle
round the lich. A division that waits is a state in which the squad goes nowhere, and there are no such states
here. It is a real loss for whoever died in a won fight, and it is cheaper than a second one.

**Height counts as much as distance.** In v1 every range check ignored it, and a squad member three tiles away and
twenty units up — on a crypt roof — counted as present in a fight it could not take part in.

## Leaving a fight is measured in damage

Not by a timer and not by proximity. While the target's health goes **below its previous low for this fight**, the
squad is working. Ninety seconds without a new low: withdraw. Four minutes: the ceiling.

The low for the fight, not the current health: otherwise regeneration between blows reads as progress.

**Why this is mandatory.** In v1 the fighting state had **no** time-based exit at all — it ended only in victory or
in the death of everybody. Any target that could not be killed held its company for ever, and taking part in a
squad outranks personal business. Twelve bots out of twenty were permanently "assisting": not idle but tied up.
The economy worked; there was nobody left to take part in it.

And the same rule applied to a single bot needed no code: a member that cannot reach the target loses the fighting
errand by itself — the errand queue drops it after a hundred fruitless attempts at a step, and what is underneath
it is a station.

## Leaving a fight it cannot win (03.10.2026)

The clock above judges a fight the company is in. Nothing judged a fight the company was *put* in: the company chose its
quarry at `BotThreat.Tolerance` (×1.5) of what it brought (`BotQuarry.Company`), and took on whatever attacked it with no
weighing at all (`Retarget`, `BotSquads.Note`, the prowl's company in `BotProwl`). On build 395 a company of nine took on a
wandering plague beast of 20262, broke off "its health has not moved" a minute later — which shuns the beast, so nothing in
the company looked at it again — and stood there while it killed seven of them.

Now every squad beat weighs the company's enemy and everything in its members' aggression lists, shunned ones included:
`BotSquad.Reckon` (the creature's strength at its kind's price in our lives — the company price since "A kind priced by
companies' own fights" below, the island's `BotQuarry.Priced` before — at the share of health it has left) against `BotSquad.Able` (the members' summed `Power`). Past `BotSquad.WithdrawOdds` (1.5) the company **withdraws**: the
creature is left alone for `BotSquad.WithdrawShunMs` (2 minutes) by companies no stronger and bots joining them (a stronger
company, a strong hunter and the harrow may still take it), every member is written down as having withdrawn from it, and the
company ends. Nobody is pressed: each member's enlistment ends "the company broke up" and the work put down beneath it is taken up
again, and a member the thing is hitting is offered the flight at once by its own rung (`BotFugitive`, via
`BotSquad.Withdrawing`) — for a guarded town when `BotHaven` judges it realistic, away otherwise — so no enlistment is thrown
away. The prowl that raised the company ends too, honestly failed. The same test keeps the company's finder, a blow on a
member and the enlistment from taking such a fight on. **Never from a caster** (Patrick, 30.09.2026: fight a mage to the
end): a company set upon by one fights it as before, and only does not choose one it cannot beat. Never for a war company, a
company underground or a delve's.

A sweep (`BotPatrol`, `BotSweep`) is no longer offered closed ground (a bar or a quarantine: that is the harrow's and the
clearing's) nor a square asking more (`BotPatrol.Asks`: the record or the fight there now, whichever is more) than the captain
and the strongest it could call bring; the call takes the strongest first and is not made short; a square closed while the
company is out for it is left.

Read in `Companies:` (`withdrawals: …`), `The captain:` (squares passed over; sweeps not raised or left), the enlistment's
clause, `The island:` (prowls given up for closed ground; deaths to a wanderer), and every `Squad N of M is dealing with`
line, which now prints the reckoning against what the company brings.

## Fighting on the strength that is on the field (06.10.2026, `BotSquadField.cs`)

Measured over 429 company deaths on builds 405–409: 89 % fell in fights priced under the company's strength over 1.5, 75 % to a
killer priced under the dead bot alone; the dead had no other member within 12 tiles in 36 % of cases and stood more than 40 tiles
from its leader in 43 %; 35 % of deaths followed another in the same company inside a minute, and only 20 companies withdrew against
233 that lost somebody. `Able` summed every living member on the map. Four rules, each a dial, all counted in `Companies:` ("the
field: …"):

- **The choice gate is stricter than the withdrawal gate.** `Beyond` refuses a creature past `BotSquad.ChooseOdds` (1.0) times what
  the company brings at every choice — the finder (`Hunt`, and `BotQuarry.Company` asked at the same odds so the next creature down is
  offered), a band's call and the muster's offer (`BeyondFor`), a cry's company, a prowl's find. Defence stays at `WithdrawOdds`
  (1.5): a blow on a member of a company with no enemy, a creature seen coming, the protection of a dire member, the enlistment into
  a fight already on (each passes `odds` to `Beyond`). Counted: `ChoiceRefused`, `BotQuarry.CompanyChoiceOut`.
- **A chosen fight musters short of the enemy.** `Engage(focus, contact, chosen: true)` with less than `MusterShare` (75 %) of the
  company's strength within `FieldReach` (16) of the enemy forms the company up at a rally `MusterOff` (14) tiles short of it: the
  anchor is the rally, the formation is the march line facing the enemy (`BotFormation`), the leader is stationed with everybody,
  nobody is pressed (`Tend` runs instead of `Press`), far members walk to the rally (`BotEnlist.Holding`). It ends when the share has
  come up, at a blow on a member (`Struck`, from `BotSquads.Note`/`Coming`), or on the `MusterMs` (30 s) clock — then with what is
  on the field if it is enough at `ChooseOdds`, else the creature is let go and shunned (`MusterShort`). The "dealing with" line is
  printed at the real engagement. A band's own gather judges the field the same way at its clock (`BotBand.GatheredShort`).
- **A company that bleeds pulls back.** At the `BleedDeaths`th (2) death inside `BleedMs` (60 s) — casters included — the company
  lets its enemy go, shuns it and the killer for `WithdrawShunMs`, offers the members within the thing's reach the run
  (`Withdrawing`), and holds on its leader for `BleedRestMs` (45 s): no finder, not scattered, not dissolved, not released. The
  company does not end; a band whose quarry this was fails "pulled back" (`BotBand.PulledBack`); a delve's party comes up
  (`BotDelve.BledOut`). `Engage` refuses the creature for the shun (`BledRefused`).
- **What is killing a member is fought by whoever can reach it.** In `Press`, a member within `PressReach` of an attacker whose
  combatant is one of ours under `DyingShare` turns on that attacker, whatever its price (`Peel`, `Peeled`). `BotSquads.Protects`
  also turns the whole company at a blow on a dire member when the company's enemy is attacking nobody of ours
  (`ProtectedIdle`). Among the things attacking the company, the strongest caster inside the `TieShare` tie is the enemy
  (`CastersFirst`, `CasterTaken`).

`FieldOvermatched` is an instrument only: creatures stood to at ×1.5 on the map's sum that the strength within `FieldReach` of
them would have withdrawn from. The withdrawal gate itself is unchanged.

## Clearing a quarantined death square (03.10.2026)

Patrick's order of 02.10.2026, item 4: companies gather, go to the squares where others died, clear them, and protect bots on
the road. A quarantine (`BotBarred`: three of ours dead within 45 tiles in 30 minutes, or one to the plague or a killer of
30000) closes a 91-tile box to every chooser for two hours; sweeps and prowls keep out of it. The clearing (`BotClearer`
offering `BotHarrow` as a clearing) is the company that goes in. On builds 395 and 396 it never marched, for four measured
reasons, each now changed:

- **The first square or nothing.** The bots within call were weighed only against the first square chosen; now the strongest
  company the bots within call make (`BotHarrow.SupplyStrength`, at most `BotSquad.MaxSize`) is weighed first and a square
  asking more is passed over for the next.
- **What the square asks** (`BotClearer.Asks`): the record, the fight standing there now, the strongest thing alive in the
  box at its kind's price in our lives (`BotClearer.Strongest`), and for a quarantine the strongest thing recorded killing
  in it, priced the same (`BotBarred.Closing`; the quarantine now keeps its killers, across restarts). The price is the one a
  company withdraws by (`BotSquad.Reckon`), so a clearing at `BotHarrow.ClearFit` times this does not withdraw from what it
  was sized for. Every standing quarantine within reach is a candidate, first in the order, walked from its own middle over
  its whole box.
- **What the offer brings.** It told the auction the leader's strength times six; now the company it will not march without.
- **Heads against strength.** The muster reckoned heads once from the leader's strength while the march waited on strength;
  every muster taken on 396 came up short with every member standing in it. The heads are re-reckoned every beat from the
  company as it stands, and the levy calls the strongest first.

Never sent: a standing bar (Patrick's), a quarantine the plague or a killer of 30000 closed, and a square asking past what the
island's strongest company of 20 brings (`BotClearer.Island`) — said once each in the log, "Quarantine (…) stays closed to the
clearings: …". A quarantine is lifted (`BotBarred.Lift`) only when its middle lies on the ground the company walked, nothing
alive in its box is reckoned past ×1.5 a lone bot (`BotBarred.EmptyOdds`), and the plague did not declare it — for the
Baron's and the guilds' harrows too. The harrow's finder no longer offers the company what it would withdraw from.

Read in `The island:` (the clearings' clause: offers, squares passed over by reason, marches into a quarantine, emptied,
won, withdrew, lifted, and the census of the quarantines standing by who could clear them), `The Baron:` (the same marches),
and `Getting about:` (the barred clause: quarantines lifted, left standing after a company's work, and why).

## A kind priced by companies' own fights (03.10.2026)

Patrick, 03.10.2026: "if they kill it, let them gather — what matters is that they level up, and the higher the level, the
higher the chance to kill later." The island's price of a kind (`BotQuarry.Price`: our deaths to it against its kills, every
death counted, mostly lone and small companies) made a Reaper ×4.8 to every company, so a company of thirteen at twice a
Reaper's strength reckoned it at 47,000 and turned away, while companies of ten and more had put Reapers down 20 times in 23
on build 396. The lone bot keeps the island's price; a company now weighs a kind at its **company price**
(`BotCompanyRecord.Price`), learned from companies' own fights:

- **What is recorded.** A kill (`BotSquad.Judge`, "it is down") at the odds the company took the creature on at — its strength
  (`Able`) over the creature's (`BotThreat.Power`, which grows with the weapon skill and the health a bot has: the level is in
  the scale); a member's death to a kind the company was fighting, or had fought within `BotCompanyRecord.FightMs` (two
  minutes: the field's next Reaper), at the odds it took that kind on at (`BotSquad.Fell`, from `BotKept.Fell`). Companies of
  two or more. A death in a company to a kind it was not fighting is the island's alone: on 396, 56 of the 85 enlisted deaths
  to Reapers fell beside a Reaper the company had not taken on, and that is the walk's business, not the price of taking it on.
- **The price.** Never above the island's. A company at odds `r` is let in when at least `BotCompanyRecord.Vouch` (3) kills,
  as the hour weighs them, were made by companies no stronger, and the deaths in the fights of companies up to
  `BotCompanyRecord.Margin` (×1.5) stronger, put through the island's own formula (`BotQuarry.Formula`) with those kills, come
  to a price its odds carry at `BotSquad.WithdrawOdds`. The lowest odds let in, times that yardstick, is the kind's company
  price; a new kind, or one no company has put down three times, keeps the island's.
- **Who asks it.** `BotSquad.Reckon` (so the withdrawal, the finder, a blow on a member, the enlistment, the band, the harrow's
  finder and the prowl), `BotQuarry.Company` (whether a company may be called at all; "one bot's work" is still asked at the
  island's price), a hunter's look for company ground (`BotHunter`, a zone kept for a company), and the clearing's ask
  (`BotClearer.Asks`, `BotBarred.Closure`). The lift of a quarantine and the harrow's win still ask whether the ground is safe
  for a lone bot, at the island's price.

Read in `Companies:` after the withdrawals: "the company record …" — company kills and deaths in companies' fights recorded,
deaths left to the island, the kinds priced below the island for companies (company price against the island's, the odds let
in, kills and deaths weighed), creatures a company chose, joined or answered and withdrawals not made on the record that the
island's price would have refused (by kind), and the looks for a company's quarry and for company ground let in on it. The
dials: `BotCompanyRecord.Prices` (off: every company weighs every kind at the island's price, as build 400), `Vouch`,
`Margin`, `FightMs`.

## The size cap lives in one place

`BotSquads.Join` is the only entrance, and the check is there. In v1 the cap was checked only on the recruiting
path, while a bot that had stumbled onto the same target and called for help was added **without a check**:
companies with a declared maximum of five held twelve.

And a squad is not created by somebody who cannot fight: in v1 the call stood above "my health is going", so a bot
on its last few points announced a company it could not join, it found nobody able and disbanded in the same tick
— and the same bot posted it again.

## What the bot provides

`IBotSquadMember` — four members: `Self`, `Class`, `Journey`, `Squad`, plus `AbleToFight` from `IBotAlly`. And two
calls:

| Where | What |
|---|---|
| `OnDamage` | `BotSquads.Note(this, from)` — this is what makes the collective mind work |
| `StepAsideFor` | ask `BotSquads.ShouldYield(this, asker)`, step in `YieldAwayFrom` |

Dividing is called by whoever opens the corpse: `BotSpoils.Share(squad, this, corpse)`.

## What to check at home

In the log at startup:

```
Squads ready: up to 5 to a squad beating every 1000ms; knots of 3 at 10 tiles while sweeping;
a fight is broken off after 90000ms without the target's health falling, and capped at 240000ms
```

| Number in the summary | Healthy | What it says |
|---|---|---|
| `squads standing` | steady | jumping means squads are falling apart and re-forming |
| `formed` / `disbanded` | close together | a gap means squads are leaking |
| `times one of them was set upon` | rising | the collective mind is working |
| `tiles given up` | non-zero | yielding in gaps works; zero means either no disputes or roles are not being read |
| `corpses divided` | equal to the number of wins | dividing is not running |

With a client:

1. **Formation.** Form a squad of a melee bot, an archer and a mage; lead it. The order along the direction of
   travel must be melee in front and mage behind. Turn it towards an enemy — the formation must turn with the
   axis.
2. **An archer leader.** Form a squad whose leader is an archer. The melee bots must stand **in front of it**.
3. **The gap.** Put a mage in a narrow gap, an enemy beyond it, a melee bot behind the mage. The mage must move
   back, not towards the enemy.
4. **Rescue.** Spread the squad out to scout (10 tiles) and hit one of them. Everybody must converge on it.
5. **An immortal target.** Give the squad something that cannot be killed. After ninety seconds it must withdraw
   — not stand there until the shard restarts, as in v1.
6. **Dividing.** Kill something with gold and a couple of items with three bots nearby. The gold must split three
   ways rather than going to one.

## What is not here

- **Who forms a squad, and when.** `Form` exists and nobody calls it. In v2's terms that is a proposer on the
  `Free` rung that nobody has written, and joining will be an obligation with a price (a share of the takings)
  rather than a muster by order.
- **Agreement to fight.** `OurPower` sums everybody able nearby, but none of them is obliged to join. In v1
  agreement arose from everybody computing the same number and seeing the same thing; the same holds here, but it
  can only be verified live.
- **Prices for items.** `BotSpoils.Worth` is unfilled. It no longer has to be — `BotAuction.Worth` exists.
- **The leader stands on the anchor while scouting** and does not take a patch of its own. Its patch is computed
  and unused — stable and slightly wasteful; if it matters, the leader can be given the centre.

## A hunter at its fight is left to it (07.10.2026, work18)

`BotSquads.Pressable` is false for a hunt whose quarry is within reach and standing (`BotSlay.Engaged`); `BotDelve` and `BotHarrow`
pass such a hunter by as they pass a carrier. Dial `BotSquads.SpareFighters`; counter `FightersSpared` in the squads line.
