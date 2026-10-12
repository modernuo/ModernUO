# Officers: the quartermaster, the herald and the scout

Patrick's approval of 07.10.2026 (night 17, "a livelier world"): three officers appointed by the guild's head, each with real
actions and its own log signature. Standing rules kept: no handouts (a kit is a debt, the store is the members' goods, a bout's
purse moves chest to chest), nothing across hostile guilds, honest counting (every gate its own counter, all in `Guilds:`).

| File | What is in it |
|---|---|
| `BotOfficers.cs` | the posts: who holds which, the head's review every six hours, fit by class and veteran status, the guild's news queue, Argus's `officers` and `appoint`, the `officers:` clause, the page's `officers` object, the phrases |
| `BotGuildStore.cs` | the guild's store at its house or hall: goods in (porter, hand-in, bought), kits out as debts, the lien that collects them, the surplus shelved |
| `BotQuartermaster.cs` | the quartermaster's shift at the store, the guild's wants paid back by the chest, and `BotKitDraw` — a member walking in for its kit |
| `BotHeraldStand.cs` | the herald's stand by the bank: invitations, the guild's news read aloud, the bout's challenge |
| `BotGuildBout.cs` | a bout between two guilds' champions at the herald's stand, the purse chest to chest, Argus announcing it |
| `BotGuildScout.cs` | the scout: a rumoured chest reached first, another guild's company shadowed and reported (a warning at war), a patrol of the hostile zones |
| `BotOfficersModule.cs` | the module (`Officers`, registers itself at priority 42), `Configuration/bot-officers.json`, the clock, and `BotOfficersStore` ("BotOfficers", shape 1) |

## The posts

Every minute the officers' clock looks at each guild of four or more (`LeastMembers`), never The Shadow. A post that is empty, or
whose holder left the guild, became its head or is gone from the island, is given at once to the member that fits it best; every
six hours (`TermMs`) the head reviews each post and gives it away only to a member fitting it a fifth better (`Margin`). A resting
holder keeps its post. Fit: quartermaster Crafter 3, Gatherer 2.5, Healer 2, others 1, times renown (fame); herald Healer/Mage 1.5,
Crafter/Gatherer 1.2, others 1, times renown squared; scout Archer 3, WarriorArcher 2.5, Tamer 2, Warrior/Brawler 1.5,
Mage/WarriorMage 1.2, the rest nought, times strength. A veteran (`BotTraits.Veteran`) ×1.4. Nobody holds two posts; the head holds
none. Log: `<Head> appointed <Bot> the <Class> <Guild>'s <post>: <why>; fit F`, `<Head> kept …`; the head says it in the guild
channel; event kind `officers`; the herald later reads it as news.

## The store and the quartermaster

The store is a ledger per guild of consumables (`BotProvision.Consumable`: bandages, draughts, reagents, arrows, bolts) kept at the
guild's town house's heart, else its hall's door; at most 120 of a kind and 30 of a draught. Goods come in three ways:

- the porter (`BotUnload.Store`) gives a whole lot of capped surplus to the store before the bank box when it stands within
  `TakeReach` (40) of the store and the store has room;
- during the quartermaster's shift, members standing at the store hand in capped surplus over their allowance (`KitSlack` 3+);
- the guild's wants: once a shift the quartermaster asks the board for the kit kind the store is shortest of against half of what
  the members' kits ask (twice that at war or saving for a war chest), at most 30 units, only when the chest holds the bill. It pays
  carriers from its own purse like any buyer; every 30 s the goods it received go into the store and the chest pays it back exactly
  what it paid for them (`BotWant.Paid`). Its lines are procurement in `Lines:`.

A gift is counted at the market's price and written to the giver's record as dues given (`BotElection.Gave`). Out: a member short of
its kit (`BotProvision.Short`) and under 150gp spendable or a newcomer (first seen in the guild under two hours ago), owing nothing
and never a defaulter, is offered a walk to the open store (`draw-kit`, within 100 tiles, only while 2.5 minutes of the shift are
left); the quartermaster hands it what it lacks up to its threshold (`BotProvision.Floor`) and it owes the chest the market price.
Every minute half of every rise in the debtor's wealth goes to the chest (`LienShare`, as the moneylender's lien), booked aside; a
debt unpaid after six hours of the debtor's play is written off and the debtor marked; a debtor gone or a guild closed writes it off
too. Once a shift a stacking kind no member drew for three hours, above 60 % of its most, goes in one lot of up to 40 onto the hall's
merchant at the market price, and the merchant's takings over its float are carried into the chest.

The shift: walk to the store, six minutes open (`Committed`, `Still`), then 40 minutes' rest — five when members are waiting for a
kit. Log lines: `X the quartermaster issued Y … a kit …`, `… posted <Guild>'s want …`, `… put N <Kind> the guild bought into …
and the chest paid it back …`, `<Bot> put N <Kind> into <Guild>'s store through the porter`, `<Bot> has paid <Guild>'s store back in
full`, `<Guild>'s store wrote off <Bot>'s debt …`.

## The herald

A stand by the banker nearest the guild's seat: ten minutes, then 45 minutes' rest. Every six seconds it looks 14 tiles round: a bot
in no guild (not barred, not inside its walk-out bar, not an outlaw) or a member of another guild that is discontented — it thinks
−30 or worse of its head (`BotTies`), or its guild has nothing to show while ours has a hall or ground — is invited once in two
hours and joins at 60 % / 35 % through the guild's own doors (`BotGuilds.Recruit`, `BotGuilds.Poach`: the walk-out bar is earned as
by walking out). Never across a war, never a head or an officer, never out of a guild at its fewest. Every 75 s it reads one piece
of news aloud ("Hear ye! …"): the queue (`BotOfficers.News(guild, text)` — open to any folder: elections, births, relics), else a
standing fact in turn (a war and its score, the chest and its purpose, the store, the strength and the head).

## The bout

Once in four hours a herald a minute into its stand challenges the guild nearest its own in might (`BotWar.Might`) that it is not at
war with. The rival declines under 0.6 of the challenger's might, or when a chest cannot hold the purse (5 % of the poorer chest,
100–800gp), or with no champion near. Champions: each guild's strongest member free within 150 tiles, at 80 % health. Both are
pressed to the stand (`guild-bout`), the duel is `BotDuel`'s, the loser's chest pays the winner's (`BotChest.Pay`). Called off if
they are not both there in five minutes. Argus's herald announces the challenge and the result when the debugger runs
(`BotGuildBout.Witness`, filled by `mindedBots/debugger/BotOfficersArgus`).

## The scout

In order: a rumoured chest within reach that is not a lair (the same `BotRumourSeek`, reckoned ×1.5, `scout-rumour`); another
guild's company of four or more within 160 tiles of the seat, followed at 16 tiles for 90 s and reported in the guild channel and as
news (`scout-shadow`) — at war and within 90 tiles of the seat it is a warning: the company's leader becomes the guild's threat
(`BotFeud.Warn`) and the war's muster rallies against it; a patrol (`scout`): the hostile zone within 220 tiles of the seat (never
deadly, never the sea, never over 3× the median bot) scouts watched longest ago, then least awake, then nearest; the scout stands at a
lookout outside its aggro range on the side of home and passes the zone's box to the scan every 3 s for 60 s
(`BotZoneScan.Focus`, the tour's own feed), then says what it saw. A death-map square in the zone that reads high while nothing that
attacks first lives there now is knocked down to a quarter (`BotPeril.Scouted`, as a sweep). Rest 15 minutes between outings.

## Where it is counted

- `Guilds:` ends with `; officers: …` — posts held, appointments by cause, kept at review, posts nobody fit, news; `store:` (stores,
  units, worth; gifts by route; kits, units, debt; debts standing, lien takes, paid off, written off, defaulters; shelved);
  `quartermasters:` (shifts, open now, wants posted / unfunded / refused / lapsed / standing, delivered, paid back, merchant takings,
  draws and their gates); `heralds:` (stands, looks, invited, joined, crossed, declined, refused, news and facts read, gates);
  `bouts:` (challenges, accepted, begun, decided, purses, called off, announced, refusals); `scouts:` (rumours, shadows, warnings,
  patrols, zones watched, scan passes, attackers sighted, squares knocked down, gates). `BotGuilds.Poached`, `BotFeud.Warned`,
  `BotPeril.ScoutedQuiet` count the three doors in their own classes.
- `/api/state` → `guilds[].officers`: `quartermaster`/`herald`/`scout` (`name`, `minutes`, `why`), `store` (kind → units),
  `storeWorth`, `debtors`, `owed`, `invited`, `joinedByHerald`, `newsRead`, `bouts`, `boutsWon`, `boutGold`, `zonesWatched`,
  `companiesReported`, `rumoursFound`.
- Argus: `do officers [<guild>]`, `do appoint <guild> <quartermaster|herald|scout> <bot>`.

## Dials

`BotOfficers.{Running, Quartermasters, Heralds, Scouts, TermMs, EmptyRetryMs, Margin, LeastMembers, VeteranLean, NewcomerMs, NewsMs,
MostNews}`; `BotGuildStore.{Running, MostUnits, MostBottles, TakeReach, PorterGives, GiftsCount, PoorBelow, MostOwed, LienShare,
DefaultHours, PriceGuess, CollectMs, SurplusAfterMs, KeepShare, ShelfLot}`; `BotQuartermaster.{Prior, WaitingWorth, ShiftMinutes,
RestMs, WaitedRestMs, Reach, StoreTiles, Counter, ActMs, Wants, WantMost, WantLeast, TargetShare, WarFactor, WantHoldMs, SettleMs}`;
`BotKitDraw.{Prior, Reach, EveryMs, LeastLeftMs, WalkMs}`; `BotHeraldStand.{Prior, ShiftMinutes, RestMs, Far, StandTiles, LookMs,
Reach, InviteRestMs, JoinChance, CrossChance, SourRegard, Poaching, NewsEveryMs}`; `BotGuildBout.{Running, EveryMs, PurseShare,
PurseLeast, PurseMost, AcceptOdds, Reach, WaitMs, RingTiles}`; `BotGuildScout.{Prior, RestMs, Rumours, RumourLean, Shadows,
ShadowLeast, ShadowReach, ShadowKeep, ShadowMs, WarnReach, ShadowRestMs, Patrols, PatrolReach, HoldMs, FocusMs, Margin, MostVsBot,
WalkMs}`; `BotOfficersModule.{TickMs, SettleMs}`. File keys (`bot-officers.json`, PascalCase): Running, Quartermasters, Heralds,
Scouts, TermHours, Stores, PorterGives, PoorBelow, LienShare, DefaultHours, GuildWants, Poaching, Bouts, BoutPurseMost, Rumours,
Shadows, Patrols.

## Not built, and why

- No election of officers: the head appoints (Patrick allowed either); `appoint` at Argus's door overrides.
- The store is a ledger at the seat, not an item in the world: nothing to lose to a decay, a raze or a save type, and a unit in is
  always a lot deleted, a unit out an item made of the same kind.
- A guild want standing across a restart loses its order record (the board keeps the want): the quartermaster then keeps what it
  bought, and nothing is paid back.
- The porter's gift is not one of `Packs:`' surplus routes (`BotSurplus`); it is counted in the store's clause.
