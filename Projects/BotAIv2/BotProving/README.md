# BotProving — Argus's proving ground

Patrick's order of 26.09.2026: *"the bots dress, train and grow stronger, and nobody goes past the Orc Caves. I want
Argus to spawn a copy of the bot and a creature in Green Acres and judge the bot's strength — counting only the weapon
and the hit points is not enough; the skills, the weapon's properties and everything else must count."*

## Why a fight and not a formula

Everything on the shard used to judge strength by `BotThreat.Power` = HitsMax × (average weapon hit + Magery / 2).
It cannot see armour, hit chance, swing speed, Tactics/Anatomy bonuses, bandages, bottles, healing spells, poison,
resistance or a vanquishing edge. The first hour of fights showed how far off it is: the ratio of proven strength to
the formula ran from **0.07 to 13** depending on the bot and the creature.

## Files

| File | What it decides |
|---|---|
| `BotStandIn.cs` | The double: a `PlayerMobile` (not a `BotMobile`, so no population hook sees it) with the bot's body, raw stats, skills, everything worn and the fighting part of the pack, copied by the engine's own dupe. Deleted after the fight; deleted on load if a save caught it. |
| `BotTrial.cs` | One fight at a ring, refereed on a 200 ms beat. The engine swings; the double answers with the bot's own primitives (`BotStrike`, `BotMend`). Reads R. |
| `BotDriver.cs` | One double's hands in a fight — bottle, heal, the blade drawn or the bow put away, a step closer, the best spell — shared by the single fight and the company's. |
| `BotRoomTrial.cs` | A guild's company of doubles against a dungeon's worst room (the spawner whose creatures add up to most): cleared, wiped out or called; the delve's proof for anything past the easiest dungeon. |
| `BotProving.cs` | The schedule (the dungeon ladder), the record, and `Against` — the strength the delve judges by. |
| `BotProvingStore.cs` | Keeps the readings across restarts (`Saves/BotProving`). |
| `BotProvingModule.cs` | Config (`Configuration/bot-proving.json`), the boot sweep, the referee's timer, the five-minute summary. |
| `BotWake.cs` | Wakes the ground round the bots as a client would (off by default; see "The world sleeps" below). |

## What is measured

**R = share of the creature killed × HitsMax / (damage taken − health healed)** — how many of these creatures the
bot's health pays for, one after another. Floored at 2% of health net damage, capped at 5 (a walkover is a lower
bound, not a measurement). For a caster also × mana pool / mana spent, the smaller of the two.

**Strength = R × the creature's `Power`**: the bot's strength on the very scale the dungeons are ranked by.

## The ladder

The rungs are each dungeon's worst creature, weakest first (read off the spawners at boot; the first boot read
OrcishMage, JukaMage, OgreLord, LichLord, PoisonElemental, Dragon, WhiteWyrm, Balron, AncientLich, AncientWyrm).
A bot is tried on the lowest rung it has not beaten (R ≥ 1), climbs a rung per win, and is tried again when its
build moves (fighting skills ±5, stats ±5, armour ±4, weapon or its damage) or after 90 minutes.

## How the delve uses it

`BotDelver` reckons a band per dungeon, in the order the call would raise it (guildmates first, then nearest), each
body at `BotProving.Against(bot, dungeon)`:

- measured on that dungeon's worst creature: R × its might;
- measured only on lower rungs: at most min(R, 1) × the might of the strongest creature it beat × 0.75 — nothing is
  extrapolated upwards;
- measured only on higher rungs: R × might as measured there;
- never measured: the old formula × the ground's calibration (median of proven / formula over real fights, R < 3,
  at least five of them, clamped to 0.25–4).

Before a raised party goes down, `BotDelver.Recheck` reckons it again on its actual members and sends it to the
richest dungeon it can answer (or the weakest there is).

On top of the sum:

- **the easiest dungeon is judged by the old formula** (build 250): it is where a band learns, and one awake orcish
  mage beats nearly every fresh bot alone while bands of them come back from the Orc Caves with twenty down;
- **the leader counts, and every member must hold a fifth of the dungeon's worst** (`WeakestShare`, build 242): three
  parties lost their leaders in Wrong under an archer at R 0.01 and a crafter without armour;
- **past the easiest dungeon, the guild's company must have cleared its worst room within two hours** (`BotRoomTrial`,
  build 246): five of the guild's fighters' doubles against the spawner whose creatures add up to most, up to six of
  them. A room is not its worst creature one at a time.

## The world sleeps where no client is

A map sector wakes only when a mobile **with a network connection** walks into it (`Map.Sector.OnEnter`), and a
creature in a sleeping sector has its AI timer stopped (`AITimer.ShouldStop`, `BaseCreature.OnSectorDeactivate`): it
does not walk, look for enemies, cast, breathe, heal or run. It still swings back at whoever struck it first while that
stands next to it (`Mobile.AggressiveAction` makes the aggressor its combatant; the swing is the engine's combat
timer). Bots hold no connection, so **with nobody logged in every creature on the shard is asleep** — in the dungeons,
in the fields, on the roads.

- The proving ground wakes its own rings before every fight (`BotProving.Wake`, build 249): a measurement of a bot
  against a creature that cannot fight back is not a measurement.
- `BotWake` can wake the bots' own ground the same way: every 2 s, the 5×5 sectors round every living bot that
  qualifies (`DeepsOnly`: inside a dungeon), let go 60 s after the last bot leaves unless a client is near. **Off by
  default** (`bot-proving.json`: `Wake`, `WakeDeepsOnly`; dials `BotWake.Running`, `BotWake.DeepsOnly`) — every balance
  on the shard was struck against the sleeping world, and waking it is Patrick's call.
- `do awake` counts, round every bot, the creatures whose AI is on.

## Door (by hand, through Argus)

- `do prove <bot> [<Creature>]` — that bot's double next, against the named creature or its due rung.
- `do proof <bot>` — every reading, and its strength against every dungeon's worst.
- `do proofs` — every bot measured, strongest first, with the ladder.
- `do awake` — of the creatures near the bots, how many have their minds on.
- `dial BotProving.Judges false` — the delve back on the formula; `dial BotProving.Running false` — no fights.
- `dial BotWake.Running true` — the bots wake the ground round them (inside dungeons unless `BotWake.DeepsOnly false`).

## Lessons of the first hour (26.09.2026)

- A walkover read as R 20 and, extrapolated up the ladder, moved four offers to Terathan Keep (build 234). Fixed by
  the cap and the no-upward-extrapolation rule (build 235).
- Summing the strongest four bodies in reach let one Baron decide every band's dungeon; the band is now reckoned in
  the call's order (build 236), and again on the party actually raised (build 237).
- For two hours every creature on the ground was asleep (build 249): archers shooting from six tiles were never touched,
  no mage creature ever cast, and two companies "cleared" Wrong. Awake, four companies were wiped out there in half a
  minute, and 33 of the first 35 single fights were lost.
- A bot that swaps bow and blade read as a new build at every swap, and read off everything carried, a looted
  scimitar on its way to a stall was one too; the build is the best blade and the best bow, a bow under ten shots
  dry (builds 251, 253).
- Archers with 130–150 arrows lost having dealt nothing. Paralyze was the first suspect (up to 7 + a fifth of the
  caster's Magery in seconds, until the next hit breaks it), but the readings show 0–9 s of it per fight; a fight's line
  now ends with the steps taken, the shots fired and the seconds stood paralysed (build 252), and the first three bow
  fights after a boot are traced once a second (build 254). The trace settled it: the bow fires every 4.4–5.5 s as the
  era says, the mage closes to one tile and outlasts a 70-hit archer — weak, not broken. The double's first swing had
  been late by a whole swing (equipped before its stamina was filled), and the quiver is out of the build (build 255).
- The dungeons woken round the bots for six minutes (04:08–04:14): nine dead in the Orc Caves, both parties' leaders
  fallen within a minute and a half of going down. The wake stays off until Patrick says otherwise.
