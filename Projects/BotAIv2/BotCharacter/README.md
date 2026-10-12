# BotCharacter — nature, life story, friends and enemies, veterans

Patrick's idea of 07.10.2026, approved for night 17 ("a livelier world"): *each bot gets 2–3 traits at birth
(greedy/generous, cautious/reckless, homebody/wanderer, vindictive) that really shift its auction of work; a biography —
memory of its three most important events (first death, who saved it, who robbed it) that it tells at the campfire and in
the trade chat; personal friendships and feuds between bots (the one who saved you is a friend who answers your cry first;
the one who killed you in a war is a personal enemy you seek out for a duel); ageing: a veteran with a hundred kills gets a
title and the right to lead companies.*

## Layout

| file | what it is |
|---|---|
| `BotLife.cs` | One bot's record (`BotLife`: traits, kills, counts, three `BotTale`s, at most twelve `BotTie`s), keyed by **name**, and the register `BotLives` (by name for the store and the ties, by serial for the auction, bodies by name refreshed every 30 s). |
| `BotTraits.cs` | The ten traits in five pairs, drawn from the name; the auction's `× nature` factor; the lone bot's daring and flight share; the veteran (`Veteran`, `Credit`, the name suffix). |
| `BotBiography.cs` | The hooks that write a life (death, rescue, raise, heal, extortion, sale, kills, duels), the three tales kept by weight, and the telling (fire, road, after a mood). |
| `BotTies.cs` | Regard from −100 to +100 one way, moved by the hooks, fading in running time; friend first at a dire cry and at a mood line; spurning; `Regard(a, b)` for the guild elections. |
| `BotGrudge.cs` | Seeking out a personal enemy (`BotGrudge`, kind `grudge`, offered by `BotGrudgeProposer`) and the duels of feuds (`BotGrudges`: chance meetings, the challenge, the end). |
| `BotCharacterStore.cs` | Persists every `BotLife` (`BotCharacter`, shape 1). |
| `BotCharacterModule.cs` | Loads `bot-character.json`, adds the phrases, offers the proposer, runs the two-second clock, writes `Character:` every five minutes; `BotCharacterWeb` writes the per-bot fields of `/api/bot/<name>`. Registers itself from its own `Configure` (priority 41) rather than from `BotCore`. |

## Nature

Two or three traits a bot, one side of two or three of the five pairs, drawn by an FNV hash of the name into a seeded
`System.Random` (stable across runs) and then kept in the store. Each trait is one lean of ×(1 ± `Lean`) = ×1.15/×0.85 on
the work it touches, multiplied outside the auction's fifth root like the calling and the toll, the whole held in
×`Least`–×`Most` (0.8–1.25). The take line says it: `; × 1.15 for its nature (greedy)`.

| pair | ×1.15 for the first, ×0.85 for the second | the reverse |
|---|---|---|
| greedy / generous | trade, peddle, shopkeep, haul, plunder, pickings | rescue, escort, housecall, tutor |
| cautious / reckless | — | delve, sweep, raid, harrow, outpost priced down for cautious, up for reckless |
| homebody / wanderer | — | a walk ≥ `FarTiles` (120) or travel, voyage, prowl, venture, scout, prospect: down for a homebody, up for a wanderer |
| vindictive / forgiving | grudge, manhunt | — |
| sociable / solitary | camp, fireside, chat, meet, band, enlist | — |

With the sides drawn evenly the mean multiplier on every group is one; the boot line and every `Character:` line print the
counts and the mean the drawn population actually gives. Cautious and reckless also move the fight: the lone bot's quarry
bar is `BotQuarry.Daring` × 0.85 / × 1.1 (never past `BotThreat.Tolerance`) and the slayer's flight share is
`BotSlay.FleeAt` × 1.1 / × 0.9 (never under `FleeFloor` 0.3; casters are still fought to the end).

## Biography

Three tales, one of a kind, the heaviest kept: first death 50; saved 40–60 (the nearer to death the heavier); raised 38;
extorted 40–60; murdered 60; killed in a war 55; the slayer's war 30 + 2 a kill; best sale 15–40; duel 33/35; veteran 60.
Told at a fire or a road meeting one line in `TaleChance` (0.3) — or a word about the strongest friend or worst enemy —
and after a mood line somebody heard one time in `VoiceChance` (0.12) in the local channel, a tale of a sale going to the
trade room one time in four. Example: *"Ragnar saved me in Orc Cave when an ettin had me at 12 hits."*

## Ties

| event | the one it happened to | the other |
|---|---|---|
| saved from a creature at its cry | +45 for the rescuer | +20 |
| raised where it fell | +45 for the raiser | +10 |
| mended by a healer | +8 | — |
| a company kill (leader and each member) | +0.5 | +0.5 |
| a sale hand to hand | +2 | +2 |
| killed in a war | −75 for the slayer | (slayer +1 kill) |
| murdered (killed by a bot out of war and out of a duel) | −80 | — |
| made to pay under threat | −40 | — |
| a feud fought out in a duel | +40 | +40 |

Friend at +40 either way; enemy at `DuelAt` (−60) this way; spurn at −30. 1 % of the way to nought every 10 minutes (at
least 0.2), grudges at half pace for the vindictive and double for the forgiving; under one is forgotten. Twelve ties a
bot, the weakest pushed out by a stronger one.

**Effects, each counted:** a friend of a dire crier is ranked before every other free bot within reach (`BotCry.Summon`),
and before every other listener for a mood line (`BotSocial.Heard`); a listener at −30 or worse does not answer. An enemy
at −60 or worse within 90 tiles is offered as `grudge` (15/min, unpaid, × nature); reaching it within 8 tiles queues a
challenge; enemies who meet by chance (looked for every 30 s) are challenged with a chance of 1 / 0.5 / 0.2 for vindictive
/ neither / forgiving. A challenge needs both alive, at 80 % health, in no company, on no committed work, not duelling, not
traders, not outlaws, not in a guarded town, no creature near, and their guilds not at war; then `BotDuel` (yield at 35 %,
two minutes) and the feud is settled +40 each way. A pair rests an hour after a challenge and five minutes after a refusal.

## Veterans

`VeteranKills` (100) kills — a lone kill 1, a bot killed in a war 1, a company's kill split among its standing members —
make a veteran, once and for good: a log line, a word to its guild, a tale, " the Veteran" after its name in the world
(`BotMobile.ApplyNameSuffix`) and `title` on its page, the right to be asked for a clearing past the class and novice bars
(`BotClearer`, traders still excepted) and to call a company past the island's ceiling (`BotMuster.MostBound`), and
`BotTraits.Veteran(bot)` for the guild elections.

## Counters and dials

`Character:` every five minutes: traits by kind and the mean multipliers; offers raised and lowered; daring and flight
judged by nature; veterans made, clearings and companies they led; events written by kind, pushed out, too light; tales
at a fire / of a friend / of an enemy / local / trade; kills credited; ties held, friendships and enmities, formed, made,
forgotten, too weak; friends sent to a dire cry of all sent, friends first at a mood, moods spurned; grudges offered,
taken, met, let pass, challenges, duels begun, settled, standing, refusals by reason.

Dials (all live through Argus, file keys in `bot-character.json`): `BotTraits.On`, `Lean`, `Least`, `Most`, `FarTiles`,
`CautiousDaring`, `RecklessDaring`, `CautiousFlee`, `RecklessFlee`, `FleeFloor`, `VeteranKills`, `VeteranTitle`;
`BotBiography.On`, `TaleChance`, `VoiceChance`, `TradeChance`, `MostTales`; `BotTies.On`, `FriendAt`, `DuelAt`, `SpurnAt`, the
eleven event weights, `DecayEveryMs`, `DecayShare`, `MostTies`, `FriendPull`; `BotGrudges.On`, `Reach`, `SeekReach`, `FitAt`,
`PairEveryMs`, `RefusedRestMs`, the three chances; `BotGrudge.Worth`, `GiveUpMs`.
