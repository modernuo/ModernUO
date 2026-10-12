# Events of the world

Patrick, 07.10.2026 00:10, five ideas approved: seasonal invasions, a daily fair, caravans, the night, and Argus's hourly
rumours. Each is behind its own switch, announced by the town crier (Argus's herald when the debugger is on, the world
channel and the dashboard's event stream — kind `event` — always), logged at its start and end, and counted on the
`Events:` line every five minutes (`BotSeasons.Describe`).

**Module** `BotEventsModule` (registers itself from `BotEventsCore.Configure`, priority 45, after `BotCore`) · **Config**
`bot-events.json` (PascalCase; a starter file is written on first boot) · **Writes** `Events:` · **Store** `BotEventsStore`

| offers work | as | on the rung | handing out |
|---|---|---|---|
| `BotMilitiaCall` | Militia | Free | `BotMilitia` (wraps `BotSweep`) |
| `BotFairCall` | Fair | Free | `BotFairgoer` |
| `BotCaravanCall` | Caravan | Free | `BotCaravan` |
| `BotGuardCall` | Guard | Free | `BotGuard` (fights through `BotSlay`) |
| `BotRumourCall` | Rumour | Free | `BotRumourSeek` |

| file | decides |
|---|---|
| `BotSeasons.cs` | The switch, the clock (5 s), the crier, the one way coin reaches a bot, holding and returning the city's coin, the `Events:` line, and the seams the debugger fills. |
| `BotInvasion.cs` | Which town is invaded and where, the creatures sized to the militia that came, the head money, and the end of a raid. |
| `BotMilitia.cs` | The town's call offered to a fighter, and the militia: the shard's patrol with the town's name on it. |
| `BotFair.cs` | The fair's calendar and town, its counter price, the lottery, and the championship at the fair. |
| `BotFairgoer.cs` | The walk to the fair offered to traders and crafters, and being there. |
| `BotCaravans.cs` | What a town is short of, the caravan's hold, delivery and pay, and the town's price after it. |
| `BotCaravan.cs` | Leading a caravan (a trader) and guarding one (a fighter). |
| `BotNight.cs` | The shard's own hour, and what the night changes on the roads and in carriage. |
| `BotRumour.cs` | The rumoured chest, filled from the crown's purse, and the race to it. |
| `BotEventsStore.cs` | What the events owe, kept across a restart and settled at the next start. |
| `BotEventsModule.cs` | The module, its configuration, and the way in. |

## The money

Nothing here mints. An invasion's head money and a caravan's pay are held out of the city's purse the moment they are
posted (`BotCity.Escrow`) and paid out of the hold (`BotCity.Disbursed`) or handed back (`BotCity.Refund`); the city keeps
`BotSeasons.CityReserve` (1000gp) whatever an event asks. A lottery is tickets charged (`BotMarket.Charge`, which destroys
the coin) and the same sum paid into one winner's account. A rumour's chest is filled from the crown's purse (the revels',
filled by the guilds' tax: `BotRevel.Lend`) and goes back there unfound (`BotRevel.Repay`). The fair's lower prices are
the town's rebate at the counter (`BotBurgh.Settle`): less coin destroyed at an NPC, none made. A restart hands back
whatever was held (the hold to the purse, the tickets to their holders), takes the invasion's creatures off the island,
and keeps the rumoured chest — the world save keeps the chest and its gold anyway.

## Invasion

Every three hours (±25 %), the first 45 minutes after a boot. The town with at least four free fighters within 120 tiles
of its square that was invaded longest ago — and when that town has no place, the next (life18a); a place on one of its
drawn roads 20–70 tiles walked past where the road leaves guarded ground and within 400 tiles of the square (`PastEdge`,
`PastEdgeMost`, `MostOut`; the road sampled every 4 tiles), else anywhere 60–100 tiles out of the square (open ground,
out of town, reachable, not deadly, not closed); a dungeon whose surveyed kinds come out (the waves' list when the survey has none);
1500gp held. **Nothing is put on the road at the call.** The town's fighters are offered the militia: the shard's patrol
(`BotSweep`) under the town's name — the captain calls the free fighters within 40 tiles, gathers, marches and holds the
ground as a company, under every company16 rule (the choice gate, the muster at the enemy, the pull-back at two deaths
in a minute). When the captain stands within 20 tiles of the place the first wave comes out ten tiles beyond it, on a
ten-tile leash: kinds priced at the company record's price, summed to at most `Odds` (0.8, never above 1.0) of the
company's `Able()`, no one creature above 40 % of it, no casters, nothing that does not attack first. The next wave waits
for the last to fall and the rest; three at most. Each creature that falls to a bot (or its pet) pays its killer the head
money, into its account. The raid ends repelled, when the militia has been off the field for two minutes, when no
company came in twenty minutes (nothing ever spawned), or forty minutes after the first wave; whatever stands then is
deleted and the unearned hold goes back. A town asks at most three captains (`MostCalls`): a call that cannot raise a
company of three is a failure on the work line. Deaths within 30 tiles of the field are counted.

*Why no invasion ran on build 416 (life18a, 07.10.2026):* the place test refused any tile inside a town's **box** — the
union of its region's rectangles — and Britain's box is 648 × 500 tiles round its square, so every tile 60–100 out of the
square lay inside it; and only the first town was tried, and a town is stamped only when invaded, so Britain was chosen
and refused at every call (4 of 4 in nine hours). Now the region itself answers (`BotSeasons.Why`), the band is measured
from the guards' edge along the road, and every answering town is tried in turn. The `Events:` line counts towns passed
over, tiles tried and refused with the commonest reasons; `dial BotInvasion.CallNow true` writes every refused candidate
and why (up to `MostProbeLines`, 60).

## Fair

Once a day, the first 90 minutes after a boot unless one was held within the day (the store keeps the date and the turn
of the towns). Announced an hour ahead, open two hours, in the next town of the turn with three shopkeepers. Bots pay
90 % at its counters (`BotBurgh.Factor` × `BotFair.Counter`). Traders and crafters within 300 tiles are offered the walk
(`fair` in the ledger, 15 gp/min until measured), buy a lottery ticket on arrival and stay three minutes. Every minute
any bot standing in the town with 200gp to spare buys a ticket (10gp, one each). The pot goes to one ticket drawn at the
close. The championship is asked for at the opening (`BotTourney.Start`, which keeps its hour) and any championship
called while the fair is open stands in its town (`BotVenues.InTown`).

**The Great Fair** (relics20, 07.10.2026): the guild holding a town's relic calls it in that town once a day (`BotFair.CallGreat`,
asked by `BotRelics`): the same fair, announced `AheadMs` ahead, its counters at twice the rebate (`BotFair.Rate`, 80 %), outside the
turn of the towns and the day's calendar — the ordinary fair's clock is put back when it closes. Counted as `Great Fairs held`.

## Caravans

Every 45 minutes, the first 20 after a boot: a kind on `BotShops.Dry()` that one town's shopkeeper holds under a quarter
of its shelf and another town's at least 120 tiles away sells. Ten at most, 60 stones at most. Held: the goods' cost, the
premium (25 % over cost, at least 100gp) and three guards' wages, both at the night's rate in case. The wage is
`GuardRate` (60gp, a hunt's) a minute of the trip as arrivals measured it (`Trip`, 8 minutes before one), at least 100gp.
A trader within 300 tiles is offered the lead, buys with its own coin, waits 150 s for guards, walks. Fighters within 60
tiles of the trader are offered the guard (three at most): they follow it and set on what attacks it or them, read off the two
bodies' own aggressor lists, when the guards together bring 1.5× the thing. A trader is of the neutral class and cannot
raise a company, so the guards walk with it rather than under it. On arrival the goods go onto the short shelf, the trader
is paid cost plus premium, each guard within 24 tiles of it or of the square its wage, the rest goes back, and the town's
price for the kind is ×0.95 for two hours. Lost when the trader dies, drops it, or takes 45 minutes.

*Why no guard was paid on build 416 (life18a):* of the two hired, one dropped the post at the counter ("could not get one
tile closer" to a trader standing inside a shop) and one fell 60 tiles behind for a minute when the trader stepped through
a gate; a guard not within 24 tiles of the trader the moment it arrived was paid nothing. Now a guard near the counter waits
where it stands, falls behind only after three minutes, and a guard still on the road at the arrival has its wage kept for
five minutes (`LateMs`) and paid when it reaches the square (counted `paid late`, `owed`, `lapsed`, and `put the post
down`). Only 2 of 43 offers were taken because the post was priced at 30gp a minute against a hunt's 60–100.

## Night

22:00–06:00 by the engine's clock (`Clock.GetTime` at the population's home: a game day is two real hours, its night 40
minutes). Every node the planner prices costs ×1.25 (`BotDanger.Cost`), the straight-road check dares ÷1.25
(`BotDanger.Road`), a haul's carriage is ×1.25 within the buyer's ceiling (`BotTrading.Quote`), and a caravan's premium
and wages are ×1.25.

## Rumours

Every hour, the first 20 minutes after a boot, with the debugger on. A wooden chest of 300gp from the crown's purse on open
ground 60–200 tiles out of a town; on ground the zones read hostile it is a lair, and only a bot bringing three times the
fight there is offered it. Two seekers at most; the first to stand at the chest takes the coin into its account; the
other fails ("found first"). Unfound in 40 minutes, the chest goes and its coin returns to the crown.

## Dials

Every number above is a `public static` property, reachable by `dial <Class.Name> <value>` at Argus's door. Switches:
`BotSeasons.Running`, `BotInvasion.Running`, `BotFair.Running`, `BotCaravans.Running`, `BotNight.Running`,
`BotRumour.Running`. To call one now: `dial BotInvasion.CallNow true`, `dial BotFair.CallNow true` (announced now, open in
`AheadMs`), `dial BotCaravans.CallNow true`, `dial BotRumour.CallNow true`.
