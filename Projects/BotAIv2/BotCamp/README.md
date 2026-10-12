# BotCamp — fires in the woods, company round them, and a word on the road

Patrick's point 6 of 29.09.2026: *more wandering, more chance meetings, more talk. Add socialisation: some bots may light
a campfire in the woods and others join them; this counts as rest. Let them stand and chat with each other.*

## Layout

| file | what it is |
|---|---|
| `BotCamp.cs` | The fires: a list of at most `MostFires`, each a hearth tile, a ring of seats two tiles out, the engine's flame burning on it, who holds each seat. The camp's clock feeds, looks round, talks and closes every fire once a second. The four hooks the rest calls (`Played`, `Beds`, `ToFire`, `Settle`, `Rested`). |
| `BotKindle.cs` | Making camp (`BotKindle`, kind `camp`) and who is offered it (`BotCamper`), including the trip to a provisioner for kindling. |
| `BotFireside.cs` | Sitting at a fire somebody else lit (`BotFireside`, kind `fireside`) and who is offered a seat (`BotBeckon`). |
| `BotChat.cs` | A chance meeting in the open (`BotChat`, kind `chat`), the meeting itself (`BotMeeting`), and the looking-round that finds them (`BotMeetings`). |
| `BotCampTalk.cs` | What is said: the phrases, the order of speaking round a fire, and the news a line carries. |
| `BotCampModule.cs` | Loads `bot-camp.json`, adds the phrases to the voice's bank, offers the two proposers, runs the camp's one-second clock. |

## How a fire happens

1. A fire-lighter — `LighterShare` (a half) of the population, dealt by a hash of the name so it survives a restart —
   standing on wild ground (not a town's guarded region, a house, a dungeon, barred ground or a quadrant reading unsafe),
   tired (six-tenths through its session), bored (restless), or with nothing to do for a minute, with no fire within
   `FireSpacing` (30) and nothing hostile within `HostileReach` (12), is offered `camp` at `Prior` (18) a minute, plus up to
   40 as it tires and 30 as it is bored (`BotCamp.Claim`). Once in `EveryMs` (45 min) per bot.
2. It gathers kindling — the pack first, then logs with a fletcher's kit (the engine's bowcraft recipe, one log a stick),
   then a knife or a sword on a tree within `TreeReach` (12), the engine's `BladedItemTarget` — up to `KindlingWant` (5).
   In town with none, a fire-lighter is offered `BotRestock` at a provisioner within `ShopReach` (40): five sticks at two gold.
3. It picks a hearth (its own tile, or one of eight near it) with at least `LeastSeats` (3) standable tiles two out, lays
   one stick on it and double-clicks it — the engine's `Kindling.OnDoubleClick`, which rolls Camping and puts the engine's
   `Campfire` where the stick lay. A beginner fumbles for half a minute; after `MostFumbles` (60) it gives up.
4. Lit, the fire is on offer to every free bot within `JoinReach` (40): `fireside` at 24 a minute, plus the same tiredness
   and boredom. A guest walks over, takes a seat, faces the flame, greets the keeper, is welcomed, and sits `GuestMinutes`
   (4, ±1.5).
5. The engine's flame dims at sixty seconds; whoever sits within two tiles with a stick (the keeper first) lays another
   and strikes it (`RelightWhenDim`). After `SitMinutes` (6, ±a quarter) — or `LonelyMs` (3 min) with nobody come — the
   keeper stops feeding it, and the fire is closed when the flame is out. Something hostile within `HostileReach` scatters it.
6. Round a lit fire with two or more seated, a line every seven to eleven seconds (`BotCampTalk.LineMs`): news of a road
   somebody walked (`BotRoadbook`), the worst ground near (`BotQuad.WorstNear`), the speaker's stall or order or the board's
   best-paid want, its guild's war or aim — or small talk; a third of them answered. At most five lines each. Every line
   lifts the listeners' boredom by `CheerPerLine`.

## Rest

- Sitting at a lit fire, a bot not yet tired has its play counted backwards at the inn's rate (`BotCamp.Played`, one hook
  in `BotRest.Look`): a session back for a rest of `BotRest.LeastRestHours`–`MostRestHours`, times `RestShare` (1).
- A tired bot secure at a fire — thirty seconds seated, the engine's own `Campfire` clock — ends its sitting and leaves the
  world from there at the rest's next look (`BotCamp.Beds`, before the walk home and the inn). The record says
  "a campfire lit by X", unpaid; on return the bot regenerates for `FireBuffShare` (a sixth) of the rest (`BotCamp.Rested`).
- A tired bot with no house and no inn to go to is sent to the nearest open fire, or makes one where it stands, before it
  would leave on the open ground (`BotCamp.ToFire`, after `BotInns.ToInn`).

## Meetings

Every second the camp looks at a slice of the population, so each bot once in `EveryMs` (8 s). A bot that is free — idle,
or at work that resumes (`BotDeed.Resumes`), not committed, not fighting, not in a company, not in a town or a house, not
met in `RestMs` (15 min) — costs one spatial query of `Reach` (3) tiles. With another such bot there, not met in
`PairRestMs` (1 h) and welcome by notoriety, a fifth of the time (`Chance`) both are pressed into `chat`: the errand in
hand is put down, they face each other, greet, answer, and seven times in ten one of them says a line of news; then the
errand is taken up again. A pair that rolled no is not rolled again for two minutes.

## What to check

- Boot: `Camp is on: 50 % of the bots light fires …`; `Proposer Camper offers work on the Free rung`; `Proposer Fireside …`.
- `… has lit a campfire at (x, y) with N seats round it (Camping 3.2); it means to keep it 6 minutes`.
- `a campfire lit by X at (x, y) is out after 7.4 minutes — it burned down: the evening is over; 2 came to it, 9 lines were said and 0 slept by it`.
- `X was pressed to chat: having a word with Y: met Y on the way; put down mine … to take up again`, then `X took up mine … again after chat`.
- `… leaves the world at (…) by a campfire lit by X, unpaid after …`; on return `…, rested by a campfire lit by X: regenerating until HH:mm`.
- The five-minute `Camp:` line: fires lit, joined, went offline at a fire, chats, and every refusal by name.
- `logs/bot-speech.log`: the lines themselves, `[local]`, with the speaker's place.

## Traps

- **The engine's campfire burns a hundred seconds.** A fire nobody feeds goes dark at ninety and is deleted at a hundred;
  the fire list's `Warm` carries it `FireGraceMs` (30 s) between flames. A fire whose keeper left with the only kindling is
  banked, not kept.
- **Kindling from the pack lands beside the striker, not under it.** The engine picks a random free tile next to whoever
  double-clicks a stick in the pack; laid on the ground first, the flame is where the stick lay. That is why the hearth is
  one tile and the seats are counted round it.
- **Camping at the skill cap never rises.** A player at the cap gains nothing, and at nought the roll never catches, so
  such a bot is never offered a camp (`BotCamp.Lights`); it can still sit at one.
- **Every one of these is unpaid work,** priced at its claim and never corrected by the ledger. Every reason it fails is
  asked by its proposer first, and a bot is not offered another for `EveryMs` whatever came of the last.
- **`LeftOutside` in the inns' summary counts the fire's sleepers**, as it already counts the house's: `BotInns.Settle` is
  asked first and sees no inn. The `Camp:` line's own count is the true one.
