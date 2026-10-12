# Relics and banners

Patrick, 07.10.2026, approved: "4–6 unique items: the Crown of Britannia (a chest behind a lair boss), each guild's banner (hangs
in its hall), the Bell of Minoc, the Anchor of Trinsic. The holder gets something tangible and visible: −5 % prices in the relic's
town, +appeal, the right to call the Great Fair. A relic can be stolen (thieves get a target), taken in war (the loser's banner
passes to the winner: humiliation, a reason for revenge), ransomed, returned. Biographies remember it; the herald tells it; the
map pins the relic and its holder."

**Module** `BotRelicsModule` (registers itself from `BotRelicsCore.Configure`, priority 46, after the events) · **Config**
`bot-relics.json` (PascalCase; a starter file is written on first boot) · **Writes** `Relics:` · **Store** `BotRelicStore`
(`Saves/BotRelics`)

| offers work | as | on the rung | handing out |
|---|---|---|---|
| `BotRelicCall` | Relic | Free | `BotRelicQuest` (wraps `BotSweep`) |
| `BotRelicBearer` | RelicHome | Free | `BotRelicHomeward` |
| `BotRelicSnatch` | RelicThief | Free | `BotRelicTheft` |

| file | decides |
|---|---|
| `BotRelicItem.cs` | The relic as an engine item: not movable by a player, blessed, never decaying; found again after a restart by itself. |
| `BotRelics.cs` | The registry: the five town relics and a banner per guild, whose each is and since when; the gifts (the counter, the appeal, the Great Fair), the war's end and revenge, ransoms and returns, the fence, the news, Argus's two verbs, the page. |
| `BotRelicLair.cs` | A town relic in a lair behind a warden: where, when, the warden sized to the company that came, who takes it; the captain's call. |
| `BotRelicHomeward.cs` | The carrier's walk home with a relic, and setting it up in the hall. |
| `BotRelicSnatch.cs` | A thief's try at a relic in a carrier's pack. |
| `BotRelicStore.cs` | Keeps the registry across restarts; settles a lair a restart cut short. |
| `BotRelicsModule.cs` | The module, its configuration, its clock and its summary line. |

## The relics

| key | relic | town | look |
|---|---|---|---|
| `crown` | the Crown of Britannia | Britain | gold helm |
| `bell` | the Bell of Minoc | Minoc | hand bell |
| `anchor` | the Anchor of Trinsic | Trinsic | anchor |
| `lantern` | the Lantern of Vesper | Vesper | lantern |
| `scales` | the Scales of Yew | Yew | scales |
| `banner:<guild>` | the banner of <guild> | — | one of the engine's standing banners, by the guild's name |

Each is a `BotRelicItem`: `Movable = false` (no player lifts it, no container is looted of it), blessed (it stays in a dead
carrier's pack), never decays, sold by no NPC. Everything that moves it is code. Places: **hidden** (out of the world, waiting to be
rumoured), **lair** (in a lair's chest), **carried** (a bot's pack, for its guild), **hall** (on the floor of its guild's hall, at
the free tile furthest from the door, `BotFittings.Spot`), **kept** (the guild has no hall, or no free floor: out of the world but
the guild's), **stolen** (a thief's pack). The registry looks at every relic every `SyncEveryMs` (30 s): a hall that moved or grew
gets its relics re-hung, a new guild gets its banner, a carrier gone or out of its guild gives the relic back to the guild, a relic
left in a pack for `CarryMostMs` (1 h) is fetched home, a banner of a guild gone is taken down.

## The rules, with their numbers

- **The lair** (`BotRelicLair`). Every `EveryMs` (4 h ±25 %), the first `FirstMs` (40 min) after a boot, with the events on: one
  hidden town relic is laid in a chest on open ground 60–200 tiles out of its town (twenty tries for hostile ground first) and the
  crier announces it. A free fighter within `Reach` (300) is offered the call (`relic` in the ledger, 60 gp/min until measured) to
  raise a company and march, as a militia does (`BotSweep`). When a captain stands within `ArriveReach` (20) the warden comes out:
  the dearest of twelve kinds the company record prices at most `Odds` (0.7) of what the company brings, no caster, nothing that
  waits to be struck, on a 6-tile leash. Too weak for any: the company is sent away and counted. The warden falls: the relic goes
  into the pack of the captain whose company felled it (or the killer, or any captain at the chest). The warden is taken back
  after `GoneMs` (3 min) with no company near; a lair nobody clears in `LapseMs` (2 h) hides the relic again.
- **Home** (`BotRelicHomeward`). The carrier is offered the walk to its guild's hall (unpaid on purpose, held against ordinary
  offers) and sets the relic up on the floor.
- **The gifts.** A town relic held openly (hall, kept or carried) by a guild: its members pay `Discount` (95 %) at that town's NPC
  counters — multiplied into `BotBurgh.Factor` and settled as its rebate (less coin destroyed at the NPC, none made); the guild's
  appeal rises by `Appeal` (3) per relic it holds other than its own banner (`BotGuilds.Appeal`, guildlife14's measure of whether a
  guild is wanted); and once a day (`GreatEveryMs`), at most one on the island every `GreatGapMs` (6 h), its holder calls the Great
  Fair in that town (`BotFair.CallGreat`): the day's fair with twice the rebate (80 % at the counters), outside the turn of the towns
  — the ordinary fair keeps its day. A guildless carrier gets the discount alone.
- **The thief** (`BotRelicSnatch`). A thief of `BotCrime` (The Shadow or a recruit) at `Bar` (30) Stealing or better, within
  `Reach` (150) of a carrier of another guild, is offered the relic. One try on the island every `EveryMs` (30 min), and each is one
  of `BotCutpurse.PerHour`. Beside the carrier: lifted with Stealing/100 × `Odds` (0.6), at most `Most` (0.6). A miss is always
  seen; a lift `SeenChance` (50 %) of the time. Seen: the carrier cries thief and reports it (`BotCrime.Report`, a lift weighing two
  reports and `Worth` 300gp, so the thief is posted at once). A relic in a hall is never a thief's target.
- **The fence.** After `FenceWordMs` (10 min) the guild the relic was taken from buys it back for `RelicRansom` (1000gp) out of its
  chest, if the chest keeps `ChestKeep` (500) after it; after `FenceWaitMs` (2 h) the richest guild that can pay buys it. The coin
  goes to the thief's account, booked aside of its work. A thief caught (in a cell), dead or gone gives the relic back free.
- **War.** At a war's end the loser's banner — and with `WarTakesAll` every relic it holds — goes to the winner's hall
  (`BotWar.End`). The loser thinks `Grudge` (−30) worse of the winner, written in its book of grievances as "its banner taken",
  so its envoy can state it; and for `RevengeMs` (6 h) after the capture it may declare on the holder of its banner past a truce or
  its own declaring clock (`BotWar.MayDeclare`), never past might, tribute or a war already standing. After `RansomAfterMs` (1 h)
  with no war between them it buys the banner back for `BannerRansom` (1500gp), chest to chest (`BotChest.Pay`), keeping 500;
  after `ReturnAfterMs` (24 h) the holder sends it home unasked.

## The money

Nothing mints. A ransom and a banner's buy-back move coin chest to chest or chest to the thief; the counter's discount is a rebate
on coin paid to an NPC; the warden's gold is the engine's own loot — a kill. No relic is sold to an NPC.

## Telling it

Every change of hands is a log line in the house style, a line of `BotRelics.News` (the last 20, for the herald; `NewsCount` says
how many were ever written), a dashboard event (kind `relic` or `crime`) and speech: the taker in the world channel, the carrier
to its guild, the thief's mark crying thief, the winner's head and the loser's. The taker, the thief, the robbed carrier and both
heads of a war keep it in their biography (`BotBiography.Keep`, kinds `relic`, `relic-theft`, `relic-robbed`, `relic-war`,
`relic-lost`). `/api/state` has a `relics` block: every relic with its holder, place, minutes held and its pin; the lair; the
counters; the news. The dashboard's `relics` layer pins each relic lying in the world and the lair.

## Dials

Every number above is a `public static` property, reachable by `dial <Class.Name> <value>`. Switches: `BotRelics.Running` (all of
it), `BotRelicLair.Running`, `BotRelicHomeward.Running`, `BotRelicSnatch.Running`, `BotRelics.Revenge`, `BotRelics.WarTakesAll`.
The gifts: `BotRelics.Discount`, `BotRelics.Appeal`. To lay a lair now: `dial BotRelicLair.CallNow true`. Argus: `do relics`;
`do relic <key or name> ; <guild>` hands one to a guild's hall (`hide` for a town relic puts it back into hiding).

## How to measure

The `Relics:` line every five minutes: relics hidden and held, trophies, hangings, the lair (laid, captains, companies, wardens,
too weak, cleared, lapsed, deaths at a lair), carried home, war's spoils and revenge waivers, ransoms and returns, thieves and the
fence, counter prices and rebates, Great Fairs. Watch deaths per bot-hour against the build before (a lair is a fight, sized to the
company), the work finished share (`relic` and `relic-home` are new kinds) and the median wealth (ransoms move coin between chests
and thieves).
