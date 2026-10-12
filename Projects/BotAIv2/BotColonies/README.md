# Colonies: expeditions, colonies on the islands, the run home, and pirates

Patrick, 07.10.2026: "Ships stand idle. A guild fits out an expedition: the chest pays for the ship and stores, a party of 5–8
with a scout boards, sails to an island (Nujel'm, Magincia, Moonglow, Ocllo) and raises an outpost-colony: its own mine on
untouched veins, a store, a trade route home on the same ship. A colony is guild ground by the claim rules; it can be lost in
war or to pirates: The Shadow at sea intercepts loaded ships."

Part of the sea's module (`BotSeaModule` starts it, `bot-sea.json` carries its switches), so no new module and no line in
`BotCore`. Its store (`BotColonyStore`) registers itself through the engine's own call of every `Configure`.

| file | decides |
|---|---|
| `BotColonies.cs` | the colonies and the parties: the islands and their sites, ships held, adopted or bought, the founding, the store, the clock (shift, claim, loss), the order by hand, the `Sea:` clause, `/api/state`'s `colonies` and Argus's `do expeditions` |
| `BotColony.cs` | one colony: guild, island, site, store, who is on its land since when, and what it has brought in |
| `BotSailing.cs` | one sailing of a guild's ship with a party: the muster, the voyage, the landing |
| `BotExpedition.cs` | `BotExpeditioner` (the guild's ship holder, else its head, weighed every 10 minutes) and the captain's deed: ship, dock, muster, stores, voyage, colony founded and claimed — or a relief |
| `BotSignOn.cs` | `BotSigner` and a member's place in a party: dock, the captain's side, aboard, ashore, the square held while claimed |
| `BotColonyRun.cs` | `BotColonyRunner` and the run home: store into the pack, going-home hands, voyage, ore melted, goods sold, the price shared |
| `BotPiracy.cs` | The Shadow on the quay for a loaded ship, laid through `BotHighway.Lay`; the crew's company when the band strikes |
| `BotColonyStore.cs` | the colonies across a restart |

## How a colony comes to be

1. **The expedition.** Every `ConsiderMs` (10 min) one member of each guild is weighed: whoever holds a small ship, else the
   head. Founding needs an island of `Islands` with no colony, a site, a lane from a dock the captain can walk to, the stores
   (`Cost`, 1500gp) in the chest, and a ship: held, **adopted** (a small ship of the population's own at anchor whose owner is
   gone or is of the guild and not sailing it — money15's thirteen; the harbour cuts it a key, the engine dry-docks it into the
   pack: nothing paid, nothing made, `Salvaged`), or **bought** at a shipwright through `BotShops`, the captain's purse and the
   guild's standing what it lacks (`BotProvision.Fund`) — money15's rule that a ship is bought only for a voyage that pays
   holds: the colony's goods are sold.
2. **The muster.** The captain walks to the dock's first berth and waits `MusterMs` (4 min) for `PartySize - 1` (7) hands at
   its side; members on that land within `MusterReach` (400) are offered the place (`Summons`, so a hold lets it through; a
   pick-holder at 80/min, anybody else at 40). Fewer than `LeastParty - 1` (4) at the clock: called off, nothing paid.
   `BotColonies.Expedition(guild)` and `BotColonies.Enlist(bot, why)` are the scout's door in.
3. **The voyage.** `BotVoyage` with the party as the helm's company, so the party sails as one unit. The stores are drawn from
   the chest into the city's purse (`BotCity.Tax`) the moment the ship is in the water.
4. **The colony.** At the site — the square near the island's dock with the most workable rock (sampled every `SiteStride`),
   off the town's guarded ground, held and claimed by nobody, walked to from the shore — a chest is set down as the store and
   the square is claimed by `BotClaim.Open`: free while the guild has free squares, then `BotClaim.Price` (5000) without the
   step past ten squares (`FlatClaim`). The party stands on it for the claim's five minutes. The colony's site is a root of the
   guild's ground in `BotClaim.Connect`, so the island square stands though no chain of squares reaches the hall.

## The colony at work

Colonists are the guild's members standing on the colony's land. The ground within `Reach` (45) of the site is the population's
ground (`BotPopulation.Within`), so the miner's own trip (`BotDig`) is offered on the island's seams; its metal goes into the
colony's store, not the miner's bank, and with no fire on the island the ore goes in as it is. Colonists are not offered the way
home by sea (`BotSeafarer`) nor a journey (`BotSea.Travel`): the colony's ship takes them.

**The run home** (`BotColonyRun`): the ship holder on the island, when the store holds `CargoAt` (60) units or a shift
(`ShiftMs`, 2 h) is over, no oftener than `RunEveryMs` (45 min). Bars first, then boards, logs and ore into the pack up to
`CargoLoad` of what it carries; the colonists whose shift is over sign on; the voyage to the dock nearest the guild's home;
ore melted at a mainland fire; each kind sold where it fetches most once the road is paid for (`BotShops.Buyer`); the price
shared — `CaptainShare` (10 %) kept, `CrewShare` (30 %) into the hands' banks, the rest into the guild's chest. The only new coin
is the counters'. Unpaid, Committed once loaded. Then a **relief** (the expedition, not founding, no stores by default) takes the
ship and whoever musters back.

**Loss.** A colony whose square was the guild's and is not any more (taken, struck off, swept) is lost; one whose guild is gone or
whose land has had nobody of its guild for `AbandonMs` (3 h) is given up. The store goes with it, its goods counted.

## Pirates

A loaded run (`LeastWorth`, 150gp) is watched at sea; within `LayAheadMs` of its quay, if the quay lies where no guards act and
two or three of The Shadow stand on that land within `PartyReach`, a match for the captain and the hands aboard, the band is laid
on the quay through `BotHighway.Lay` — the highway's own ambush: the wait, the strike, `BotBrawl`'s robbery, `BotQuarter`'s
choice, the goods hot for the fence, the report. The ship holds its course. Two seconds after the band strikes, the crew forms a
company round its captain and is set on the band (`BotSquad.Engage`): company16's rules from there. Every quay of a town on this
map that lies inside its guards is never boarded — counted as `making for a guarded quay`.

## Dials, counters, door

Dials: `BotColonies.Running`, `PartySize`, `LeastParty`, `Cost`, `RelieveCost`, `MusterMs`, `MusterReach`, `ConsiderMs`,
`ShiftMs`, `RunEveryMs`, `CargoAt`, `CargoLoad`, `CaptainShare`, `CrewShare`, `Reach`, `SiteRings`, `SiteStride`, `MinRocks`,
`Islands`, `AbandonMs`, `BuyShip`, `Salvage`, `FlatClaim`, `ClaimRetryMs`, `Order`; `BotPiracy.Running`, `EveryMs`, `LayAheadMs`,
`PartyReach`, `LeastParty`, `MostParty`, `ChooseOdds`, `LeastWorth`; `BotSignOn.Prior`, `HandsPrior`, `WaitMs`, `StandMs`;
`BotExpedition.Prior`, `BotColonyRun.Prior`, `MostKinds`.

Counters: the `Sea:` line ends with `colonies: …` (each colony with its land, store, yield a colonist-hour and runs), then
`expeditions: …`, `hands: …`, `runs home: …`, `pirates: …`. `/api/state` → `colonies`. Argus: `do expeditions`; `do colony
<guild>` (or `dial BotColonies.Order <guild>`) presses an expedition now, its clock ignored and its money not.

Log lines: "X of G takes on an expedition to found a colony on …", "X of G puts the ship in at … with N of the party", "G's chest
paid Ngp for the expedition's stores", "G has founded a colony on I at (x, y)", "X put N bars, N ore and N wood into G's colony
store", "X of G loaded N units …", "X brought N units of G's colony goods home …: sold for Ngp — N into the chest", "G's colony on
I … is lost: …", "The Shadow waits on the quay at …", "X's crew forms a company of N against The Shadow's band".
