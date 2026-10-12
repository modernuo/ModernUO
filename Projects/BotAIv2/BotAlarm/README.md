# Alarm: the shard says something is wrong, without being asked

`logs/alerts.ndjson` — one line of JSON per event, appended across restarts. Meant to be watched with
`tail -f`, not searched.

| File | What is in it |
|---|---|
| `BotAlarm.cs` | the channel: raise, still, clear, note, alive; the swallowing of repeats |
| `BotSigns.cs` | the six rules and their thresholds, read once a minute; the work alarm judges finished over the work that ended and names the loudest (bot, kind, reason) of its window (§5 S5, build 64) |
| `BotTail.cs` | counts the errors the shard prints, by reading the tail of its own session log |
| `BotBoot.cs` | the `world` alarm: a boot that loaded fewer than half the mobiles of the best boot on record (`logs/bot-boot.txt`) is a truncated save, said as an error and raised as an alert — the 16.09.2026 incident ran five hours with nothing saying so (build 70) |
| `BotAlarmModule.cs` | the module, the clock, and `Configuration/bot-alarm.json` |

---

## Why it exists

**Everything else the shard knows has to be asked for.** The session log says everything and therefore says
nothing without a question. The debugger answers when spoken to. The summaries come on a clock whether or not
anything happened. So the shard could be broken for half an hour before anybody looked — and the half hour
was the expensive part, because the evidence for what caused it is in that same half hour and it scrolls past
either way.

This is the other direction: the shard speaks first.

## What an event looks like

```json
{"at":"2026-09-07T19:40:29","state":"raised","kind":"errors","n":7,"of":5,"window":"1m",
 "since":"19:40:29","heldMinutes":0,"repeats":0,"say":"7 errors printed in the last 1m; the newest reads: …"}
```

`state` is one of five, and the set is the design:

| state | means |
|---|---|
| `raised` | the first crossing of a threshold |
| `still` | it is still true, said again after `BotAlarm.RestMs` (15 minutes) with the repeats it swallowed |
| `clear` | it has gone back to normal — **a watcher told only about breakage can never be told it is fixed** |
| `note` | a one-off with no beginning or end: the shard came up, the debugger found something |
| `alive` | the hourly heartbeat, with the numbers it is asserting "nothing is wrong" about |

## The three rules the rules obey

**Every event carries a numerator, a denominator and a window.** `Raise` takes `n`, `of` and `window` as
arguments rather than as advice, so a rule that cannot say what it counted cannot be written. This shard has
produced twelve distinct kinds of false alarm and most were the same fault twice: a count with nothing to
divide it by, or a total accumulating since boot read as the state of this minute.

**Everything is a difference over a window, never a total.** Each rule keeps an anchor — a value and the
moment it was taken — and asks its question of the difference. `848 could not afford armour` was true, and
was about the first five minutes of a session that had been running for hours.

**The window reported is the window measured.** Not the one intended. The hour this was written, the
heartbeat was dialled to a minute against a one-minute tick, fired every *two* minutes, and still labelled
its own window `1m`. Windows now carry half a tick of slack and report what actually elapsed.

## The six rules

| kind | fires when | denominator |
|---|---|---|
| `errors` | `ErrorsAt` (5) or more ` ERR]` lines in one look | the threshold |
| `work-not-finishing` | under `WorkFloor` (25%) of `WorkLeast` (20)+ pieces of work finished in `WorkMs` (5m) | work taken on |
| `standing` | `StandShare` (30%) of the population standing still, `StandLeast` (10)+ alive | the population |
| `dying` | `DeathsAt` (10) deaths in a work window | the population |
| `market-quiet` | nothing bought or sold in `MarketMs` (10m) **with stalls standing and wants on the board** | stalls + wants |
| `alive` | every `AliveMs` (1h), unconditionally | — |

Thresholds were chosen against a live shard, not invented: errors were running at 0.3 a minute and the alarm
is at five; four pieces of work in five were finishing and the alarm is at one in four. A rule that fires
wrongly costs more than no rule — the debugger's first day produced ten false alarms against two real
defects.

**Every threshold is a dial**, so it moves through the debugger's door without a restart:
`dial BotSigns.ErrorsAt 3`. That is the point of tuning a watchdog on a living shard — the first version of a
rule is wrong, the evidence arrives over the following hour, and restarting would throw that hour away.

## Two things it deliberately does not do

**It does not reprint the log.** The channel gets *"26 errors in the last minute"* plus one specimen line.
The log is already the place to read errors; a channel that copied it would be a second, slower copy of the
thing it points at.

**It does not stay quiet when all is well.** `alive` writes an hourly heartbeat with the numbers behind it.
Silence on a channel is otherwise indistinguishable from a channel that has stopped working — which is
exactly how an instrument fails without anybody noticing, and this project has already lost an evening to a
monitor whose filter matched nothing at all.

## Errors, and why the shard reads its own log

Serilog is configured once in `Projects/Logger` with a single console sink, and the engine is not modified in
this fork — so there is no seam to subscribe to. Every error, from every subsystem *and from the engine*, is
nonetheless already in one file. `BotTail` opens the newest `session-*.log`, starts at its end (errors
already in it belong to a previous life) and counts ` ERR]` — with the space, because the level sits outside
the bracket and a pattern of `[ERR` matches nothing and reports a clean run for ever.

Two honest limits, both named in the event itself: the file arrives in buffers, so a count can lag the world;
and a reader that falls more than `MostBytes` behind skips to the end and counts the skip rather than
pretending.

## Switching it off

`bots.alarm.enabled` in `modernuo.json`. It is separate from the debugger on purpose: the debugger is an
observer with a model, a body and an opinion, and this is a smoke alarm. A shard being worked on with no
watcher at all should still be able to shout when its work stops finishing.
