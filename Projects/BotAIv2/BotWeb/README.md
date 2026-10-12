# Web: the shard as a page

The whole population as one JSON document on the loopback address, a stream of events, and the page that
draws them. Built on 28.09.2026 for the order "an online dashboard: how many bots there are, where they are,
what they are doing, what routes they walk, and who cannot find a path — with maximum logging."

| File | What is in it |
|---|---|
| `BotEvents.cs` | the event stream: one object per thing that happened, with a sequence number, kept in a ring, written to `logs/bot-events.ndjson`, and fed to live streams; failed searches and alarm lines kept apart for their pages |
| `BotWebSnapshot.cs` | the whole shard as JSON, rebuilt on the game loop every two seconds: population, work, paths, every bot with its route, guilds, wars, squads, alarms, the thinking layer |
| `BotWebCraft.cs` | the crafting chain as the craft systems define it and the market fills it |
| `BotWebHistory.cs` | a sample a minute for the charts |
| `BotWebServer.cs` | the listener: `HttpListener` on `127.0.0.1:2599`, the routes, server-sent events, the log tail |
| `BotWebMap.cs` | a picture of the map from the client's `radarcol.mul`, sampled in slices of the loop and written once |
| `BotWebLive.cs` | the live window: a frame a second for each bot somebody is looking at, and the debugger squad's page |
| `BotWebLivePicture.cs` | the live window's picture: what stands where round a watched bot in the client's own terms, the ground cut as a scene when the bot walks off it |
| `BotArtFiles.cs` | the client's own files read the way the client reads them: art, land textures, hues, tiledata, and the animations in both their forms |
| `BotWebArt.cs` | the pictures the page draws with, decoded on the listener's threads from those files and kept, and the tiledata the page sorts by |
| `BotWebHooks.cs` | what the minds assembly reports about itself, since this one may not reference it |
| `BotWebConfig.cs` | `Configuration/bot-web.json` |
| `BotWebModule.cs` | the module: opens the stream, starts the listener and the clock |

## The routes

| | |
|---|---|
| `/` | the page, `Distribution/Data/bot-web/index.html`, re-read when the file changes |
| `/api/state` | everything, rebuilt every `SnapshotMs` |
| `/api/events?since=N&limit=M` | events after N; `/api/stream` is the same as server-sent events |
| `/api/paths` | the last failed searches and who, why and where they fail |
| `/api/history` | a minute at a time since boot |
| `/api/craft` | material → products → makers, wants, and what the craft system refused |
| `/api/log?n=300&grep=text` | the tail of the session log |
| `/api/bot/<name>` | one bot with the events about it |
| `/api/map.png` | the map picture |
| `/api/health` | how long the snapshot takes and how many are listening; under `live`, what the live window costs |
| `/api/live?bot=Name` | the newest frame of one bot, and marks it watched; `{"pending":true}` until the first is built |
| `/api/live/stream?bot=Name` | the same as server-sent events (`event: frame`), each new frame once |
| `/api/debuggers` | the debugger squad: where each stands, what it is doing and thinking, its last log lines |
| `/api/live/scene?bot=Name` | the ground round a watched bot: land ids and heights, statics and the pieces of houses and boats; the frame's `pic.scene` says which version goes with it |
| `/api/art/land/ID.png` | the 44 × 44 land diamond |
| `/api/art/tex/ID.png` | the texture a slope of that land tile is drawn with; 404 when the client has none |
| `/api/art/static/ID.png?hue=H` | a static or an item through its hue, partial where tiledata or the hue's top bit says so |
| `/api/art/tiledata` | each item's height and the flags the draw order needs, and which land tiles have a texture; `?ids=1,2` for a few in words |
| `/api/anim/BODY/DIR.png?hue=H&a=stand` | one frame of a body: `a` is `stand`, `war`, `war2`, `ride` or `die` (a corpse's last frame) |
| `/api/anim/item/ITEM/DIR.png?body=B&hue=H&a=…` | a worn item as the client draws it on that body; `/api/anim/BODY/meta` gives the eight directions' sizes and anchors |

## Two rules, and the reasons

**The listener reads nothing from the world.** Every answer is a string the loop wrote and gave away, or a
file, or the event ring under its lock. The game runs on one thread and every observer this project has built
has had to prove it costs the population nothing; this one proves it by construction. `BotWebSnapshot.Describe`
prints what the building costs, and `/api/health` prints it live: about a millisecond for sixty bots.

**Every number on the page is a number the log already prints.** The page adds no counters. It reads the
same statics the five-minute summaries read — `BotWill.Taken`, `BotPath.Reached`, `NavigationService.Routes` —
so the page and the log cannot disagree. The one thing added for the page is the split of searches by purpose
(`BotWalk.FixedSearches`, `ChaseSearches`, `FlightSearches`), because a share of all searches reads a chase
after a moving creature and a flight with no goal as failures of the roads, and the 99 % is a question about
roads.

## The events

One object per event, and the `kind` says which: `say` (with a `channel`: world, guild, local, mood, cry),
`work` (`data.ending`: took, finished, failed, dropped, died), `path` (a search that did not reach; `data.outcome`,
`data.reason`, `from`, `to`), `death`, `war`, `alarm`. They come from the hooks the subsystems already had or
now have: `BotWill.Started` and `BotWill.Ended`, `BotWalk.PlanLeg`, `BotWill.Note`, `BotMobile.OnDeath`,
`BotAlarm.Write`. The file is appended across restarts, like the alarm channel.

## The live window

Patrick, 02.10.2026: "a window that shows in real time what is happening in the game and with a bot, say once a second",
and "windows that show the debuggers and what they are thinking right now". The page's **Live** tab.

**Built only for what somebody is looking at.** Asking for `/api/live?bot=Name` marks the bot watched for ten seconds;
at most four are watched, and a fifth takes the place of the one asked about longest ago. The page asks once a second
while the tab is open and visible. With nobody watching, the clock's beat costs a tick comparison, allocates nothing,
and the event hook is off. `/api/debuggers` is built the same way, only while asked for.

**A frame** is one bot on the loop: position and map; HP, mana and stamina with maxima; alive, war mode, mounted,
hidden; the spell being cast; its combatant and its work's foe (name, distance, HP%); work, stage, reason and standing
as the snapshot writes them; guild, company and role; gold in the pack and weight; the destination and the next
waypoints of the walk; the top skills. Round it, a square of radius 20: the terrain as radar colours (base64 of
little-endian 16-bit colours in rows, 0xFFFF for a tile not read yet), every mobile from `GetMobilesInRange` (name,
place, HP%, kind, the engine's notoriety towards the bot, hostile by `BotThreat.Menacing` or `BotRegard.Hostile`,
whom it is fighting, and for a bot its class, guild and work), corpses and fires from `GetItemsInRange`, and the bot's
own shop. And the newest 40 events about it from the event stream and the newest 40 session-log lines naming it, each
with its own number, so a reader that misses a frame misses nothing.

**Terrain** comes from the same `radarcol.mul` as the map picture, read on the loop in eight-by-eight blocks that are
kept (up to 8192), centred again when the bot has walked four tiles, and never more than 1024 tiles a beat. **The log
lines** are read off the newest session log by a task on the thread pool, which reads only the file.

**The debuggers** report through `BotWebHooks.Debuggers` (the minds assembly, `BotDebugWeb`): what each is doing in the
vigil's own order (witness at a meeting, a zone-tour stop, herald at a venue, charting the dungeons, beside a bot; the
marshal's revel and its next ask), the last answer it got from the model with the question, the claim, the rest of the
answer, whom about and how sure, what it is waiting on, and the last lines of `bot-debugger.log` that name it.

`Live` in bot-web.json switches it off. `/api/health` prints `live.frames`, `msEach`, `msPerBeat`, `worstBeatMs`, the
terrain tiles read and what they cost, and who is watched.

## The live picture

Patrick, 02.10.2026: "can the picture be shown straight from the game?" The Live tab's default view is the bot's
surroundings drawn with the client's own pictures; the old scheme is one click away.

**The shard sends what stands where; the browser draws it.** Each frame adds `look` (the bot as the client is told about it:
body, hue, facing, war mode, the mount's body and hue, the worn items as layer, item and hue), the same fields on every
mobile, `ground` (the items lying about, a corpse with its body, facing and what it wears) and `pic.scene`. The ground and
what is built on it — three bytes a land tile, seven a static, base64 — is cut on the loop only when the bot has walked six
tiles off the last square or a house or boat near it has come, gone or moved, and fetched from `/api/live/scene` when the
number moves on. The map blocks are the tile matrix's own; at most `SceneBlockBudget` blocks are read for the first time in
one beat across all watched bots.

**The pictures never touch the loop.** `/api/art` and `/api/anim` decode on the listener's thread from the client folder the
server already knows: `artLegacyMUL.uop` (or `art.mul`), `texmaps.mul`, `hues.mul`, `tiledata.mul`, `anim*.idx/mul` with
`mobtypes.txt`, `Bodyconv.def`, `Body.def` and `Equipconv.def`, and `AnimationFrame*.uop` with `AnimationSequence.uop`.
Opened once by the first request (about 0.1 s), read with positional reads that any thread may make. Each picture is kept as
PNG bytes (at most 4096 or 96 MB, the oldest asked-for goes), says where its feet are in `X-Anchor`, and is cached by the
browser for a week; the page puts the files' fingerprint in every address, so a patched client gets new addresses.

**The client's rules.** A standing person is group 4 (7 or 8 holding a weapon in war mode, 25 on a mount), a monster 1, an
animal 2, a corpse the last frame of its death; directions 0, 1 and 2 are the files' 3, 2 and 1 mirrored. The page draws by
the diagonal `x+y`, then the height the client sorts by (land two below its height or its slope's average, a background one
below, anything with height one above, a mobile or corpse one above its feet), land first where they tie; a slope is its
texture stretched over the four corner heights and shaded by the ground's tilt; standing under something solid or a roof,
the roofs and everything at or above the ceiling are not drawn. Over the picture: names in their notoriety colour, health
bars, a red line to the combatant, a dashed one to the work's foe, the route ahead as dots. Positions slide between frames.

`LivePicture` in bot-web.json switches the picture off (the routes answer 503, the frame carries no picture, the page shows
the scheme). `/api/health` prints `live.picture` (frames, ms each, scenes cut and what they cost, blocks read) and
`live.sprites` (decoded, missing, hits, cached, ms each, the worst).

## The map picture

Drawn once from `radarcol.mul` in the client folder the server already knows: the topmost static tile's colour,
else the land tile's, one pixel per `MapScale` tiles. Sampled on the loop `RowsPerSlice` rows at a time because
the tile matrix is not safe to read from another thread; encoded to PNG off the loop. Written to
`Data/bot-web/<map>.png` and not drawn again while the file exists. Without the client files the page draws
the bots on a grid.

## Configuration

`Configuration/bot-web.json`, PascalCase, every key optional: `Enabled`, `Bind`, `Port`, `SnapshotMs`,
`PagesMs`, `HistoryMs`, `KeepHistory`, `KeepEvents`, `KeepPaths`, `MapImage`, `MapScale`, `Folder`, `Live`, `LivePicture`. The
listener binds to the machine itself by default; the page names every bot and every guild's chest.
