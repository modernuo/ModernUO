# BotDay — the daily cycle

PLAN-life A1 (Patrick, 08.10 and 12.10.2026): *the island has a rhythm. At dawn the markets fill; through the day bots
work; at noon some eat; in the evening the taverns and firesides are full and the roads empty; at night the watch walks,
thieves come out, most bots sleep at home, at an inn or by a fire. A player at 22:00 finds a different island from one at
10:00.*

## Layout

| file | what it is |
|---|---|
| `BotDay.cs` | The clock (six parts, local time, read into a static every two seconds), the auction's `× day` factor with its floor, the counters, the `Day:` line and the `day` key of `/api/state`. |
| `BotTimetable.cs` | One bot's timetable (wake, bed, meal and supper hours, evening habit, night owl, bed place) and the register `BotTimetables` (by name for the store, by serial for the auction), with the draw from name, class and nature. |
| `BotDayStore.cs` | Persists every timetable (`BotDay`, shape 1); `do reset` wipes it through `BotTimetables.Wipe`. |
| `BotDayConfig.cs` | `Configuration/bot-day.json`, PascalCase keys; the six hours are taken together or not at all. |
| `BotDayModule.cs` | Registers itself (priority 42), loads the file, draws the living bots' timetables at boot, runs the clock, writes `Day:`. |

## The weight

Outside the auction's root, beside the calling, the toll and the nature: `score = estimate × root(product) × calling × toll ×
nature × day`. `Lift` 1.5 up, 1/`Lift` down, half the lean on leisure that is not the bot's own habit, the whole held in
`Least` 0.5 – `Most` 2.0. The take line says `; × 1.50 for the evening (tavern)` when it bites.

| the bot's own part | raised | lowered |
|---|---|---|
| dawn | market work (trade, peddle, shopkeep, restock, resell, order, acquire, supply, haul, deliver, unload, fetch, fair, keep-store, keeping a tavern) | leisure |
| morning | — | leisure |
| evening | leisure (supper, fireside, camp, home, chat, meet); keeping a tavern (the full house); a prowler's prowl | journeys (travel, voyage, venture, expedition, caravan, delve, scout, prospect, colony-run, escort, a caravan guard, any walk ≥ `JourneyTiles` 200) |
| night | fireside, camp, home | journeys |
| the clock's night, everybody | the night's trades (watch, guard-duty, ward, skulk, steal, burgle, rob, waylay, ambush, lielow, manhunt) | — |
| within an hour of a meal hour | supper | — |

**The floor.** A markdown is not applied to paid work for a bot that is broke (spendable under `BrokeUnder` 100 gp, or
`Urges.Need` > 0) and is counted in `Floored`. A starving or broke bot still works at night.

## Who rests

The prime time's bands and shares (100/75/50 %) are untouched. `BotRest.Look` puts the bots at their bedtime (`Bed` to `Bed` +
`BedWindowHours`, having played `BedPlayedShare` of their hours) first in the queue to tire, lets them tire before their hours
(`Record.AtBed` keeps the next look from waking them), brings the resting back wake-hour-first under the floor, and sends a
field sleeper to a fire before an inn. Night owls (a fifth of fighters, a twentieth of the rest, offices never, thieves of The
Shadow live) live the same day `OwlShift` 10 hours later.

## Measure

`Day:` every five minutes and `day` in `/api/state`: the part and hour, the timetables' mix, offers raised / lowered / floored,
offers taken by part × group (work, leisure, journey, market, night — auction takes and presses both), tavern guests by hour,
sleepers at home / inn / field / ground, `ToBedOnTime`, `ToBedEarly`, `HeldPastBed` (once a bot a spell), `WokeOnTime`,
`FieldFirst`, `FieldFirstMissed`. The guards are the work share, Needs and
deaths per bot-hour: the cycle must pay for itself in life, not cost work.
