# Bots' shops: trade between bots in a place

Patrick's order of 29.09.2026, evening: move away from the internal auction by degrees; bots keep shops by the bank or
their own storage and call out to the bots that need their goods. **Since 01.10.2026 (Patrick's order of 30.09) the auction
is gone and these shops are the market** (`BotMarket/README.md`): only makers and gatherers keep shop
(`BotShopkeeper.Merchant`), by Britain's bank — a guild's member by its guild's own town's — the opening said in the world's
chat, a customer buying within two tiles (`ServeReach`), and the city's buys, fairs and standing orders served here
(`BotShopkeep.CityBuy`). Where this file speaks of stalls, the lot cap or escrow below, it describes the shops of 29.09.

| File | What is in it |
|---|---|
| `BotShopkeep.cs` | the open shops, the stock count and its prices, the customer's weighing (`Best`, `Beats`), the visit and the till (`Visit`, `Serve`), the cry, the page, the summary |
| `BotStorefront.cs` | one open shop: keeper, pitch, bank, town, the count of its goods and asks, what it has sold this shift |
| `BotShopkeeper.cs` | the proposer: whose goods are worth a shift, and where it would stand |
| `BotKeepShop.cs` | the shift: walk to the pitch, open, call out, serve, shut on the clock or when sold out; put down and reopened for what will not wait |
| `BotPatron.cs` | the proposer for a want on the board: walk to a shop that sells the thing for less than the want offers |
| `BotShopBook.cs` | per town and kind: what bots bought at bots' shops, what they bought from shopkeepers, what they came for and found gone — the merchant's book |
| `BotShopkeepConfig.cs` | `Configuration/bot-botshops.json` |
| `BotShopkeepModule.cs` | module, phase `World`, requires `Will`, `Shops`, `Auction`, `Population`; sets the auction's lot cap; writes `Bot shops:` every five minutes |

Hooks in shared files as of 29.09 (the auction's are gone since 01.10.2026), each commented where it stands: `BotCore` registers the module; `BotAuction.List` refused a new kind
past `LotsPerBot` (with `Capped` and a clause on `The market:` line); `BotShopper.Propose` asks `BotShopkeep.Beats` before
the stall and the counter; `BotRestock` has a fourth constructor, a branch in `Advance`, `Bend`, `AtCounter` and `Outlay`
for it, and books each shopkeeper purchase in `BotShopBook`; `BotWebSnapshot` writes `shops`; `BotPhrases` has the four
`shop:` occasions; the page draws a `shops` layer.

---

## Where the goods are

**In the keeper's pack and bank box, all the time.** Nothing is lifted out of the world as a stall lifts it. The shop is a
count (`BotStorefront`), taken when it opens, every `StockMs` after, and at every sale; the till counts again before
anything moves. What is for sale is what the porter would call surplus: the bank box whole (nothing but surplus is ever put
there, `BotUnload.Store`), and the pack over `BotUnload.Keeps`. Never what is bound, coin, a container, a key, a rune or a
runebook, a spellbook, a ship's or a house's deed, anything this assembly made for a bot's own use, or a kind the keeper
has a want out for.

**At the bank it first serves itself.** On opening, a keeper tops its pack up to its own keep list out of its own box
(`SelfSupply`): the porter's `Store` puts the surplus there and nothing else ever takes it out, so without this a shop would
sell its keeper's bandages while the keeper went to a shopkeeper for more.

## Where the shop stands

**Beside the banker nearest home** (`BotPopulation.HomeOf`: the town's bank for a resident of a town, the guild's seat or
the population's home otherwise), within `BankTiles`. The places round the banker from `PitchNear` to `PitchFar` tiles, on
its floor, are read off the map once per `BankMemoryMs`; a pitch is free when no open shop is within `Apart`, the bot has
not lately failed to reach it, and the shard has not proved there is no way to it. At most `PerBank` shops a banker and
`MostOpen` on the island. A guild hall's own counter is the estate's business and is left to it.

## Who comes, and why the shop wins where it does

| buyer | already asks | the shop is weighed against |
|---|---|---|
| a bot short of its kit (`BotShopper`: weapon, ammunition, bandages, draughts, tools, reagents) | the stall if no dearer than the shopkeeper, else the guild's counter, else the shopkeeper | the stall's price and the shopkeeper's price plus the walk to him; the shop's price plus the walk to it |
| a bot with a want on the board (`BotPatron`: materials, replacement gear, scrolls) | waits for a supplier or a stall to cross it | the want's own offer; the shop's price plus the walk |

The walk is priced at `WalkGold` a minute (following `BotRestock.Prior`, what the shard reckons a shopping minute worth),
at the appraisal's own walking pace. Ties go to the shop. A want is taken back off the board only at the till, with the
goods in front of it; what the shop could not supply is asked for again at the same offer.

## Prices

Open at the nearest shopkeeper's ask, or `BotMarket.Worth` when that is less; a kind no shopkeeper sells opens at
`Worth`, falling back on what a shopkeeper pays — and a coloured ingot, which no shopkeeper in this era buys or sells, on
`BotAnvil.Reckon`: iron's price raised by the metal's Blacksmithy floor (01.10.2026; before it, no coloured bar was ever shown and
no smith could see the island had any). Never under `BotMarket.Floor`. Up by `BotMarket.RaiseStep` when a kind
sells again inside `BotMarket.BriskMs`; down by `BotMarket.CutStep` for each kind nobody bought during a whole shift;
always inside `BotMarket.LeastMultiple`…`MostMultiple` of the opening. A buyer pays the price it was quoted when it set
out if the price has risen since. `BotShopkeep.PriceFor` is the one place the price a buyer pays is read.

## Money

The auction's order: the buyer is charged first (`BotMarket.Charge`, pack then account, all or nothing); the goods move;
what did not move is refunded to the buyer's account; the keeper is paid for what did, as coin in its pack (the account if
the pack will not take it, the pack regardless if neither will). No levy.

## The work

`shopkeep` is a steadfast, still work with its own clock: `Still` from the moment it opens (the stall watch and the labour
clock leave it to its shift), held against better offers, put down for a flight or a friend in trouble — the shop shuts
while the keeper is away and opens again when it is back. Finished when the shift has run or the goods are gone (a shift
that sold nothing is finished and says so); failed when the pitch cannot be reached or the bank will not take another
shop. Its takings are measured as coin. The customer's walk is `restock`, the shopper's own errand, unpaid.

## What to check

1. Boot: `Bots' shops are open: … the auction takes at most 3 kinds from one bot`.
2. `X opened a shop by the bank at (x, y) in <town>: …`, and the bot standing there on a client, calling out every
   `CryMs`.
3. `X bought N <kind> at Y's shop in <town> for Ngp … — short of it for its kit` and `— against its own want on the board`.
4. `Bot shops:` every five minutes: shifts, sales by need, the weighing, the till, the book.
5. `The market:` — the new clause `N things stayed in their sellers' hands for want of a lot, at 3 kinds a bot`.
6. The page: the `shops` layer.

## The pitch is in the banker's sight and never a doorway; the walk ends when the shop opens (07.10.2026, work18)

On build 416 (01:16-10:52 on 07.10) shopkeep ended 594 finished, 217 failed, 51 dropped; 195 of the failures were the strip by
Britain's bank: (1422, 1684-1692) outside the west wall and (1423-1430, 1693-1694) on the south wall line, where the doors are.
Only 26 pitches were struck and 2 shifts opened where the keeper stood, because most of the failures came with the shop already
open: the shop opens with the keeper a tile from its pitch, the walk the will sent it on stayed on the road (a work answer does not
end a walk), and when the walker gave up on the pitch's own tile the open shift failed "could not get nearer" (Lucan, 10:00-10:06,
in the doorway at (1423, 1693)). Now:

- `BotKeepShop.Opening` ends the shift's own walk when the shop opens (not a company's station nor an interruption).
- `BotKeepShop.Bend` with the shop open keeps the shift when the keeper is within `StandReach + ServeReach` of its pitch or on
  fit ground in the bank's ring (its stand moves there). `BendIsTrouble` is false for an open shop.
- A keeper still walking after `BotShopkeep.OpenStandingMs` (30 s) on fit ground opens where it stands.
- `Pitches` leaves out a tile within one of a door (`BaseDoor`, open or shut place; `KeepOffDoors`) and a tile the banker has no
  line of sight to (the wall line). `StandsFit` asks the same.
- Counters in `Bot shops:`: tiles left out as a doorway / out of the banker's sight, shifts opened where the keeper stood (of them
  after 30 s of walking), keepers who stood in a doorway and walked on, walks ended when the shop opened, failed walks back that
  kept the shift.
