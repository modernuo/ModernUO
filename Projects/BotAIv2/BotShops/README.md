# Shops: trading with the NPCs, both ways

A capability for **every** bot rather than for one trade: a mage buys reagents, anybody buys bandages, a crafter
buys cloth to work with — and whatever the population refused goes back over the same counter for coin.

| File | What is in it |
|---|---|
| `BotShops.cs` | where the shopkeepers are, what they sell, what they buy, and both transactions |
| `BotRestock.cs` | an obligation: go and buy what has run out — off a shelf or off another bot's stall; a guild shelf bought out on arrival sends it on to a shopkeeper |
| `BotShopper.cs` | the proposer: who is short of what; keeps back `BotPurse.KeepBack` (build 64) |
| `BotAskFirst.cs` | a restock of cloth or bandages, a reagent, arrows or bolts or a tool asks the island first when a bot within a carrier's reach holds it over its keep: a want at the counter's price, three minutes for a carrier, then the walk (03.10.2026) |
| `BotPeddle.cs` | an obligation: take what nobody wanted to somebody who will buy it; a load bought off its stall on the way ends finished, and one the market handed back into the pack on the way is sold from the pack |
| `BotPeddler.cs` | the proposer: whose stall the population has ignored; a load out of a bank box holds what the pack carries under `BotUnload.Heavy` and at most `BarLoad` (300) bars, a coloured load no more than the counters' hourly purse (`BotOre.CounterRoom`; 06.10.2026, ingots16) |
| `BotShopsConfig.cs` | `Configuration/bot-shops.json` |
| `BotShopsModule.cs` | module, phase `World`, requires `Classes` and `Will` |
| `BotBullion.cs` | a crafter with money buys its metal instead of going and digging it; keeps back `BotPurse.KeepBack` (build 64); stocks to the metal the dearest armour order the smith passed over takes, when that is more than `Enough` (`BotSmith.Short`, 03.10.2026) |
| `BotNeeds.cs` | how often a bot reconsiders what it is short of |
| `BotOrder.cs` | putting an order on the board, and going back for it when somebody has filled it; paperwork, taken at the next choice rather than over work in hand (build 50) |
| `BotStores.cs` | a crafter short of the raw material of its trade, putting the order to the population; keeps back `BotPurse.KeepBack` (build 64); raw meat five at a time (`MeatBatch`) and not while the board already asks for more than half an hour of what the island brings it (`SupplyGated`, `StockShare`, `StockFloor`; 03.10.2026), sized to the lesser of the hour and the last quarter-hour's pace, said smaller (`MeatLeast` 3) before it is held or while the pace is thin (`ThinShare`), and taken back when the cook's own hunting brought it (`TakeBackSated`; 06.10.2026, ribs17) |
| `BotUpkeep.cs` | asking the population, by name, for a replacement for something that is wearing out; keeps back `BotPurse.KeepBack` (build 64) |
| `BotProvision.cs` | What a bot must carry before it goes into a fight it chose — a dungeon's delve, or a war company sent against somebody else's ground — and the guild's money for what it cannot buy itself. |
| `BotCapital.cs` | Britain as the centre of trade: the capital's shopkeepers pay better, a stall's load walks to them when that pays for the road, a trader's journey goes there (30.09.2026) |

---

## Both directions of the world's money live here

Coin handed to a shopkeeper **leaves** the world; coin taken from one is the only coin that **enters** it. Bots
are born with none and nothing else mints any, so without `BotPeddler` every piece of work with an `Outlay`
failed on its first beat and the only thing that could happen on the shard was digging, which is free.

Which of the two dominates is the whole health of the economy, and it is one subtraction: see the
`N things bought for Xgp, M sold for Ygp` line this module prints.

---

## A purchase goes through the shopkeeper itself

`BaseVendor.OnBuyItems` is the same call a player's shop window makes. It charges the shard's real prices out of
the pack and the account behind it, and hands over real goods. A bot is a customer on exactly a player's terms,
which matters more than it sounds: every price scalar, every access rule and every stock limit applies without
being reimplemented, and nothing here can invent goods or money.

**Two lines that are not obvious, both paid for by the first version:**

Shelves refill on a timer that is only wound when somebody **opens the shop window**. Bots never open one — so
without an explicit `Restock()` a shop a bot has cleaned out stays empty for good.

Prices carry the shard's own scalars and update **on demand** — `UpdateBuyInfo()` before every transaction.

And a third, ours: **affordability is worked out here** rather than left to the engine. It takes an order whole
or not at all — an order for ten against a purse of eight buys nothing and says nothing.

---

## A sale goes through it too

`Buys` asks the shopkeeper about **this object** rather than about a type, because that is the only question the
engine answers: `IsSellable` looks at the item. `Buyer` finds the nearest one that will take it, and `Sell` goes
through `OnSellItems`.

**The takings are measured, not added up from the price tags.** The engine decides how much of an order it
honours — it refuses anything not in the seller's own pack, anything immovable, anything above its per-visit
limit — so believing the asking prices would be counting an intention. And it requires `IsStandardLoot()`, while
the bind marks issued gear `Newbied`: **bound gear cannot be sold, enforced by the engine**, which is a free
guarantee on the bind's third promise.

---

## Who is short of what

The list of needs comes **from the class's kit** — the same list the world handed the bot at birth. No table of
who buys what: add a class and it arrives with its own kit, and this works by itself.

| What | For whom | When |
|---|---|---|
| **the weapon** | anybody whose blade has worn through | it is gone from hand and pack |
| **ammunition** | archers | below half of what it was born with |
| **a tool** | anybody with one on the class list | worn through, or left in a corpse |
| bandages | everybody | below half of what it was born with |
| **potions** | everybody, per `PotionLimits` | one for one, as soon as one is missing |
| reagents (**eight** kinds) | anybody whose kit has reagents | below half |
| blank scrolls | anybody with a pen | in the course of the work, by the scribe's own obligation |

**The weapon comes before everything**, because a bot with no weapon cannot even defend itself — and because it
is the one thing on this list a crafter can make. A blade wears down on every landed hit and the engine destroys
it at zero.

**Then the tool.** Only the weapon is bound; a tool wears through — the engine gives a fresh one 25 to 75 uses,
spends one an attempt and destroys it at zero. A bot with no tool has no trade at all and cannot even earn the
price of bandages. **And the failure is silent by nature:** the proposer simply stops offering the work, which
looks exactly like a bot that never had a kit. This errand is the whole of what stands between "tools wear out"
and "trades quietly end".

The tool list comes from `BotOutfit.ToolsFor` — the same one birth issued from, including the mortar and the pen
derived from the build. The potion list comes from `BotOutfit.PotionsFor` the same way.

Then bandages, then potions, then reagents: one keeps a bot standing, one is the only mending that works while
something is hitting it, and the last makes it useful.

**The bots' market is asked before a counter, always.** `BotShopper` compares the shelf against a bot's shop by
the bank (`BotShopkeep.Beats`, the walk to each counted) and takes whichever is cheaper — so a shopkeeper is the
**ceiling** on what a bot can charge and never the preference. (The auction's cheapest stall, which needed no walk,
was the comparison until 01.10.2026. A counter four minutes' walk or more away for a thing the island makes is asked of
the island aloud first — `BotShopper.AskFarMinutes` — and a carrier brings it.) That ordering is where a crafter's living comes from: a fighter's gold came off
a monster, and it goes to a smith rather than out of the world whenever the smith asks less than the shelf.

**Then the guild's own counter, asked the same two questions as a shopkeeper.** A shopkeeper this bot lately failed to
reach is skipped (its caution is filed under `BotShops.ShopKind` at the counter's place, which is the word
`BotShops.Nearest` asks in), and so is one the shard's reach ledger has proved there is no way to. The hall's merchant
came later and was offered on price alone, and `BotRestock.Bend` wrote nothing for it: on 14.09.2026 Edda Ashdown was
sent to The Needle's counter six times in two seconds, "no way from here", until `BotBreaker` rested the work. The
merchant route now files the same caution, the hall is passed over on either answer (`BotShopper.HallWalled`), and the
supply is sought at a shopkeeper instead.

**Who sells what** (read out of `SBInfo`): pickaxe 22 at the blacksmith, miner, tinker and weaponsmith;
**hatchet 25 at the weaponsmith only** — the narrowest supply on the shard; sewing kit 3 at the tailor and
tinker; smith hammer 21; scissors 11; mortar 8 at the alchemist and herbalist; pen 8 at the mage and scribe;
lesser heal and cure potions 15 at the alchemist and mage. The tinker stocks nearly everything at once, so one
tinker's shop in a town covers almost any loss.

Supplies are deliberately **not bound** — they are meant to run out. That is what makes a shop worth walking to,
and what makes another bot's production worth paying for.

---

## How the two errands are priced

**Buying: at nothing, and that is honest.** A purchase creates no wealth — coin becomes goods — so the
obligation declares `Made` equal to what was paid and the trip comes out at **about nothing per minute** rather
than at a loss. It is never punished and never preferred over work that produces something, which is exactly
where an errand to the shops belongs.

The one number that does real work there is `Outlay`: the brain measures need against it. A bot that cannot
afford its own bandages feels short of money, and stops the moment it can.

**Both directions go through the pack, and a pack has a cap on things as well as on weight.** The engine takes a
bill under two thousand gold out of the backpack and puts a shopkeeper's payment into it, so a pack at
`Container.GlobalMaxItems` (125 things) with no pile of gold on top can neither pay nor be paid: the coin bounces at
the counter. Every errand that changes money over a counter says so (`BotDeed.AtCounter`: restocking at a shop,
sewing, brewing, fletching, inscribing, a scroll bought at a counter, a contract, a guild's batch, peddling), and the
appraisal refuses it to such a pack before it is weighed (`BotYield.Pocket`); the porter offers that bot the trip that
makes room instead. Found on 14.09.2026, when one bot jammed that way failed a trip to the shops 8,719 times in half
an hour.

**Selling: at what the load is actually worth, per minute.** The proposer knows both numbers exactly — how many
there are and what this shopkeeper pays for one — so a guess would be strictly worse than the truth, and twenty
ingots outranking three is the behaviour anybody would want. `Coin` is 1.0, and this is the only obligation in
the project of which that is true.

**And the condition for going there at all is the market's own price.** A stall that has never sold one and has
stood for a full stale period has been in front of every bot on the shard for half an hour with nobody
interested. That is the shard saying "nobody here wants this", in the only language it has, and it costs no new
number to hear. No "is this junk" test, no table of worthless things.

`BotPeddler` can also only ever offer what the bot **itself decided to sell**: tools, herbs, paper and bandages
are never on a stall, so they are never candidates. That is the same sign rule that kills the ginseng carousel —
in v1 two bots sold the same shopkeeper the same reagents four thousand times because nothing distinguished
"goods" from "the things I need to do my job".

---

## Ask the island before the walk (03.10.2026, `BotAskFirst`)

Patrick, 03.10 night: *"the focus is 99 % fulfilment of Needs; we have to bring the economy to life."* On build 395 (03:08–04:19,
210 bots) the restocks bought over a counter 1,580 cloth (3,703gp), 1,406 arrows (3,438gp), 404 bandages (2,333gp), 70 pickaxes
(1,588gp) and 856 reagents (3,288gp), while herb pickers came back with herbs "kept to sell", fletchers kept arrows and tinkers kept
tools; the shopper named 90.9 % of its errands to a counter (`flow9.md`). The far ask needs a four-minute walk and a thing a craft
makes (no cloth, bandage, reagent or tool); the pile ask needs an island-wide glut and leaves out arrows.

| step | what happens |
|---|---|
| **who holds it** | every `HoldersMs` (15 s) the population is read the way a carrier reads a pack: merchandise over `BotUnload.Keeps`, and a bank box's for a bot at a bank; a worn-out tool does not count; a bot in a company is left out. A holder counts for a buyer when it is within `BotCourier.Reach` on the same land, does not want the kind itself, is not already on a delivery or a haul, and holds at least a carrier's least (`LeastGold`) of it; less what other buyers within that reach already ask for |
| **the ask** | before the walk to a counter (and, where no counter in reach sells it, before the board), for cloth or bandages, a reagent, arrows or bolts, a tool the class carries: a want (`BotMarket.Ask`) at the counter's price to this bot (`BotBurgh.Dear` included; the island's lowest counter price where none is in reach), for no more than the largest single holder holds — one carrier can meet it — and only when that is at least half of what the purse would buy. What the bot would buy is asked for at what it would pay: cloth at cloth's price when it cuts cloth into bandages (cloth handed over is cut), bandages at a bandage's otherwise |
| **the wait** | the bot goes on with its work and its other supplies: the shopper passes the waiting supply over (`BotAskFirst.Parked`, read in `Wanting`) and looks at the next, so nothing is bought twice and nothing else waits. A carrier that has taken it is always waited for, urgent or not: taking it back would fail the carrier's walk |
| **the end** | at every look of the bot (`Review`) and swept every `SweepMs` (5 s): met — no walk; nobody has taken it at `WaitMs` (3 min) — taken back by the existing road (`BotWithdraw.Walked`, "nobody brought it, walked to a counter"; "other" for a buyer dead or out of the world) and the bot walks as before; its need turned urgent with nobody bringing it — taken back at once and walked for; not asked again for `AgainMs` (9 min) |
| **never** | a bot already walking a restock (in hand or put down); a bot below its entry threshold (`BotProvision.Short`, the shopper's `needy`: companies and delves do not wait on it), under `HurtShare` (half) of its health, in a fight (struck lately, or a foe set), out of a supply it fights or mends with (its kit's bandages, a bottle its class carries, its own arrows, a reagent its key spell burns), a medic below the bandages the doors ask; the band's order; the bot's own weapon; a potion; a purse that cannot carry the compulsory part (that bot walks, and its guild pays below the threshold as before) |

**Counting is unchanged.** A line taken back inside its first ten minutes is kept out by build 391's rule; one taken back later (a
carrier who took the walk and gave up late) fails, and is counted beside the ask too. `Running` false says nothing new and still sees
the standing asks through. Every gate is in the `Trade:` line, by family: asked, met before the walk by a carrier, by a trader's haul
(counter goods with carriage) and at a bot's shop, walked after the wait (part met, past ten minutes), had it another way, taken
back with the buyer dead or away, ended otherwise, units and gold handed; looks that walked as before for urgency (five causes), no
holder, holders already asked of, no one holder with half; already on the walk, the purse, a want already standing, an ask inside
`AgainMs`, refused by the market; taken back mid-wait for urgency, and looks urgent with a carrier on the way (waited for); looks that passed a waiting supply
over, had nothing short but it, had a carrier on the way; read back after a restart (adopted once per bot at its first look);
bandages cut; holders at the last reading.

---

## The capital: Britain as the centre of trade

Patrick's order of 30.09.2026, evening: *«rework the trade: the traders must put Britain first as the centre of trade»*.
Britain already carried half the counter trade, all of it from its own ring: every chooser of a counter was "the nearest",
and every other town stands 800 to 2200 tiles off. `BotCapital` makes the capital worth a road without ordering anybody down
one, because a long road kills (restocks of 600 tiles and more died 11.5 % of the time on 30.09, journeys 26 %).

| what | how |
|---|---|
| **the capital's price** | Britain's shopkeepers pay `BotCapital.Premium` (×1.25) of their price, never above the island's lowest ask for the thing (`Factor`: no counter may pay more than a shelf asks); the difference is paid by the city out of its treasury above `Reserve` (`BotCity.Draw`), into the pack, the account, or the pack regardless |
| **the sale's counter** | `BotShops.Buyer` weighs every buyer at `units × price × premium − tiles × BotPeddler.PettyPerTile`, the premium counted only for a load on a stall, a counter within the bot's leash and `Reach` (1000), on a joined land, with the treasury able to pay (`Pull`); pack goods go to the best buyer at its own price. Sealed and unplaceable counters are passed over, as `Nearest` has done since build 357 |
| **the trader's journey** | a bot holding a shop's worth of what the island asks for (`Trader`: `BotShopkeep.Look` against `OpenWorth`), offered a journey by `BotTraveller`, goes to the capital when its road is on offer; standing in the capital, it is not offered a journey away. Its shift then opens at Britain's bank (`BotShopkeeper.Britain`, which now reads the capital's name) |
| **not changed** | buying (a consumer buys at the nearest counter: no price difference between towns pays for 800 tiles of road), the porter (the bank box is global and the market placeless, so the bank chosen changes only the walk), the guild's courier, the town prices of `BotBurgh` |

Everything is counted in the `Trade:` line (`the capital (Britain) — …`): sales over its counters against elsewhere, the premium
paid and short, peddles offered past a nearer buyer and sold there, the reasons its price did not pull, purchases, shop shifts,
and the traders' journeys; the city's line says what the premium has cost the treasury.

---

## What to check with a client

1. `Shops ready: ...` at startup, then `Found N shopkeepers within 160 tiles of ...` from the first bot to ask.
2. `No shopkeeper within reach ... sells Bandage` — the spawn point is too far from the shops; move it or raise
   `Reach`.
3. Once something starts being consumed: `Alden bought 12 Bandage from Ellie for 36gp` and `took on restock`.
4. `Alden sold 1 things to Verity for 80gp` — the first gold to enter the world over a counter.
5. The reload line: `N things bought for Xgp, M sold for Ygp`. **The difference between those two numbers is the
   health of the economy**, in one subtraction. In v1 it was −110,900 over a night and nobody could say where.

---

## Rough edges

**Reagents are now consumed.** Not by casting — nothing casts — but by inscription: a failed scroll ruins the
herbs and the paper, so climbing Inscribe from nothing to eighty burns hundreds of reagents. That is the first
real demand for restocking in the project, and it makes `Short = 0.5` a number that finally means something.
Bandages are consumed as of `BotMend/`: self-mending burns them for anybody without mana or a book.

Eight kinds of reagent rather than six: almost nothing can be written without bloodmoss and mandrake root. See
`BotSpells/README.md`.

**Shopkeepers are held by reference** and live exactly one world: the list is cleared on a reload.

**The Britain boundary** applies here as it does in the ground: a shop outside `BotPopulation.Roam` is never
offered.

**It compiles** — the engine surface was read out of the fork and confirmed by the compiler:
`BaseVendor.OnBuyItems/OnSellItems/GetBuyInfo/GetSellInfo/Restock/UpdateBuyInfo/IsActiveSeller/IsActiveBuyer/
LastRestock/RestockDelay`, `GenericBuyInfo.Type/Price/Amount/GetDisplayEntity`, `BuyItemResponse`,
`SellItemResponse`, `IShopSellInfo.IsSellable/GetSellPriceFor`, `Item.IsStandardLoot`.
