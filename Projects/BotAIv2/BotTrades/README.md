# BotTrades — establishments of the professions

Patrick, 07.10.2026 00:10, approved: *a tavern/inn kept by a bot (the cook sells food, travellers sit and eat and rest THERE
rather than in a field; rest with food restores faster); … a healer with an infirmary (resurrection and healing for a fee by its
guild); a moneylender (loans to the poor against future spoils: an honest way out of poverty, not a handout).*

The shops by the bank (`BotShopkeep`) were the first establishment. These are the next three. Each is a bot's own shift — a
deed in the auction, held like a shop's — its customers sent by their own proposers, its prices the market's, its coin hand
to hand, its numbers in the five-minute `Bot shops:` line after the shops' own (`establishments: …`). Nothing here mints coin.

## Layout

| file | what it is |
|---|---|
| `BotTrades.cs` | The switches (`BotTrades.Running`, `Taverns`, `Lenders`) and the `Bot shops:` clause. |
| `BotTavern.cs` | `BotTavern` (one open tavern) and `BotTaverns`: the registry, the meal and its price, serving, the table (seats, regeneration, talk), and the hooks for the rest's clock, the inns and the lodging errand. |
| `BotKeepTavern.cs` | The cook's shift (`BotKeepTavern`, kind `tavern`) and who is offered it (`BotTaverner`). |
| `BotSupper.cs` | Supper at a tavern (`BotSup`, kind `supper`) and who is asked (`BotSupperer`). |
| `BotInfirmary.cs` | `BotInfirmary`, `BotInfirmaries` (registry, fees, the claim on a fallen guildmate and the hold on its revival), the healer's shift (`BotKeepInfirmary`, kind `infirmary`) and its proposer (`BotInfirmarer`), the patient's visit (`BotTend`, kind `infirmary visit`) and its proposer (`BotPatienter`). |
| `BotLoans.cs` | The moneylenders' book: loans, the lien, defaults, the marked; and `BotLoanStore`, which saves it. |
| `BotBorrow.cs` | The borrower's walk (`BotBorrow`, kind `borrow`), its proposer (`BotBorrower`), and the lenders' call in the trade room. |
| `BotLibrary.cs` | `BotLibrary`, `BotLibraries` (registry, prices, what the casters lack), the scribe's shift (`BotKeepLibrary`, kind `library`) and its proposer (`BotLibrarian`), the caster's visit (`BotReadAt`, kind `library visit`) and its proposer (`BotReader`). life18b. |
| `BotRagman.cs` | `BotRagPost`, `BotRagmen` (the mouths and their hunters, the safe post, buying, the stock to haul), the trader's shift (`BotKeepRagPost`, kind `ragpost`) and its proposer (`BotRagman`), the hunter's sale (`BotRagSale`, kind `ragman sale`) and its proposer (`BotRagSeller`), and the haul to a counter (`BotRagHauler`, a `BotPeddle`). life18b. |
| `BotTradesModule.cs` | `bot-trades.json`, the phrases, the three proposers' registration, one clock (table talk every second, the infirmaries' look for the fallen every 3 s, the lien every minute, the lenders' call every 30 s), the `Trades:` boot line. |

## The tavern

1. A bot with a skillet (`BotOven.Kit`, the cook's own test) and `LeastMeals` (4) cooked meals in its pack and bank box, not
   resting from a shift (20 min), is offered a tavern at the nearest inn (`BotInns`, 79 on Felucca) within `KeeperReach` (150)
   with no tavern within `Spacing` (40), at most `MostOpen` (6) on the island. Claim: meals × price × measured sell-through
   (a half until five shifts) / `ShiftMinutes` (20), corrected by the ledger like any trade.
2. It takes up to `StockMeals` (15) out of its bank box at the bank on the way, walks to the inn, opens the table where it
   stands (trade room: "Supper's on at …"), and keeps it until the meals or the twenty minutes run out. Put down for what will
   not wait, the table is shut and opened again on return.
3. A free bot within `GuestReach` (100) that is tiring (two fifths through its session), bored, hurt (under 80 %) or has nothing
   cooked on it, not fed in the last ten minutes, and keeps `GuestKeeps` (100gp) after the plate (a poor bot hunts), is asked to
   supper at `Prior` (26) + the fireside's tiredness and boredom + `HurtWorth` (30) × hits missing + `HungryWorth` (6).
4. At the cook's side the guest buys one meal hand to hand (`BotHandover.Exchange`, the market's price `BotMarket.Worth` ×
   `Markup` 1.0) and eats it at once (`BotMeal`: twice the regeneration for ten minutes), then sits `GuestMinutes` (5 ± 1):
   its play counted backwards at `RestShare` (1.5) of the inn's rate — a campfire's is 1 — and `RegenPoints` (2) of hits,
   stamina and mana every 4 s on top. Two or more at the table talk the campfire's talk (`BotCampTalk.Topic`).
5. A tired bot is sent to an inn with an open tavern when it is no more than `Detour` (60) tiles further than the nearest inn
   (`BotTaverns.InnFor`, hook in `BotInns.ToInn`); arriving, it walks to the cook (a minute at most), buys supper, then pays for
   the bed (hook in `BotLodge.Advance`). The rest record keeps "the inn of X after supper" (`BotRest.Leave`), and the return
   gives `SupperBuff` (1.5) × the rested regeneration (`BotRest.Return`).

## The infirmary

1. A bot of class Healer in a guild (not The Shadow), with `LeastBandages` (10) or the herbs for a heal, within `KeeperReach`
   (150) of its guild's seat (`BotSeat.Of`: the guild's house in a town first), and none open for its guild, is offered the shift
   (`Prior` 20/min, the ledger corrects it). It keeps the infirmary `ShiftMinutes` (20) where it stands at the seat.
2. A wounded bot of that guild or an ally (under seven tenths of its hits), free, with no bandage and no heal ready of its own,
   able to pay `FeeLeast` (5), within `PatientReach` (160), is sent (`Prior` 25 + 80 × hits missing). It queues; the healer binds
   (`BotMend.Wind`) or heals by spell (`BotMend.Begin`/`Aim`) one patient at a time, up to `HealTries` (6), and charges
   `FeePerHit` (0.5) a point healed, at least 5, coin hand to hand.
3. Every three seconds an open infirmary with nobody out looks down the population list (no spatial query) for a guildmate or
   ally fallen within `RaiseReach` (150) in the last `ClaimWithinMs` (30 s), not on a company's list (`BotRaise.Awaited`), not
   delving, not unholy, on ground that takes a body, and able to pay the fee (`RaiseFee` 60, or a scroll's price × 1.25). The healer goes out to it and raises it with
   the engine's means (`BotRaise.Best`: scroll, book, master's bandage — the company raiser's own steps); `BotRaise.Rise` strips
   the corpse at once; the fee is charged and booked aside for the raised. The ordinary revival is held for it at most
   `RaiseHoldMs` (3 min) after the death (`BotInfirmaries.Holding`, hook in `BotRaise.Holding`).

### Why nobody came on build 416, and what changed (07.10.2026, life18b)

Nine hours of build 416 (`session-2026-10-07_01-16.log`): 142 shifts opened, 0 patients, 0 claims. Three causes, measured:

- **The shift lasted 2.1 minutes.** 140 of 142 ended "the healer was called away" — 104 pressed into a company (`enlist`), 30 by a
  meeting, 6 by a duel — and a hall shut on every call. Now a hall waits `AwayMs` (3 min) for its healer (`Away`: no new patient
  is sent meanwhile, a patient already sent waits at the hall, a body gone out to is given back to the revival), and a healer at
  its open infirmary is left alone by a company's call (`SpareKeepers`: one hook in `BotSquads.Pressable`, one in
  `BotBand.LevyMedic`, which levies the next healer instead).
- **Few wounded qualified, and none was sent.** 719 looked at in nine hours: 514 could heal themselves, 166 had under 5gp, 39 had no
  infirmary within reach (mostly because none was open). `HurtUnder` is 0.7 now (was 0.6), `PatientReach` 160 (was 120); a patient
  walks to the hall, not after the healer; a bot in a fight, a company or a delve is counted (`Engaged`) instead of left out silently.
- **No healer could raise anybody, and the dead were far.** Of 31 healers none had 80 Healing and 80 Anatomy, five had Magery 52–67
  and none carried a resurrection scroll (`BotLifeline` asks 300gp in the pocket; healers hold ~100). Of 386 guild deaths, 2 fell
  within 60 tiles of their seat, 14 within 200, 300 over 400. A guild's Healer that reads a scroll now stocks one keeping only
  `ScrollKeep` (100gp) of all it may spend (`BotInfirmaries.Stocks`, hook in `BotLifeline`); the reach is `RaiseReach` 150 and the
  hold `RaiseHoldMs` 3 min; a raise off a scroll costs the scroll's market price × `RaiseMarkup` (1.25), never under `RaiseFee`,
  because a 60gp raise off an 87gp scroll was a loss for the healer. Every fresh death of a guild is now judged on every scan and
  its last reason counted once (`none open`, `far`, `no means`, `busy`, `away`, `company`, `delving`, `unholy`, `ground`, `poor`):
  the geography is the binding limit, and the clause says so in numbers.

Why a fee is honest here: a death costs some 186gp (67 of coin, 119 of kit on the corpse: an-money14 §3.2), most of which a raising
where the bot fell gives back; a bandaged wound is minutes of a bot that could not hunt below `BotHunter.FitAt`.

### The shift on demand (07.10.2026, life19)

Build 419 (2.3 h, `session-2026-10-07_12-27.log`), after life18b: 14 shifts opened, 13 shut, 0 patients, 0 raised. Of 699 wounded
looks 341 were in a fight, a company or a delve and 358 "could heal themselves" (one bandage in the pack, or a heal ready), so 0 were
sent; of 88 guild deaths 66 fell with no hall open and 18 over 150 tiles from one. Almost every bot carries bandages: the honest rule
makes the infirmary rare, and the clause now says how rare.

- **Who mends itself** (`BotPatienter.Mends`): a heal spell ready (Cure first when poisoned), a potion for what ails it, or bandages
  with `SelfBindSkill` (30) Healing — 60 Healing and 60 Anatomy for a poison. Everybody else wounded under `HurtUnder`, or poisoned,
  is a patient. The look is split: at a fight / in a company / delving; mends by spell / bandage / potion (and how many of those had
  a hall of theirs open within reach); cannot: no bandage and no heal / bandages under the skill / poisoned with no cure; then too
  soon, poor, none open with the seat near (a want) / none open and the seat far / open but farther, full, away; and every offer not
  taken, by what the bot held at its next look instead (`nothing` = refused at the auction, a road veto among them).
- **The shift on demand** (`BotInfirmaries.OnDemand`, on): a guild's healer is offered the shift only while a want of its guild
  stands (`DemandMs` 2 min): a wounded guildmate that cannot mend itself with none open and the seat within `PatientReach`, or a
  death within `RaiseReach` of the seat with a healer of the guild able to raise (the ordinary revival is then held `DemandHoldMs`
  90 s for the healer to open and claim it). The claim is the prior plus the want's fees over `DemandMinutes` (3); the hall shuts
  after `IdleMs` (2 min) with nobody waiting, sent or claimed, once its 3 minutes are kept; the healer rests `DemandRestMs` (2 min).
  Off, the standing 20-minute shift comes back. Counted: wants raised (wounded, fallen), deaths with none open by where (near the seat
  with a raiser / near with none / far), revivals held, claimed and lapsed, shifts opened on demand and shut idle, and the minutes
  kept open against the minutes kept by shifts that saw nobody.

**The want opens the shift (life19b).** On build 420 (70 min) no shift opened on demand: the want waited for a healer's own auction
(2 offers, 0 taken). Now a death within `RaiseReach` of its seat, or a wounded guildmate that cannot mend itself near it, with no hall
of the guild's open, presses the nearest able guild Healer within `KeeperReach` of the seat (one a company's call could press) into an
on-demand shift (`BotInfirmaries.Call`, one call a guild per `DemandMs`); for a death the hall opens at once at the seat and the body is
claimed at the next scan, the revival held `DemandHoldMs` meanwhile. Logged per call ("X's death at (x, y) calls Y to open the infirmary
at Z's seat (d tiles from it …)") and counted by outcome (pressed, found open, already called, healers busy by work, none able); the far
deaths are split by distance to the seat (300 / 600 / beyond). On 420 all 25 deaths with none open lay over 150 tiles from their seat.

**The trigger traced (life19c).** On 421 the clause's own split showed the trigger ran (1 call, "found none able"; 4 deaths far from
the seat) while the main count read only "no infirmary open". Now each guild death logs `Infirmary trigger 1/4, death seen`, `2/4,
seat: … d tiles … verdict` (or `2/4, no call: …` for a criminal, a delve, a company's raiser, bad ground), `3/4, healer: able healer
found: Y / no able healer found; G has n Healers: dead, on another map, far from the seat, in a company, tired, without the means,
busy`, `4/4, pressed: yes/no` (dial `BotInfirmaries.TraceTrigger`). The main clause says the trigger had its turn before "no
infirmary open" and splits it. `raisers:` in the clause and `Infirmaries at boot` say how many guilds have a Healer able to raise at
all (scroll, book, 80/80 bandage); none, and the line says raising is moot.

## The library (07.10.2026, life18b)

Patrick: *a scribe-librarian (sells scrolls by the guild stone)* — "we do it".

1. A guild's bot with Inscription `LeastInscribe` (50)+, within `KeeperReach` (150) of its guild's seat, holding `LeastScrolls` (3)
   spare scrolls (over its own keep list, pack and box) of kinds its guild's or allies' casters lack, none open for its guild and
   20 minutes since its last, is offered the shift. Claim: the lacked scrolls in hand × the market's price × `SellThrough` (½) /
   `ShiftMinutes` (20), clamped 4–120, corrected by the ledger. It takes up to `StockScrolls` (30) out of its box at the bank on
   the way, stands two to four tiles off the seat, says what it has in the trade room, and keeps it until the shift or the scrolls
   run out.
2. What the casters lack is the gap `BotSeeker` works on: `BotGrimoire.Missing`, counted guild by guild once a minute from the
   population list (`BotLibraries.Demand`).
3. A caster of that guild or an ally, short of a spell whose scroll an open library within `ReaderReach` (150) holds, able to pay
   (a want standing for it pays with its own reservation), is sent (`BotReader`, unpaid, `Prior` 16 + `WantWorth` 8 when a want is
   standing, once per 5 minutes). At the scribe's side it buys one hand to hand at the market's price × `Markup` (1.0), writes it into
   its book at once, and a want still standing for it is withdrawn and its money freed.

## The ragman (07.10.2026, life18b)

Patrick: *a ragman buying loot from hunters at the dungeon mouth* — "let's try, but if he dies often to the mobs … we take the role
out, or make him stand REALLY safely, though not too far from the entrance". `BotTrades.Ragmen` switches it off live.

1. A trader (`BotTrading.Trader`) with `LeastPurse` (400gp) to spend, not carrying the last post's goods, 30 minutes since its last,
   at most `MostOpen` (3) posts on the island, is offered a post at the busiest mouth within `KeeperReach` (600): mouths are the
   gates from the island into the dungeon block (`BotGates.All`), hunters counted round each within `TrafficReach` (200) and inside
   its dungeon, once a minute, `LeastTraffic` (2) at least.
2. **The post is safe ground**, found once a mouth and kept 10 minutes: rings `PostNear` (40) to `PostFar` (80) tiles from the mouth,
   sixteen bearings; a floor (`BotStep.Settle`), on the island, joined to the mouth, no blood on the square (`BotQuad.Muscle`), the
   zones' map clear (`BotZones.LevelAt`: no patrol and no fringe) for `SafeSlack` (16) tiles every way, and no creature that attacks
   on sight alive within its perception + 4 (the reach `BotSight` prices). None found: the clause counts it, no post.
3. At the post the ragman **leaves at once** when its sight holds a danger it cannot beat (`BotJourney.Sighted`); the post is not
   used again for `UnsafeMs` (30 min). Within a creature's own reach `BotSight`'s stand-down abandons the work anyway. Its deaths
   are counted apart: "ragmen killed at the post" and "on the way to it".
4. A hunter (`BotPeddler.Hunter`) outside every town, not in a fight, a company or a delve, within `BotRagSeller.Reach` (120) of an
   open post, whose loot fetches `LeastLoad` (40gp) at the counters and whose nearest counter for it is `FarGain` (60) tiles further
   than the post, is sent. Each kind is bought hand to hand at `Share` (0.6) of the nearest counter's price, as much as the ragman
   may spend keeping `KeepPurse` (150). The hunter is measured as coin; the post as coin out plus the goods at the counters' price.
5. The post shuts at `ShiftMinutes` (25), with the purse spent or the pack at `Heavy` (0.6) of its weight. The goods go to the counter
   that pays most for them once the road is paid for (`BotRagHauler` offers a `BotPeddle` a kind at a time); the counter is where
   the coin enters the world, as before.

## The moneylender

1. A desk is any open shop by the bank whose keeper holds `LenderWealth` (1200gp) spendable and keeps `LenderKeeps` (800) after
   the loan, with fewer than `PerLender` (5) loans standing. It says so once a shift in the trade room. Lending is the shop's
   side business: the keeper does not choose it and its ledger never sees it.
2. A bot under `PoorBar` (100gp) spendable, short of the kit its trade takes into a fight (`BotProvision.Short`) and unable to buy
   it (`BotProvision.Bill` over what it has), whose own record shows work that paid `LeastPays` (10/min) over 2 settled rounds
   (`BotLedger.Best`: no loan without a trade), not owing and never defaulted, is sent to the nearest desk within 200 tiles.
3. The loan: what the kit costs over what it has, between `Least` (30) and `Sum` (150), lender's pack and account to the
   borrower's pack, booked aside for both. `Interest` (0.2) on top. The lien looks every minute: `LienShare` (0.5) of every rise
   in the debtor's wealth goes to the lender's account until the debt is paid; it does not run while the debtor sleeps.
4. `DefaultHours` (4) of the debtor's own play with the debt unpaid, or the debtor gone from the island, is a default: the lender has
   lost what is left of the principal, the debtor is marked and lent nothing again. A lender gone from the island forgives.
5. A lender minds its risk (`MindsRisk`): no loan while the measured default rate makes its expected return nought or less (at a
   fifth on top, more than one in six). The rate is a tenth until five loans have settled.
6. The book is saved (`BotLoanStore`, `BotLoans`): every loan standing, the marked, and the settled counts for the rate.

## What to check

- Boot: `Trades: establishments are on. …`; `Proposer Taverner/Tavern guest/Borrower/Infirmarer/Patient offers work on the Free rung`; `Loans read back: …`.
- `X opened a tavern at the inn of Y (x, y) with N meals …`, `X handed 1 Ribs to Y for 8gp — supper at X's table at …`,
  `X shut its tavern at … after 20.0 minutes — the evening is over: 9 meals sold to 9 guests for 72gp …`.
- `X ate Ribs at …'s table at the inn of Y for 8gp before bed`; the leave line `… in the inn of Y, a bed paid for …`; the return
  line `rested well at the inn of Y after supper: regenerating until HH:mm`.
- `X lent Y the Warrior 120gp at 20 % against its next takings: 144gp owed …`; `Y repaid X in full: …`; `Y defaulted on X's loan — …`.
- `X opened G's infirmary at (x, y) …`; `X healed Y the Mage 34 points at …'s infirmary of G for 17gp`;
  `X leaves …'s infirmary of G to raise Y …`; `X raised Y the Warrior where it fell, with a resurrection scroll, from … for 60gp`.
- The five-minute `Bot shops:` line, after the shops' clause: `establishments: taverns …; lenders: …; infirmaries …` with every
  gate of every proposer by name.
- `X opened a library at G's hall (x, y) with N scrolls …`; `X sold Y the Mage a FlamestrikeScroll at …'s library at G's hall for 52gp`.
- `X opened a buying post at (x, y), N tiles from the mouth of Despise …`; `X shut …'s buying post … — left the post: a danger it
  cannot beat came within sight: …`; `X the ragman was killed at … — ragmen killed at the post: N`.
- `logs/bot-speech.log`: `tavern:*`, `loan:*`, `infirmary:*`, `library:*`, `ragman:*`, `took:tavern`, `took:supper`, `took:borrow`,
  `took:infirmary`, `took:library`, `took:ragpost`.

## Measures

- Coin per hour of each establishment: the difference of two `Bot shops:` lines — taverns' `meals sold for Ngp`, lenders'
  `the lien took Ngp (… interest)` and `losing Ngp of principal`, infirmaries' `healed … for Ngp` and `raised … for Ngp`.
- Rests at a table against rests by a fire: `N guests sat at a table after supper` here against `Camp:`'s sat-down count.
- Loans: repaid against defaulted, the default rate, and whether the borrowers' deaths per hour fall (`At death's door:`) and
  their wealth climbs over the poverty bar.
- Raisings at infirmaries against the ordinary revivals: `raised where they fell` here, `Companies:`' raised, and the deaths.

## Traps

- **Unpaid work asks every reason it fails before it is offered.** Supper, the borrower's walk and the patient's visit are paid
  nothing; their proposers check the tavern, the price, the lender's purse and the infirmary's queue first.
- **The meals are the cook's own.** `BotMeal.Keep` eats from the cook's pack too, so a keeper eats a plate of its stock every ten
  minutes when it is not fed.
- **A lender is a shop.** No shop open by a bank with a rich keeper, no loan — `NoLender` in the borrower's clause says so.
- **The lien charges through `BotMarket.Charge`**, which will not spend money held for the debtor's wants on the board: such a
  take is held back and tried again next minute (`held back for wants`).
