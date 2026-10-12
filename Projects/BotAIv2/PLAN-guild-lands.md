# Guild lands — tools, territory, standing, war

Patrick's order of 08.09.2026, late evening, in four parts:

1. **Tools are bought, not given.** A guild pays for a workbench and puts it in its hall. Only the owner and
   the friends of that house may use it.
2. **A hall makes land.** The ground round a guild's hall becomes its own: its members work there by
   preference, and non-members are moved along.
3. **Standing.** Guilds form an opinion of other guilds and of individual bots — worsened by working on
   their land, by their lands touching, by blood. Bad enough, and war is declared.
4. **War means killing and looting.** A bot at war kills a non-friendly bot and loots its corpse. This is the
   first time on this shard that a bot takes anything off another bot.

And the consequence he drew himself, which is why part 5 was built first:

5. **Bots must stop carrying everything they own**, or the first war redistributes an evening's work.

---

## What the engine already does, checked before any of this was designed

| fact | where | what follows |
|---|---|---|
| A guild war makes the other side `Notoriety.Enemy` | `Misc/Notoriety.cs` | Killing them is lawful: no criminal flag, no guard reaction. This was already established for sparring in `PLAN-guilds-houses-sparring.md`. |
| Looting an enemy's corpse is not a criminal act | `Corpse.IsCriminalAction` → `CorpseNotoriety(from, this) == Innocent` | An enemy corpse is not `Innocent`, so the loot is lawful. On Felucca `CanLoot` never refuses at all — the only question is whether it is a crime. |
| **Bots already loot corpses** | `BotHunt/BotPickings.cs` | And the rule is `corpse.Killer == body` — only what it killed itself. So "kill an enemy and loot it" needs *no new looting code*: it needs a bot willing to strike, which is part 4's real content. |
| War is one call in this era | `Guild.AddEnemy(g)` (`NewGuildSystem` is false, so `IsWar` is `Enemies.Contains`) | Declaring and ending a war is two lines. The hard part is deciding, never doing. |
| A forge, loom or oven has **no** access check of its own | `Items/Addons/*`, `CraftItem.NearHeatSource` | "Only the owner and friends" cannot come from the engine. It has to be ours, at the one place a workshop is chosen. |
| A house knows its friends | `BaseHouse.IsOwner/IsCoOwner/IsFriend` | Which is the test to use, so the answer is the engine's own and cannot drift from what a door would decide. |

---

## Stage 5 — carrying less (built 08.09.2026)

`BotUnload.Risk`, 400gp. A fourth reason to walk to a counter, beside a heavy pack, a full purse and a
standing order: **the pack is worth more than the bot should be carrying about.**

The three that existed all ask whether the bot is uncomfortable; none asked what a death would cost. Weight
is about walking, coin is about the market, and this is about risk. It is measured with `BotUnload.Sellable`,
the same reading the counter itself makes, so a smith's own hammer and the ingots for the order it is working
do not count against it — only merchandise does.

Counted in the `Needs:` line as *trips begun because a pack was worth more than a bot should be carrying
about*. When stage 3 exists this number becomes a multiplier rather than a constant: on another guild's land,
carry a fraction of it.

---

## Stage 2 — tools bought and owned

**What.** `BotFitter`: a piece of work that buys one workbench for the guild's hall and installs it. Price
per kind, out of the same levy that paid for the hall (`BotEstate.Levy`), so a guild that has just built is
poor for a while and fits out its workshop over the evening.

**Why not free with the house.** Because the guild then has something to *spend* on, and this shard's oldest
economic problem is that its money has almost nowhere to go: the captain's till reached 98,000gp doing
nothing. A hall that can be improved is a sink with a purpose.

**Access.** The one place it can be enforced is where a workshop is chosen — `BotGround.Fire`, `.Hearth`,
`.Anvil`. Each surveyed place records the house it stands in, if any, at the moment it is surveyed; a bot
asking for a forge is only offered one whose house it is a friend of. A bot with no guild, or of another
guild, sees the town's public forges exactly as it does now.

**The trap to avoid:** this must not become a veto. A population whose only forges are in halls it may not
enter is a population that stops smithing altogether. So the rule is *offered*, not *filtered from the
survey*: the place stays in `BotGround`, and the bot that may not use it simply prefers another. Count the
refusals, because a smith walking past three forges it may not use is a fact worth seeing.

---

## Stage 3 — guild lands

**What.** A hall makes a claim: every `BotQuad` square within `BotLand.Reach` of it belongs to that guild.

**What it changes,** each with a bucket:

- **Preference.** Work whose place is on your own guild's land is worth more (a multiplier, in the shape
  `BotGuilds.Kinship` already has). Work on another guild's land is worth less.
- **Presence.** A bot with nothing better to do strolls its own land rather than the town.
- **Eviction.** A member who finds a non-member working on its guild's land takes the errand of moving them
  along: walk over, say so, and — only if standing is bad enough — strike.

**The trap:** an island of four guilds each claiming ninety tiles is an island where every square belongs to
somebody and nobody may work anywhere. Land is a preference and eviction is rare; the measure that says it
has gone wrong is the completion band falling while `evict` rises.

---

## Stage 4 — standing, and war

**A number per pair.** Guild→guild and guild→bot, starting at nought, moved by events:

| event | change |
|---|---|
| worked on our land | small penalty, per errand finished |
| refused to move when told | larger |
| killed one of ours | large |
| shares a border with us | slow drift downwards, so neighbours quarrel and distant guilds do not |
| traded with us, filled our order | upwards — the only thing that mends it |

Below `BotStanding.War`, the guild declares war (`Guild.AddEnemy`), and its members may strike and loot
members of the other. Above `BotStanding.Peace` again, the war ends. Both thresholds are dials, and the gap
between them is what stops a war flickering on and off every minute.

**What makes this safe to try:** the engine's own notoriety does the enforcement. A bot at war is not a
criminal, so the town watch does not intervene and the rest of the population does not turn on it. Nothing
here touches the guards, the karma system or the criminal flag.

**What makes it dangerous:** a war between the Blade (35 members) and anybody is not a war, it is a
massacre — and every death is skills lost and kit lost. So the first war must be small and the numbers must
be read before the second: deaths per hour, skill lost per death, and how much of what was looted was ever
used by whoever took it.

---

## Order, and why

1. **Carrying less** — done first because everything below makes death expensive.
2. **Tools** — smallest, and it makes the halls worth visiting, which stage 3 needs.
3. **Lands** — needs halls (there is one) and gives standing something to be about.
4. **Standing and war** — last, because it is the only part that can make the population smaller.

---

## What was built, 09.09.2026

All four stages stand. `BotEstate/README.md` carries the reasoning; this is the register.

| stage | where | state |
|---|---|---|
| 5 — carrying less | `BotUnload.Risk` | built 08.09.2026 |
| 2 — tools bought and owned | `BotFitter`, `BotBench`, `BotEstate.MayUse` | built 08.09.2026 |
| 3 — guild lands | `BotLand`, factor in `BotAppraisal`; `BotBailiff`, `BotEvict` | built 09.09.2026 |
| 4 — standing | `BotRegard`, moved from five kinds of event | built 09.09.2026, **running** |
| 4 — war | `BotRegard.Warring` | built, **switched on** by Patrick's order of 09.09.2026 |
| 4 — loot | nothing written, by Patrick's instruction | `BotPickings` already does it |

**Presence** — the third bullet of stage 3, *a bot with nothing better to do strolls its own land rather than
the town* — is not a separate errand. It falls out of the preference: prowling has a place like any other
work, so the land factor already makes a prowl at home worth a quarter more than a prowl abroad. A separate
errand would have been a second way to say the same thing.

**The war switch.** `dial BotRegard.Warring true` through Argus, or `"War": true` in `bot-estate.json`. Read
these three before turning it on, all in the `Estate:` line:

- *N wars withheld because war is switched off* — how often the numbers would have declared one already.
- The worst standing between any two guilds, and what moved it. A quarrel driven entirely by border drift is
  a quarrel about nothing; one driven by trespasses and refusals is the thing the plan wanted.
- *N moved along when told, N stayed and were minded* — if nobody ever stays, defiance is doing no work and
  the whole chain from land to war rests on the border drift alone.

And after: deaths per hour, skill lost per death, and how much of what was looted was ever used. Those are
Patrick's own three from the paragraph above, and none of them can be read before the switch is thrown.

**One number that was measured and changed.** Trade at +2 both ways against a border drift of -1 made every
opinion on the island rise: 22 trades against 12 drifts in the first five minutes, so stage four could only
ever end in peace. Trade is +0.5 and the drift is -4 scaled by how much two yards actually overlap. The two
numbers had never been put beside each other before they were both running.
