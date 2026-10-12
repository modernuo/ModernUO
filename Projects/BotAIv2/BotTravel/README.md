# BotTravel — the towns, the roads between them, and the journeys

Patrick's order of 29.09.2026: *the bots should scatter over the world by themselves, travel, meet new horizons,
build roads to other towns and share what they find; some guilds may settle by other towns, so as not to be so dense
round Britain; give them knowledge of the roads and the ways round.*

## Layout

| file | what it is |
|---|---|
| `BotTowns.cs` | The towns, read from the map's `TownRegion`s (the fields round Britain and Yew passed over). Each has a square — the middle of its box settled on the ground — and `FromHome`, the gates' answer to whether a walker at home can get there. `Within(where)` says whether a place lies within `Roam` (600) of a reachable town, and `BotPopulation.Within` counts it. |
| `BotRoadbook.cs` | One road per pair of reachable towns, drawn square to square by the navigation graph's long tier, a pair a slice; its length is the steps along its waypoints. What the population knows of a road — how often it was walked — is saved in `Saves/BotRoadbook` (`BotRoadbookStore`). The danger of a road is the quadrant map's reading averaged along its waypoints, asked when wanted. `/api/roads` for the dashboard. |
| `BotTravel.cs` | The journey (`BotTravel`) and who is offered one (`BotTraveller`). |
| `BotTravelModule.cs` | Loads `bot-travel.json`, offers the traveller, draws the roads on a 250 ms clock, refreshes the roads' JSON once a minute. |

## How a journey happens

1. A bot with nothing pressing — fit, supplied, in no company, not tired, not underground, and more than
   `EveryHours` (2) since its last journey — is offered a reachable town other than the one it is in, within `Furthest`
   (1800) tiles as the crow flies. Towns whose road nobody has walked are likelier: the weight is
   `1 / (1 + walked / Curiosity)`.
2. The journey is worth `Prior` (8) a minute — above the walk home's five, below any trade — so it is what a bot does
   when nothing else is worth doing. It is steadfast, so a traveller does not turn back for a stall on the road, and it
   is counted as coin so a bot born poor may go.
3. The walk is the ordinary one: the journey draws the route over the graph, a leg at a time, through a cave mouth
   where one is on the way (`BotGates`).
4. On arriving within `Arrival` (10) of the square, the road is walked in the book, and the traveller tells its guild
   what the road read on the danger map: `road:safe`, `road:quiet` or `road:unsafe`, with the band as the reason
   (`bot-voice.json`).
5. From there it is a bot in that town: the population's ground includes every reachable town's surroundings, so the
   quarry, the counters, the seams and the corpses round it are its to work; when nothing there is worth doing, the walk
   home is offered as ever, and home is its guild's seat or the population's.

## Guilds by other towns

`BotSeat.Choose` (in `BotEstate`) seats a guild founded on the shard, when the file and the store say nothing about it:
by `SeatAbroadShare` (0.5, `bot-estate.json`) it settles by the reachable town with the fewest guilds already seated
within `Roam` of its square, `TownSpread` (40) tiles off the square and not too near another seat; otherwise at home.
The seat is written to the store like one set by hand, so a restart keeps it; members are born and rise there, the hall
is raised there, and the walk home leads there.

## What to check

- Boot: `Towns: N of M can be walked to from home: ...`; `Roads: N drawn and M found no way`. A town reached only
  through a gate has no road in the book (the long tier does not route through gates), and is not offered to travellers;
  it is still reachable to anything else that wants it.
- `... is the first offered a journey: from Britain to Minoc`; `... has walked from Britain to Minoc in 9.3 minutes; the
  road reads positive`.
- `{Guild} settles by {Town}`.
- The dashboard's map: the "towns" layer names every town, the "roads" layer draws a road once somebody has walked it,
  dashed until then, red where the ground along it reads unsafe.

## Traps

- **The roam is the population's word for "ground worth wanting".** Widening it to every reachable town's surroundings
  is what lets a traveller work where it arrives; it also lets a hunt in Britain be offered quarry near Trinsic. The hunt's
  own look is fifty tiles round the asker, so this does not happen in practice, but a proposer that looks further than it
  walks would now see further than it should.
- **A road's danger is the ground's, not the road's.** The quadrant map is one map for everybody; a road that reads
  unsafe reads so because something killed a bot on a square it crosses, whether or not that square is on the road's
  own tiles. Good enough to warn a guild; not a reason to route round it — the ways round are the long tier's business.
