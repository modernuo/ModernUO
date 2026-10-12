# The market: hand to hand, in Britain

Patrick's order of 30.09.2026, night: "The auction is removed completely; all trade and all the workshops live in Britain, and
a guild's traders prefer to trade in their home town. Trade through talk and the global chat. Every order and every purchase must
now be hand to hand." The auction (`BotAuction/`: stalls, escrowed wants, fills, crossing, fetches,
haggling, the levy) is gone. Nothing changes hands between two bots that are not standing within `BotHandover.MeetTiles`
(two) of each other.

| File | What is in it |
|---|---|
| `BotMarket.cs` | Britain's square (`Square`), the spoken wants (`Ask`, `Wanted`, `Demand`, `Best`, `Received`, `Withdrawn`), the prices learned from goods that changed hands (`Worth`, `Traded`, `Floor`, `Staple`), the one way to take a bot's money (`Charge`, `Refund`, `Pay`), the own-bank-box withdrawal at a bank (`FromBank`), the wants' beat, `The market:` and `The board:` |
| `BotWant.cs` | one spoken want: what, how many, at what; units carriers are bringing (`Coming`), what is still open to a carrier (`Payable`) |
| `BotHandover.cs` | the meeting: goods pack to pack, coin pack to pack, both within two tiles; the auction's order of charge, move, refund, pay |
| `BotCourier.cs` | the proposer: a bot holding what somebody said it wants (pack, or bank box when it stands at a bank) is offered the walk; since 06.10.2026 the best walk whose straight road its auction will not refuse (`BotPass.Road`), the last units of a part-handed want under `LeastGold` (`TailExempt`), and a buyer past `Reach` nobody near has answered for `FarQuietMs`, out to `FarReach`, for kinds no counter sells, the buyer paying carriage within its ceiling (`FarOpen`, `FarPrice`) |
| `BotDeliver.cs` | the walk to the buyer (followed, not aimed at) and the handing over; eight minutes of walking, or twice the walk reckoned when longer, and it gives up; put down three minutes in all, its units are opened to other carriers (`ReleaseDownMs`); held against a better offer for the road still ahead by the planner's route (`HoldsFor`, `HoldStretch`), and a meeting place it finds no way to sends it to the buyer itself (`Bend`) |
| `BotRoom.cs` | a buyer whose pack refused a hand-over: marked for up to `MarkMs`, passed over by carriers (`BotPass.Room`) until its pack shows `Places` free places; its peddler and porter are pressed to make room (06.10.2026); since work17 also read before any walk: `Receives` (a free place or a stack it joins) gates wants (`BotMarket.Ask`), restocks, hauls, carrier looks and meetings, `SpecShut` keeps `SpecPlaces` free before a piece is made on spec, `Packed` lifts a pack's shop hold at `PackedShare` |
| `BotWayless.cs` | places a carrier's walk found no way to, shared by every carrier: a cell of `Grain` tiles answering "no way" `Strikes` times is struck for `RestMs`; meeting places and havens (`BotHaven.Edge`, `BotRendezvous`) pass it by, carriers pass over a buyer standing in one; a hand-over there clears it (06.10.2026) |
| `BotUnreached.cs` | the dungeon block (x ≥ 5120): buyers down there, and their ground, that a walk or a haul from outside found no way to, remembered for every carrier and trader (`Refuses`, `NoWay`, `Reached`); and a carrier or trader standing in the block walks only inside its own land (`Shut`) |
| `BotCity.cs`, `BotCityStore.cs` | the treasury (moved here from `BotAuction/`, store unchanged): its buys, fairs and standing orders are served at the bots' shops by the bank (`BotShopkeep.CityBuy`) |
| `BotGlut.cs` | the balance (01.10.2026, Patrick's point 8): supply against spoken demand by kind — what the shops show and what was made, sold and peddled this half hour against what is asked for; `Factor` (a glutted kind is a low price, never a veto), `Yield` (what a maker counts its output at: the island's price for what is asked for, the surplus price — a shopkeeper's learned price or the factor — for the rest), `Claim` (a maker's claim on spec marked down by the glut), `Glutted` (a chooser passes a glutted kind over for one the island is not full of); the `Balance:` line |
| `BotMarketConfig.cs` | `Configuration/bot-market.json` — speeds and distances, never prices; the balance's `GlutEnabled`, `GlutWindowMs`, `GlutFloor`, `GlutTolerance`, `GlutShunned` |
| `BotMarketModule.cs` | module `Market`, phase `World`, requires `Will`; offers `Courier` |

---

## Where trade happens now

**A sale is one of three meetings, and every one of them is two bots or a bot and a shopkeeper standing together.**

| road | who walks | where | code |
|---|---|---|---|
| a bot's shop | the buyer walks to the keeper | Britain's bank; a guild member's by its guild's own town's bank | `BotShopkeep`, `BotKeepShop`, `BotRestock(front)`, `BotPatron` |
| a delivery | the carrier walks to the buyer | wherever the buyer is, within `BotCourier.Reach` and on its land; out to `BotCourier.FarReach` for a no-shelf want nobody near answered | `BotCourier`, `BotDeliver`, `BotHandover` |
| a shopkeeper | the bot walks to the counter | the world's shops, which stay | `BotShopper`, `BotPeddler`, `BotPeddle`, `BotCapital` |

**Only makers and gatherers keep shop** (`BotShopkeeper.Merchant`, `BotRole.Producer`): 88 of the 103 shift-hours of 30.09.2026
were fighters standing at a bank with what came off corpses, and 53 % of shifts sold nothing. A fighter's surplus goes to its
bank box, to a carrier's walk when somebody wants it, and to a shopkeeper when nobody does.

## A want is a sentence with a price in it

`BotMarket.Ask` takes no gold. The buyer must be able to pay for what it asks at its offer — pack and bank together — or the want
is not said; the beat stands a want down the moment its buyer could no longer pay for one. It is said in the trade chat
(`market:want`, see `BotVoice/README.md`), again every `SayWantMs` while open, and louder (`market:raise`) when nobody has answered it for `RaiseMs`, up
to ×`MostMultiple` of its opening offer and of the cheapest shopkeeper's price (`BotWant.Highest`), if the buyer could pay the louder
price; at the ceiling it gives up. It opens at what the thing is worth (`Worth`: what it sold for between bots, else the buyer's own
estimate), never at another buyer's standing offer — see "What a want opens at" below. A carrier that takes
the delivery reserves the units it carries (`BotWant.Reserve`), so a second carrier does not set out for the same five ribs.

A want is answered by either road: a carrier brings it (`BotDeliver`), or the buyer walks to a shop that shows it for no more than
its offer (`BotPatron`). Either way the want comes down by what was handed over (`Received`); answered again inside `BriskMs`, it
asks for less.

## Money

`BotMarket.Charge` is the auction's own: coin out of the pack first (`ConsumeTotal`), the rest from the account, all or nothing.
At a meeting the buyer is charged first, the goods move, what did not move is refunded (coin back to the pack as far as coin came
out of it), and only then is the seller paid — in coin into its pack (`BotMarket.Pay`), the account when the pack will not take
it. Charged equals paid plus refunded. No levy.

**The buyer's purchase at a carrier's meeting is booked aside** (`BotYield.Aside`): the buyer pays in the middle of its own work,
and that work must not learn from the coin that it loses money. A want holds no gold, so `BotYield.Standing` is plain wealth now.

## What stays where it was made

Nothing is put out from a bench, a seam, a corpse or a field any more. A trade's ending counts what somebody's want is waiting for
(`BotMarket.Answering`, "for an order") and keeps the rest; the goods ride in the pack. The porter (`BotUnload`) keeps in the
pack what a buyer within a carrier's reach has asked for (`BotCourier.Deliverable`) and puts everything else over the keep list in
the bank box; from there a shift at the shop by the bank sells what the island asks for, and a peddle at a bank takes what
nobody asks for to a shopkeeper (`BotPeddler`). A bot at a bank reaches into its own box (`FromBank`) — never from anywhere.

## Workshops

Smithing (`BotSmith.WorkshopsInBritain`) and spinning and weaving (`BotLooms.WorkshopsInBritain`) use Britain's forges, wheels and
looms. Smelting stays at the forge nearest the seam: it is the miner's own step, and ore is about a hundred times heavier than
the ingots it becomes. Tailoring, tinkering, fletching, bowcraft, alchemy and inscription need no station and happen where the
bot is; cooking needs a fire, and a campfire is one.

## Counters to watch

`The market:` (every five minutes, `BotBeat` and `BotSquads`): the wants — said, asked for more, said aloud, met, given up, stood
down for want of money, refused at the asking, taken back, gone with their buyer, raised, cut; things handed over against wants;
the meetings (`BotHandover.Describe`: moved, coin against cheque, refusals by cause, most by value); the carriers
(`BotCourier.Describe`: looks, carrying, offered, from the box, passed over by cause); the deliveries (`BotDeliver.Describe`:
taken, handed over, tiles, failures by cause); bank-box withdrawals at a bank; learned prices. `The board:` — most wanted beside
what the shops show. `Bot shops:` — shifts, sales, the city's buys at the shops, fighters refused, guild members sent home.
`Needs:` — `AskedFar`/`WaitedFar`/`WalkedFar` (supplies asked for aloud instead of a four-minute walk), and the porter's
"kept for a carrier" count.

## Known rough edges

A buyer who keeps walking away is followed for eight minutes and then given up on (`Late`). A want for something nobody on the
island makes rises to its ceiling and gives up, as the auction's did. Goods nobody asks for and no shopkeeper buys stay in a bank
box.

## Lines, the reservation and the saved board (02.10.2026, build 391)

- **Lines** (`BotLine`, `BotLines`). A want's units asked at one time are a line with its own clock; a top-up more than ten
  minutes after the newest line is a new line. A line is met if every unit reaches the buyer within its first hour, failed if
  not (or if, after its tenth minute, the buyer could not pay, took it back or gave it up), kept out if the buyer is gone or it
  ended inside ten minutes. Failures name a cause from what the want recorded: who passed it over and why (`BotWant.Passed`),
  walks offered and taken, how long the buyer lay dead. Every settle is one log line, `Line #<want>.<n> ...`; the totals are
  the `Lines:` summary line and the `needs` block of `/api/state`. A trader's want is procurement, counted apart.
- **The reservation.** A want's money is promised when it is said: `BotMarket.Reserved` is units still wanted times the offer
  over a bot's open wants, `BotYield.Spendable` is wealth less that, and a want that cannot be reserved is not said. Every
  decision to spend elsewhere reads `Spendable`; `Charge` refuses a bill that would reach into the reservation unless it pays
  for the reserved kind itself. A raise is a larger promise and must be reservable; when it is not, the want waits at its price
  until `MostAgeMs`.
- **A price over the offer pays only with free money, and is held once agreed (03.10.2026, build next).** `BotMarket.Affords`
  says how many units a buyer may pay for at a price: the units' own reservation pays the offer, free money (and the carriage
  held for that carrier) the rest. `Backing` lent the whole want's reservation to part of it, so a trader's 19gp for 11 of 13
  ribs at 13 spent what the last two held and the want stood down on the next beat: all seven rib lines failed "buyer could not
  pay" (three of them "after a death") on live 395 between 03:08 and 03:25 were this. A carrier's reservation carries the price
  it agreed (`BotWant.Reserve(courier, units, price)`), and what that is over the offer is held with the want (`Premium`, in
  `Reserved` and in the beat's purse walk) until the meeting. Counted: `The market:` → `reserve:` "carriage over the offer
  agreed with traders and held now", and the meetings' "handed fewer than brought for what the buyer could pay".
- **A dead buyer keeps its want.** Its purse is not asked while it lies dead, nor for `RisenGraceMs` after it rises.
- **The board is saved** (`BotMarketStore`): wants and lines by ages, never ticks, buyers by serial, kinds by name; no
  carrier's reservation. Made again when the market starts; a want whose buyer or kind is gone is dropped and counted. A wipe
  empties it. This reverses "a restart is a new market", which held while a want escrowed gold.

## A material's ceiling and the reserve in the account (02.10.2026, build next)

- **A raw material is never worth more than twice what it becomes** (Patrick). `BotMarket.Ceiling` reads the craft systems the
  bots work in: of every recipe that uses the material, the product's worth (`Worth`, else the cheapest counter) over the units
  it takes, the best of them, times `MaterialShare` (2). Raw meat is capped lower, at the meal it makes (`BotOven.MealWorth`):
  the cook's own sum. A new want opens no higher than its ceiling and is not said when nothing sells or makes the material under
  it (`Sourced`); a raise stops at it and the want then waits at its price until `MostAgeMs`; a trader's price never passes it
  (`BotTrading.Alternative`) and a source that costs as much is passed over (`OverItsCeiling`). Bandages, potions, reagents,
  arrows and bolts have no ceiling. On build 392 traders' raw ribs from the butcher at 16gp were 51 % of all counter spending.
- **A want read back from the save gets its ceiling on the beat (03.10.2026, build next).** The board is read back before the
  craft systems are built and before anything is priced, so `Materialize` read every ceiling as nothing, and traders bought
  read-back rib wants at the butcher and charged 17–25gp a rib against offers of 6: 50 of 52 rib orders in live 395's first 20
  minutes, 3,657gp of carriage. A read-back want is now `CeilingPending`: the beat (and a trader's look) reads its ceiling again
  until it has one (`Recap`, up to `RecapMs`), and an offer above it comes down to it (`Bring`). Counted in `reserve:` ("given
  their material's ceiling on the beat … brought down to it").
- **Raw meat is asked for five at a time and no faster than the island brings it (03.10.2026, build next).** `BotStores`:
  `MeatBatch` five (a carrier hands 4.5 ribs a meeting on live 395; a meal lasts ten minutes), and a want for meat is not said
  while the board already asks for more of it than `StockShare` (half) of what was handed over against it in the last hour
  (`BotMarket.Brought`, twelve five-minute buckets filled at `Received`), or `StockFloor` (30). What the board asks counts only
  buyers in the world (`Wanting`): a resting buyer's want stands for hours and would shut the room for everybody. On live 395 the
  rib wants said after the start asked 521 units in 27 minutes against about 380 an hour carried to every rib want. Counted in
  `Needs:` (the stores clause: bots held back once a hold, their units, every refused ask, the board's ribs against its room),
  and `Lines:` prints the share for raw meat and for everything else side by side (`needs.shareStock`, `needs.shareOther`), so
  fewer rib lines said is never read as better service.
- **The rib room follows a fall within a quarter of an hour, and a hunter's own ribs take its want back (06.10.2026, ribs17).**
  The room is now half of `BotMarket.Supply`: the lesser of the hour's sum (`Brought`) and `Pace`, a decaying sum of the same
  hand-overs with a half-life of `SupplyHalfLifeMs` (15 min) read as an hourly rate; a rise is believed an hour at a time, a fall
  within a quarter. An ask that would pass the room is said smaller, down to `BotStores.MeatLeast` (3), before it is held; and a
  cook asks for three while the pace runs under `ThinShare` (half) of the hour's sum. A cook whose own hunting brought the units
  its rib want asks for takes the want back (`TakeBackSated`, "had it another way", counted as any taking back): on builds 411
  and 412, 75–95 % of the ribs carved from the 60th minute on were carved by bots with their own rib want standing, and a carrier
  that wants a kind is passed over for every want of it. Counted in `Needs:` (the stores clause) and `Lines:` (the raw-meat part:
  pace, hour, room, lines said smaller, taken back), `needs.stockPace`, `stockHour`, `stockRoom`, `stockAsked`,
  `stockSizedToRoom`, `stockSizedThin`, `stockSated`, `stockSatedEarly`.
- **A buyer down the dungeon block that nobody from outside could reach is left until it comes up (03.10.2026, build next).**
  `BotUnreached`: a carrier's walk or a trader's haul that set out from outside the block and found no way (its `Bend`) to a
  buyer standing at x ≥ 5120 marks the buyer and its ground (its dungeon's box, else a 512-tile square of the block) for 30
  minutes, doubling to two hours — not a walker that never moved, nor one already in the buyer's own dungeon, square or land;
  carriers and traders outside the buyer's land pass such a want over ("no way through to the buyer", `BotPass.Sealed`) until
  the buyer is back on the island or something is handed over there by a walk from outside; a looker already in the buyer's
  land is never refused. A carrier or trader standing in the block is offered no walk out of its
  own land of the walkers' graph (`Shut`). On live 396, 33 of the 35 traders' orders struck "no way through to the buyer"
  touched the block, 4 of its 38 orders were handed over against 149 of 186 on the island, and seven traders bought and walked
  for Edith in a pocket at (6139, 1432) one after another: the reach map calls it open and the gates join it, so the haul's own
  questions could not say no, and each trader's ledger was its own. Counted: `The market:` → "the dungeon block: …" (marked,
  resting now, looks refused, cleared, come up); carriers' "from a carrier standing in the dungeon block …" and "down the dungeon
  block where …"; `Traders:` the same two among "wants passed over".
- **A delivery put down three minutes in all lets its units go (03.10.2026, build next).** `BotDeliver.Stale` read one pause
  of eight minutes; a company's call is a minute or two, so a carrier pressed six times held five ribs fifteen minutes and then
  dropped the walk. Past `ReleaseDownMs` (summed) the sweep opens the units to other carriers (`Payable`) while the want still
  reads the carrier as coming (`Coming`, until the old eight-minute single pause), so a shopper's far want, ask10's ask and the
  raise wait as they did before; taken up again, the walk holds what is still open and the buyer's money still carries, or ends
  "another carrier took … on while this walk was put down" / "… money no longer carried …". A re-hold is not counted as a new take on the
  want (`BotWant.Reserve(…, retake)`), so "taken by a carrier, not brought" counts carriers. Counted in the deliveries clause.
- **The reserve is kept in the account** (`BotMarket.Secure`): what a buyer's account lacks of what its wants hold is put in from
  its pocket when a want is said and on each beat. `Charge` pays from the pack and then the account, so the money still pays the
  meeting, and a death no longer takes it: 21 of 44 failed lines on live 392 at 16:44 were "buyer could not pay after a death".
- **A scroll no shelf sells is never asked for above what writing it costs here** (03.10.2026, `BotSpells/BotScribes.Ceiling`):
  a new want opens no higher and a raise stops there, the want then waiting at its price to `MostAgeMs` like a material's; wants
  read back from the save are cut to it at the scribes' first census. No `Sourced` test: the island's scribes are the source.
  Cirdan's Greater Heal had reached 326gp for 17gp of paper and herbs. Held raises are counted on the `Needs:` line's `scrolls:`
  clause, not in `CeilingHeld`, and a line given up there says "given up at the price of writing it". See `BotSpells/README.md`.

## What a want opens at, and how far it climbs (03.10.2026, build next)

Patrick, 03.10 ~12:40: "Traders' procurement offers climb to 20× (ChainLegs 2636gp): a new want starts from the ceiling of the
last and climbs ×4 again. Fix?" — "We fix it."

- **Measured (build 396, 04:19–12:47; `agents/price13-tools/wants.py`).** `Worth` read the dearest standing offer first (`Best`),
  up to ×4 of the caller's estimate, and that offer was usually a want that had climbed ×4 unanswered. Every one of 26 chain-legs
  wants opened at 864 (×4 of the armourer's 216) and climbed to 3,456 — 23 times the smith's counter (149); 14 of them were the
  same buyer saying again a want just given up. Chain chests reached 3,840 (27× the counter's 143), harm scrolls 352 (16× the
  mage's 22), plate gorgets 1,273 (12×), leather chests 902 (9×). Leather chests opened at 384 (×4 of 96), lesser cures at 20
  (×4 of the shopper's 5). The same bids set the materials' ceilings: "Leather 130gp (LeatherBustierArms at 388)", "IronIngot 84gp
  (PlateGorget at 416)".
- **What it opens at.** `Worth(kind, estimate)` is what the kind really changed hands for between bots, else the caller's own
  estimate — a shopkeeper's price, what it costs to make, a stand-in. It never reads a standing offer: an unmet offer is a
  sentence, not a sale, and what it is met at, if it is met, is learned in `Traded`. So openings, materials' ceilings (`Ceiling`
  reads the products' `Worth`) and every producer's reckoning read prices that were paid. Two callers whose estimate was only
  ever a floor under the bid now say what the thing costs: the armourer opens at twice the material at what the material goes
  for (`BotHarness.Piece.Making`: a leather chest at 12 leathers × max(4, the leather's price) × 2, not 12 × 4 × 2), and the
  shopper's stand-in is never under the cheapest shopkeeper's price (`BotShopper.Stand`: a lesser cure at 15, not 5).
- **How far it climbs.** A want's top (`BotWant.Highest`) is the lowest of ×`MostMultiple` its opening, its material's ceiling, and
  ×`MostMultiple` the cheapest shopkeeper's price for the kind (`BotWant.Shelf`, `BotShops.Lowest`): a buyer can always walk to
  the counter. A new want opens no higher than that (`ShelfCapped`). At the shopkeeper's ×4 a want waits at its price to
  `MostAgeMs`, as a material's want does, rather than giving up ten minutes later (`ShelfHeld`; its line, if it fails, says
  "given up at four times a shopkeeper's price").
- **Re-asks.** A want that gives up is remembered for its buyer and kind (`ReaskMs`, two hours); the same buyer's next want for
  the kind opens no higher than the given-up one opened (`Reasked`, `ReaskCapped`).
- **Read back from the save.** The board keeps its anchors (the save holds no other); each read-back want is given its
  shopkeeper's price on the beat once the counters are surveyed (`Shelve`, within `RecapMs` of the start, as `Recap` does for a
  material's ceiling), and an offer above ×4 of it comes down to it (logged per want: "X's want for Chain Legs came down from
  993gp to 596gp, four times the 149gp a shopkeeper asks for one").
- **Counted** in `The market:` → "what a want opens at …": wants said while a dearer offer for the kind stood (the old opening) and
  the gold that offer was over the new one, wants anchored where a shopkeeper sells the kind, openings held to ×4 of its price,
  raises not taken there, re-asks and re-asks held to their first opening, read-back wants given their shopkeeper's price and
  brought down, and the dearest standing offer of each kind against what it opened at, steepest first.
- **Kinds with no shopkeeper and no material's ceiling** (a scroll of the fourth circle up is held by `BotScribes.Ceiling`
  instead) climb ×4 of their opening as before; a sale at the top of a climb is learned in `Worth` and may lift the next
  opening. None of the kinds measured above is one.
