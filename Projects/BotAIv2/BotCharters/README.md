# BotCharters — town charters, a monopoly on a craft

Patrick, 07.10.2026 evening, approved: *"Town charters: every N hours a town auctions charters (the smiths' charter of Britain,
the tailors' of Minoc, the woodsmen's of Yew). The charter guild sells that kind in the town's shops without a levy; everyone
else pays the charter guild a levy on each sale (the guild's money, not the town's); the town's purse gets the auction price.
Charters make real specialisation; losing one at the auction is a grievance; a failed town task can cost the charter. Argus:
`do charters`, `do charter <town> <craft>`."*

| file | what is in it |
|---|---|
| `BotCharters.cs` | The crafts and what kinds they cover, the period's books, the levy at the three tills, the auction, losing a charter early, the `Charters:` line, the page's `towns[].charters`, the door's two verbs, the store's half. |
| `BotChartersModule.cs` | `bot-charters.json` (`BotChartersSettings`), the module (a look a minute, the line every five), `BotChartersCore` (registers module and store at `CallPriority(45)`, like the events), `BotChartersStore` (`GenericPersistence` "BotCharters"). |

## Crafts and their goods

Six crafts, one charter each per town: **smith** (ingots, and whatever `DefBlacksmithy` makes), **tailor** (leather, cloth, bolts,
`DefTailoring`), **woodsman** (logs, boards, shafts, arrows, bolts, `DefBowFletching`, `DefCarpentry`), **alchemist** (potions,
`DefAlchemy`), **scribe** (scrolls, `DefInscription`), **cook** (food, `DefCooking`). Read off the type once and cached
(`BotCharters.Of`). Hides, ore, reagents, tools and gems belong to nobody's charter.

**A craft's sales are its practitioners' sales** (`LootFree`, on): a seller under `CraftSkill` (30) in the craft's skills —
Blacksmith or Mining; Tailoring; Fletching, Carpentry or Lumberjacking; Alchemy; Inscribe; Cooking — sells free and is not
written in the book. A hunter peddling a looted kryss or a Flamestrike scroll is not a smith or a scribe; the poor hunt.

## The book and the auction

Every practitioner's sale of a craft's kind in a town is written into the period's book: gold by the seller's guild (guildless
as nobody). Every `EveryMs` (6 h of running time; the first `FirstMs`, 1 h, after a boot with nothing saved) each reachable
town's crafts that sold anything or are held go under the hammer:

- **worth** of the charter over a term = `Levy` × the period's gold × term ÷ period (the period at least `LeastPeriodMs`, 30 min);
- a guild bids when its practitioners took ≥ `LeastShare` (10 %) of the gold, it is not the Shadow, not put out of the town, not
  barred, and its chest was not voted a war chest (`BotPurpose`): bid = `Appetite` (0.8) × (½ + ½ × share) × worth, at most
  `SpareShare` (½) of `BotDues.Spare` — what the chest spares beyond what the members voted to save for; under `LeastBid`
  (50gp) is no bid;
- the highest sealed bid (ties to the larger share) is drawn from its chest (`BotChest.Draw`, logged) and paid into the city's
  purse (`BotCity.Tax`); a chest that cannot pay passes to the next bid; no bid and the charter **rests**;
- the old holder that lost it thinks `LostRegard` (−8) of the winner, every other losing bidder half that, written in the
  grievance book as "a charter lost at auction" (`BotRegard.Move`).

A general auction closes every book. An early or hand-called auction (`do charter`) runs until the next general one, its
worth scaled to that.

## The levy

`Levy` (10 %) of a practitioner's sale of a chartered kind by anybody but the holder's guild, hand to hand, no coin made:

| where | hook | how |
|---|---|---|
| a bot's shop (`BotShopkeep.Serve`) and the city's buy there (`BotShopkeep.CityBuy`) | `BotCharters.Split` | the buyer's coin split at the till: the levy into the holder's chest, the rest to the keeper |
| a shopkeeper's counter (`BotShops.Sell`) | `BotCharters.Counter` | the counter's measured pay shared out by line, the levy taken from the seller's pack then account |
| a tavern's meal (`BotTaverns.Serve`) | `BotCharters.After` | taken from the cook's pack then account after the guest paid |

A seller hostile to the holder (at war, or either at the war line of regard) pays the **town's purse** instead: nothing crosses
between enemies. The holder's own sales are free and counted. A levy the seller cannot cover is counted short.

## Losing it early

A holder that fails `FailsToLose` (2) of the town's tasks in a row (`BotBurgh.Fail` → `BotCharters.Failed`), or is put out of the
town (`BotBurgh.Bars`), loses all its charters there; each goes to an early auction at the next minute, and that guild may not
bid for it until the next general auction. A holder guild that is gone loses them too.

## Where it is counted, said and asked

- `Charters:` every five minutes: the charters held with price, levies and own sales; auctions (sold, kept, rested, early),
  bids and their gold, gold into the towns' purse, chests too poor, kept for a war chest, bids unpaid; levies to holders, to a
  town from enemies, short; holders' own and non-practitioners' sales spared; sales booked; charters lost for tasks, exile, a
  guild gone; grievances; minutes to the next auction.
- Log lines: `X won <Town>'s <craft> charter (why) for Ngp into the town's purse: …; the bids: …`, `<Town>'s <craft> charter
  rests …`, `<Town> took the <craft> charter from X: …`, `S paid X a Ngp levy on N Kind sold … in <Town> for Ngp: …`,
  `The towns auctioned their charters …`.
- Speech: `charter:won` / `charter:kept` on the trade channel by the winner's head ("The Hammer holds the smiths' charter of
  Britain."), `charter:lost`, `charter:revoked`, `charter:rests` to the guild, `charter:levy` by the seller; event kind
  `charter` on the stream.
- `/api/state` → `towns[].charters[]`: `craft`, `name`, `holder`, `price`, `since`, `levies`, `levyGold`, `spared`, `sparedGold`,
  `periodGold` — one per craft held or sold this period.
- Argus (by hand): `do charters` lists everything; `do charter <town> <craft>` auctions it now.

Dials: `BotCharters.Running` (off: no levy, no auction; the charters stand), `EveryMs`, `FirstMs`, `Levy`, `Appetite`,
`SpareShare`, `LeastShare`, `LeastBid`, `LeastPeriodMs`, `FailsToLose`, `LostRegard`, `CraftSkill`, `LootFree`, `SayLevy`;
`BotChartersModule.LookMs`, `SayEveryMs`. The file's keys are PascalCase (`EveryHours`, `FirstHours` in hours).
