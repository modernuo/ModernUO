# Traders: buying where a thing is, carrying it to whoever asked

Patrick's order of 30.09.2026, point 5: *"traders, a new faction: they like buying in other towns, trading at the market,
offering to resell goods and services; the result of their work must be healthy demand for products and supplying it in
time; once well off they may sail to other towns to buy materials for other bots."* His answer that evening: **neutral in
wars** — not attacked in guild wars and not taking part in them. Plan v2 of 01.10.2026, point 3: *99 % for traders; they feel
the market and follow the needs; write the orders into a book and strike them out when delivered.* Built 01.10.2026 (build 385).

| File | What is in it |
|---|---|
| `../BotClasses/BotTrader.cs` | the class: producer by role, `Neutral`, a rolled blade, Tactics, Arms Lore (its trade), Healing |
| `BotTrading.cs` | the dials, the counters, what neutrality closes (`Neutral`, `Refuses`, `Waive`), the porter's keep hook, the shopkeepers that sell a kind, the buyer's own alternative and the trader's quote, the `Traders:` line |
| `BotHauler.cs` | the proposer `Trader`: which spoken want, from which source, at what price, with up to two more buyers on the same purchase |
| `BotHaul.cs` | the deed `haul`: the book lines, the source (stock, box, a bot's shop, a shopkeeper, or a gathering at its town's bank), the road by land, gate or sea, the meetings, the endings |
| `BotOrderBook.cs` | the book item (`BotOrderBook`, a blue book) and what is written in it (`BotOrders`, `BotTradeOrder`) |
| `BotTradersModule.cs` | module `Traders`, phase `World`, requires `Will`, `Market`, `Shops`, `Shopkeep`; `Configuration/bot-traders.json` |

## Neutral in wars

| door | how it is shut |
|---|---|
| guilds | `BotGuilds.Barred` holds `Trader`, and `Outside` reads the class's `Neutral` flag: a trader is in no guild |
| everything that picks an enemy or an ally by guild | regard, `BotRegard.Hostile`/`AtWar`, the feud and its rallies and quarrels (`BotFeuder` needs the bot's guild and the other's), a war company's attackers (`BotSquad.Consider` asks `AtWar`), war kills and plunder (`BotWar`), envoys and meetings — none can name a bot of no guild |
| companies | `BotSquads.Form` and `Join` — the one door in — refuse a neutral bot (`BotTrading.Refuses`, counted): no levy, harrow, delve, press, muster or war company carries a trader |
| tolls | `BotToll.Rate` is nought for a trader, `Exempted` counts the waiver, no warden is sent to one |
| robbery and the underworld | the producer role: `BotRob`, `BotSkulk`, `BotUnderworld` pass producers over, so a trader never turns bandit and never joins The Shadow |

Self-defence is a reflex and stays one. A bandit may still rob a trader on the road: that is a crime, not a war.

## The haul

1. **Demand only.** The proposer reads `BotMarket.Wants`: open, at least `FreshMs` old (the carriers within reach go first),
   not a trader's own, not one the trader wants itself, the buyer alive, on the same map, not on deadly ground, not past a way
   through, not down the dungeon block where a walk or a haul from outside lately found no way, and — for a trader or a counter
   standing in the block — not outside its own land (see "The dungeon block" below). No open want, no haul — a trader never
   stocks a shelf on spec, so it cannot flood one.
2. **Source**, best profit a minute: its own pack (last haul's leftovers), its box when it stands at a bank, a bot's shop
   within reach (`BotShopkeep.Best`), a shopkeeper in any town (cheapest goods-and-road at the shard's price of a minute,
   then vetted by `BotShops.Nearest`), or — for a buyer **no carrier from here reaches** (past `BotCourier.Reach`, or another
   land) — a **gathering**: the trader says a want of its own at its town's bank at the buyer's offer less its least markup,
   and the island's carriers bring what they hold (a hunter's ribs and hides, a maker's spare piece) hand to hand, at a better
   price than a shopkeeper pays them. Up to `GatherMs` (longer while a carrier is on its way), topped up at a counter when one
   sells the rest under the orders' prices.
3. **Price.** The buyer's own offer when it pays `LeastMarkup` over cost. Under that, carriage: the trader's least price plus
   `CarriageShare` (half) of the way up to the buyer's own alternative — its walk to its nearest counter and back at
   `BotShopkeep.WalkGold` a minute — never above `MostMarkup` over cost, never under its offer. The buyer agrees aloud. Units are
   what the buyer can pay for (median purse 70gp on 384, so the split is half, not all): since 03.10.2026 (build next)
   `BotMarket.Affords` — the units' own reservation pays the offer and only free money the carriage over it, so a price over
   the offer for part of a want never spends what the rest of it holds — and the carriage agreed in the book is held with the
   want (`BotWant.Premium`) from the moment it is written until the meeting, so nothing else the buyer does spends it and a
   second trader does not count the same free coin. A want read back from the save is not bought for until its material's
   ceiling is read (`CeilingUnread`, raw meat only): on live 395 every rib order written in the first twenty minutes was for one,
   at 17–25gp against an offer of 6.
4. **Batch.** Up to `MostOrders` buyers of the same kind within `BatchTiles` of the first, each at its own price.
5. **Road.** Walked where a road or gate joins the places; sailed (`BotVoyage`, run inside the haul) where only the sea does,
   by a trader worth `SailWealth` or owning a ship; otherwise not offered. Far buyers are walked to; near ones (`NearTiles`)
   followed; `ChaseMs` after one that keeps walking away the line is struck.
6. **Meeting.** `BotHandover.Exchange` at the agreed price; `BotMarket.Received`; the reservation released; the line struck as
   delivered. Leftovers go to another open want for the same kind (`Adopt`), else stay as stock: the porter banks them and the
   trader's own shift at Britain's bank (`BotShopkeeper.Merchant`, the producer role) sells them.

The haul is steadfast and `Committed` from the first line written: the book is the trader's word, so no better price takes it
off the road. Its own clocks end it: `MostHaulMs` for the whole, `ChaseMs` after one buyer. It is finished when at least one
order was handed over, failed when none was. Its takings are its margin: purchases at a gathering are put back into them
(`BotYield.Aside` undone), and leftovers are counted as made at cost.

Traders live in Britain (`BotRelocate.StayHome`): the market is there, and so is their shop. A trader that has just delivered
in Yew looks from Yew — and a Britain cook's want is then one no carrier from Yew reaches, so it gathers in Yew and carries
home. That is the round trip Patrick asked for: buying in other towns.

## The book

A `BotOrderBook` (a blue book, not writable by anybody else, not movable, blessed) in the trader's pack. Page one: written,
delivered, struck, open, margin, the next number. One page for each open order: number, buyer, how many of what, the price
(and the buyer's own offer when carriage was agreed above it), when and where it was taken, where it is being bought. Then
the closed orders, newest first, `BookHistory` of them: `X #12 Odile` / ` 20 leather` / ` done 180gp (+60)`, or
` struck: the buyer died`. Read back from its own pages the first time the trader writes after a boot, so the counts and the
history survive a restart, and whatever was open is struck "the island slept". A GM opens the trader's pack and reads it.

## Counters

`Traders:` every five minutes: the traders and what they are doing; hauls taken, finished, failed, ended from outside, died,
and the finished share; the looks and every reason a want was passed over; bought by source; gathered; sold, margin and the
carriage agreed above offers; adopted and left over; sailings; the book's lines written, delivered, struck (by reason) and
open; the company doors shut and the tolls waived. The will's own tally of the `haul` kind is the authoritative finished share.

## The balance (Fable's review, 01.10.2026)

- A trader never buys from a **shopkeeper's counter into a glut**: `BotHauler.Weigh` and the gathering's top-up pass the counter
  over while `BotGlut.Glutted(kind)` (counted `Glutted` in the `Traders:` line). A bot's shop, the trader's own stock and a
  gathering drain the pile and are still weighed.
- A trader's **gathering want is not demand** to the balance: `BotGlut` leaves the units a neutral class asks for out of its
  reading (`Balance:` prints them), so a five-minute gathering never turns a glutted kind back to ×1 for the makers.
- **Leftovers** are counted as made at cost or at the balance's price of the kind, whichever is lower (`BotHaul.Keep` →
  `BotGlut.Yield`), so a haul that buys eighty and hands over twenty does not read as well as one that hands over eighty.
- A want an order was struck on **for the buyer's own doing** — walked away, unreachable, refused, across water — is left alone
  by that trader for `ShunMs` (ten minutes; `BotHauler.Shun`, counted `Shunned`): the haul's strikes marked nothing before, so
  the next look re-took the same want and chased the same buyer for another `ChaseMs`.

## The dungeon block (03.10.2026, build next)

- *Measured (live 396, 04:19–05:40, `agents/haul11-tools/orders.py`):* 35 of 224 orders were struck "no way through to the
  buyer" and 18 of the 21 failed hauls failed on it; 33 of the 35 touched the dungeon block (x ≥ 5120: the dungeons, the Lost
  Lands, and a pocket at (6139, 1432) a gate leads walkers into and no walk leads out of). Orders touching the block were handed
  over 4 times in 38 (11 %), on the island 149 in 186 (80 %). Seven traders bought and walked for Edith in that pocket in 26
  minutes; the reach map calls it open, the gates join it, and each trader's ledger and shun were its own.
- *Decision (`BotMarket/BotUnreached.cs`):* the haul's `Bend` marks a buyer standing in the block, and its ground, when the
  haul was taken outside the block (not a walker that never moved, nor one already in the buyer's own dungeon, square or land);
  every trader (`Weigh`, `Batch`, `Adopt`) and carrier outside the buyer's land passes such a want over for 30 minutes, doubling
  to two hours, until the buyer comes up or a hand-over there from outside clears it. A trader standing in
  the block, or a counter there, is offered nothing outside its own land of the walkers' graph (`Shut`). The want stands; the
  pass is "no way through to the buyer".
- *Counters:* `Traders:` "wants passed over: … N the buyer down the dungeon block where a walk or a haul from outside lately
  found no way, M wants or counters out of the dungeon block or across its lands from a trader or a counter standing in it";
  `The market:` "the dungeon block: …" (marked, resting now, refused, cleared, come up). Log: "No way was found to X at (…) in
  …: carriers and traders leave the want for N minutes, or until it comes up or something is handed over there".

## Traps

- A gathering's want is a real want on the board: makers within reach may start a piece for it. The trader withdraws when it
  leaves; a piece finished after that is the maker's stock.
- `BotShopper` keeps a far-counter want standing while a carrier is bringing it (`Coming > 0`); the haul's own clocks and the
  market's sweep (`IBotWantCarrier`) free it if the haul ends.
- A sea leg runs the ordinary `BotVoyage` inside the haul, so a ship is bought the first time (10,177gp) — which is why
  `SailWealth` is twelve thousand.
